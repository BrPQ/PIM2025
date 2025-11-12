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
using System.IO;
using ClosedXML.Excel;

namespace GestaoChamados.Controllers
{
    [Authorize] // Regra geral (qualquer um logado)
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IHubContext<TicketHub> _hubContext;

        public TicketsController(ApiDbContext context, IHubContext<TicketHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // --- MÉTODO GET PRINCIPAL ---
        // Este método já tem a lógica correta para "Tecnico", então está OK.
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            // ... (Nenhuma mudança aqui)
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

        // GET: api/tickets/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            // 1. Busca o ticket
            var ticket = await _context.Tickets
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound("Ticket não encontrado.");
            }

            // 2. Busca os dados ATUAIS do usuário
            var usuario = await _context.Usuarios
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Id == ticket.UsuarioId);

            // 3. Monta o DTO com os dados corretos
            var respostaDto = new TicketDetalheDto
            {
                // Dados do Ticket
                Id = ticket.Id,
                Titulo = ticket.Titulo,
                Descricao = ticket.Descricao,
                DataAbertura = ticket.DataAbertura,
                Status = ticket.Status,
                ProfissionalDesignado = ticket.ProfissionalDesignado,
                Solucao = ticket.Solucao,
                DataFinalizacao = ticket.DataFinalizacao,

                // Dados ATUAIS do Usuário
                UsuarioId = ticket.UsuarioId,
                NomeUsuario = usuario?.NomeUsuario ?? "Usuário não encontrado",
                PerfilUsuario = usuario?.Role ?? "Perfil não encontrado"
            };

            // 4. Retorna o DTO completo
            return Ok(respostaDto); // <-- MUITO IMPORTANTE: Retornar o 'respostaDto'
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequestDto request)
        {
            // ... (Nenhuma mudança aqui)
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

            await _hubContext.Clients.All.SendAsync("ReceberNovoTicket", novoTicket);

            return StatusCode(201, novoTicket);
        }

        // PUT: api/tickets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketRequestDto request)
        {
            // ... (Nenhuma mudança aqui)
            var ticketExistente = await _context.Tickets.FindAsync(id);
            if (ticketExistente == null)
            {
                return NotFound("Ticket não encontrado.");
            }

            if (ticketExistente.Status != "Finalizado" && request.Status == "Finalizado")
            {
                ticketExistente.DataFinalizacao = DateTime.UtcNow;
            }

            ticketExistente.Status = request.Status;
            ticketExistente.ProfissionalDesignado = request.ProfissionalDesignado;
            ticketExistente.Solucao = request.Solucao;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceberAtualizacaoTicket", ticketExistente);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            // ... (Nenhuma mudança aqui)
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceberTicketDeletado", id);

            return NoContent();
        }

        // GET api/tickets/por-usuario/5
        [HttpGet("por-usuario/{usuarioId}")]
        public async Task<IActionResult> GetTicketsPorUsuario(int usuarioId)
        {
            // ... (Nenhuma mudança aqui)
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

        // GET api/tickets/usuario/5/conversas
        [HttpGet("usuario/{usuarioId}/conversas")]
        public async Task<IActionResult> GetConversasDoUsuario(int usuarioId)
        {
            // ... (Nenhuma mudança aqui)
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

        // --- NOVO MÉTODO 1: BUSCAR DADOS PARA O RELATÓRIO (HTML) ---
        // --- MUDANÇA AQUI ---
        [HttpGet("relatorio")]
        [Authorize(Roles = "Gestor, Admin, Tecnico")] // Adicionamos "Tecnico"
        public async Task<IActionResult> GetTicketsRelatorio()
        {
            var tickets = await _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" ||
                            t.Status.Trim().ToUpper() == "CANCELADO")
                .Select(t => new {
                    t.Id,
                    t.Titulo,
                    t.Status,
                    t.DataAbertura,
                    t.ProfissionalDesignado,
                    t.DataFinalizacao
                })
                .OrderByDescending(t => t.DataAbertura)
                .ToListAsync();

            return Ok(tickets);
        }

        // --- NOVO MÉTODO 2: EXPORTAR PARA EXCEL ---
        // --- MUDANÇA AQUI ---
        [HttpGet("exportar-excel")]
        [Authorize(Roles = "Gestor, Admin, Tecnico")] // Adicionamos "Tecnico"
        public async Task<IActionResult> ExportarExcel()
        {
            // ... (Nenhuma mudança na lógica interna do método)
            var tickets = await _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" ||
                            t.Status.Trim().ToUpper() == "CANCELADO")
                .OrderByDescending(t => t.DataAbertura)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Relatorio Tickets");

                worksheet.Cell(1, 1).Value = "Ticket #";
                worksheet.Cell(1, 2).Value = "Título";
                worksheet.Cell(1, 3).Value = "Status";
                worksheet.Cell(1, 4).Value = "Data Abertura";
                worksheet.Cell(1, 5).Value = "Profissional";
                worksheet.Cell(1, 6).Value = "Data Finalização";
                worksheet.Cell(1, 7).Value = "Solução";

                int linhaAtual = 2;
                foreach (var ticket in tickets)
                {
                    worksheet.Cell(linhaAtual, 1).Value = ticket.Id;
                    worksheet.Cell(linhaAtual, 2).Value = ticket.Titulo;
                    worksheet.Cell(linhaAtual, 3).Value = ticket.Status;
                    worksheet.Cell(linhaAtual, 4).Value = ticket.DataAbertura;
                    worksheet.Cell(linhaAtual, 5).Value = ticket.ProfissionalDesignado;
                    worksheet.Cell(linhaAtual, 6).Value = ticket.DataFinalizacao;
                    worksheet.Cell(linhaAtual, 7).Value = ticket.Solucao;
                    linhaAtual++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Relatorio_Tickets.xlsx");
                }
            }
        }
    }
}