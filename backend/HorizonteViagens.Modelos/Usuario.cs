using System.ComponentModel.DataAnnotations;

namespace HorizonteViagens.Modelos;

// Classe MÃE de todo mundo que tem cadastro no sistema. É o exemplo de
// HERANÇA e POLIMORFISMO do trabalho:
// 'abstract' porque ninguém é "só um usuário": ou é Cliente (o viajante),
// ou é Administrador (a agência). Os campos comuns (Id, Nome, Email,
// SenhaHash, Ativo) ficam aqui, e cada filha herda sem repetir código.
// O 'Perfil' é abstract: cada filha responde o seu com 'override', e o login
// e o token usam uma variável do tipo Usuario para os dois casos.
public abstract class Usuario
{
    // Chave da tabela. Quem gera o número é o banco.
    public int Id { get; set; }

    // Data limite do acesso. Vazio (null) quer dizer que a conta não expira.
    // Serve para contas temporárias, como as de teste da apresentação.
    public DateTime? AcessoAte { get; set; }

    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    // ENCAPSULAMENTO: a senha digitada nunca é guardada.
    // O que fica no banco é o "hash", um resumo que não dá para desfazer.
    // Quem gera o hash é a camada Negocio (classe SenhaHasher).
    public string SenhaHash { get; set; } = string.Empty;

    // Conta desativada não consegue entrar, mas o cadastro continua guardado.
    public bool Ativo { get; set; } = true;

    // POLIMORFISMO: cada classe filha responde do seu jeito.
    // É usado no login para dizer ao ASP.NET qual é o perfil de quem entrou.
    // Não vira coluna no banco (ver o Ignore no AppDbContext).
    public abstract string Perfil { get; }
}
