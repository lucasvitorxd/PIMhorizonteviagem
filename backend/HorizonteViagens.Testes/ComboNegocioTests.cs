using HorizonteViagens.Modelos;
using HorizonteViagens.Negocio;
using HorizonteViagens.Repositorio.Interfaces;
using Xunit;

namespace HorizonteViagens.Testes;

// Testes das regras do combo, com o ComboRepositorioFalso (lista em memória),
// que está no fim do arquivo CompraNegocioTests.cs.
public class ComboNegocioTests
{
    // Simula uma compra gravada depois da conferência do Negocio e antes do DELETE:
    // o repositório avisa que o banco barrou (EmUso) e nada é apagado.
    [Fact]
    public void Remover_VinculoConcorrente_TraduzErroSemApagar()
    {
        var repo = new ComboRepositorioFalso(new Combo { Id = 1 })
        {
            ResultadoAoRemover = ResultadoExclusao.EmUso
        };
        var erro = Assert.Throws<RegraDeNegocioException>(() => new ComboNegocio(repo).Remover(1));
        Assert.Contains("reservas ou serviços", erro.Message);
        Assert.Single(repo.Itens);
    }

    // Um erro que o repositório não reconhece não vira mensagem de regra:
    // ele sobe como está, para aparecer no log.
    [Fact]
    public void Remover_ErroDesconhecido_NaoDisfarcaComoVinculo()
    {
        var repo = new ComboRepositorioFalso(new Combo { Id = 1 })
        {
            ErroAoRemover = new InvalidOperationException("falha no banco")
        };
        Assert.Throws<InvalidOperationException>(() => new ComboNegocio(repo).Remover(1));
    }

    // Alguém salvou o combo antes: a mensagem pede para atualizar a página.
    [Fact]
    public void Remover_Concorrencia_OrientaAtualizarPagina()
    {
        var repo = new ComboRepositorioFalso(new Combo { Id = 1 })
        {
            ResultadoAoRemover = ResultadoExclusao.AlteradoPorOutro
        };
        var erro = Assert.Throws<RegraDeNegocioException>(() => new ComboNegocio(repo).Remover(1));
        Assert.Contains("Atualize a página", erro.Message);
        Assert.Single(repo.Itens);
    }

    // Combo correto é gravado, e os espaços sobrando no nome são removidos.
    [Fact]
    public void Cadastrar_ComboValido_GuardaNoRepositorio()
    {
        var repo = new ComboRepositorioFalso();                                       // ARRANGE
        var negocio = new ComboNegocio(repo);

        negocio.Cadastrar(new Combo { Nome = " Paris ", Destino = "Paris", Categoria = "Luxo", Vagas = 20, Valor = 15000m }); // ACT

        Assert.Single(repo.Itens);                                                    // ASSERT
        Assert.Equal("Paris", repo.Itens[0].Nome);
    }

    // Vagas negativas não fazem sentido: o Negocio recusa e nada é gravado.
    [Fact]
    public void Cadastrar_VagasNegativas_LancaExcecaoENaoGrava()
    {
        var repo = new ComboRepositorioFalso();
        var negocio = new ComboNegocio(repo);
        var combo = new Combo { Nome = "Paris", Destino = "Paris", Categoria = "Luxo", Vagas = -1, Valor = 15000m };

        var erro = Assert.Throws<RegraDeNegocioException>(() => negocio.Cadastrar(combo));

        Assert.Equal("As vagas não podem ser negativas.", erro.Message);
        Assert.Empty(repo.Itens);
    }

    // Combo que já tem compras não pode ser excluído, e continua na lista.
    [Fact]
    public void Remover_ComboComReservas_RecusaENaoApaga()
    {
        var combo = new Combo { Id = 1, Nome = "Paris", Destino = "Paris", Categoria = "Luxo", Vagas = 20, Valor = 15000m };
        var repo = new ComboRepositorioFalso(combo);
        repo.Vendidos.Add(1);
        var negocio = new ComboNegocio(repo);

        var erro = Assert.Throws<RegraDeNegocioException>(() => negocio.Remover(1));

        Assert.StartsWith("Este combo possui reservas", erro.Message);
        Assert.Single(repo.Itens);
    }

    // Na alteração, os dados novos são copiados para o combo que já estava no banco.
    [Fact]
    public void Atualizar_CopiaOsDadosParaOComboQueVeioDoBanco()
    {
        var existente = new Combo { Id = 1, Nome = "Paris", Destino = "Paris", Categoria = "Luxo", Vagas = 20, Valor = 15000m };
        var repo = new ComboRepositorioFalso(existente);
        var negocio = new ComboNegocio(repo);

        negocio.Atualizar(new Combo { Id = 1, Nome = "Paris Premium", Destino = "Paris", Categoria = "Luxo", Vagas = 18, Valor = 16000m });

        Assert.Equal("Paris Premium", existente.Nome);
        Assert.Equal(18, existente.Vagas);
        Assert.Equal(16000m, existente.Valor);
    }
}
