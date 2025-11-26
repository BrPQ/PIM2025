using GestaoChamados.Data;
using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoChamados.Controllers
{
    // [Authorize]: Garante que apenas usuários logados com um Token JWT válido possam acessar essas rotas.
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class AnexosController : ControllerBase
    {
        // _context: Variável que representa a conexão com o Banco de Dados
        private readonly ApiDbContext _context;
        // Construtor: Utiliza Injeção de Dependência para receber o contexto do banco pronto para uso.
        public AnexosController(ApiDbContext context)
        {
            _context = context;
        }

        // UPLOAD
        [HttpPost("upload/{ticketId}")]
        public async Task<IActionResult> UploadAnexo(int ticketId, [FromForm] AnexoUploadModel model)
        {
            //verifica se o arquivo veio vazio.
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado.");
            }

            // Define o caminho físico onde os arquivos serão salvos no servidor (pasta "Uploads").
            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            // Se a pasta não existir, o código cria ela automaticamente.
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            // Guid.NewGuid(): Gera um código único universal para garantir que o nome do arquivo seja exclusivo.
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
            // Caminho completo final (Pasta + Nome Único).
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            // FileStream: Abre um fluxo de dados para "escrever" o arquivo fisicamente no disco do servidor.
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                // Copia os bits do arquivo recebido para o servidor de forma assíncrona
                await model.File.CopyToAsync(stream);
            }

            // Note: O arquivo físico fica na pasta, no banco salvamos apenas o CAMINHO
            var novoAnexo = new Anexo
            {
                TicketId = ticketId,
                NomeArquivo = model.File.FileName, // Nome original     
                CaminhoArquivo = uniqueFileName,   // Nome único     
                DataUpload = DateTime.Now,
                TipoAnexo = model.TipoAnexo             
            };
            // Adiciona ao contexto e salva no SQL Server.
            _context.Anexos.Add(novoAnexo);
            await _context.SaveChangesAsync();

            return Ok(novoAnexo);
        }

        // LISTAR ANEXOS DE UM TICKET
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetAnexosPorTicket(int ticketId, [FromQuery] string tipoAnexo)
        {
            // Cria uma consulta LINQ base filtrando pelo ID do Ticket.
            var query = _context.Anexos.Where(a => a.TicketId == ticketId);

            // Filtro dinâmico: Se o usuário passou um "tipo" na URL, filtra por ele também.
            if (!string.IsNullOrEmpty(tipoAnexo))
            {
                query = query.Where(a => a.TipoAnexo == tipoAnexo);
            }
            // Executa a consulta no banco de dados (ToListAsync).
            var anexos = await query.ToListAsync();
            return Ok(anexos);
        }

        // DOWNLOAD
        [HttpGet("download/{anexoId}")]
        public async Task<IActionResult> DownloadAnexo(int anexoId)
        {
            // 1. Busca o registro no banco de dados pelo ID do anexo.
            var anexo = await _context.Anexos.FindAsync(anexoId);
            if (anexo == null)
            {
                return NotFound("Anexo não encontrado.");
            }
            // 2. Reconstrói o caminho físico onde o arquivo deveria estar.
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", anexo.CaminhoArquivo);
            // 3. Verifica se o arquivo físico realmente existe no servidor.
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Arquivo não encontrado no servidor.");
            }
            // 4. Prepara o arquivo para envio.
            //Carrega o arquivo na memória RAM temporariamente para enviar
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            // Reseta a posição do ponteiro de leitura para o início do arquivo na memória.
            memory.Position = 0;

            // Retorna o arquivo. "application/octet-stream" força o app a baixar o arquivo
            // ao invés de tentar abrir (como faria com um PDF ou JPG).
            return File(memory, "application/octet-stream", anexo.NomeArquivo);
        }
    }
}