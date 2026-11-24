# Organização do aplicativo Flutter

Esta revisão separa o aplicativo existente por responsabilidade, preservando telas, textos, navegação, endpoints e payloads. O código anterior não foi substituído por outro aplicativo.

| Arquivo ou pasta em lib | Conteúdo |
|---|---|
| main.dart | Inicialização do Flutter e da API |
| app.dart e tema.dart | MaterialApp, cores e formatação de moeda |
| telas/inicio.dart | Abas, sessão, estado dos filtros e carregamento |
| telas/catalogo.dart | Busca, filtros, favoritos e cards |
| telas/login.dart | Login |
| telas/cadastro.dart e perfil.dart | Cadastro e perfil |
| telas/detalhe_combo.dart | Orçamento e criação de reserva |
| telas/reservas.dart e reserva.dart | Reservas e pagamento de teste |
| telas/carteira.dart | Documentos e QR |
| servicos/horizonte_api.dart | HTTP, autenticação, sessão e erros |
| servicos/links.dart | Abertura de links HTTPS |
| servicos/requisicao.dart | Identificador contra reserva duplicada |
| modelos | Combo, Reserva e Documento |
| api.dart | Exportações de compatibilidade com imports anteriores |

O estado dos filtros permanece na tela de início. Mudar de aba continua preservando a busca. Os componentes recebem dados e callbacks para atualizar esse estado. Reserva e Documento dão nomes aos campos JSON, sem mudar o contrato.

A estimativa dos cards do Flutter mantém o cálculo original de Combo.total. O detalhe consulta o orçamento no servidor e a reserva envia o total consultado. Essa fórmula local foi preservada porque o escopo do Flutter é reorganização sem alteração de comportamento. A centralização completa de preços solicitada para o MVC está no backend.

## Conferir na máquina da equipe

Com a API iniciada, execute na pasta mobile:

```powershell
flutter pub get
flutter analyze
flutter test
flutter run -d chrome --web-port 5300 --dart-define=API_URL=http://localhost:5218
```

Para Android emulado com API no mesmo computador:

```powershell
flutter devices
flutter run -d ID-DO-EMULADOR --dart-define=API_URL=http://10.0.2.2:5218
```

Confira catálogo, viajantes, busca, cadastro, login, favoritos, perfil, reserva, pagamento de teste, atualização de status, carteira, erros e catálogo vazio. Documentos aparecem após emissão pela agência para reserva confirmada.
