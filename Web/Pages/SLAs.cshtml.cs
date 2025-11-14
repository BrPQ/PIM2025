using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace PIMIIIWeb_SQL_Login.Pages
{
    public class SLAsModel : PageModel
    {
        public List<SlaVm> Slas { get; set; } = new();

        public void OnGet()
        {
            Slas = new()
            {
                new("SLA Incidente Crítico", 30, "Infraestrutura", true),
                new("SLA Incidente Alto", 60, "Sistemas", true),
                new("SLA Atendimento Geral", 180, "Service Desk", true),
            };
        }

        public record SlaVm(string Nome, int TempoAlvoMin, string Categoria, bool Ativo);
    }
}
