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
    // [Authorize]: Aqui temos uma REGRA DE NEGÓCIO importante.
    // O Dashboard mostra dados sensíveis da empresa (produtividade, gargalos).
    // Bloqueamos usuários comuns ("Funcionario"), liberando apenas para quem tem poder de decisão.
    [Authorize(Roles = "Admin, Gestor, Tecnico")]
    public class DashboardController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public DashboardController(ApiDbContext context)
        {
            _context = context;
        }

        // ROTA 1: ESTATÍSTICAS GERAIS (Números Absolutos)
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // UtcNow: Sempre usamos horário universal no servidor para evitar confusão de fuso horário.
            var agora = DateTime.UtcNow;

            // 1. Em Atendimento: Quantos técnicos estão trabalhando AGORA?
            // .Trim().ToUpper(): Garante que "Aceito", "ACEITO" ou " aceito " sejam lidos iguais. Proteção contra dados sujos.
            int emAtendimento = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() == "ACEITO");

            // 2. Atrasados: Tickets que NÃO finalizaram e já passaram de 24 horas (SLA hipotético).
            // Regra: Não está Finalizado, nem Cancelado, E a data de abertura foi há mais de 24h.
            int atrasados = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() != "FINALIZADO" &&
                                 t.Status.Trim().ToUpper() != "CANCELADO" &&
                                 t.DataAbertura < agora.AddHours(-24));

            // 3. Gargalos (Fila de Espera): Tickets que estão "ABERTO" (ninguém pegou) há mais de 2 horas.
            // Isso indica para o gestor que falta gente para atender.
            int gargalos = await _context.Tickets
                .CountAsync(t => t.Status.Trim().ToUpper() == "ABERTO" &&
                                 t.DataAbertura < agora.AddHours(-2));

            // 4. Cálculo de SLA Médio (Tempo médio de resolução).
            // Primeiro, pegamos apenas os tickets que JÁ ACABARAM.
            var ticketsFechados = _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" &&
                            t.DataFinalizacao != null);

            double slaMedioMinutos = 0;
            if (await ticketsFechados.AnyAsync())
            {
                // EF.Functions.DateDiffMinute: FUNÇÃO CRUCIAL!
                // O C# não sabe traduzir (DataA - DataB) direto para SQL de forma otimizada.
                // Usamos essa função para que o cálculo seja feito lá no SQL Server, retornando apenas o número final.
                slaMedioMinutos = await ticketsFechados
                    .Select(t => EF.Functions.DateDiffMinute(t.DataAbertura, t.DataFinalizacao.Value))
                    .AverageAsync(minutos => (double?)minutos) ?? 0;
            }

            // Monta o objeto DTO (Data Transfer Object) para enviar ao Front.
            var stats = new DashboardStatsDto
            {
                EmAtendimento = emAtendimento,
                Atrasados = atrasados,
                SlaMedioMinutos = Math.Round(slaMedioMinutos, 0), // Arredonda para não mostrar casas decimais feias.
                Gargalos = gargalos
            };

            return Ok(stats);
        }

        // ROTA 2: KPIs (Indicadores de Desempenho)
        [HttpGet("kpis")]
        public async Task<IActionResult> GetDashboardKpis()
        {
            var agora = DateTime.UtcNow;
            var hojeInicio = agora.Date; // Meia-noite de hoje (00:00:00)
            var ontemInicio = hojeInicio.AddDays(-1); // Meia-noite de ontem

            // 1. Comparativo de Volume (Hoje vs Ontem)
            // Isso gera aqueles gráficos que mostram "setinha verde" (subiu) ou "vermelha" (caiu).
            int chamadosHoje = await _context.Tickets
                .CountAsync(t => t.DataAbertura >= hojeInicio);

            int chamadosOntem = await _context.Tickets
                .CountAsync(t => t.DataAbertura >= ontemInicio && t.DataAbertura < hojeInicio);

            // 2. Percentual de cumprimento de SLA (% de tickets fechados em menos de 24h)
            var ticketsFinalizados = _context.Tickets
                .Where(t => t.Status.Trim().ToUpper() == "FINALIZADO" && t.DataFinalizacao != null);

            double slaPercentual = 0;
            if (await ticketsFinalizados.AnyAsync())
            {
                int totalFinalizados = await ticketsFinalizados.CountAsync();

                // Conta quantos respeitaram a regra de 24h.
                int finalizadosNoPrazo = await ticketsFinalizados
                    .CountAsync(t => t.DataFinalizacao.Value <= t.DataAbertura.AddHours(24));

                // Regra de três simples para achar a porcentagem.
                slaPercentual = (totalFinalizados == 0) ? 100 : ((double)finalizadosNoPrazo / totalFinalizados) * 100;
            }

            // 3. Tempo Médio HOJE
            // Diferente da média geral, aqui o gestor quer saber: "Como está a performance HOJE?"
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