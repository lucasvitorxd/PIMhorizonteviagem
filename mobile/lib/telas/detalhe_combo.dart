// Exibe o orçamento do servidor e envia a reserva com o total consultado.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import '../tema.dart';
import '../modelos/combo.dart';
import '../modelos/reserva.dart';
import '../servicos/requisicao.dart';
import 'login.dart';
import 'reserva.dart';

// Tela de um pacote. O orçamento vem da Api (a mesma regra de preço do site),
// e a reserva manda o total que o cliente viu para o servidor conferir.
class DetalheCombo extends StatefulWidget {
  final HorizonteApi api;
  final Combo combo;
  final int adultos;
  const DetalheCombo({
    super.key,
    required this.api,
    required this.combo,
    required this.adultos,
  });
  @override
  State<DetalheCombo> createState() => _DetalheState();
}

class _DetalheState extends State<DetalheCombo> {
  late int adultos;
  late Future<dynamic> orcamento;
  // Identificador desta tentativa de reserva. Se o cliente tocar duas vezes,
  // o servidor reconhece a mesma chave e não cria outra reserva.
  String chave = requisicao();
  String? erro;
  bool busy = false;
  @override
  // Prepara o estado uma vez, quando esta tela é criada.
  void initState() {
    super.initState();
    adultos = widget.adultos;
    carregar();
  }

  // Consulta o orçamento novamente ao mudar a quantidade de viajantes.
  void carregar() {
    orcamento = widget.api.request(
      'GET',
      '/api/portal/orcamento/${widget.combo.id}?viajantes=$adultos',
    );
  }

  @override
  Widget build(BuildContext context) {
    final c = widget.combo;
    return Scaffold(
      appBar: AppBar(title: Text(c.destino)),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: Image.asset(
              'assets/${c.imagem}.jpg',
              height: 210,
              fit: BoxFit.cover,
            ),
          ),
          const SizedBox(height: 20),
          Text(
            c.nome,
            style: const TextStyle(fontSize: 30, fontWeight: FontWeight.bold),
          ),
          Text(c.evento),
          Text('${c.noites + 1} dias / ${c.noites} noites'),
          Text(c.inclusoes),
          const SizedBox(height: 24),
          DropdownButtonFormField<int>(
            initialValue: adultos,
            decoration: const InputDecoration(labelText: 'Viajantes adultos'),
            items: List.generate(
              6,
              (i) => DropdownMenuItem(
                value: i + 1,
                child: Text('${i + 1} adulto(s)'),
              ),
            ),
            onChanged: busy
                ? null
                : (v) => setState(() {
                    adultos = v!;
                    chave = requisicao();
                    carregar();
                  }),
          ),
          const SizedBox(height: 20),
          // Mostra o orçamento separado em transporte, hospedagem e experiências.
          FutureBuilder<dynamic>(
            future: orcamento,
            builder: (context, s) {
              if (s.hasError) {
                return Column(
                  children: [
                    Text(s.error.toString()),
                    TextButton(
                      onPressed: () => setState(carregar),
                      child: const Text('Tentar novamente'),
                    ),
                  ],
                );
              }
              if (!s.hasData) {
                return const Center(child: CircularProgressIndicator());
              }
              final o = s.data;
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Transporte: ${moeda.format(o['transporte'])}\n${o['quartos']} quarto(s) / ${o['noites']} noites: ${moeda.format(o['hospedagem'])}\nExperiências: ${moeda.format(o['experiencias'])}',
                  ),
                  Text(
                    'Total ${c.confirmado ? '' : 'estimado'}: ${moeda.format(o['total'])}',
                    style: const TextStyle(
                      fontSize: 26,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  if (erro != null)
                    Text(erro!, style: const TextStyle(color: Colors.red)),
                  const SizedBox(height: 20),
                  // Só mostra o botão quando faz sentido reservar. É só para a tela:
                  // quem decide de verdade é o servidor, que recusa se algo mudou.
                  if (!c.confirmado ||
                      c.partida == null ||
                      !c.partida!.isAfter(DateTime.now()) ||
                      c.vagas < adultos)
                    const Text(
                      'Saída sob consulta. Aguarde a confirmação de data, preço e vagas pela agência.',
                    )
                  else
                    FilledButton(
                      onPressed: busy
                          ? null
                          : () async {
                              if (!widget.api.autenticado) {
                                await Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (_) => LoginPage(api: widget.api),
                                  ),
                                );
                                return;
                              }
                              setState(() => busy = true);
                              try {
                                final reserva = await widget.api
                                    // Envia a reserva. totalEsperado é o valor da tela: se o preço mudou,
                                    // a Api recusa em vez de cobrar outro valor.
                                    .request('POST', '/api/portal/reservas', {
                                      'comboId': c.id,
                                      'viajantes': adultos,
                                      'totalEsperado': o['total'],
                                      'requisicaoId': chave,
                                    });
                                if (context.mounted) {
                                  await Navigator.pushReplacement(
                                    context,
                                    MaterialPageRoute(
                                      builder: (_) => ReservaPage(
                                        api: widget.api,
                                        reserva: Reserva.fromJson(
                                          Map<String, dynamic>.from(reserva),
                                        ),
                                      ),
                                    ),
                                  );
                                }
                              } catch (e) {
                                if (mounted) {
                                  setState(() => erro = e.toString());
                                }
                              } finally {
                                if (mounted) setState(() => busy = false);
                              }
                            },
                      child: Text(busy ? 'Reservando…' : 'Reservar vagas'),
                    ),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}
