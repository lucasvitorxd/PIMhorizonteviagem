using HorizonteViagens.Modelos;

namespace HorizonteViagens.Negocio;

// Calcula o preço de um combo para um grupo de viajantes.
// Fica numa classe só, para a vitrine, o orçamento e a reserva usarem
// exatamente a mesma conta (e o preço da tela nunca ser diferente do cobrado).
public static class Precificacao
{
    // Regra: cada quarto acomoda até 2 adultos. Transporte e passeios são por
    // pessoa; a hospedagem é por quarto e por noite.
    // Exemplo: 3 adultos ocupam 2 quartos ((3 + 1) / 2 = 2, divisão inteira).
    public static Orcamento Calcular(Combo combo, int viajantes)
    {
        if (viajantes is < 1 or > 6)
            throw new RegraDeNegocioException("Escolha de 1 a 6 viajantes adultos.");

        var quartos = (viajantes + 1) / 2;

        var transporte = decimal.Round(combo.Valor * viajantes, 2);
        var hospedagem = decimal.Round(combo.DiariaQuarto * combo.Noites * quartos, 2);
        var experiencias = decimal.Round(combo.ValorExperiencias * viajantes, 2);
        var total = transporte + hospedagem + experiencias;

        return new Orcamento(combo.Id, viajantes, quartos, combo.Noites, transporte, hospedagem, experiencias, total);
    }
}
