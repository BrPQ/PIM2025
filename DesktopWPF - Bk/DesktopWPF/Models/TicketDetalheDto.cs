// Em: Models/TicketDetalheDto.cs
using System;
using Newtonsoft.Json;

namespace DesktopWPF.Models // Certifique-se que o namespace está correto
{
    // Em: Models/TicketDetalheDto.cs

    public class TicketDetalheDto
    {
        [JsonProperty("id")] // <-- ADICIONE ISSO
        public int Id { get; set; }

        [JsonProperty("titulo")] // <-- ADICIONE ISSO
        public string Titulo { get; set; }

        [JsonProperty("descricao")] // <-- ADICIONE ISSO
        public string Descricao { get; set; }

        [JsonProperty("dataAbertura")] // <-- ADICIONE ISSO
        public DateTime DataAbertura { get; set; }

        [JsonProperty("status")] // <-- ADICIONE ISSO
        public string Status { get; set; }

        [JsonProperty("profissionalDesignado")] // <-- ADICIONE ISSO
        public string? ProfissionalDesignado { get; set; }

        [JsonProperty("solucao")] // <-- ADICIONE ISSO
        public string? Solucao { get; set; }

        [JsonProperty("dataFinalizacao")] // <-- ADICIONE ISSO
        public DateTime? DataFinalizacao { get; set; }

        [JsonProperty("usuarioId")] // <-- ADICIONE ISSO
        public int UsuarioId { get; set; }

        [JsonProperty("nomeUsuario")] // <-- ADICIONE ISSO
        public string NomeUsuario { get; set; }

        [JsonProperty("perfilUsuario")] // <-- ADICIONE ISSO
        public string PerfilUsuario { get; set; }
    }
}