using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    // Mapeamento: Define que esta classe vira a tabela "Mensagens" no SQL Server.
    [Table("Mensagens")]
    public class Mensagem
    {
        // Chave Primária (PK): Identificador único de cada balão de fala do chat.
        [Key]
        public int MensagemId { get; set; }

        // --- CHAVES ESTRANGEIRAS (FK) ---
        // Aqui está a integridade referencial.
        // [Required]: Garante que NÃO existe mensagem "solta" no sistema. 
        // Toda mensagem OBRIGATORIAMENTE pertence a um Ticket.
        [Required]
        public int TicketId { get; set; }

        // [Required]: Garante que toda mensagem tem um autor.
        // Se um usuário for deletado, o banco pode impedir ou apagar as mensagens dele (depende da regra de Cascade Delete),
        // mas nunca deixará uma mensagem com UsuarioId nulo.
        [Required]
        public int UsuarioId { get; set; }

        // O texto da conversa.
        // [Required]: Impede enviar mensagem vazia/branca.
        [Required]
        public string Conteudo { get; set; }

        // Auditoria e Ordenação:
        // Essencial para o chat mostrar as mensagens na ordem certa (cronológica).
        public DateTime DataEnvio { get; set; }

        // --- PROPRIEDADES DE NAVEGAÇÃO ---
        // É aqui que a mágica do Entity Framework acontece.
        // Enquanto as propriedades 'int' acima guardam só o NÚMERO (ex: 10),
        // estas propriedades 'virtual' guardam o OBJETO INTEIRO.

        // Isso permite fazer coisas como: mensagem.Usuario.NomeUsuario
        // Sem precisar escrever um "SELECT JOIN" manual no SQL.

        [ForeignKey("TicketId")] // Avisa o EF que a chave para montar esse objeto é o 'TicketId' lá de cima.
        public virtual Ticket Ticket { get; set; }

        [ForeignKey("UsuarioId")] // Avisa o EF que a chave é o 'UsuarioId'.
        public virtual Usuario Usuario { get; set; }
    }
}