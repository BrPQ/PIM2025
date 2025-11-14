using System;

namespace DesktopWPF.Models 
{
    public class Mensagem
    {
        public int MensagemId { get; set; }
        public int TicketId { get; set; }
        public int UsuarioId { get; set; }
        public string NomeUsuario { get; set; }
        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; }
        public bool IsSentByMe { get; set; }
        public string Author { get; set; }
        public string AuthorRole { get; set; }
    }
}