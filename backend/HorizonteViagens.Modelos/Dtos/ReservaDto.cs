namespace HorizonteViagens.Modelos.Dtos;

// DTO de SAÍDA de uma reserva: o mesmo formato para o site, a Api e o app.
// Não leva senha do cliente nem as navegações do EF (Cliente, Combo).
public record ReservaDto(
    int Id,
    int ComboId,
    string NomePacote,
    string Destino,
    decimal Valor,
    int Viajantes,
    int Quartos,
    int Noites,
    DateOnly? Partida,
    string Status,
    DateTime ExpiraEm,
    string? PagamentoUrl)
{
    public static ReservaDto De(Compra c) => new(
        c.Id,
        c.ComboId,
        c.NomePacote,
        c.Destino,
        c.Valor,
        c.Viajantes,
        c.Quartos,
        c.Noites,
        c.Partida,
        c.Status,
        c.ExpiraEm,
        c.PagamentoUrl);
}
