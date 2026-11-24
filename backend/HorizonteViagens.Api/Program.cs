// Cada entidade nova ganha duas linhas aqui, uma de repositório e uma de negócio:
// builder.Services.AddScoped<IIngressoRepositorio, IngressoRepositorio>();
// builder.Services.AddScoped<IngressoNegocio>();
// --------------------------------------------

using HorizonteViagens.Api;
using HorizonteViagens.Negocio;
using HorizonteViagens.Repositorio;
using HorizonteViagens.Repositorio.Contexto;
using HorizonteViagens.Repositorio.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string não encontrada.");

var conexaoInfo = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);

Console.WriteLine($"SQL SERVER UTILIZADO: {conexaoInfo.DataSource}");
Console.WriteLine($"BANCO UTILIZADO: {conexaoInfo.InitialCatalog}");

builder.Services.AddDbContext<AppDbContext>(
    opcoes => opcoes.UseSqlServer(connectionString));

builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<ClienteNegocio>();
builder.Services.AddScoped<IAdministradorRepositorio, AdministradorRepositorio>();
builder.Services.AddScoped<AdministradorNegocio>();
builder.Services.AddScoped<IComboRepositorio, ComboRepositorio>();
builder.Services.AddScoped<ComboNegocio>();
builder.Services.AddScoped<ICompraRepositorio, CompraRepositorio>();
builder.Services.AddScoped<CompraNegocio>();

builder.Services.AddControllers();

// Health check: o endereço /health responde se a Api está de pé (Aula 07 de Desenvolvimento Web).
builder.Services.AddHealthChecks();

// ---------- Login do administrador (token JWT) ----------
// A Api não usa cookie, porque quem chama é o app Flutter, não um navegador.
// No lugar do cookie, o login devolve um TOKEN: um texto assinado com a chave
// secreta do appsettings.json. O app manda esse token em toda chamada, no
// cabeçalho "Authorization: Bearer <token>". Se alguém alterar o token,
// a assinatura deixa de bater e a Api recusa (401).
// A CHAVE é um segredo: não fica no appsettings.json, que vai para o Git
// (Aula 07 de Desenvolvimento Web, "Onde cada segredo mora").
// Em desenvolvimento ela mora no User Secrets, fora da pasta do projeto:
//   dotnet user-secrets set "Jwt:Chave" "uma-frase-longa-e-secreta-com-32-caracteres-ou-mais" --project HorizonteViagens.Api
// No servidor, numa variável de ambiente chamada Jwt__Chave.
var jwt = builder.Configuration.GetSection("Jwt");
var chave = jwt["Chave"];
if (string.IsNullOrWhiteSpace(chave))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Chave 'Jwt:Chave' não configurada. Defina a variável de ambiente Jwt__Chave no servidor.");

    // Só em desenvolvimento: sem User Secrets configurado, gera uma chave aleatória
    // ao iniciar, para a Api subir em qualquer máquina (inclusive na apresentação).
    // Efeito colateral: ao reiniciar a Api, os tokens antigos deixam de valer
    // e é preciso fazer login de novo.
    chave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    builder.Configuration["Jwt:Chave"] = chave; // o GeradorDeToken lê daqui
}

builder.Services.AddSingleton<GeradorDeToken>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        // A cada requisição, confere se a conta dona do token continua ativa:
        // administrador ou cliente desativado perde o acesso na hora, mesmo
        // com um token que ainda não venceu. Quem consulta é o Negocio.
        opcoes.Events = new JwtBearerEvents
        {
            OnTokenValidated = contexto =>
            {
                if (!ContaAtiva(contexto.Principal!, contexto.HttpContext.RequestServices))
                    contexto.Fail("Conta indisponível.");

                return Task.CompletedTask;
            }
        };

        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Emissor"],
            ValidateAudience = true,
            ValidAudience = jwt["Publico"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            ClockSkew = TimeSpan.FromSeconds(30), // tolerância pequena para relógios diferentes
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
// CORS: mantém as origens configuradas e, somente em Development,
// permite o Flutter Web no localhost, independentemente da porta aleatória.
var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(opcoes =>
    opcoes.AddDefaultPolicy(politica =>
    {
        politica.WithOrigins(origens)
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
        {
            politica.SetIsOriginAllowed(origem =>
            {
                if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri))
                    return false;

                var origemConfigurada = origens.Contains(origem, StringComparer.OrdinalIgnoreCase);
                var localhost = uri.Scheme == Uri.UriSchemeHttp &&
                    (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                     uri.Host == "127.0.0.1" || uri.Host == "[::1]");

                return origemConfigurada || localhost;
            });
        }
    }));
