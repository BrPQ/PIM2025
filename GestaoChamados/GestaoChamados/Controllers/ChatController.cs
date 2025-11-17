using GestaoChamados.Data;
using GestaoChamados.DTOs;
using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR; 
using GestaoChamados.Hubs;          

namespace GestaoChamados.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ApiDbContext _context;
        
        private readonly IHubContext<ChatHub> _chatHubContext;

        
        public ChatController(ApiDbContext context, IHubContext<ChatHub> chatHubContext)
        {
            _context = context;
            _chatHubContext = chatHubContext; 
        }

       
        [HttpGet("contatos")]
        public async Task<IActionResult> GetChatContacts()
        {
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return BadRequest("Não foi possível identificar o ID do usuário a partir do token.");
            }

            var ticketsDeContato = await _context.Tickets
                    .Where(t => t.Status == "Aceito" && t.UsuarioId == userId)
                    .OrderBy(t => t.DataAbertura)
                    .ToListAsync();

            return Ok(ticketsDeContato);
        }

        
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetMensagensPorTicket(int ticketId)
        {
            
            var mensagens = await _context.Mensagens
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.DataEnvio)
                .Include(m => m.Usuario)
                .Select(m => new
                {
                    m.MensagemId,
                    m.TicketId,
                    m.UsuarioId,
                    NomeUsuario = m.Usuario.NomeUsuario,
                    AuthorRole = m.Usuario.Role,
                    m.Conteudo,
                    m.DataEnvio
                })
                .ToListAsync();

            return Ok(mensagens);
        }

        [HttpPost]
        public async Task<IActionResult> PostMensagem([FromBody] CreateMensagemRequestDto request)
        {
            var novaMensagem = new Mensagem
            {
                TicketId = request.TicketId,
                UsuarioId = request.UsuarioId,
                Conteudo = request.Conteudo,
                DataEnvio = DateTime.Now
            };
            _context.Mensagens.Add(novaMensagem);
            await _context.SaveChangesAsync();

            

            
            var usuario = await _context.Usuarios.FindAsync(novaMensagem.UsuarioId);
            if (usuario == null)
            {
                
                return StatusCode(201, novaMensagem);
            }

            
            var mensagemPayload = new
            {
                novaMensagem.MensagemId,
                novaMensagem.TicketId,
                novaMensagem.UsuarioId,
                NomeUsuario = usuario.NomeUsuario,
                AuthorRole = usuario.Role,
                novaMensagem.Conteudo,
                novaMensagem.DataEnvio
            };

            
            var groupName = $"ticket-{novaMensagem.TicketId}";

            
            await _chatHubContext.Clients.Group(groupName).SendAsync("ReceberNovaMensagem", mensagemPayload);

            

            return StatusCode(201, novaMensagem);
        }
    }
}