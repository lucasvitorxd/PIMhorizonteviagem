using HorizonteViagens.Modelos;
using HorizonteViagens.Repositorio.Contexto;
using HorizonteViagens.Repositorio.Interfaces;

namespace HorizonteViagens.Repositorio;

// Única classe que conversa com a tabela Administradores.
public class AdministradorRepositorio : IAdministradorRepositorio
{
    private readonly AppDbContext _contexto;

    // INJEÇÃO DE DEPENDÊNCIA: o contexto chega pronto pelo construtor,
    // igual no ClienteRepositorio.
    public AdministradorRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    // Busca pelo Id; usado para saber se a conta do token ainda está ativa.
    public Administrador? ObterPorId(int id) =>
        _contexto.Administradores.FirstOrDefault(a => a.Id == id);

    // O EF Core transforma esta linha num SELECT com parâmetro (@email),
    // então a consulta já é parametrizada: não há risco de SQL Injection.
    public Administrador? ObterPorEmail(string email) =>
        _contexto.Administradores.FirstOrDefault(a => a.Email == email);

    // INSERT de um administrador novo (usado pelas contas de teste).
    public void Adicionar(Administrador administrador)
    {
        _contexto.Administradores.Add(administrador);
        _contexto.SaveChanges();
    }

    // UPDATE, usado no login para gravar a data do último acesso.
    public void Atualizar(Administrador administrador)
    {
        _contexto.Administradores.Update(administrador);
        _contexto.SaveChanges();
    }
}
