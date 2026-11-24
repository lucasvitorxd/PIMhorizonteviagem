using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HorizonteViagens.Modelos.Dtos;
using HorizonteViagens.Negocio;

namespace HorizonteViagens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComprasController : ControllerBase
{
    private readonly CompraNegocio _negocio;
    private readonly PortalNegocio _portal;
    private readonly ILogger<ComprasController> _log;

    public ComprasController(CompraNegocio negocio, PortalNegocio portal, ILogger<ComprasController> log)
    {
        _negocio = negocio;
        _portal = portal;
        _log = log;
    }

    // POST api/compras   → 200 com a reserva, ou 400 (regra de negócio)
    // Basta mandar { "comboId": 1 }. O cliente vem do TOKEN, não do JSON:
    // assim ninguém compra em nome de outra pessoa.
    [Authorize(Roles = "Cliente")]
    [HttpPost]
    public IActionResult Comprar(CompraCriarDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
            return Unauthorized();

        try
        {
            var compra = _portal.Comprar(clienteId, dto.ComboId, dto.Viajantes);
            // Só Ids no log: nunca nome, CPF, e-mail ou token do cliente.
            _log.LogInformation("Compra {CompraId} registrada: combo {ComboId}, cliente {ClienteId}",
                compra.Id, compra.ComboId, compra.ClienteId);
            return Ok(ReservaDto.De(compra));
        }
        catch (RegraDeNegocioException ex)
        {
            // Mesmo padrão do ClientesController do template: regra não atendida → 400.
            _log.LogWarning("Compra recusada: combo {ComboId}, motivo {Motivo}", dto.ComboId, ex.Message);
            return BadRequest(new { erro = ex.Message });
        }
    }

    // GET api/compras   → 200, monitoramento exclusivo do administrador
    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public ActionResult<IEnumerable<CompraDto>> Listar() =>
        Ok(_negocio.Listar().Select(CompraDto.De));

    // POST api/compras/5/monitoramento   → 204 ou 404
    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:int}/monitoramento")]
    public IActionResult Monitorar(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var administradorId))
            return Unauthorized();

        return _negocio.Monitorar(id, administradorId) ? NoContent() : NotFound();
    }
}
