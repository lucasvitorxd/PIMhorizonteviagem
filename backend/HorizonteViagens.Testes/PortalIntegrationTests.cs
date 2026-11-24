using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HorizonteViagens.Modelos;
using HorizonteViagens.Negocio;
using HorizonteViagens.Repositorio.Contexto;
using HorizonteViagens.Repositorio.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HorizonteViagens.Testes;

// Testes de integração: sobem a Api (ou a Web) inteira em memória e fazem
// chamadas HTTP de verdade (controllers, autorização, Razor, banco e regras).
// O banco aqui é SQLite em memória, só para os testes; o sistema de verdade
// continua usando SQL Server e as migrations.

// SQLite não tem "rowversion" automático como o SQL Server, então este
// contexto de teste gera um valor novo a cada gravação, para imitar o comportamento.
public class BancoTeste : AppDbContext
{
    public BancoTeste(DbContextOptions<AppDbContext> opcoes) : base(opcoes)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);
        modelo.Entity<Cliente>().Property(c => c.DataCadastro).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelo.Entity<Combo>().Property(c => c.RowVersion).ValueGeneratedNever();
        modelo.Entity<Compra>().Property(c => c.RowVersion).ValueGeneratedNever();
    }

    public override int SaveChanges()
    {
        foreach (var entrada in ChangeTracker.Entries<Combo>().Where(e => e.State is EntityState.Added or EntityState.Modified))
            entrada.Entity.RowVersion = Guid.NewGuid().ToByteArray();

        foreach (var entrada in ChangeTracker.Entries<Compra>().Where(e => e.State is EntityState.Added or EntityState.Modified))
            entrada.Entity.RowVersion = Guid.NewGuid().ToByteArray();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancelamento = default) =>
        Task.FromResult(SaveChanges());
}

// Sobe a aplicação (T = qualquer classe da Api ou da Web) trocando o SQL Server pelo SQLite.
public class AplicacaoTeste<T> : WebApplicationFactory<T> where T : class
{
    private readonly SqliteConnection _conexao = new("Data Source=:memory:");

    public AplicacaoTeste()
    {
        _conexao.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder construtor)
    {
        construtor.UseEnvironment("Development");
        construtor.ConfigureServices(servicos =>
        {
            var registrosDoBanco = servicos
                .Where(d => d.ServiceType == typeof(AppDbContext) ||
                            d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                            d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();

            foreach (var registro in registrosDoBanco)
                servicos.Remove(registro);

            // Os testes não podem ler a chave de proteção de dados do Windows de
            // quem está rodando: cada execução usa uma chave temporária, só em memória.
            servicos.AddSingleton<IDataProtectionProvider>(_ => new EphemeralDataProtectionProvider());

            servicos.AddScoped<AppDbContext>(_ =>
                new BancoTeste(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options));
        });
    }

    // Cria as tabelas e cadastra um cliente e um combo reservável (10 vagas).
    public void Preparar()
    {
        using var escopo = Services.CreateScope();
        var banco = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        banco.Database.EnsureCreated();

        banco.Clientes.Add(new Cliente
        {
            Nome = "Cliente Teste",
            Email = "cliente@teste.invalid",
            Cpf = "52998224725",
            Celular = "11999999999",
            SenhaHash = SenhaHasher.Gerar("TesteSeguro!2026"),
            Ativo = true
        });

        banco.Combos.Add(new Combo
        {
            Nome = "Ubatuba + experiências",
            Destino = "Ubatuba",
            Evento = "Surf",
            Categoria = "Econômico",
            Imagem = "coast",
            Valor = 300,
            DiariaQuarto = 550,
            ValorExperiencias = 350,
            Noites = 6,
            Vagas = 10,
            Publicado = true,
            PrecoConfirmado = true,
            Partida = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        });

        banco.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _conexao.Dispose();
    }
}

public class PortalIntegrationTests
{
    // ---------- Preço ----------

