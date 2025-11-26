namespace GestaoChamados.DTOs
{
    // DTO de Atualização:
    // Usado na rota PUT /api/tickets/{id}
    // Note que NÃO permitimos alterar o "Titulo", "Descricao" ou "UsuarioId".
    // Regra de Ouro (Auditabilidade): O problema original reportado pelo usuário é SAGRADO. 
    // Ninguém pode editar a descrição depois, para evitar que um técnico mude o texto para esconder um erro.
    public class UpdateTicketRequestDto
    {
        // Obrigatório. Todo movimento no chamado envolve mudança de status.
        // Ex: "Aberto" -> "Em Atendimento" -> "Finalizado".
        public string Status { get; set; }

        // Nullable (?):
        // Por que pode ser nulo?
        // Se o técnico acabou de pegar o chamado, ele preenche esse campo com o nome dele.
        // Mas se ele quiser apenas mudar o status de "Finalizado" para "Reaberto" (por exemplo), 
        // ele pode mandar esse campo nulo para manter quem já estava.
        // O '?' indica ao compilador C# que aceitamos valor NULL aqui sem dar erro.
        public string? ProfissionalDesignado { get; set; }

        // Nullable (?):
        // Quando o técnico assume o chamado (status "Em Atendimento"), ele ainda não sabe a solução.
        // Então ele envia Solucao = null.
        // A solução só é preenchida obrigatoriamente quando o status vira "Finalizado".
        public string? Solucao { get; set; }
    }
}