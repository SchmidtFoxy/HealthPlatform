param(
    [string]$TargetVersion = "",
    [string]$VpsHost = "",
    [string]$VpsUser = "",
    [string]$RemoteRoot = "/opt/healthplatform",
    [string]$ComposeFile = "compose.production.yml",
    [string]$PostgresService = "postgres",
    [string]$DatabaseName = "",
    [string]$BackupDir = "/opt/healthplatform/backups",
    [switch]$ValidarSomente,
    [switch]$Aplicar
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$VersionFile = Join-Path $ScriptRoot "VERSION.txt"
$DeployLogDir = Join-Path $ScriptRoot ".deploy-logs"
$script:BackupConcluido = $false
$script:BackupValidado = $false
$script:MutacaoLiberada = $false

function Write-DeployTitle([string]$Text) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor DarkGray
    Write-Host (" AESYN DEPLOY PRODUCAO v0.57.0 | " + $Text) -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor DarkGray
}

function Assert-Command([string]$Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) { throw "Comando obrigatorio nao encontrado: $Name" }
    return $cmd.Source
}

function Get-LocalVersion {
    if (-not (Test-Path -LiteralPath $VersionFile)) {
        throw "VERSION.txt ausente em $VersionFile"
    }

    $value = (Get-Content -LiteralPath $VersionFile -Raw -Encoding UTF8).Trim()
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "VERSION.txt vazio."
    }

    return $value
}

function Assert-TargetVersion([string]$LocalVersion) {
    if ([string]::IsNullOrWhiteSpace($TargetVersion)) {
        $script:TargetVersion = $LocalVersion
    }

    if ($TargetVersion -ne $LocalVersion) {
        throw "Versao alvo '$TargetVersion' difere da versao local '$LocalVersion'."
    }

    Write-Host ("Versao local/alvo confirmada: v" + $LocalVersion) -ForegroundColor Green
}

function Assert-DeployArguments {
    if ([string]::IsNullOrWhiteSpace($VpsHost)) {
        throw "Informe -VpsHost. Nenhum host de producao e assumido automaticamente."
    }
    if ([string]::IsNullOrWhiteSpace($VpsUser)) {
        throw "Informe -VpsUser. Nenhum usuario de producao e assumido automaticamente."
    }
    if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
        throw "Informe -DatabaseName para que o backup PostgreSQL seja inequivoco."
    }
    if ($RemoteRoot -notmatch '^/') {
        throw "RemoteRoot deve ser um caminho absoluto Linux."
    }
}

function Invoke-SshChecked {
    param(
        [Parameter(Mandatory=$true)][string]$Command,
        [switch]$Capture
    )

    $ssh = Assert-Command "ssh"
    $target = "$VpsUser@$VpsHost"

    if ($Capture) {
        $output = & $ssh $target $Command 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "SSH falhou: $Command`n$($output -join "`n")"
        }
        return ($output -join "`n").Trim()
    }

    & $ssh $target $Command
    if ($LASTEXITCODE -ne 0) {
        throw "SSH falhou com codigo $LASTEXITCODE: $Command"
    }
}

function Assert-RemotePreflight {
    Write-DeployTitle "PREFLIGHT REMOTO"

    $remoteCheck = @"
set -e
test -d '$RemoteRoot'
test -f '$RemoteRoot/$ComposeFile'
command -v docker >/dev/null
docker compose version >/dev/null
docker compose -f '$RemoteRoot/$ComposeFile' ps >/dev/null
printf 'REMOTE_PREFLIGHT_OK'
"@

    $result = Invoke-SshChecked -Command $remoteCheck -Capture
    if ($result -notmatch 'REMOTE_PREFLIGHT_OK') {
        throw "Preflight remoto nao confirmou REMOTE_PREFLIGHT_OK."
    }

    Write-Host "VPS, Docker Compose e arquivo de producao validados." -ForegroundColor Green
}

