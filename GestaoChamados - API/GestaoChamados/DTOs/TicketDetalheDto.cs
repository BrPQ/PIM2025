using System;

namespace GestaoChamados.DTOs
{
    // DTO de Detalhes (View Model):
    // Serve para exibir a tela cheia do chamado.
    // Diferente da lista (que só tem resumo), aqui mostramos TUDO.
    public class TicketDetalheDto
    {
        // --- Dados do Ticket (Cópia da tabela Ticket) ---
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; } // Texto longo explicando o problema.
        public DateTime DataAbertura { get; set; }
        public string Status { get; set; }

        // --- Nullables (?) ---
        // Por que estes campos têm interrogação?
        // Porque quando o chamado é aberto, ainda NÃO EXISTE técnico e NÃO EXISTE solução.
        // Se fossem obrigatórios, o código quebraria na hora de abrir um chamado novo.
        public string? ProfissionalDesignado { get; set; }
        public string? Solucao { get; set; }
        public DateTime? DataFinalizacao { get; set; }

        // --- Dados ATUAIS do Usuário (Flattening / Achatamento) ---
        // O Front-end não quer receber um objeto complexo do tipo "Ticket.Usuario.Nome".
        // Ele prefere receber tudo no primeiro nível para facilitar o "Data Binding" na tela.
        // O Controller faz a busca (JOIN) e preenche estes campos.
        public int UsuarioId { get; set; }
        public string NomeUsuario { get; set; } // Ex: "Maria da Silva"
        public string PerfilUsuario { get; set; } // Ex: "Marketing" (Para o técnico saber com quem está lidando).
    }
}