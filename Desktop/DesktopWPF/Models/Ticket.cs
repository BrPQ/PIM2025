using System;

namespace DesktopWPF.Models
{
    // POCO (Plain Old CLR Object):
    // Uma classe simples, sem herança complexa e sem atributos de banco de dados.
    // Ela serve de "espelho" para o JSON que vem da API na rota GET /api/tickets.
    public class Ticket
    {
        // O ID é invisível para o usuário na lista, mas essencial para o botão "Ver Detalhes".
        // Quando clicamos num item da lista, o WPF usa esse ID para chamar a API.
        public int Id { get; set; }

        // Propriedades exibidas nas colunas do DataGrid ou ListView do WPF.
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public DateTime DataAbertura { get; set; }

        // O Status é vital para os "Converters" do WPF.
        // No XAML, você vai ter algo como: "Se Status == 'Aberto', pinte a linha de Verde".
        public string Status { get; set; }

        public int UsuarioId { get; set; }

        // --- NULLABLES (?) ---
        // Aqui a interrogação (?) é importante para a Interface Gráfica (GUI).
        // Se ProfissionalDesignado for NULL, o WPF entende e deixa o campo em branco
        // (ou podemos usar um Trigger para escrever "Aguardando Técnico").
        public string? ProfissionalDesignado { get; set; }

        // Se Solucao for NULL, significa que o chamado ainda não foi resolvido.
        public string? Solucao { get; set; }
    }
}