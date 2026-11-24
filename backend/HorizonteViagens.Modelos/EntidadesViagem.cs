using System.ComponentModel.DataAnnotations;

namespace HorizonteViagens.Modelos;

// Entidades da parte de viagem do banco unificado do grupo:
// hotéis, voos, passagens, quartos, passeios, combos e compras.
// Cada classe aqui vira uma tabela (o mapeamento fica no AppDbContext).

// Hotel parceiro da agência, com seus telefones e quartos.
public class Hotel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string? Classificacao { get; set; }
    public ICollection<TelefoneHotel> Telefones { get; set; } = new List<TelefoneHotel>();
    public ICollection<ReservaQuarto> Quartos { get; set; } = new List<ReservaQuarto>();
}

// Telefones extras de cada pessoa ou hotel ficam em tabelas separadas,
// porque um mesmo dono pode ter vários números.
public class TelefoneAdministrador
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int AdministradorId { get; set; }
    public Administrador Administrador { get; set; } = null!;
}

public class TelefoneCliente
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}

public class TelefoneHotel
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;
}

// Voo com cidade e horário de partida e de chegada.
public class Voo
{
    public int Id { get; set; }
    public string CidadePartida { get; set; } = string.Empty;
    public TimeOnly HoraPartida { get; set; }
    public string CidadeChegada { get; set; } = string.Empty;
    public TimeOnly HoraChegada { get; set; }
    public ICollection<Passagem> Passagens { get; set; } = new List<Passagem>();
}

// Passagem de um voo: classe, assento e datas de ida e volta.
public class Passagem
{
    public int Id { get; set; }
    public string Classe { get; set; } = string.Empty;
    public string Assento { get; set; } = string.Empty;
    public DateOnly DataIda { get; set; }
    public DateOnly DataVolta { get; set; }
    public int VooId { get; set; }
    public Voo Voo { get; set; } = null!;
    public ICollection<ItemCombo> ItensCombo { get; set; } = new List<ItemCombo>();
}

// Quarto reservado num hotel, com as datas de entrada e saída.
public class ReservaQuarto
{
    public int Id { get; set; }
    public string NumeroQuarto { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateOnly DataCheckin { get; set; }
    public DateOnly DataCheckout { get; set; }
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;
    public ICollection<ItemCombo> ItensCombo { get; set; } = new List<ItemCombo>();
}

// O pacote que a agência vende: destino, vagas, preço e o que está incluído.
// É o produto principal do sistema, mostrado na vitrine da Web e do app.
public class Combo
{
    public int Id { get; set; }

    // Campos da vitrine definidos na lista de pendências da Etapa 4.
    public string Nome { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public int Vagas { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public decimal Valor { get; set; }

    // Campos usados pelo portal para montar o card e calcular o preço.
    // Valor é o transporte por pessoa, DiariaQuarto é a diária de um quarto
    // para até 2 adultos e ValorExperiencias é o valor dos passeios por pessoa.
    public string Origem { get; set; } = "São Paulo";
    public string Evento { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Inclusoes { get; set; } = "";
    public string Imagem { get; set; } = "beach";
    public int Noites { get; set; } = 1;
    public decimal DiariaQuarto { get; set; }
    public decimal ValorExperiencias { get; set; }
    public DateOnly? Partida { get; set; }

    // Publicado: aparece na vitrine. PrecoConfirmado: a agência já conferiu
    // o preço com os fornecedores e o combo pode ser vendido.
    public bool Publicado { get; set; }
    public bool PrecoConfirmado { get; set; }

    // Concorrência otimista, como na Aula 03 de Desenvolvimento Web.
    // O SQL Server muda este número sozinho a cada alteração da linha. Ao gravar,
    // o EF Core confere se ele ainda é o mesmo que foi lido: se outra pessoa alterou
    // o combo nesse meio-tempo (por exemplo, comprou a última vaga), o EF Core lança
    // DbUpdateConcurrencyException em vez de sobrescrever em silêncio.
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public ICollection<ItemCombo> Itens { get; set; } = new List<ItemCombo>();
}

// A compra (ou reserva) de um combo feita por um cliente.
// Guarda uma cópia do nome, destino e valor do combo no momento da compra,
// para o histórico não mudar se o combo for alterado depois.
public class Compra
{
    public int Id { get; set; }
    // Compra sem combo foi tratada como inválida, conforme recomendação do PDF.
    public int ComboId { get; set; }
    public Combo Combo { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public decimal Valor { get; set; }
    public int Viajantes { get; set; } = 1;
    public int Quartos { get; set; } = 1;
    public int Noites { get; set; } = 1;
    public DateOnly? Partida { get; set; }
    public string NomePacote { get; set; } = "";
    public string Destino { get; set; } = "";
    // Pendente, Confirmada, Cancelada, Expirada ou Revisão de pagamento.
    public string Status { get; set; } = "Pendente";
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    // Reserva pendente tem 30 minutos para ser paga; depois disso as vagas voltam.
    public DateTime ExpiraEm { get; set; } = DateTime.UtcNow.AddMinutes(30);
    public Guid RequisicaoId { get; set; } = Guid.NewGuid();
    // Dados do pagamento de teste no Mercado Pago (vazios até o cliente pagar).
    public string? PreferenciaId { get; set; }
    public string? PagamentoUrl { get; set; }
    public string? PagamentoId { get; set; }
    // Mesmo controle de concorrência do Combo.
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
    public ICollection<MonitoramentoCompra> Monitoramentos { get; set; } = new List<MonitoramentoCompra>();
}

// Registra qual administrador acompanhou cada compra.
public class MonitoramentoCompra
{
    public int AdministradorId { get; set; }
    public Administrador Administrador { get; set; } = null!;
    public int CompraId { get; set; }
    public Compra Compra { get; set; } = null!;
}

// Passeio ou experiência com local, horário e capacidade.
public class Passeio
{
    public int Id { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public DateTime Horario { get; set; }
    public int Capacidade { get; set; }
    public ICollection<ItemCombo> ItensCombo { get; set; } = new List<ItemCombo>();
}

// Liga um combo ao quarto, à passagem e ao passeio que fazem parte dele.
public class ItemCombo
{
    public int QuartoId { get; set; }
    public ReservaQuarto Quarto { get; set; } = null!;
    public int PassagemId { get; set; }
    public Passagem Passagem { get; set; } = null!;
    public int PasseioId { get; set; }
    public Passeio Passeio { get; set; } = null!;
    public int ComboId { get; set; }
    public Combo Combo { get; set; } = null!;
}
