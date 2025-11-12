using GestaoChamados.Data;
using GestaoChamados.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GestaoChamados.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Apenas Admins e Gestores podem ver o dashboard
    [Authorize(Roles = "Admin, Gestor, Tecnico")]
    public class DashboardController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public DashboardController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var agora = DateTime.UtcNow;

            // --- CORREÇÃO 1: "À PROVA DE FALHAS" ---
            // Agora compara "ACEITO" com "ACEITO", "aceito", " Aceito ", etc.
            int emAtendimento = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() == "ACEITO");

            // --- CORREÇÃO 2: "À PROVA DE FALHAS" ---
            // Compara todos os status que NÃO são de finalização
            int atrasados = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() != "FINALIZADO" &&
                                 t.Status.Trim().ToUpper() != "CANCELADO" &&
                                 t.DataAbertura < agora.AddHours(-24));

            // --- CORREÇÃO 3: "À PROVA DE FALHAS" ---
            // (Ainda assumindo que o status "Aberto" existe)
            int gargalos = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() == "ABERTO" &&
                                 t.DataAbertura < agora.AddHours(-2));

            // --- CORREÇÃO 4: "À PROVA DE FALHAS" ---
            // Busca tickets finalizados para o SLA
            var ticketsFechados = _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" &&
                            t.DataFinalizacao != null);

            double slaMedioMinutos = 0;
            if (await ticketsFechados.AnyAsync())
            {
                slaMedioMinutos = await ticketsFechados
                    .Select(t => EF.Functions.DateDiffMinute(t.DataAbertura, t.DataFinalizacao.Value))
                    .AverageAsync(minutos => (double?)minutos) ?? 0;
            }

            var stats = new DashboardStatsDto
            {
                EmAtendimento = emAtendimento,
                Atrasados = atrasados,
                SlaMedioMinutos = Math.Round(slaMedioMinutos, 0),
                Gargalos = gargalos
            };

            return Ok(stats);
        }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetDashboardKpis()
        {
            var agora = DateTime.UtcNow;
            var hojeInicio = agora.Date; // Hoje, 00:00:00
            var ontemInicio = hojeInicio.AddDays(-1); // Ontem, 00:00:00

            // 1. Chamados hoje vs ontem
            int chamadosHoje = await _context.Tickets
                .CountAsync(t => t.DataAbertura >= hojeInicio);

            int chamadosOntem = await _context.Tickets
                .CountAsync(t => t.DataAbertura >= ontemInicio && t.DataAbertura < hojeInicio);

            // 2. SLA Cumprido (Regra: 24h)
            var ticketsFinalizados = _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" && t.DataFinalizacao != null);

            double slaPercentual = 0;
            if (await ticketsFinalizados.AnyAsync())
            {
                int totalFinalizados = await ticketsFinalizados.CountAsync();
                int finalizadosNoPrazo = await ticketsFinalizados
                    .CountAsync(t => t.DataFinalizacao.Value <= t.DataAbertura.AddHours(24));

                slaPercentual = (totalFinalizados == 0) ? 100 : ((double)finalizadosNoPrazo / totalFinalizados) * 100;
            }

            // 3. Tempo Médio (apenas dos tickets finalizados HOJE)
            var ticketsFechadosHoje = _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" &&
                            t.DataFinalizacao != null &&
                            t.DataFinalizacao.Value >= hojeInicio);

            double tempoMedioHoje = 0;
            if (await ticketsFechadosHoje.AnyAsync())
            {
                tempoMedioHoje = await ticketsFechadosHoje
                    .Select(t => EF.Functions.DateDiffMinute(t.DataAbertura, t.DataFinalizacao.Value))
                    .AverageAsync(minutos => (double?)minutos) ?? 0;
            }

            // Monta o DTO de resposta
            var dto = new DashboardKpiDto
            {
                ChamadosHoje = chamadosHoje,
                ChamadosOntem = chamadosOntem,
                SlaCumpridoPercentual = Math.Round(slaPercentual, 0),
                TempoMedioMinutosHoje = Math.Round(tempoMedioHoje, 0)
            };

            return Ok(dto);
        }
    }
}