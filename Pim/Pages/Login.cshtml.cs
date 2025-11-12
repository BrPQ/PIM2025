using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
// --- NOSSAS MUDANÇAS ---
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic; // <-- ADICIONADO PARA A LISTA
// --- FIM DAS MUDANÇAS ---

namespace PIMIIIWeb_SQL_Login.Pages
{
    public class LoginModel : PageModel
    {
        // --- Construtor e Propriedades (Sem mudanças) ---
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public string Login { get; set; } = string.Empty;
        [BindProperty]
        public string Senha { get; set; } = string.Empty;
        public string MensagemErro { get; set; } = string.Empty;

        public void OnGet()
        {
        }
        // --- Fim do Construtor ---

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Criar o objeto de requisição
            var loginRequest = new LoginRequestDto
            {
                Matricula = this.Login,
                Senha = this.Senha
            };

            var client = _httpClientFactory.CreateClient();
            var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/auth/login";

            try
            {
                // 2. Chamar a API
                var response = await client.PostAsJsonAsync(apiUrl, loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    MensagemErro = "Matrícula ou senha inválidos.";
                    return Page();
                }

                // 3. Ler a resposta da API
                var responseData = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (responseData == null || string.IsNullOrEmpty(responseData.Token) || responseData.Usuario == null)
                {
                    MensagemErro = "Erro ao ler a resposta da API.";
                    return Page();
                }

                // --- NOVA VERIFICAÇÃO DE PERMISSÃO (REGRA 1) ---
                var userRole = responseData.Usuario.Role;
                var permittedRoles = new List<string> { "Admin", "Gestor", "Tecnico" };

                // Se o perfil do usuário NÃO ESTIVER na lista, bloqueia o login.
                if (!permittedRoles.Contains(userRole))
                {
                    MensagemErro = "Você não tem permissão de acesso a este sistema.";
                    return Page();
                }
                // --- FIM DA VERIFICAÇÃO ---

                // 4. GUARDAR O TOKEN NA SESSÃO
                _httpContextAccessor.HttpContext.Session.SetString("jwt", responseData.Token);

                // 5. CRIAR O COOKIE DE LOGIN DO WEBAPP
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, responseData.Usuario.NomeUsuario),
                    new Claim(ClaimTypes.NameIdentifier, responseData.Usuario.Id.ToString()),
                    new Claim(ClaimTypes.Role, responseData.Usuario.Role),
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

    // --- CLASSES AUXILIARES (Sem mudanças) ---
    public class LoginRequestDto
    {
        public string Matricula { get; set; }
        public string Senha { get; set; }
    }
    public class LoginResponseDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("usuario")]
        public UsuarioApiDto Usuario { get; set; }
    }
    public class UsuarioApiDto
    {
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