using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HorizonteViagens.Negocio;
using HorizonteViagens.Repositorio;
using HorizonteViagens.Repositorio.Contexto;
using HorizonteViagens.Repositorio.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem do appsettings.json, nunca do código-fonte.
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não encontrada no appsettings.json.");

// ---------- Injeção de dependência ----------
// Esta é a LISTA DE MONTAGEM do sistema. Cada linha ensina o ASP.NET Core a
// criar uma peça. Depois, quando um controller pede um ClienteNegocio no
// construtor, o ASP.NET monta a cadeia inteira sozinho:
//
//   ClientesController  pede  ClienteNegocio
//   ClienteNegocio      pede  IClienteRepositorio  -> recebe um ClienteRepositorio
//   ClienteRepositorio  pede  AppDbContext         -> recebe com a connection string
//
// Ninguém escreve "new" para essas classes. Explicação passo a passo, com os
// erros mais comuns: INJECAO-DE-DEPENDENCIA.md, na pasta da solution.
//
// ATENÇÃO: este bloco é IGUAL no Program.cs da Api. Mudou aqui, mude lá também.

// 1) O banco: "quando alguém pedir um AppDbContext, crie um usando SQL Server
//    e esta connection string".
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlServer(connectionString));

// 2) O repositório: "quando alguém pedir um IClienteRepositorio (a interface),
//    entregue um ClienteRepositorio (a classe)".
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();

// 3) O negócio: "quando alguém pedir um ClienteNegocio, crie um".
builder.Services.AddScoped<ClienteNegocio>();

// 4) O administrador: repositório e negócio, as mesmas duas linhas de sempre.
builder.Services.AddScoped<IAdministradorRepositorio, AdministradorRepositorio>();
builder.Services.AddScoped<AdministradorNegocio>();
// Reuso do mesmo catálogo de combos na aplicação MVC.
builder.Services.AddScoped<IComboRepositorio, ComboRepositorio>();
builder.Services.AddScoped<ComboNegocio>();
builder.Services.AddScoped<ICompraRepositorio, CompraRepositorio>();
builder.Services.AddScoped<CompraNegocio>();

// AddScoped = um objeto novo para cada requisição (cada clique na Web, cada chamada
// da API), jogado fora quando a resposta sai. O AddDbContext funciona do mesmo jeito:
// cada requisição ganha o seu contexto, e a conexão é fechada quando ele é descartado.

// Cada entidade nova ganha duas linhas aqui, uma de repositório e uma de negócio:
// builder.Services.AddScoped<IIngressoRepositorio, IngressoRepositorio>();
// builder.Services.AddScoped<IngressoNegocio>();
// --------------------------------------------

// AutoValidateAntiforgeryToken: todo POST de formulário precisa trazer o token
// anti-falsificação (o @Html.AntiForgeryToken / asp-* dos formulários já colocam).
builder.Services.AddControllersWithViews(opcoes =>
    opcoes.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

// Health check: o endereço /health responde se o site está de pé.
builder.Services.AddHealthChecks();

// ---------- Login do administrador (cookie) ----------
// Depois que o administrador acerta e-mail e senha, o navegador recebe um
// "crachá" (cookie). A cada clique, o crachá volta junto, e o ASP.NET sabe
// quem é. Quem não tem crachá e tenta abrir uma tela com [Authorize] é
// mandado para a tela de login.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Conta/Login";          // sem crachá: vai para cá
        opcoes.AccessDeniedPath = "/Conta/Negado";  // crachá sem permissão: vai para cá
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;
        opcoes.Cookie.HttpOnly = true;              // o JavaScript da página não lê o crachá
        opcoes.Cookie.SameSite = SameSiteMode.Lax;
        opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        // A cada clique, confere se a conta do crachá continua ativa.
        // Conta desativada ou vencida é mandada de volta para o login.
        opcoes.Events.OnValidatePrincipal = contexto =>
        {
            if (!ContaAtiva(contexto.Principal!, contexto.HttpContext.RequestServices))
                contexto.RejectPrincipal();

            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
// ------------------------------------------------------

// Portal do cliente: repositório e negócio, as mesmas duas linhas de sempre.
builder.Services.AddScoped<IPortalRepositorio, PortalRepositorio>();
builder.Services.AddScoped<PortalNegocio>();

// Pagamento de TESTE do Mercado Pago (token no User Secrets, opcional).
builder.Services.AddSingleton(new PagamentoConfiguracao(
    builder.Configuration["Pagamento:Token"] ?? "",
    builder.Configuration["Pagamento:Retorno"] ?? ""));
builder.Services.AddHttpClient<PagamentoNegocio>(cliente => cliente.Timeout = TimeSpan.FromSeconds(20));

builder.Services.AddControllers(opcoes => opcoes.Filters.Add<HorizonteViagens.Web.Filtros.ErrosFiltro>());

// Limite de tentativas de login: no máximo 20 por minuto para cada IP.
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
// Preparar o banco é um comando separado (mesmo da Api):
//   dotnet run --project HorizonteViagens.Web -- --preparar-banco
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Tudo em português do Brasil: R$ 1.299,90 e datas dd/mm/aaaa,
// tanto ao mostrar quanto ao ler o que o usuário digitou no formulário.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("pt-BR")
    .AddSupportedCultures("pt-BR")
    .AddSupportedUICultures("pt-BR"));
app.UseHttpsRedirection();
app.UseRouting();

// A ordem importa: primeiro descobre QUEM é (UseAuthentication),
// depois confere se PODE entrar (UseAuthorization).
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    ;


app.MapHealthChecks("/health");

app.Run();

// Pergunta ao Negocio se a conta do crachá (administrador ou cliente) ainda está ativa.
static bool ContaAtiva(System.Security.Claims.ClaimsPrincipal usuario, IServiceProvider servicos)
{
    var idTexto = usuario.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(idTexto, out var id))
        return false;

    if (usuario.IsInRole("Administrador"))
        return servicos.GetRequiredService<AdministradorNegocio>().ContaAtiva(id);

    if (usuario.IsInRole("Cliente"))
        return servicos.GetRequiredService<ClienteNegocio>().ContaAtiva(id);

    return false;
}

// Permite que os testes de integração subam o site em memória (WebApplicationFactory).
public partial class Program { }
