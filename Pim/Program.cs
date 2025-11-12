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

// --- Serviços de API e Sessão (Correto) ---
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- CONFIGURANDO AS PÁGINAS (REGRA 2) ---
builder.Services.AddRazorPages(options =>
{
    // 1. REGRA GERAL: Tranca o site todo e exige a política "AcessoGeral"
    options.Conventions.AuthorizeFolder("/", "AcessoGeral");

    // 2. EXCEÇÃO 1: A página de Login é pública
    options.Conventions.AllowAnonymousToPage("/Login");

    // 3. EXCEÇÃO 2: A página de Gestão de Usuários exige uma política mais forte
    options.Conventions.AuthorizePage("/GestaoUsuarios", "AcessoGestao");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });

// --- DEFININDO AS POLÍTICAS DE ACESSO (REGRA 2) ---
builder.Services.AddAuthorization(options =>
{
    // Política para o site em geral (Admin, Gestor, Tecnico)
    options.AddPolicy("AcessoGeral", policy =>
        policy.RequireRole("Admin", "Gestor", "Tecnico"));

    // Política específica para a página de Gestão de Usuários (Admin, Gestor)
    options.AddPolicy("AcessoGestao", policy =>
        policy.RequireRole("Admin", "Gestor"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

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