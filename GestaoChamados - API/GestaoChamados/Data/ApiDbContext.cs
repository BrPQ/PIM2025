using GestaoChamados.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoChamados.Data
{
    // A classe herda de DbContext, que é a classe base do Entity Framework Core.
    // É ela que possui toda a inteligência para conectar, salvar e buscar dados.
    public class ApiDbContext : DbContext
    {
        // Construtor: Essencial para a Injeção de Dependência.
        // O "options" traz as configurações lá do Program.cs (como a String de Conexão com o SQL Server).
        // O ": base(options)" repassa essas configurações para a classe pai (DbContext) inicializar tudo.
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }
  
        // MAPEAMENTO TABELAS (DbSets)
        // Cada propriedade "DbSet" abaixo representa uma TABELA no seu banco de dados SQL Server.
        // O nome da propriedade será o nome da tabela (ex: Tabela "Tickets").

        public DbSet<Ticket> Tickets { get; set; }   // Tabela onde ficam os chamados
        public DbSet<Usuario> Usuarios { get; set; } // Tabela de login e usuários
        public DbSet<Anexo> Anexos { get; set; }     // Tabela de arquivos anexados
        public DbSet<Mensagem> Mensagens { get; set; } // Tabela do chat
    }
}