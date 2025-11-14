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

        
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        
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
                
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/dashboard/kpis";
                var stats = await client.GetFromJsonAsync<DashboardKpiDto>(apiUrl);

                if (stats != null)
                {
                    
                    int comparacao = stats.ChamadosHoje - stats.ChamadosOntem;
                    string obsChamados = (comparacao >= 0 ? "+" : "") + comparacao + " vs ontem";
                    Kpis.Add(new KpiVm("Chamados hoje", stats.ChamadosHoje.ToString(), obsChamados));

                    
                    Kpis.Add(new KpiVm("SLA cumprido", stats.SlaCumpridoPercentual.ToString("F0") + "%", "meta >= 90%"));

                    
                    Kpis.Add(new KpiVm("Tempo médio", stats.TempoMedioMinutosHoje.ToString("F0") + " min", "dentro do esperado"));
                }
            }
            catch (HttpRequestException ex)
            {
                
                Kpis = new List<KpiVm> { new KpiVm("Erro", "N/A", ex.Message) };
            }

            return Page();
        }

        
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