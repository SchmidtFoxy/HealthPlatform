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

# Nao dependa do launchSettings para o ambiente local: deixe explicito.
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5180"

# O TESTAR.ps1 usa as credenciais de appsettings.json.
# Force o processo local a usar a mesma fonte, neutralizando Seed__* antigo
# herdado do Windows/PowerShell que poderia sobrescrever o JSON e causar 401.
$localSettings = Get-Content (Join-Path $root "src\HealthPlatform.Api\appsettings.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$env:Seed__AdminEmail = [string]$localSettings.Seed.AdminEmail
$env:Seed__AdminPassword = [string]$localSettings.Seed.AdminPassword

if ([string]::IsNullOrWhiteSpace($env:Seed__AdminEmail) -or [string]::IsNullOrWhiteSpace($env:Seed__AdminPassword)) {
    throw "Seed:AdminEmail/AdminPassword ausentes no appsettings.json local."
}

# Evita testar sem querer contra uma instancia antiga ainda presa na porta 5180.
try {
    $listeners = @(Get-NetTCPConnection -LocalPort 5180 -State Listen -ErrorAction SilentlyContinue)
    foreach ($listener in $listeners) {
        $proc = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
        if ($proc -and $proc.ProcessName -eq "dotnet") {
            Write-Host "Encerrando instancia antiga da API (PID $($proc.Id))..." -ForegroundColor Yellow
            Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            Start-Sleep -Milliseconds 700
        }
    }
} catch {
    Write-Host "Aviso: nao foi possivel verificar a porta 5180 automaticamente: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "Iniciando HealthPlatform v0.8.0..." -ForegroundColor Green
Write-Host "Ambiente: Development (forcado pelo RODAR.ps1)" -ForegroundColor DarkGreen
Write-Host "Interface: http://localhost:5180" -ForegroundColor Cyan
Write-Host "Swagger:   http://localhost:5180/swagger" -ForegroundColor DarkCyan

$browserScript = 'Start-Sleep -Seconds 4; Start-Process "http://localhost:5180"'
Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile', '-Command', $browserScript | Out-Null

dotnet run --no-launch-profile --project .\src\HealthPlatform.Api --urls "http://localhost:5180"
