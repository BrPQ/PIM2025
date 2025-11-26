namespace GestaoChamados.DTOs
{
    // Request DTO: Define estritamente o que é necessário para criar uma mensagem.
    // O Front-end NÃO manda o ID da mensagem (o banco cria automático) nem a Data (o servidor define "Agora").
    // Ele manda apenas ONDE, QUEM e O QUÊ.
    public class CreateMensagemRequestDto
    {
        // Chave Estrangeira: Diz a qual chamado essa mensagem pertence.
        // Sem isso, a mensagem ficaria "perdida" no banco sem dono.
        public int TicketId { get; set; }

        // Chave Estrangeira: Diz quem escreveu.
        // Importante para saber se a bolinha do chat vai ficar na esquerda (outro) ou direita (você).
        public int UsuarioId { get; set; }

        // O conteúdo propriamente dito.
        // É importante que o Front-end valide para não enviar string vazia ou nula.
        public string Conteudo { get; set; }
    }
}