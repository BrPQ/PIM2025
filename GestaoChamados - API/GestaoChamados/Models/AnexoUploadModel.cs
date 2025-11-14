using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestaoChamados.Models
{
    public class AnexoUploadModel
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; }

        public string TipoAnexo { get; set; } = "Usuario";
    }
}