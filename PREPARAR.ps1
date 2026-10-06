$ErrorActionPreference = "Stop"

# Compatibilidade com Windows PowerShell 5.1: scripts com caracteres Unicode precisam de UTF-8 BOM.
$testarPath = Join-Path $PSScriptRoot "TESTAR.ps1"
if (Test-Path $testarPath) {
    $bytes = [System.IO.File]::ReadAllBytes($testarPath)
    $hasUtf8Bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    if (-not $hasUtf8Bom) {
        throw "TESTAR.ps1 precisa estar salvo como UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1."
    }
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# v0.59.0 - Product Brain gate: a fase funcional substitui a espiral de microversoes de handoff.
$versionAtual0590 = (Get-Content -Encoding UTF8 (Join-Path $root "VERSION.txt") -Raw).Trim()
if ($versionAtual0590 -ne "0.59.0") { throw "VERSION.txt esperado 0.59.0; atual: $versionAtual0590" }
$roadmapAtual0590 = Get-Content -Encoding UTF8 (Join-Path $root "ROADMAP.md") -Raw
foreach ($token0590 in @("Anti-Roadmap-Loop", "Nutrition & Communication Experience", "v0.59.1 — Meal Templates Quick Apply")) {
    if (-not $roadmapAtual0590.Contains($token0590)) { throw "Roadmap v0.59.0 incompleto: $token0590" }
}
Write-Host "[Produto] v0.59.0 / Nutrition & Communication Experience: roadmap vivo OK." -ForegroundColor DarkCyan

# v0.27.5-r6: limpa residuos criados pelos hotfixes r1-r5 empacotados sem a raiz HealthPlatform/.
# O AESYN AUTO v10 trata uma unica pasta de topo como raiz do pacote; por isso "src/" e
# "scripts/" isolados foram copiados um nivel acima do destino correto.
$accidentalSetup = Join-Path $root "setup.ps1"
if (Test-Path -LiteralPath $accidentalSetup) {
    $accidentalSetupText = Get-Content -LiteralPath $accidentalSetup -Raw -ErrorAction SilentlyContinue
    if ($accidentalSetupText -match 'AESYN_SETUP_DIAGNOSTIC|AESYN setup diagnostic v0\.27\.5-r[45]') {
        Remove-Item -LiteralPath $accidentalSetup -Force
        Write-Host "[Limpeza] setup.ps1 residual de hotfix removido da raiz." -ForegroundColor Yellow
    }
}

$accidentalApiRoot = Join-Path $root "HealthPlatform.Api"
$expectedApiRoot = Join-Path $root "src\HealthPlatform.Api"
if ((Test-Path -LiteralPath $accidentalApiRoot) -and (Test-Path -LiteralPath $expectedApiRoot)) {
    $accidentalProject = Join-Path $accidentalApiRoot "HealthPlatform.Api.csproj"
    $knownMisplacedService = Join-Path $accidentalApiRoot "Services\WorkoutIntelligenceService.cs"
    $knownMisplacedContract = Join-Path $accidentalApiRoot "Contracts\WorkoutIntelligence\WorkoutIntelligenceContracts.cs"
    if (-not (Test-Path -LiteralPath $accidentalProject) -and
        ((Test-Path -LiteralPath $knownMisplacedService) -or (Test-Path -LiteralPath $knownMisplacedContract))) {
        Remove-Item -LiteralPath $accidentalApiRoot -Recurse -Force
        Write-Host "[Limpeza] HealthPlatform.Api residual fora de src removido." -ForegroundColor Yellow
    }
}

# AESYN Product Governance: documentacao viva faz parte da entrega.
function Assert-AesynLivingDocs {
    $required = @("README.md", "ROADMAP.md", "CHANGELOG.md", "VERSION.txt", "TESTAR.ps1", "RODAR.ps1")
    foreach ($relative in $required) {
        if (-not (Test-Path (Join-Path $root $relative))) { throw "Governanca AESYN: arquivo obrigatorio ausente: $relative" }
    }

    $readme = Get-Content (Join-Path $root "README.md") -Encoding UTF8 -Raw
    $roadmap = Get-Content (Join-Path $root "ROADMAP.md") -Encoding UTF8 -Raw
    foreach ($token in @("North Star", "Documentação viva obrigatória", "Professional Action Center 2.0")) {
        if (-not $readme.Contains($token)) { throw "Governanca AESYN: README desatualizado; token ausente: $token" }
    }
    foreach ($token in @("Direção mestre definida em 2026-09-29", "AESYN Explore", "v0.20.10 — Period Comparison 2.0", "AESYN 1.0")) {
        if (-not $roadmap.Contains($token)) { throw "Governanca AESYN: ROADMAP desatualizado; token ausente: $token" }
    }

    Write-Host "[Docs] README + ROADMAP + CHANGELOG + scripts: OK" -ForegroundColor Green
}

$versionAesyn = (Get-Content (Join-Path $root "VERSION.txt") -Encoding UTF8 -Raw).Trim()
Write-Host ""
Write-Host "============================================================" -ForegroundColor DarkGray
Write-Host "                         AESYN" -ForegroundColor Cyan
Write-Host "            Athlete & Human Performance" -ForegroundColor DarkCyan
Write-Host "============================================================" -ForegroundColor DarkGray
Write-Host (" Versao        : {0}" -f $versionAesyn) -ForegroundColor White
Write-Host " Etapa         : PREPARAR / Quality Gate" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor DarkGray
Write-Host ""
Assert-AesynLivingDocs


# Hotfix v0.19.24-r3: pacotes extraidos por sobreposicao no Windows nao
# removem arquivos que deixaram de existir no ZIP. Como Render foi aposentado,
# limpamos explicitamente apenas os artefatos legados conhecidos antes do build.
function Remove-LegacyRenderArtifacts {
    $legacy = @(
        "render.yaml",
        "DEPLOY-RENDER-MVP.md",
        "TESTAR-RENDER.ps1",
        "POPULAR-REMOTO.ps1",
        "POPULAR-REMOTO-RICO.ps1"
    )

    $removed = @()
    foreach ($relative in $legacy) {
        $path = Join-Path $root $relative
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
            $removed += $relative
        }
    }

    if ($removed.Count -gt 0) {
        Write-Host "[Limpeza] Artefatos legados do Render removidos: $($removed -join ', ')" -ForegroundColor Yellow
    } else {
        Write-Host "[Limpeza] Nenhum artefato legado do Render encontrado." -ForegroundColor DarkGray
    }
}

