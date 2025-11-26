using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoChamados.Models
{
    // [Table("Tickets")]:
    // Mapeamento Explícito. Garante que a tabela no SQL se chame "Tickets".
    // Isso evita confusão se o Entity Framework tentar criar como "Ticket" (singular).
    [Table("Tickets")]
    public class Ticket
    {
        // [Key]: Define a Chave Primária (PK).
        [Key]
        // [Column("ChamadoId")]: *** MUITO IMPORTANTE ***
        // Aqui você está fazendo um "De/Para".
        // No seu código C#, a propriedade se chama "Id" (padrão universal).
        // Mas no Banco de Dados, a coluna se chama "ChamadoId".
        // O Entity Framework traduz isso automaticamente em toda query.
        // Isso mostra que você sabe trabalhar com bancos legados ou com convenções de nomes específicas.
        [Column("ChamadoId")]
        public int Id { get; set; }

        [Column("Titulo")]
        public string Titulo { get; set; }

        // Descrição do problema. No SQL Server, isso vai virar um nvarchar(MAX)
        // para caber textos longos.
        [Column("Descricao")]
        public string Descricao { get; set; }

        [Column("DataAbertura")]
        public DateTime DataAbertura { get; set; }

        // O Status controla o Fluxo de Trabalho (Workflow).
        // Ex: Aberto -> Aceito -> Finalizado.
        [Column("Status")]
        public string Status { get; set; }

        // Chave Estrangeira (Foreign Key).
        // Aponta para a tabela de Usuários. É quem abriu o chamado.
        // Diferente da classe Mensagem, aqui você não colocou a propriedade virtual "Usuario".
        // Isso significa que para pegar o nome do usuário, você terá que fazer o Join manualmente no Controller
        // (como vimos no TicketsController) ou adicionar a propriedade virtual depois.
        [Column("UsuarioId")]
        public int UsuarioId { get; set; }

        // --- CAMPOS NULÁVEIS (Ciclo de Vida) ---

        // string? (com interrogação):
        // Significa que aceita NULL no banco de dados.
        // Lógica: Quando o chamado nasce, NINGUÉM pegou ele ainda. Então é NULL.
        [Column("ProfissionalDesignado")]
        public string? ProfissionalDesignado { get; set; }

        // Lógica: Se o problema não foi resolvido, não existe solução ainda. NULL.
        [Column("Solucao")]
        public string? Solucao { get; set; }

        // DateTime? (Nullable):
        // Lógica: Um chamado aberto não tem data de fim.
        // Esse campo só é preenchido no exato milissegundo em que o status muda para "Finalizado".
        [Column("DataFinalizacao")]
        public DateTime? DataFinalizacao { get; set; }
    }
}