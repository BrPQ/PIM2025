using GestaoChamados.Data;
using GestaoChamados.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Cria o "Construtor" da aplicação web.
var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DE BANCO DE DADOS
// Pega a string de conexão lá do arquivo 'appsettings.json'.
// É uma boa prática não deixar a senha do banco hardcoded aqui no código C#.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Injeção de Dependência (DI):
// Avisa ao sistema: "Sempre que alguém pedir um ApiDbContext, entregue uma conexão SQL Server pronta".
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(connectionString));

// Adiciona o suporte a Controllers (para criar as rotas da API).
builder.Services.AddControllers();

// Adiciona o HttpClient (usado internamente, talvez para chamar a IA).
builder.Services.AddHttpClient();

// 2. CONFIGURAÇÃO DE SEGURANÇA (JWT)
// Aqui definimos como o sistema sabe quem é quem.
builder.Services.AddAuthentication(options =>
{
    // Define que o padrão de autenticação é via "Bearer Token" (aquele token longo gerado no login).
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Regras de validação do crachá (Token):
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Verifica se foi ESTA API que gerou o token (evita tokens falsos).
        ValidateAudience = true, // Verifica se o token serve para este site.
        ValidateLifetime = true, // Verifica se o token já venceu (ex: passou das 8 horas).
        ValidateIssuerSigningKey = true, // Verifica a assinatura digital (criptografia).

        // Lê as chaves secretas do appsettings.json
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        // A Chave Simétrica é o segredo mais importante. Se vazar, clonam os tokens.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
    };
});

// Habilita o sistema de permissões (Roles: Admin, Tecnico, etc).
builder.Services.AddAuthorization();

// 3. DOCUMENTAÇÃO (SWAGGER)
// Ferramentas para gerar aquela página de teste automática da API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. TEMPO REAL (SIGNALR)
// Adiciona o serviço que gerencia os WebSockets.
builder.Services.AddSignalR();


// --- FIM DA CONFIGURAÇÃO (BUILD) ---
// Agora o aplicativo é efetivamente construído.
var app = builder.Build();

// 5. PIPELINE DE REQUISIÇÃO (MIDDLEWARES)
// Aqui definimos a ordem que as coisas acontecem quando chega um pedido da internet.

// Se estiver rodando no PC do desenvolvedor (Development), mostra o Swagger.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redireciona HTTP para HTTPS automaticamente (segurança).
app.UseHttpsRedirection();

// *** A ORDEM AQUI É CRUCIAL ***
// Primeiro verifica QUEM é a pessoa (Authentication).
app.UseAuthentication();
// Depois verifica O QUE ela pode fazer (Authorization).
app.UseAuthorization();

// Mapeia os Controllers (AnexosController, AuthController, etc).
app.MapControllers();

// 6. ROTAS DO SIGNALR (WEBSOCKETS)
// Aqui definimos os "endereços" para conectar o Chat e os Tickets em tempo real.
// O Front-end vai conectar em "https://localhost.../ticketHub".
app.MapHub<TicketHub>("/ticketHub");
app.MapHub<ChatHub>("/chathub");

// Inicia o servidor e fica escutando as requisições.
app.Run();