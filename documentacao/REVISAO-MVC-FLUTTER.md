# Revisão do MVC e organização do Flutter

O frontend web oficial desta entrega é o ASP.NET Core MVC. Esta revisão trata das pendências solicitadas para o frontend e da organização do app Flutter existente. O manual do PIM foi usado como referência para a arquitetura, sem ampliar o trabalho para as demais etapas acadêmicas.

## O que já estava corrigido no ZIP recebido

A versão recebida já continha várias correções descritas como pendentes. Elas foram conferidas e preservadas:

- Precificacao.Calcular centraliza o orçamento do MVC. A partial _Card exibe o total recebido, e horizonte.js reenvia o formulário GET quando muda a quantidade de viajantes. O servidor renderiza o novo preço; não existe fórmula de preço no JavaScript do MVC.
- As views novas já estavam formatadas e comentadas, incluindo _Layout.
- ComboCardViewModel, ComboDetalheViewModel, VitrineViewModel, ReservaViewModel e DocumentoViewModel já estavam em uso. As telas de reservas não recebem mais Compra diretamente. Os formulários administrativos existentes ainda usam seus DTOs de entrada, preservando as validações.
- Os filtros e a ordenação da vitrine já estavam no PortalNegocio.
- Criar e Editar combo já carregavam _ValidationScriptsPartial.
- O menu móvel já tinha botão, aria-controls, aria-expanded e CSS para telas até 900 px.
- Clientes/Index e Combos/Gerenciar já usavam table-responsive.

## Ajustes realizados nesta revisão

### MVC e Negocio

- O preço por pessoa ainda era dividido no ViewModel. Agora ItemVitrine, no Negocio, fornece PorPessoa pronto.
- A normalização da quantidade de viajantes é compartilhada pelo Negocio e pela montagem do ViewModel.
- A condição usada para apresentar o cancelamento da reserva foi movida para PortalNegocio.PodeCancelar.
- A exclusão de combo trata concorrência e vínculo criado entre a consulta inicial e a exclusão. O ComboRepositorio traduz os erros do banco em ResultadoExclusao, e o ComboNegocio só converte essa resposta em mensagem, sem depender do Entity Framework. Uma falha de banco sem vínculo conhecido não é disfarçada como regra de exclusão.
- O controller trata ausência do combo ao voltar de uma exclusão recusada, sem desreferenciar um valor nulo.
- Foram acrescentados testes de preços, renderização Razor para 1/3/6 viajantes e exclusão concorrente.

### Frontend oficial

O ZIP final não inclui o site estático da raiz, seus arquivos de apoio, o workflow de GitHub Pages nem a pasta PIM - Horizonte (Web). As imagens usadas pelo MVC e pelo Flutter continuam nas respectivas pastas de assets. SQL, API, migrations e os demais projetos foram preservados.

README e guias de execução agora apontam para o MVC. Os guias específicos do frontend estático não fazem parte deste pacote. O ZIP original em Downloads não foi alterado.

### Flutter

- main.dart ficou restrito à inicialização; MaterialApp e tema estão separados.
- Telas foram extraídas para telas/: início, catálogo, login, cadastro, perfil, detalhes, reservas e carteira.
- HTTP/autenticação, links e identificador de requisição foram separados em servicos/.
- Combo foi movido para modelos/. Reserva e Documento representam os campos usados pelas telas.
- O estado dos filtros permanece na navegação principal. As telas extraídas recebem dados e callbacks, preservando o comportamento.
- Comentários explicam responsabilidades, ciclo de vida e comunicação com a API, sem travessões nos comentários adicionados.
- api.dart mantém exportações para os imports antigos, e main.dart exporta HorizonteApp para compatibilidade com os testes existentes.
- Não foram alterados endpoints, payloads, desenho das telas ou configuração de pagamento. O lockfile e as dependências declaradas foram preservados.

A estimativa local do card Flutter em Combo.total foi mantida nesta reorganização. O detalhe continua pedindo o orçamento ao servidor. A centralização completa do cálculo solicitada nesta revisão se aplica ao MVC; o Flutter foi reorganizado sem mudar seu comportamento.

## Verificações executadas

| Verificação | Resultado |
|---|---|
| Compilação dos projetos usados pelos testes .NET, incluindo Web e Api | Aprovada |
| Testes .NET | 47 aprovados, nenhum ignorado ou com falha |
| Formatação Dart | Concluída |
| flutter analyze | Nenhum problema encontrado |
| flutter test | 7 aprovados |

Os testes Flutter incluem os três testes existentes e quatro adicionais: preservação do modelo de reserva, atualização de status pelo endpoint original, exibição da carteira e abertura da edição do perfil. Os testes .NET usam banco SQLite em memória e renderizam Razor em servidor de teste.

Nesta revisão não foram executados SQL Server real, procedures/trigger T-SQL, checkout externo, emulador Android ou aparelho físico. Também não houve nova inspeção visual em navegador de todas as resoluções; menu e tabelas responsivas foram conferidos no código recebido. Use o roteiro da equipe para a validação integrada final.

## Executar na máquina da equipe

Na raiz do projeto:

```powershell
dotnet test backend/HorizonteViagens.Testes/HorizonteViagens.Testes.csproj
```

Na pasta mobile, com a API iniciada:

```powershell
flutter pub get
flutter analyze
flutter test
flutter run -d chrome --web-port 5300 --dart-define=API_URL=http://localhost:5218
```

Para emulador Android com API no mesmo PC, use API_URL=http://10.0.2.2:5218. Para celular físico, use o IP do PC da API na rede local. Veja [COMO-EXECUTAR.md](COMO-EXECUTAR.md), [ROTEIRO-DE-TESTES.md](ROTEIRO-DE-TESTES.md) e [a organização do app](../mobile/ORGANIZACAO.md).

## Ajustes de 22/11

- A exclusão de combo ficou no padrão das camadas: o `ComboRepositorio` trata os erros do Entity Framework e devolve um `ResultadoExclusao` (Excluido, EmUso ou AlteradoPorOutro). O `ComboNegocio` só transforma essa resposta em mensagem e não referencia mais o Entity Framework. Os três testes da exclusão foram adaptados; o total continua em 47.
- O app Flutter recebeu comentários explicando cada tela, o serviço da Api e os modelos. Nenhuma linha de código foi alterada, só comentários.
- A estimativa do card no Flutter (`Combo.total`) foi mantida e agora está comentada: ela repete a conta da `Precificacao` só para exibição, e o valor que vale para a reserva continua sendo o orçamento do servidor.
- Removidos do pacote: `backend/PreviewApi`, `backend/PreviewLocal`, os documentos antigos `RELATORIO_CORRECOES_ETAPA4.docx` e `STATUS_PENDENCIAS_ETAPA4.pdf`, o inventário `ARQUIVOS-ALTERADOS.csv`, o `ESCOPO-DOS-ARQUIVOS.txt` e a pasta `sql/`. O script oficial do banco é o `backend/banco-completo.sql`, gerado pelas migrations.
