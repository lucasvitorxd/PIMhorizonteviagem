// Ponto de entrada: inicializa o Flutter e a sessão da API.
import 'package:flutter/material.dart';

import 'app.dart';
import 'servicos/horizonte_api.dart';

export 'app.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(HorizonteApp(api: HorizonteApi()));
}
