$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# Hotfix v0.9.7-r1: o PREPARAR precisa conseguir recompilar mesmo quando uma
# instancia local anterior da API ainda esta aberta. O processo mantem as DLLs
# de Domain/Infrastructure bloqueadas no Windows e faz o dotnet build falhar
# com MSB3021/MSB3027. Encerramos somente a instancia local do HealthPlatform
# associada a este workspace/porta de desenvolvimento antes de compilar.
function Stop-HealthPlatformLocalApi {
    $stopped = @()

    # Primeiro, tenta localizar quem esta escutando na porta local oficial.
    try {
        $listeners = Get-NetTCPConnection -LocalPort 5180 -State Listen -ErrorAction SilentlyContinue
        foreach ($listener in $listeners) {
            $proc = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
            if ($proc -and ($proc.ProcessName -eq 'HealthPlatform.Api' -or $proc.ProcessName -eq 'dotnet')) {
                Write-Host "[API] Encerrando instancia local antiga na porta 5180 (PID $($proc.Id))..." -ForegroundColor Yellow
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $stopped += $proc.Id
            }
        }
    } catch {
        Write-Host "[API] Nao foi possivel consultar/encerrar a porta 5180: $($_.Exception.Message)" -ForegroundColor DarkYellow
    }

    # Fallback para executavel self-hosted que pode continuar segurando DLLs
    # mesmo sem listener ativo. Restringimos ao caminho do workspace atual.
    Get-Process -Name 'HealthPlatform.Api' -ErrorAction SilentlyContinue | ForEach-Object {
        if ($stopped -contains $_.Id) { return }
        $path = $null
        try { $path = $_.Path } catch { $path = $null }
        if (-not $path -or $path.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
            Write-Host "[API] Encerrando HealthPlatform.Api antigo (PID $($_.Id)) antes do build..." -ForegroundColor Yellow
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            $stopped += $_.Id
        }
    }

    if ($stopped.Count -gt 0) {
        Start-Sleep -Milliseconds 700
        Write-Host "[API] Instancia anterior encerrada; DLLs liberadas para compilacao." -ForegroundColor DarkGray
    }
}

Stop-HealthPlatformLocalApi

# Evita um segundo prompt de seguranca ao chamar scripts internos extraidos do ZIP.
Get-ChildItem (Join-Path $root "scripts") -Filter "*.ps1" -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue

function Assert-NativeSuccess([string]$Message) {
    if ($LASTEXITCODE -ne 0) { throw $Message }
}

Write-Host "[Docker] Verificando Docker Desktop..." -ForegroundColor Cyan
docker info *> $null
Assert-NativeSuccess "Docker Desktop nao esta rodando. Abra o Docker Desktop e execute este script novamente."

# Versoes anteriores usavam o mesmo container_name, mas outro Compose project.
# Como o v0.1.0 anterior nem chegou a criar as migrations, nesta revisao podemos
# descartar apenas o CONTAINER legado (volumes antigos nao sao apagados).
$existingId = docker ps -aq --filter "name=^/healthplatform-postgres$"
Assert-NativeSuccess "Falha ao consultar containers Docker."

if ($existingId) {
    # Lemos o JSON do inspect no PowerShell para evitar problemas de escaping
    # dos templates Go do Docker no Windows PowerShell 5.1.
    $inspectJson = docker inspect healthplatform-postgres 2>$null
    if ($LASTEXITCODE -eq 0 -and $inspectJson) {
        $inspectData = $inspectJson | ConvertFrom-Json
        $composeProject = $inspectData[0].Config.Labels.'com.docker.compose.project'
    } else {
        $composeProject = ""
    }

    if ($composeProject -ne "healthplatform") {
        Write-Host "[Docker] Container legado encontrado. Removendo somente o container antigo..." -ForegroundColor Yellow
        docker rm -f healthplatform-postgres | Out-Null
        Assert-NativeSuccess "Nao foi possivel remover o container legado healthplatform-postgres."
    }
}

Write-Host "[Docker] Subindo PostgreSQL..." -ForegroundColor Cyan
docker compose up -d
Assert-NativeSuccess "Falha ao iniciar PostgreSQL."

Write-Host "[Docker] Aguardando PostgreSQL ficar saudavel..." -ForegroundColor Cyan
$healthy = $false
for ($i = 0; $i -lt 30; $i++) {
    $status = docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' healthplatform-postgres 2>$null
    if ($status -eq "healthy" -or $status -eq "running") {
        if ($status -eq "healthy") { $healthy = $true; break }
    }
    Start-Sleep -Seconds 1
}
if (-not $healthy) {
    docker logs --tail 30 healthplatform-postgres
    throw "PostgreSQL nao ficou saudavel no tempo esperado."
}

& .\scripts\setup.ps1
