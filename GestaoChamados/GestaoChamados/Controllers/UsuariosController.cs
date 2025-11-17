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

        
        [HttpGet]
        [Authorize(Roles = "Admin, Gestor, Tecnico")]
        public async Task<IActionResult> GetAllUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new
                {
                    
                    Id = u.Id,
                    NomeUsuario = u.NomeUsuario,
                    Login = u.Matricula,
                    Role = u.Role // Role (que é o Perfil/Setor)
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> CreateUsuario([FromBody] CreateUsuarioRequestDto request)
        {
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Matricula == request.Matricula);
            if (usuarioExistente != null)
            {
                return Conflict("Já existe um usuário cadastrado com esta matrícula.");
            }

            var novoUsuario = new Usuario
            {
                NomeUsuario = request.NomeUsuario,
                Matricula = request.Matricula,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                Role = request.Role,
                Email = request.Email,
                Ativo = true,
                DataCadastro = DateTime.UtcNow
            };

            _context.Usuarios.Add(novoUsuario);
            await _context.SaveChangesAsync();
            return StatusCode(201, novoUsuario);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Tecnico")] 
        public async Task<IActionResult> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

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