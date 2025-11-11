namespace GestaoChamados.DTOs
{
    public class CreateUsuarioRequestDto
    {
        // CORREÇÃO: Mudamos 'Nome' para 'NomeUsuario'
        public string NomeUsuario { get; set; }
        public string Matricula { get; set; }
        public string Senha { get; set; }
        // CORREÇÃO: Mudamos 'Perfil' para 'Role'
        public string Role { get; set; }
        public string Email { get; set; }
    }
}