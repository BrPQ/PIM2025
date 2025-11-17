using DesktopWPF.Models;
using DesktopWPF.Services;
using Microsoft.AspNetCore.SignalR.Client; 
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
        private HubConnection _hubConnection;       
        private HubConnection _chatHubConnection;   
        private string _currentChatGroup = "";      
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

            
            this.Closing += Main_Closing;
        }

        #region Métodos de Inicialização e UI Geral
        private async void SetupInitialState()
        {
            txtNomeUsuario.Text = _usuarioLogado.NomeUsuario;

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();

            
            TicketsView.Visibility = Visibility.Visible;
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            ChatView.Visibility = Visibility.Collapsed;
            ConfirmCancelView.Visibility = Visibility.Collapsed;
            NotificationToast.Visibility = Visibility.Collapsed;
            IaSuggestionView.Visibility = Visibility.Collapsed;

            await LoadTicketsFromApiAsync();

            
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
            this.Close(); 
        }
        #endregion

        #region Lógica de SignalR (Comunicação em Tempo Real)

        
        private async Task IniciarConexaoSignalR()
        {
            
            string token = _apiService.AuthToken;
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Token de autenticação não encontrado. A conexão em tempo real falhará.");
                return;
            }

            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_apiService.BaseUrl + "/ticketHub", options =>
                {
                    
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect()
                .Build();

            

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

        
        private async void Main_Closing(object sender, CancelEventArgs e)
        {
            
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
            }

            
            await StopChatConnectionAsync();
        }

        #endregion

        #region Lógica de Tickets

        private async Task LoadTicketsFromApiAsync()
        {
            try
            {
                _allTickets = await _apiService.GetTicketsAsync();
                
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
            
            if (btnAceito.Background is SolidColorBrush b1 && b1.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnAceito;
            if (btnFinalizado.Background is SolidColorBrush b2 && b2.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnFinalizado;
            if (btnCancelado.Background is SolidColorBrush b3 && b3.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnCancelado;
            return btnAberto; 
        }


        private async void TicketButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var ticketContext = button?.DataContext as Ticket;
            if (ticketContext == null) return;

            try
            {
                
                _ticketDetalhadoAtual = await _apiService.GetTicketByIdAsync(ticketContext.Id);

                

                
                txtDetailTicketId.Text = $"TICKET#{_ticketDetalhadoAtual.Id}";
                txtDetailDescricao.Text = _ticketDetalhadoAtual.Descricao;

                
                txtDetailUsuario.Text = _ticketDetalhadoAtual.NomeUsuario;

                
                txtDetailSetor.Text = _ticketDetalhadoAtual.PerfilUsuario; 

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

        
        private async void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            var contact = clickedButton?.DataContext as ChatContact;
            if (contact == null) return;

            
            if (_lastSelectedContactButton != null)
            {
                _lastSelectedContactButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));
            }
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            _lastSelectedContactButton = clickedButton;

            _chatSelecionadoAtual = contact.OriginalTicket;

            try
            {
                
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

                
                await StartChatConnectionAsync(_chatSelecionadoAtual.Id);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar o histórico do chat: {ex.Message}");
            }
        }

        
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
                
                await _apiService.EnviarMensagemAsync(
                    _chatSelecionadoAtual.Id,
                    _usuarioLogado.Id,
                    mensagemParaEnviar
                );

                
            }
            catch (Exception ex)
            {
                
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

        
        private async Task StartChatConnectionAsync(int ticketId)
        {
            
            await StopChatConnectionAsync();

            string token = _apiService.AuthToken;
            if (string.IsNullOrEmpty(token)) return; 

            _chatHubConnection = new HubConnectionBuilder()
                .WithUrl(_apiService.BaseUrl + "/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .Build();

            
            _chatHubConnection.On<MensagemPayload>("ReceberNovaMensagem", (payload) =>
            {
                
                var novaMensagem = new Mensagem
                {
                    MensagemId = payload.MensagemId,
                    TicketId = payload.TicketId,
                    UsuarioId = payload.UsuarioId,
                    Conteudo = payload.Conteudo,
                    DataEnvio = payload.DataEnvio,

                    
                    IsSentByMe = (payload.UsuarioId == _usuarioLogado.Id),
                    Author = payload.NomeUsuario,
                    AuthorRole = payload.AuthorRole
                };

                
                Dispatcher.Invoke(() =>
                {
                    
                    if (_chatSelecionadoAtual != null && _chatSelecionadoAtual.Id == novaMensagem.TicketId)
                    {
                        var currentMessages = MessagesList.ItemsSource as List<Mensagem> ?? new List<Mensagem>();
                        currentMessages.Add(novaMensagem);

                        MessagesList.ItemsSource = null; 
                        MessagesList.ItemsSource = currentMessages;

                        ChatScrollViewer.ScrollToBottom(); 
                    }
                });
            });

            
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
                    await _chatHubConnection.DisposeAsync(); 
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

            
            btnAberto.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnAceito.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnFinalizado.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnCancelado.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));

            
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

        
        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton == null) return;

            
            btnMenuTicket.Background = Brushes.Transparent;
            btnMenuChat.Background = Brushes.Transparent;
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));

            
            TicketsView.Visibility = Visibility.Collapsed;
            ChatView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            

            if (clickedButton.Name == "btnMenuTicket")
            {
                TicketsView.Visibility = Visibility.Visible;
                
                await StopChatConnectionAsync();
            }
            else if (clickedButton.Name == "btnMenuChat")
            {
                ChatView.Visibility = Visibility.Visible;
                LoadChatContacts(); 
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
                
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir o anexo: {ex.Message}");
            }
        }
        #endregion
    } 

    
    public class ChatContact
    {
        public Ticket OriginalTicket { get; set; }
        public string DisplayName { get; set; }
        public string TicketInfo { get; set; }
    }
}