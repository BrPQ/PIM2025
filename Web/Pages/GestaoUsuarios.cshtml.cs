using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using PIMIIIWeb_SQL_Login.Models; 
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers; 
using System.Net.Http.Json;
using System.Threading.Tasks;



namespace PIMIIIWeb_SQL_Login.Pages
{
    [Authorize(Roles = "Gestor, Admin")] 
    public class GestaoUsuariosModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GestaoUsuariosModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }


        [BindProperty]
        public Usuario NovoUsuario { get; set; } = new(); 

        public List<Usuario> Usuarios { get; set; } = new();

        public string MensagemErro { get; private set; }

        public async Task OnGetAsync()
        {
            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return;

            try
            {
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/usuarios";
                Usuarios = await client.GetFromJsonAsync<List<Usuario>>(apiUrl);
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao buscar usuários da API: " + ex.Message;
                Usuarios = new List<Usuario>(); 
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); 
                return Page();
            }

            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return Page();

            try
            {
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/usuarios";

                
                var requestDto = new
                {
                    NomeUsuario = NovoUsuario.Nome, 
                    Email = NovoUsuario.Email,
                    Matricula = NovoUsuario.Login,
                    Senha = NovoUsuario.Senha,
                    Role = NovoUsuario.Perfil       
                };

                var response = await client.PostAsJsonAsync(apiUrl, requestDto);
                

                if (!response.IsSuccessStatusCode)
                {
                    MensagemErro = "Erro ao criar usuário: " + await response.Content.ReadAsStringAsync();
                    await OnGetAsync(); 
                    return Page();
                }
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao salvar usuário na API: " + ex.Message;
                await OnGetAsync(); 
                return Page();
            }

            return RedirectToPage();
        }

        
        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
            {
                MensagemErro = "Sessão expirada. Faça login novamente.";
                
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }
}