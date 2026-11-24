// Comunicação do app com a Api do Horizonte Viagens (HTTP + login com token JWT).
import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

// Erro com a mensagem que a Api devolveu, pronta para mostrar na tela.
class ApiError implements Exception {
  final String message;
  final int status;
  ApiError(this.message, [this.status = 0]);
  @override
  String toString() => message;
}

// Única porta do app para a Api. Monta cada chamada HTTP, coloca o token
// de login e avisa as telas (notifyListeners) quando a sessão muda.
class HorizonteApi extends ChangeNotifier {
  final http.Client client;
  final String baseUrl;
  String? _token;
  String? nome;
  final _storage = const FlutterSecureStorage();
  HorizonteApi({http.Client? client, String? baseUrl})
    : client = client ?? http.Client(),
      baseUrl =
          baseUrl ??
          const String.fromEnvironment(
            'API_URL',
            defaultValue: 'http://localhost:5218',
          );
  bool get autenticado => _token != null;
  // Em navegador a sessão fica apenas em memória; Android/iOS usam cofre do sistema.
  Future<void> restaurar() async {
    if (!kIsWeb) {
      _token = await _storage.read(key: 'horizonte_token');
    }
    if (_token != null) {
      try {
        final p = await request('GET', '/api/portal/perfil');
        nome = p['nome'];
      } catch (_) {
        await sair();
      }
    }
    notifyListeners();
  }

  // Faz uma chamada à Api e devolve o JSON da resposta. Erros de conexão,
  // 401 (sessão expirada) e 400 (regra recusada) viram ApiError com texto amigável.
  Future<dynamic> request(
    String method,
    String path, [
    Map<String, dynamic>? data,
  ]) async {
    final req = http.Request(method, Uri.parse('$baseUrl$path'));
    req.headers['Accept'] = 'application/json';
    if (_token != null) req.headers['Authorization'] = 'Bearer $_token';
    if (data != null) {
      req.headers['Content-Type'] = 'application/json';
      req.body = jsonEncode(data);
    }
    http.Response res;
    try {
      res = await http.Response.fromStream(
        await client.send(req).timeout(const Duration(seconds: 20)),
      ).timeout(const Duration(seconds: 20));
    } on TimeoutException {
      throw ApiError('A conexão demorou. Tente novamente.');
    } on http.ClientException {
      throw ApiError('Não foi possível conectar à API. Confira sua conexão.');
    }
    dynamic body;
    try {
      body = res.body.isEmpty ? null : jsonDecode(res.body);
    } catch (_) {
      throw ApiError(
        'O servidor retornou uma resposta inválida.',
        res.statusCode,
      );
    }
    if (res.statusCode == 401) {
      await sair();
      throw ApiError('Entre novamente para continuar.', 401);
    }
    if (res.statusCode >= 400) {
      var message = body is Map ? body['erro']?.toString() : null;
      if (message == null && body is Map && body['errors'] is Map) {
        message = (body['errors'] as Map).values
            .expand((v) => v as List)
            .join('\n');
      }
      throw ApiError(
        message ?? 'Não foi possível concluir a solicitação.',
        res.statusCode,
      );
    }
    return body;
  }

  // Login do cliente: guarda o token JWT que a Api devolve.
  Future<void> entrar(String email, String senha) async {
    final data = await request('POST', '/api/autenticacao/login-cliente', {
      'email': email.trim(),
      'senha': senha,
    });
    _token = data['token'];
    nome = data['nome'];
    if (!kIsWeb) await _storage.write(key: 'horizonte_token', value: _token);
    notifyListeners();
  }

  // Esquece o token e avisa as telas para voltarem ao estado de visitante.
  Future<void> sair() async {
    _token = null;
    nome = null;
    if (!kIsWeb) await _storage.delete(key: 'horizonte_token');
    notifyListeners();
  }

  @override
  void dispose() {
    client.close();
    super.dispose();
  }
}
