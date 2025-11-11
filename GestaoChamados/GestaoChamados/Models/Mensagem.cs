using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    [Table("Mensagens")]
    public class Mensagem
    {
        [Key]
        public int MensagemId { get; set; }

        [Required]
        public int TicketId { get; set; } // A qual ticket esta mensagem pertence

        [Required]
        public int UsuarioId { get; set; } // Quem enviou a mensagem

        [Required]
        public string Conteudo { get; set; }

        public DateTime DataEnvio { get; set; }

        // Propriedades de Navegação (opcionais, mas boas práticas)
        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}