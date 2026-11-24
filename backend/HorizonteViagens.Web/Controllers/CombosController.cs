using HorizonteViagens.Modelos.Dtos;
using HorizonteViagens.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HorizonteViagens.Web.Models;

namespace HorizonteViagens.Web.Controllers;

// Vitrine pública de combos e o cadastro completo (CRUD) para o administrador.
// Usa o mesmo ComboNegocio da Api: "duas cascas sobre o mesmo miolo".
public class CombosController : Controller
{
    private readonly ComboNegocio _negocio;
    private readonly PortalNegocio _portal;

    public CombosController(ComboNegocio negocio, PortalNegocio portal)
    {
        _negocio = negocio;
        _portal = portal;
    }

    // ---------- Vitrine ----------

    // GET /Combos?destino=paris&categoria=Luxo&viajantes=2&ordem=preco
    // O controller só repassa os filtros: quem filtra, calcula e ordena é o PortalNegocio.
    [HttpGet]
    public IActionResult Index(string? destino, string? categoria, int viajantes = 2, string? ordem = null)
    {
        var itens = _portal.Vitrine(destino, categoria, viajantes, ordem);
        return View(VitrineViewModel.De(itens, destino, categoria, viajantes, ordem));
    }

    // GET /Combos/Detalhes/5?viajantes=3
    [HttpGet]
    public IActionResult Detalhes(int id, int viajantes = 2)
    {
        var item = _portal.Detalhe(id, viajantes);
        if (item is null)
            return NotFound();

        return View(ComboDetalheViewModel.De(item));
    }

    // ---------- Administrador ----------

    [Authorize(Roles = "Administrador")]
    public IActionResult Gerenciar() =>
        View(_negocio.Listar().Select(ComboDto.De).ToList());

    [Authorize(Roles = "Administrador")]
    public IActionResult Criar() =>
        View(new ComboSalvarDto());

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public IActionResult Criar(ComboSalvarDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            _negocio.Cadastrar(dto.ParaCombo());
            TempData["Mensagem"] = "Combo cadastrado.";
            return RedirectToAction(nameof(Gerenciar));
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Editar(int id)
    {
        var combo = _negocio.ObterPorId(id);
        if (combo == null)
            return NotFound();

        ViewBag.Id = id;
        return View(ComboSalvarDto.De(combo));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public IActionResult Editar(int id, ComboSalvarDto dto)
    {
        ViewBag.Id = id;

        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            _negocio.Atualizar(dto.ParaCombo(id));
            TempData["Mensagem"] = "Combo atualizado.";
            return RedirectToAction(nameof(Gerenciar));
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Excluir(int id)
    {
        var combo = _negocio.ObterPorId(id);
        return combo == null ? NotFound() : View(ComboDto.De(combo));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ActionName("Excluir")]
    public IActionResult ExcluirConfirmado(int id)
    {
        try
        {
            if (!_negocio.Remover(id))
                return NotFound();

            TempData["Mensagem"] = "Combo excluído.";
            return RedirectToAction(nameof(Gerenciar));
        }
        catch (RegraDeNegocioException ex)
        {
            // O ComboNegocio recusa a exclusão de combo já vendido; a tela mostra o motivo.
            ModelState.AddModelError("", ex.Message);
            var combo = _negocio.ObterPorId(id);
            return combo == null ? NotFound() : View(ComboDto.De(combo));
        }
    }
}