Remove-LegacyRenderArtifacts

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

# Hotfix v0.10.2-r2: protege contra extracao/mesclagem que preserve os
# controllers corrompidos da r0. Antes do build, valida o cabecalho dos dois
# arquivos e restaura copias canonicas incluidas no proprio pacote se necessario.
function Repair-V0102ControllersIfNeeded {
    $pairs = @(
        @{
            Target = Join-Path $root "src\HealthPlatform.Api\Controllers\PortalPacienteController.cs"
            Clean  = Join-Path $root "scripts\recovery\PortalPacienteController.cs.clean"
            First  = "using HealthPlatform.Api.Contracts.Portal;"
        },
        @{
            Target = Join-Path $root "src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs"
            Clean  = Join-Path $root "scripts\recovery\MeuPortalPacienteController.cs.clean"
            First  = "using System.Text.Json;"
        }
    )

    foreach ($pair in $pairs) {
        if (-not (Test-Path $pair.Target)) { throw "Arquivo esperado ausente: $($pair.Target)" }
        if (-not (Test-Path $pair.Clean)) { throw "Copia de recuperacao ausente: $($pair.Clean)" }

        $firstLine = Get-Content -LiteralPath $pair.Target -TotalCount 1
        if ($firstLine -ne $pair.First) {
            Write-Host "[Fonte] Controller antigo/corrompido detectado. Restaurando $([IO.Path]::GetFileName($pair.Target))..." -ForegroundColor Yellow
            Copy-Item -LiteralPath $pair.Clean -Destination $pair.Target -Force
            $firstLine = Get-Content -LiteralPath $pair.Target -TotalCount 1
            if ($firstLine -ne $pair.First) {
                throw "Falha ao restaurar controller: $($pair.Target)"
            }
        }
    }
}

