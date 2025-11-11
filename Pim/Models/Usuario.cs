using System.Text.Json.Serialization; // <-- 1. ADICIONE ESTE USING

namespace PIMIIIWeb_SQL_Login.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        // 2. Mapeia 'Nome' (do Web) para 'NomeUsuario' (da API)
        [JsonPropertyName("nomeUsuario")]
        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // 3. Mapeia 'Login' (do Web) para 'Login' (da API)
        // (O nome 'Login' já está sendo enviado pelo GET da API)
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        // 4. Mapeia 'Perfil' (do Web) para 'Role' (da API)
        [JsonPropertyName("role")]
        public string Perfil { get; set; } = "Colaborador";
    }
}