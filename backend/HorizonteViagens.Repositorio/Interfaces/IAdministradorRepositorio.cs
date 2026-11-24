using HorizonteViagens.Modelos;

namespace HorizonteViagens.Repositorio.Interfaces;

// Contrato do repositório de administradores.
// O AdministradorNegocio conhece só esta interface, nunca a classe concreta.
public interface IAdministradorRepositorio
{
    // Usado para conferir, a cada requisição, se a conta continua ativa.
    Administrador? ObterPorId(int id);
    // Usado no login.
    Administrador? ObterPorEmail(string email);
    // Adicionar e Atualizar são usados pela preparação do banco (contas de teste)
    // e pelo login, que grava a data do último acesso.
    void Adicionar(Administrador administrador);
    void Atualizar(Administrador administrador);
}
