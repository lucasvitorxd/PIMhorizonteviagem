# Horizonte Viagens Backend

Backend da solução Horizonte Viagens, desenvolvido em ASP.NET Core com Entity Framework Core e SQL Server. Este README descreve as camadas, o fluxo dos dados, as migrations e a API. As telas Razor existentes fazem parte do projeto Web; alterações visuais ficam separadas deste guia.

## O que o backend oferece

- Cadastro, consulta, alteração e exclusão de clientes pela API.
- Login de administrador com senha armazenada como hash e emissão de token JWT.
- Persistência de clientes e administradores com Entity Framework Core.
- Busca de clientes por nome por meio da procedure `sp_Clientes_BuscarPorNome`, chamada com Dapper.
- Modelo EF Core para hotéis, telefones, voos, passagens, quartos, combos, compras, monitoramentos e passeios.
- Migrations para criar e atualizar o banco SQL Server.

As entidades de viagens já estão mapeadas no `AppDbContext`, mas ainda não têm repositórios, regras de negócio nem endpoints CRUD na API. A API atualmente expõe operações de clientes e autenticação.

## Projetos e responsabilidades

| Projeto | Responsabilidade |
|---|---|
| `HorizonteViagens.Modelos` | Entidades de domínio e DTOs usados nas requisições e respostas. |
| `HorizonteViagens.Repositorio` | `AppDbContext`, mapeamentos EF Core, repositórios, migrations e script SQL de referência. |
| `HorizonteViagens.Negocio` | Regras de cadastro, validação, autenticação e hash de senha. |
| `HorizonteViagens.Api` | Endpoints REST, autenticação JWT, autorização, OpenAPI e Scalar. |
| `HorizonteViagens.Web` | Aplicação MVC existente, que usa as mesmas regras e repositórios. |

Dependências principais:

```text
API ──► Negocio ──► Repositorio ──► Modelos
Web ──► Negocio ──► Repositorio ──► Modelos
```

O controller recebe uma classe de negócio por injeção de dependência. A classe de negócio recebe uma interface de repositório; o ASP.NET Core monta essa cadeia no `Program.cs`.

## Fluxo do cadastro de cliente

1. `ClientesController` recebe o JSON como `ClienteSalvarDto`.
2. O DTO valida campos de entrada e converte os dados em uma entidade `Cliente`.
3. `ClienteNegocio` normaliza e valida CPF, celular, e-mail e senha. A senha nunca é salva em texto puro: `SenhaHasher` gera um hash.
4. `ClienteRepositorio` grava ou consulta a entidade usando `AppDbContext` e EF Core.
5. A API converte a entidade em `ClienteDto`, que não inclui `SenhaHash`.

A busca por nome segue o mesmo caminho até o repositório, mas chama a stored procedure com Dapper e parâmetro separado, evitando concatenar a entrada do usuário no SQL.

## Entity Framework Core e banco de dados

O `AppDbContext` representa a conexão e o modelo relacional. Cada `DbSet` representa uma tabela. O método `OnModelCreating` configura tamanhos, campos obrigatórios, índices únicos, chaves compostas, relacionamentos e restrições de datas e valores.

`Clientes` tem uma trigger de atualização. O mapeamento declara `HasTrigger("TR_Clientes_DataAtualizacao")` para que o EF Core gere comandos compatíveis com essa tabela.

Migrations existentes:

- `Inicial`: tabela `Clientes`, índices únicos, trigger de atualização e procedure de busca.
- `AdministradorELogin`: senha e celular do cliente, tabela `Administradores` e conta inicial.
- `EsquemaViagens`: endereço do cliente e tabelas do modelo de viagens.
- `CatalogoCombos`: nome, destino, vagas e categoria do combo, mais a procedure `sp_Combos_BuscarPorDestino`.
- `ConcorrenciaCombos`: coluna `RowVersion` no combo, para duas compras não pegarem a mesma vaga.
- `IntegracaoPortal`: campos do portal (preço por grupo, partida, publicação), reservas, favoritos e documentos.

