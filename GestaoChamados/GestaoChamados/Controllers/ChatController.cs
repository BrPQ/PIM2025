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
using Microsoft.AspNetCore.SignalR; // <-- 1. ADICIONE O IMPORT DO SIGNALR
using GestaoChamados.Hubs;          // <-- 2. ADICIONE O IMPORT DOS SEUS HUBS

namespace GestaoChamados.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ApiDbContext _context;
        // 3. Crie um campo privado para o contexto do Hub
        private readonly IHubContext<ChatHub> _chatHubContext;

        // 4. Modifique o construtor para "injetar" o IHubContext
        public ChatController(ApiDbContext context, IHubContext<ChatHub> chatHubContext)
        {
            _context = context;
            _chatHubContext = chatHubContext; // 5. Atribua o contexto do Hub
        }

        // ... [O método GetChatContacts não precisa de mudanças] ...
        [HttpGet("contatos")]
        public async Task<IActionResult> GetChatContacts()
        {
            // ... (código existente sem alteração) ...
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

        // ... [O método GetMensagensPorTicket não precisa de mudanças] ...
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetMensagensPorTicket(int ticketId)
        {
            // ... (código existente sem alteração) ...
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

            // --- INÍCIO DA LÓGICA DO SIGNALR ---

            // 6. Buscamos os dados do usuário. É importante enviar o nome/role
            //    junto com a mensagem, assim como seu método GET faz.
            var usuario = await _context.Usuarios.FindAsync(novaMensagem.UsuarioId);
            if (usuario == null)
            {
                // Se não achar o usuário, apenas retorna (o salvamento no DB já ocorreu)
                return StatusCode(201, novaMensagem);
            }

            // 7. Criamos um "payload" (objeto de dados) para enviar via SignalR
            //    que seja idêntico ao que o seu [HttpGet("{ticketId}")] retorna.
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

            // 8. Definimos o nome do grupo (ex: "ticket-123")
            var groupName = $"ticket-{novaMensagem.TicketId}";

            // 9. Usamos o Hub para enviar a mensagem APENAS para os clientes
            //    que estão ouvindo o grupo "ticket-123"
            await _chatHubContext.Clients.Group(groupName).SendAsync("ReceberNovaMensagem", mensagemPayload);

            // --- FIM DA LÓGICA DO SIGNALR ---

            return StatusCode(201, novaMensagem);
        }
    }
}