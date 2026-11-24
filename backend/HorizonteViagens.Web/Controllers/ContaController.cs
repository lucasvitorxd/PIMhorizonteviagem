using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HorizonteViagens.Modelos.Dtos;
using HorizonteViagens.Negocio;

namespace HorizonteViagens.Web.Controllers;

// Login e saída (administrador e cliente) e o perfil do cliente.
// O controller não confere senha: quem faz isso é o AdministradorNegocio
// (para a agência) ou o ClienteNegocio (para o viajante).
// Aqui só se entrega (ou se recolhe) o "crachá" do navegador.
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
public class ContaController : Controller
{
    private readonly ClienteNegocio _clientes;
    private readonly AdministradorNegocio _negocio;

    public ContaController(AdministradorNegocio negocio, ClienteNegocio clientes)
    {
        _negocio = negocio;
        _clientes = clientes;
    }

    // GET /Conta/Login
    // returnUrl: a tela que a pessoa tentou abrir antes de ser mandada para o login.
    // admin = true mostra o login da agência; sem ele, é o login do cliente.
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl, bool admin = false)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Admin"] = admin;
        return View(new LoginDto());
    }

    // POST /Conta/Login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto modelo, string? returnUrl, bool admin = false)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Admin"] = admin;
        if (!ModelState.IsValid) return View(modelo);

        // Usuario é a classe mãe de Administrador e Cliente: a mesma variável serve para os dois.
        // A tela Entrar aceita também administradores, sem exigir o link do rodapé.
        // Cada tipo de conta continua validado pelo seu próprio serviço.
        HorizonteViagens.Modelos.Usuario? administrador;

        if (admin)
        {
            administrador = _negocio.Autenticar(modelo.Email, modelo.Senha);
        }
        else
        {
            administrador = _clientes.Autenticar(modelo.Email, modelo.Senha);

            if (administrador == null)
            {
                administrador = _negocio.Autenticar(modelo.Email, modelo.Senha);
            }
        }

        if (administrador is null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            return View(modelo);
        }

        // As "claims" são as informações escritas no crachá.
        // A Role vem do Perfil (polimorfismo): para um Administrador, "Administrador".
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, administrador.Id.ToString()),
            new(ClaimTypes.Name, administrador.Nome),
            new(ClaimTypes.Email, administrador.Email),
            new(ClaimTypes.Role, administrador.Perfil)
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade));

        // Url.IsLocalUrl impede que alguém use o returnUrl para mandar o
        // administrador para um site falso depois do login.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return administrador.Perfil == "Administrador"
            ? RedirectToAction("Gerenciar", "Combos")
            : RedirectToAction("Index", "Viagens");
    }

    // POST /Conta/Sair
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // GET /Conta/Negado  → logado, mas sem permissão para a tela
    [AllowAnonymous]
    public IActionResult Negado() => View();

    // O cliente só vê e altera o PRÓPRIO perfil: o Id vem do cookie, nunca do formulário.
    private int ClienteLogadoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize(Roles = "Cliente")]
    public IActionResult Perfil()
    {
        var cliente = _clientes.ObterPorId(ClienteLogadoId);
        return cliente is null ? NotFound() : View(ClienteSalvarDto.De(cliente));
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Perfil(ClienteSalvarDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var cliente = dto.ParaCliente(ClienteLogadoId);
            cliente.Ativo = true;
            _clientes.Atualizar(cliente);
            TempData["Mensagem"] = "Perfil atualizado.";
            return RedirectToAction(nameof(Perfil));
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }
}
