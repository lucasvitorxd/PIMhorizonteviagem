using System.Security.Claims;
using HorizonteViagens.Modelos;
using HorizonteViagens.Web.Models;
using HorizonteViagens.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HorizonteViagens.Web.Controllers;

// Telas de reservas: o cliente vê as dele, o administrador vê todas e
// anexa os documentos da viagem (voucher, bilhete) depois da confirmação.
[Authorize]
public class ViagensController : Controller
{
    private readonly PortalNegocio _portal;
    private readonly PagamentoNegocio _pagamentos;

    public ViagensController(PortalNegocio portal, PagamentoNegocio pagamentos)
    {
        _portal = portal;
        _pagamentos = pagamentos;
    }

    // Id de quem está logado (vem do cookie).
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Administrador vê tudo (null); cliente vê só o que é dele.
    private int? ClienteId => User.IsInRole("Administrador") ? null : UsuarioId;

    // GET /Viagens   (lista de reservas)
    public IActionResult Index()
    {
        var reservas = _portal.Reservas(ClienteId)
            .Select(c => ReservaViewModel.De(c))
            .ToList();

        return View(reservas);
    }

    // GET /Viagens/Detalhes/5   (reserva com documentos e botões de pagamento)
    public IActionResult Detalhes(int id)
    {
        var reserva = _portal.Reserva(id, ClienteId);
        var documentos = _portal.Documentos(id, ClienteId);
        return View(ReservaViewModel.De(reserva, documentos, _pagamentos.Configurado));
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public IActionResult Reservar(ReservaEntrada dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = "Confira os dados da reserva.";
            return RedirectToAction("Detalhes", "Combos", new { id = dto.ComboId });
        }

        try
        {
            var reserva = _portal.Reservar(UsuarioId, dto);
            return RedirectToAction(nameof(Detalhes), new { id = reserva.Id });
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction("Detalhes", "Combos", new { id = dto.ComboId, viajantes = dto.Viajantes });
        }
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public async Task<IActionResult> Pagar(int id)
    {
        try
        {
            var link = await _pagamentos.Iniciar(id, UsuarioId);
            return Redirect(link);
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Detalhes), new { id });
        }
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public async Task<IActionResult> Verificar(int id)
    {
        try
        {
            await _pagamentos.Verificar(id, UsuarioId);
            TempData["Mensagem"] = "Status consultado no serviço de pagamento.";
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public IActionResult Cancelar(int id)
    {
        try
        {
            _portal.Cancelar(id, UsuarioId);
            TempData["Mensagem"] = "Reserva cancelada.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public IActionResult Emitir(int id, DocumentoEntrada dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = "Confira título, tipo, código e URL do documento.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        try
        {
            _portal.Emitir(id, dto);
            TempData["Mensagem"] = "Documento adicionado à carteira do cliente.";
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // Carteira: todos os documentos das reservas confirmadas do cliente.
    [Authorize(Roles = "Cliente")]
    public IActionResult Carteira()
    {
        var documentos = _portal.Reservas(UsuarioId)
            .Where(r => r.Status == "Confirmada")
            .SelectMany(r => _portal.Documentos(r.Id, UsuarioId))
            .Select(DocumentoViewModel.De)
            .ToList();

        return View(documentos);
    }

    [Authorize(Roles = "Cliente")]
    public IActionResult Favoritos()
    {
        var ids = _portal.Favoritos(UsuarioId);

        // Mesma vitrine do site (preço calculado pelo Negocio), só com os favoritos.
        var itens = _portal.Vitrine(destino: null, categoria: null, viajantes: 2, ordem: null)
            .Where(i => ids.Contains(i.Combo.Id))
            .ToList();

        // Reaproveita a mesma tela da vitrine.
        return View("~/Views/Combos/Index.cshtml", VitrineViewModel.De(itens, null, null, 2, null));
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public IActionResult Favoritar(int id, bool remover = false)
    {
        _portal.Favoritar(UsuarioId, id, adicionar: !remover);
        TempData["Mensagem"] = remover ? "Favorito removido." : "Destino salvo.";
        return RedirectToAction(nameof(Favoritos));
    }
}
