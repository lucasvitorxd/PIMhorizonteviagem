using System.ComponentModel.DataAnnotations;

namespace HorizonteViagens.Modelos.Dtos;

// Compra simples pela Api: o cliente escolhe o combo (e, se quiser, quantos viajantes).
// A identidade vem do token, nunca do JSON. O preço é calculado no servidor.
public class CompraCriarDto
{
    [Range(1, int.MaxValue)]
    public int ComboId { get; set; }

    // Se não vier no JSON, vale 1 viajante.
    [Range(1, 6)]
    public int Viajantes { get; set; } = 1;
}
