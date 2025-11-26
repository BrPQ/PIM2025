using DesktopWPF.Services;
using DesktopWPF.Models;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DesktopWPF
{
    // Partial Class: Significa que este código se junta ao desenho XAML para formar a janela completa.
    public partial class Login : Window
    {
        // Dependência do Serviço de API.
        // A tela de Login não sabe fazer chamadas HTTP, ela delega isso para o ApiService.
        private readonly ApiService _apiService;

        public Login()
        {
            InitializeComponent(); // Carrega os componentes visuais do XAML.
            _apiService = new ApiService();
        }

        // -------------------------------------------------------------------------
        // EVENTO DE CLIQUE (Lógica Principal)
        // -------------------------------------------------------------------------
        // async void:
        // ATENÇÃO: "async void" só é permitido em Event Handlers (cliques de botão).
        // Em qualquer outro lugar, deve-se usar "async Task".
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Reset visual: Esconde qualquer erro anterior.
            ErrorToast.Visibility = Visibility.Collapsed;

            // 1. Validação Básica (Front-end First):
            // Antes de incomodar o servidor, verificamos se o usuário digitou algo.
            // Isso economiza banda de rede.
            if (string.IsNullOrWhiteSpace(txtMatricula.Text) || string.IsNullOrWhiteSpace(txtSenha.Password))
            {
                await ShowErrorToast("Por favor, preencha a matrícula e a senha.");
                return;
            }

            // 2. Feedback Visual (UX):
            // Esconde o botão "Entrar" e mostra o "Loading" (bolinha girando).
            // Isso impede que o usuário clique 50 vezes no botão achando que travou.
            btnLogin.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Visible;

            try
            {
                // 3. Chamada Assíncrona (Non-Blocking UI):
                // O comando 'await' libera a tela para não travar enquanto a internet processa.
                // Se não usasse await, a janela ficaria "Não Respondendo" até o servidor voltar.
                Usuario usuarioLogado = await _apiService.LoginAsync(txtMatricula.Text, txtSenha.Password);

                if (usuarioLogado != null)
                {
                    // 4. Regra de Negócio (Segurança de Acesso):
                    // O Desktop é exclusivo para TÉCNICOS.
                    // Se um funcionário comum (que deveria usar o App Mobile) tentar logar aqui, barramos.
                    if (usuarioLogado.Role.Equals("Tecnico", StringComparison.OrdinalIgnoreCase))
                    {
                        // Sucesso: Abre a tela principal e passa os dados do usuário.
                        Main telaPrincipal = new Main(usuarioLogado, _apiService);
                        telaPrincipal.Show();
                        this.Close(); // Fecha a tela de login para não ficar aberta no fundo.
                        return;
                    }
                    else
                    {
                        await ShowErrorToast("Acesso negado. Apenas Técnicos podem usar este sistema.");
                    }
                }
                else
                {
                    await ShowErrorToast("Matrícula ou senha inválidos.");
                }

                // Se falhou, limpa os campos para nova tentativa.
                txtMatricula.Clear();
                txtSenha.Clear();
                txtMatricula.Focus();
            }
            catch (Exception ex)
            {
                // Tratamento de Erro Robusto:
                // Se a internet cair ou o servidor estiver desligado, o app não "crasha" (fecha na cara).
                // Ele mostra uma mensagem amigável.
                MessageBox.Show($"Não foi possível conectar à API.\n\nDetalhes: {ex.Message}", "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 5. Bloco Finally:
                // Executa SEMPRE, dando certo ou errado.
                // Garante que o Loading suma e o botão volte a aparecer.
                if (ErrorToast.Visibility != Visibility.Visible)
                {
                    btnLogin.Visibility = Visibility.Visible;
                }
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        // -------------------------------------------------------------------------
        // HELPERS VISUAIS (Toast Notification)
        // -------------------------------------------------------------------------
        // Cria uma mensagem de erro temporária que some sozinha após 3 segundos.
        private async Task ShowErrorToast(string message)
        {
            btnLogin.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Collapsed;

            ErrorToastText.Text = message;
            ErrorToast.Visibility = Visibility.Visible;

            await Task.Delay(3000); // Espera 3 segundos sem travar a tela.

            ErrorToast.Visibility = Visibility.Collapsed;
            btnLogin.Visibility = Visibility.Visible;
        }

        // -------------------------------------------------------------------------
        // VALIDAÇÃO DE ENTRADA (REGEX)
        // -------------------------------------------------------------------------
        // Impede que o usuário digite letras no campo de matrícula.
        private void txtMatricula_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Regex: [^0-9]+ significa "Qualquer coisa que NÃO seja número".
            Regex regex = new Regex("[^0-9]+");
            // Se o texto digitado der match na regex (ou seja, for letra), Handled = true (cancela a digitação).
            e.Handled = regex.IsMatch(e.Text);
        }

        // Impede espaços em branco na matrícula.
        private void txtMatricula_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}