### Aplicar as migrations

Instale a ferramenta do EF Core uma vez na máquina, se ainda não estiver instalada:

```bash
dotnet tool install --global dotnet-ef --version 9.0.22
```

Na pasta onde está `HorizonteViagens.sln`, aplique as migrations:

```bash
dotnet ef database update --project HorizonteViagens.Repositorio --startup-project HorizonteViagens.Api
```

Para criar uma migration depois de alterar uma entidade ou o mapeamento:

```bash
dotnet ef migrations add NomeDaMudanca --project HorizonteViagens.Repositorio --startup-project HorizonteViagens.Api
dotnet ef database update --project HorizonteViagens.Repositorio --startup-project HorizonteViagens.Api
```

O banco é criado **só pelas migrations**. O script SQL completo para o grupo de banco de dados já está pronto em `banco-completo.sql`. Ele é idempotente: pode rodar mais de uma vez no mesmo banco sem duplicar nada. No SSMS, escolha o banco de destino antes de executar.

Para gerar o script de novo depois de uma migration nova, rode `gerar-banco.ps1` (precisa do `dotnet-ef` instalado) ou, no Console do Gerenciador de Pacotes:

```
Script-Migration -Idempotent -Project HorizonteViagens.Repositorio -StartupProject HorizonteViagens.Api -Output banco-completo.sql
```

Se gerar pelo Console, acrescente `SET QUOTED_IDENTIFIER ON;` e `GO` no topo do arquivo; sem isso o SSMS recusa um dos índices filtrados. O `gerar-banco.ps1` já faz isso sozinho.

O script `novo_banco_14.11.sql` do grupo de banco **não** serve para este backend: ele cria tabelas com outros nomes e sem colunas que o sistema usa (vagas do combo, status da compra, senha com hash). A lista do que precisa mudar nele está no documento de tarefas do grupo.

### Validação no SQL Server (18/11/2026)

Feita num SQL Server local, no banco `HorizonteViagensTeste`: a migration `Verificacao` veio vazia, o `Update-Database` aplicou as seis migrations, o `banco-completo.sql` criou as 17 tabelas num banco vazio (e rodou de novo sem duplicar nada), e o fluxo da Api passou inteiro: login do administrador, criação de combo, cadastro e login da cliente, compras e `/health`.

### Configurar a conexão

A chave `ConnectionStrings:Padrao` está nos `appsettings.json` da API e do projeto Web. O valor de exemplo aponta para `localhost`, banco `HorizonteViagensTeste`, usando autenticação do Windows:

```json
"Padrao": "Server=localhost;Database=HorizonteViagensTeste;Trusted_Connection=True;TrustServerCertificate=True"
```

Troque `Server=` pelo nome da instância mostrado no SSMS. Para SQL Express, por exemplo, a instância costuma ser `localhost\\SQLEXPRESS` dentro do JSON. Se usar autenticação SQL, mantenha usuário e senha fora de arquivos versionados; use User Secrets ou variáveis de ambiente.

## API REST

Em desenvolvimento, a API disponibiliza OpenAPI em `/openapi/v1.json` e a interface Scalar em `/scalar`. Com o perfil HTTP padrão, a base é `http://localhost:5218`.

