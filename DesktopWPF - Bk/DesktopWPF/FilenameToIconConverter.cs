using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DesktopWPF // Use o seu namespace
{
    public class FilenameToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // O valor que recebemos é o nome do arquivo (ex: "foto.png")
            if (value is string filename)
            {
                // Pegamos a extensão do arquivo (ex: ".png")
                string extension = Path.GetExtension(filename).ToLower();

                // Decidimos qual ícone retornar com base na extensão
                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                        // Se for uma imagem, retorna o caminho do ícone de imagem
                        return new BitmapImage(new Uri("Images/foto.png", UriKind.Relative));

                    case ".pdf":
                    case ".doc":
                    case ".docx":
                    case ".txt":
                    case ".xls":
                    case ".xlsx":
                        // Se for um documento, retorna o caminho do ícone de documento
                        return new BitmapImage(new Uri("Images/documento.png", UriKind.Relative));

                    default:
                        // Para qualquer outra coisa, retorna um ícone genérico
                        return new BitmapImage(new Uri("Images/generico.png", UriKind.Relative));
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Não precisamos disso
        }
    }
}