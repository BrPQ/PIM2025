using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace PIMIIIWeb_SQL_Login.Pages
{
    // Model da Página de Login (Controller da View Login.cshtml).
    public class LoginModel : PageModel
    {
        // Dependências para comunicação HTTP e acesso à configuração (URL da API).
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Accessor: Necessário para gravar dados na Sessão do usuário (Session["jwt"]).
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // [BindProperty]: Captura o que o usuário digitou no input <input asp-for="Login">
        [BindProperty]
        public string Login { get; set; } = string.Empty;

        [BindProperty]
        public string Senha { get; set; } = string.Empty;

        // Exibe erros na tela (ex: "Senha incorreta").
        public string MensagemErro { get; set; } = string.Empty;

        // Método GET: Carrega a tela de login vazia.
        public void OnGet()
        {
        }

        // -------------------------------------------------------------------------
        // MÉTODO POST (Ao clicar em "Entrar")
        // -------------------------------------------------------------------------
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Prepara o DTO para enviar à API.
            var loginRequest = new LoginRequestDto
            {
                Matricula = this.Login,
                Senha = this.Senha
            };

            var client = _httpClientFactory.CreateClient();
            var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/auth/login";

            try
            {
                // 2. Chama a API de Autenticação.
                var response = await client.PostAsJsonAsync(apiUrl, loginRequest);

                // Se a API recusar (401 Unauthorized), mostramos erro pro usuário.
                if (!response.IsSuccessStatusCode)
                {
                    MensagemErro = "Matrícula ou senha inválidos.";
                    return Page();
                }

                // 3. Lê a resposta (que contém o Token JWT e os dados do usuário).
                var responseData = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                // Validação de integridade.
                if (responseData == null || string.IsNullOrEmpty(responseData.Token) || responseData.Usuario == null)
                {
                    MensagemErro = "Erro ao ler a resposta da API.";
                    return Page();
                }

                // 4. VERIFICAÇÃO DE PERFIL (Segurança de Front-end):
                // O site é APENAS para gestão. Funcionários comuns não entram aqui.
                // Se um funcionário tentar logar, barramos, mesmo que a senha esteja certa.
                var userRole = responseData.Usuario.Role;
                var permittedRoles = new List<string> { "Admin", "Gestor", "Tecnico" }; // "Funcionario" está fora.

                if (!permittedRoles.Contains(userRole))
                {
                    MensagemErro = "Você não tem permissão de acesso a este sistema.";
                    return Page();
                }

                // 5. ARMAZENAMENTO DO TOKEN:
                // Guardamos o JWT na Sessão do servidor Web.
                // Nas próximas páginas (Dashboard, Usuarios), vamos ler daqui para chamar a API.
                _httpContextAccessor.HttpContext.Session.SetString("jwt", responseData.Token);

                // 6. CRIAÇÃO DO COOKIE DE AUTENTICAÇÃO (Identidade Local):
                // O ASP.NET Core Web usa Cookies para saber quem está logado.
                // Criamos uma "Identidade" baseada nos dados que vieram da API.
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, responseData.Usuario.NomeUsuario),
                    new Claim(ClaimTypes.NameIdentifier, responseData.Usuario.Id.ToString()),
                    new Claim(ClaimTypes.Role, responseData.Usuario.Role),
                    new Claim("Matricula", responseData.Usuario.Matricula)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // O comando SignInAsync emite o Cookie criptografado para o navegador do usuário.
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Redireciona para o Dashboard.
                return RedirectToPage("/Index");
            }
            catch (HttpRequestException ex)
            {
                // Tratamento para quando a API está offline.
                MensagemErro = "Não foi possível conectar à API. " + ex.Message;
                return Page();
            }
        }
    }

    // --- DTOs LOCAIS (Helpers) ---
    // Classes simples para mapear o JSON de envio e resposta da API.
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