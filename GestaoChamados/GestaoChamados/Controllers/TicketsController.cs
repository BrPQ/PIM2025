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
using Microsoft.AspNetCore.SignalR; // <-- 1. Adicione este using
using GestaoChamados.Hubs;          // <-- 2. Adicione este using

namespace GestaoChamados.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IHubContext<TicketHub> _hubContext; // <-- 3. Adicione o campo para o Hub

        // 4. Modifique o construtor para receber o HubContext
        public TicketsController(ApiDbContext context, IHubContext<TicketHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext; // <-- 5. Atribua o HubContext
        }

        // --- MÉTODO GET PRINCIPAL (sem alterações) ---
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var nomeUsuarioLogado = User.FindFirst(ClaimTypes.Name)?.Value;
            var perfilUsuarioLogado = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(nomeUsuarioLogado) || string.IsNullOrEmpty(perfilUsuarioLogado))
            {
                return Unauthorized("Não foi possível identificar o usuário ou seu perfil a partir do token.");
            }
            var nomeUsuarioLogadoNormalizado = nomeUsuarioLogado.Trim().ToUpper();

            IQueryable<Ticket> query = _context.Tickets;

            if (perfilUsuarioLogado.Equals("Tecnico", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t =>
                    t.Status == "Aberto" ||
                    (t.ProfissionalDesignado != null && t.ProfissionalDesignado.Trim().ToUpper() == nomeUsuarioLogadoNormalizado)
                );
            }
            else if (perfilUsuarioLogado.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin vê tudo
            }
            else
            {
                return Forbid("Seu perfil não tem permissão para acessar esta lista de tickets.");
            }

            var tickets = await query.OrderByDescending(t => t.DataAbertura).ToListAsync();
            return Ok(tickets);
        }

        // GET: api/tickets/5 (sem alterações)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            return Ok(ticket);
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequestDto request)
        {
            var novoTicket = new Ticket
            {
                Titulo = request.Titulo,
                Descricao = request.Descricao,
                UsuarioId = request.UsuarioId,
                Status = "Aberto",
                DataAbertura = DateTime.Now
            };

            _context.Tickets.Add(novoTicket);
            await _context.SaveChangesAsync();

            // *** AVISO EM TEMPO REAL! ***
            // Envia uma mensagem para todos os clientes informando sobre o novo ticket.
            await _hubContext.Clients.All.SendAsync("ReceberNovoTicket", novoTicket);

            return StatusCode(201, novoTicket);
        }

        // PUT: api/tickets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketRequestDto request)
        {
            var ticketExistente = await _context.Tickets.FindAsync(id);
            if (ticketExistente == null)
            {
                return NotFound("Ticket não encontrado.");
            }

            // --- LÓGICA DO SLA ---
            // Se o status *anterior* não era "Fechado" e o *novo* status é "Fechado",
            // salve a data de finalização.
            if (ticketExistente.Status != "Finalizado" && request.Status == "Finalizado")
            {
                ticketExistente.DataFinalizacao = DateTime.UtcNow;
            }
            // --- FIM DA LÓGICA ---

            ticketExistente.Status = request.Status;
            ticketExistente.ProfissionalDesignado = request.ProfissionalDesignado;
            ticketExistente.Solucao = request.Solucao;

            await _context.SaveChangesAsync();

            // *** AVISO EM TEMPO REAL! ***
            await _hubContext.Clients.All.SendAsync("ReceberAtualizacaoTicket", ticketExistente);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            // *** AVISO EM TEMPO REAL! ***
            // Envia uma mensagem para todos os clientes informando sobre a exclusão.
            await _hubContext.Clients.All.SendAsync("ReceberTicketDeletado", id);

            return NoContent();
        }

        // GET api/tickets/por-usuario/5 (sem alterações)
        [HttpGet("por-usuario/{usuarioId}")]
        public async Task<IActionResult> GetTicketsPorUsuario(int usuarioId)
        {
            var loggedInUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var loggedInUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(loggedInUserIdStr) || !int.TryParse(loggedInUserIdStr, out int loggedInUserId))
            {
                return Unauthorized("Token inválido ou não contém ID do usuário.");
            }

            if (loggedInUserRole != "Tecnico" && loggedInUserId != usuarioId)
            {
                return Forbid("Acesso negado. Você só pode visualizar os seus próprios tickets.");
            }

            var tickets = await _context.Tickets
                .Where(t => t.UsuarioId == usuarioId)
                .OrderByDescending(t => t.DataAbertura)
                .ToListAsync();

            return Ok(tickets);
        }

        // GET api/tickets/usuario/5/conversas (sem alterações)
        [HttpGet("usuario/{usuarioId}/conversas")]
        public async Task<IActionResult> GetConversasDoUsuario(int usuarioId)
        {
            var loggedInUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(loggedInUserIdStr, out int loggedInUserId) || loggedInUserId != usuarioId)
            {
                return Forbid("Acesso negado. Você só pode visualizar suas próprias conversas.");
            }

            var conversas = await _context.Tickets
                .Where(t => t.UsuarioId == usuarioId && t.Status == "Aceito")
                .Select(t => new ConversaResumoDto
                {
                    TicketId = t.Id,
                    TituloTicket = t.Titulo,
                    Status = t.Status,
                    NomeProfissional = t.ProfissionalDesignado,
                    UltimaMensagem = _context.Mensagens
                                    .Where(m => m.TicketId == t.Id)
                                    .OrderByDescending(m => m.DataEnvio)
                                    .Select(m => m.Conteudo)
                                    .FirstOrDefault() ?? "Nenhuma mensagem ainda.",
                    DataUltimaMensagem = _context.Mensagens
                                    .Where(m => m.TicketId == t.Id)
                                    .OrderByDescending(m => m.DataEnvio)
                                    .Select(m => (DateTime?)m.DataEnvio)
                                    .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(conversas);
        }
    }
}