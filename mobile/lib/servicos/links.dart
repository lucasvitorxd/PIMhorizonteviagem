// Abre documentos e checkout HTTPS no aplicativo externo.
import 'package:url_launcher/url_launcher.dart';

import 'horizonte_api.dart';

Future<void> abrirUrl(String url) async {
  final u = Uri.tryParse(url);
  if (u == null || u.scheme != 'https') throw ApiError('Endereço inválido.');
  if (!await launchUrl(u, mode: LaunchMode.externalApplication)) {
    throw ApiError('Não foi possível abrir o endereço.');
  }
}
