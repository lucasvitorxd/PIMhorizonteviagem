using HorizonteViagens.Modelos;
using HorizonteViagens.Repositorio.Interfaces;

namespace HorizonteViagens.Negocio;

// Um combo da vitrine já com o preço calculado para o grupo e a resposta
// "dá para reservar agora?". A Web só mostra esses valores, sem fazer conta.
public record ItemVitrine(Combo Combo, Orcamento Orcamento, bool Reservavel)
{
    // A divisão do total também pertence ao Negocio; o ViewModel só copia o resultado.
    public decimal PorPessoa => decimal.Round(Orcamento.Total / Orcamento.Viajantes, 2);
}

// Regras do portal do cliente: catálogo, orçamento, reservas, favoritos e documentos.
// É usado pela Api (app e site) e pela Web MVC.
public class PortalNegocio
{
    private readonly IPortalRepositorio _repositorio;

    public PortalNegocio(IPortalRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public List<Combo> Catalogo() => _repositorio.Catalogo();

    // Calcula o preço de um combo para a quantidade de viajantes informada.
    public Orcamento Orcar(int comboId, int viajantes)
    {
        var combo = _repositorio.Combo(comboId)
            ?? throw new RegraDeNegocioException("Pacote não encontrado.");

        if (!combo.Publicado)
            throw new RegraDeNegocioException("Pacote indisponível.");

        return Precificacao.Calcular(combo, viajantes);
    }

    // Vitrine da Web: filtra, calcula o preço de cada combo para o grupo e,
    // se pedirem, ordena do menor para o maior total. Antes isso ficava
    // espalhado entre o controller, a view (_Card) e o JavaScript.
    public List<ItemVitrine> Vitrine(string? destino, string? categoria, int viajantes, string? ordem)
    {
        viajantes = NormalizarViajantes(viajantes);

        // O filtro de destino também procura no nome do evento (ex.: "carnaval").
        var itens = _repositorio.Catalogo()
            .Where(c => string.IsNullOrWhiteSpace(destino) ||
                        (c.Destino + " " + c.Evento).Contains(destino.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(c => string.IsNullOrWhiteSpace(categoria) || c.Categoria == categoria)
            .Select(c => new ItemVitrine(c, Precificacao.Calcular(c, viajantes), Reservavel(c, viajantes)));

        if (ordem == "preco")
            itens = itens.OrderBy(i => i.Orcamento.Total);

        return itens.ToList();
    }

    // Página de detalhe de um combo publicado. Devolve null se ele não existir
    // ou não estiver publicado, e a Web responde 404.
    public ItemVitrine? Detalhe(int comboId, int viajantes)
    {
        var combo = _repositorio.Combo(comboId);
        if (combo is null || !combo.Publicado)
            return null;

        viajantes = NormalizarViajantes(viajantes);
        return new ItemVitrine(combo, Precificacao.Calcular(combo, viajantes), Reservavel(combo, viajantes));
    }

    // As mesmas condições que o Reservar confere: publicado, preço confirmado,
    // partida no futuro e vagas suficientes para o grupo. A tela usa isto para
    // decidir se mostra o botão "Reservar" ou o aviso "saída sob consulta".
    public static bool Reservavel(Combo combo, int viajantes) =>
        combo.Publicado &&
        combo.PrecoConfirmado &&
        combo.Partida is not null &&
        combo.Partida > DateOnly.FromDateTime(DateTime.Today) &&
        combo.Vagas >= viajantes;

    // Mantém a quantidade da busca e do orçamento dentro dos limites aceitos.
    public static int NormalizarViajantes(int viajantes) => Math.Clamp(viajantes, 1, 6);

    // A apresentação consulta esta regra; o cancelamento continua validado no servidor.
    public static bool PodeCancelar(Compra compra) =>
        compra.Status == "Pendente" && compra.PreferenciaId == null;

    public Cliente Cliente(int id) =>
        _repositorio.Cliente(id)
            ?? throw new RegraDeNegocioException("Cliente não encontrado ou inativo.");

    public Compra Reservar(int clienteId, ReservaEntrada entrada)
    {
        Cliente(clienteId);

        if (entrada.RequisicaoId == Guid.Empty)
            throw new RegraDeNegocioException("Informe o identificador da requisição.");

        // Pedido repetido (mesmo RequisicaoId): devolve a reserva já feita.
        var antiga = _repositorio.PorRequisicao(clienteId, entrada.RequisicaoId);
        if (antiga != null)
        {
            if (antiga.ComboId != entrada.ComboId ||
                antiga.Viajantes != entrada.Viajantes ||
                antiga.Valor != entrada.TotalEsperado)
            {
                throw new RegraDeNegocioException("Identificador já utilizado para outra reserva.");
            }

            return antiga;
        }

        // Estas regras rodam DENTRO da transação do repositório,
        // com o combo já travado para os outros pedidos.
        return _repositorio.Reservar(entrada, clienteId, combo =>
        {
            if (!combo.Publicado ||
                !combo.PrecoConfirmado ||
                combo.Partida is null ||
                combo.Partida <= DateOnly.FromDateTime(DateTime.Today))
            {
                throw new RegraDeNegocioException("Saída sob consulta. Aguarde a confirmação da agência.");
            }

            if (combo.Vagas <= 0)
                throw new RegraDeNegocioException("Não há vagas disponíveis para este combo.");

            if (combo.Vagas < entrada.Viajantes)
                throw new RegraDeNegocioException("Não há vagas para todos os viajantes.");

            var orcamento = Precificacao.Calcular(combo, entrada.Viajantes);

            if (orcamento.Total <= 0)
                throw new RegraDeNegocioException("A oferta precisa ter valor positivo.");

            // O preço que o cliente viu na tela precisa ser o mesmo de agora.
            if (orcamento.Total != entrada.TotalEsperado)
                throw new RegraDeNegocioException("O preço mudou. Consulte o pacote novamente.");

            return orcamento;
        });
    }

    // Compra simples (POST api/compras): o cliente manda só o combo.
    // O servidor calcula o preço e gera o identificador do pedido,
    // e depois segue exatamente o mesmo caminho da reserva do site.
    public Compra Comprar(int clienteId, int comboId, int viajantes = 1)
    {
        var orcamento = Orcar(comboId, viajantes);

        var entrada = new ReservaEntrada
        {
            ComboId = comboId,
            Viajantes = viajantes,
            RequisicaoId = Guid.NewGuid(),
            TotalEsperado = orcamento.Total
        };

        return Reservar(clienteId, entrada);
    }

    // clienteId nulo = administrador. Antes de listar, devolve as vagas das reservas vencidas.
    public List<Compra> Reservas(int? clienteId)
    {
        if (clienteId != null)
            Cliente(clienteId.Value);

        _repositorio.Expirar();
        return _repositorio.Reservas(clienteId);
    }

    public Compra Reserva(int id, int? clienteId)
    {
        if (clienteId != null)
            Cliente(clienteId.Value);

        return _repositorio.Reserva(id, clienteId)
            ?? throw new RegraDeNegocioException("Reserva não encontrada.");
    }

    public List<int> Favoritos(int clienteId) => _repositorio.Favoritos(clienteId);

    public void Favoritar(int clienteId, int comboId, bool adicionar)
    {
        Cliente(clienteId);
        _repositorio.Favoritar(clienteId, comboId, adicionar);
    }

    // Documentos (voucher, bilhete) só aparecem depois que a reserva é confirmada.
    public List<DocumentoViagem> Documentos(int compraId, int? clienteId)
    {
        var compra = Reserva(compraId, clienteId);
        return compra.Status == "Confirmada" ? _repositorio.Documentos(compraId) : [];
    }

    public void Emitir(int compraId, DocumentoEntrada dto)
    {
        var compra = Reserva(compraId, null);

        if (compra.Status != "Confirmada")
            throw new RegraDeNegocioException("A reserva precisa estar confirmada para receber documentos.");

        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var endereco) || endereco.Scheme != "https")
            throw new RegraDeNegocioException("Use uma URL HTTPS do documento emitido pelo fornecedor.");

        _repositorio.Documento(new DocumentoViagem
        {
            CompraId = compraId,
            Tipo = dto.Tipo,
            Titulo = dto.Titulo,
            Url = dto.Url,
            Codigo = dto.Codigo
        });
    }

    public bool Cancelar(int id, int clienteId) => _repositorio.Cancelar(id, clienteId);
}
