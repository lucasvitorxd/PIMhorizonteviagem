
using Microsoft.EntityFrameworkCore;
using HorizonteViagens.Modelos;
using HorizonteViagens.Repositorio.Contexto;
using HorizonteViagens.Repositorio.Interfaces;

namespace HorizonteViagens.Repositorio;

// Repositório responsável pelo acesso aos combos no banco de dados.
public class ComboRepositorio : IComboRepositorio
{
    private readonly AppDbContext _contexto;

    public ComboRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    // Lista todos os combos para o administrador.
    public List<Combo> Listar() =>
        _contexto.Combos
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToList();

    // Busca um combo pelo ID.
    public Combo? ObterPorId(int id) =>
        _contexto.Combos.FirstOrDefault(c => c.Id == id);

    // Cadastra um novo combo.
    public void Adicionar(Combo combo)
    {
        _contexto.Combos.Add(combo);
        _contexto.SaveChanges();
    }

    // Atualiza um combo existente.
    public void Atualizar(Combo combo)
    {
        _contexto.Combos.Update(combo);
        _contexto.SaveChanges();
    }

    // Remove um combo, respeitando as regras de integridade.
    public ResultadoExclusao Remover(Combo combo)
    {
        _contexto.Combos.Remove(combo);

        try
        {
            _contexto.SaveChanges();
            return ResultadoExclusao.Excluido;
        }
        catch (DbUpdateConcurrencyException)
        {
            _contexto.Entry(combo).State = EntityState.Detached;
            return ResultadoExclusao.AlteradoPorOutro;
        }
        catch (DbUpdateException) when (EmUso(combo.Id))
        {
            _contexto.Entry(combo).State = EntityState.Detached;
            return ResultadoExclusao.EmUso;
        }
    }

    // Verifica se o combo possui reservas ou serviços vinculados.
    public bool EmUso(int id) =>
        _contexto.Compras.Any(c => c.ComboId == id) ||
        _contexto.ItensCombos.Any(i => i.ComboId == id);

    // Busca os combos disponíveis diretamente pelo Entity Framework.
    // Não depende mais da procedure sp_Combos_BuscarPorDestino.
    public List<Combo> BuscarDisponiveis(string? destino = null)
    {
        var consulta = _contexto.Combos
            .AsNoTracking()
            .Where(c => c.Publicado && c.Vagas > 0);

        if (!string.IsNullOrWhiteSpace(destino))
        {
            var filtro = destino.Trim();

            consulta = consulta.Where(c =>
                c.Destino.Contains(filtro));
        }

        return consulta
            .OrderBy(c => c.Nome)
            .ToList();
    }
}