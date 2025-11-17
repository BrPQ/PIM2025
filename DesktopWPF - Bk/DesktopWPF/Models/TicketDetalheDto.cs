
using System;
using Newtonsoft.Json;

namespace DesktopWPF.Models 
{


    public class TicketDetalheDto
    {
        [JsonProperty("id")] 
        public int Id { get; set; }

        [JsonProperty("titulo")] 
        public string Titulo { get; set; }

        [JsonProperty("descricao")] 
        public string Descricao { get; set; }

        [JsonProperty("dataAbertura")] 
        public DateTime DataAbertura { get; set; }

        [JsonProperty("status")] 
        public string Status { get; set; }

        [JsonProperty("profissionalDesignado")] 
        public string? ProfissionalDesignado { get; set; }

        [JsonProperty("solucao")] 
        public string? Solucao { get; set; }

        [JsonProperty("dataFinalizacao")] 
        public DateTime? DataFinalizacao { get; set; }

        [JsonProperty("usuarioId")] 
        public int UsuarioId { get; set; }

        [JsonProperty("nomeUsuario")] 
        public string NomeUsuario { get; set; }

        [JsonProperty("perfilUsuario")] 
        public string PerfilUsuario { get; set; }
    }
}