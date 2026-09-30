$ErrorActionPreference = "Stop"
# AESYN v0.26.6: runtime permanece versionado dinamicamente por VERSION.txt.
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$versionAesyn = if (Test-Path (Join-Path $root "VERSION.txt")) { (Get-Content (Join-Path $root "VERSION.txt") -Raw).Trim() } else { "desconhecida" }
Write-Host ""
Write-Host "============================================================" -ForegroundColor DarkGray
Write-Host (" AESYN {0} | Runtime local" -f $versionAesyn) -ForegroundColor Cyan
Write-Host " Athlete & Human Performance" -ForegroundColor DarkCyan
Write-Host "============================================================" -ForegroundColor DarkGray
Write-Host ""

Get-ChildItem (Join-Path $root "scripts") -Filter "*.ps1" -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue
& .\scripts\run.ps1
