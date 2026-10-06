$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.5 | WORKOUT EXECUTION READABILITY" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$health = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/HealthController.cs') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.5') { throw "VERSION.txt esperado 0.60.5; atual: $version" }
foreach ($token in @(
    'HP_WORKOUT_EXECUTION_READABILITY_V0605',
    'hpWorkoutPrescriptionChipsV0605',
    'hpUpdateWorkoutExecutionProgressV0605',
    'data-workout-execution-feedback-v0605'
)) {
    if (-not $app.Contains($token)) { throw "PREPARAR v0.60.5 incompleto: $token" }
}
if ([regex]::Matches($health,'version = "0.60.5"').Count -lt 2) {
    throw 'HealthController nao anuncia 0.60.5 nos dois payloads.'
}
if (-not $roadmap.Contains('v0.60.6')) { throw 'ROADMAP sem proxima etapa v0.60.6.' }

Write-Host "    Workout Execution Readability: OK." -ForegroundColor Green
Write-Host "    Gates estaticos de Health corrente alinhados para v0.60.5: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.6 / Workout Series Blocks / Sub-series Builder" -ForegroundColor DarkCyan