
// Coordena as abas, a sessão e a atualização dos dados sem alterar a navegação.
import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import '../tema.dart';
import '../modelos/reserva.dart';
import '../modelos/documento.dart';
import 'login.dart';
import 'catalogo.dart';
import 'reservas.dart';
import 'carteira.dart';
import 'perfil.dart';

// Tela principal com as quatro abas (Explorar, Viagens, Carteira, Perfil).
// Ela guarda os filtros da busca, então trocar de aba não perde o que foi digitado.
class Inicio extends StatefulWidget {
  final HorizonteApi api;

  const Inicio({super.key, required this.api});

  @override
  State<Inicio> createState() => _InicioState();
}

class _InicioState extends State<Inicio> {
  int tab = 0, adultos = 2;
  String busca = '', categoria = 'Todas';
  bool favoritos = false;
  late Future<dynamic> dados;

  @override
  void initState() {
    super.initState();
    widget.api.addListener(sessao);
    dados = carregar();
    widget.api.restaurar();
  }

  @override
  void dispose() {
    widget.api.removeListener(sessao);
    super.dispose();
  }

  // Atualiza os dados quando a sessão muda.
  void sessao() {
    if (!mounted) return;

    setState(() {
      dados = carregar();
    });
  }

  // Carrega os dados da aba selecionada.
  Future<dynamic> carregar() async {
    if (tab == 0) {
      final lista = await widget.api.request(
        'GET',
        '/api/portal/catalogo',
      );

      final ids = widget.api.autenticado
          ? await widget.api.request('GET', '/api/portal/favoritos')
          : [];

      return {
        'lista': lista,
        'favoritos': ids,
      };
    }

    if (!widget.api.autenticado) return null;

    if (tab == 3) {
      return widget.api.request('GET', '/api/portal/perfil');
    }

    final reservas = await widget.api.request(
      'GET',
      '/api/portal/reservas',
    );

    if (tab == 1) return reservas;

    final docs = <dynamic>[];

    for (final r in reservas) {
      if (r['status'] == 'Confirmada') {
        docs.addAll(
          await widget.api.request(
            'GET',
            '/api/portal/reservas/${r['id']}/documentos',
          ),
        );
      }
    }

    return docs;
  }

  // Atualiza os dados sem retornar um Future dentro do setState.
  void atualizar() {
    if (!mounted) return;

    setState(() {
      dados = carregar();
    });
  }

  // Abre o login e atualiza a tela ao retornar.
  Future<void> login() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => LoginPage(api: widget.api),
      ),
    );

    if (mounted) atualizar();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text(
        '☀ horizonte',
        style: TextStyle(
          fontWeight: FontWeight.w800,
          letterSpacing: -1,
        ),
      ),
      actions: [
        if (!widget.api.autenticado)
          TextButton(
            onPressed: login,
            child: const Text('Entrar'),
          )
        else
          IconButton(
            onPressed: () => widget.api.sair(),
            icon: const Icon(Icons.logout),
            tooltip: 'Sair',
          ),
      ],
    ),

    body: RefreshIndicator(
      onRefresh: () async {
        atualizar();
        await dados;
      },
      child: FutureBuilder<dynamic>(
        future: dados,
        builder: (context, s) {
          if (s.connectionState != ConnectionState.done) {
            return const Center(
              child: CircularProgressIndicator(),
            );
          }

          if (s.hasError) {
            return estado(
              'Não foi possível carregar',
              s.error.toString(),
              atualizar,
            );
          }

          if (tab > 0 && !widget.api.autenticado) {
            return estado(
              'Seu próximo horizonte',
              'Entre para acessar suas viagens e documentos.',
              login,
            );
          }

          if (tab == 0) {
            return CatalogoTela(
              api: widget.api,
              data: s.data,
              adultos: adultos,
              busca: busca,
              categoria: categoria,
              favoritos: favoritos,
              onBusca: (v) {
                setState(() {
                  busca = v;
                });
              },
              onAdultos: (v) {
                setState(() {
                  adultos = v;
                });
              },
              onCategoria: (v) {
                setState(() {
                  categoria = v;
                });
              },
              onFavoritos: (v) {
                setState(() {
                  favoritos = v;
                });
              },
              login: login,
              atualizar: atualizar,
              mensagem: mensagem,
            );
          }

          if (tab == 1) {
            return ReservasTela(
              api: widget.api,
              lista: (s.data as List)
                  .map(
                    (j) => Reserva.fromJson(
                      Map<String, dynamic>.from(j),
                    ),
                  )
                  .toList(),
              atualizar: atualizar,
            );
          }

          if (tab == 2) {
            return CarteiraTela(
              docs: (s.data as List)
                  .map(
                    (j) => Documento.fromJson(
                      Map<String, dynamic>.from(j),
                    ),
                  )
                  .toList(),
            );
          }

          return PerfilTela(
            api: widget.api,
            perfil: Map<String, dynamic>.from(s.data),
            atualizar: atualizar,
          );
        },
      ),
    ),

    // Barra inferior de navegação.
    bottomNavigationBar: NavigationBar(
      selectedIndex: tab,
      onDestinationSelected: (v) {
        setState(() {
          tab = v;
          dados = carregar();
        });
      },
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.travel_explore),
          label: 'Explorar',
        ),
        NavigationDestination(
          icon: Icon(Icons.luggage_outlined),
          label: 'Viagens',
        ),
        NavigationDestination(
          icon: Icon(Icons.wallet_outlined),
          label: 'Carteira',
        ),
        NavigationDestination(
          icon: Icon(Icons.person_outline),
          label: 'Perfil',
        ),
      ],
    ),
  );

  // Tela de aviso para erros e autenticação.
  Widget estado(
    String titulo,
    String texto,
    VoidCallback acao,
  ) => ListView(
    padding: const EdgeInsets.all(28),
    children: [
      const SizedBox(height: 90),
      const Icon(
        Icons.explore_outlined,
        size: 70,
        color: navy,
      ),
      const SizedBox(height: 24),
      Text(
        titulo,
        style: Theme.of(context).textTheme.headlineSmall,
        textAlign: TextAlign.center,
      ),
      const SizedBox(height: 12),
      Text(
        texto,
        textAlign: TextAlign.center,
      ),
      const SizedBox(height: 25),
      FilledButton(
        onPressed: acao,
        child: Text(
          widget.api.autenticado || tab == 0
              ? 'Tentar novamente'
              : 'Entrar',
        ),
      ),
    ],
  );

  // Exibe mensagens na parte inferior.
  void mensagem(String s) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(s)),
      );
    }
  }
}
