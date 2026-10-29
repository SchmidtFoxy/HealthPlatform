$ErrorActionPreference = "Stop"
Write-Host "============================================================" -ForegroundColor DarkGreen
Write-Host " AESYN v0.72.1 | PAINEL DO ADMINISTRADOR E EQUIPE DO PACIENTE" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkGreen

$version=(Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if($version -ne '0.72.1'){throw "VERSION.txt esperado 0.72.1; atual: $version"}

# O front e o build do Vite (src/HealthPlatform.Web) commitado em wwwroot.
$www=Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot'
foreach($arquivo in @('index.html','sw.js','push-sw.js','manifest.webmanifest','app/sw.js','icons/icon-192.png')){
 if(-not (Test-Path (Join-Path $www $arquivo))){throw "PREPARAR v0.72.1 incompleto: wwwroot/$arquivo (rode npm run build em src/HealthPlatform.Web)"}
}
if(-not (Get-ChildItem (Join-Path $www 'assets') -Filter 'index-*.js')){throw 'PREPARAR v0.72.1 incompleto: bundle do app ausente.'}

Write-Host "    Front do app: OK." -ForegroundColor Green
