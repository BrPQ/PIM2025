using System;

namespace GestaoChamados.DTOs
{
    public class ConversaResumoDto
    {
        public int TicketId { get; set; }
        public string TituloTicket { get; set; }
        public string Status { get; set; }
        public string NomeProfissional { get; set; }
        public string UltimaMensagem { get; set; }
        public DateTime? DataUltimaMensagem { get; set; }
    }
}