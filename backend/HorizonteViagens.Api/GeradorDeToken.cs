using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using HorizonteViagens.Modelos;

namespace HorizonteViagens.Api;

// Monta o token JWT entregue ao app depois de um login certo.
// Fica na Api (e não no Negocio) porque token é um detalhe de COMO a Api
// conversa com o app, não uma regra da agência. A regra (e-mail e senha
// corretos?) continua no AdministradorNegocio.
public class GeradorDeToken
{
    private readonly IConfiguration _configuracao;

    public GeradorDeToken(IConfiguration configuracao)
    {
        _configuracao = configuracao;
    }

    // Recebe um Usuario (a classe mãe), então serve tanto para Administrador
    // quanto para Cliente. O Perfil de cada um vira o "papel" (Role) do token.
    // Devolve o token e a hora em que ele deixa de valer.
    public (string Token, DateTime ExpiraEm) Gerar(Usuario administrador)
    {
        var jwt = _configuracao.GetSection("Jwt");
        var horas = int.TryParse(jwt["HorasDeValidade"], out int h) ? h : 8;
        var expiraEm = DateTime.UtcNow.AddHours(horas);

        // A chave assina o token. Quem não tem a chave não consegue fabricar
        // um token válido nem alterar um que já existe.
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Chave"]!));

        var descricao = new SecurityTokenDescriptor
        {
            // As mesmas informações do "crachá" da Web, inclusive o Perfil.
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, administrador.Id.ToString()),
                new Claim(ClaimTypes.Name, administrador.Nome),
                new Claim(ClaimTypes.Email, administrador.Email),
                new Claim(ClaimTypes.Role, administrador.Perfil)
            }),
            Issuer = jwt["Emissor"],
            Audience = jwt["Publico"],
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descricao), expiraEm);
    }
}
