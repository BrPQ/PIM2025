using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // <-- Importante
using System.Threading.Tasks;

// Um DTO local para receber os dados da API
public class DashboardStatsDto
{
    public int EmAtendimento { get; set; }
    public int Atrasados { get; set; }
    public double SlaMedioMinutos { get; set; }
    public int Gargalos { get; set; }
}

public class IndexModel : PageModel
{
    // Injeção de dependência (igual ao GestaoUsuariosModel)
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Propriedades para exibir no HTML
    [ViewData]
    public int EmAtendimentoCount { get; set; } = 0;
    [ViewData]
    public int AtrasadosCount { get; set; } = 0;
    [ViewData]
    public int SlaMedioMinutos { get; set; } = 0;
    [ViewData]
    public int GargalosCount { get; set; } = 0;
    [ViewData]
    public string MensagemErro { get; set; }

    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    // Trocamos OnGet por OnGetAsync
    public async Task OnGetAsync()
    {
        var client = await CreateAuthenticatedClientAsync();
        if (client == null)
        {
            MensagemErro = "Sessão expirada. Faça login novamente.";
            return;
        }

        try
        {
            // Chamamos o novo endpoint da API
            var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/dashboard/stats";
            var stats = await client.GetFromJsonAsync<DashboardStatsDto>(apiUrl);

            if (stats != null)
            {
                // Preenchemos as propriedades
                EmAtendimentoCount = stats.EmAtendimento;
                AtrasadosCount = stats.Atrasados;
                SlaMedioMinutos = (int)stats.SlaMedioMinutos;
                GargalosCount = stats.Gargalos;
            }
        }
        catch (HttpRequestException ex)
        {
            // Se o usuário não for Admin/Gestor, vai dar erro 403 (Forbidden)
            if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                MensagemErro = "Você não tem permissão para ver este dashboard.";
            }
            else
            {
                MensagemErro = "Erro ao buscar dados do dashboard: " + ex.Message;
            }
        }
    }

    // Método auxiliar (copiado do GestaoUsuariosModel)
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");
        if (string.IsNullOrEmpty(token))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}