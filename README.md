# Horizonte Viagens

O frontend web oficial é o ASP.NET Core MVC, em backend/HorizonteViagens.Web. O Flutter usa a Web API. Os dois compartilham Negocio, Repositorio, Modelos e o banco SQL Server.

## Estrutura

| Pasta | Responsabilidade |
|---|---|
| backend/HorizonteViagens.Web | Site oficial, controllers, ViewModels e Razor |
| backend/HorizonteViagens.Api | Endpoints consumidos pelo Flutter |
| backend/HorizonteViagens.Negocio | Preços, catálogo e regras |
| backend/HorizonteViagens.Repositorio | EF Core, SQL e procedures |
| backend/HorizonteViagens.Modelos | Entidades e contratos |
| backend/HorizonteViagens.Testes | Testes automatizados .NET |
| mobile | Aplicativo Flutter |
| banco-de-dados | Script da disciplina de Banco de Dados, com tabelas, procedure, trigger e dados de exemplo |
| scripts | Preparação e execução local |

O site estático da raiz e a pasta PIM - Horizonte (Web) foram retirados deste pacote. O ZIP original permanece como histórico. Não é necessário Node.js para executar o MVC. GitHub Pages não executa ASP.NET Core.

## Executar

Use SDK .NET 9 compatível com os projetos, SQL Server acessível e Flutter compatível com mobile/pubspec.yaml. Abra backend/HorizonteViagens.sln no Visual Studio ou use PowerShell.

Na raiz, prepare o banco de desenvolvimento:

```powershell
.\scripts\preparar-banco.ps1 -Conexao 'Server=.\SQLEXPRESS;Database=HorizonteViagensPIM;Trusted_Connection=True;TrustServerCertificate=True' -AcessosTeste
```

Execute em dois terminais separados:

```powershell
.\scripts\iniciar-site.ps1 -Conexao 'Server=.\SQLEXPRESS;Database=HorizonteViagensPIM;Trusted_Connection=True;TrustServerCertificate=True'
```

```powershell
.\scripts\iniciar-api.ps1 -Conexao 'Server=.\SQLEXPRESS;Database=HorizonteViagensPIM;Trusted_Connection=True;TrustServerCertificate=True'
```

Site: http://localhost:5178. API: http://localhost:5218. Veja [o guia de execução](documentacao/COMO-EXECUTAR.md) para banco remoto, acessos de teste e pagamento sandbox.

## Validar

```powershell
dotnet test backend/HorizonteViagens.Testes/HorizonteViagens.Testes.csproj
cd mobile
flutter pub get
flutter analyze
flutter test
flutter run -d chrome --web-port 5300 --dart-define=API_URL=http://localhost:5218
```

No Android emulado, use http://10.0.2.2:5218; em celular físico, use o IP da máquina da API na mesma rede. SQL Server e checkout sandbox precisam de conferência no ambiente da equipe.

Leia [a revisão](documentacao/REVISAO-MVC-FLUTTER.md) e [a organização do Flutter](mobile/ORGANIZACAO.md).
