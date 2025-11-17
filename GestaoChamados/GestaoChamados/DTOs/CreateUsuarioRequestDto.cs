namespace GestaoChamados.DTOs
{
    public class CreateUsuarioRequestDto
    {
        public string NomeUsuario { get; set; }
        public string Matricula { get; set; }
        public string Senha { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
    }
}