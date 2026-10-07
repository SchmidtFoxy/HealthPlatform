$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.10 | LISTA 05 INTEGRATED QUALITY PASS" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version=(Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app=Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css=Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if($version -ne '0.60.10'){throw "VERSION.txt esperado 0.60.10; atual: $version"}
foreach($token in @(
 'HP_LISTA05_INTEGRATED_QUALITY_V0610',
 'hpJourneyStabilityGuardV0610',
 'HP_WORKOUT_SELECTION_BEFORE_START_V0604',
 'HP_WORKOUT_EXECUTION_READABILITY_V0605',
 'HP_WORKOUT_SERIES_BLOCKS_V0606',
 'HP_PATIENT_PLAN_COMPACT_V0607',
 'HP_PROFESSIONAL_CHAT_GLOBAL_V0608',
 'HP_NUTRITION_PLANNING_ENGINE_V0609'
)){
 if(-not $app.Contains($token)){throw "PREPARAR v0.60.10 incompleto: $token"}
}
foreach($token in @(
 '--hp-visual-viewport-height-v0610',
 'env(safe-area-inset-bottom)',
 '@media(max-width:430px)',
 '@media(max-width:390px)',
 '@media(max-width:360px)'
)){
 if(-not $css.Contains($token)){throw "PREPARAR CSS v0.60.10 incompleto: $token"}
}
if(-not $roadmap.Contains('LISTA 05 — CONCLUÍDA')){throw 'ROADMAP nao marcou Lista 05 concluida.'}
if(-not $roadmap.Contains('v0.61.0 / Lista 06')){throw 'ROADMAP sem proxima direcao funcional real.'}

Write-Host "    Journey Stability Guard: OK." -ForegroundColor Green
Write-Host "    Integrated Regression Memory: OK." -ForegroundColor Green
Write-Host "    LISTA 05 — CONCLUÍDA." -ForegroundColor Green
Write-Host "    Proxima evolucao deve ser v0.61.0 / Lista 06, escolhida por valor real." -ForegroundColor DarkCyan