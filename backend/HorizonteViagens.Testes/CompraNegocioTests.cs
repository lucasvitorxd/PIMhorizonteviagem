using HorizonteViagens.Modelos;
using HorizonteViagens.Negocio;
using HorizonteViagens.Repositorio.Interfaces;
using Xunit;

namespace HorizonteViagens.Testes;

// Testes da regra de negócio da compra, sem banco: os repositórios são falsos
// (dublês), guardando tudo em memória. Formato AAA: Arrange, Act, Assert.
public class CompraNegocioTests
{
    // Caminho feliz: tem vaga, a compra sai com o valor do combo e sobra uma vaga a menos.
    [Fact]
    public void Comprar_ComVaga_GravaCompraComValorDoComboEDescontaUmaVaga()
    {
        var (negocio, compras, _, combo) = Montar(vagas: 3, valor: 4500m);   // ARRANGE

        var compra = negocio.Comprar(clienteId: 1, comboId: 10);           // ACT

        Assert.Equal(4500m, compra.Valor);                                  // ASSERT
        Assert.Equal(2, combo.Vagas);
        Assert.Single(compras.Gravadas);
    }

    // Combo com 0 vagas: a compra é recusada e nada é gravado.
    [Fact]
    public void Comprar_SemVaga_RecusaENaoGrava()
    {
        var (negocio, compras, _, combo) = Montar(vagas: 0, valor: 4500m);

        var erro = Assert.Throws<RegraDeNegocioException>(() => negocio.Comprar(1, 10));

        Assert.Equal("Não há vagas disponíveis para este combo.", erro.Message);
        Assert.Empty(compras.Gravadas);
        Assert.Equal(0, combo.Vagas);
    }

    // Cliente desativado não pode comprar.
    [Fact]
    public void Comprar_ClienteInativo_Recusa()
    {
        var (negocio, compras, clientes, _) = Montar(vagas: 3, valor: 4500m);
        clientes.Cliente.Ativo = false;

        var erro = Assert.Throws<RegraDeNegocioException>(() => negocio.Comprar(1, 10));

        Assert.Equal("Cliente não encontrado ou inativo.", erro.Message);
        Assert.Empty(compras.Gravadas);
    }

    // Combo que não existe (Id 99).
    [Fact]
    public void Comprar_ComboInexistente_Recusa()
    {
        var (negocio, _, _, _) = Montar(vagas: 3, valor: 4500m);

        var erro = Assert.Throws<RegraDeNegocioException>(() => negocio.Comprar(1, comboId: 99));

        Assert.Equal("Combo não encontrado.", erro.Message);
    }

    // Concorrência: duas pessoas tentando a última vaga ao mesmo tempo.
    [Fact]
    public void Comprar_OutraPessoaAlterouOComboAoMesmoTempo_Recusa()
    {
        // O combo ainda mostrava vaga, mas outra pessoa comprou primeiro:
        // no banco de verdade o RowVersion não confere e o repositório devolve false.
        var (negocio, compras, _, _) = Montar(vagas: 1, valor: 4500m);
        compras.SimularConcorrencia = true;

        var erro = Assert.Throws<RegraDeNegocioException>(() => negocio.Comprar(1, 10));

        Assert.Equal("As vagas deste combo acabaram de mudar. Tente novamente.", erro.Message);
        Assert.Empty(compras.Gravadas);
    }

    // Monta o cenário de todos os testes: um combo (Id 10), uma cliente ativa (Id 1)
    // e os repositórios falsos já ligados ao CompraNegocio.
    private static (CompraNegocio, CompraRepositorioFalso, ClienteRepositorioFalso, Combo) Montar(int vagas, decimal valor)
    {
        var combo = new Combo { Id = 10, Nome = "Paris", Destino = "Paris", Categoria = "Luxo", Vagas = vagas, Valor = valor };
        var compras = new CompraRepositorioFalso();
        var clientes = new ClienteRepositorioFalso();
        var combos = new ComboRepositorioFalso(combo);
        return (new CompraNegocio(compras, clientes, combos), compras, clientes, combo);
    }

    // Repositório falso de compras. SimularConcorrencia = true faz o Registrar
    // devolver false, como aconteceria no banco se o RowVersion não conferisse.
    private sealed class CompraRepositorioFalso : ICompraRepositorio
    {
        public List<Compra> Gravadas { get; } = new();
        public bool SimularConcorrencia { get; set; }

        public bool Registrar(Compra compra)
        {
            if (SimularConcorrencia) return false;
            compra.Id = Gravadas.Count + 1;
            Gravadas.Add(compra);
            return true;
        }
        public List<Compra> Listar() => Gravadas;
        public bool Monitorar(int compraId, int administradorId) => true;
    }

    // Repositório falso de clientes: sempre a mesma cliente, a Ana (Id 1).
    private sealed class ClienteRepositorioFalso : IClienteRepositorio
    {
        public Cliente Cliente { get; } = new() { Id = 1, Nome = "Ana", Ativo = true };
        public List<Cliente> Listar() => [Cliente];
        public Cliente? ObterPorId(int id) => id == Cliente.Id ? Cliente : null;
        public Cliente? ObterPorEmail(string email) => null;
        public bool EmailEmUso(string email, int? ignorarId = null) => false;
        public bool CpfEmUso(string cpf, int? ignorarId = null) => false;
        public void Adicionar(Cliente cliente) { }
        public void Atualizar(Cliente cliente) { }
        public void Remover(Cliente cliente) { }
        public List<Cliente> BuscarPorNome(string trecho) => [];
    }
}

// Dublê do repositório de combos, usado pelos testes de compra e de combo.
internal sealed class ComboRepositorioFalso : IComboRepositorio
{
    public List<Combo> Itens { get; } = new();

    public ComboRepositorioFalso(params Combo[] combos) => Itens.AddRange(combos);

    public List<Combo> Listar() => Itens;
    public Combo? ObterPorId(int id) => Itens.FirstOrDefault(c => c.Id == id);
    public void Adicionar(Combo combo) { combo.Id = Itens.Count + 1; Itens.Add(combo); }
    public void Atualizar(Combo combo) { }
    // O teste escolhe a resposta da exclusão (ou um erro) para simular o banco.
    public ResultadoExclusao ResultadoAoRemover { get; set; } = ResultadoExclusao.Excluido;
    public Exception? ErroAoRemover { get; set; }
    public ResultadoExclusao Remover(Combo combo)
    {
        if (ErroAoRemover != null) throw ErroAoRemover;
        if (ResultadoAoRemover == ResultadoExclusao.Excluido) Itens.Remove(combo);
        return ResultadoAoRemover;
    }
    public List<Combo> BuscarDisponiveis(string? destino = null) => Itens.Where(c => c.Vagas > 0).ToList();

    // Ids de combos que o teste quer tratar como "já vendidos".
    public HashSet<int> Vendidos { get; } = new();
    public bool EmUso(int id) => Vendidos.Contains(id);
}
