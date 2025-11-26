using System;
using Newtonsoft.Json; // A biblioteca que lê e escreve JSON.

namespace DesktopWPF.Models
{
    // DTO de Detalhes para o Desktop.
    // Esta classe recebe o JSON vindo da rota GET /api/tickets/{id}.
    public class TicketDetalheDto
    {
        // [JsonProperty("id")]: O QUE É ISSO?
        // É um "Mapeamento Explícito".
        // No mundo JavaScript/JSON, a convenção é usar "camelCase" (letra minúscula no começo: 'id').
        // No mundo C#, a convenção é "PascalCase" (letra Maiúscula no começo: 'Id').

        // Esse atributo diz: "Quando chegar um campo chamado 'id' (minúsculo) no JSON, 
        // coloque o valor dele dentro da propriedade 'Id' (maiúsculo) do C#."
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

        // Nullables (?):
        // Se o JSON vier com "profissionalDesignado": null,
        // o C# aceita sem dar erro de conversão.
        [JsonProperty("profissionalDesignado")]
        public string? ProfissionalDesignado { get; set; }

        [JsonProperty("solucao")]
        public string? Solucao { get; set; }

        [JsonProperty("dataFinalizacao")]
        public DateTime? DataFinalizacao { get; set; }

        // FLATTENING (Achatamento de Dados):
        // Lembre-se que no Back-end, "NomeUsuario" vinha de outra tabela (Join).
        // Aqui no Front-end, recebemos tudo mastigado.
        [JsonProperty("usuarioId")]
        public int UsuarioId { get; set; }

        [JsonProperty("nomeUsuario")]
        public string NomeUsuario { get; set; }

        [JsonProperty("perfilUsuario")]
        public string PerfilUsuario { get; set; }
    }
}