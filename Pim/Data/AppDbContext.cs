using Microsoft.EntityFrameworkCore;
using PIMIIIWeb_SQL_Login.Models;

namespace PIMIIIWeb_SQL_Login.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    Nome = "Administrador",
                    Email = "admin@sistema.com",
                    Login = "admin",
                    Senha = "admin123",
                    Perfil = "Gestor"
                },
                new Usuario
                {
                    Id = 2,
                    Nome = "Técnico N1",
                    Email = "tec1@sistema.com",
                    Login = "tec1",
                    Senha = "123",
                    Perfil = "Técnico"
                },
                new Usuario
                {
                    Id = 3,
                    Nome = "Colaborador Demo",
                    Email = "user@sistema.com",
                    Login = "user",
                    Senha = "123",
                    Perfil = "Colaborador"
                }
            );
        }
    }
}
