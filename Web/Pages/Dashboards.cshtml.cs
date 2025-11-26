using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PIMIIIWeb_SQL_Login.Pages
{
    // DTO local para receber os dados do JSON da API.
    public class DashboardKpiDto
    {
        public int ChamadosHoje { get; set; }
        public int ChamadosOntem { get; set; }
        public double SlaCumpridoPercentual { get; set; }
        public double TempoMedioMinutosHoje { get; set; }
    }

    // PageModel: É o "Controller" das Razor Pages.
    public class DashboardsModel : PageModel
    {
        // ViewModel (VM) para exibir na tela HTML.
        // Record é uma forma moderna e curta de criar classes de dados imutáveis no C# 9+.
        public List<KpiVm> Kpis { get; set; } = new();
        public record KpiVm(string Label, string Valor, string Obs);

        // Dependências:
        // 1. Factory para criar HttpClients de forma performática.
        private readonly IHttpClientFactory _httpClientFactory;
        // 2. Configuration para ler o appsettings.json (onde está a URL da API).
        private readonly IConfiguration _configuration;
        // 3. Accessor para conseguir ler a Sessão do usuário atual.
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // Método executado automaticamente ao carregar a página (GET).
        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Cria um cliente HTTP já configurado com o Token JWT do usuário.
            var client = await CreateAuthenticatedClientAsync();

            // Se não conseguiu criar (ex: token expirou), retorna a página vazia (ou poderia redirecionar pro login).
            if (client == null)
            {
                Kpis = new List<KpiVm>();
                return Page();
            }

            try
            {
                // 2. Monta a URL chamando a API externa.
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/dashboard/kpis";

                // 3. Faz a chamada REST e já converte o JSON para objeto C# (GetFromJsonAsync).
                var stats = await client.GetFromJsonAsync<DashboardKpiDto>(apiUrl);

                if (stats != null)
                {
                    // 4. Processa os dados para exibição (Lógica de Apresentação).

                    // Cálculo de tendência:
                    int comparacao = stats.ChamadosHoje - stats.ChamadosOntem;
                    string obsChamados = (comparacao >= 0 ? "+" : "") + comparacao + " vs ontem";

                    Kpis.Add(new KpiVm("Chamados hoje", stats.ChamadosHoje.ToString(), obsChamados));
                    Kpis.Add(new KpiVm("SLA cumprido", stats.SlaCumpridoPercentual.ToString("F0") + "%", "meta >= 90%"));
                    Kpis.Add(new KpiVm("Tempo médio", stats.TempoMedioMinutosHoje.ToString("F0") + " min", "dentro do esperado"));
                }
            }
            catch (HttpRequestException ex)
            {
                // Em caso de erro (API fora do ar), mostra na tela em vez de quebrar o site.
                Kpis = new List<KpiVm> { new KpiVm("Erro", "N/A", ex.Message) };
            }

            return Page();
        }

        // --- HELPER PARA AUTENTICAÇÃO ---
        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            // Recupera o Token JWT que guardamos na Sessão no momento do Login.
            var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");

            if (string.IsNullOrEmpty(token))
            {
                // Se não tem token, desloga o usuário do Cookie local também.
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            // Cria o cliente HTTP.
            var client = _httpClientFactory.CreateClient();

            // Injeta o cabeçalho "Authorization: Bearer XYZ..."
            // Sem isso, a API retornaria 401 Unauthorized.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }
}