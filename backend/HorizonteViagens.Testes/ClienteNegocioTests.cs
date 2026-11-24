using HorizonteViagens.Modelos;
using HorizonteViagens.Negocio;
using HorizonteViagens.Repositorio.Interfaces;
using Xunit;

namespace HorizonteViagens.Testes;

// Testes das regras do cliente, sem banco: o repositório é falso e guarda
// o cliente numa variável. Formato AAA: Arrange (preparar), Act (agir), Assert (conferir).
public class ClienteNegocioTests
{
    // CPF com o último dígito errado (24 em vez de 25) precisa ser recusado.
    [Fact]
    public void Cadastrar_RejeitaCpfComDigitoVerificadorIncorreto()
    {
        var negocio = new ClienteNegocio(new RepositorioClienteFalso());
        var cliente = new Cliente { Nome = "Teste", Email = "teste@example.com", Cpf = "529.982.247-24", Celular = "11999999999" };
        var excecao = Assert.Throws<RegraDeNegocioException>(() => negocio.Cadastrar(cliente, "SenhaForte1!"));
        Assert.Equal("O CPF informado é inválido.", excecao.Message);
    }

    // CPF certo com pontos e traço é aceito, gravado só com números, e a senha
    // é guardada como hash (nunca o texto que a pessoa digitou).
    [Fact]
    public void Cadastrar_AceitaCpfValidoFormatadoEArmazenaSomenteHash()
    {
        var repo = new RepositorioClienteFalso();
        var negocio = new ClienteNegocio(repo);
        var cliente = new Cliente { Nome = "Teste", Email = "teste@example.com", Cpf = "529.982.247-25", Celular = "(11) 99999-9999" };
        negocio.Cadastrar(cliente, "SenhaForte1!");
        Assert.Equal("52998224725", repo.Salvo!.Cpf);
        Assert.NotEqual("SenhaForte1!", repo.Salvo.SenhaHash);
        Assert.True(SenhaHasher.Conferir("SenhaForte1!", repo.Salvo.SenhaHash));
    }

    // CPF em formato inválido é recusado e nada é gravado.
    [Theory]
    [InlineData("12345")]
    [InlineData("11111111111")]
    [InlineData("abc52998224725")]
    [InlineData("５２９９８２２４７２５")] // dígitos "largos" (outro alfabeto) não valem
    public void Cadastrar_RecusaFormatoCpfSemGravar(string cpf)
    {
        var repo = new RepositorioClienteFalso();
        var negocio = new ClienteNegocio(repo);
        var cliente = new Cliente { Nome = "Teste", Email = "teste@example.com", Cpf = cpf, Celular = "11999999999" };

        Assert.Throws<RegraDeNegocioException>(() => negocio.Cadastrar(cliente, "SenhaForte1!"));
        Assert.Null(repo.Salvo);
    }

    // Senha fraca (curta, sem maiúscula, sem minúscula, sem número, sem símbolo ou com espaço) é recusada.
    [Theory]
    [InlineData("Ab1!")]
    [InlineData("abcdefgh1!")]
    [InlineData("ABCDEFGH1!")]
    [InlineData("Abcdefgh!")]
    [InlineData("Abcdefgh1")]
    [InlineData("Abcdefg1 ")]
    [InlineData("Abcdefg1! ")]
    public void Cadastrar_RecusaSenhaInvalidaSemGravar(string senha)
    {
        var repo = new RepositorioClienteFalso();
        var negocio = new ClienteNegocio(repo);
        var cliente = new Cliente { Nome = "Teste", Email = "teste@example.com", Cpf = "52998224725", Celular = "11999999999" };

        Assert.Throws<RegraDeNegocioException>(() => negocio.Cadastrar(cliente, senha));
        Assert.Null(repo.Salvo);
    }

    // Repositório "de mentira": em vez de gravar no banco, guarda na propriedade Salvo.
    private sealed class RepositorioClienteFalso : IClienteRepositorio
    {
        public Cliente? Salvo { get; private set; }
        public List<Cliente> Listar() => [];
        public Cliente? ObterPorId(int id) => Salvo?.Id == id ? Salvo : null;
        public Cliente? ObterPorEmail(string email) => null;
        public bool EmailEmUso(string email, int? ignorarId = null) => false;
        public bool CpfEmUso(string cpf, int? ignorarId = null) => false;
        public void Adicionar(Cliente cliente) { cliente.Id = 1; Salvo = cliente; }
        public void Atualizar(Cliente cliente) => Salvo = cliente;
        public void Remover(Cliente cliente) => Salvo = null;
        public List<Cliente> BuscarPorNome(string trecho) => [];
    }
}
