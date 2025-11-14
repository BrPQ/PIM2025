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
    public class LoginModel : PageModel
    {
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
        

        public async Task<IActionResult> OnPostAsync()
        {
            
            var loginRequest = new LoginRequestDto
            {
                Matricula = this.Login,
                Senha = this.Senha
            };

            var client = _httpClientFactory.CreateClient();
            var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/auth/login";

            try
            {
                
                var response = await client.PostAsJsonAsync(apiUrl, loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    MensagemErro = "Matrícula ou senha inválidos.";
                    return Page();
                }

                
                var responseData = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (responseData == null || string.IsNullOrEmpty(responseData.Token) || responseData.Usuario == null)
                {
                    MensagemErro = "Erro ao ler a resposta da API.";
                    return Page();
                }

                
                var userRole = responseData.Usuario.Role;
                var permittedRoles = new List<string> { "Admin", "Gestor", "Tecnico" };

                
                if (!permittedRoles.Contains(userRole))
                {
                    MensagemErro = "Você não tem permissão de acesso a este sistema.";
                    return Page();
                }
                

                
                _httpContextAccessor.HttpContext.Session.SetString("jwt", responseData.Token);

                
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