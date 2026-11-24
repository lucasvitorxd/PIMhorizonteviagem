using HorizonteViagens.Modelos;

namespace HorizonteViagens.Repositorio.Interfaces;

// Contrato do repositório. A camada Negocio conhece só esta interface,
// nunca a classe concreta. Assim dá para trocar EF por Dapper sem mexer no negócio.
public interface IClienteRepositorio
{
    List<Cliente> Listar();
    Cliente? ObterPorId(int id);
    // Usado só no login do cliente; a senha nunca volta para a Api.
    Cliente? ObterPorEmail(string email);
    // Os dois "EmUso" evitam e-mail ou CPF repetido entre clientes diferentes.
    bool EmailEmUso(string email, int? ignorarId = null);
    bool CpfEmUso(string cpf, int? ignorarId = null);
    void Adicionar(Cliente cliente);
    void Atualizar(Cliente cliente);
    void Remover(Cliente cliente);

    // Executa a stored procedure sp_Clientes_BuscarPorNome.
    List<Cliente> BuscarPorNome(string trecho);
}
