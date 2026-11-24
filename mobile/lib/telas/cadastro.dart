// Formulário compartilhado entre cadastro e edição do perfil.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';

// Formulário usado em dois casos: criar conta (perfil vazio) e editar perfil.
class CadastroPage extends StatefulWidget {
  final HorizonteApi api;
  final Map<String, dynamic>? perfil;
  const CadastroPage({super.key, required this.api, this.perfil});
  @override
  State<CadastroPage> createState() => _CadastroState();
}

class _CadastroState extends State<CadastroPage> {
  final campos = <String, TextEditingController>{};
  String? erro;
  bool busy = false;
  // Nome de cada campo no JSON da Api e o rótulo mostrado na tela.
  final nomes = {
    'nome': 'Nome completo',
    'email': 'E-mail',
    'cpf': 'CPF',
    'celular': 'Celular com DDD',
    'cep': 'CEP',
    'rua': 'Rua',
    'bairro': 'Bairro',
    'cidade': 'Cidade',
    'senha': 'Senha',
  };
  @override
  // Prepara o estado uma vez, quando esta tela é criada.
  void initState() {
    super.initState();
    for (final key in nomes.keys) {
      campos[key] = TextEditingController(
        text: widget.perfil?[key]?.toString() ?? '',
      );
    }
  }

  @override
  // Libera controladores e ouvintes quando a tela sai da navegação.
  void dispose() {
    for (final c in campos.values) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(widget.perfil == null ? 'Criar conta' : 'Editar perfil'),
    ),
    body: ListView(
      padding: const EdgeInsets.all(24),
      children: [
        ...nomes.entries
            .where((e) => widget.perfil == null || e.key != 'senha')
            .map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: TextField(
                  controller: campos[e.key],
                  obscureText: e.key == 'senha',
                  decoration: InputDecoration(labelText: e.value),
                ),
              ),
            ),
        if (widget.perfil == null)
          const Text(
            'Senha com 8 ou mais caracteres, maiúscula, minúscula, número e símbolo.',
          ),
        if (erro != null)
          Text(erro!, style: const TextStyle(color: Colors.red)),
        FilledButton(
          onPressed: busy
              ? null
              : () async {
                  setState(() => busy = true);
                  try {
                    // Cadastro novo: POST /api/clientes. Edição: PUT /api/portal/perfil, sem senha.
                    // As validações (CPF, senha forte, e-mail repetido) são feitas pela Api.
                    await widget.api.request(
                      widget.perfil == null ? 'POST' : 'PUT',
                      widget.perfil == null
                          ? '/api/clientes'
                          : '/api/portal/perfil',
                      {
                        for (final e in campos.entries)
                          if (e.key != 'senha' || widget.perfil == null)
                            e.key: e.value.text,
                      },
                    );
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Dados salvos.')),
                      );
                      Navigator.pop(context);
                    }
                  } catch (e) {
                    if (mounted) setState(() => erro = e.toString());
                  } finally {
                    if (mounted) setState(() => busy = false);
                  }
                },
          child: Text(busy ? 'Salvando…' : 'Salvar'),
        ),
      ],
    ),
  );
}
