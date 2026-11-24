import 'package:flutter/material.dart';

import '../servicos/horizonte_api.dart';
import '../tema.dart';
import '../modelos/combo.dart';
import 'detalhe_combo.dart';

class CatalogoTela extends StatelessWidget {
  final HorizonteApi api;
  final dynamic data;
  final int adultos;
  final String busca, categoria;
  final bool favoritos;
  final ValueChanged<String> onBusca, onCategoria, mensagem;
  final ValueChanged<int> onAdultos;
  final ValueChanged<bool> onFavoritos;
  final Future<void> Function() login;
  final VoidCallback atualizar;

  const CatalogoTela({super.key, required this.api, required this.data,
    required this.adultos, required this.busca, required this.categoria,
    required this.favoritos, required this.onBusca, required this.onCategoria,
    required this.onAdultos, required this.onFavoritos, required this.login,
    required this.atualizar, required this.mensagem});

  static const _ink = Color(0xFF152B35);
  static const _muted = Color(0xFF73828A);
  static const _line = Color(0xFFE7EBEC);

  @override
  Widget build(BuildContext context) {
    final favoritosIds = data['favoritos'] as List;
    final items = (data['lista'] as List)
        .map((j) => Combo.fromJson(Map<String, dynamic>.from(j)))
        .where((c) => ('${c.destino} ${c.evento}').toLowerCase().contains(busca.toLowerCase()) &&
            (categoria == 'Todas' || categoria == c.categoria) &&
            (!favoritos || favoritosIds.contains(c.id)))
        .toList();
    final wide = MediaQuery.sizeOf(context).width >= 800;
    final cols = MediaQuery.sizeOf(context).width >= 1100 ? 3 : wide ? 2 : 1;

    return ListView(children: [
      Center(child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 1180),
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: wide ? 28 : 18, vertical: 20),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            _hero(context, wide),
            const SizedBox(height: 28),
            Text('Encontre sua próxima experiência',
                style: TextStyle(fontSize: wide ? 29 : 23, fontWeight: FontWeight.w800,
                    letterSpacing: -0.8, color: _ink)),
            const SizedBox(height: 7),
            const Text('Destinos selecionados para transformar viagens em memórias.',
                style: TextStyle(color: _muted, fontSize: 14)),
            const SizedBox(height: 20),
            LayoutBuilder(builder: (context, box) {
              final search = TextField(
                decoration: const InputDecoration(
                  hintText: 'Para onde vamos?', prefixIcon: Icon(Icons.search_rounded),
                  contentPadding: EdgeInsets.symmetric(horizontal: 18, vertical: 19)),
                onChanged: onBusca,
              );
              final travelers = DropdownButtonFormField<int>(
                value: adultos,
                decoration: const InputDecoration(labelText: 'Viajantes',
                    prefixIcon: Icon(Icons.people_outline_rounded)),
                items: List.generate(6, (i) => DropdownMenuItem(
                    value: i + 1, child: Text('${i + 1} adulto(s)'))),
                onChanged: (v) { if (v != null) onAdultos(v); },
              );
              if (box.maxWidth < 650) return Column(children: [search,
                const SizedBox(height: 12), travelers]);
              return Row(children: [Expanded(flex: 3, child: search),
                const SizedBox(width: 12), Expanded(flex: 2, child: travelers)]);
            }),
            const SizedBox(height: 18),
            Wrap(spacing: 9, runSpacing: 9, crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                for (final c in ['Todas', 'Luxo', 'Econômico'])
                  ChoiceChip(label: Text(c), selected: categoria == c,
                      onSelected: (_) => onCategoria(c)),
                FilterChip(label: const Text('Meus favoritos'),
                  avatar: Icon(favoritos ? Icons.favorite : Icons.favorite_border,
                      size: 17, color: favoritos ? const Color(0xFFB34E5A) : _muted),
                  selected: favoritos,
                  onSelected: (v) async {
                    if (!api.autenticado) { await login(); return; }
                    onFavoritos(v);
                  }),
              ]),
            const SizedBox(height: 32),
            Row(children: [
              const Expanded(child: Text('Destinos em destaque',
                  style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800,
                      letterSpacing: -0.5, color: _ink))),
              Text('${items.length} opções', style: const TextStyle(color: _muted)),
            ]),
            const SizedBox(height: 17),
            if (items.isEmpty)
              Container(width: double.infinity, padding: const EdgeInsets.all(35),
                decoration: BoxDecoration(color: Colors.white,
                    borderRadius: BorderRadius.circular(20)),
                child: const Column(children: [Icon(Icons.travel_explore_rounded, size: 44,
                    color: _muted), SizedBox(height: 12),
                  Text('Nenhum pacote encontrado. Experimente outros filtros.',
                      textAlign: TextAlign.center)]))
            else
              GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: items.length,
                gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: cols, crossAxisSpacing: 18, mainAxisSpacing: 18,
                  mainAxisExtent: wide ? 425 : 405),
                itemBuilder: (context, index) => _packageCard(
                    context, items[index], favoritosIds),
              ),
            const SizedBox(height: 30),
          ]),
        ),
      )),
    ]);
  }

  Widget _hero(BuildContext context, bool wide) => Container(
    height: wide ? 340 : 300,
    clipBehavior: Clip.antiAlias,
    decoration: BoxDecoration(borderRadius: BorderRadius.circular(24)),
    child: Stack(fit: StackFit.expand, children: [
      Image.asset('assets/hero.jpg', fit: BoxFit.cover,
        errorBuilder: (_, __, ___) => const ColoredBox(color: _ink)),
      const DecoratedBox(decoration: BoxDecoration(gradient: LinearGradient(
        begin: Alignment.centerLeft, end: Alignment.centerRight,
        colors: [Color(0xE6142C36), Color(0xA0142C36), Color(0x15142C36)]))),
      Padding(padding: EdgeInsets.all(wide ? 42 : 25),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.center, children: [
          const Text('HORIZONTE VIAGENS  /  EXPLORE MAIS',
            style: TextStyle(color: Color(0xFFF0CF8A), fontSize: 10,
                letterSpacing: 2.0, fontWeight: FontWeight.w700)),
          const SizedBox(height: 18),
          Text('O mundo espera\npor você.',
            style: TextStyle(fontSize: wide ? 46 : 36, height: 1.1,
              fontWeight: FontWeight.w800, letterSpacing: -1.4,
              color: Colors.white)),
          const SizedBox(height: 15),
          const Text('Descubra destinos extraordinários e viva cada momento.',
            style: TextStyle(color: Color(0xFFE5EBED), fontSize: 14)),
        ])),
    ]),
  );

  Widget _packageCard(BuildContext context, Combo c, List favoritosIds) {
    final selected = favoritosIds.contains(c.id);
    return Material(color: Colors.white, borderRadius: BorderRadius.circular(20),
      clipBehavior: Clip.antiAlias,
      child: InkWell(onTap: () => _abrir(context, c),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Stack(children: [
            SizedBox(height: 178, width: double.infinity,
              child: Image.asset('assets/${c.imagem}.jpg', fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => const ColoredBox(
                  color: Color(0xFFE6EFF0), child: Center(
                    child: Icon(Icons.landscape_outlined, size: 45, color: _muted))))),
            Positioned(top: 12, left: 12,
              child: Container(padding: const EdgeInsets.symmetric(
                  horizontal: 11, vertical: 7),
                decoration: BoxDecoration(color: Colors.white,
                    borderRadius: BorderRadius.circular(30)),
                child: Text(c.categoria, style: const TextStyle(
                  fontSize: 11, color: _ink, fontWeight: FontWeight.w700)))),
            Positioned(top: 8, right: 8,
              child: Material(color: Colors.white, shape: const CircleBorder(),
                child: IconButton(tooltip: 'Favoritar',
                  icon: Icon(selected ? Icons.favorite : Icons.favorite_border,
                    color: selected ? const Color(0xFFB34E5A) : _ink),
                  onPressed: () => _favoritar(c, selected)))),
          ]),
          Expanded(child: Padding(padding: const EdgeInsets.all(17),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text('${c.noites + 1} dias  •  ${c.noites} noites',
                style: const TextStyle(color: _muted, fontSize: 12)),
              const SizedBox(height: 5),
              Text(c.destino, maxLines: 1, overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: _ink, fontWeight: FontWeight.w800,
                  fontSize: 21, letterSpacing: -0.5)),
              Text(c.evento, maxLines: 1, overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: _muted, fontSize: 12)),
              const Spacer(),
              const Divider(color: _line),
              Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
                Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(c.confirmado ? 'Total para $adultos adulto(s)' :
                      'Estimativa para $adultos adulto(s)',
                      style: const TextStyle(color: _muted, fontSize: 11)),
                    const SizedBox(height: 3),
                    Text(moeda.format(c.total(adultos)),
                      style: const TextStyle(fontWeight: FontWeight.w800,
                        color: _ink, fontSize: 20)),
                  ])),
                const Icon(Icons.arrow_outward_rounded, color: _ink),
              ]),
            ]))),
        ])));
  }

  Future<void> _abrir(BuildContext context, Combo c) async {
    await Navigator.push(context, MaterialPageRoute(
      builder: (_) => DetalheCombo(api: api, combo: c, adultos: adultos)));
    atualizar();
  }

  Future<void> _favoritar(Combo c, bool selected) async {
    if (!api.autenticado) { await login(); return; }
    try {
      await api.request(selected ? 'DELETE' : 'PUT',
          '/api/portal/favoritos/${c.id}');
      atualizar();
    } catch (e) { mensagem(e.toString()); }
  }
}
