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
using ClosedXML.Excel; // Biblioteca externa usada para gerar arquivos .xlsx

namespace GestaoChamados.Controllers
{
    [Authorize] // Bloqueia acesso anônimo. Tem que ter Token.
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IHubContext<TicketHub> _hubContext;

        // Injeção de dependência dupla: Banco de Dados + Sistema de WebSocket (SignalR)
        public TicketsController(ApiDbContext context, IHubContext<TicketHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 1. LISTAGEM INTELIGENTE (FILTRO POR PERFIL)
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            // Quem está pedindo a lista? Extraímos isso do Token JWT.
            var nomeUsuarioLogado = User.FindFirst(ClaimTypes.Name)?.Value;
            var perfilUsuarioLogado = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(nomeUsuarioLogado) || string.IsNullOrEmpty(perfilUsuarioLogado))
            {
                return Unauthorized("Não foi possível identificar o usuário ou seu perfil a partir do token.");
            }
            var nomeUsuarioLogadoNormalizado = nomeUsuarioLogado.Trim().ToUpper();

            // Prepara a consulta base (SELECT * FROM Tickets...)
            IQueryable<Ticket> query = _context.Tickets;

            // REGRA DE NEGÓCIO IMPORTANTE:
            // Se for Técnico: Só vê chamados "Abertos" (disponíveis para pegar) OU os que já são dele.
            // O técnico não pode ver os chamados privados de outro técnico.
            if (perfilUsuarioLogado.Equals("Tecnico", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t =>
                    t.Status == "Aberto" ||
                    (t.ProfissionalDesignado != null && t.ProfissionalDesignado.Trim().ToUpper() == nomeUsuarioLogadoNormalizado)
                );
            }
            // Se for Admin: A query não muda, ele vê tudo (poder total).
            else if (perfilUsuarioLogado.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin vê tudo.
            }
            else
            {
                // Se for um perfil estranho ou Funcionario comum tentando acessar a lista geral, bloqueia.
                return Forbid("Seu perfil não tem permissão para acessar esta lista de tickets.");
            }

            // Ordena do mais novo para o mais antigo e executa a query.
            var tickets = await query.OrderByDescending(t => t.DataAbertura).ToListAsync();
            return Ok(tickets);
        }

        // 2. DETALHES DO TICKET
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            // AsNoTracking(): Otimização de performance. Como só vamos ler (e não editar agora),
            // avisamos o Entity Framework para não gastar memória rastreando esse objeto.
            var ticket = await _context.Tickets
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound("Ticket não encontrado.");

            // Busca manual dos dados do autor do chamado para exibir no front.
            var usuario = await _context.Usuarios
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Id == ticket.UsuarioId);

            // DTO (Data Transfer Object): Monta um objeto limpo para entregar ao Front-end.
            var respostaDto = new TicketDetalheDto
            {
                Id = ticket.Id,
                Titulo = ticket.Titulo,
                Descricao = ticket.Descricao,
                DataAbertura = ticket.DataAbertura,
                Status = ticket.Status,
                ProfissionalDesignado = ticket.ProfissionalDesignado,
                Solucao = ticket.Solucao,
                DataFinalizacao = ticket.DataFinalizacao,
                UsuarioId = ticket.UsuarioId,
                NomeUsuario = usuario?.NomeUsuario ?? "Usuário não encontrado", // Tratamento de nulo (Elvis operator)
                PerfilUsuario = usuario?.Role ?? "Perfil não encontrado"
            };

            return Ok(respostaDto);
        }

        // 3. CRIAR TICKET + NOTIFICAÇÃO
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

            // Passo 1: Salva no Banco SQL
            _context.Tickets.Add(novoTicket);
            await _context.SaveChangesAsync();

            // Passo 2: Avisa TODOS os conectados via WebSocket (SignalR) que chegou chamado novo.
            // Isso faz a tela do técnico atualizar sozinha ("piscar") sem ele precisar dar F5.
            await _hubContext.Clients.All.SendAsync("ReceberNovoTicket", novoTicket);

            return StatusCode(201, novoTicket);
        }

        // 4. ATUALIZAR TICKET (ATENDER/FINALIZAR)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketRequestDto request)
        {
            var ticketExistente = await _context.Tickets.FindAsync(id);
            if (ticketExistente == null) return NotFound("Ticket não encontrado.");

            // Lógica automática: Se mudou para "Finalizado", grava a data/hora exata do fim.
            if (ticketExistente.Status != "Finalizado" && request.Status == "Finalizado")
            {
                ticketExistente.DataFinalizacao = DateTime.Now;
            }

            ticketExistente.Status = request.Status;
            ticketExistente.ProfissionalDesignado = request.ProfissionalDesignado;
            ticketExistente.Solucao = request.Solucao;

            await _context.SaveChangesAsync();

            // Novamente, avisa via SignalR que o status mudou.
            await _hubContext.Clients.All.SendAsync("ReceberAtualizacaoTicket", ticketExistente);

            return NoContent();
        }

        // 5. RELATÓRIO EXCEL (CLOSEDXML)
        [HttpGet("exportar-excel")]
        [Authorize(Roles = "Gestor, Admin, Tecnico")] // Funcionário comum não exporta relatório.
        public async Task<IActionResult> ExportarExcel()
        {
            // Busca apenas tickets finalizados ou cancelados (histórico).
            var tickets = await _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" ||
                            t.Status.Trim().ToUpper() == "CANCELADO")
                .OrderByDescending(t => t.DataAbertura)
                .ToListAsync();

            // Criação do arquivo na memória RAM (não salva no disco do servidor para não acumular lixo).
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Relatorio Tickets");

                // Cabeçalho das colunas
                worksheet.Cell(1, 1).Value = "Ticket #";
                worksheet.Cell(1, 2).Value = "Título";
                worksheet.Cell(1, 3).Value = "Status";
                worksheet.Cell(1, 4).Value = "Data Abertura";
                worksheet.Cell(1, 5).Value = "Profissional";
                worksheet.Cell(1, 6).Value = "Data Finalização";
                worksheet.Cell(1, 7).Value = "Solução";

                // Preenchimento das linhas (Iteração)
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

                // Transforma o Excel em bytes e envia para download no navegador.
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // MIME Type do Excel
                        "Relatorio_Tickets.xlsx");
                }
            }
        }
    }
}