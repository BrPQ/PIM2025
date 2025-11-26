using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

// DTO Local:
// Serve para receber o JSON que vem da rota /api/dashboard/stats.
// Tem exatamente os mesmos campos do DTO que criamos lá no Back-end.
public class DashboardStatsDto
{
    public int EmAtendimento { get; set; }
    public int Atrasados { get; set; }
    public double SlaMedioMinutos { get; set; }
    public int Gargalos { get; set; }
}

// Model da Página (Controller da View):
public class IndexModel : PageModel
{
    // Dependências padrão para fazer chamadas HTTP e ler configurações.
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // --- VIEW DATA (Comunicação C# -> HTML) ---
    // O atributo [ViewData] é mágico no Razor Pages.
    // Ao definir valores aqui, eles ficam disponíveis automaticamente no arquivo .cshtml
    // através do dicionário ViewData["EmAtendimentoCount"].
    // Usamos isso para preencher os "Cards" coloridos do Dashboard.

    [ViewData]
    public int EmAtendimentoCount { get; set; } = 0;

    [ViewData]
    public int AtrasadosCount { get; set; } = 0;

    [ViewData]
    public int SlaMedioMinutos { get; set; } = 0;

    [ViewData]
    public int GargalosCount { get; set; } = 0;

    [ViewData]
    public string MensagemErro { get; set; } // Para mostrar alertas vermelhos no topo da página.

    // Construtor com Injeção de Dependência.
    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    // -------------------------------------------------------------------------
    // MÉTODO ON GET (Ao carregar a página)
    // -------------------------------------------------------------------------
    public async Task OnGetAsync()
    {
        // 1. Prepara o cliente HTTP com o Token JWT da sessão.
        var client = await CreateAuthenticatedClientAsync();

        // Se o cliente voltou nulo, significa que o token expirou ou não existe.
        if (client == null)
        {
            MensagemErro = "Sessão expirada. Faça login novamente.";
            return; // Para a execução e mostra a página com erro (ou redireciona).
        }

        try
        {
            // 2. Chama a API de Estatísticas (aquela que calcula os números rápidos).
            var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/dashboard/stats";
            var stats = await client.GetFromJsonAsync<DashboardStatsDto>(apiUrl);

            // 3. Se a API respondeu com dados, preenchemos as variáveis do ViewData.
            if (stats != null)
            {
                EmAtendimentoCount = stats.EmAtendimento;
                AtrasadosCount = stats.Atrasados;
                SlaMedioMinutos = (int)stats.SlaMedioMinutos; // Cast para int (arredondando visualmente).
                GargalosCount = stats.Gargalos;
            }
        }
        catch (HttpRequestException ex)
        {
            // --- TRATAMENTO DE ERRO ESPECÍFICO ---
            // Se a API retornar 403 (Forbidden), significa que o usuário logou, 
            // mas ele é um "Técnico" ou "Funcionário" e não tem permissão de ver Dashboard.
            if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                MensagemErro = "Você não tem permissão para ver este dashboard.";

                // Zera os contadores para não mostrar dados parciais.
                EmAtendimentoCount = 0;
                AtrasadosCount = 0;
                // ...
            }
            else
            {
                // Erro genérico (Servidor desligado, 500 Internal Server Error, etc).
                MensagemErro = "Erro ao buscar dados do dashboard: " + ex.Message;
            }
        }
    }

    // -------------------------------------------------------------------------
    // HELPER DE AUTENTICAÇÃO (Igual às outras páginas)
    // -------------------------------------------------------------------------
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        // Recupera o Token da Sessão.
        var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");

        if (string.IsNullOrEmpty(token))
        {
            // Se não tem token, força o logout do Cookie do site.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        // Anexa o token no cabeçalho da requisição.
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}