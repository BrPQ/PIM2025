using GestaoChamados.Data;
using GestaoChamados.Hubs; // Adicione este using para referenciar a pasta Hubs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Configuração dos Serviços ---

// 1. Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(connectionString));

// 2. Controllers e HttpClient
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// 3. Autenticação JWT (O "Segurança")
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
    };
});
builder.Services.AddAuthorization();


// 4. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. SignalR (NOVA LINHA ADICIONADA AQUI)
builder.Services.AddSignalR();


var app = builder.Build();

// --- Configuração do Pipeline HTTP ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// A ORDEM É CRUCIAL AQUI
app.UseAuthentication(); // 1º: Verifica o crachá
app.UseAuthorization();  // 2º: Verifica se o crachá dá permissão

app.MapControllers();

// MAPEAMENTO DO HUB SIGNALR (NOVA LINHA ADICIONADA AQUI)
// Isso cria o "endereço" para a comunicação em tempo real
app.MapHub<TicketHub>("/ticketHub");
app.MapHub<ChatHub>("/chathub");


app.Run();