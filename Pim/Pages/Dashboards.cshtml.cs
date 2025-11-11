using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // Importante
using System.Threading.Tasks;

namespace PIMIIIWeb_SQL_Login.Pages
{
    // DTO local para receber os dados da API
    public class DashboardKpiDto
    {
        public int ChamadosHoje { get; set; }
        public int ChamadosOntem { get; set; }
        public double SlaCumpridoPercentual { get; set; }
        public double TempoMedioMinutosHoje { get; set; }
    }

    public class DashboardsModel : PageModel
    {
        public List<KpiVm> Kpis { get; set; } = new();
        public record KpiVm(string Label, string Valor, string Obs);

        // --- Injeção de Dependência ---
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // --- Trocamos OnGet por OnGetAsync ---
        public async Task<IActionResult> OnGetAsync()
        {
            var client = await CreateAuthenticatedClientAsync();
            if (client == null)
            {
                Kpis = new List<KpiVm>();
                return Page();
            }

            try
            {
                // Chamamos o novo endpoint da API
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/dashboard/kpis";
                var stats = await client.GetFromJsonAsync<DashboardKpiDto>(apiUrl);

                if (stats != null)
                {
                    // 1. Formata "Chamados hoje"
                    int comparacao = stats.ChamadosHoje - stats.ChamadosOntem;
                    string obsChamados = (comparacao >= 0 ? "+" : "") + comparacao + " vs ontem";
                    Kpis.Add(new KpiVm("Chamados hoje", stats.ChamadosHoje.ToString(), obsChamados));

                    // 2. Formata "SLA cumprido"
                    Kpis.Add(new KpiVm("SLA cumprido", stats.SlaCumpridoPercentual.ToString("F0") + "%", "meta >= 90%"));

                    // 3. Formata "Tempo médio"
                    Kpis.Add(new KpiVm("Tempo médio", stats.TempoMedioMinutosHoje.ToString("F0") + " min", "dentro do esperado"));
                }
            }
            catch (HttpRequestException ex)
            {
                // Se der erro (ex: 403 Forbidden), mostra kpis vazios
                Kpis = new List<KpiVm> { new KpiVm("Erro", "N/A", ex.Message) };
            }

            return Page();
        }

        // --- Método Auxiliar (copiado do IndexModel) ---
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
}