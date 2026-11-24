using HorizonteViagens.Modelos;
using HorizonteViagens.Negocio;
using HorizonteViagens.Web.Models;
using HorizonteViagens.Repositorio.Contexto;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using Xunit;

namespace HorizonteViagens.Testes;

public class VitrineTests
{
    // Renderiza Razor para conferir que mudar os viajantes consulta o preço correto.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public async Task Mvc_ExibeOrcamentoDoNegocio(int viajantes)
    {
        using var app = new AplicacaoTeste<HorizonteViagens.Web.Controllers.HomeController>();
        app.Preparar();
        using var escopo = app.Services.CreateScope();
        var combo = escopo.ServiceProvider.GetRequiredService<AppDbContext>().Combos.Single();
        var esperado = Precificacao.Calcular(combo, viajantes);
        using var http = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var detalhe = WebUtility.HtmlDecode(await http.GetStringAsync($"/Combos/Detalhes/{combo.Id}?viajantes={viajantes}"));
        var vitrine = WebUtility.HtmlDecode(await http.GetStringAsync($"/Combos?viajantes={viajantes}&ordem=preco"));
        var preco = esperado.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        Assert.Contains(preco, detalhe);
        Assert.Contains(preco, vitrine);
        Assert.Contains("data-viajantes", detalhe);
        Assert.Contains("aria-controls=\"menu-principal\"", vitrine);
    }

    // Quantidades pares e ímpares precisam chegar à view com o mesmo orçamento do negócio.
    [Theory]
    [InlineData(1, 3500, 3500)]
    [InlineData(2, 4000, 2000)]
    [InlineData(3, 7500, 2500)]
    [InlineData(6, 12000, 2000)]
    public void Card_RecebePrecoPronto(int viajantes, decimal total, decimal porPessoa)
    {
        var combo = new Combo { Valor = 300, DiariaQuarto = 500, Noites = 6, ValorExperiencias = 200 };
        var item = new ItemVitrine(combo, Precificacao.Calcular(combo, viajantes), false);
        var card = ComboCardViewModel.De(item);
        Assert.Equal(total, card.Total);
        Assert.Equal(porPessoa, card.PorPessoa);
        Assert.Equal(viajantes, card.Viajantes);
    }
}
