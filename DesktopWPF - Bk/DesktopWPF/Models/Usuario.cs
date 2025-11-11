using System;

namespace DesktopWPF.Models // <-- Usando o nome correto do seu projeto
{
    public class Usuario
    {
        // Construtor vazio, essencial para a tela de login
        public Usuario() { }

        // Propriedades que espelham a tabela do banco
        public int Id { get; set; }
        public string NomeUsuario { get; set; }
        public string Matricula { get; set; }
        public string Role { get; set; }
        // Adicione outras propriedades se a sua tela 'Main' precisar
    }
}