$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

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
