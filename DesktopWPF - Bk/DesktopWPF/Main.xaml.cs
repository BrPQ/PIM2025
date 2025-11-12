using DesktopWPF.Models;
using DesktopWPF.Services;
using Microsoft.AspNetCore.SignalR.Client; // Using do SignalR
using Microsoft.Win32;
using SeuProjetoWPF.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopWPF
{
    public partial class Main : Window
    {
        #region Variáveis Globais
        private readonly ApiService _apiService;
        private readonly Usuario _usuarioLogado;
        private HubConnection _hubConnection;       // Conexão principal (para Tickets)
        private HubConnection _chatHubConnection;   // Conexão do Chat (dinâmica)
        private string _currentChatGroup = "";      // Para saber de qual grupo de chat sair
        private List<Ticket> _allTickets;
        private TicketDetalheDto _ticketDetalhadoAtual;
        private Ticket _chatSelecionadoAtual;
        private List<string> _anexosParaUpload = new List<string>();
        private Button _lastSelectedContactButton = null;
        #endregion

        public Main(Usuario usuarioLogado, ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            _usuarioLogado = usuarioLogado;

            SetupInitialState();

            // Adiciona um evento para desconectar os Hubs ao fechar a janela
            this.Closing += Main_Closing;
        }

        #region Métodos de Inicialização e UI Geral
        private async void SetupInitialState()
        {
            txtNomeUsuario.Text = _usuarioLogado.NomeUsuario;

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();

            // Configuração de visibilidade inicial
            TicketsView.Visibility = Visibility.Visible;
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            ChatView.Visibility = Visibility.Collapsed;
            ConfirmCancelView.Visibility = Visibility.Collapsed;
            NotificationToast.Visibility = Visibility.Collapsed;
            IaSuggestionView.Visibility = Visibility.Collapsed;

            await LoadTicketsFromApiAsync();

            // Inicia a conexão principal (Tickets)
            await IniciarConexaoSignalR();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            txtClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close(); // O evento Main_Closing será disparado aqui
        }
        #endregion

        #region Lógica de SignalR (Comunicação em Tempo Real)

        /// <summary>
        /// (MODIFICADO) Inicia a conexão com o HUB DE TICKETS, agora enviando o Token.
        /// </summary>
        private async Task IniciarConexaoSignalR()
        {
            // Agora isso funciona, pois criamos a propriedade "AuthToken" no ApiService
            string token = _apiService.AuthToken;
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Token de autenticação não encontrado. A conexão em tempo real falhará.");
                return;
            }

            // Agora isso funciona, pois criamos a propriedade "BaseUrl" no ApiService
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_apiService.BaseUrl + "/ticketHub", options =>
                {
                    // Envia o token "Bearer ..." em cada requisição do SignalR
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect()
                .Build();

            // --- Define o que fazer quando a API envia uma mensagem (LISTENERS DE TICKET) ---

            _hubConnection.On<Ticket>("ReceberAtualizacaoTicket", (ticketAtualizado) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    await ShowNotificationToast($"Ticket #{ticketAtualizado.Id} foi atualizado!");
                    await LoadTicketsFromApiAsync();
                });
            });

            _hubConnection.On<Ticket>("ReceberNovoTicket", (novoTicket) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    await ShowNotificationToast($"Novo ticket #{novoTicket.Id} criado!");
                    await LoadTicketsFromApiAsync();
                });
            });

            _hubConnection.On<int>("ReceberTicketDeletado", (ticketId) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    await ShowNotificationToast($"O ticket #{ticketId} foi excluído!");
                    await LoadTicketsFromApiAsync();
                });
            });

            // Inicia a conexão
            try
            {
                await _hubConnection.StartAsync();
                Debug.WriteLine("Conexão SignalR (Tickets) estabelecida com sucesso!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível conectar ao serviço de tempo real (Tickets): {ex.Message}");
            }
        }

        /// <summary>
        /// (MODIFICADO) Garante que AMBAS as conexões sejam fechadas de forma limpa.
        /// </summary>
        private async void Main_Closing(object sender, CancelEventArgs e)
        {
            // Desconecta do Hub de Tickets
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
            }

            // Desconecta também do Hub de Chat, se estiver conectado
            await StopChatConnectionAsync();
        }

        #endregion

        #region Lógica de Tickets

        private async Task LoadTicketsFromApiAsync()
        {
            try
            {
                _allTickets = await _apiService.GetTicketsAsync();
                // Mantém o filtro que estava ativo anteriormente
                Button activeFilterButton = FindActiveFilterButton();
                FilterButton_Click(activeFilterButton, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tickets da API: {ex.Message}");
            }
        }

        private Button FindActiveFilterButton()
        {
            // Lógica simples para encontrar qual botão de filtro está ativo pela cor
            if (btnAceito.Background is SolidColorBrush b1 && b1.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnAceito;
            if (btnFinalizado.Background is SolidColorBrush b2 && b2.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnFinalizado;
            if (btnCancelado.Background is SolidColorBrush b3 && b3.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnCancelado;
            return btnAberto; // Se nenhum estiver ativo, retorna o "Aberto" como padrão
        }


        private async void TicketButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var ticketContext = button?.DataContext as Ticket;
            if (ticketContext == null) return;

            try
            {
                // 1. Faz UMA ÚNICA chamada que já retorna TUDO (Ticket + Usuário)
                _ticketDetalhadoAtual = await _apiService.GetTicketByIdAsync(ticketContext.Id);

                // 2. A chamada GetUserByIdAsync() FOI REMOVIDA (não é mais necessária)

                // 3. Preenche a UI com os dados DIRETOS do DTO
                txtDetailTicketId.Text = $"TICKET#{_ticketDetalhadoAtual.Id}";
                txtDetailDescricao.Text = _ticketDetalhadoAtual.Descricao;

                // --- AQUI ESTÁ A CORREÇÃO DO BUG ---
                txtDetailUsuario.Text = _ticketDetalhadoAtual.NomeUsuario;

                // Supondo que o TextBlock do Setor no seu XAML se chama 'txtDetailSetor':
                txtDetailSetor.Text = _ticketDetalhadoAtual.PerfilUsuario; // Usando o novo campo!
                                                                           // ------------------------------------

                var anexos = await _apiService.GetAnexosByTicketIdAsync(_ticketDetalhadoAtual.Id, "Usuario");
                ItemsControlTicketAnexos.ItemsSource = anexos;

                if (_ticketDetalhadoAtual.Status.Equals("Aberto", StringComparison.OrdinalIgnoreCase))
                {
                    txtDetailProfissional.Text = string.Empty;
                }
                else
                {
                    txtDetailProfissional.Text = _ticketDetalhadoAtual.ProfissionalDesignado ?? "Nenhum";
                }

                UpdateDetailViewButtons(_ticketDetalhadoAtual.Status);

                TicketsView.Visibility = Visibility.Collapsed;
                TicketDetailView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar detalhes do ticket: {ex.Message}");
            }
        }

        private async void btnAceitar_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null) return;

            var updateData = new
            {
                status = "Aceito",
                profissionalDesignado = _usuarioLogado.NomeUsuario,
                solucao = _ticketDetalhadoAtual.Solucao
            };

            try
            {
                // A notificação de sucesso agora virá automaticamente pelo SignalR para todos,
                // então não precisamos necessariamente recarregar a tela aqui, mas mantemos para feedback imediato.
                bool success = await _apiService.UpdateTicketAsync(_ticketDetalhadoAtual.Id, updateData);
                if (success)
                {
                    await ShowNotificationToast("Ticket aceito com sucesso!");
                    TicketDetailView.Visibility = Visibility.Collapsed;
                    TicketsView.Visibility = Visibility.Visible;
                }
                else
                {
                    await ShowNotificationToast("Falha ao atualizar o ticket.", isError: true);
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationToast($"Erro: {ex.Message}", isError: true);
            }
        }

        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            txtSolucao.Text = string.Empty;
            _anexosParaUpload.Clear();
            ItemsControlAnexos.ItemsSource = null;

            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Visible;
        }

        private async void btnConfirmarFinalizacao_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null || string.IsNullOrWhiteSpace(txtSolucao.Text))
            {
                await ShowNotificationToast("Por favor, preencha a descrição da solução.", isError: true);
                return;
            }

            try
            {
                foreach (var filePath in _anexosParaUpload)
                {
                    await _apiService.UploadAnexoAsync(_ticketDetalhadoAtual.Id, filePath, "Tecnico");
                }

                var updateData = new
                {
                    status = "Finalizado",
                    solucao = txtSolucao.Text,
                    profissionalDesignado = _ticketDetalhadoAtual.ProfissionalDesignado
                };

                bool success = await _apiService.UpdateTicketAsync(_ticketDetalhadoAtual.Id, updateData);

                if (success)
                {
                    await ShowNotificationToast("Ticket finalizado com sucesso!");
                    FinalizarTicketView.Visibility = Visibility.Collapsed;
                    TicketsView.Visibility = Visibility.Visible;
                }
                else
                {
                    await ShowNotificationToast("Falha ao finalizar o ticket.", isError: true);
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationToast($"Erro: {ex.Message}", isError: true);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ConfirmCancelView.Visibility = Visibility.Visible;
        }

        private async void btnConfirmCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null) return;
            ConfirmCancelView.Visibility = Visibility.Collapsed;

            var updateData = new
            {
                status = "Cancelado",
                solucao = "Ticket cancelado pelo técnico.",
                profissionalDesignado = _ticketDetalhadoAtual.ProfissionalDesignado
            };

            try
            {
                bool success = await _apiService.UpdateTicketAsync(_ticketDetalhadoAtual.Id, updateData);
                if (success)
                {
                    await ShowNotificationToast("Ticket cancelado com sucesso!");
                    TicketDetailView.Visibility = Visibility.Collapsed;
                    TicketsView.Visibility = Visibility.Visible;
                }
                else
                {
                    await ShowNotificationToast("Falha ao cancelar o ticket.", isError: true);
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationToast($"Erro: {ex.Message}", isError: true);
            }
        }

        private async void btnInformacoes_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null) return;

            try
            {
                var anexos = await _apiService.GetAnexosByTicketIdAsync(_ticketDetalhadoAtual.Id, "Tecnico");
                txtSolucaoDetalhes.Text = _ticketDetalhadoAtual.Solucao;
                ItemsControlSolucaoAnexos.ItemsSource = anexos;

                TicketDetailView.Visibility = Visibility.Collapsed;
                SolucaoTicketView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar informações do ticket: {ex.Message}");
            }
        }

        private async void btnSugestaoIA_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null || string.IsNullOrWhiteSpace(_ticketDetalhadoAtual.Descricao))
            {
                await ShowNotificationToast("Não há descrição do problema para analisar.", isError: true);
                return;
            }

            try
            {
                var originalContent = btnSugestaoIA.Content;
                btnSugestaoIA.Content = "Pensando...";
                btnSugestaoIA.IsEnabled = false;

                string sugestao = await _apiService.GetSugestaoIaAsync(_ticketDetalhadoAtual.Descricao, "Tecnico");

                IaSuggestionText.Text = sugestao;
                IaSuggestionView.Visibility = Visibility.Visible;

                btnSugestaoIA.Content = originalContent;
                btnSugestaoIA.IsEnabled = true;
            }
            catch (Exception ex)
            {
                btnSugestaoIA.Content = "Sugerir Solução (IA)";
                btnSugestaoIA.IsEnabled = true;
                await ShowNotificationToast($"Erro ao consultar a IA: {ex.Message}", isError: true);
            }
        }

        private void btnIaOk_Click(object sender, RoutedEventArgs e)
        {
            IaSuggestionView.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Lógica de Chat

        private async void LoadChatContacts()
        {
            if (_allTickets == null) return;

            var ticketsAceitos = _allTickets
                .Where(ticket =>
                    ticket.Status.Equals("Aceito", StringComparison.OrdinalIgnoreCase) &&
                    ticket.ProfissionalDesignado.Equals(_usuarioLogado.NomeUsuario, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            var contacts = new List<ChatContact>();

            foreach (var ticket in ticketsAceitos)
            {
                var usuarioDoTicket = await _apiService.GetUserByIdAsync(ticket.UsuarioId);

                contacts.Add(new ChatContact
                {
                    OriginalTicket = ticket,
                    DisplayName = usuarioDoTicket?.NomeUsuario ?? "Usuário Desconhecido",
                    TicketInfo = $"Ticket #{ticket.Id} - {ticket.Titulo}"
                });
            }
            ContactsList.ItemsSource = contacts;
        }

        /// <summary>
        /// (MODIFICADO) Carrega o histórico e inicia a conexão SignalR para o chat selecionado.
        /// </summary>
        private async void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            var contact = clickedButton?.DataContext as ChatContact;
            if (contact == null) return;

            // Destaca o contato selecionado
            if (_lastSelectedContactButton != null)
            {
                _lastSelectedContactButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));
            }
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            _lastSelectedContactButton = clickedButton;

            _chatSelecionadoAtual = contact.OriginalTicket;

            try
            {
                // 1. Carrega o histórico de mensagens (código que você já tinha)
                List<Mensagem> mensagens = await _apiService.GetMensagensAsync(_chatSelecionadoAtual.Id);
                foreach (var msg in mensagens)
                {
                    msg.IsSentByMe = (msg.UsuarioId == _usuarioLogado.Id);
                    msg.Author = msg.NomeUsuario;
                    msg.AuthorRole = msg.IsSentByMe ? "Técnico" : msg.AuthorRole;
                }
                MessagesList.ItemsSource = mensagens;
                ConversationPanel.Visibility = Visibility.Visible;
                ChatScrollViewer.ScrollToBottom();

                // --- ADIÇÃO IMPORTANTE ---
                // 2. Agora, conecta ao SignalR para este chat específico
                // *** Lembre-se de criar a classe MensagemPayload.cs no seu projeto ***
                await StartChatConnectionAsync(_chatSelecionadoAtual.Id);
                // --- FIM DA ADIÇÃO ---
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar o histórico do chat: {ex.Message}");
            }
        }

        /// <summary>
        /// (MODIFICADO) Apenas envia a mensagem para a API. O SignalR cuidará da atualização da UI.
        /// </summary>
        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (_chatSelecionadoAtual == null || string.IsNullOrWhiteSpace(txtChatMessage.Text))
            {
                return;
            }

            string mensagemParaEnviar = txtChatMessage.Text;
            txtChatMessage.Clear();

            try
            {
                // 1. Apenas envia a mensagem para a API
                // O SignalR (via API) cuidará de nos enviar a mensagem de volta
                // e o listener "ReceberNovaMensagem" vai atualizar a tela.
                await _apiService.EnviarMensagemAsync(
                    _chatSelecionadoAtual.Id,
                    _usuarioLogado.Id,
                    mensagemParaEnviar
                );

                // --- TODO O CÓDIGO ANTIGO DE ATUALIZAÇÃO MANUAL DA UI FOI REMOVIDO ---
                // (O listener _chatHubConnection.On<MensagemPayload>... fará o trabalho)
            }
            catch (Exception ex)
            {
                // Se a API falhar (ex: sem internet), mostre o erro.
                await ShowNotificationToast($"Erro ao enviar mensagem: {ex.Message}", isError: true);
            }
        }

        private void txtChatMessage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !(Keyboard.Modifiers == ModifierKeys.Shift))
            {
                e.Handled = true;
                btnSendMessage_Click(sender, e);
            }
        }

        // --- MÉTODOS NOVOS ADICIONADOS PARA O CHAT ---

        /// <summary>
        /// (NOVO) Conecta ao ChatHub, entra em um grupo específico e começa a ouvir mensagens.
        /// </summary>
        private async Task StartChatConnectionAsync(int ticketId)
        {
            // Se já estivermos conectados (ex: usuário clicou em outro contato)
            // desconecta primeiro da sala anterior.
            await StopChatConnectionAsync();

            string token = _apiService.AuthToken;
            if (string.IsNullOrEmpty(token)) return; // Se não tiver token, não continua

            _chatHubConnection = new HubConnectionBuilder()
                .WithUrl(_apiService.BaseUrl + "/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .Build();

            // *** O OUVINTE (LISTENER) DO CHAT ***
            // Ouve o evento "ReceberNovaMensagem" (que criamos na API)
            // e espera um objeto do tipo "MensagemPayload"
            _chatHubConnection.On<MensagemPayload>("ReceberNovaMensagem", (payload) =>
            {
                // A API enviou o payload. Convertemos para o modelo local "Mensagem"
                var novaMensagem = new Mensagem
                {
                    MensagemId = payload.MensagemId,
                    TicketId = payload.TicketId,
                    UsuarioId = payload.UsuarioId,
                    Conteudo = payload.Conteudo,
                    DataEnvio = payload.DataEnvio,

                    // Definimos as propriedades que a UI do WPF usa
                    IsSentByMe = (payload.UsuarioId == _usuarioLogado.Id),
                    Author = payload.NomeUsuario,
                    AuthorRole = payload.AuthorRole
                };

                // IMPORTANTE: O SignalR roda em outra thread.
                // Precisamos usar o Dispatcher para atualizar a UI.
                Dispatcher.Invoke(() =>
                {
                    // Verifica se ainda estamos na mesma conversa
                    if (_chatSelecionadoAtual != null && _chatSelecionadoAtual.Id == novaMensagem.TicketId)
                    {
                        var currentMessages = MessagesList.ItemsSource as List<Mensagem> ?? new List<Mensagem>();
                        currentMessages.Add(novaMensagem);

                        MessagesList.ItemsSource = null; // Força a atualização da lista
                        MessagesList.ItemsSource = currentMessages;

                        ChatScrollViewer.ScrollToBottom(); // Rola para a nova mensagem
                    }
                });
            });

            // Inicia a conexão e entra no grupo
            try
            {
                await _chatHubConnection.StartAsync();
                _currentChatGroup = $"ticket-{ticketId}";
                await _chatHubConnection.InvokeAsync("JoinChatGroup", _currentChatGroup);
                Debug.WriteLine($"Conexão SignalR (Chat) estabelecida. Entrou no grupo: {_currentChatGroup}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao conectar ao ChatHub: {ex.Message}");
            }
        }

        /// <summary>
        /// (NOVO) Para e limpa a conexão atual do ChatHub.
        /// </summary>
        private async Task StopChatConnectionAsync()
        {
            if (_chatHubConnection != null && _chatHubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_currentChatGroup))
                    {
                        await _chatHubConnection.InvokeAsync("LeaveChatGroup", _currentChatGroup);
                        Debug.WriteLine($"Saiu do grupo {_currentChatGroup}.");
                    }
                    await _chatHubConnection.StopAsync();
                    await _chatHubConnection.DisposeAsync(); // Limpa os recursos
                    Debug.WriteLine("Desconectado do ChatHub.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro ao desconectar do ChatHub: {ex.Message}");
                }
            }
            _chatHubConnection = null;
            _currentChatGroup = "";
        }

        #endregion

        #region Métodos Auxiliares e de Navegação

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allTickets == null) return;
            var clickedButton = sender as Button;
            if (clickedButton == null) return;

            // Reseta todos os botões
            btnAberto.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnAceito.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnFinalizado.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnCancelado.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));

            // Destaca o clicado
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));

            string status = clickedButton.Content.ToString();
            var filteredTickets = _allTickets.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            ItemsControlTickets.ItemsSource = filteredTickets;
        }

        private void UpdateDetailViewButtons(string status)
        {
            btnAceitar.Visibility = Visibility.Collapsed;
            btnFinalizar.Visibility = Visibility.Collapsed;
            btnCancelar.Visibility = Visibility.Collapsed;
            btnInformacoes.Visibility = Visibility.Collapsed;
            btnSugestaoIA.Visibility = Visibility.Collapsed;

            if (status.Equals("Aberto", StringComparison.OrdinalIgnoreCase))
            {
                btnAceitar.Visibility = Visibility.Visible;
            }
            else if (status.Equals("Aceito", StringComparison.OrdinalIgnoreCase))
            {
                btnFinalizar.Visibility = Visibility.Visible;
                btnCancelar.Visibility = Visibility.Visible;
                btnSugestaoIA.Visibility = Visibility.Visible;
            }
            else if (status.Equals("Finalizado", StringComparison.OrdinalIgnoreCase) || status.Equals("Concluído", StringComparison.OrdinalIgnoreCase))
            {
                btnInformacoes.Visibility = Visibility.Visible;
            }
        }

        private async Task ShowNotificationToast(string message, bool isError = false)
        {
            NotificationText.Text = message;
            NotificationToast.BorderBrush = isError ? Brushes.Red : new SolidColorBrush(Color.FromRgb(92, 184, 92));
            NotificationToast.Visibility = Visibility.Visible;
            await Task.Delay(3000);
            NotificationToast.Visibility = Visibility.Collapsed;
        }

        private void btnVoltar_Click(object sender, RoutedEventArgs e)
        {
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            TicketsView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// (MODIFICADO) Adiciona a desconexão do ChatHub ao trocar de menu.
        /// </summary>
        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton == null) return;

            // Destaca o botão do menu
            btnMenuTicket.Background = Brushes.Transparent;
            btnMenuChat.Background = Brushes.Transparent;
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));

            // Esconde todas as "telas" principais
            TicketsView.Visibility = Visibility.Collapsed;
            ChatView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            // ... (outras views principais, se houver)

            if (clickedButton.Name == "btnMenuTicket")
            {
                TicketsView.Visibility = Visibility.Visible;
                // --- ADICIONADO ---
                // Se o usuário saiu da tela de chat, desconecta
                await StopChatConnectionAsync();
            }
            else if (clickedButton.Name == "btnMenuChat")
            {
                ChatView.Visibility = Visibility.Visible;
                LoadChatContacts(); // Carrega a lista de contatos do chat
            }
        }

        private void btnAddAnexo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    if (!_anexosParaUpload.Contains(filename))
                    {
                        _anexosParaUpload.Add(filename);
                    }
                }
                ItemsControlAnexos.ItemsSource = null;
                ItemsControlAnexos.ItemsSource = _anexosParaUpload.Select(f => Path.GetFileName(f)).ToList();
            }
        }

        private void btnRemoverAnexo_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var fileName = button?.DataContext as string;
            if (string.IsNullOrEmpty(fileName)) return;

            var fullPathToRemove = _anexosParaUpload.FirstOrDefault(path => Path.GetFileName(path) == fileName);
            if (fullPathToRemove != null)
            {
                _anexosParaUpload.Remove(fullPathToRemove);
            }

            ItemsControlAnexos.ItemsSource = null;
            ItemsControlAnexos.ItemsSource = _anexosParaUpload.Select(f => Path.GetFileName(f)).ToList();
        }

        private void btnAbortCancel_Click(object sender, RoutedEventArgs e)
        {
            ConfirmCancelView.Visibility = Visibility.Collapsed;
        }

        private void btnVoltarDaFinalizacao_Click(object sender, RoutedEventArgs e)
        {
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Visible;
        }

        private void btnVoltarDaSolucao_Click(object sender, RoutedEventArgs e)
        {
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Visible;
        }

        private async void Anexo_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var anexoContext = button?.DataContext as Anexo;
            if (anexoContext == null) return;

            try
            {
                byte[] fileBytes = await _apiService.DownloadAnexoAsync(anexoContext.AnexoId);
                string tempPath = Path.Combine(Path.GetTempPath(), anexoContext.NomeArquivo);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                // Usa UseShellExecute = true para abrir o arquivo com o programa padrão do Windows
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir o anexo: {ex.Message}");
            }
        }
        #endregion
    } // Fim da classe Main

    // Classe auxiliar para a lista de contatos do chat
    public class ChatContact
    {
        public Ticket OriginalTicket { get; set; }
        public string DisplayName { get; set; }
        public string TicketInfo { get; set; }
    }
}