using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PIMIIIWeb_SQL_Login.Pages
{
    // PageModel: A classe que controla a lógica da página "LGPD.cshtml".
    public class LGPDModel : PageModel
    {
        // Método OnGet:
        // Executado automaticamente quando o usuário clica no link "Política de Privacidade" ou acessa /LGPD.
        //
        // POR QUE ESTÁ VAZIO?
        // Porque esta é uma página ESTÁTICA.
        // O texto da lei ("Nós coletamos seus dados para...") está escrito direto no HTML (.cshtml).
        // Não precisamos ir no Banco de Dados nem na API para mostrar um texto fixo.
        // O ASP.NET apenas renderiza a View e pronto.
        public void OnGet()
        {
            // Nenhuma lógica necessária. Apenas exibe o HTML.
        }
    }
}