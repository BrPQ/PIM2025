using DesktopWPF.Models;
using DesktopWPF.Services;
using Microsoft.AspNetCore.SignalR.Client; // Biblioteca essencial para o Realtime (WebSockets)
using Microsoft.Win32; // Para abrir a janela de selecionar arquivos (OpenFileDialog)
using SeuProjetoWPF.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics; // Para usar Debug.WriteLine (logs no console do dev)
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading; // Para o Dispatcher (Gerenciamento de Threads da UI)

namespace DesktopWPF
{
    // A classe Main herda de Window, ou seja, ela é uma Janela do Windows.
    // "partial" significa que existe outro arquivo (Main.xaml) que completa o desenho dessa tela.
    public partial class Main : Window
    {
        #region Variáveis Globais
        // --- DEPENDÊNCIAS ---
        // Serviço que faz as chamadas HTTP (nossa ponte com a API).
        private readonly ApiService _apiService;

        // Objeto com os dados de quem logou (Nome, ID, Token, etc).
        private readonly Usuario _usuarioLogado;

        // --- CONEXÕES SIGNALR (REALTIME) ---
        // Conexão 1: Escuta eventos globais de Tickets (Novo Ticket, Atualização, Exclusão).
        private HubConnection _hubConnection;

        // Conexão 2: Escuta eventos de Chat (Mensagens chegando).
        // Separamos em duas conexões para organizar melhor o tráfego.
        private HubConnection _chatHubConnection;

        // Guarda o nome da "Sala" de chat atual (ex: "ticket-105") para saber de onde sair/entrar.
        private string _currentChatGroup = "";

        // --- ESTADO DA TELA (STATE) ---
        // Cache local: Guardamos a lista aqui para não precisar ir na API toda vez que filtrar (Aberto/Fechado).
        private List<Ticket> _allTickets;

        // Guarda o ticket que está aberto na tela de detalhes (para saber qual ID atualizar).
        private TicketDetalheDto _ticketDetalhadoAtual;

        // Guarda qual conversa do chat está selecionada no menu lateral.
        private Ticket _chatSelecionadoAtual;

        // Lista temporária de caminhos de arquivos (strings) que o usuário selecionou para upload.
        private List<string> _anexosParaUpload = new List<string>();

        // Referência visual ao último botão clicado no menu de contatos (para pintar de cinza).
        private Button _lastSelectedContactButton = null;
        #endregion

        // CONSTRUTOR: Executado quando a janela é criada (new Main(...)).
        public Main(Usuario usuarioLogado, ApiService apiService)
        {
            InitializeComponent(); // Método padrão do WPF que "desenha" os botões na tela.

            // Injeção de Dependência manual: Recebemos as instâncias vindas do Login.
            _apiService = apiService;
            _usuarioLogado = usuarioLogado;

            // Chama nosso método customizado para configurar a tela inicial.
            SetupInitialState();

            // Event Subscription:
            // "Quando a janela estiver fechando (Closing), execute o método Main_Closing".
            // Isso é vital para desconectar do servidor corretamente.
            this.Closing += Main_Closing;
        }

        #region Métodos de Inicialização e UI Geral

        // Configura a tela assim que ela abre.
        private async void SetupInitialState()
        {
            // Preenche o nome do técnico no canto superior direito.
            txtNomeUsuario.Text = _usuarioLogado.NomeUsuario;

            // --- RELÓGIO EM TEMPO REAL ---
            // Cria um timer que roda a cada 1 segundo.
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick; // A cada tick, roda o método Timer_Tick.
            timer.Start();

            // --- CONTROLE DE VISIBILIDADE (NAVIGATION) ---
            // O WPF não tem "páginas" nativas fáceis como na Web.
            // Aqui usamos o truque de "Esconder/Mostrar" Grids.
            // Começamos mostrando apenas a lista de Tickets e escondendo todo o resto.
            TicketsView.Visibility = Visibility.Visible;
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            ChatView.Visibility = Visibility.Collapsed;
            ConfirmCancelView.Visibility = Visibility.Collapsed;
            NotificationToast.Visibility = Visibility.Collapsed;
            IaSuggestionView.Visibility = Visibility.Collapsed;

            // 1. Busca os dados iniciais na API (HTTP GET).
            await LoadTicketsFromApiAsync();

            // 2. Conecta no WebSocket para receber atualizações em tempo real.
            await IniciarConexaoSignalR();
        }

