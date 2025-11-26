using System;

namespace DesktopWPF.Models
{
    // MODELO ESPELHO (Mirror Model):
    // Esta classe é uma cópia exata da estrutura que a API devolve.
    // O "JsonConvert" lá do ApiService vai pegar o texto da internet e preencher essas variáveis.
    // Note que aqui NÃO temos [Table] ou [Key], porque o Desktop NÃO acessa o SQL Server diretamente.
    public class Anexo
    {
        // ID único para sabermos qual arquivo baixar quando o usuário clicar.
        public int AnexoId { get; set; }

        // A qual chamado este anexo pertence.
        public int TicketId { get; set; }

        // O nome que vai aparecer na tela (ex: "PrintDoErro.png").
        // É isso que mostramos no ListView do WPF.
        public string NomeArquivo { get; set; }

        // Caminho relativo no servidor.
        // O Desktop usa isso para saber a extensão (ex: .pdf, .jpg) e desenhar o ícone correto na tela.
        public string CaminhoArquivo { get; set; }

        // Data para ordenação (mostrar os mais recentes primeiro).
        public DateTime DataUpload { get; set; }

        // Tipo para categorização visual (ícone de "Usuário" vs ícone de "Sistema").
        public string TipoAnexo { get; set; }
    }
}