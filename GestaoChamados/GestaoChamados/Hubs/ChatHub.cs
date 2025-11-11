using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

// ADICIONE O NAMESPACE CORRESPONDENTE À PASTA
namespace GestaoChamados.Hubs
{
    public class ChatHub : Hub
    {
        // Método para o cliente (Mobile/Desktop) chamar quando entrar na tela de chat
        public async Task JoinChatGroup(string groupName)
        {
            // Adiciona o usuário atual ao grupo especificado
            // Ex: groupName pode ser "ticket-123"
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Método para o cliente chamar quando sair da tela de chat
        public async Task LeaveChatGroup(string groupName)
        {
            // Remove o usuário atual do grupo
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // Este Hub não precisa de um método "EnviarMensagem" público,
        // porque quem vai disparar a mensagem será o Controller (servidor),
        // e não um cliente diretamente para outros clientes.
    }
}