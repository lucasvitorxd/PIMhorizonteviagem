// Documento emitido pela agência; o QR usa a URL recebida, sem gerar assinatura local.
class Documento {
  final String tipo, titulo, codigo, url;
  Documento.fromJson(Map<String, dynamic> dados)
    : tipo = dados['tipo'],
      titulo = dados['titulo'],
      codigo = dados['codigo'],
      url = dados['url'];
}
