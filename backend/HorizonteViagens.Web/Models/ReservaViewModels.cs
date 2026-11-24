using HorizonteViagens.Modelos;
using HorizonteViagens.Negocio;

namespace HorizonteViagens.Web.Models;

// ViewModels das telas de reservas e da carteira de documentos.
// As telas usavam a entidade Compra direto; agora recebem só o que mostram,
// e as perguntas do tipo "pode pagar?" já chegam respondidas.

// Um documento da viagem (voucher, bilhete, ingresso) emitido pela agência.
public class DocumentoViewModel
{
    public string Tipo { get; init; } = "";
    public string Titulo { get; init; } = "";
    public string Codigo { get; init; } = "";
    public string Url { get; init; } = "";

    public static DocumentoViewModel De(DocumentoViagem d) => new()
    {
        Tipo = d.Tipo,
        Titulo = d.Titulo,
        Codigo = d.Codigo,
        Url = d.Url
    };
}

// Uma reserva, usada na lista "Minhas viagens" e na tela de detalhe.
public class ReservaViewModel
{
    public int Id { get; init; }

    // Código que o cliente vê e usa no atendimento (ex.: HV-15).
    public string Codigo => $"HV-{Id}";

    public string NomePacote { get; init; } = "";
    public string Destino { get; init; } = "";
    public string PartidaTexto { get; init; } = "";
    public int Viajantes { get; init; }
    public int Quartos { get; init; }
    public int Noites { get; init; }
    public decimal Valor { get; init; }
    public string Status { get; init; } = "";

    // Prazo para pagar, já no horário do servidor (o banco guarda em UTC).
    public DateTime ExpiraEmLocal { get; init; }

    // Nome do cliente; só aparece para o administrador.
    public string ClienteNome { get; init; } = "";

    // Respostas prontas para a tela decidir quais botões mostrar.
    public bool Pendente => Status == "Pendente";
    public bool Confirmada => Status == "Confirmada";

    // Depois que o pagamento foi aberto no Mercado Pago, o cancelamento
    // passa a ser pela agência (a mesma regra do PortalRepositorio.Cancelar).
    public bool PodeCancelar { get; init; }

    // Documentos e pagamento só são preenchidos na tela de detalhe.
    public List<DocumentoViewModel> Documentos { get; init; } = new();
    public bool PagamentoConfigurado { get; init; }

    public static ReservaViewModel De(Compra c, List<DocumentoViagem>? documentos = null, bool pagamentoConfigurado = false) => new()
    {
        Id = c.Id,
        NomePacote = c.NomePacote,
        Destino = c.Destino,
        PartidaTexto = c.Partida?.ToString("dd/MM/yyyy") ?? "Data a confirmar",
        Viajantes = c.Viajantes,
        Quartos = c.Quartos,
        Noites = c.Noites,
        Valor = c.Valor,
        Status = c.Status,
        ExpiraEmLocal = c.ExpiraEm.ToLocalTime(),
        ClienteNome = c.Cliente?.Nome ?? "",
        PodeCancelar = PortalNegocio.PodeCancelar(c),
        Documentos = (documentos ?? new()).Select(DocumentoViewModel.De).ToList(),
        PagamentoConfigurado = pagamentoConfigurado
    };
}
