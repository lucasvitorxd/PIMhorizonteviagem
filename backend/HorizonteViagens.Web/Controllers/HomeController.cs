using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HorizonteViagens.Negocio;
using HorizonteViagens.Web.Models;

namespace HorizonteViagens.Web.Controllers;

// Página inicial, política de privacidade e tela de erro do site.
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PortalNegocio _portal;

    public HomeController(ILogger<HomeController> logger, PortalNegocio portal)
    {
        _logger = logger;
        _portal = portal;
    }

    // GET /   (página inicial com os combos publicados)
    public IActionResult Index()
    {
        // A página inicial mostra o mesmo catálogo que a Api entrega ao app,
        // com o preço calculado pelo Negocio para 2 adultos (o padrão da busca).
        var itens = _portal.Vitrine(destino: null, categoria: null, viajantes: 2, ordem: null);
        return View(VitrineViewModel.De(itens, null, null, 2, null));
    }

    // GET /Home/Privacy
    public IActionResult Privacy() => View();

    // Tela mostrada quando acontece um erro inesperado fora do ambiente de desenvolvimento.
    // ResponseCache desligado para o navegador não guardar a página de erro.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
