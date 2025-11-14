namespace GestaoChamados.DTOs
{
    public class DashboardKpiDto
    {
        public int ChamadosHoje { get; set; }
        public int ChamadosOntem { get; set; }
        public double SlaCumpridoPercentual { get; set; }
        public double TempoMedioMinutosHoje { get; set; }
    }
}