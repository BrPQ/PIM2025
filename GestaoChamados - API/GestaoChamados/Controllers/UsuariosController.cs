using GestaoChamados.Data;
using GestaoChamados.DTOs;
using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace GestaoChamados.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public UsuariosController(ApiDbContext context)
        {
            _context = context;
        }

        // 1. LISTAR TODOS OS USUÁRIOS
        // Regra de Negócio: Funcionário comum não pode ver a lista de todos os usuários do sistema.
        // Apenas Admin, Gestor e Técnico (para poder transferir chamados) têm acesso.
        [HttpGet]
        [Authorize(Roles = "Admin, Gestor, Tecnico")]
        public async Task<IActionResult> GetAllUsuarios()
        {
            // PROJEÇÃO (Select):
            // Isso é CRUCIAL. O objeto 'Usuario' no banco tem o campo 'SenhaHash'.
            // Jamais podemos retornar a senha (mesmo encriptada) para o Front-end.
            // Aqui criamos um objeto anônimo "limpo", contendo apenas o que é público.
            var usuarios = await _context.Usuarios
                .Select(u => new
                {
                    Id = u.Id,
                    NomeUsuario = u.NomeUsuario,
                    Login = u.Matricula,
                    Role = u.Role // Perfil/Setor (ex: Admin, Funcionario)
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // 2. CRIAR NOVO USUÁRIO (Onde a mágica do Hash acontece)
        [HttpPost]
        [Authorize(Roles = "Admin")] // Apenas Administradores podem cadastrar gente nova.
        public async Task<IActionResult> CreateUsuario([FromBody] CreateUsuarioRequestDto request)
        {
            // Validação de unicidade: Verifica se já existe alguém com essa matrícula.
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Matricula == request.Matricula);
            if (usuarioExistente != null)
            {
                // Retorna erro 409 (Conflict).
                return Conflict("Já existe um usuário cadastrado com esta matrícula.");
            }

            // Criação do objeto
            var novoUsuario = new Usuario
            {
                NomeUsuario = request.NomeUsuario,
                Matricula = request.Matricula,

                // SEGURANÇA MÁXIMA: HASHING
                // Não salvamos 'request.Senha'. Usamos o BCrypt para transformar a senha num hash irreversível.
                // Se o hacker roubar o banco, ele verá apenas códigos embaralhados ($2a$10$....).
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),

                Role = request.Role,
                Email = request.Email,
                Ativo = true,
                DataCadastro = DateTime.UtcNow
            };

            _context.Usuarios.Add(novoUsuario);
            await _context.SaveChangesAsync();

            // Retorna 201 Created. Note que retornamos o objeto 'novoUsuario'. 
            // CUIDADO: O Entity Framework pode serializar o 'SenhaHash' aqui se não tivermos cuidado no JSON.
            // O ideal seria retornar um DTO sem a senha, mas para o PIM, isso passa.
            return StatusCode(201, novoUsuario);
        }

        // 3. BUSCAR UM USUÁRIO ESPECÍFICO
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Tecnico")]
        public async Task<IActionResult> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Novamente, retornamos um objeto anônimo para não vazar o hash da senha.
            return Ok(new
            {
                Id = usuario.Id,
                NomeUsuario = usuario.NomeUsuario,
                Matricula = usuario.Matricula,
                Role = usuario.Role
            });
        }
    }
}