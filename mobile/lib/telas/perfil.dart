// Exibe os dados da conta e abre o formulário existente para editá-los.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import 'cadastro.dart';

// Aba Perfil: mostra os dados da conta e abre o formulário para editar.
class PerfilTela extends StatelessWidget {
  final HorizonteApi api;
  final Map<String, dynamic> perfil;
  final VoidCallback atualizar;
  const PerfilTela({
    super.key,
    required this.api,
    required this.perfil,
    required this.atualizar,
  });
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(24),
    children: [
      const Text(
        'Meu perfil',
        style: TextStyle(fontSize: 30, fontWeight: FontWeight.bold),
      ),
      Text(perfil['nome']),
      Text(perfil['email']),
      Text(perfil['celular']),
      const SizedBox(height: 24),
      FilledButton(
        onPressed: () async {
          await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => CadastroPage(api: api, perfil: perfil),
            ),
          );
          atualizar();
        },
        child: const Text('Editar dados'),
      ),
      TextButton(
        onPressed: () => api.sair(),
        child: const Text('Sair da conta'),
      ),
    ],
  );
}
