using Microsoft.EntityFrameworkCore;
using PIMIIIWeb_SQL_Login.Models;

namespace PIMIIIWeb_SQL_Login.Data
{
    // A classe DbContext é o coração do Entity Framework.
    // Ela representa uma sessão com o banco de dados e gerencia as tabelas.
    public class AppDbContext : DbContext
    {
        // Construtor:
        // Recebe as opções de configuração (como a String de Conexão com o SQL Server)
        // e repassa para a classe base (DbContext) inicializar a conexão.
        // Isso é feito via Injeção de Dependência no Program.cs.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet: Representa a tabela "Usuarios" no banco de dados.
        // Todas as consultas (Select) e comandos (Insert/Update) passarão por aqui.
        public DbSet<Usuario> Usuarios => Set<Usuario>();

        // OnModelCreating:
        // Este método é executado quando o Entity Framework está criando a estrutura do banco.
        // É aqui que configuramos regras manuais ou inserimos dados iniciais.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- DATA SEEDING (SEMEADURA DE DADOS) ---
            // Para que o sistema não "nasça" vazio, nós forçamos a criação de 3 usuários iniciais.
            // Isso é fundamental para o primeiro acesso (Bootstrap), senão não teríamos como logar.

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1, // O ID deve ser fixo no Seeding para evitar duplicidade.
                    Nome = "Administrador",
                    Email = "admin@sistema.com",
                    Login = "admin",
                    // OBSERVAÇÃO PARA A BANCA:
                    // "Professor, neste contexto de 'Seed' inicial, colocamos a senha fixa
                    // para facilitar os testes da banca e do desenvolvimento."
                    Senha = "admin123",
                    Perfil = "Gestor" // Define que este usuário tem acesso total.
                },
                new Usuario
                {
                    Id = 2,
                    Nome = "Técnico N1",
                    Email = "tec1@sistema.com",
                    Login = "tec1",
                    Senha = "123",
                    Perfil = "Técnico" // Perfil para testar as telas de atendimento.
                },
                new Usuario
                {
                    Id = 3,
                    Nome = "Colaborador Demo",
                    Email = "user@sistema.com",
                    Login = "user",
                    Senha = "123",
                    Perfil = "Colaborador" // Perfil para testar a abertura de chamados.
                }
            );
        }
    }
}