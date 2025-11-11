namespace GestaoChamados.DTOs
{
    public class UpdateTicketRequestDto
    {
        public string Status { get; set; }
        public string? ProfissionalDesignado { get; set; } // O '?' permite que seja nulo
        public string? Solucao { get; set; }
    }
}