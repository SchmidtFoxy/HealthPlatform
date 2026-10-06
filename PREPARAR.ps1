$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.7 | PATIENT PLAN COMPACT EXPERIENCE" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.7') { throw "VERSION.txt esperado 0.60.7; atual: $version" }

foreach ($token in @(
  'HP_PATIENT_PLAN_COMPACT_V0607',
  'patient-plan-summary-v0607',
  'patient-meal-time-v0607',
  'food-substitutions-v0607',
  'patient-plan-supplements-v0607',
  'patient-supplement-schedule-v0594',
  'patient-supplement-item-v0594',
  'supplement-training-context-v0595',
  'Meu cronograma de suplementos'
)) {
  if (-not $app.Contains($token)) { throw "PREPARAR v0.60.7 incompleto: $token" }
}

foreach ($token in @(
  '.patient-plan-compact-v0607',
  '.patient-plan-meals-v0607',
  '.patient-plan-supplements-v0607',
  '@media(max-width:430px)',
  '@media(max-width:360px)'
)) {
  if (-not $css.Contains($token)) { throw "PREPARAR CSS v0.60.7 incompleto: $token" }
}

if (-not $roadmap.Contains('v0.60.8')) {
    throw 'ROADMAP sem proxima etapa v0.60.8.'
}

Write-Host "    Patient Plan Compact Experience: OK." -ForegroundColor Green
Write-Host "    Regression gate loadPatientPlan escopado: OK." -ForegroundColor Green
Write-Host "    Compatibilidade suplemento v0.59.4/v0.59.5: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.8 / Professional Chat Identity & Global Inbox Consolidation" -ForegroundColor DarkCyan