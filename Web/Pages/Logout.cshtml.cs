using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace PIMIIIWeb_SQL_Login.Pages
{
    // Model da funcionalidade de Logout.
    public class LogoutModel : PageModel
    {
        // -------------------------------------------------------------------------
        // MÉTODO ON POST (Ação de Sair)
        // -------------------------------------------------------------------------
        // Por que POST e não GET?
        // Por segurança. Se o Logout fosse um GET (link simples), um hacker poderia colocar 
        // uma imagem escondida em outro site com src="/Logout" e desconectar o usuário sem ele saber (CSRF).
        // Forçando ser um POST, exigimos que o usuário clique intencionalmente num botão dentro do nosso site.
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Destrói o Cookie de Autenticação.
            // O navegador vai esquecer quem é o usuário.
            // Se ele tentar entrar em /Dashboard depois disso, o sistema vai barrar.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Redireciona para a tela de Login.
            return RedirectToPage("/Login");
        }
    }
}