using System.Text.Json.Serialization; // Biblioteca nativa do .NET Core para lidar com JSON (mais leve que o Newtonsoft).

namespace PIMIIIWeb_SQL_Login.Models
{
    // POCO (Plain Old CLR Object) Híbrido:
    // Serve tanto para o Entity Framework criar a tabela no SQL Server local
    // quanto para desserializar o JSON que vem da API externa.
    public class Usuario
    {
        public int Id { get; set; }

        // [JsonPropertyName("nomeUsuario")]:
        // TRADUÇÃO DE JSON:
        // A API principal devolve um JSON assim: { "nomeUsuario": "Breno" }.
        // Mas aqui no site, preferimos chamar a propriedade apenas de "Nome".
        // Esse atributo diz: "Quando ler 'nomeUsuario' do JSON, jogue o valor aqui dentro de 'Nome'".
        [JsonPropertyName("nomeUsuario")]
        public string Nome { get; set; } = string.Empty; // Inicializa vazio para evitar NullReferenceException.

        public string Email { get; set; } = string.Empty;

        // Mapeamento:
        // A API devolve o campo "login" (que contém a matrícula).
        // Aqui mapeamos para a propriedade Login.
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        // Senha simples (para o contexto do Site Web).
        // Note que aqui não tem JsonPropertyName, pois a API NUNCA devolve a senha.
        // Esse campo só é usado localmente quando o usuário digita no formulário de login.
        public string Senha { get; set; } = string.Empty;

        // Mapeamento Importante:
        // A API envia "role": "Admin".
        // O Site espera "Perfil": "Admin".
        // O atributo faz a ponte entre o Inglês da API e o Português do Site.
        [JsonPropertyName("role")]
        public string Perfil { get; set; } = "Colaborador"; // Valor padrão se nada for informado.
    }
}