using System;

namespace GestaoChamados.DTOs
{
    // DTO: Data Transfer Object (Objeto de Transferência de Dados).
    // Por que criar essa classe e não usar a classe "Ticket" direto?
    // 1. Performance: A classe Ticket tem muitos dados (descrição longa, logs, chaves estrangeiras) que não precisamos na lista de resumo.
    // 2. Agrupamento: Aqui misturamos dados do Ticket (Título) com dados da Mensagem (Última mensagem enviada).
    public class ConversaResumoDto
    {
        // ID para saber qual chat abrir quando o usuário clicar na conversa.
        public int TicketId { get; set; }

        // Contexto: Sobre o que é esse chat? (Ex: "Impressora quebrada")
        public string TituloTicket { get; set; }

        // Visual: Para mostrar ícones ou cores (ex: "Aceito" fica verde, "Finalizado" fica cinza).
        public string Status { get; set; }

        // Informação visual: Com quem estou falando?
        public string NomeProfissional { get; set; }

        // UX (Experiência do Usuário): 
        // Mostra o "preview" da última mensagem (ex: "Ok, estou indo aí...")
        // Isso evita que o usuário tenha que abrir o chat só para ver se responderam.
        public string UltimaMensagem { get; set; }

        // Ordenação: Usado para ordenar a lista (as conversas mais recentes ficam no topo).
        // DateTime? (Nullable): O ponto de interrogação é IMPORTANTE.
        // Significa que a data pode ser NULA. Por quê?
        // Porque um ticket pode ter sido criado ("Aceito"), mas ninguém mandou mensagem ainda. 
        // Se não fosse nullable, o código quebraria ao tentar ler uma data que não existe.
        public DateTime? DataUltimaMensagem { get; set; }
    }
}