function New-RemotePostgresBackup {
    Write-DeployTitle "BACKUP POSTGRESQL"

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupFile = "$BackupDir/healthplatform-$TargetVersion-$timestamp.dump"

    $backupCommand = @"
set -e
mkdir -p '$BackupDir'
cd '$RemoteRoot'
docker compose -f '$ComposeFile' exec -T '$PostgresService' pg_dump -Fc -d '$DatabaseName' > '$backupFile'
test -s '$backupFile'
printf '%s' '$backupFile'
"@

    $result = Invoke-SshChecked -Command $backupCommand -Capture
    if ([string]::IsNullOrWhiteSpace($result)) {
        throw "Backup PostgreSQL nao retornou caminho."
    }

    $script:BackupConcluido = $true
    Write-Host ("Backup criado: " + $result) -ForegroundColor Green
    return $result.Trim()
}

function Assert-RemoteBackupIntegrity([string]$BackupFile) {
    if (-not $script:BackupConcluido) {
        throw "Validacao de backup solicitada antes da criacao do backup."
    }

    Write-DeployTitle "VALIDACAO DO BACKUP"

    $validateCommand = @"
set -e
test -s '$BackupFile'
cd '$RemoteRoot'
docker compose -f '$ComposeFile' exec -T '$PostgresService' pg_restore --list < '$BackupFile' >/dev/null
bytes=`$(wc -c < '$BackupFile')
test "`$bytes" -gt 0
printf 'BACKUP_VALIDADO:%s' "`$bytes"
"@

    $result = Invoke-SshChecked -Command $validateCommand -Capture
    if ($result -notmatch '^BACKUP_VALIDADO:\d+$') {
        throw "Backup nao passou pela validacao de integridade."
    }

    $script:BackupValidado = $true
    Write-Host ("Integridade confirmada: " + $result) -ForegroundColor Green
}

function Assert-MutationGuard {
    if (-not $script:BackupConcluido -or -not $script:BackupValidado) {
        throw "BLOQUEIO DE SEGURANCA: nenhuma migration ou substituicao da aplicacao pode ocorrer antes de backup PostgreSQL concluido e validado."
    }

    $script:MutacaoLiberada = $true
}

function Show-FoundationBoundary {
    Write-DeployTitle "FOUNDATION"

    Write-Host "v0.57.0 conclui apenas a fundacao segura de deploy." -ForegroundColor Cyan
    Write-Host "Esta versao NAO executa migration, upload, substituicao da aplicacao, restart ou rollback." -ForegroundColor Yellow
    Write-Host "Essas etapas entram nas proximas entregas da serie v0.57.x." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Garantia ativa:" -ForegroundColor Cyan
    Write-Host "  nenhuma migration ou substituicao da aplicacao antes de backup PostgreSQL concluido e validado." -ForegroundColor Green
}

# -------------------------------------------------------------------------
# Fluxo v0.57.0
# -------------------------------------------------------------------------
Write-DeployTitle "INICIO"

$localVersion = Get-LocalVersion
Assert-TargetVersion -LocalVersion $localVersion

if ($Aplicar -and $ValidarSomente) {
    throw "Use apenas um modo: -ValidarSomente ou -Aplicar."
}

# O modo Aplicar existe desde a fundacao para tornar a intencao explicita,
# mas a v0.57.0 ainda nao executa mutacoes de producao.
if (-not $ValidarSomente -and -not $Aplicar) {
    $ValidarSomente = $true
}

Assert-DeployArguments
[void](Assert-Command "ssh")
Assert-RemotePreflight

$backupFile = New-RemotePostgresBackup
Assert-RemoteBackupIntegrity -BackupFile $backupFile
Assert-MutationGuard

if (-not $script:MutacaoLiberada) {
    throw "Guard de mutacao nao foi liberado apos backup validado."
}

Show-FoundationBoundary

New-Item -ItemType Directory -Path $DeployLogDir -Force | Out-Null
$logFile = Join-Path $DeployLogDir ("deploy-foundation-v" + $localVersion + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
@(
    "version=v$localVersion",
    "target=$VpsUser@$VpsHost",
    "remoteRoot=$RemoteRoot",
    "composeFile=$ComposeFile",
    "database=$DatabaseName",
    "backup=$backupFile",
    "backupValidated=true",
    "mutationGuard=true",
    "foundationOnly=true",
    "completedAt=" + (Get-Date).ToString("o")
) | Set-Content -LiteralPath $logFile -Encoding UTF8

Write-Host ""
Write-Host ("Fundacao de deploy seguro validada. Log local: " + $logFile) -ForegroundColor Green
Write-Host "Nenhuma migration ou substituicao da aplicacao foi executada." -ForegroundColor Green
