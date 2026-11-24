# Resumo das alterações anteriores

Este documento descreve a evolução anterior do projeto. Para as alterações e verificações desta revisão, consulte [REVISAO-MVC-FLUTTER.md](REVISAO-MVC-FLUTTER.md).

## Site

O HTML foi integrado às views Razor do MVC. Mantidos azul/dourado, destinos, viagens e eventos. Acrescentados busca, categorias, detalhe do combo, composição de preço, telas de cliente e navegação administrativa. Combo ganhou Criar, Editar, Excluir e Gerenciar, com formulário validado e proteção contra sobrescrita de alterações.

## Preços

Transporte e experiências são por adulto; hospedagem é diária por quarto de até dois adultos × noites × teto(viajantes/2). No MVC, a troca de viajantes reenvia o formulário GET; o servidor calcula o orçamento e renderiza o novo total. Não há fórmula de preço no JavaScript ou na view. O Flutter aplica a mesma regra para exibição. A API recusa divergências. Valores iniciais são estimativas documentadas, não ofertas de fornecedores com disponibilidade confirmada.

## API/C#

Endpoints de catálogo, orçamento, perfil, favoritos, reservas, cancelamento, pagamento e documentos. Autenticação cliente/admin, JWT/cookies com verificação de conta ativa/expirada, limite de tentativas, CSRF nas telas e respostas sem detalhes internos. Reservas guardam composição e preço histórico; identificador de requisição impede duplicação. Controle transacional de vagas e rowversion.

## Pagamento

Mercado Pago Checkout Pro sandbox, conta vendedora de teste obrigatória, nenhum cartão/Pix sensível armazenado. Preferência usa o valor do banco. Confirmação exige valor/referência/modo de teste compatíveis; consulta autenticada ao provedor. A URL de retorno não confirma pagamento. Integração externa depende das credenciais da equipe e ainda precisa ser homologada.

## SQL

Evoluído o esquema Entity Framework do C#, incluindo favoritos, documentos, detalhes do combo, reservas e índices de unicidade. Migrations preservam compras antigas com status de revisão. Procedures/trigger existentes foram mantidos; CREATE foi encapsulado em EXEC para funcionar em scripts idempotentes. Script consolidado não apaga nem escolhe o banco automaticamente.

## Flutter

Substituída a carteira estática por catálogo, busca, detalhe, autenticação, cadastro, perfil, favoritos, reservas, pagamento e documentos via API. Retirada assinatura HMAC embutida. Android/iOS usam armazenamento seguro de token; navegador usa memória. URL da API configurada via dart-define. Internet Android e HTTP apenas debug. Identidade/idioma Web atualizados.

## Preparação e documentação

Criados scripts PowerShell para preparar banco e iniciar site/API/Flutter em computadores separados. Acessos temporários por sete dias, criados somente por opção explícita em Development. Inventário de arquivos e guias de execução/testes. Não alterados os arquivos originais em Downloads. Nada enviado ao GitHub ou ao site hospedado antigo.

## Comentários

Código novo/alterado contém comentários explicando a finalidade das mudanças. Arquivos de configuração sem suporte a comentários (JSON/lock) e arquivos gerados são identificados no inventário. O inventário compara o pacote com as fontes recebidas; diferenças de codificação/quebras de linha também podem aparecer.

## Ajustes de 22/11 (noite)

Incluída a pasta `banco-de-dados` com o script da disciplina de Banco de Dados (versão de 22/11). Corrigida a procedure `sp_CadastrarCliente`, que gravava na coluna inexistente `senha`, e trocadas as datas de check-in e check-out de `int` para `date`. Incluídos dados de exemplo em todas as tabelas. Corrigidos dois READMEs: o da raiz citava a pasta `sql`, que não existe mais, e o do Flutter apontava para `docs` em vez de `documentacao`.

## Pendências dependentes da equipe

Conectar e executar no SQL Server real; configurar e homologar sandbox; conferir cotações/datas/vagas; compilar Android/iOS no ambiente nativo; definir hospedagem/HTTPS se forem publicar. A entrega não representa emissão real de passagens, vouchers de hotel ou ingressos por fornecedores.
