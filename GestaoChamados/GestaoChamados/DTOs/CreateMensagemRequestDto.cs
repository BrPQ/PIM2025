namespace GestaoChamados.DTOs
{
    public class CreateMensagemRequestDto
    {
        public int TicketId { get; set; }
        public int UsuarioId { get; set; } // O ID do usuário que está enviando
        public string Conteudo { get; set; }
    }
}