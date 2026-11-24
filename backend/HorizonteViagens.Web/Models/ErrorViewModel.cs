namespace HorizonteViagens.Web.Models;

// Modelo da tela de erro padrão do ASP.NET (Views/Shared/Error.cshtml).
// O RequestId ajuda a achar o erro no log do servidor.
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
