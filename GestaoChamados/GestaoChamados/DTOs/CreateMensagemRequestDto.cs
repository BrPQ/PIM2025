namespace GestaoChamados.DTOs
{
    public class CreateMensagemRequestDto
    {
        public int TicketId { get; set; }
        public int UsuarioId { get; set; } 
        public string Conteudo { get; set; }
    }
}