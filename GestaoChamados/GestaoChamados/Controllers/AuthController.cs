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
        private readonly IConfiguration _configuration;
        private readonly ApiDbContext _context;

        public AuthController(IConfiguration configuration, ApiDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            var matriculaLimpa = loginRequest.Matricula.Trim();
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Matricula.ToUpper() == matriculaLimpa.ToUpper());

            
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Senha, user.SenhaHash))
            {
                return Unauthorized("Matrícula ou senha inválidos.");
            }
            

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                Usuario = new
                {
                    Id = user.Id,
                    NomeUsuario = user.NomeUsuario,
                    Matricula = user.Matricula,
                    Role = user.Role
                }
            });
        }

        private string GenerateJwtToken(Usuario user)
        {
            
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.NomeUsuario),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

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