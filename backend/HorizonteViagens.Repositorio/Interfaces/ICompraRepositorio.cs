using HorizonteViagens.Modelos;

namespace HorizonteViagens.Repositorio.Interfaces;

// Contrato do repositório de compras. O CompraNegocio conhece só esta interface.
public interface ICompraRepositorio
{
    // Grava a compra e a vaga já descontada do combo, numa transação.
    // Devolve false se outra pessoa alterou o combo antes (concorrência).
    bool Registrar(Compra compra);
    // Lista para o administrador acompanhar as vendas.
    List<Compra> Listar();

    // Registra que um administrador está acompanhando a compra.
    bool Monitorar(int compraId, int administradorId);
}
