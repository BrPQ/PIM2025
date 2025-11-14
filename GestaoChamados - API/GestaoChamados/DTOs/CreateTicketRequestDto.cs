namespace GestaoChamados.DTOs
{
    public class CreateTicketRequestDto
    {
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public int UsuarioId { get; set; }
    }
}