// Configura o tema e abre a navegação principal.
import 'package:flutter/material.dart';

import 'servicos/horizonte_api.dart';
import 'telas/inicio.dart';
import 'tema.dart';

class HorizonteApp extends StatelessWidget {
  final HorizonteApi api;
  const HorizonteApp({super.key, required this.api});
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'Horizonte Viagens',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: navy,
        primary: navy,
        secondary: gold,
      ),
      scaffoldBackgroundColor: const Color(0xfff5f7f8),
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
        filled: true,
        fillColor: Colors.white,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          padding: const EdgeInsets.all(18),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
      ),
    ),
    home: Inicio(api: api),
  );
}
