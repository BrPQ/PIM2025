using System;

namespace SeuProjetoWPF.Models
{
    // PAYLOAD (Carga Útil):
    // Este objeto não é salvo no banco do Desktop.
    // Ele é o pacote de dados que chega "voando" pelo SignalR quando alguém envia uma mensagem.
    // O servidor "empurra" (Push) este objeto para o cliente.
    public class MensagemPayload
    {
        public int MensagemId { get; set; }

        // Roteamento:
        // O WPF usa isso para saber em qual janela de chat deve desenhar a mensagem.
        // Se a janela do Ticket 105 estiver aberta e chegar um payload do Ticket 200, o sistema ignora ou mostra notificação.
        public int TicketId { get; set; }

        // Lógica de UI:
        // Usado para saber se a mensagem é Minha (Direita) ou do Outro (Esquerda).
        public int UsuarioId { get; set; }

        // Exibição Imediata:
        // O servidor já manda o nome pronto. Se mandasse só o ID, o Desktop teria que ir no banco buscar o nome,
        // o que causaria atraso (lag) na mensagem aparecendo.
        public string NomeUsuario { get; set; }

        // Estilização:
        // "Tecnico" fica vermelho, "Usuario" fica azul (exemplo).
        public string AuthorRole { get; set; }

        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}