// Dados do catálogo. O cálculo de estimativa recebido foi preservado nesta reorganização.
class Combo {
  final int id, noites, vagas;
  final String nome, destino, origem, evento, imagem, inclusoes, categoria;
  final double valor, diaria, experiencias;
  final bool confirmado;
  final DateTime? partida;
  Combo.fromJson(Map<String, dynamic> j)
    : id = j['id'],
      noites = j['noites'],
      vagas = j['vagas'],
      nome = j['nome'],
      destino = j['destino'],
      origem = j['origem'],
      evento = j['evento'],
      imagem = j['imagem'],
      inclusoes = j['inclusoes'],
      categoria = j['categoria'],
      valor = (j['valor'] as num).toDouble(),
      diaria = (j['diariaQuarto'] as num).toDouble(),
      experiencias = (j['valorExperiencias'] as num).toDouble(),
      confirmado = j['precoConfirmado'],
      partida = DateTime.tryParse(j['partida'] ?? '');
  // Mesma conta da classe Precificacao do C#: transporte e experiências por
  // adulto, mais diária por quarto de até 2 adultos. Serve só para o card;
  // se a regra mudar no servidor, esta conta precisa mudar junto.
  double total(int adultos) =>
      (((valor + experiencias) * adultos +
                  diaria * noites * ((adultos + 1) ~/ 2)) *
              100)
          .round() /
      100;
}
