// Mostra documentos emitidos pela agência e o QR para abrir cada documento.
import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../tema.dart';
import '../modelos/documento.dart';
import '../servicos/links.dart';

// Aba Carteira: documentos emitidos pela agência para reservas confirmadas.
// Cada documento ganha um QR Code com o link, para mostrar no embarque.
class CarteiraTela extends StatelessWidget {
  final List<Documento> docs;
  const CarteiraTela({super.key, required this.docs});
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      const Text(
        'Minha carteira',
        style: TextStyle(fontSize: 30, fontWeight: FontWeight.bold),
      ),
      const Text('Documentos emitidos para suas reservas confirmadas.'),
      if (docs.isEmpty)
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 60),
          child: Text(
            'Seus documentos aparecerão aqui após a emissão pela agência.',
          ),
        ),
      ...docs.map(
        (d) => Card(
          color: navy,
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(d.tipo, style: const TextStyle(color: gold)),
                Text(
                  d.titulo,
                  style: const TextStyle(fontSize: 24, color: Colors.white),
                ),
                Text(
                  'Localizador: ${d.codigo}',
                  style: const TextStyle(color: Colors.white),
                ),
                const SizedBox(height: 20),
                Container(
                  color: Colors.white,
                  padding: const EdgeInsets.all(10),
                  // O QR é gerado no próprio celular a partir do link recebido da Api.
                  child: QrImageView(data: d.url, size: 150),
                ),
                const Text(
                  'QR para abrir o documento do fornecedor.',
                  style: TextStyle(color: Colors.white, fontSize: 12),
                ),
                TextButton(
                  onPressed: () => abrirUrl(d.url),
                  child: const Text(
                    'Abrir documento',
                    style: TextStyle(color: gold),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    ],
  );
}
