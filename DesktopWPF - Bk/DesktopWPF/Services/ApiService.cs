using Newtonsoft.Json;
using DesktopWPF.Models;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DesktopWPF.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7293"; // conferir se a porta está correta

        
        public string BaseUrl { get; } = ApiBaseUrl;

       
        public string AuthToken { get; private set; }


        public ApiService()
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler);
        }

        public async Task<Usuario> LoginAsync(string matricula, string senha)
        {
            var loginData = new { matricula = matricula, senha = senha };
            var jsonContent = JsonConvert.SerializeObject(loginData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{ApiBaseUrl}/api/auth/login", httpContent);

            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseJson);

                // Define o token para as requisições HTTP normais
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

                // (NOVO) Salva o token na nossa propriedade pública para o SignalR usar
                this.AuthToken = loginResponse.Token;

                return loginResponse.Usuario;
            }
            return null;
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{ApiBaseUrl}/api/tickets");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Ticket>>(content);
            }
            throw new HttpRequestException($"Erro ao buscar tickets: {response.StatusCode}");
        }

        public async Task<TicketDetalheDto> GetTicketByIdAsync(int ticketId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{ApiBaseUrl}/api/tickets/{ticketId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                // A única mudança é aqui: desserializa para o novo DTO
                return JsonConvert.DeserializeObject<TicketDetalheDto>(content);
            }
            throw new HttpRequestException($"Erro ao buscar detalhes do ticket: {response.StatusCode}");
        }

        public async Task<Usuario> GetUserByIdAsync(int usuarioId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{ApiBaseUrl}/api/usuarios/{usuarioId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Usuario>(content);
            }
            return null;
        }

        public async Task<bool> UpdateTicketAsync(int ticketId, object updateData)
        {
            var jsonContent = JsonConvert.SerializeObject(updateData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PutAsync($"{ApiBaseUrl}/api/tickets/{ticketId}", httpContent);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadAnexoAsync(int ticketId, string filePath, string tipoAnexo)
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            content.Add(new StreamContent(fileStream), "File", Path.GetFileName(filePath));
            content.Add(new StringContent(tipoAnexo), "tipoAnexo");

            HttpResponseMessage response = await _httpClient.PostAsync($"{ApiBaseUrl}/api/anexos/upload/{ticketId}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<byte[]> DownloadAnexoAsync(int anexoId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{ApiBaseUrl}/api/anexos/download/{anexoId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            throw new HttpRequestException($"Erro ao baixar o anexo: {response.StatusCode}");
        }

        public async Task<string> GetSugestaoIaAsync(string descricaoProblema, string perfil)
        {
            var requestData = new { Descricao = descricaoProblema, Perfil = perfil };
            var jsonContent = JsonConvert.SerializeObject(requestData);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/api/ai/sugestao-solucao")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Erro ao consultar a IA: {response.StatusCode} - {errorContent}");
        }

        public async Task<List<Anexo>> GetAnexosByTicketIdAsync(int ticketId, string tipoAnexo)
        {
            var url = $"{ApiBaseUrl}/api/anexos/{ticketId}?tipoAnexo={tipoAnexo}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Anexo>>(content);
            }
            return new List<Anexo>();
        }

        public async Task<List<Mensagem>> GetMensagensAsync(int ticketId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{ApiBaseUrl}/api/chat/{ticketId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Mensagem>>(content);
            }
            throw new HttpRequestException($"Erro ao buscar mensagens: {response.StatusCode}");
        }

        public async Task<Mensagem> EnviarMensagemAsync(int ticketId, int usuarioId, string conteudo)
        {
            var requestData = new { ticketId, usuarioId, conteudo };
            var jsonContent = JsonConvert.SerializeObject(requestData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync($"{ApiBaseUrl}/api/chat", httpContent);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Mensagem>(content);
            }
            return null;
        }

        public async Task<List<Ticket>> GetChatContactsAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{ApiBaseUrl}/api/chat/contatos");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Ticket>>(content);
            }
            throw new HttpRequestException($"Erro ao buscar contatos do chat: {response.StatusCode}");
        }
    }
}