// ---------------------------------------------------------

// OpenAPI: gera a documentação da API (quais endpoints existem, o que cada um
// recebe e devolve) num arquivo JSON padrão, em /openapi/v1.json.
// É esse documento que o grupo usa para mostrar ao app Flutter o que a API oferece.
builder.Services.AddOpenApi();

// Portal do cliente (reservas, favoritos, documentos): repositório e negócio,
// as mesmas duas linhas de sempre.
builder.Services.AddScoped<IPortalRepositorio, PortalRepositorio>();
builder.Services.AddScoped<PortalNegocio>();

// Pagamento de TESTE do Mercado Pago. O token fica no User Secrets
// (Pagamento:Token). Sem ele, o sistema funciona e só o pagamento fica indisponível.
builder.Services.AddSingleton(new PagamentoConfiguracao(
    builder.Configuration["Pagamento:Token"] ?? "",
    builder.Configuration["Pagamento:Retorno"] ?? ""));
builder.Services.AddHttpClient<PagamentoNegocio>(cliente => cliente.Timeout = TimeSpan.FromSeconds(20));

// Erros inesperados viram uma resposta curta, sem mostrar SQL nem detalhes internos.
builder.Services.AddControllers(opcoes => opcoes.Filters.Add<HorizonteViagens.Api.Filtros.ErrosFiltro>());

// Limite de tentativas de login: no máximo 20 por minuto para cada IP,
// para dificultar quem tenta adivinhar senhas.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = 429;
    opcoes.AddPolicy("login", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();
// Preparar o banco é um comando separado, nunca roda num acesso comum:
//   dotnet run --project HorizonteViagens.Api -- --preparar-banco
// Acrescente --acessos-teste para criar as contas de teste (só em Development).
if (args.Contains("--preparar-banco"))
{
    using var escopo = app.Services.CreateScope();
    Preparacao.Inicializar(
        escopo.ServiceProvider.GetRequiredService<IPortalRepositorio>(),
        escopo.ServiceProvider.GetRequiredService<IAdministradorRepositorio>(),
        escopo.ServiceProvider.GetRequiredService<IClienteRepositorio>(),
        acessosTeste: args.Contains("--acessos-teste"),
        desenvolvimento: app.Environment.IsDevelopment());
    return;
}

if (app.Environment.IsDevelopment())
{
    // Publica o documento só durante o desenvolvimento.
    app.MapOpenApi();

    // Scalar: tela gráfica que lê o documento OpenAPI acima e permite ver e
    // testar cada endpoint pelo navegador, em /scalar.
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Primeiro descobre QUEM é (lê o token), depois confere se PODE entrar.
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Pergunta ao Negocio se a conta do token (administrador ou cliente) ainda está ativa.
static bool ContaAtiva(ClaimsPrincipal usuario, IServiceProvider servicos)
{
    if (!int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        return false;

    if (usuario.IsInRole("Administrador"))
        return servicos.GetRequiredService<AdministradorNegocio>().ContaAtiva(id);

    if (usuario.IsInRole("Cliente"))
        return servicos.GetRequiredService<ClienteNegocio>().ContaAtiva(id);

    return false;
}

// Permite que os testes de integração subam a Api em memória (WebApplicationFactory).
public partial class Program { }
