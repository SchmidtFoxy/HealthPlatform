$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.60.8 | PROFESSIONAL CHAT IDENTITY & GLOBAL INBOX" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$app = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$index = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/index.html') -Raw
$roadmap = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw

if ($version -ne '0.60.8') { throw "VERSION.txt esperado 0.60.8; atual: $version" }

foreach ($token in @(
  'HP_PROFESSIONAL_CHAT_GLOBAL_V0608',
  'hpEnhanceProfessionalChatGlobalV0608',
  'data-chat-global-search-v0608',
  'HP_DEMO_PROFESSIONAL_NAME_V0608'
)) {
  if (-not $app.Contains($token)) { throw "PREPARAR v0.60.8 incompleto: $token" }
}

$demoIdentityExactSignature0608 = 'return /^(Dr\.?\s*Testinho|Doutor\s+Testinho)$/i.test'
if (-not $app.Contains($demoIdentityExactSignature0608)) {
    throw 'Protecao de identidade demo por match exato ausente.'
}

foreach ($token in @(
  'nav-chat-global-v0608',
  'data-view="chat-profissional"',
  'data-chat-global-badge'
)) {
  if (-not $index.Contains($token)) { throw "PREPARAR sidebar v0.60.8 incompleta: $token" }
}

foreach ($token in @(
  '.professional-chat-global-tools-v0608',
  '.chat-global-filtered-v0608',
  '@media(max-width:430px)'
)) {
  if (-not $css.Contains($token)) { throw "PREPARAR CSS v0.60.8 incompleto: $token" }
}

if (-not $roadmap.Contains('v0.60.9')) {
    throw 'ROADMAP sem proxima etapa v0.60.9.'
}

Write-Host "    Professional Chat Identity & Global Inbox: OK." -ForegroundColor Green
Write-Host "    Demo identity exact-match protection: OK." -ForegroundColor Green
Write-Host "    Proxima etapa: v0.60.9 / Professional Nutrition Planning Engine 3.0" -ForegroundColor DarkCyan