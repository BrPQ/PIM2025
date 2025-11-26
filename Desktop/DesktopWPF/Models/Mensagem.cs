using System;

namespace DesktopWPF.Models
{
    // MODELO DE UI (User Interface):
    // Esta classe representa uma mensagem NA TELA do computador.
    // Ela tem campos extras que não existem necessariamente no banco de dados.
    public class Mensagem
    {
        public int MensagemId { get; set; }
        public int TicketId { get; set; }

        // Identificador de quem mandou.
        public int UsuarioId { get; set; }

        // Nome para exibir no topo do balão.
        public string NomeUsuario { get; set; }

        // O texto da conversa.
        public string Conteudo { get; set; }

        // Data para mostrar "10:30" no rodapé do balão.
        public DateTime DataEnvio { get; set; }

        // *** A LÓGICA DO CHAT (Layout) ***
        // Esta propriedade é boolean (Verdadeiro/Falso).
        // Ela NÃO vem do banco de dados (geralmente calculamos ela no Front-end).
        // Lógica: Se (UsuarioId == MeuIdLogado) -> IsSentByMe = true.
        // O XAML do WPF usa isso para:
        // 1. Alinhar à Direita (True) ou Esquerda (False).
        // 2. Mudar a cor de fundo (Azul vs Cinza).
        public bool IsSentByMe { get; set; }

        // Campos auxiliares de exibição
        public string Author { get; set; } // Pode ser igual ao NomeUsuario ou formatado.

        // Role (Cargo):
        // Usado para destacar visualmente se quem falou foi um "Técnico" ou o "Usuário".
        // Ex: Se AuthorRole == "Tecnico", o nome pode aparecer em Vermelho ou com um ícone de chave inglesa.
        public string AuthorRole { get; set; }
    }
}