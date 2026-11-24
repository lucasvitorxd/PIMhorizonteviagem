using HorizonteViagens.Negocio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HorizonteViagens.Web.Filtros;

// Mesmo papel do ErrosFiltro da Api: erro inesperado vira resposta curta,
// sem mostrar detalhes internos para o usuário.
public class ErrosFiltro : IExceptionFilter
{
    public void OnException(ExceptionContext contexto)
    {
        var (status, mensagem) = contexto.Exception switch
        {
            UnauthorizedAccessException =>
                (403, "Acesso restrito."),
            RegraDeNegocioException =>
                (400, contexto.Exception.Message),
            FormatException =>
                (400, "Não foi possível concluir. Verifique o serviço e tente novamente."),
            DbUpdateException =>
                (409, "Os dados mudaram ou possuem vínculos. Atualize a página e tente novamente."),
            _ =>
                (503, "Não foi possível concluir. Verifique o serviço e tente novamente.")
        };

        contexto.Result = new ObjectResult(new { erro = mensagem }) { StatusCode = status };
        contexto.ExceptionHandled = true;
    }
}
