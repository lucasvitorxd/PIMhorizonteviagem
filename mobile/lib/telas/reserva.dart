// Detalha a reserva e mantém as ações de pagamento, consulta e cancelamento.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import '../tema.dart';
import '../modelos/reserva.dart';
import '../servicos/links.dart';

// Detalhe de uma reserva, com os botões de pagar, conferir e cancelar.
class ReservaPage extends StatefulWidget {
  final HorizonteApi api;
  final Reserva reserva;
  const ReservaPage({super.key, required this.api, required this.reserva});
  @override
  State<ReservaPage> createState() => _ReservaState();
}

class _ReservaState extends State<ReservaPage> {
  late Reserva r;
  bool busy = false;
  String? erro;
  @override
  // Prepara o estado uma vez, quando esta tela é criada.
  void initState() {
    super.initState();
    r = widget.reserva;
  }

  // Bloqueia cliques repetidos enquanto aguarda a resposta da ação solicitada.
  Future<void> acao(String tipo) async {
    setState(() => busy = true);
    try {
      final d = await widget.api.request(
        'POST',
        '/api/portal/reservas/${r.id}/$tipo',
      );
      // pagamento: abre o Mercado Pago de teste; verificar: traz o status novo;
      // cancelar: a Api aceitou, então só atualizamos o status na tela.
      if (tipo == 'pagamento') {
        await abrirUrl(d['url']);
      } else if (tipo == 'verificar') {
        setState(() => r = Reserva.fromJson(Map<String, dynamic>.from(d)));
      } else {
        setState(() => r = r.comStatus('Cancelada'));
      }
    } catch (e) {
      if (mounted) setState(() => erro = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text('Reserva HV-${r.id}')),
    body: ListView(
      padding: const EdgeInsets.all(24),
      children: [
        Text(
          r.nomePacote,
          style: const TextStyle(fontSize: 30, fontWeight: FontWeight.bold),
        ),
        Text('${r.viajantes} adulto(s) · ${r.quartos} quarto(s)'),
        Text(r.status, style: const TextStyle(fontWeight: FontWeight.bold)),
        Text(moeda.format(r.valor), style: const TextStyle(fontSize: 28)),
        const SizedBox(height: 30),
        if (r.status == 'Pendente') ...[
          const Text(
            'Pagamento de teste. Nenhuma cobrança real. Após finalizar, volte e atualize o status.',
          ),
          FilledButton(
            onPressed: busy ? null : () => acao('pagamento'),
            child: const Text('Abrir pagamento de teste'),
          ),
        ],
        OutlinedButton(
          onPressed: busy ? null : () => acao('verificar'),
          child: const Text('Atualizar status'),
        ),
        // Depois que o pagamento foi aberto, só a agência pode cancelar.
        if (r.status == 'Pendente' && r.pagamentoUrl == null)
          TextButton(
            onPressed: busy ? null : () => acao('cancelar'),
            child: const Text('Cancelar reserva pendente'),
          ),
        if (erro != null)
          Text(erro!, style: const TextStyle(color: Colors.red)),
        const SizedBox(height: 25),
        const Text(
          'Os documentos emitidos pela agência ficam na aba Carteira.',
        ),
      ],
    ),
  );
}