Repair-V0102ControllersIfNeeded

# Hotfix v0.11.0-r2: algumas extracoes por sobreposicao no Windows podem
# preservar uma copia antiga do PainelMedicinaEsporteService.cs. Como a suite
# valida guardrails clinicos diretamente na fonte, garantimos que o arquivo
# local contenha as travas canonicas antes do build/teste.
function Repair-V0110PainelMedicinaEsporteIfNeeded {
    $target = Join-Path $root "src\HealthPlatform.Api\Services\PainelMedicinaEsporteService.cs"
    $clean  = Join-Path $root "scripts\recovery\PainelMedicinaEsporteService.cs.clean"

    if (-not (Test-Path $target)) { throw "Arquivo esperado ausente: $target" }
    if (-not (Test-Path $clean))  { throw "Copia de recuperacao ausente: $clean" }

    $source = Get-Content -LiteralPath $target -Encoding UTF8 -Raw
    $required = @('nao produzir diagnostico', 'previsao de lesao', 'prescricao automatica')
    $missing = @($required | Where-Object { -not $source.Contains($_) })

    if ($missing.Count -gt 0) {
        Write-Host "[Fonte] PainelMedicinaEsporteService antigo detectado. Restaurando copia canonica v0.11.0..." -ForegroundColor Yellow
        Copy-Item -LiteralPath $clean -Destination $target -Force
        $source = Get-Content -LiteralPath $target -Encoding UTF8 -Raw
        $missing = @($required | Where-Object { -not $source.Contains($_) })
        if ($missing.Count -gt 0) {
            throw "Falha ao restaurar guardrails clinicos do PainelMedicinaEsporteService: $($missing -join ', ')"
        }
    }
}

Repair-V0110PainelMedicinaEsporteIfNeeded

# Hotfix v0.20.4-r1: extracoes por sobreposicao podem deixar o controller
# de notas internas novo junto de um AppDbContext antigo. Antes do build,
# garantimos o DbSet e o mapeamento EF canonicos da v0.20.4.
function Repair-V0204NotasInternasDbContextIfNeeded {
    $target = Join-Path $root "src\HealthPlatform.Infrastructure\Data\AppDbContext.cs"
    $clean  = Join-Path $root "scripts\recovery\AppDbContext.v0204.clean"

    if (-not (Test-Path $target)) { throw "Arquivo esperado ausente: $target" }
    if (-not (Test-Path $clean))  { throw "Copia de recuperacao ausente: $clean" }

    $source = Get-Content -LiteralPath $target -Encoding UTF8 -Raw
    $required = @(
        'DbSet<NotaInternaProfissional> NotasInternasProfissionais',
        'builder.Entity<NotaInternaProfissional>(entity =>'
    )
    $missing = @($required | Where-Object { -not $source.Contains($_) })

    if ($missing.Count -gt 0) {
        Write-Host "[Fonte] AppDbContext anterior a v0.20.4 detectado. Restaurando contexto canonico..." -ForegroundColor Yellow
        Copy-Item -LiteralPath $clean -Destination $target -Force
        $source = Get-Content -LiteralPath $target -Encoding UTF8 -Raw
        $missing = @($required | Where-Object { -not $source.Contains($_) })
        if ($missing.Count -gt 0) {
            throw "Falha ao restaurar suporte EF de notas internas: $($missing -join ', ')"
        }
    }
}

Repair-V0204NotasInternasDbContextIfNeeded

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
