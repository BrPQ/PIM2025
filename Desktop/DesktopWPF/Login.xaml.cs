using DesktopWPF.Services;
using DesktopWPF.Models;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DesktopWPF
{
    public partial class Login : Window
    {
        private readonly ApiService _apiService;

        public Login()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            ErrorToast.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtMatricula.Text) || string.IsNullOrWhiteSpace(txtSenha.Password))
            {
                await ShowErrorToast("Por favor, preencha a matrícula e a senha.");
                return;
            }

            btnLogin.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Visible;

            try
            {
                Usuario usuarioLogado = await _apiService.LoginAsync(txtMatricula.Text, txtSenha.Password);

                if (usuarioLogado != null)
                {
                    if (usuarioLogado.Role.Equals("Tecnico", StringComparison.OrdinalIgnoreCase))
                    {
                        Main telaPrincipal = new Main(usuarioLogado, _apiService);
                        telaPrincipal.Show();
                        this.Close();
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

                txtMatricula.Clear();
                txtSenha.Clear();
                txtMatricula.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível conectar à API.\n\nDetalhes: {ex.Message}", "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (ErrorToast.Visibility != Visibility.Visible)
                {
                    btnLogin.Visibility = Visibility.Visible;
                }
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ShowErrorToast(string message)
        {
            btnLogin.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Collapsed;

            ErrorToastText.Text = message;
            ErrorToast.Visibility = Visibility.Visible;

            await Task.Delay(3000);
            ErrorToast.Visibility = Visibility.Collapsed;
            btnLogin.Visibility = Visibility.Visible;
        }

        private void txtMatricula_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void txtMatricula_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}