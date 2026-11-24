using System.Data;
using HorizonteViagens.Modelos;
using HorizonteViagens.Repositorio.Contexto;
using HorizonteViagens.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HorizonteViagens.Repositorio;

// Única classe que grava reservas, favoritos, documentos e status de pagamento.
public class PortalRepositorio : IPortalRepositorio
{
    private readonly AppDbContext _contexto;

    public PortalRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    // ---------- Catálogo e clientes ----------

    // Só aparecem no site os combos que o administrador marcou como publicados.
    public List<Combo> Catalogo() =>
        _contexto.Combos
            .AsNoTracking()
            .Where(c => c.Publicado)
            .OrderBy(c => c.Nome)
            .ToList();

    public Combo? Combo(int id) =>
        _contexto.Combos.AsNoTracking().FirstOrDefault(c => c.Id == id);

    // Cliente inativo ou com acesso vencido é tratado como se não existisse.
    public Cliente? Cliente(int id) =>
        _contexto.Clientes.FirstOrDefault(c =>
            c.Id == id &&
            c.Ativo &&
            (c.AcessoAte == null || c.AcessoAte > DateTime.UtcNow));

    // ---------- Reservas ----------

    // clienteId nulo = administrador vendo todas as reservas.
    public List<Compra> Reservas(int? clienteId) =>
        _contexto.Compras
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Combo)
            .Where(c => clienteId == null || c.ClienteId == clienteId)
            .OrderByDescending(c => c.Id)
            .ToList();

    public Compra? Reserva(int id, int? clienteId = null) =>
        _contexto.Compras
            .Include(c => c.Combo)
            .Include(c => c.Cliente)
            .FirstOrDefault(c => c.Id == id && (clienteId == null || c.ClienteId == clienteId));

    public Compra? PorRequisicao(int clienteId, Guid requisicao) =>
        _contexto.Compras.FirstOrDefault(c => c.ClienteId == clienteId && c.RequisicaoId == requisicao);

    // Transação SERIALIZABLE: enquanto esta reserva não termina, ninguém mais
    // consegue mexer nas vagas do mesmo combo. Assim o preço, as vagas e a
    // checagem de pedido repetido ficam todos na mesma "fotografia" do banco.
    public Compra Reservar(ReservaEntrada entrada, int clienteId, Func<Combo, Orcamento> validar)
    {
        using var transacao = _contexto.Database.BeginTransaction(IsolationLevel.Serializable);

        // Se o mesmo pedido chegar duas vezes (clique duplo, internet lenta),
        // devolve a reserva que já existe em vez de criar outra.
        var antiga = PorRequisicao(clienteId, entrada.RequisicaoId);
        if (antiga != null)
        {
            if (antiga.ComboId != entrada.ComboId ||
                antiga.Viajantes != entrada.Viajantes ||
                antiga.Valor != entrada.TotalEsperado)
            {
                throw new InvalidOperationException("Identificador já utilizado para outra reserva.");
            }

            transacao.Commit();
            return antiga;
        }

        var combo = _contexto.Combos.FirstOrDefault(c => c.Id == entrada.ComboId)
            ?? throw new InvalidOperationException("Pacote não encontrado.");

        // As regras (vagas, preço, publicação) estão no PortalNegocio;
        // o repositório só pede para ele conferir antes de gravar.
        var orcamento = validar(combo);
        combo.Vagas -= entrada.Viajantes;

        var reserva = new Compra
        {
            ComboId = combo.Id,
            ClienteId = clienteId,
            Viajantes = entrada.Viajantes,
            Quartos = orcamento.Quartos,
            Noites = orcamento.Noites,
            Valor = orcamento.Total,
            NomePacote = combo.Nome,
            Destino = combo.Destino,
            Partida = combo.Partida,
            RequisicaoId = entrada.RequisicaoId
        };

        _contexto.Compras.Add(reserva);
        _contexto.SaveChanges();
        transacao.Commit();
        return reserva;
    }

    // Reserva pendente que passou do prazo devolve as vagas para o combo.
    public void Expirar()
    {
        using var transacao = _contexto.Database.BeginTransaction(IsolationLevel.Serializable);

        var vencidas = _contexto.Compras
            .Include(c => c.Combo)
            .Where(c => c.Status == "Pendente" && c.ExpiraEm < DateTime.UtcNow)
            .ToList();

        foreach (var compra in vencidas)
        {
            compra.Status = "Expirada";
            compra.Combo.Vagas += compra.Viajantes;
        }

        _contexto.SaveChanges();
        transacao.Commit();
    }

    public bool Cancelar(int id, int clienteId)
    {
        using var transacao = _contexto.Database.BeginTransaction(IsolationLevel.Serializable);

        var compra = Reserva(id, clienteId);
        if (compra == null)
            return false;

        if (compra.Status == "Cancelada")
            return true;

        // Depois que o pagamento começou, o cancelamento passa pela agência.
        if (compra.Status != "Pendente" || compra.PreferenciaId != null)
            throw new InvalidOperationException("Esta reserva exige atendimento para cancelamento. Não há reembolso automático.");

        compra.Status = "Cancelada";
        compra.Combo.Vagas += compra.Viajantes;
        _contexto.SaveChanges();
        transacao.Commit();
        return true;
    }

    // ---------- Favoritos e documentos ----------

    public List<int> Favoritos(int clienteId) =>
        _contexto.Favoritos
            .Where(f => f.ClienteId == clienteId)
            .Select(f => f.ComboId)
            .ToList();

    public void Favoritar(int clienteId, int comboId, bool adicionar)
    {
        if (adicionar && !_contexto.Combos.Any(c => c.Id == comboId && c.Publicado))
            throw new InvalidOperationException("Pacote não encontrado.");

        var favorito = _contexto.Favoritos.Find(clienteId, comboId);

        if (adicionar && favorito == null)
            _contexto.Favoritos.Add(new Favorito { ClienteId = clienteId, ComboId = comboId });

        if (!adicionar && favorito != null)
            _contexto.Favoritos.Remove(favorito);

        _contexto.SaveChanges();
    }

    public List<DocumentoViagem> Documentos(int compraId) =>
        _contexto.Documentos
            .AsNoTracking()
            .Where(d => d.CompraId == compraId)
            .ToList();

    public void Documento(DocumentoViagem documento)
    {
        _contexto.Documentos.Add(documento);
        _contexto.SaveChanges();
    }

    // ---------- Pagamento ----------

    // Confere se o pagamento que o Mercado Pago informou bate com a reserva
    // (referência, valor e ambiente de teste) antes de confirmar.
    public void AplicarPagamento(int id, string pagamentoId, string status, decimal valor, string referencia, bool teste)
    {
        using var transacao = _contexto.Database.BeginTransaction(IsolationLevel.Serializable);

        var compra = Reserva(id) ?? throw new InvalidOperationException("Reserva não encontrada.");

        if (!teste || referencia != $"HV-{compra.Id}-{compra.RequisicaoId:N}" || valor != compra.Valor)
            throw new InvalidOperationException("Pagamento incompatível com a reserva.");

        if (compra.PagamentoId != null && compra.PagamentoId != pagamentoId)
            throw new InvalidOperationException("Pagamento duplicado requer conferência.");

        if (status == "approved")
        {
            if (compra.Status == "Pendente" && compra.ExpiraEm >= DateTime.UtcNow)
                compra.Status = "Confirmada";
            else if (compra.Status != "Confirmada")
                compra.Status = "Revisão de pagamento";

            compra.PagamentoId = pagamentoId;
        }

        // Pagamento devolvido ou contestado volta para revisão da agência.
        if (status is "refunded" or "charged_back")
        {
            compra.Status = "Revisão de pagamento";
            compra.PagamentoId = pagamentoId;
        }

        _contexto.SaveChanges();
        transacao.Commit();
    }

    public void Salvar() => _contexto.SaveChanges();

    // ---------- Preparação do banco ----------

    // Aplica as migrations pendentes (o mesmo que o Update-Database faz).
    public void AtualizarBanco() => _contexto.Database.Migrate();

    public bool ExisteCombo() => _contexto.Combos.Any();

    public void AdicionarCombos(List<Combo> combos)
    {
        _contexto.Combos.AddRange(combos);
        _contexto.SaveChanges();
    }
}
