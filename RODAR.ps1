$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Get-ChildItem (Join-Path $root "scripts") -Filter "*.ps1" -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue
& .\scripts\run.ps1