    // Dois adultos dividem um quarto: 3 pessoas = 2 quartos, 6 pessoas = 3 quartos.
    [Theory]
    [InlineData(1, 1, 3950)]
    [InlineData(2, 1, 4600)]
    [InlineData(3, 2, 8550)]
    [InlineData(6, 3, 13800)]
    public void PrecoPorGrupo_ConsideraQuartosInteiros(int pessoas, int quartos, decimal esperado)
    {
        var combo = new Combo { Valor = 300, DiariaQuarto = 550, ValorExperiencias = 350, Noites = 6 };

        var orcamento = Precificacao.Calcular(combo, pessoas);

        Assert.Equal(quartos, orcamento.Quartos);
        Assert.Equal(esperado, orcamento.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Preco_RecusaQuantidadeInvalida(int pessoas) =>
        Assert.Throws<RegraDeNegocioException>(() => Precificacao.Calcular(new Combo(), pessoas));

    // ---------- Compra simples (POST api/compras) ----------

    [Fact]
    public async Task Api_CompraSimples_SoComboId_DepoisSemVagaRetorna400()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        DeixarComUmaVaga(aplicacao);
        using var http = aplicacao.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginCliente(http);

        var primeira = await http.PostAsJsonAsync("/api/compras", new { comboId = 1 });
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);

        var segunda = await http.PostAsJsonAsync("/api/compras", new { comboId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
        var erro = (await segunda.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("erro").GetString();
        Assert.Equal("Não há vagas disponíveis para este combo.", erro);
    }

    // ---------- Reservas do portal ----------

    [Fact]
    public async Task Api_ReservaIdempotente_TotalAutoritativo_Estoque()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var http = aplicacao.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginCliente(http);

        // Total diferente do calculado pelo servidor é recusado.
        var entrada = new ReservaEntrada { ComboId = 1, Viajantes = 3, TotalEsperado = 1, RequisicaoId = Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/portal/reservas", entrada)).StatusCode);

        // Com o total certo, reserva. Repetindo o mesmo pedido, volta a MESMA reserva.
        entrada.TotalEsperado = 8550;
        var primeira = await http.PostAsJsonAsync("/api/portal/reservas", entrada);
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        var id = (await primeira.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var repetida = await http.PostAsJsonAsync("/api/portal/reservas", entrada);
        Assert.Equal(id, (await repetida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());

        using (var escopo = aplicacao.Services.CreateScope())
        {
            var banco = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(7, banco.Combos.Single().Vagas);
            Assert.Single(banco.Compras);
        }

        // Mesmo identificador com outro conteúdo é recusado.
        entrada.Viajantes = 2;
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/portal/reservas", entrada)).StatusCode);
    }

    [Fact]
    public async Task Api_ClienteNaoAdmin_NemReservaDeOutro()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var http = aplicacao.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/portal/reservas")).StatusCode);
        await LoginCliente(http);

        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/portal/reservas/987/documentos")).StatusCode);

        var documento = new DocumentoEntrada { Titulo = "Teste", Url = "https://example.invalid", Codigo = "XX" };
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/portal/documentos/1", documento)).StatusCode);

        // Favoritar duas vezes não duplica.
        await http.PutAsync("/api/portal/favoritos/1", null);
        await http.PutAsync("/api/portal/favoritos/1", null);
        Assert.Equal(new[] { 1 }, await http.GetFromJsonAsync<int[]>("/api/portal/favoritos"));
    }

    [Fact]
    public async Task Api_ContaDesativada_InvalidaTokenEmitido()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var http = aplicacao.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginCliente(http);

        using (var escopo = aplicacao.Services.CreateScope())
        {
            var banco = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            banco.Clientes.Single().Ativo = false;
            banco.SaveChanges();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/portal/perfil")).StatusCode);
    }

    [Fact]
    public async Task Api_CancelamentoDevolveVagasApenasUmaVez()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var http = aplicacao.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginCliente(http);

        var entrada = new ReservaEntrada { ComboId = 1, Viajantes = 2, TotalEsperado = 4600, RequisicaoId = Guid.NewGuid() };
        var resposta = await http.PostAsJsonAsync("/api/portal/reservas", entrada);
        var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsync($"/api/portal/reservas/{id}/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsync($"/api/portal/reservas/{id}/cancelar", null)).StatusCode);

        using var escopo = aplicacao.Services.CreateScope();
        Assert.Equal(10, escopo.ServiceProvider.GetRequiredService<AppDbContext>().Combos.Single().Vagas);
    }

    // ---------- Site (Web MVC) ----------

    [Fact]
    public async Task Web_Razor_Login_CrudCombo_Csrf()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Web.Controllers.HomeController>();
        aplicacao.Preparar();
        using var http = aplicacao.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        var inicio = await http.GetStringAsync("/");
        Assert.Contains("Ubatuba", inicio);
        Assert.Contains("4.600", inicio);
        Assert.Equal(HttpStatusCode.Redirect, (await http.GetAsync("/Combos/Gerenciar")).StatusCode);

        var token = TokenDoFormulario(await http.GetStringAsync("/Conta/Login?admin=true"));
        var login = await http.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "Email", "admin@horizonteviagens.com" },
            { "Senha", "Horizonte@2026" },
            { "admin", "true" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        // POST sem o token anti-falsificação é recusado.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await http.PostAsync("/Combos/Criar", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);

        token = TokenDoFormulario(await http.GetStringAsync("/Combos/Criar"));
        var formulario = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "Nome", "Novo pacote" },
            { "Destino", "Recife" },
            { "Categoria", "Econômico" },
            { "Noites", "2" },
            { "Valor", "400,50" },
            { "DiariaQuarto", "200,00" },
            { "ValorExperiencias", "50,00" },
            { "Vagas", "5" },
            { "Imagem", "beach" },
            { "Origem", "São Paulo" },
            // Evento, descrição e inclusões passaram a ser obrigatórios no cadastro do combo.
            { "Evento", "Festival de verão" },
            { "Descricao", "Pacote de teste com passagem, hospedagem e evento." },
            { "Inclusoes", "Passagem\nHospedagem\nEvento" },
            { "Publicado", "true" }
        };

        var criar = await http.PostAsync("/Combos/Criar", new FormUrlEncodedContent(formulario));
        Assert.Equal(HttpStatusCode.Redirect, criar.StatusCode);
        Assert.Contains("Novo pacote", await http.GetStringAsync("/Combos/Gerenciar"));
    }

    // ---------- Pagamento ----------

    [Fact]
    public void Pagamento_RecusaValorOuModoReal_ExpiracaoNaoConfirma()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var escopo = aplicacao.Services.CreateScope();
        var portal = escopo.ServiceProvider.GetRequiredService<PortalNegocio>();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IPortalRepositorio>();
        var banco = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        var reserva = portal.Reservar(1, new ReservaEntrada { ComboId = 1, Viajantes = 2, TotalEsperado = 4600, RequisicaoId = Guid.NewGuid() });
        var referencia = $"HV-{reserva.Id}-{reserva.RequisicaoId:N}";

        // Valor errado ou pagamento de conta real: recusado.
        Assert.Throws<InvalidOperationException>(() => repositorio.AplicarPagamento(reserva.Id, "42", "approved", 1, referencia, true));
        Assert.Throws<InvalidOperationException>(() => repositorio.AplicarPagamento(reserva.Id, "42", "approved", 4600, referencia, false));

        // Reserva vencida devolve as vagas uma vez só, e o pagamento atrasado vai para revisão.
        reserva.ExpiraEm = DateTime.UtcNow.AddMinutes(-1);
        banco.SaveChanges();
        repositorio.Expirar();
        repositorio.Expirar();
        Assert.Equal(10, banco.Combos.Single().Vagas);

        repositorio.AplicarPagamento(reserva.Id, "42", "approved", 4600, referencia, true);
        Assert.Equal("Revisão de pagamento", reserva.Status);
    }

