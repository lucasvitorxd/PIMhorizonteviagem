using System.ComponentModel.DataAnnotations;

namespace HorizonteViagens.Modelos.Dtos;

// DTO de ENTRADA do login, usado tanto pelo administrador quanto pelo cliente.
// A Web usa no formulário de login, e a Api recebe no POST de autenticação.
// Só tem e-mail e senha: quem confere se estão certos é o Negocio.
public class LoginDto
{
    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Senha")]
    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;
}
