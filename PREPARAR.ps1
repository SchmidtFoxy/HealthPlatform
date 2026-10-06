$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.6 | WORKOUT SERIES BLOCKS / SUB-SERIES BUILDER" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$controller = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/ModelosPlanosTreinoController.cs') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.6') { throw "VERSION.txt esperado 0.60.6; atual: $version" }

foreach ($token in @(
    'HP_WORKOUT_SERIES_BLOCKS_V0606',
    'hpBindSeriesBlocksEditorV0606',
    'hpWorkoutSeriesBlocksPatientHtmlV0606'
)) {
    if (-not $app.Contains($token)) { throw "PREPARAR v0.60.6 incompleto: $token" }
}

foreach ($token in @(
    'TemplateBlocoSerie',
    'BlocosSeriesPrefixo',
    'ExtrairBlocosSeries',
    '?? new List<TemplateBlocoSerie>();'
)) {
    if (-not $controller.Contains($token)) { throw "PREPARAR backend v0.60.6 incompleto: $token" }
}

if ($controller.Contains('?? Array.Empty<TemplateBlocoSerie>();')) {
    throw 'Regression: fallback List/Array incompatível voltou ao backend.'
}

foreach ($token in @(
    '.series-blocks-editor-v0606',
    '.patient-series-blocks-v0606',
    '@media(max-width:430px)'
)) {
    if (-not $css.Contains($token)) { throw "PREPARAR CSS v0.60.6 incompleto: $token" }
}

if (-not $roadmap.Contains('v0.60.7')) {
    throw 'ROADMAP sem proxima etapa v0.60.7.'
}

Write-Host "    Workout Series Blocks / Sub-series Builder: OK." -ForegroundColor Green
Write-Host "    Backend compile fallback List: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.7 / Patient Plan Compact Experience" -ForegroundColor DarkCyan