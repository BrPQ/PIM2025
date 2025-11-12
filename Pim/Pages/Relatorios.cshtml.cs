using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PIMIIIWeb_SQL_Login.Pages
{
    // Modelo local para receber os dados do ticket
    public class TicketRelatorioVm
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("titulo")]
        public string Titulo { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("dataAbertura")]
        public DateTime DataAbertura { get; set; }
        [JsonPropertyName("profissionalDesignado")]
        public string? ProfissionalDesignado { get; set; }
        [JsonPropertyName("dataFinalizacao")]
        public DateTime? DataFinalizacao { get; set; }
    }

    [Authorize(Roles = "Gestor, Admin, Tecnico")] // Protege a página
    public class RelatoriosModel : PageModel
    {
        public List<TicketRelatorioVm> Tickets { get; set; } = new();
        public string MensagemErro { get; private set; }

        // URL da API para o botão de exportar
        public string ExportUrl { get; set; }

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RelatoriosModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnGetAsync()
        {
            var client = await CreateAuthenticatedClientAsync();
            if (client == null)
            {
                MensagemErro = "Sessão expirada.";
                return;
            }

            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            // 1. Define a URL para o botão de exportar
            ExportUrl = baseUrl + "/api/tickets/exportar-excel";

            try
            {
                // 2. Busca a lista de tickets para exibir na tela
                var apiUrl = baseUrl + "/api/tickets/relatorio";
                Tickets = await client.GetFromJsonAsync<List<TicketRelatorioVm>>(apiUrl);
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao buscar dados da API: " + ex.Message;
            }
        }

        // Método auxiliar (o mesmo das outras páginas)
        private async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> OnPostExportarExcelAsync()
        {
            var client = await CreateAuthenticatedClientAsync();
            if (client == null)
            {
                // Se o token expirar, não faz nada
                return Page();
            }

            try
            {
                // 1. Define a URL da API (que estava no OnGet)
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/tickets/exportar-excel";

                // 2. Chama a API e pega a resposta
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // 3. Lê o arquivo como um array de bytes
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // 4. Retorna o arquivo para download
                    return File(
                        fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Relatorio_Tickets.xlsx");
                }

                // Se der erro na API, apenas recarrega a página
                MensagemErro = "Erro ao gerar o relatório: " + response.ReasonPhrase;
                await OnGetAsync(); // Recarrega a lista
                return Page();
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro de conexão com a API: " + ex.Message;
                await OnGetAsync(); // Recarrega a lista
                return Page();
            }
        }
    }
}