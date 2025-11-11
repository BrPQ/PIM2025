using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using PIMIIIWeb_SQL_Login.Models; // <-- Seu modelo 'Usuario' local
using System.Collections.Generic;
// --- NOSSAS MUDANÇAS ---
using System.Net.Http;
using System.Net.Http.Headers; // Para usar o "Bearer" token
using System.Net.Http.Json;
using System.Threading.Tasks;
// --- FIM DAS MUDANÇAS ---


namespace PIMIIIWeb_SQL_Login.Pages
{
    // CUIDADO: A Role "Gestor" precisa existir na sua API,
    // ou o usuário do cookie (ex: "Admin") precisa bater com essa regra.
    [Authorize(Roles = "Gestor, Admin")] // Adicionei Admin por segurança
    public class GestaoUsuariosModel : PageModel
    {
        // --- SUBSTITUÍMOS O DBCONTEXT ---
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GestaoUsuariosModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        // --- FIM DA SUBSTITUIÇÃO ---


        [BindProperty]
        public Usuario NovoUsuario { get; set; } = new(); // <-- Este é o seu modelo local PIMIIIWeb_SQL_Login.Models.Usuario

        public List<Usuario> Usuarios { get; set; } = new();

        public string MensagemErro { get; private set; }

        public async Task OnGetAsync()
        {
            // O endpoint 'api/usuarios' (GET) NÃO ESTÁ no código que você me mandou
            // Estou ASSUMINDO que ele existe e retorna uma lista de usuários
            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return;

            try
            {
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/usuarios";
                // Usamos o seu modelo local 'Usuario' para ler a lista
                Usuarios = await client.GetFromJsonAsync<List<Usuario>>(apiUrl);
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao buscar usuários da API: " + ex.Message;
                Usuarios = new List<Usuario>(); // Garante que a lista não é nula
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Recarrega a lista de usuários
                return Page();
            }

            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return Page();

            try
            {
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/usuarios";

                // --- CORREÇÃO AQUI ---
                // Criamos o DTO com os nomes que a API agora espera
                var requestDto = new
                {
                    NomeUsuario = NovoUsuario.Nome, // <-- CORREÇÃO
                    Email = NovoUsuario.Email,
                    Matricula = NovoUsuario.Login,
                    Senha = NovoUsuario.Senha,
                    Role = NovoUsuario.Perfil       // <-- CORREÇÃO
                };

                var response = await client.PostAsJsonAsync(apiUrl, requestDto);
                // --- FIM DA CORREÇÃO ---

                if (!response.IsSuccessStatusCode)
                {
                    MensagemErro = "Erro ao criar usuário: " + await response.Content.ReadAsStringAsync();
                    await OnGetAsync(); // Recarrega a lista
                    return Page();
                }
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao salvar usuário na API: " + ex.Message;
                await OnGetAsync(); // Recarrega a lista
                return Page();
            }

            return RedirectToPage();
        }

        // --- MÉTODO AUXILIAR ---
        // Cria um HttpClient e já coloca o Token de autenticação nele
        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
            {
                MensagemErro = "Sessão expirada. Faça login novamente.";
                // Opcional: Fazer o logout
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