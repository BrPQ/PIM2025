namespace GestaoChamados.DTOs
{
    // KPI = Key Performance Indicator (Indicador Chave de Desempenho).
    // Este DTO agrupa 4 métricas vitais para entregar tudo de uma vez ao Front-end.
    public class DashboardKpiDto
    {
        // 1. Volume Atual: Quantos chamados entraram desde a meia-noite?
        // Ajuda o gestor a dimensionar a equipe do dia.
        public int ChamadosHoje { get; set; }

        // 2. Base de Comparação:
        // Por que enviar o dado de ontem?
        // Para o Front-end poder desenhar aquela "setinha" de tendência.
        // Ex: Se Hoje = 15 e Ontem = 10, o sistema mostra "Setinha pra Cima (+50%)".
        // Isso transforma um dado frio em inteligência de negócio.
        public int ChamadosOntem { get; set; }

        // 3. Qualidade (SLA - Service Level Agreement):
        // É do tipo 'double' (ponto flutuante) para permitir precisão (ex: 95.5%).
        // Se fosse 'int', 99.9% viraria 99 ou 100, perdendo precisão.
        public double SlaCumpridoPercentual { get; set; }

        // 4. Eficiência:
        // Mede a velocidade da equipe HOJE.
        // Se esse número estiver muito alto, o gestor sabe que precisa intervir agora.
        public double TempoMedioMinutosHoje { get; set; }
    }
}