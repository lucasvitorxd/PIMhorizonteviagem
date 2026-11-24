using HorizonteViagens.Modelos;

namespace HorizonteViagens.Repositorio.Interfaces;

// Contrato do repositório de combos, no mesmo formato do IClienteRepositorio:
// Listar, ObterPorId, Adicionar, Atualizar e Remover, mais a busca pela procedure.
// Respostas possíveis de uma exclusão de combo.
// Excluido: deu certo. EmUso: o banco recusou porque uma reserva foi ligada ao
// combo no meio do caminho. AlteradoPorOutro: alguém salvou o combo antes.
public enum ResultadoExclusao
{
    Excluido,
    EmUso,
    AlteradoPorOutro
}

public interface IComboRepositorio
{
    List<Combo> Listar();
    Combo? ObterPorId(int id);
    void Adicionar(Combo combo);
    void Atualizar(Combo combo);

    // Apaga o combo e diz como foi. Os erros do banco são traduzidos aqui,
    // na única camada que conhece o Entity Framework; o Negocio só lê a resposta.
    ResultadoExclusao Remover(Combo combo);

    // Diz se o combo já tem compras ou itens ligados a ele. O Negocio usa isso
    // para recusar a exclusão antes de o banco dar erro de chave estrangeira.
    bool EmUso(int id);

    // Executa a stored procedure sp_Combos_BuscarPorDestino (só combos com vaga).
    List<Combo> BuscarDisponiveis(string? destino = null);
}
