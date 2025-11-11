using System;

// Coloque o namespace correto do seu projeto WPF
namespace SeuProjetoWPF.Models
{
    // Esta classe deve espelhar o objeto JSON que o ChatController envia
    public class MensagemPayload
    {
        // Use os mesmos nomes que a API envia (C# diferencia maiúsculas/minúsculas)
        public int MensagemId { get; set; }
        public int TicketId { get; set; }
        public int UsuarioId { get; set; }
        public string NomeUsuario { get; set; }
        public string AuthorRole { get; set; }
        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}