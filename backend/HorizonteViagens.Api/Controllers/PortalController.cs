using System.Security.Claims;
using HorizonteViagens.Modelos;
using HorizonteViagens.Modelos.Dtos;
using HorizonteViagens.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HorizonteViagens.Api.Controllers;

// Área do cliente usada pelo site e pelo app: catálogo, orçamento, perfil,
// favoritos, reservas, pagamento e documentos. O cliente é sempre o do TOKEN.
[ApiController]
[Route("api/portal")]
[Authorize]
public class PortalController : ControllerBase
{
    private readonly PortalNegocio _portal;
    private readonly ClienteNegocio _clientes;
    private readonly PagamentoNegocio _pagamentos;

    public PortalController(PortalNegocio portal, ClienteNegocio clientes, PagamentoNegocio pagamentos)
    {
        _portal = portal;
        _clientes = clientes;
        _pagamentos = pagamentos;
    }

    // Id do cliente logado. Administrador não usa as rotas de cliente (vira 403 no ErrosFiltro).
    private int ClienteId
    {
        get
        {
            if (!User.IsInRole("Cliente"))
                throw new UnauthorizedAccessException();

            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }

    // ---------- Público ----------

    // GET api/portal/catalogo
    [AllowAnonymous]
    [HttpGet("catalogo")]
    public IActionResult Catalogo() =>
        Ok(_portal.Catalogo().Select(ComboDto.De));

    // GET api/portal/orcamento/5?viajantes=2
    [AllowAnonymous]
    [HttpGet("orcamento/{comboId:int}")]
    public IActionResult Orcamento(int comboId, int viajantes = 1) =>
        Ok(_portal.Orcar(comboId, viajantes));

    // ---------- Cliente ----------

    [HttpGet("perfil")]
    public IActionResult Perfil() =>
        Ok(ClienteDto.De(_portal.Cliente(ClienteId)));

    [HttpPut("perfil")]
    public IActionResult Perfil(ClienteSalvarDto dto)
    {
        var cliente = dto.ParaCliente(ClienteId);
        cliente.Ativo = true;
        _clientes.Atualizar(cliente);
        return Ok(ClienteDto.De(_portal.Cliente(ClienteId)));
    }

    [HttpGet("favoritos")]
    public IActionResult Favoritos() =>
        Ok(_portal.Favoritos(ClienteId));

    [HttpPut("favoritos/{comboId:int}")]
    public IActionResult Favoritar(int comboId)
    {
        _portal.Favoritar(ClienteId, comboId, adicionar: true);
        return NoContent();
    }

    [HttpDelete("favoritos/{comboId:int}")]
    public IActionResult Desfavoritar(int comboId)
    {
        _portal.Favoritar(ClienteId, comboId, adicionar: false);
        return NoContent();
    }

    [HttpGet("reservas")]
    public IActionResult Reservas() =>
        Ok(_portal.Reservas(ClienteId).Select(ReservaDto.De));

    [HttpPost("reservas")]
    public IActionResult Reservar(ReservaEntrada dto) =>
        Ok(ReservaDto.De(_portal.Reservar(ClienteId, dto)));

    [HttpPost("reservas/{id:int}/cancelar")]
    public IActionResult Cancelar(int id) =>
        _portal.Cancelar(id, ClienteId) ? NoContent() : NotFound();

    [HttpGet("reservas/{id:int}/documentos")]
    public IActionResult Documentos(int id) =>
        Ok(_portal.Documentos(id, ClienteId));

    [HttpPost("reservas/{id:int}/pagamento")]
    public async Task<IActionResult> Pagar(int id)
    {
        var link = await _pagamentos.Iniciar(id, ClienteId);
        return Ok(new { url = link, ambiente = "teste" });
    }

    [HttpPost("reservas/{id:int}/verificar")]
    public async Task<IActionResult> Verificar(int id) =>
        Ok(ReservaDto.De(await _pagamentos.Verificar(id, ClienteId)));

    // ---------- Administrador ----------

    [Authorize(Roles = "Administrador")]
    [HttpGet("gestao/reservas")]
    public IActionResult GestaoReservas() =>
        Ok(_portal.Reservas(null).Select(ReservaDto.De));

    [Authorize(Roles = "Administrador")]
    [HttpGet("gestao/reservas/{id:int}/documentos")]
    public IActionResult GestaoDocumentos(int id) =>
        Ok(_portal.Documentos(id, null));

    [Authorize(Roles = "Administrador")]
    [HttpPost("documentos/{id:int}")]
    public IActionResult Documento(int id, DocumentoEntrada dto)
    {
        _portal.Emitir(id, dto);
        return NoContent();
    }
}