    // ---------- Gestão pela Api ----------

    [Fact]
    public async Task Api_GestaoPages_ExigeAdmin_E_CrudPreservaVersao()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var http = aplicacao.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await LoginCliente(http);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/portal/gestao/reservas")).StatusCode);

        await LoginAdministrador(http);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/portal/gestao/reservas")).StatusCode);

        var dto = new HorizonteViagens.Modelos.Dtos.ComboSalvarDto
        {
            Nome = "Recife",
            Destino = "Recife",
            Categoria = "Econômico",
            Valor = 400,
            Noites = 2,
            Imagem = "beach",
            // Campos obrigatórios no cadastro do combo.
            Evento = "Festival de verão",
            Descricao = "Pacote de teste com passagem, hospedagem e evento.",
            Inclusoes = "Passagem\nHospedagem\nEvento"
        };

        var criado = await http.PostAsJsonAsync("/api/combos", dto);
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);

        var item = await criado.Content.ReadFromJsonAsync<JsonElement>();
        var id = item.GetProperty("id").GetInt32();
        dto.Versao = item.GetProperty("versao").GetString();
        dto.Nome = "Recife atualizado";

        // Primeira alteração com a versão certa passa; a segunda, com a versão velha, é recusada.
        Assert.Equal(HttpStatusCode.NoContent, (await http.PutAsJsonAsync($"/api/combos/{id}", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PutAsJsonAsync($"/api/combos/{id}", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/combos/{id}")).StatusCode);
    }

    // ---------- Preparação do banco ----------

    [Fact]
    public void Preparacao_MantemAdminOriginal_E_ContasDeTesteNaoExpiram()
    {
        using var aplicacao = new AplicacaoTeste<HorizonteViagens.Api.Controllers.PortalController>();
        aplicacao.Preparar();
        using var escopo = aplicacao.Services.CreateScope();
        var servicos = escopo.ServiceProvider;

        Preparacao.Inicializar(
            servicos.GetRequiredService<IPortalRepositorio>(),
            servicos.GetRequiredService<IAdministradorRepositorio>(),
            servicos.GetRequiredService<IClienteRepositorio>(),
            acessosTeste: true,
            desenvolvimento: true,
            aplicarMigrations: false);

        var administradores = servicos.GetRequiredService<AdministradorNegocio>();
        Assert.NotNull(administradores.Autenticar("admin@horizonteviagens.com", "Horizonte@2026"));

        var adminTeste = servicos.GetRequiredService<IAdministradorRepositorio>().ObterPorEmail("admin@horizonte.test");
        Assert.NotNull(adminTeste);
        Assert.Null(adminTeste!.AcessoAte);

        var clienteTeste = servicos.GetRequiredService<IClienteRepositorio>().ObterPorEmail("cliente@horizonte.test");
        Assert.NotNull(clienteTeste);
        Assert.Null(clienteTeste!.AcessoAte);
    }

    // ---------- Ajudantes ----------

    private static void DeixarComUmaVaga<T>(AplicacaoTeste<T> aplicacao) where T : class
    {
        using var escopo = aplicacao.Services.CreateScope();
        var banco = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        banco.Combos.Single().Vagas = 1;
        banco.SaveChanges();
    }

    private static async Task LoginCliente(HttpClient http)
    {
        var resposta = await http.PostAsJsonAsync("/api/autenticacao/login-cliente",
            new { email = "cliente@teste.invalid", senha = "TesteSeguro!2026" });
        await UsarToken(http, resposta);
    }

    private static async Task LoginAdministrador(HttpClient http)
    {
        var resposta = await http.PostAsJsonAsync("/api/autenticacao/login",
            new { email = "admin@horizonteviagens.com", senha = "Horizonte@2026" });
        await UsarToken(http, resposta);
    }

    private static async Task UsarToken(HttpClient http, HttpResponseMessage resposta)
    {
        resposta.EnsureSuccessStatusCode();
        var token = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Lê o token anti-falsificação escondido no formulário HTML.
    private static string TokenDoFormulario(string html) =>
        WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
}
