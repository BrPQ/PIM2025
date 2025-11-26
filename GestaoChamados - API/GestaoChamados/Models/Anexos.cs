using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    // [Table]: Força o Entity Framework a criar a tabela com o nome exato "Anexos".
    // Se não usar isso, ele pode tentar criar como "Anexo" (singular) ou "Anexos" (plural) dependendo da configuração.
    // Isso garante controle total sobre o nome no SQL Server.
    [Table("Anexos")]
    public class Anexo
    {
        // [Key]: Define que este campo é a CHAVE PRIMÁRIA (PK).
        // Garante que cada anexo tenha um número único (1, 2, 3...).
        [Key]
        // [Column]: Define o nome exato da coluna no banco. Útil se você estiver usando um banco legado.
        [Column("AnexoId")]
        public int AnexoId { get; set; }

        // Chave Estrangeira (FK):
        // Liga este anexo a um Ticket específico.
        // Sem isso, o anexo ficaria "orfão" no sistema.
        [Column("TicketId")]
        public int TicketId { get; set; }

        // Nome original do arquivo (ex: "print_erro.png").
        // Importante para quando o usuário baixar o arquivo de volta, ele ter o nome certo.
        [Column("NomeArquivo")]
        public string NomeArquivo { get; set; }

        // O CAMPO MAIS IMPORTANTE:
        // Aqui guardamos apenas o endereço (ex: "89d7as8d7-print.png").
        // O arquivo pesado (binário) fica no HD do servidor, não dentro do banco.
        [Column("CaminhoArquivo")]
        public string CaminhoArquivo { get; set; }

        // Auditoria: Saber quando foi enviado.
        [Column("DataUpload")]
        public DateTime DataUpload { get; set; }

        // Metadados:
        // Serve para filtrar ou categorizar (ex: "Comprovante", "Erro", "Log").
        [Column("TipoAnexo")]
        public string TipoAnexo { get; set; }
    }
}