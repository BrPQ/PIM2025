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
    // DTO para exibir a tabela na tela.
    public class TicketRelatorioVm
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("titulo")] public string Titulo { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("dataAbertura")] public DateTime DataAbertura { get; set; }
        [JsonPropertyName("profissionalDesignado")] public string? ProfissionalDesignado { get; set; }
        [JsonPropertyName("dataFinalizacao")] public DateTime? DataFinalizacao { get; set; }
    }

    // Acesso permitido para todo mundo (Gestor, Admin, Tecnico).
    [Authorize(Roles = "Gestor, Admin, Tecnico")]
    public class RelatoriosModel : PageModel
    {
        public List<TicketRelatorioVm> Tickets { get; set; } = new();
        public string MensagemErro { get; private set; }

        // URL para gerar o botão de download direto (se necessário).
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

        // --- MÉTODO GET (Carregar Tabela) ---
        public async Task OnGetAsync()
        {
            var client = await CreateAuthenticatedClientAsync();
            if (client == null)
            {
                MensagemErro = "Sessão expirada.";
                return;
            }

            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            ExportUrl = baseUrl + "/api/tickets/exportar-excel";

            try
            {
                // Busca apenas o histórico (Finalizados/Cancelados).
                var apiUrl = baseUrl + "/api/tickets/relatorio";
                Tickets = await client.GetFromJsonAsync<List<TicketRelatorioVm>>(apiUrl);
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro ao buscar dados da API: " + ex.Message;
            }
        }

        // Helper de autenticação (padrão).
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

        // --- MÉTODO POST (Baixar Excel) ---
        // Executado quando o usuário clica no botão "Exportar Excel".
        public async Task<IActionResult> OnPostExportarExcelAsync()
        {
            var client = await CreateAuthenticatedClientAsync();
            if (client == null) return Page();

            try
            {
                // 1. Chama o endpoint da API que gera o Excel (aquele com ClosedXML).
                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/tickets/exportar-excel";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // 2. Lê os bytes do arquivo que veio da API.
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // 3. Entrega o arquivo para o navegador do usuário baixar.
                    // Define o MIME Type correto para planilhas .xlsx.
                    return File(
                        fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Relatorio_Tickets.xlsx");
                }

                MensagemErro = "Erro ao gerar o relatório: " + response.ReasonPhrase;
                await OnGetAsync();
                return Page();
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = "Erro de conexão com a API: " + ex.Message;
                await OnGetAsync();
                return Page();
            }
        }
    }
}