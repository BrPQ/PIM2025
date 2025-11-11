using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace DesktopWPF.Converters // <-- Ou InfinitiPro.Converters
{
    public class FilenameToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // O 'value' que recebemos é o nome do arquivo (ex: "relatorio.pdf")
            if (value is string filename && !string.IsNullOrEmpty(filename))
            {
                // Pegamos a extensão do arquivo em letras minúsculas (ex: ".pdf")
                string extension = Path.GetExtension(filename).ToLowerInvariant();

                // Decidimos qual ícone retornar com base na extensão
                switch (extension)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                    case ".gif":
                        // Retorna o caminho para o ícone de imagem
                        return "/Images/foto.png";

                    case ".pdf":
                    case ".txt":
                    case ".docx":
                        return "/Images/documento.png";

                    // Adicione outros casos aqui (ex: ".docx", ".xlsx", etc.)

                    default:
                        // Se não for nenhum dos acima, retorna o ícone genérico
                        return "/Images/generico.png";
                }
            }

            // Se algo der errado, retorna o ícone genérico
            return "/Images/generico.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}