using Newtonsoft.Json; // A biblioteca padrão-ouro para transformar Objetos C# em Texto JSON e vice-versa.
using DesktopWPF.Models;
using System.Collections.Generic;
using System.IO;
using System.Net.Http; // Namespace onde mora o HttpClient (nosso navegador invisível).
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DesktopWPF.Services
{
    // SERVICE PATTERN (Padrão de Serviço):
    // Centralizamos toda a comunicação externa nesta classe.
    // As janelas (Views) nunca chamam a URL direta, elas pedem para o ApiService fazer isso.
    public class ApiService
    {
        // HttpClient: É a classe que efetivamente faz a conexão TCP/IP com o servidor.
        private readonly HttpClient _httpClient;

        // Endereço fixo do seu Back-end rodando localmente.
        // Dica pra banca: "Professor, usamos localhost para desenvolvimento, mas em produção isso ficaria num arquivo de configuração."
        private const string ApiBaseUrl = "https://localhost:7293";

        // Propriedade pública para leitura da URL base.
        public string BaseUrl { get; } = ApiBaseUrl;

        // MEMÓRIA DE SESSÃO:
        // Aqui guardamos o Token JWT. Enquanto o aplicativo estiver aberto, 
        // essa variável segura o "crachá" do usuário logado.
        public string AuthToken { get; private set; }

        // CONSTRUTOR (Executado assim que o app abre)
        public ApiService()
        {
            // --- HACK DE SSL (LOCALHOST) ---
            // Certificados de localhost (desenvolvimento) não são assinados por autoridades globais.
            // O Windows normalmente bloquearia a conexão dizendo "Site Inseguro".
            // O código abaixo diz: "Confie em qualquer certificado, mesmo que pareça suspeito."
            // Importante: Isso só deve ser usado em ambiente de testes (Dev).
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };

            // Inicializa o cliente com essa regra de permissão total.
            _httpClient = new HttpClient(handler);
        }

        // 1. AUTENTICAÇÃO (LOGIN)
        public async Task<Usuario> LoginAsync(string matricula, string senha)
        {
            // Passo 1: Cria um objeto anônimo com os dados.
            var loginData = new { matricula = matricula, senha = senha };

            // Passo 2: SERIALIZAÇÃO (C# -> JSON).
            // Transforma o objeto na string: '{"matricula":"123", "senha":"abc"}'
            var jsonContent = JsonConvert.SerializeObject(loginData);

            // Passo 3: Empacota o JSON para trafegar via HTTP (definindo que é application/json).
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Passo 4: Envia o POST e espera (await) a resposta.
            HttpResponseMessage response = await _httpClient.PostAsync($"{ApiBaseUrl}/api/auth/login", httpContent);

            if (response.IsSuccessStatusCode)
            {
                // Passo 5: Lê a resposta do servidor (que contém o Token).
                string responseJson = await response.Content.ReadAsStringAsync();

                // Passo 6: DESERIALIZAÇÃO (JSON -> C#).
                // Transforma o texto de volta em um objeto 'LoginResponse'.
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseJson);

                // *** PONTO CRUCIAL (Persistence do Token) ***
                // Configuramos o cabeçalho "Authorization" padrão.
                // A partir de agora, TODAS as requisições levarão esse token automaticamente.
                // Não precisamos passar o token manualmente nas outras funções.
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

                // Guarda o token na propriedade da classe também.
                this.AuthToken = loginResponse.Token;

                // Retorna os dados do usuário para a tela poder mostrar "Bem vindo, Breno".
                return loginResponse.Usuario;
            }
            // Se errou a senha, retorna nulo.
            return null;
        }

        // 2. LISTAGEM DE TICKETS
        public async Task<List<Ticket>> GetTicketsAsync()
        {
            // Faz o GET. O Token vai junto automaticamente graças à linha do Login acima.
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/api/tickets");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                // Converte o JSON Array "[{}, {}, {}]" em uma Lista C# "List<Ticket>".
                return JsonConvert.DeserializeObject<List<Ticket>>(content);
            }
            // Tratamento de erro robusto: Lança exceção se o servidor cair ou der erro 500.
            throw new HttpRequestException($"Erro ao buscar tickets: {response.StatusCode}");
        }

        // 3. DETALHES DO TICKET (DTO)
        public async Task<TicketDetalheDto> GetTicketByIdAsync(int ticketId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/api/tickets/{ticketId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                // Note que aqui retorna o DTO (TicketDetalheDto), que é mais completo que o Ticket normal.
                return JsonConvert.DeserializeObject<TicketDetalheDto>(content);
            }
            throw new HttpRequestException($"Erro ao buscar detalhes do ticket: {response.StatusCode}");
        }

        // 4. BUSCAR USUÁRIO
        public async Task<Usuario> GetUserByIdAsync(int usuarioId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/api/usuarios/{usuarioId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Usuario>(content);
            }
            return null;
        }


        // 5. ATUALIZAR TICKET (PUT)
        public async Task<bool> UpdateTicketAsync(int ticketId, object updateData)
        {
            // Recebe um objeto genérico (pode ser o DTO de update).
            var jsonContent = JsonConvert.SerializeObject(updateData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // PUT: Verbo HTTP usado para Alteração/Edição.
            HttpResponseMessage response = await _httpClient.PutAsync($"{BaseUrl}/api/tickets/{ticketId}", httpContent);

            // Retorna True se deu certo (200 OK ou 204 NoContent), False se deu erro.
            return response.IsSuccessStatusCode;
        }

        // 6. UPLOAD DE ARQUIVO (COMPLEXO)
        public async Task<bool> UploadAnexoAsync(int ticketId, string filePath, string tipoAnexo)
        {
            // MultipartFormDataContent:
            // Diferente do JSON, este formato permite enviar arquivos binários + textos misturados.
            // É o mesmo formato usado quando você anexa algo no Gmail.
            using var content = new MultipartFormDataContent();

            // Abre o arquivo do disco local do computador (Leitura).
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            // Adiciona o Fluxo (Stream) do arquivo ao pacote.
            // "File" é o nome do campo que o Back-end espera no [FromForm].
            content.Add(new StreamContent(fileStream), "File", Path.GetFileName(filePath));

            // Adiciona o metadado (tipo do anexo) como string simples.
            content.Add(new StringContent(tipoAnexo), "tipoAnexo");

            // Envia via POST. O C# gerencia o streaming dos bytes automaticamente.
            HttpResponseMessage response = await _httpClient.PostAsync($"{BaseUrl}/api/anexos/upload/{ticketId}", content);
            return response.IsSuccessStatusCode;
        }

        // 7. DOWNLOAD DE ARQUIVO (BYTES)
        public async Task<byte[]> DownloadAnexoAsync(int anexoId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/api/anexos/download/{anexoId}");
            if (response.IsSuccessStatusCode)
            {
                // Aqui está a diferença: ReadAsByteArrayAsync.
                // Não queremos texto string. Queremos o array de bytes bruto (byte[]).
                // Isso é necessário para reconstruir imagens, PDFs, etc. sem corromper os dados.
                return await response.Content.ReadAsByteArrayAsync();
            }
            throw new HttpRequestException($"Erro ao baixar o anexo: {response.StatusCode}");
        }

        // 8. INTELIGÊNCIA ARTIFICIAL (IA)
        public async Task<string> GetSugestaoIaAsync(string descricaoProblema, string perfil)
        {
            var requestData = new { Descricao = descricaoProblema, Perfil = perfil };
            var jsonContent = JsonConvert.SerializeObject(requestData);

            // Configuração Manual da Requisição (HttpRequestMessage).
            // Usamos isso quando precisamos de configurações finas que os métodos atalhos (PostAsync) não dão.
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/ai/sugestao-solucao")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // Negociação de Conteúdo (Content Negotiation):
            // Limpamos os headers de aceitação e dizemos explicitamente:
            // "Servidor, por favor, me responda em TEXTO PLANO (text/plain)".
            // Isso garante que a IA não mande um JSON complexo, apenas a frase de resposta.
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            // Envia a requisição customizada.
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Erro ao consultar a IA: {response.StatusCode} - {errorContent}");
        }

        // 9. CHAT E MENSAGENS
        public async Task<List<Anexo>> GetAnexosByTicketIdAsync(int ticketId, string tipoAnexo)
        {
            // Concatenação de URL com Query String (?tipoAnexo=...)
            var url = $"{BaseUrl}/api/anexos/{ticketId}?tipoAnexo={tipoAnexo}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Anexo>>(content);
            }
            // Se der erro ou não tiver anexos, retorna lista vazia para não quebrar a tela.
            return new List<Anexo>();
        }

        public async Task<List<Mensagem>> GetMensagensAsync(int ticketId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/api/chat/{ticketId}");
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

            HttpResponseMessage response = await _httpClient.PostAsync($"{BaseUrl}/api/chat", httpContent);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                // Retorna a mensagem criada (com ID e Data preenchidos pelo servidor) 
                // para o Desktop poder adicionar na tela imediatamente.
                return JsonConvert.DeserializeObject<Mensagem>(content);
            }
            return null;
        }

        public async Task<List<Ticket>> GetChatContactsAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{BaseUrl}/api/chat/contatos");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Ticket>>(content);
            }
            throw new HttpRequestException($"Erro ao buscar contatos do chat: {response.StatusCode}");
        }
    }
}