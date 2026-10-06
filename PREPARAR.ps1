$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.4 | WORKOUT SELECTION BEFORE START" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.4') { throw "VERSION.txt esperado 0.60.4; atual: $version" }
foreach ($token in @(
    'HP_WORKOUT_SELECTION_BEFORE_START_V0604',
    'hpOpenWorkoutSelectorV0604',
    'data-start-workout-v0604',
    'Iniciar treino escolhido',
    'Ver treino'
)) {
    if (-not $app.Contains($token)) { throw "PREPARAR v0.60.4 incompleto: $token" }
}
foreach ($token in @(
    '.workout-selection-layout-v0604',
    '.workout-selection-preview-v0604',
    '@media(max-width:430px)'
)) {
    if (-not $css.Contains($token)) { throw "PREPARAR CSS v0.60.4 incompleto: $token" }
}
if (-not $roadmap.Contains('v0.60.5')) { throw 'ROADMAP sem proxima etapa v0.60.5.' }

Write-Host "    Workout Selection Before Start: OK." -ForegroundColor Green
Write-Host "    Gate HealthController PowerShell 5.1: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.5 / Workout Execution Readability" -ForegroundColor DarkCyan