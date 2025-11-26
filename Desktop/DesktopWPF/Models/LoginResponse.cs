namespace DesktopWPF.Models
{
    // Classe de Resposta (Response Model):
    // Serve exclusivamente para mapear o JSON que volta da rota /api/auth/login.
    // Estrutura do JSON esperado:
    // {
    //    "token": "eyJhbGciOiJIUz...",
    //    "usuario": { "id": 1, "nome": "Breno", ... }
    // }
    public class LoginResponse
    {
        // O "Crachá Digital" (JWT).
        // O ApiService vai pegar essa string e guardar na memória para usar no Header Authorization.
        public string Token { get; set; }

        // Os dados do usuário logado.
        // O Desktop usa isso para:
        // 1. Mostrar o nome no canto da tela.
        // 2. Esconder botões (ex: Se Role != "Admin", esconde o botão "Cadastrar Usuário").
        public Usuario Usuario { get; set; }
    }
}