namespace GestaoChamados.DTOs
{
    // DTO de Status em Tempo Real.
    // Serve para o gestor bater o olho e saber "onde está pegando fogo".
    public class DashboardStatsDto
    {
        // 1. Carga de Trabalho Atual:
        // Mostra quantos chamados estão sendo trabalhados neste exato momento.
        // Se este número for muito baixo e a fila estiver cheia, significa que os técnicos estão ociosos ou ausentes.
        public int EmAtendimento { get; set; }

        // 2. Alerta Crítico (Vermelho):
        // Chamados que já estouraram o prazo de 24h.
        // É o número mais importante da tela. Deve ser zero. Se for maior que zero, exige ação imediata.
        public int Atrasados { get; set; }

        // 3. Performance Geral (Média):
        // Quanto tempo (em minutos) a equipe demora, em média, para fechar um chamado.
        // Diferente do KPI de SLA (que é %), aqui é tempo absoluto.
        // Ex: "Estamos levando 45 minutos por chamado".
        public double SlaMedioMinutos { get; set; }

        // 4. Gargalos (Fila Parada):
        // Diferente de "Atrasados", aqui são chamados que estão "Abertos" (ninguém pegou ainda) há muito tempo (ex: > 2h).
        // Indica que falta triagem ou falta gente para pegar novos serviços.
        public int Gargalos { get; set; }
    }
}