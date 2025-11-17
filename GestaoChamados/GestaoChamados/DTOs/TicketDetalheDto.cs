using System;

namespace GestaoChamados.DTOs
{
    public class TicketDetalheDto
    {
        // --- Dados do Ticket ---
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public DateTime DataAbertura { get; set; }
        public string Status { get; set; }
        public string? ProfissionalDesignado { get; set; }
        public string? Solucao { get; set; }
        public DateTime? DataFinalizacao { get; set; }

        // --- Dados ATUAIS do Usuário ---
        public int UsuarioId { get; set; }
        public string NomeUsuario { get; set; } // Vem de Usuario.NomeUsuario
        public string PerfilUsuario { get; set; } // Vem de Usuario.Role
    }
}