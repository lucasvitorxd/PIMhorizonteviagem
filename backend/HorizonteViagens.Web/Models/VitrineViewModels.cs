using HorizonteViagens.Negocio;

namespace HorizonteViagens.Web.Models;

// ViewModels da vitrine (página inicial, lista de pacotes e favoritos).
// Um ViewModel é a "bandeja" que o controller monta para a tela: só os dados
// que ela mostra, já formatados e calculados. Assim a view não faz conta e
// não precisa conhecer a entidade do banco.

// Um card de combo da vitrine.
public class ComboCardViewModel
{
    public int Id { get; init; }
    public string Destino { get; init; } = "";
    public string Categoria { get; init; } = "";
    public string Origem { get; init; } = "";
    public string Evento { get; init; } = "";
    public string Imagem { get; init; } = "";
    public int Noites { get; init; }

    // Ex.: "20/12/2026", ou o aviso de que a data ainda não foi definida.
    public string PartidaTexto { get; init; } = "";

    // Preço confirmado pela agência mostra "Total do grupo";
    // sem confirmação, o card avisa que é uma estimativa.
    public bool PrecoConfirmado { get; init; }

    // Os valores abaixo vêm prontos do Negocio (Precificacao.Calcular).
    public int Viajantes { get; init; }
    public decimal Total { get; init; }
    public decimal PorPessoa { get; init; }

    // Dias = noites + 1 (quem fica 6 noites viaja 7 dias).
    public int Dias => Noites + 1;

    // Converte o resultado do Negocio no que o card precisa.
    public static ComboCardViewModel De(ItemVitrine item) => new()
    {
        Id = item.Combo.Id,
        Destino = item.Combo.Destino,
        Categoria = item.Combo.Categoria,
        Origem = item.Combo.Origem,
        Evento = item.Combo.Evento,
        Imagem = item.Combo.Imagem,
        Noites = item.Combo.Noites,
        PartidaTexto = item.Combo.Partida?.ToString("dd/MM/yyyy") ?? "Próximas saídas sob consulta",
        PrecoConfirmado = item.Combo.PrecoConfirmado,
        Viajantes = item.Orcamento.Viajantes,
        Total = item.Orcamento.Total,
        PorPessoa = item.PorPessoa
    };
}

// A página de vitrine inteira: os filtros que a pessoa escolheu e os cards.
// O formulário de busca (_Busca) lê os filtros daqui para continuar preenchido.
public class VitrineViewModel
{
    public string? Destino { get; init; }
    public string? Categoria { get; init; }
    public int Viajantes { get; init; } = 2;
    public string? Ordem { get; init; }
    public List<ComboCardViewModel> Combos { get; init; } = new();

    // Link "ordenar pelo menor total" mantendo os outros filtros.
    public string LinkOrdenarPorPreco =>
        $"/Combos?destino={Uri.EscapeDataString(Destino ?? "")}" +
        $"&categoria={Uri.EscapeDataString(Categoria ?? "")}" +
        $"&viajantes={Viajantes}&ordem=preco";

    public static VitrineViewModel De(List<ItemVitrine> itens, string? destino, string? categoria, int viajantes, string? ordem) => new()
    {
        Destino = destino,
        Categoria = categoria,
        Viajantes = PortalNegocio.NormalizarViajantes(viajantes),
        Ordem = ordem,
        Combos = itens.Select(ComboCardViewModel.De).ToList()
    };
}

// A página de detalhe de um combo, com o orçamento separado por partes.
public class ComboDetalheViewModel
{
    public ComboCardViewModel Card { get; init; } = new();
    public string Nome { get; init; } = "";
    public string Descricao { get; init; } = "";

    // As inclusões vêm do banco num texto só, uma por linha; aqui viram uma lista.
    public List<string> Inclusoes { get; init; } = new();

    // Partes do orçamento (Precificacao): transporte, hospedagem e experiências.
    public int Quartos { get; init; }
    public decimal Transporte { get; init; }
    public decimal Hospedagem { get; init; }
    public decimal Experiencias { get; init; }

    // Resposta do Negocio: dá para reservar agora (preço confirmado, data futura e vagas)?
    public bool Reservavel { get; init; }

    // Número único deste formulário de reserva. Se o cliente clicar duas vezes,
    // o servidor reconhece o mesmo número e não cria uma segunda reserva.
    public Guid RequisicaoId { get; init; } = Guid.NewGuid();

    public static ComboDetalheViewModel De(ItemVitrine item) => new()
    {
        Card = ComboCardViewModel.De(item),
        Nome = item.Combo.Nome,
        Descricao = item.Combo.Descricao,
        Inclusoes = item.Combo.Inclusoes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList(),
        Quartos = item.Orcamento.Quartos,
        Transporte = item.Orcamento.Transporte,
        Hospedagem = item.Orcamento.Hospedagem,
        Experiencias = item.Orcamento.Experiencias,
        Reservavel = item.Reservavel
    };
}
