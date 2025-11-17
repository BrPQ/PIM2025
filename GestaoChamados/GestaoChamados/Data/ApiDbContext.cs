// EM: Data/ApiDbContext.cs
using GestaoChamados.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoChamados.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Anexo> Anexos { get; set; }
        public DbSet<Mensagem> Mensagens { get; set; }
    }
}