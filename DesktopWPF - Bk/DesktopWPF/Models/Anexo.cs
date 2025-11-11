using System;
namespace DesktopWPF.Models
{
    public class Anexo
    {
        public int AnexoId { get; set; }
        public int TicketId { get; set; }
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public DateTime DataUpload { get; set; }
        public string TipoAnexo { get; set; } // <-- ADICIONE AQUI
    }
}