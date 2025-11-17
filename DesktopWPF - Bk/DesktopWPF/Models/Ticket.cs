using System;

namespace DesktopWPF.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public DateTime DataAbertura { get; set; }
        public string Status { get; set; }
        public int UsuarioId { get; set; }
        public string? ProfissionalDesignado { get; set; }
        public string? Solucao { get; set; }
    }
}