| Método e rota | Acesso | Resultado |
|---|---|---|
| `POST /api/autenticacao/login` | Público | Valida administrador e devolve token JWT. |
| `POST /api/clientes` | Público | Cadastra cliente; responde `201` ou `400`. |
| `GET /api/clientes` | Administrador | Lista clientes. |
| `GET /api/clientes/{id}` | Administrador | Obtém cliente ou responde `404`. |
| `GET /api/clientes/buscar?nome=ana` | Administrador | Busca pela stored procedure. |
| `PUT /api/clientes/{id}` | Administrador | Atualiza cliente; senha vazia mantém a senha atual. |
| `DELETE /api/clientes/{id}` | Administrador | Exclui cliente. |
| `POST /api/autenticacao/login-cliente` | Público | Autentica cliente e devolve token com papel `Cliente`. |
| `GET /api/combos?destino=Recife` | Público | Lista combos disponíveis; filtro é opcional e usa `sp_Combos_BuscarPorDestino` via Dapper. |
| `POST /api/combos` | Administrador | Cria combo da vitrine. `PUT` e `DELETE /api/combos/{id}` atualizam e removem. |
| `POST /api/compras` | Cliente | Compra simples: basta `{ "comboId": 1 }` (`viajantes` opcional, padrão 1). O preço é calculado no servidor, o cliente vem do token e a vaga é debitada na mesma transação. Sem vaga: `400` com `"erro"`. |
| `GET /api/compras` | Administrador | Monitora compras com cliente, combo e valor. |
| `POST /api/compras/{id}/monitoramento` | Administrador | Associa a compra ao administrador que a acompanha. |
| `GET /api/portal/catalogo` e `GET /api/portal/orcamento/{id}?viajantes=2` | Público | Catálogo publicado e preço calculado para o grupo. |
| `/api/portal/perfil`, `/favoritos`, `/reservas` | Cliente | Área do cliente usada pelo site e pelo app (reservas com até 6 viajantes, cancelamento, documentos). |
| `POST /api/portal/reservas/{id}/pagamento` | Cliente | Gera o link de pagamento de **teste** do Mercado Pago (só funciona com o token configurado). |
| `GET /api/portal/gestao/reservas` | Administrador | Todas as reservas, para a agência acompanhar e anexar documentos. |

Envie o token de administrador no cabeçalho `Authorization: Bearer <token>`. Os endpoints de clientes, exceto o cadastro, exigem o papel `Administrador`.

Para um combo poder ser comprado, ele precisa estar **publicado**, com **preço confirmado**, **data de partida futura** e **vagas**. Os combos de exemplo criados pelo `--preparar-banco` nascem com preço não confirmado e 0 vagas: o administrador revisa e libera cada um. Para compra, use o token retornado por `login-cliente`.

### Preparar o banco (opcional)

```bash
dotnet run --project HorizonteViagens.Api -- --preparar-banco
dotnet run --project HorizonteViagens.Api -- --preparar-banco --acessos-teste
```

Aplica as migrations (como o `Update-Database`) e cadastra 6 combos de exemplo, só se o banco ainda não tiver nenhum. Com `--acessos-teste` (só em Development), cria também `admin@horizonte.test` / `HorizontePim!2026` e `cliente@horizonte.test` / `ViagemPim!2026`, sem data de validade. O administrador original (`admin@horizonteviagens.com` / `Horizonte@2026`) continua funcionando.

### Testes automatizados

Execute `dotnet test` na pasta da solution (ou, no Visual Studio, **Teste › Executar Todos os Testes**). Há dois tipos:

- **Unitários** (sem banco, com repositórios falsos): CPF e senha (`ClienteNegocioTests`), cadastro e alteração de combo (`ComboNegocioTests`) e regras da compra (`CompraNegocioTests`).
- **Integração** (`PortalIntegrationTests`): sobem a Api e a Web em memória com SQLite e fazem chamadas HTTP de verdade: compra simples e sem vaga, preço por grupo, reserva repetida, autorização, cancelamento, CRUD de combo com versão, pagamento e preparação do banco.

O SQLite é só para os testes. A concorrência de verdade (o `RowVersion` no SQL Server) e as migrations precisam ser conferidas num SQL Server.

## Executar e conferir

```bash
dotnet restore
dotnet build HorizonteViagens.sln
dotnet run --project HorizonteViagens.Api
```

Abra `http://localhost:5218/scalar`. O arquivo `HorizonteViagens.Api/HorizonteViagens.Api.http` contém chamadas de exemplo para login, cadastro, consulta, alteração e exclusão. Algumas chamadas alteram dados do banco; use dados de teste.

