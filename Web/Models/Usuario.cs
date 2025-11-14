using System.Text.Json.Serialization; 

namespace PIMIIIWeb_SQL_Login.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        
        [JsonPropertyName("nomeUsuario")]
        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        
        
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        
        [JsonPropertyName("role")]
        public string Perfil { get; set; } = "Colaborador";
    }
}