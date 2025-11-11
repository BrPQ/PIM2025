using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    [Table("Anexos")]
    public class Anexo
    {
        [Key]
        [Column("AnexoId")]
        public int AnexoId { get; set; }

        [Column("TicketId")]
        public int TicketId { get; set; }

        [Column("NomeArquivo")]
        public string NomeArquivo { get; set; }

        [Column("CaminhoArquivo")]
        public string CaminhoArquivo { get; set; }

        [Column("DataUpload")]
        public DateTime DataUpload { get; set; }

        [Column("TipoAnexo")]
        public string TipoAnexo { get; set; }
    }
}