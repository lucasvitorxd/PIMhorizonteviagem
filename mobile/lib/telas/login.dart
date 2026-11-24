// Entrada na conta. A API valida a senha e mantém a sessão.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import 'cadastro.dart';

// Tela de login. Quem confere a senha é a Api; o app só guarda o token recebido.
class LoginPage extends StatefulWidget {
  final HorizonteApi api;
  const LoginPage({super.key, required this.api});
  @override
  State<LoginPage> createState() => _LoginState();
}

class _LoginState extends State<LoginPage> {
  final email = TextEditingController(), senha = TextEditingController();
  String? erro;
  bool busy = false;
  @override
  // Libera controladores e ouvintes quando a tela sai da navegação.
  void dispose() {
    email.dispose();
    senha.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Minha conta')),
    body: ListView(
      padding: const EdgeInsets.all(26),
      children: [
        const SizedBox(height: 30),
        const Text(
          'Seu horizonte espera.',
          style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 25),
        TextField(
          controller: email,
          keyboardType: TextInputType.emailAddress,
          autofillHints: const [AutofillHints.username],
          decoration: const InputDecoration(labelText: 'E-mail'),
        ),
        const SizedBox(height: 16),
        TextField(
          controller: senha,
          obscureText: true,
          autofillHints: const [AutofillHints.password],
          decoration: const InputDecoration(labelText: 'Senha'),
        ),
        if (erro != null)
          Text(erro!, style: const TextStyle(color: Colors.red)),
        const SizedBox(height: 24),
        FilledButton(
          onPressed: busy
              ? null
              : () async {
                  setState(() => busy = true);
                  try {
                    // Em caso de sucesso fecha a tela; em caso de erro mostra a mensagem da Api.
                    await widget.api.entrar(email.text, senha.text);
                    if (context.mounted) Navigator.pop(context);
                  } catch (e) {
                    if (mounted) setState(() => erro = e.toString());
                  } finally {
                    if (mounted) setState(() => busy = false);
                  }
                },
          child: Text(busy ? 'Entrando…' : 'Entrar'),
        ),
        TextButton(
          onPressed: () => Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => CadastroPage(api: widget.api)),
          ),
          child: const Text('Ainda não tem conta? Cadastre-se'),
        ),
      ],
    ),
  );
}
