using HorizonteViagens.Modelos;

namespace HorizonteViagens.Repositorio.Interfaces;

// Contrato do repositório do portal do cliente (catálogo, reservas, favoritos,
// documentos e pagamento). O PortalNegocio e o PagamentoNegocio conhecem só
// esta interface, nunca a classe concreta, igual aos outros repositórios.
public interface IPortalRepositorio
{
    // Catálogo e clientes
    List<Combo> Catalogo();
    Combo? Combo(int id);
    Cliente? Cliente(int id);

    // Reservas
    List<Compra> Reservas(int? clienteId);
    Compra? Reserva(int id, int? clienteId = null);
    Compra? PorRequisicao(int clienteId, Guid requisicao);
    Compra Reservar(ReservaEntrada entrada, int clienteId, Func<Combo, Orcamento> validar);
    void Expirar();
    bool Cancelar(int id, int clienteId);

    // Favoritos e documentos
    List<int> Favoritos(int clienteId);
    void Favoritar(int clienteId, int comboId, bool adicionar);
    List<DocumentoViagem> Documentos(int compraId);
    void Documento(DocumentoViagem documento);

    // Pagamento
    void AplicarPagamento(int id, string pagamentoId, string status, decimal valor, string referencia, bool teste);
    void Salvar();

    // Preparação do banco (usada só pelo comando --preparar-banco)
    void AtualizarBanco();
    bool ExisteCombo();
    void AdicionarCombos(List<Combo> combos);
}
