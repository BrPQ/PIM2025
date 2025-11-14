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

            
            int emAtendimento = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() == "ACEITO");

            
            int atrasados = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() != "FINALIZADO" &&
                                 t.Status.Trim().ToUpper() != "CANCELADO" &&
                                 t.DataAbertura < agora.AddHours(-24));

            
            int gargalos = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() == "ABERTO" &&
                                 t.DataAbertura < agora.AddHours(-2));

            
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
            var hojeInicio = agora.Date; 
            var ontemInicio = hojeInicio.AddDays(-1); 

            
            int chamadosHoje = await _context.Tickets
                .CountAsync(t => t.DataAbertura >= hojeInicio);

            int chamadosOntem = await _context.Tickets
                .CountAsync(t => t.DataAbertura >= ontemInicio && t.DataAbertura < hojeInicio);

            
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