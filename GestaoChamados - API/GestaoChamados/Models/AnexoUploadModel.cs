using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestaoChamados.Models
{
    // Este modelo não representa uma tabela do banco.
    // Ele serve exclusivamente para receber dados do formulário de upload (Multipart/Form-Data).
    public class AnexoUploadModel
    {
        // [FromForm(Name = "file")]: 
        // Isso é o "Model Binding" (Amarração).
        // Diz ao ASP.NET: "Procure no formulário HTML/Postman um campo chamado 'file' 
        // e coloque o conteúdo dele dentro desta propriedade."
        [FromForm(Name = "file")]
        // IFormFile: É a interface padrão do ASP.NET Core para lidar com upload.
        // Ela nos dá acesso ao fluxo de bytes (Stream), ao nome original do arquivo e ao tamanho dele.
        // Não usamos "byte[]" direto aqui porque o IFormFile é mais eficiente para arquivos grandes.
        public IFormFile File { get; set; }

        // Um campo extra para categorizar o arquivo.
        // O valor padrão é "Usuario" (caso o Front-end não envie nada).
        // Isso é útil se, no futuro, vocês quiserem diferenciar uploads feitos pelo "Sistema" (logs) 
        // dos uploads feitos pelo "Usuario" (prints de erro).
        public string TipoAnexo { get; set; } = "Usuario";
    }
}