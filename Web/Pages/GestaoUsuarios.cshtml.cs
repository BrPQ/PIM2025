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
    // [Authorize]: Segurança de Rota.
    // Garante que apenas usuários com perfil "Gestor" ou "Admin" entrem aqui.
    // Técnicos ou Funcionários comuns receberão "Acesso Negado" se tentarem acessar a URL direto.
    [Authorize(Roles = "Gestor, Admin")]
    public class GestaoUsuariosModel : PageModel
    {
        // INJEÇÃO DE DEPENDÊNCIA
        // _httpClientFactory: Cria clientes HTTP de forma otimizada (evita desperdício de sockets).
        // _configuration: Lê o arquivo appsettings.json (para pegar a URL da API).
        // _httpContextAccessor: Permite acessar a Sessão do usuário atual para pegar o Token JWT.
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GestaoUsuariosModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // [BindProperty]: DATA BINDING (Vínculo de Dados)
        // Esta propriedade recebe automaticamente os dados do formulário HTML quando o botão "Salvar" é clicado.
        // O objeto 'Usuario' aqui é o modelo local do site (PIMIIIWeb_SQL_Login.Models.Usuario).
        [BindProperty]
        public Usuario NovoUsuario { get; set; } = new();

        // Lista usada para preencher a tabela de usuários na parte de baixo da página.
        public List<Usuario> Usuarios { get; set; } = new();

        // Variável para exibir erros na tela (ex: "API fora do ar" ou "Matrícula duplicada").
        public string MensagemErro { get; private set; }

        // -------------------------------------------------------------------------
        // MÉTODO ON GET (Carregamento da Página)
        // Executado quando o usuário abre a URL /GestaoUsuarios
        // -------------------------------------------------------------------------
        public async Task OnGetAsync()
        {
            // 1. Cria um cliente HTTP já autenticado com o Token do usuário logado.
            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return; // Se não tem token (sessão expirou), para aqui.

            try
            {
                // 2. Define o endereço da API.
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/usuarios";

                // 3. Faz a chamada GET para a API.
                // O método GetFromJsonAsync faz duas coisas:
                // a) Baixa o JSON da API.
                // b) Converte (Deserializa) para List<Usuario>.
                // Graças ao atributo [JsonPropertyName] no modelo Usuario, os nomes dos campos são traduzidos automaticamente.
                Usuarios = await client.GetFromJsonAsync<List<Usuario>>(apiUrl);
            }
            catch (HttpRequestException ex)
            {
                // Tratamento de erro para não quebrar a página inteira ("Tela Amarela da Morte").
                MensagemErro = "Erro ao buscar usuários da API: " + ex.Message;
                Usuarios = new List<Usuario>();
            }
        }

        // -------------------------------------------------------------------------
        // MÉTODO ON POST (Envio do Formulário)
        // Executado quando o usuário clica no botão de submit do formulário.
        // -------------------------------------------------------------------------
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Validação Server-Side.
            // Verifica se o usuário preencheu campos obrigatórios (Nome, Email, Senha, etc).
            if (!ModelState.IsValid)
            {
                // Se inválido, recarrega a lista de usuários para a tabela não sumir e mostra os erros.
                await OnGetAsync();
                return Page();
            }

            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return Page();

            try
            {
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/usuarios";

                // 2. ADAPTER PATTERN (Adaptação de Dados):
                // O Site Web tem um modelo 'Usuario' (com campos Nome, Login, Perfil).
                // A API espera um DTO específico 'CreateUsuarioRequestDto' (com campos NomeUsuario, Matricula, Role).
                // Aqui criamos um objeto anônimo para "traduzir" os dados antes de enviar.
                var requestDto = new
                {
                    NomeUsuario = NovoUsuario.Nome,
                    Email = NovoUsuario.Email,
                    Matricula = NovoUsuario.Login, // De Login (Site) para Matricula (API)
                    Senha = NovoUsuario.Senha,
                    Role = NovoUsuario.Perfil      // De Perfil (Site) para Role (API)
                };

                // 3. Envia os dados via POST para a API.
                var response = await client.PostAsJsonAsync(apiUrl, requestDto);

                // 4. Verifica sucesso.
                if (!response.IsSuccessStatusCode)
                {
                    // Se a API retornou erro (ex: 409 Conflict - Matrícula já existe),
                    // lemos a mensagem de erro enviada pela API e mostramos na tela.
                    MensagemErro = "Erro ao criar usuário: " + await response.Content.ReadAsStringAsync();
                    await OnGetAsync(); // Recarrega a tabela.
                    return Page();
                }
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao salvar usuário na API: " + ex.Message;
                await OnGetAsync();
                return Page();
            }

            // 5. PADRÃO PRG (Post-Redirect-Get):
            // Se deu tudo certo, redirecionamos o usuário para a mesma página.
            // Isso limpa o formulário e evita que, se ele apertar F5, o navegador tente reenviar o cadastro (duplicando dados).
            return RedirectToPage();
        }

        // -------------------------------------------------------------------------
        // MÉTODO AUXILIAR DE AUTENTICAÇÃO
        // -------------------------------------------------------------------------
        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            // O Token JWT está guardado na Sessão (Session) do servidor Web.
            // Ele foi colocado lá no momento do Login.
            var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");

            if (string.IsNullOrEmpty(token))
            {
                MensagemErro = "Sessão expirada. Faça login novamente.";

                // Se o token sumiu, forçamos o logout do Cookie também para deixar tudo sincronizado.
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            // Cria o cliente HTTP usando a Factory.
            var client = _httpClientFactory.CreateClient();

            // Injeta o token no cabeçalho "Authorization".
            // Formato: "Bearer eyJhbGciOiJIUz..."
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }
}