$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Assert-NativeSuccess([string]$Message) {
    if ($LASTEXITCODE -ne 0) { throw $Message }
}

docker info *> $null
Assert-NativeSuccess "Docker Desktop nao esta rodando. Abra o Docker Desktop e tente novamente."

docker compose up -d
Assert-NativeSuccess "Falha ao iniciar PostgreSQL via Docker Compose."

Write-Host "Iniciando HealthPlatform v0.2.2..." -ForegroundColor Green
Write-Host "Interface: http://localhost:5180" -ForegroundColor Cyan
Write-Host "Swagger:   http://localhost:5180/swagger" -ForegroundColor DarkCyan

$browserScript = 'Start-Sleep -Seconds 4; Start-Process "http://localhost:5180"'
Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile', '-Command', $browserScript | Out-Null

dotnet run --project .\src\HealthPlatform.Api
