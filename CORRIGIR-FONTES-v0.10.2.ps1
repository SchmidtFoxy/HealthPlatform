$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$controllers = Join-Path $root "src\HealthPlatform.Api\Controllers"
$recovery = Join-Path $root "scripts\recovery"

$pairs = @(
    @{ Name = "PortalPacienteController.cs"; First = "using HealthPlatform.Api.Contracts.Portal;" },
    @{ Name = "MeuPortalPacienteController.cs"; First = "using System.Text.Json;" }
)

foreach ($pair in $pairs) {
    $target = Join-Path $controllers $pair.Name
    $clean = Join-Path $recovery ($pair.Name + ".clean")
    if (-not (Test-Path $clean)) { throw "Copia limpa ausente: $clean" }
    Copy-Item -LiteralPath $clean -Destination $target -Force
    $first = Get-Content -LiteralPath $target -TotalCount 1
    if ($first -ne $pair.First) { throw "Falha ao restaurar $($pair.Name)." }
    Write-Host "[OK] $($pair.Name) restaurado." -ForegroundColor Green
}

Write-Host "Fontes v0.10.2 restauradas. Agora execute .\\PREPARAR.ps1" -ForegroundColor Cyan
