﻿$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $root "src\HealthPlatform.Api\Services\PainelMedicinaEsporteService.cs"
$clean = Join-Path $root "scripts\recovery\PainelMedicinaEsporteService.cs.clean"
if (-not (Test-Path $clean)) { throw "Copia canonica ausente: $clean" }
Copy-Item -LiteralPath $clean -Destination $target -Force
$source = Get-Content -LiteralPath $target -Encoding UTF8 -Raw
foreach ($guard in @('nao produzir diagnostico','previsao de lesao','prescricao automatica')) {
    if (-not $source.Contains($guard)) { throw "Guardrail ausente apos reparo: $guard" }
}
Write-Host "PainelMedicinaEsporteService restaurado com guardrails v0.11.0." -ForegroundColor Green
