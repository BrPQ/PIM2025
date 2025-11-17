using System;

namespace DesktopWPF.Models 
{
    public class Usuario
    {
        
        public Usuario() { }

        
        public int Id { get; set; }
        public string NomeUsuario { get; set; }
        public string Matricula { get; set; }
        public string Role { get; set; }
        
    }
}