## Segurança e configuração

- O sistema guarda hash da senha, não a senha original.
- O DTO de resposta não expõe a coluna `SenhaHash`.
- A busca Dapper usa parâmetros separados do comando SQL.
- A chave do JWT **não fica no `appsettings.json`**. Em desenvolvimento, cada pessoa guarda a sua no User Secrets (fora da pasta do projeto, nunca vai para o Git):
  ```bash
  dotnet user-secrets set "Jwt:Chave" "uma-frase-longa-e-secreta-com-32-caracteres-ou-mais" --project HorizonteViagens.Api
  ```
  Se ninguém configurar, a Api gera uma chave aleatória ao iniciar, só em desenvolvimento: ela sobe normalmente, mas o login precisa ser refeito toda vez que a Api reinicia. Em produção, a chave vem da variável de ambiente `Jwt__Chave`, e sem ela a Api não sobe.
- `/health` na Api e na Web responde `Healthy` quando o sistema está de pé.
- Os controllers registram log estruturado (`ILogger`) só com Ids, nunca com dados pessoais.
- O pagamento usa só a conta de **teste** do Mercado Pago e é opcional. O token também fica no User Secrets:
  ```bash
  dotnet user-secrets set "Pagamento:Token" "TEST-..." --project HorizonteViagens.Api
  dotnet user-secrets set "Pagamento:Token" "TEST-..." --project HorizonteViagens.Web
  ```
  Sem token, tudo funciona; só o botão de pagar avisa que o pagamento está indisponível. Mesmo que alguém configure o token de uma conta real por engano, o sistema recusa.
- O CORS libera só os endereços de `Cors:Origens` (site estático do grupo) e o login tem limite de 20 tentativas por minuto por IP.
- `TrustServerCertificate=True` é adequado para desenvolvimento local; revise a política de certificados antes de usar em produção.

## Site (HorizonteViagens.Web)

O site segue o padrão MVC: o controller recebe o pedido, chama a camada de Negocio e entrega um **ViewModel** para a view, que só mostra os dados.

- **ViewModels** (`Web/Models`): `VitrineViewModel`, `ComboCardViewModel` e `ComboDetalheViewModel` para a vitrine e o detalhe do pacote; `ReservaViewModel` e `DocumentoViewModel` para reservas e carteira. Eles chegam com os valores já calculados.
- **Regras fora da tela:** o preço do grupo (`Precificacao`) e a pergunta "dá para reservar?" (`PortalNegocio.Reservavel`) ficam no Negocio. A view e o JavaScript não fazem conta; ao trocar o número de viajantes, o formulário é reenviado e o servidor calcula de novo.
- **Layout e partial views:** `_Layout` (cabeçalho, menus, mensagens e rodapé), `_Busca` (formulário de busca), `_Card` (card do pacote), `_Documento` (documento da carteira) e `_Formulario` (campos de combo e de cliente, usados em Criar e Editar).
- **Responsivo:** `horizonte.css` tem dois pontos de quebra (900px e 550px). Abaixo de 900px o menu principal vira o botão ☰ e as tabelas do administrador rolam dentro da própria caixa (`table-responsive`).
- **Exclusão de combo com reservas:** o `ComboNegocio` recusa e a tela mostra a mensagem, sugerindo despublicar o combo. Se uma reserva entrar bem no meio da exclusão, o `ComboRepositorio` traduz o erro do banco numa resposta (`ResultadoExclusao`) e o Negocio mostra a mesma mensagem, sem conhecer o Entity Framework.

## Guia do código

O documento [DOCUMENTACAO-BACKEND.docx](DOCUMENTACAO-BACKEND.docx) explica as classes principais, o caminho de uma requisição, o uso do EF Core e do Dapper e o que ainda falta para disponibilizar as entidades de viagem pela API.
