using System;

namespace DesktopWPF.Models
{
    // MODELO DE CLIENTE (Front-end):
    // Esta classe representa o usuário logado ou um usuário listado numa grid.
    public class Usuario
    {
        // Construtor Vazio:
        // Necessário para o 'Newtonsoft.Json' conseguir criar a instância da classe
        // antes de começar a preencher as propriedades.
        public Usuario() { }

        // Identificador único.
        public int Id { get; set; }

        // O nome que aparece no canto superior direito ("Olá, Breno").
        public string NomeUsuario { get; set; }

        // O Login (Matrícula).
        public string Matricula { get; set; }

        // O Perfil de Acesso (Admin, Tecnico, etc).
        // Essencial para o WPF decidir quais botões mostrar e quais esconder.
        public string Role { get; set; }

        // *** ONDE ESTÁ A SENHA? ***
        // EXATAMENTE! Não tem senha aqui.
        // Diferente do "LoginRequestDto" (que envia a senha pra entrar),
        // este objeto "Usuario" é o que fica na memória do computador durante o uso.
        // Por segurança, a API nunca devolve a senha (nem o hash) para o Front-end.
        // Se um vírus vasculhar a memória RAM desse programa, não vai achar a senha do usuário.
    }
}