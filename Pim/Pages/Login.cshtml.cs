using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
// --- NOSSAS MUDANÇAS ---
using System.Net.Http;
using System.Net.Http.Json; // Precisa do package System.Net.Http.Json
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
// --- FIM DAS MUDANÇAS ---

namespace PIMIIIWeb_SQL_Login.Pages
{
    public class LoginModel : PageModel
    {
        // --- SUBSTITUÍMOS O DBCONTEXT ---
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        // --- FIM DA SUBSTITUIÇÃO ---

        [BindProperty]
        public string Login { get; set; } = string.Empty; // <-- Lembre-se: Isso será enviado como "Matricula"

        [BindProperty]
        public string Senha { get; set; } = string.Empty;

        public string MensagemErro { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Criar o objeto de requisição para a API
            // (Assumindo que "Login" da página é a "Matricula" da API)
            var loginRequest = new LoginRequestDto
            {
                Matricula = this.Login,
                Senha = this.Senha
            };

            var client = _httpClientFactory.CreateClient();
            var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/auth/login"; // Pega a URL do appsettings

            try
            {
                // 2. Chamar a API
                var response = await client.PostAsJsonAsync(apiUrl, loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    MensagemErro = "Matrícula ou senha inválidos.";
                    return Page();
                }

                // 3. Ler a resposta da API (Token e Usuário)
                var responseData = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (responseData == null || string.IsNullOrEmpty(responseData.Token) || responseData.Usuario == null)
                {
                    MensagemErro = "Erro ao ler a resposta da API.";
                    return Page();
                }

                // 4. GUARDAR O TOKEN NA SESSÃO
                // O "crachá" para falar com a API no futuro
                _httpContextAccessor.HttpContext.Session.SetString("jwt", responseData.Token);

                // 5. CRIAR O COOKIE DE LOGIN DO WEBAPP
                // (Usando os dados do usuário que a API retornou)
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, responseData.Usuario.NomeUsuario),
                    new Claim(ClaimTypes.NameIdentifier, responseData.Usuario.Id.ToString()),
                    new Claim(ClaimTypes.Role, responseData.Usuario.Role),
                    // Adicionei a matrícula aqui, pode ser útil
                    new Claim("Matricula", responseData.Usuario.Matricula)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToPage("/Index");
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Não foi possível conectar à API. " + ex.Message;
                return Page();
            }
        }
    }

    // --- CLASSES AUXILIARES PARA A API ---
    // (Precisa criar elas para o C# entender a requisição e a resposta)

    public class LoginRequestDto
    {
        public string Matricula { get; set; }
        public string Senha { get; set; }
    }

    public class LoginResponseDto
    {
        [JsonPropertyName("token")] // Garante que vai ler "token" minúsculo
        public string Token { get; set; }

        [JsonPropertyName("usuario")] // Garante que vai ler "usuario" minúsculo
        public UsuarioApiDto Usuario { get; set; }
    }

    public class UsuarioApiDto
    {
        // As propriedades aqui devem bater EXATAMENTE com o que a API retorna
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("nomeUsuario")]
        public string NomeUsuario { get; set; }
        [JsonPropertyName("matricula")]
        public string Matricula { get; set; }
        [JsonPropertyName("role")]
        public string Role { get; set; }
    }
}