namespace DesktopWPF.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public Usuario Usuario { get; set; } // <-- Adicionamos o objeto do usuário aqui
    }
}