        // Atualiza o relógio da tela.
        private void Timer_Tick(object sender, EventArgs e)
        {
            txtClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        // Botão de Logout.
        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            // Cria uma nova tela de login.
            Login loginWindow = new Login();
            loginWindow.Show();

            // Fecha a tela atual (Main) para liberar memória.
            this.Close();
        }
        #endregion

        #region Lógica de SignalR (Comunicação em Tempo Real)

        // Método responsável por conectar ao "TicketHub" do servidor.
        private async Task IniciarConexaoSignalR()
        {
            // Validação de segurança: Sem token, não conecta.
            string token = _apiService.AuthToken;
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Token de autenticação não encontrado. A conexão em tempo real falhará.");
                return;
            }

            // BUILDER PATTERN:
            // Configura a conexão apontando para a URL do Hub e injetando o Token JWT.
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_apiService.BaseUrl + "/ticketHub", options =>
                {
                    // Envia o token via WebSocket (Query String ou Header, dependendo da biblioteca).
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect() // Se a internet cair, tenta reconectar sozinho (0s, 2s, 10s, 30s).
                .Build();

            // --- EVENT LISTENER 1: ATUALIZAÇÃO ---
            // O servidor chamou "ReceberAtualizacaoTicket"? Executamos isso:
            _hubConnection.On<Ticket>("ReceberAtualizacaoTicket", (ticketAtualizado) =>
            {
                // DISPATCHER (IMPORTANTE):
                // O SignalR roda numa thread de fundo (Background Thread).
                // O WPF não deixa threads de fundo mexerem na UI (Label, Button).
                // O Dispatcher.Invoke transfere a execução para a Thread Principal (UI Thread).
                Dispatcher.Invoke(async () =>
                {
                    await ShowNotificationToast($"Ticket #{ticketAtualizado.Id} foi atualizado!");
                    await LoadTicketsFromApiAsync(); // Recarrega a lista para o usuário ver a mudança.
                });
            });

            // --- EVENT LISTENER 2: NOVO TICKET ---
            _hubConnection.On<Ticket>("ReceberNovoTicket", (novoTicket) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    await ShowNotificationToast($"Novo ticket #{novoTicket.Id} criado!");
                    await LoadTicketsFromApiAsync();
                });
            });

            // --- EVENT LISTENER 3: DELEÇÃO ---
            _hubConnection.On<int>("ReceberTicketDeletado", (ticketId) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    await ShowNotificationToast($"O ticket #{ticketId} foi excluído!");
                    await LoadTicketsFromApiAsync();
                });
            });

            // Tenta efetivamente conectar.
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

        // Método executado quando o usuário clica no "X" para fechar o programa.
        private async void Main_Closing(object sender, CancelEventArgs e)
        {
            // Boa prática: Desconectar explicitamente para não deixar conexões presas no servidor.
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
            }

            // Desconecta também o Chat.
            await StopChatConnectionAsync();
        }

        #endregion

        #region Lógica de Tickets

        // Busca a lista de tickets na API e preenche a tela.
        private async Task LoadTicketsFromApiAsync()
        {
            try
            {
                // Busca TODOS os tickets e guarda na variável global.
                _allTickets = await _apiService.GetTicketsAsync();

                // Reaplica o filtro visual.
                // Ex: Se eu estava vendo a aba "Finalizados" e chegou um ticket novo,
                // eu quero continuar vendo "Finalizados", e não resetar para "Abertos".
                Button activeFilterButton = FindActiveFilterButton();
                FilterButton_Click(activeFilterButton, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tickets da API: {ex.Message}");
            }
        }

        // Lógica visual para descobrir qual botão de filtro está "Cinza Claro" (ativo).
        private Button FindActiveFilterButton()
        {
            // Compara a cor de fundo de cada botão com a cor cinza (#555555).
            if (btnAceito.Background is SolidColorBrush b1 && b1.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnAceito;
            if (btnFinalizado.Background is SolidColorBrush b2 && b2.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnFinalizado;
            if (btnCancelado.Background is SolidColorBrush b3 && b3.Color == ((SolidColorBrush)new BrushConverter().ConvertFrom("#555555")).Color) return btnCancelado;

            // Se nenhum estiver ativo, assume que é o "Aberto" (Padrão).
            return btnAberto;
        }

        // Evento de clique no botão "Ver Detalhes" (dentro da lista de tickets).
        private async void TicketButton_Click(object sender, RoutedEventArgs e)
        {
            // Recupera o botão que foi clicado.
            var button = sender as Button;

            // DATA CONTEXT:
            // No WPF, cada linha da lista sabe qual objeto "Ticket" ela representa.
            var ticketContext = button?.DataContext as Ticket;
            if (ticketContext == null) return;

            try
            {
                // Busca os detalhes completos na API (incluindo descrição longa).
                _ticketDetalhadoAtual = await _apiService.GetTicketByIdAsync(ticketContext.Id);

                // Preenche os campos de texto da tela de Detalhes.
                txtDetailTicketId.Text = $"TICKET#{_ticketDetalhadoAtual.Id}";
                txtDetailDescricao.Text = _ticketDetalhadoAtual.Descricao;
                txtDetailUsuario.Text = _ticketDetalhadoAtual.NomeUsuario;
                txtDetailSetor.Text = _ticketDetalhadoAtual.PerfilUsuario;

                // Busca anexos enviados pelo Usuário.
                var anexos = await _apiService.GetAnexosByTicketIdAsync(_ticketDetalhadoAtual.Id, "Usuario");
                ItemsControlTicketAnexos.ItemsSource = anexos;

                // Lógica visual do campo "Profissional".
                if (_ticketDetalhadoAtual.Status.Equals("Aberto", StringComparison.OrdinalIgnoreCase))
                {
                    txtDetailProfissional.Text = string.Empty;
                }
                else
                {
                    txtDetailProfissional.Text = _ticketDetalhadoAtual.ProfissionalDesignado ?? "Nenhum";
                }

                // Decide quais botões mostrar (Aceitar? Finalizar? Cancelar?).
                UpdateDetailViewButtons(_ticketDetalhadoAtual.Status);

                // Troca de Telas (Esconde a lista, mostra o detalhe).
                TicketsView.Visibility = Visibility.Collapsed;
                TicketDetailView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar detalhes do ticket: {ex.Message}");
            }
        }

        // Ação: ACEITAR CHAMADO
        private async void btnAceitar_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null) return;

            // Monta o objeto anônimo para o Update.
            var updateData = new
            {
                status = "Aceito",
                profissionalDesignado = _usuarioLogado.NomeUsuario, // Eu assumo o chamado
                solucao = _ticketDetalhadoAtual.Solucao // Solução continua null/vazia
            };

            try
            {
                // Chama a API (PUT).
                bool success = await _apiService.UpdateTicketAsync(_ticketDetalhadoAtual.Id, updateData);
                if (success)
                {
                    await ShowNotificationToast("Ticket aceito com sucesso!");
                    // Volta para a tela principal.
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

        // Ação: FINALIZAR (Abre a tela de preencher solução)
        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            // Limpa os campos anteriores.
            txtSolucao.Text = string.Empty;
            _anexosParaUpload.Clear();
            ItemsControlAnexos.ItemsSource = null;

            // Navegação.
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Visible;
        }

        // Ação: CONFIRMAR FINALIZAÇÃO (Botão verde na tela de solução)
        private async void btnConfirmarFinalizacao_Click(object sender, RoutedEventArgs e)
        {
            // Validação: Não pode finalizar sem escrever o que fez.
            if (_ticketDetalhadoAtual == null || string.IsNullOrWhiteSpace(txtSolucao.Text))
            {
                await ShowNotificationToast("Por favor, preencha a descrição da solução.", isError: true);
                return;
            }

            try
            {
                // 1. Envia todos os anexos primeiro (Upload).
                foreach (var filePath in _anexosParaUpload)
                {
                    await _apiService.UploadAnexoAsync(_ticketDetalhadoAtual.Id, filePath, "Tecnico");
                }

                // 2. Atualiza o chamado no banco.
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

        // Ação: CANCELAR (Mostra popup de confirmação)
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            ConfirmCancelView.Visibility = Visibility.Visible;
        }

        // Ação: CONFIRMAR CANCELAMENTO
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

        // Ação: VER INFORMAÇÕES (Para chamados já fechados)
        private async void btnInformacoes_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketDetalhadoAtual == null) return;

            try
            {
                // Busca anexos que o TÉCNICO enviou (evidências de solução).
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

        // --- INTEGRAÇÃO COM IA ---
        private async void btnSugestaoIA_Click(object sender, RoutedEventArgs e)
        {
            // Validação: Tem que ter descrição para a IA ler.
            if (_ticketDetalhadoAtual == null || string.IsNullOrWhiteSpace(_ticketDetalhadoAtual.Descricao))
            {
                await ShowNotificationToast("Não há descrição do problema para analisar.", isError: true);
                return;
            }

            try
            {
                // UX: Feedback de "Carregando".
                var originalContent = btnSugestaoIA.Content;
                btnSugestaoIA.Content = "Pensando...";
                btnSugestaoIA.IsEnabled = false; // Trava o botão para não clicar 2x.

                // Chama a IA.
                string sugestao = await _apiService.GetSugestaoIaAsync(_ticketDetalhadoAtual.Descricao, "Tecnico");

                // Mostra o resultado.
                IaSuggestionText.Text = sugestao;
                IaSuggestionView.Visibility = Visibility.Visible;

                // Destrava o botão.
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

        // Botão "OK" do Popup da IA.
        private void btnIaOk_Click(object sender, RoutedEventArgs e)
        {
            IaSuggestionView.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Lógica de Chat

        // Carrega a lista lateral de contatos.
        // REGRA DE NEGÓCIO: Só mostra chamados que estão "Aceito" e que são MEUS.
        private async void LoadChatContacts()
        {
            if (_allTickets == null) return;

            // Filtro LINQ.
            var ticketsAceitos = _allTickets
                .Where(ticket =>
                    ticket.Status.Equals("Aceito", StringComparison.OrdinalIgnoreCase) &&
                    ticket.ProfissionalDesignado.Equals(_usuarioLogado.NomeUsuario, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            var contacts = new List<ChatContact>();

            foreach (var ticket in ticketsAceitos)
            {
                // Busca o nome do usuário dono do ticket para exibir na lista de contatos.
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

        // Clique num Contato (Menu Lateral do Chat).
        private async void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            var contact = clickedButton?.DataContext as ChatContact;
            if (contact == null) return;

            // --- Lógica Visual de Seleção (Destaque Cinza) ---
            if (_lastSelectedContactButton != null)
            {
                _lastSelectedContactButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));
            }
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            _lastSelectedContactButton = clickedButton;

            _chatSelecionadoAtual = contact.OriginalTicket;

            try
            {
                // 1. Busca o histórico de mensagens (REST).
                List<Mensagem> mensagens = await _apiService.GetMensagensAsync(_chatSelecionadoAtual.Id);

                // 2. Processamento Local: Define quais mensagens são "Minhas" e quais são "Dele".
                // Isso serve para o XAML desenhar na direita ou esquerda.
                foreach (var msg in mensagens)
                {
                    msg.IsSentByMe = (msg.UsuarioId == _usuarioLogado.Id);
                    msg.Author = msg.NomeUsuario;
                    // Se fui eu que mandei, muda a Role para "Técnico" visualmente.
                    msg.AuthorRole = msg.IsSentByMe ? "Técnico" : msg.AuthorRole;
                }

                // Atualiza a lista na tela.
                MessagesList.ItemsSource = mensagens;

                // Mostra o painel de conversa.
                ConversationPanel.Visibility = Visibility.Visible;

                // Rola para a última mensagem.
                ChatScrollViewer.ScrollToBottom();

                // 3. Conecta no SignalR do Chat.
                await StartChatConnectionAsync(_chatSelecionadoAtual.Id);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar o histórico do chat: {ex.Message}");
            }
        }

        // Ação: ENVIAR MENSAGEM
        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (_chatSelecionadoAtual == null || string.IsNullOrWhiteSpace(txtChatMessage.Text))
            {
                return;
            }

            string mensagemParaEnviar = txtChatMessage.Text;
            txtChatMessage.Clear(); // Limpa o campo imediatamente (UX rápida).

            try
            {
                // Envia para a API (POST).
                // A API salva no banco e avisa o SignalR, que avisa a gente de volta.
                // Então não precisamos adicionar manualmente na lista aqui, esperamos o evento do SignalR.
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

        // Permite enviar com ENTER.
        private void txtChatMessage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter envia, Shift+Enter pula linha.
            if (e.Key == Key.Enter && !(Keyboard.Modifiers == ModifierKeys.Shift))
            {
                e.Handled = true; // Impede que o Enter pule linha.
                btnSendMessage_Click(sender, e);
            }
        }

        // Conecta ao SignalR do Chat.
        private async Task StartChatConnectionAsync(int ticketId)
        {
            // Se já estava num chat, sai dele antes de entrar no próximo.
            await StopChatConnectionAsync();

            string token = _apiService.AuthToken;
            if (string.IsNullOrEmpty(token)) return;

            _chatHubConnection = new HubConnectionBuilder()
                .WithUrl(_apiService.BaseUrl + "/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .Build();

            // EVENTO: RECEBER NOVA MENSAGEM
            _chatHubConnection.On<MensagemPayload>("ReceberNovaMensagem", (payload) =>
            {
                // Converte o Payload (JSON leve) para o Modelo Visual.
                var novaMensagem = new Mensagem
                {
                    MensagemId = payload.MensagemId,
                    TicketId = payload.TicketId,
                    UsuarioId = payload.UsuarioId,
                    Conteudo = payload.Conteudo,
                    DataEnvio = payload.DataEnvio,

                    // Lógica visual crucial:
                    IsSentByMe = (payload.UsuarioId == _usuarioLogado.Id),
                    Author = payload.NomeUsuario,
                    AuthorRole = payload.AuthorRole
                };

                // Atualiza a UI na Thread Principal.
                Dispatcher.Invoke(() =>
                {
                    // VERIFICAÇÃO DE SEGURANÇA VISUAL:
                    // Só mostra a mensagem se o usuário estiver com ESTE chat aberto.
                    // Se chegar mensagem do Ticket 200 enquanto estou vendo o 100, ignora (ou poderia mostrar alerta).
                    if (_chatSelecionadoAtual != null && _chatSelecionadoAtual.Id == novaMensagem.TicketId)
                    {
                        var currentMessages = MessagesList.ItemsSource as List<Mensagem> ?? new List<Mensagem>();
                        currentMessages.Add(novaMensagem);

                        // Hack para refresh da lista.
                        MessagesList.ItemsSource = null;
                        MessagesList.ItemsSource = currentMessages;

                        ChatScrollViewer.ScrollToBottom();
                    }
                });
            });

            // Conecta e entra no Grupo (Sala).
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

        // Desconecta do Chat.
        private async Task StopChatConnectionAsync()
        {
            if (_chatHubConnection != null && _chatHubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    // Avisa o servidor que estou saindo da sala.
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

        // Filtra a lista de tickets visualmente.
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allTickets == null) return;
            var clickedButton = sender as Button;
            if (clickedButton == null) return;

            // Reseta a cor de todos os botões para "Escuro".
            btnAberto.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnAceito.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnFinalizado.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            btnCancelado.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));

            // Pinta o botão clicado de "Claro" (Ativo).
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(85, 85, 85));

            // Filtra a lista usando LINQ.
            string status = clickedButton.Content.ToString();
            var filteredTickets = _allTickets.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            ItemsControlTickets.ItemsSource = filteredTickets;
        }

        // Controle de visibilidade dos botões de ação (Aceitar, Finalizar...)
        private void UpdateDetailViewButtons(string status)
        {
            // Começa escondendo tudo.
            btnAceitar.Visibility = Visibility.Collapsed;
            btnFinalizar.Visibility = Visibility.Collapsed;
            btnCancelar.Visibility = Visibility.Collapsed;
            btnInformacoes.Visibility = Visibility.Collapsed;
            btnSugestaoIA.Visibility = Visibility.Collapsed;

            // Mostra apenas o que faz sentido para o status atual.
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

        // Toast de Notificação (Popup verde/vermelho que some sozinho).
        private async Task ShowNotificationToast(string message, bool isError = false)
        {
            NotificationText.Text = message;
            // Muda a cor da borda (Vermelho se erro, Verde se sucesso).
            NotificationToast.BorderBrush = isError ? Brushes.Red : new SolidColorBrush(Color.FromRgb(92, 184, 92));

            NotificationToast.Visibility = Visibility.Visible;
            await Task.Delay(3000); // Espera 3s.
            NotificationToast.Visibility = Visibility.Collapsed;
        }

        // Botão "Voltar" (Seta) na tela de detalhes.
        private void btnVoltar_Click(object sender, RoutedEventArgs e)
        {
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            TicketsView.Visibility = Visibility.Visible; // Volta para a lista principal.
        }

        // Navegação do Menu Principal (Esquerda).
        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton == null) return;

            // Reset visual dos botões do menu.
            btnMenuTicket.Background = Brushes.Transparent;
            btnMenuChat.Background = Brushes.Transparent;
            clickedButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));

            // Esconde todas as Views.
            TicketsView.Visibility = Visibility.Collapsed;
            ChatView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Collapsed;
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            SolucaoTicketView.Visibility = Visibility.Collapsed;

            // Mostra a View selecionada.
            if (clickedButton.Name == "btnMenuTicket")
            {
                TicketsView.Visibility = Visibility.Visible;
                // Importante: Se saí do chat, desconecto para economizar recursos.
                await StopChatConnectionAsync();
            }
            else if (clickedButton.Name == "btnMenuChat")
            {
                ChatView.Visibility = Visibility.Visible;
                LoadChatContacts(); // Recarrega a lista de contatos.
            }
        }

        // Adicionar Anexo (Botão "+").
        private void btnAddAnexo_Click(object sender, RoutedEventArgs e)
        {
            // Abre janela do Windows para selecionar arquivos.
            var openFileDialog = new OpenFileDialog { Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    // Evita duplicatas na lista.
                    if (!_anexosParaUpload.Contains(filename))
                    {
                        _anexosParaUpload.Add(filename);
                    }
                }
                // Atualiza a lista visual.
                ItemsControlAnexos.ItemsSource = null;
                // Exibe apenas o nome do arquivo, não o caminho completo (Path.GetFileName).
                ItemsControlAnexos.ItemsSource = _anexosParaUpload.Select(f => Path.GetFileName(f)).ToList();
            }
        }

        // Remover Anexo da lista de upload.
        private void btnRemoverAnexo_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var fileName = button?.DataContext as string; // Nome do arquivo clicado.
            if (string.IsNullOrEmpty(fileName)) return;

            // Acha o caminho completo correspondente ao nome clicado.
            var fullPathToRemove = _anexosParaUpload.FirstOrDefault(path => Path.GetFileName(path) == fileName);
            if (fullPathToRemove != null)
            {
                _anexosParaUpload.Remove(fullPathToRemove);
            }

            // Atualiza a lista visual.
            ItemsControlAnexos.ItemsSource = null;
            ItemsControlAnexos.ItemsSource = _anexosParaUpload.Select(f => Path.GetFileName(f)).ToList();
        }

        // Botão "Não" no popup de cancelar.
        private void btnAbortCancel_Click(object sender, RoutedEventArgs e)
        {
            ConfirmCancelView.Visibility = Visibility.Collapsed;
        }

        // Botão "Voltar" na tela de finalizar.
        private void btnVoltarDaFinalizacao_Click(object sender, RoutedEventArgs e)
        {
            FinalizarTicketView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Visible;
        }

        // Botão "Voltar" na tela de solução.
        private void btnVoltarDaSolucao_Click(object sender, RoutedEventArgs e)
        {
            SolucaoTicketView.Visibility = Visibility.Collapsed;
            TicketDetailView.Visibility = Visibility.Visible;
        }

        // Clique num anexo (para baixar e abrir).
        private async void Anexo_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var anexoContext = button?.DataContext as Anexo;
            if (anexoContext == null) return;

            try
            {
                // 1. Baixa os bytes do servidor.
                byte[] fileBytes = await _apiService.DownloadAnexoAsync(anexoContext.AnexoId);

                // 2. Salva na pasta temporária do Windows (%TEMP%).
                string tempPath = Path.Combine(Path.GetTempPath(), anexoContext.NomeArquivo);
                await File.WriteAllBytesAsync(tempPath, fileBytes);

                // 3. Manda o Windows abrir o arquivo com o programa padrão.
                // UseShellExecute = true é necessário no .NET Core para abrir arquivos.
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir o anexo: {ex.Message}");
            }
        }
        #endregion
    }

    // CLASSE AUXILIAR (VIEW MODEL):
    // Usada apenas para montar a lista de contatos do chat (Foto + Nome + Resumo).
    public class ChatContact
    {
        public Ticket OriginalTicket { get; set; }
        public string DisplayName { get; set; }
        public string TicketInfo { get; set; }
    }
}