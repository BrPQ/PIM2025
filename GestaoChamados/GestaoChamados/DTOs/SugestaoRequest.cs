namespace GestaoChamados.DTOs // Ou só GestaoChamados.DTOs
{
    public class SugestaoRequestDto
    {
        public string Descricao { get; set; }
        public string Perfil { get; set; } // "Usuario" ou "Tecnico"
    }
}