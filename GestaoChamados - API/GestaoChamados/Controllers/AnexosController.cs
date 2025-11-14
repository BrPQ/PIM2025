using GestaoChamados.Data;
using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoChamados.Controllers
{
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class AnexosController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public AnexosController(ApiDbContext context)
        {
            _context = context;
        }

        
        [HttpPost("upload/{ticketId}")]
        public async Task<IActionResult> UploadAnexo(int ticketId, [FromForm] AnexoUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado.");
            }

            
            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            
            var novoAnexo = new Anexo
            {
                TicketId = ticketId,
                NomeArquivo = model.File.FileName,      
                CaminhoArquivo = uniqueFileName,        
                DataUpload = DateTime.Now,
                TipoAnexo = model.TipoAnexo             
            };

            _context.Anexos.Add(novoAnexo);
            await _context.SaveChangesAsync();

            return Ok(novoAnexo);
        }

        
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetAnexosPorTicket(int ticketId, [FromQuery] string tipoAnexo)
        {
            
            var query = _context.Anexos.Where(a => a.TicketId == ticketId);

            
            if (!string.IsNullOrEmpty(tipoAnexo))
            {
                query = query.Where(a => a.TipoAnexo == tipoAnexo);
            }

            var anexos = await query.ToListAsync();
            return Ok(anexos);
        }

        
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

            
            return File(memory, "application/octet-stream", anexo.NomeArquivo);
        }
    }
}