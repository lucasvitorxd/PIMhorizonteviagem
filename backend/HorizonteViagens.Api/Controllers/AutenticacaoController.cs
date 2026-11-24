using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HorizonteViagens.Modelos.Dtos;
using HorizonteViagens.Negocio;

namespace HorizonteViagens.Api.Controllers;

// Login pela Api, para o administrador e para o cliente.
// Quem acerta e-mail e senha recebe um TOKEN (JWT) e passa a mandar esse token
// nas próximas chamadas. O limite de tentativas ("login") vale para as duas rotas.
[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
public class AutenticacaoController : ControllerBase
{
    private readonly AdministradorNegocio _negocio;
    private readonly ClienteNegocio _clienteNegocio;
    private readonly GeradorDeToken _gerador;

    public AutenticacaoController(AdministradorNegocio negocio, ClienteNegocio clienteNegocio, GeradorDeToken gerador)
    {
        _negocio = negocio;
        _clienteNegocio = clienteNegocio;
        _gerador = gerador;
    }

    // POST api/autenticacao/login   → 200 com o token, ou 401 se errar
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var administrador = _negocio.Autenticar(dto.Email, dto.Senha);
        if (administrador is null)
            return Unauthorized(new { erro = "E-mail ou senha incorretos." });

        var (token, expiraEm) = _gerador.Gerar(administrador);
        return Ok(new { token, expiraEm, nome = administrador.Nome });
    }

    // POST api/autenticacao/login-cliente   → 200 com o token, ou 401
    // Login separado para o app e o site: o token sai com o papel "Cliente",
    // e é com ele que o cliente compra e vê as próprias reservas.
    [AllowAnonymous]
    [HttpPost("login-cliente")]
    public IActionResult LoginCliente(LoginDto dto)
    {
        var cliente = _clienteNegocio.Autenticar(dto.Email, dto.Senha);
        if (cliente is null)
            return Unauthorized(new { erro = "E-mail ou senha incorretos." });

        var (token, expiraEm) = _gerador.Gerar(cliente);
        return Ok(new { token, expiraEm, nome = cliente.Nome });
    }
}
