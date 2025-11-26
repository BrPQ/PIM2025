namespace GestaoChamados.DTOs
{
    // DTO usado APENAS no momento de criar um chamado.
    // Perceba que a classe "Ticket" original tem uns 10 campos, mas aqui só pedimos 3.
    public class CreateTicketRequestDto
    {
        // O Básico que o usuário digita na tela.
        public string Titulo { get; set; }
        public string Descricao { get; set; }

        // Quem está abrindo o chamado.
        // (Novamente: num sistema real usaríamos o Token, mas aqui facilita os testes).
        public int UsuarioId { get; set; }

        // POR QUE NÃO TEM "STATUS"?
        // Porque todo chamado novo nasce obrigatoriamente como "Aberto".
        // Não deixamos o usuário escolher começar como "Finalizado" ou "Em Andamento".
        // Isso é Regra de Negócio imposta pelo Back-end.

        // POR QUE NÃO TEM "DATA"?
        // Porque usamos o horário do servidor (DateTime.Now) para evitar fraudes
        // (ex: usuário mentir que abriu o chamado ontem para culpar o SLA).
    }
}