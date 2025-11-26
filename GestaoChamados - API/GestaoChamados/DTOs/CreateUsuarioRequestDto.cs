namespace GestaoChamados.DTOs
{
    // DTO de Entrada: Usado exclusivamente na rota POST /api/usuarios
    public class CreateUsuarioRequestDto
    {
        // Dados cadastrais básicos
        public string NomeUsuario { get; set; }

        // Identificador Único de Login:
        // O sistema usa a Matrícula como login, não o e-mail.
        // Isso é comum em sistemas corporativos/universitários.
        public string Matricula { get; set; }

        // *** PONTO DE ATENÇÃO MÁXIMA ***
        // Esta é a "Senha Pura" (Raw Password), ex: "Mudar123!".
        // Ela chega aqui vinda do Front-end.
        // O Controller vai pegar essa string e passar imediatamente pelo BCrypt.
        // A classe "Usuario" (banco de dados) NÃO tem esse campo, ela tem o "SenhaHash".
        // Isso garante que a senha pura nunca seja salva no banco.
        public string Senha { get; set; }

        // Perfil de Acesso:
        // O Front-end deve enviar exatamente as strings esperadas: "Admin", "Tecnico" ou "Funcionario".
        // Se enviar "Administrador" (por extenso), a validação de Authorize vai falhar depois.
        public string Role { get; set; }

        // Contato (opcional ou obrigatório dependendo da regra de negócio)
        public string Email { get; set; }
    }
}