using GestaoChamados.Data;
using GestaoChamados.DTOs;
using GestaoChamados.Hubs;          
using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GestaoChamados.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ApiDbContext _context;
        // permite que este Controller envie mensagens para o Hub do SignalR
        private readonly IHubContext<ChatHub> _chatHubContext;

        // Injeção de dependência: Recebe o banco e o contexto do ChatHub.
        public ChatController(ApiDbContext context, IHubContext<ChatHub> chatHubContext)
        {
            _context = context;
            _chatHubContext = chatHubContext; 
        }

        // LISTAR CONTATOS
         [HttpGet("contatos")]
        public async Task<IActionResult> GetChatContacts()
        {
            // Segurança: Extrai o ID do usuário de dentro do Token JWT.
            // Isso impede que um usuário tente ver os chats de outra pessoa.
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return BadRequest("Não foi possível identificar o ID do usuário a partir do token.");
            }
            // Tickets "Fechados" ou "Pendentes" não aparecem no chat.
            var ticketsDeContato = await _context.Tickets
                    .Where(t => t.Status == "Aceito" && t.UsuarioId == userId)
                    .OrderBy(t => t.DataAbertura)
                    .ToListAsync();

            return Ok(ticketsDeContato);
        }

        // CARREGAR HISTÓRICO DE MENSAGENS
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetMensagensPorTicket(int ticketId)
        {
            // Busca as mensagens no banco de dados.
            var mensagens = await _context.Mensagens
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.DataEnvio)
                .Include(m => m.Usuario) // JOIN: Traz os dados da tabela Usuário junto com a mensagem.
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
        // ENVIAR MENSAGEM
        [HttpPost]
        public async Task<IActionResult> PostMensagem([FromBody] CreateMensagemRequestDto request)
        {
            // Primeiro, salvamos a mensagem no SQL Server. Se o servidor reiniciar, a conversa não se perde.
            var novaMensagem = new Mensagem
            {
                TicketId = request.TicketId,
                UsuarioId = request.UsuarioId,
                Conteudo = request.Conteudo,
                DataEnvio = DateTime.Now
            };
            _context.Mensagens.Add(novaMensagem);
            await _context.SaveChangesAsync();

            // Busca dados extras do usuário (Nome e Role) para enviar junto na notificação.
            var usuario = await _context.Usuarios.FindAsync(novaMensagem.UsuarioId);
            if (usuario == null)
            {
                // Se der erro aqui, retorna 201 (Criado) mas sem notificar o realtime
                return StatusCode(201, novaMensagem);
            }

            // Monta o objeto bonitinho que vai aparecer na tela de quem está com o chat aberto.
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

            // Define o nome do grupo.
            // Isso garante que a mensagem vá APENAS para quem está vendo o Ticket 105, e não para todos do sistema.
            var groupName = $"ticket-{novaMensagem.TicketId}";

            // SendAsync: Envia o evento "ReceberNovaMensagem" para todos os Front-ends conectados nesse grupo.
            // O JavaScript no navegador vai "escutar" esse evento e desenhar a mensagem na hora, sem recarregar a página.
            await _chatHubContext.Clients.Group(groupName).SendAsync("ReceberNovaMensagem", mensagemPayload);

            

            return StatusCode(201, novaMensagem);
        }
    }
}