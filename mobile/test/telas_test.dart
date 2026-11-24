// Confere os contratos e a navegação após separar as telas do main.dart.
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:horizonte_viagens/modelos/documento.dart';
import 'package:horizonte_viagens/modelos/reserva.dart';
import 'package:horizonte_viagens/servicos/horizonte_api.dart';
import 'package:horizonte_viagens/telas/carteira.dart';
import 'package:horizonte_viagens/telas/perfil.dart';
import 'package:horizonte_viagens/telas/reserva.dart';

Map<String, dynamic> reservaJson() => {
  'id': 12,
  'nomePacote': 'Viagem de teste',
  'viajantes': 3,
  'quartos': 2,
  'status': 'Pendente',
  'valor': 7500,
  'pagamentoUrl': null,
};

void main() {
  test('Reserva preserva os campos ao receber cancelamento', () {
    final original = Reserva.fromJson(reservaJson());
    final cancelada = original.comStatus('Cancelada');
    expect(original.status, 'Pendente');
    expect(cancelada.status, 'Cancelada');
    expect(cancelada.id, 12);
    expect(cancelada.valor, 7500);
    expect(cancelada.quartos, 2);
  });

  testWidgets('Reserva consulta o mesmo endpoint e mostra o novo status', (
    tester,
  ) async {
    final chamadas = <String>[];
    final api = HorizonteApi(
      client: MockClient((request) async {
        chamadas.add('${request.method} ${request.url.path}');
        return http.Response(
          jsonEncode({...reservaJson(), 'status': 'Confirmada'}),
          200,
        );
      }),
    );
    addTearDown(api.dispose);
    await tester.pumpWidget(
      MaterialApp(
        home: ReservaPage(api: api, reserva: Reserva.fromJson(reservaJson())),
      ),
    );
    await tester.tap(find.text('Atualizar status'));
    await tester.pumpAndSettle();
    expect(chamadas, ['POST /api/portal/reservas/12/verificar']);
    expect(find.text('Confirmada'), findsOneWidget);
    expect(find.text('Abrir pagamento de teste'), findsNothing);
  });

  testWidgets('Carteira exibe o documento recebido da API', (tester) async {
    final documento = Documento.fromJson({
      'tipo': 'Voucher',
      'titulo': 'Hospedagem',
      'codigo': 'HV-ABC',
      'url': 'https://example.com/voucher',
    });
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: CarteiraTela(docs: [documento])),
      ),
    );
    expect(find.text('Hospedagem'), findsOneWidget);
    expect(find.text('Localizador: HV-ABC'), findsOneWidget);
  });

  testWidgets('Perfil abre edição com os dados existentes', (tester) async {
    final api = HorizonteApi(
      client: MockClient((_) async => http.Response('{}', 200)),
    );
    addTearDown(api.dispose);
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PerfilTela(
            api: api,
            perfil: {
              'nome': 'Ana',
              'email': 'ana@example.com',
              'celular': '11999999999',
            },
            atualizar: () {},
          ),
        ),
      ),
    );
    await tester.tap(find.text('Editar dados'));
    await tester.pumpAndSettle();
    expect(find.text('Editar perfil'), findsOneWidget);
    expect(find.widgetWithText(TextField, 'Ana'), findsOneWidget);
    expect(find.widgetWithText(TextField, 'Senha'), findsNothing);
  });
}
