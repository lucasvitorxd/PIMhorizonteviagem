# Gera o banco-completo.sql a partir das migrations do EF Core.
# Rode na pasta backend:  pwsh -NoProfile -File .\gerar-banco.ps1
# Precisa do dotnet-ef instalado (dotnet tool install --global dotnet-ef --version 9.0.22).
$ErrorActionPreference = "Stop"

$saida = Join-Path $PSScriptRoot "banco-completo.sql"
$temporario = Join-Path $PSScriptRoot "banco-completo.gerado.sql"

Push-Location $PSScriptRoot
try {
    dotnet ef migrations script `
        --idempotent `
        --project "HorizonteViagens.Repositorio\HorizonteViagens.Repositorio.csproj" `
        --startup-project "HorizonteViagens.Api\HorizonteViagens.Api.csproj" `
        --output $temporario

    if ($LASTEXITCODE -ne 0) {
        throw "O EF Core não conseguiu gerar o script das migrations."
    }

    $sql = Get-Content -Raw -LiteralPath $temporario
    $cabecalho = "-- Script idempotente gerado pelas migrations do EF Core.`r`n-- Selecione o banco de destino no SSMS antes de executar.`r`nSET QUOTED_IDENTIFIER ON;`r`nGO`r`n"
    Set-Content -LiteralPath $saida -Value ($cabecalho + $sql) -Encoding utf8
}
finally {
    if (Test-Path -LiteralPath $temporario) {
        Remove-Item -LiteralPath $temporario -Force
    }
    Pop-Location
}

Write-Host "Script atualizado: $saida"
