using GestaoChamados.Data;
using GestaoChamados.DTOs;
using GestaoChamados.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net; 

namespace GestaoChamados.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // IConfiguration: Usado para ler o arquivo appsettings.json (onde fica a "Chave Secreta" do Token).
        private readonly IConfiguration _configuration;
        private readonly ApiDbContext _context;

        // Injeção de Dependência: O sistema entrega a configuração e o banco prontos.
        public AuthController(IConfiguration configuration, ApiDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // MÉTODO DE LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            // Tratamento de entrada: Remove espaços em branco para evitar erros de digitação.
            var matriculaLimpa = loginRequest.Matricula.Trim();
            // 1. Busca o usuário no banco (independente de maiúsculas/minúsculas).
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Matricula.ToUpper() == matriculaLimpa.ToUpper());

            // Verifica se o usuário existe E se a senha bate com o Hash.
            // NÃO comparamos (senha == user.Senha) porque a senha no banco está criptografada (Hash).
            // O BCrypt pega a senha que o usuário digitou agora, aplica a criptografia e compara com o banco.
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Senha, user.SenhaHash))
            {
                // Retorna 401 Unauthorized.
                return Unauthorized("Matrícula ou senha inválidos.");
            }

            // 3. Se passou, gera o Token JWT.
            var token = GenerateJwtToken(user);
            // 4. Retorna o Token e os dados básicos do usuário
            return Ok(new
            {
                token = token,
                Usuario = new
                {
                    Id = user.Id,
                    NomeUsuario = user.NomeUsuario,
                    Matricula = user.Matricula,
                    Role = user.Role // Perfil
                }
            });
        }

        // MÉTODO AUXILIAR: CRIAÇÃO DO TOKEN JWT
        // Private porque só o Controller usa isso internamente.
        private string GenerateJwtToken(Usuario user)
        {
            // 1. Pega a chave secreta do appsettings.json e transforma em bytes.
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            // 2. Define o algoritmo de criptografia
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            // 3. CLAIMS (As "anotações" dentro do crachá):
            // Guardamos dentro do token o ID, Nome e Perfil do usuário.
            // Assim, nas próximas requisições, não precisamos ir no banco saber quem é o usuário, basta ler o token.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.NomeUsuario),
                new Claim(ClaimTypes.Role, user.Role)
            };
            // 4. Monta o objeto do token com validade de 8 horas.
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"], // Quem emitiu
                audience: _configuration["JwtSettings:Audience"], // Quem pode usar
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);
            // 5. Escreve o token como uma string longa codificada.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // MÉTODO UTILITÁRIO: MIGRAÇÃO DE SENHAS
        [HttpGet("migrar-senhas")] 
        public async Task<IActionResult> MigrarSenhasAntigas()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            int usuariosAtualizados = 0;

            foreach (var user in usuarios)
            {
                
                if (user.SenhaHash != null && !user.SenhaHash.StartsWith("$2"))
                {
                   
                    user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(user.SenhaHash);
                    usuariosAtualizados++;
                }
            }

            
            if (usuariosAtualizados > 0)
            {
                await _context.SaveChangesAsync();
                return Ok($"Concluído! {usuariosAtualizados} senhas de usuários foram atualizadas para o formato hash.");
            }

            return Ok("Nenhuma senha precisou ser atualizada. Elas já estavam em formato hash.");
        }
    }
}