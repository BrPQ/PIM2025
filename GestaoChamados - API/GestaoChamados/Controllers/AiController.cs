using GestaoChamados.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace GestaoChamados.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        // Variável privada "somente leitura" para a chave da API
        private readonly string _geminiApiKey;
        // Variável privada "somente leitura" para a fábrica de HttpClients.
        private readonly IHttpClientFactory _httpClientFactory;

        //"configuration" Usado para ler configurações do appsettings.json.
        //"httpClientFactory" Usado para criar instâncias de HttpClient.
        public AiController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            // Lê a chave "GeminiSettings:ApiKey" do arquivo appsettings.json e armazena na variável.
            _geminiApiKey = configuration["GeminiSettings:ApiKey"];
            // Armazena a factory para uso nos métodos.
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("listar-modelos")]
public async Task<IActionResult> ListarModelos()
{
    // "Guard Clause" (Cláusula de Guarda): Verifica se a chave da API foi configurada.
    // Se estiver vazia ou nula, retorna um erro 500 (Internal Server Error) e para a execução.
    if (string.IsNullOrWhiteSpace(_geminiApiKey)) return StatusCode(500, "API Key do Gemini não configurada.");
    
    // Cria um cliente HTTP usando a factory.
    var httpClient = _httpClientFactory.CreateClient();
    // Monta a URL da API do Google, injetando a chave de API.
    var apiUrl = $"https://generativelanguage.googleapis.com/v1/models?key={_geminiApiKey}";
    

    // Bloco try-catch para capturar exceções de rede ou de requisição.
    try
    {
        // Faz a chamada HTTP GET assíncrona para a API do Google.
        var response = await httpClient.GetAsync(apiUrl);
        // Lê o conteúdo da resposta (o JSON ou a mensagem de erro) como uma string.
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Verifica se a chamada NÃO foi bem-sucedida (ex: erro 400, 404, 500 da API do Google).
        if (!response.IsSuccessStatusCode)
        {
            // Retorna o mesmo status code e a mensagem de erro que a API do Google nos deu.
            return StatusCode((int)response.StatusCode, $"Erro da API do Google: {responseContent}");
        }

        // --- LÓGICA DE RETORNO BASEADA NO CLIENTE ---
        // O cliente (Desktop ou Mobile) nos diz o que ele prefere receber através do cabeçalho "Accept".

        // Pega o valor do cabeçalho "Accept" da requisição que este endpoint recebeu.
        var acceptHeader = Request.Headers["Accept"].FirstOrDefault();

        // Verifica se o cabeçalho "Accept" existe e contém "text/plain".
        if (acceptHeader != null && acceptHeader.Contains("text/plain"))
        {
            // Se o cliente (Desktop) pediu texto, devolvemos o conteúdo como texto plano.
            return Ok(responseContent); 
        }
        else
        {
            // Para todos os outros (Mobile), devolvemos JSON.
            // Além disso, retornamos o conteúdo diretamente, pois ele JÁ É UM JSON.
            return Content(responseContent, "application/json");
        }
    }
    catch (Exception ex)
    {
        // Se ocorrer um erro na nossa API (ex: falha de rede), retornamos um erro 500.
        return StatusCode(500, $"Ocorreu um erro na requisição: {ex.Message}");
    }
}
        //A sugestão de solução gerada pela IA, formatada para Desktop ou Mobile.
        [HttpPost("sugestao-solucao")]
        public async Task<IActionResult> GetSugestaoSolucao([FromBody] SugestaoRequestDto request)
        {
            // Validação 1: Garante que a chave da API está configurada.
            if (string.IsNullOrWhiteSpace(_geminiApiKey)) return StatusCode(500, "API Key do Gemini não configurada.");
            // Validação 2: Garante que o corpo da requisição não está vazio ou inválido.
            if (request == null || string.IsNullOrWhiteSpace(request.Descricao)) return BadRequest("A descrição não pode ser vazia.");

            // Variável que armazenará o texto final a ser enviado para a IA.
            string prompt;

            // Compara o perfil (ignorando maiúsculas/minúsculas) para ver se é "Tecnico".
            if (request.Perfil != null && request.Perfil.Equals("Tecnico", StringComparison.OrdinalIgnoreCase))
            {
                // Se for "Tecnico", criamos um prompt muito específico, exigindo formato técnico,
                // sem saudações e com seções claras (Análise, Passos, Ações).
                prompt = $"Sua única função é gerar guias de troubleshooting técnico para profissionais de TI. Para o problema a seguir: '{request.Descricao}', gere um guia." +
                         $"REGRAS OBRIGATÓRIAS:" +
                         $"- NÃO use saudações, despedidas ou qualquer frase conversacional." +
                         $"- Siga estritamente o formato abaixo." +
                         $"FORMATO DE SAÍDA OBRIGATÓRIO:" +
                         $"**[Título Conciso do Problema]**" +
                         $"**1. Análise Inicial e Hipóteses:**" +
                         $"[Breve análise técnica da causa provável.]" +
                         $"**2. Passos de Verificação:**" +
                         $"1. [Primeiro passo técnico detalhado. Inclua comandos se aplicável.]" +
                         $"2. [Segundo passo técnico detalhado.]" +
                         $"**3. Ações Corretivas Sugeridas:**" +
                         $"* [Primeira solução a ser tentada.]" +
                         $"* [Segunda solução a ser tentada.]";
            }
            else
            {
                // Se for "Usuário" (ou qualquer outro perfil), criamos um prompt simples,
                // exigindo linguagem fácil, sem termos técnicos e uma frase final padrão.
                prompt = $"Sua única função é criar um guia de ajuda simples para um usuário sem conhecimento técnico. O problema é: '{request.Descricao}'." +
                         $"REGRAS OBRIGATÓRIAS:" +
                         $"- NÃO use saudações, introduções ou despedidas." +
                         $"- Comece a resposta DIRETAMENTE com o primeiro passo numerado." +
                         $"- Use linguagem extremamente simples, sem termos técnicos." +
                         $"- Termine a resposta EXATAMENTE com a frase: 'Se o problema persistir, por favor, abra um chamado com nossa equipe de suporte.'";
            }

            // Cria o cliente HTTP.
            var httpClient = _httpClientFactory.CreateClient();
            // Define a URL do modelo específico do Gemini para gerar conteúdo.
            var apiUrl = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";
            // Monta o "corpo" (payload) da requisição que a API do Gemini espera.
            // É um objeto JSON complexo. Usamos um "objeto anônimo" do C# para representá-lo.
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            // Serializa (converte) o objeto C# (requestBody) em uma string JSON.
            var jsonContent = JsonSerializer.Serialize(requestBody);
            // Empacota a string JSON em um conteúdo HTTP, especificando a codificação (UTF8 para acentos)
            // e o tipo de mídia (application/json).
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // Envia a requisição POST (assíncrona) para a API do Gemini com o prompt.
                var response = await httpClient.PostAsync(apiUrl, httpContent);
                // Lê a resposta da API do Gemini.
                var responseContent = await response.Content.ReadAsStringAsync();
                // Se a API do Gemini retornar um erro, repassamos esse erro.
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, $"Erro da API do Google: {responseContent}");

                // Variável para armazenar o texto puro extraído da resposta.
                string rawText;
                // Usamos 'JsonDocument.Parse' para analisar o JSON de resposta da API do Gemini.
                // É mais eficiente do que criar classes DTO só para isso.
                using (var jsonDoc = JsonDocument.Parse(responseContent))
                {
                    // "Navegamos" pela estrutura do JSON para encontrar o texto da resposta.
                    rawText = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                }

                // Novamente, verificamos o cabeçalho "Accept" para saber quem é o cliente.
                var acceptHeader = Request.Headers["Accept"].FirstOrDefault();

                if (acceptHeader != null && acceptHeader.Contains("text/plain"))
                {
                    // Se o cliente (Desktop) pediu texto, limpamos e retornamos texto puro.
                    string cleanedText = rawText.Replace("**", "") // Remove o Markdown de negrito (ex: "**Título**" vira "Título")
                                                .Replace("\\n", Environment.NewLine) // Converte a string '\n' em quebras de linha reais
                                                .Trim(); // Remove espaços em branco desnecessários no início e no fim

                    // Retorna o texto limpo, especificando o tipo "text/plain" e a codificação UTF-8.
                    return Content(cleanedText, "text/plain", Encoding.UTF8);
                }
                else
                {
                    // Para todos os outros (Mobile), retornamos o JSON original.
                    return Ok(new { solucao = rawText });
                }
            }
            catch (Exception ex)
            {
                // Captura de erro geral (ex: falha ao analisar o JSON, falha de rede).
                return StatusCode(500, $"Ocorreu um erro na requisição: {ex.Message}");
            }
        }
    }
}