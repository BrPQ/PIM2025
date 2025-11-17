using System;


namespace SeuProjetoWPF.Models
{
    
    public class MensagemPayload
    {
        public int MensagemId { get; set; }
        public int TicketId { get; set; }
        public int UsuarioId { get; set; }
        public string NomeUsuario { get; set; }
        public string AuthorRole { get; set; }
        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}