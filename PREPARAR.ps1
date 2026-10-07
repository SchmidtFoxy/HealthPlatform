$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.9 | PROFESSIONAL NUTRITION PLANNING ENGINE 3.0" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.9') { throw "VERSION.txt esperado 0.60.9; atual: $version" }

foreach ($token in @(
  'HP_NUTRITION_PLANNING_ENGINE_V0609',
  'hpOpenDietModelStudioV0609',
  'hpInstallNutritionModelCtaBridgeV0609',
  'hpNutritionPlanningSummaryV0609',
  'Montar dieta modelo',
  'Nova dieta',
  'hpMetabolicCalculatorCard(metabolicContext)',
  'Use os detalhes abaixo antes de decidir',
  'Use como referência. A ficha continua totalmente editável pelo profissional.',
  'nutrition-professional-context-v0609'
)) {
  if (-not $app.Contains($token)) { throw "PREPARAR v0.60.9 incompleto: $token" }
}

foreach ($token in @(
  '.nutrition-planning-steps-v0609',
  '.nutrition-planning-summary-v0609',
  '.nutrition-model-studio-v0609',
  '@media(max-width:430px)'
)) {
  if (-not $css.Contains($token)) { throw "PREPARAR CSS v0.60.9 incompleto: $token" }
}

if (-not $roadmap.Contains('v0.60.10')) {
    throw 'ROADMAP sem proxima etapa v0.60.10.'
}

Write-Host "    Nutrition Planning Engine 3.0: OK." -ForegroundColor Green
Write-Host "    Professional builder context: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.10 / Lista 05 Integrated Mobile & Cross-surface Quality Pass" -ForegroundColor DarkCyan