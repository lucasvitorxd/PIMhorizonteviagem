// Lista as reservas da conta e abre seus detalhes.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import '../tema.dart';
import '../modelos/reserva.dart';
import 'reserva.dart';

// Aba Viagens: lista as reservas do cliente logado. Tocar abre o detalhe
// e, na volta, a lista é recarregada para mostrar o status atualizado.
class ReservasTela extends StatelessWidget {
  final HorizonteApi api;
  final List<Reserva> lista;
  final VoidCallback atualizar;
  const ReservasTela({
    super.key,
    required this.api,
    required this.lista,
    required this.atualizar,
  });
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      const Text(
        'Minhas viagens',
        style: TextStyle(fontSize: 30, fontWeight: FontWeight.bold),
      ),
      if (lista.isEmpty)
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 60),
          child: Text(
            'Você ainda não tem reservas. Encontre seu destino na aba Explorar.',
          ),
        ),
      ...lista.map(
        (r) => Card(
          child: ListTile(
            contentPadding: const EdgeInsets.all(20),
            title: Text(r.nomePacote),
            subtitle: Text(
              '${r.viajantes} adulto(s) · ${r.status}\n${moeda.format(r.valor)}',
            ),
            trailing: const Icon(Icons.chevron_right),
            onTap: () async {
              await Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (_) => ReservaPage(api: api, reserva: r),
                ),
              );
              atualizar();
            },
          ),
        ),
      ),
    ],
  );
}
