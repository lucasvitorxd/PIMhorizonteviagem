using System.ComponentModel.DataAnnotations;

namespace HorizonteViagens.Modelos;

// Classes do portal do cliente: favoritos, documentos da viagem,
// os dados que chegam numa reserva e o orçamento calculado.

// Um combo que o cliente marcou como favorito (o "coraçãozinho" da vitrine).
// A chave é a dupla ClienteId + ComboId, então o mesmo favorito não repete.
public class Favorito
{
    public int ClienteId { get; set; }
    public int ComboId { get; set; }
}

// Documento que a agência anexa depois que a reserva é confirmada
// (voucher do hotel, bilhete aéreo, ingresso). Fica na "carteira" do cliente.
public class DocumentoViagem
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public string Tipo { get; set; } = "Voucher";
    public string Titulo { get; set; } = "";

    // Link do documento emitido pelo fornecedor (sempre HTTPS).
    public string Url { get; set; } = "";

    // Localizador ou código de confirmação do fornecedor.
    public string Codigo { get; set; } = "";

    public DateTime EmitidoEm { get; set; } = DateTime.UtcNow;
}

// O que o site ou o app manda quando o cliente clica em "reservar".
public class ReservaEntrada
{
    [Range(1, int.MaxValue)]
    public int ComboId { get; set; }

    // De 1 a 6 adultos por reserva.
    [Range(1, 6)]
    public int Viajantes { get; set; } = 1;

    // Número único gerado pela tela para cada pedido. Se o mesmo pedido chegar
    // duas vezes (clique duplo, internet lenta), o sistema percebe e não cria outra reserva.
    public Guid RequisicaoId { get; set; }

    // O total que o cliente viu na tela. O servidor recalcula e, se não bater,
    // recusa a reserva ("o preço mudou"). Os limites usam ponto decimal
    // para não dar problema com a vírgula do português.
    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true)]
    public decimal TotalEsperado { get; set; }
}

// O que o administrador preenche para anexar um documento à reserva.
public class DocumentoEntrada
{
    [Required, StringLength(40)]
    public string Tipo { get; set; } = "Voucher";

    [Required, StringLength(140)]
    public string Titulo { get; set; } = "";

    [Required, StringLength(1000), Url]
    public string Url { get; set; } = "";

    [Required, StringLength(100)]
    public string Codigo { get; set; } = "";
}

// Resultado do cálculo de preço de um combo para um grupo de viajantes.
// "record" é uma classe só de leitura: depois de criada, ninguém muda os valores.
public record Orcamento(
    int ComboId,
    int Viajantes,
    int Quartos,
    int Noites,
    decimal Transporte,
    decimal Hospedagem,
    decimal Experiencias,
    decimal Total);
