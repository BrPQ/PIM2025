using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;


namespace GestaoChamados.Hubs
{
    public class TicketHub : Hub
    {
        // Este método pode ser chamado pelos clientes, se necessário (ex: um chat)
        public async Task EnviarMensagem(string usuario, string mensagem)
        {
            // Este comando envia a mensagem para TODOS os clientes conectados
            await Clients.All.SendAsync("ReceberNovaMensagem", usuario, mensagem);
        }
    }
}