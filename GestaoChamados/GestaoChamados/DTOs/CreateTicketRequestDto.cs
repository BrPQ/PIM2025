namespace GestaoChamados.DTOs
{
    public class CreateTicketRequestDto
    {
        public string Titulo { get; set; }
        public string Descricao { get; set; }

        // O ID do usuário que está criando o ticket
        // O app cliente (mobile/desktop) será responsável por enviar esta informação
        public int UsuarioId { get; set; }
    }
}