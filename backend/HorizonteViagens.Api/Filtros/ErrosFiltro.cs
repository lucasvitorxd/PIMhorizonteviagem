using HorizonteViagens.Negocio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HorizonteViagens.Api.Filtros;

// Pega as exceções que escaparam dos controllers e devolve uma resposta curta.
// O detalhe do erro fica só no log do servidor: nunca vai SQL, senha ou
// "stack trace" para quem chamou a Api.
public class ErrosFiltro : IExceptionFilter
{
    private readonly ILogger<ErrosFiltro> _log;

    public ErrosFiltro(ILogger<ErrosFiltro> log)
    {
        _log = log;
    }

    public void OnException(ExceptionContext contexto)
    {
        _log.LogError(contexto.Exception, "Falha ao processar {Path}", contexto.HttpContext.Request.Path);

        var (status, mensagem) = Traduzir(contexto.Exception);

        contexto.Result = new ObjectResult(new { erro = mensagem }) { StatusCode = status };
        contexto.ExceptionHandled = true;
    }

    // Tipo de erro → código HTTP e mensagem que o usuário pode ver.
    private static (int Status, string Mensagem) Traduzir(Exception erro) => erro switch
    {
        UnauthorizedAccessException =>
            (403, "Acesso restrito."),
        RegraDeNegocioException =>
            (400, erro.Message),
        FormatException =>
            (400, "Não foi possível concluir. Verifique o serviço e tente novamente."),
        DbUpdateException =>
            (409, "Os dados mudaram ou possuem vínculos. Atualize a página e tente novamente."),
        _ =>
            (503, "Não foi possível concluir. Verifique o serviço e tente novamente.")
    };
}
