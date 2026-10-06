$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.3 | ATHLETE INTERESTS DISCOVERY 2.0" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$program = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Program.cs') -Raw
$healthController = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/HealthController.cs') -Raw
$index = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/index.html') -Raw
$sw = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/sw.js') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.3') { throw "VERSION.txt esperado 0.60.3; atual: $version" }

foreach ($token in @(
    'HP_ATHLETE_INTERESTS_DISCOVERY_V0603',
    'data-interest-tab-v0603="suggestions"',
    'data-interest-tab-v0603="mine"',
    'hpEnhanceAthleteInterestsV0603'
)) {
    if (-not $app.Contains($token)) { throw "PREPARAR v0.60.3 incompleto: $token" }
}

foreach ($token in @(
    '.athlete-interests-discovery-v0603',
    '.athlete-interests-tabs-v0603',
    '@media(max-width:430px)'
)) {
    if (-not $css.Contains($token)) { throw "PREPARAR CSS v0.60.3 incompleto: $token" }
}

if (-not $app.Contains("const HP_MVP_VERSION='0.60.3';")) {
    throw 'HP_MVP_VERSION nao esta em 0.60.3.'
}
if (-not $program.Contains('Version = "v0.60.3"')) {
    throw 'Swagger nao esta em v0.60.3.'
}
if (($healthController | Select-String -Pattern 'version = "0.60.3"' -AllMatches).Matches.Count -lt 2) {
    throw 'HealthController nao esta em 0.60.3.'
}
foreach ($asset in @('/app.css?v=0.60.3','/operations.css?v=0.60.3','/app.js?v=0.60.3')) {
    if (-not $index.Contains($asset)) { throw "Cache busting v0.60.3 ausente: $asset" }
}
if (-not $sw.Contains('aesyn-static-v0.60.3')) {
    throw 'Service Worker nao esta em v0.60.3.'
}
if (-not $roadmap.Contains('v0.60.4')) {
    throw 'ROADMAP sem proxima etapa v0.60.4.'
}

Write-Host "    Athlete Interests Discovery + identidade v0.60.3: OK." -ForegroundColor Green
Write-Host "    Compatibilidade PowerShell 5.1 / ROADMAP UTF-8: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.4 / Workout Selection Before Start" -ForegroundColor DarkCyan
