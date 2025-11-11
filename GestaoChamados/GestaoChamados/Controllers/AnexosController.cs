using GestaoChamados.Data;
using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoChamados.Controllers
{
    [Authorize] // Garante que apenas usuários logados podem acessar estes endpoints
    [Route("api/[controller]")]
    [ApiController]
    public class AnexosController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public AnexosController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Faz o upload de um anexo e o associa a um ticket.
        /// Este método usa um modelo [FromForm] para ser compatível tanto com o
        /// app Desktop (que envia o TipoAnexo) quanto com o app Mobile (que não envia).
        /// </summary>
        [HttpPost("upload/{ticketId}")]
        public async Task<IActionResult> UploadAnexo(int ticketId, [FromForm] AnexoUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado.");
            }

            // Garante que o diretório de uploads exista
            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            // Gera um nome de arquivo único para evitar conflitos
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            // Salva o arquivo fisicamente no servidor
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            // Cria o registro do anexo no banco de dados
            var novoAnexo = new Anexo
            {
                TicketId = ticketId,
                NomeArquivo = model.File.FileName,      // Nome original do arquivo para exibição
                CaminhoArquivo = uniqueFileName,        // Nome único para armazenamento
                DataUpload = DateTime.Now,
                TipoAnexo = model.TipoAnexo             // Usa o tipo recebido ou o padrão "Usuario" do modelo
            };

            _context.Anexos.Add(novoAnexo);
            await _context.SaveChangesAsync();

            return Ok(novoAnexo);
        }

        /// <summary>
        /// Retorna uma lista de anexos para um ticket específico.
        /// Permite filtrar por tipo de anexo (ex: "Usuario" ou "Tecnico").
        /// </summary>
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetAnexosPorTicket(int ticketId, [FromQuery] string tipoAnexo)
        {
            // Inicia a consulta buscando todos os anexos do ticket
            var query = _context.Anexos.Where(a => a.TicketId == ticketId);

            // Se o parâmetro 'tipoAnexo' foi passado na URL, aplica o filtro
            if (!string.IsNullOrEmpty(tipoAnexo))
            {
                query = query.Where(a => a.TipoAnexo == tipoAnexo);
            }

            var anexos = await query.ToListAsync();
            return Ok(anexos);
        }

        /// <summary>
        /// Faz o download do conteúdo de um anexo específico.
        /// </summary>
        [HttpGet("download/{anexoId}")]
        public async Task<IActionResult> DownloadAnexo(int anexoId)
        {
            var anexo = await _context.Anexos.FindAsync(anexoId);
            if (anexo == null)
            {
                return NotFound("Anexo não encontrado.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", anexo.CaminhoArquivo);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Arquivo não encontrado no servidor.");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // Retorna o arquivo para o cliente, que pode então salvá-lo ou abri-lo.
            return File(memory, "application/octet-stream", anexo.NomeArquivo);
        }
    }
}