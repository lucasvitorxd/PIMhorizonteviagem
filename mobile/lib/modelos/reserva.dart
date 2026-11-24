// Dados usados pelas telas de reservas, com os mesmos campos JSON da API.
class Reserva {
  final Map<String, dynamic> _dados;
  Reserva.fromJson(Map<String, dynamic> dados)
    : _dados = Map.unmodifiable(dados);
  int get id => _dados['id'];
  String get nomePacote => _dados['nomePacote'];
  int get viajantes => _dados['viajantes'];
  int get quartos => _dados['quartos'];
  String get status => _dados['status'];
  num get valor => _dados['valor'];
  String? get pagamentoUrl => _dados['pagamentoUrl'];

  // Preserva os dados da reserva ao refletir um cancelamento já aceito pela API.
  Reserva comStatus(String status) =>
      Reserva.fromJson({..._dados, 'status': status});
}
