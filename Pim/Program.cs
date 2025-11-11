using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PIMIIIWeb_SQL_Login.Data;
using PIMIIIWeb_SQL_Login.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-0962ELI;Database=GestaoChamadosDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- NOSSAS MUDANÇAS COMEÇAM AQUI ---

// 1. Permite que o App faça requisições HTTP (para chamar a API)
builder.Services.AddHttpClient();

// 2. Permite que o App acesse o HttpContext (para ler/gravar na Sessão)
builder.Services.AddHttpContextAccessor();

// 3. Adiciona o serviço de Sessão (onde vamos guardar o Token JWT)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // Mesmo tempo do token
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- FIM DAS NOSSAS MUDANÇAS ---

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/LGPD");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- MAIS UMA MUDANÇA ---
// 4. Habilita o uso da Sessão
app.UseSession();
// --- FIM DA MUDANÇA ---

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// ensure db
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}


app.Run();