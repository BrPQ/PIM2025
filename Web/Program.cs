using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PIMIIIWeb_SQL_Login.Data;
using PIMIIIWeb_SQL_Login.Models;

// Cria o "Construtor" da aplicação Web.
// O 'args' contém argumentos de linha de comando (se houver).
var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DE BANCO DE DADOS (Contexto Local)
// Tenta pegar a string de conexão do arquivo 'appsettings.json'.
// O operador '??' (Null Coalescing) funciona como um backup:
// Se não achar no JSON, usa a string hardcoded logo em seguida.
// Isso é ótimo para evitar que o projeto não rode na banca por falta de config.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-0962ELI;Database=GestaoChamadosDb;Trusted_Connection=True;TrustServerCertificate=True;";

// Adiciona o DbContext ao container de Injeção de Dependência.
// Isso permite que a gente peça 'AppDbContext' nos construtores das Pages.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. CONFIGURAÇÃO DE SERVIÇOS HTTP
// AddHttpClient: Registra a fábrica de clientes HTTP (IHttpClientFactory).
// Usamos isso para criar o 'client' que vai chamar a API externa de forma eficiente.
builder.Services.AddHttpClient();

// AddHttpContextAccessor: Permite acessar o contexto HTTP (Sessão, Cookies, IP)
// em classes que não são Controllers (como nossos Services ou Models).
builder.Services.AddHttpContextAccessor();

// 3. CONFIGURAÇÃO DE SESSÃO (Memória Temporária)
// A Sessão é onde guardaremos o Token JWT que veio da API.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // A sessão dura 8 horas (turno de trabalho).
    options.Cookie.HttpOnly = true; // Segurança: JavaScript do navegador não consegue ler esse cookie.
    options.Cookie.IsEssential = true; // O cookie é gravado mesmo se o usuário não aceitar cookies de marketing.
});

// 4. RAZOR PAGES E REGRAS DE ACESSO (Conventions)
// Aqui definimos a segurança baseada na estrutura de pastas.
builder.Services.AddRazorPages(options =>
{
    // Regra Global: A pasta raiz "/" (todo o site) exige a política "AcessoGeral".
    // Ou seja, ninguém entra no site sem estar logado.
    options.Conventions.AuthorizeFolder("/", "AcessoGeral");

    // Exceção: A página "/Login" é pública (AllowAnonymous).
    // Se não fizéssemos isso, o usuário entraria num loop infinito de redirecionamento (Login -> Login -> Login).
    options.Conventions.AllowAnonymousToPage("/Login");

    // Regra Específica: A página "/GestaoUsuarios" exige a política "AcessoGestao".
    // Só gestores e admins entram nela.
    options.Conventions.AuthorizePage("/GestaoUsuarios", "AcessoGestao");
});

// 5. AUTENTICAÇÃO (Cookies)
// Define que o site usa Cookies para saber quem está logado.
// Diferente da API (que é Stateless/JWT), o site precisa lembrar do usuário entre os cliques (Stateful).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login"; // Se tentar acessar algo restrito sem logar, joga pra cá.
        options.AccessDeniedPath = "/Login"; // Se tiver logado mas sem permissão (ex: Técnico tentando ver Gestão), joga pra cá.
    });

// 6. AUTORIZAÇÃO (Políticas/Policies)
// Aqui criamos os "Grupos de Acesso" lógicos baseados nas Roles.
builder.Services.AddAuthorization(options =>
{
    // Política Geral: Basta ter um desses cargos para entrar no site.
    options.AddPolicy("AcessoGeral", policy =>
        policy.RequireRole("Admin", "Gestor", "Tecnico"));

    // Política Restrita: Apenas chefia. Técnicos ficam de fora.
    options.AddPolicy("AcessoGestao", policy =>
        policy.RequireRole("Admin", "Gestor"));
});

// --- FIM DA CONFIGURAÇÃO (BUILD) ---
var app = builder.Build();

// 7. PIPELINE DE MIDDLEWARE (O Túnel de Segurança)
// A ordem dessas linhas é CRUCIAL. Uma requisição passa por cada uma delas em sequência.

// Em produção, usa tratamento de exceção amigável e HSTS (HTTPS estrito).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Redireciona automaticamente de HTTP para HTTPS.
app.UseHttpsRedirection();

// Permite servir arquivos estáticos (imagens, CSS, JS) da pasta wwwroot.
// Está no topo para ser rápido: se for uma imagem, entrega logo e não perde tempo com banco de dados.
app.UseStaticFiles();

// Define o sistema de rotas (para saber qual página chamar).
app.UseRouting();

// Ativa a Sessão.
// IMPORTANTE: Deve vir ANTES da Autenticação, pois a autenticação pode precisar ler dados da sessão.
app.UseSession();

// [Segurança] Quem é você? (Lê o Cookie de Auth).
app.UseAuthentication();

// [Segurança] O que você pode fazer? (Verifica as Policies configuradas acima).
app.UseAuthorization();

// Mapeia as Razor Pages (.cshtml) para as rotas da URL.
app.MapRazorPages();

// 8. AUTO-INICIALIZAÇÃO DO BANCO LOCAL
// Este bloco garante que o banco de dados local (para o login do site) exista.
// Se não existir, ele cria as tabelas e insere o usuário Admin padrão (definido no AppDbContext).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Inicia o servidor Web.
app.Run();