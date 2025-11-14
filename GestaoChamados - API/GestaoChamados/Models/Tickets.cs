using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    [Table("Tickets")] 
    public class Ticket
    {
        [Key] // Diz que esta é a Chave Primária
        [Column("ChamadoId")]
        public int Id { get; set; }

        [Column("Titulo")]
        public string Titulo { get; set; }

        [Column("Descricao")]
        public string Descricao { get; set; }

        [Column("DataAbertura")]
        public DateTime DataAbertura { get; set; }

        [Column("Status")]
        public string Status { get; set; }

        [Column("UsuarioId")]
        public int UsuarioId { get; set; }

        [Column("ProfissionalDesignado")]
        public string? ProfissionalDesignado { get; set; }

        [Column("Solucao")]
        public string? Solucao { get; set; }

        [Column("DataFinalizacao")]
        public DateTime? DataFinalizacao { get; set; }
    }
}