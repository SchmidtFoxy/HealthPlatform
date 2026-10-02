param(
    [string]$TargetVersion = "",
    [string]$VpsHost = "",
    [string]$VpsUser = "",
    [string]$RemoteRoot = "/opt/healthplatform",
    [string]$ComposeFile = "compose.production.yml",
    [string]$PostgresService = "postgres",
    [string]$DatabaseName = "",
    [string]$BackupDir = "/opt/healthplatform/backups",
    [string]$ReleaseStagingDir = "/opt/healthplatform/releases",
    [string]$HealthUrl = "",
    [int]$BackupRetentionCount = 7,
    [string]$ProductionConfirmation = "",
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
$script:RemoteMetadata = $null
$script:BackupMetadata = $null
$script:PackageMetadata = $null
$script:StagingMetadata = $null
$script:PreActivationMetadata = $null
$script:RollbackPlan = $null

function Write-DeployTitle([string]$Text) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor DarkGray
    Write-Host (" AESYN DEPLOY PRODUCAO v0.57.3 | " + $Text) -ForegroundColor Cyan
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
    if ($BackupRetentionCount -lt 2) {
        throw "BackupRetentionCount deve ser pelo menos 2 para preservar margem de seguranca."
    }
    if ($ReleaseStagingDir -notmatch '^/') {
        throw "ReleaseStagingDir deve ser um caminho absoluto Linux."
    }
    if ($ReleaseStagingDir -eq $RemoteRoot) {
        throw "ReleaseStagingDir nao pode ser igual ao RemoteRoot ativo."
    }
    if ([string]::IsNullOrWhiteSpace($HealthUrl)) {
        throw "Informe -HealthUrl para validar a saude da aplicacao ativa antes da promocao."
    }
    if ($HealthUrl -notmatch '^https?://') {
        throw "HealthUrl deve usar http:// ou https://."
    }

    $expectedConfirmation = "PRODUCAO:$VpsHost:$TargetVersion"
    if ($ProductionConfirmation -ne $expectedConfirmation) {
        throw "Confirmacao de producao invalida. Informe -ProductionConfirmation '$expectedConfirmation'."
    }

    Write-Host ("Alvo de producao confirmado explicitamente: " + $VpsUser + "@" + $VpsHost + " / v" + $TargetVersion) -ForegroundColor Green
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

function Get-RemoteEnvironmentMetadata {
    Write-DeployTitle "METADADOS REMOTOS"

    $metadataCommand = @"
set -e
hostname_value=`$(hostname)
kernel_value=`$(uname -sr)
docker_value=`$(docker --version | tr '\n' ' ')
compose_value=`$(docker compose version | tr '\n' ' ')
disk_value=`$(df -Pk '$RemoteRoot' | awk 'NR==2 {print `$4}')
utc_value=`$(date -u +%Y-%m-%dT%H:%M:%SZ)
printf 'HOSTNAME=%s\nKERNEL=%s\nDOCKER=%s\nCOMPOSE=%s\nDISK_FREE_KB=%s\nUTC=%s\n' "`$hostname_value" "`$kernel_value" "`$docker_value" "`$compose_value" "`$disk_value" "`$utc_value"
"@

    $raw = Invoke-SshChecked -Command $metadataCommand -Capture
    $map = @{}
    foreach ($line in ($raw -split "`r?`n")) {
        if ($line -match '^(?<key>[A-Z_]+)=(?<value>.*)$') {
            $map[$Matches['key']] = $Matches['value']
        }
    }

    foreach ($required in @('HOSTNAME','KERNEL','DOCKER','COMPOSE','DISK_FREE_KB','UTC')) {
        if (-not $map.ContainsKey($required) -or [string]::IsNullOrWhiteSpace([string]$map[$required])) {
            throw "Metadado remoto obrigatorio ausente: $required"
        }
    }

    if ([int64]$map['DISK_FREE_KB'] -le 0) {
        throw "Espaco livre remoto invalido."
    }

    $script:RemoteMetadata = [pscustomobject]@{
        Hostname = [string]$map['HOSTNAME']
        Kernel = [string]$map['KERNEL']
        Docker = [string]$map['DOCKER']
        Compose = [string]$map['COMPOSE']
        DiskFreeKb = [int64]$map['DISK_FREE_KB']
        Utc = [string]$map['UTC']
    }

    Write-Host ("Host remoto: " + $script:RemoteMetadata.Hostname) -ForegroundColor Green
    Write-Host ("Espaco livre: " + $script:RemoteMetadata.DiskFreeKb + " KB") -ForegroundColor Green
    return $script:RemoteMetadata
}

function Get-RemoteBackupMetadata([string]$BackupFile) {
    if (-not $script:BackupValidado) {
        throw "Metadados do backup so podem ser coletados apos validacao de integridade."
    }

    Write-DeployTitle "METADADOS DO BACKUP"

    $metadataCommand = @"
set -e
test -s '$BackupFile'
bytes=`$(wc -c < '$BackupFile')
sha=`$(sha256sum '$BackupFile' | awk '{print `$1}')
entries=`$(cd '$RemoteRoot' && docker compose -f '$ComposeFile' exec -T '$PostgresService' pg_restore --list < '$BackupFile' | wc -l)
modified=`$(date -u -r '$BackupFile' +%Y-%m-%dT%H:%M:%SZ)
printf 'BYTES=%s\nSHA256=%s\nENTRIES=%s\nMODIFIED_UTC=%s\n' "`$bytes" "`$sha" "`$entries" "`$modified"
"@

    $raw = Invoke-SshChecked -Command $metadataCommand -Capture
    $map = @{}
    foreach ($line in ($raw -split "`r?`n")) {
        if ($line -match '^(?<key>[A-Z0-9_]+)=(?<value>.*)$') {
            $map[$Matches['key']] = $Matches['value']
        }
    }

    foreach ($required in @('BYTES','SHA256','ENTRIES','MODIFIED_UTC')) {
        if (-not $map.ContainsKey($required) -or [string]::IsNullOrWhiteSpace([string]$map[$required])) {
            throw "Metadado de backup obrigatorio ausente: $required"
        }
    }

    if ([int64]$map['BYTES'] -le 0) { throw "Backup possui tamanho invalido." }
    if ([int64]$map['ENTRIES'] -le 0) { throw "Backup nao possui entradas restauraveis listadas." }
    if ([string]$map['SHA256'] -notmatch '^[a-fA-F0-9]{64}$') { throw "SHA256 do backup invalido." }

    $script:BackupMetadata = [pscustomobject]@{
        File = $BackupFile
        Bytes = [int64]$map['BYTES']
        Sha256 = ([string]$map['SHA256']).ToLowerInvariant()
        Entries = [int64]$map['ENTRIES']
        ModifiedUtc = [string]$map['MODIFIED_UTC']
    }

    Write-Host ("Backup bytes: " + $script:BackupMetadata.Bytes) -ForegroundColor Green
    Write-Host ("Backup SHA256: " + $script:BackupMetadata.Sha256) -ForegroundColor Green
    Write-Host ("Entradas pg_restore: " + $script:BackupMetadata.Entries) -ForegroundColor Green
    return $script:BackupMetadata
}

function Invoke-SafeBackupRetention {
    if (-not $script:BackupConcluido -or -not $script:BackupValidado -or -not $script:BackupMetadata) {
        throw "Retencao bloqueada: backup atual ainda nao foi concluido, validado e catalogado."
    }

    Write-DeployTitle "RETENCAO SEGURA"

    $retentionCommand = @"
set -e
mkdir -p '$BackupDir'
current='$($script:BackupMetadata.File)'
keep='$BackupRetentionCount'
test -s "`$current"
files=`$(find '$BackupDir' -maxdepth 1 -type f -name 'healthplatform-*.dump' -printf '%T@ %p\n' | sort -nr | awk '{print `$2}')
count=0
removed=0
for file in `$files; do
  count=`$((count+1))
  if [ "`$count" -le "`$keep" ]; then
    continue
  fi
  if [ "`$file" = "`$current" ]; then
    continue
  fi
  rm -- "`$file"
  removed=`$((removed+1))
done
printf 'RETENTION_OK:kept=%s:removed=%s' "`$keep" "`$removed"
"@

    $result = Invoke-SshChecked -Command $retentionCommand -Capture
    if ($result -notmatch '^RETENTION_OK:kept=\d+:removed=\d+$') {
        throw "Retencao segura nao confirmou resultado esperado."
    }

    Write-Host ("Retencao concluida: " + $result) -ForegroundColor Green
    return $result
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

function New-LocalApplicationPackage {
    if (-not $script:MutacaoLiberada) {
        throw "Empacotamento bloqueado: guard de backup ainda nao foi liberado."
    }

    Write-DeployTitle "PACOTE DA APLICACAO"

    $tar = Assert-Command "tar"
    $packageDir = Join-Path $ScriptRoot ".deploy-packages"
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $packageName = "healthplatform-v$TargetVersion-$timestamp.tar.gz"
    $packagePath = Join-Path $packageDir $packageName

    if (Test-Path -LiteralPath $packagePath) {
        Remove-Item -LiteralPath $packagePath -Force
    }

    $tarArgs = @(
        "-czf", $packagePath,
        "--exclude=.git",
        "--exclude=.deploy-logs",
        "--exclude=.deploy-packages",
        "--exclude=.env",
        "--exclude=.env.*",
        "--exclude=backups",
        "-C", $ScriptRoot,
        "."
    )

    & $tar @tarArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao criar pacote local da aplicacao."
    }
    if (-not (Test-Path -LiteralPath $packagePath)) {
        throw "Pacote local nao foi criado."
    }

    $file = Get-Item -LiteralPath $packagePath
    if ($file.Length -le 0) {
        throw "Pacote local foi criado vazio."
    }

    $hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -notmatch '^[a-f0-9]{64}$') {
        throw "SHA256 local do pacote invalido."
    }

    $script:PackageMetadata = [pscustomobject]@{
        Name = $packageName
        Path = $packagePath
        Bytes = [int64]$file.Length
        Sha256 = $hash
        CreatedAt = (Get-Date).ToString("o")
    }

    Write-Host ("Pacote local: " + $script:PackageMetadata.Path) -ForegroundColor Green
    Write-Host ("Pacote SHA256: " + $script:PackageMetadata.Sha256) -ForegroundColor Green
    return $script:PackageMetadata
}

function Send-ApplicationPackageToStaging {
    if (-not $script:BackupConcluido -or -not $script:BackupValidado -or -not $script:MutacaoLiberada) {
        throw "Staging bloqueado: backup PostgreSQL precisa estar concluido e validado antes de qualquer envio."
    }
    if (-not $script:PackageMetadata) {
        throw "Staging bloqueado: pacote local ainda nao foi criado."
    }

    Write-DeployTitle "STAGING REMOTO"

    $scp = Assert-Command "scp"
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $stageName = "staging-v$TargetVersion-$timestamp"
    $stagePath = "$ReleaseStagingDir/$stageName"
    $remotePackage = "$ReleaseStagingDir/$($script:PackageMetadata.Name)"
    $target = "$VpsUser@$VpsHost"

    $prepareCommand = @"
set -e
mkdir -p '$ReleaseStagingDir'
test '$ReleaseStagingDir' != '$RemoteRoot'
rm -rf '$stagePath'
mkdir -p '$stagePath'
printf 'STAGING_PREPARED'
"@

    $prepared = Invoke-SshChecked -Command $prepareCommand -Capture
    if ($prepared -notmatch 'STAGING_PREPARED') {
        throw "Diretorio de staging remoto nao foi preparado."
    }

    & $scp $script:PackageMetadata.Path ($target + ":" + $remotePackage)
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao enviar pacote para staging remoto."
    }

    $validateCommand = @"
set -e
test -s '$remotePackage'
remote_sha=`$(sha256sum '$remotePackage' | awk '{print `$1}')
test "`$remote_sha" = '$($script:PackageMetadata.Sha256)'
tar -xzf '$remotePackage' -C '$stagePath'
test -f '$stagePath/VERSION.txt'
staged_version=`$(tr -d '\r\n ' < '$stagePath/VERSION.txt')
test "`$staged_version" = '$TargetVersion'
test -f '$stagePath/$ComposeFile'
test '$stagePath' != '$RemoteRoot'
printf 'STAGING_VALIDATED:path=%s:sha256=%s:version=%s' '$stagePath' "`$remote_sha" "`$staged_version"
"@

    $validated = Invoke-SshChecked -Command $validateCommand -Capture
    if ($validated -notmatch '^STAGING_VALIDATED:path=(?<path>[^:]+):sha256=(?<sha>[a-f0-9]{64}):version=(?<version>.+)$') {
        throw "Staging remoto nao retornou comprovacao valida."
    }
    if ($Matches['sha'] -ne $script:PackageMetadata.Sha256) {
        throw "SHA256 remoto do staging difere do pacote local."
    }
    if ($Matches['version'] -ne $TargetVersion) {
        throw "VERSION.txt do staging difere da versao alvo."
    }

    $script:StagingMetadata = [pscustomobject]@{
        Path = $Matches['path']
        Package = $remotePackage
        Sha256 = $Matches['sha']
        Version = $Matches['version']
        Activated = $false
    }

    Write-Host ("Staging validado: " + $script:StagingMetadata.Path) -ForegroundColor Green
    Write-Host "Aplicacao ativa permanece intacta; nenhuma troca de release foi executada." -ForegroundColor Green
    return $script:StagingMetadata
}


function Assert-StagingStructureReady {
    if (-not $script:StagingMetadata -or $script:StagingMetadata.Activated) {
        throw "Pre-activation bloqueada: staging valido e nao ativado e obrigatorio."
    }

    Write-DeployTitle "PRE-ACTIVATION STRUCTURE"
    $stagePath = $script:StagingMetadata.Path
    $cmd = @"
set -e
test '$stagePath' != '$RemoteRoot'
test -d '$stagePath'
test -f '$stagePath/VERSION.txt'
test -f '$stagePath/$ComposeFile'
test -f '$stagePath/DEPLOY-PRODUCAO.ps1'
test ! -e '$stagePath/.git'
test ! -e '$stagePath/.env'
test ! -e '$stagePath/.env.production'
staged_version=`$(tr -d '\r\n ' < '$stagePath/VERSION.txt')
test "`$staged_version" = '$TargetVersion'
printf 'STRUCTURE_READY:version=%s' "`$staged_version"
"@
    $result = Invoke-SshChecked -Command $cmd -Capture
    if ($result -notmatch '^STRUCTURE_READY:version=(?<version>.+)$') {
        throw "Estrutura staged nao passou pelos gates pre-ativacao."
    }
    Write-Host ("Estrutura staged pronta para pre-ativacao: v" + $Matches['version']) -ForegroundColor Green
}

function Assert-ProductionConfigurationPreserved {
    if (-not $script:StagingMetadata) {
        throw "Configuracao de producao nao pode ser validada sem staging."
    }

    Write-DeployTitle "CONFIGURACAO PRESERVADA"
    $stagePath = $script:StagingMetadata.Path
    $cmd = @"
set -e
test -f '$RemoteRoot/.env'
test ! -e '$stagePath/.env'
test ! -e '$stagePath/.env.production'
test -f '$RemoteRoot/$ComposeFile'
active_env_sha=`$(sha256sum '$RemoteRoot/.env' | awk '{print `$1}')
test -n "`$active_env_sha"
printf 'CONFIG_PRESERVED:env_sha=%s' "`$active_env_sha"
"@
    $result = Invoke-SshChecked -Command $cmd -Capture
    if ($result -notmatch '^CONFIG_PRESERVED:env_sha=(?<sha>[a-fA-F0-9]{64})$') {
        throw "Configuracao de producao nao foi confirmada como preservada no host."
    }
    Write-Host "Configuracao .env permanece somente no host ativo e nao foi empacotada no staging." -ForegroundColor Green
    return $Matches['sha'].ToLowerInvariant()
}

function New-PreActivationRollbackPlan([string]$BackupFile) {
    if (-not $script:BackupValidado -or -not $script:StagingMetadata) {
        throw "Plano de rollback bloqueado: backup validado e staging sao obrigatorios."
    }

    Write-DeployTitle "PLANO DE ROLLBACK"
    $stagePath = $script:StagingMetadata.Path
    $cmd = @"
set -e
active_version='unknown'
if [ -f '$RemoteRoot/VERSION.txt' ]; then
  active_version=`$(tr -d '\r\n ' < '$RemoteRoot/VERSION.txt')
fi
test -s '$BackupFile'
test -d '$RemoteRoot'
test -d '$stagePath'
plan='$stagePath/ROLLBACK-PLAN.txt'
{
  printf 'active_root=%s\n' '$RemoteRoot'
  printf 'active_version=%s\n' "`$active_version"
  printf 'target_version=%s\n' '$TargetVersion'
  printf 'database_backup=%s\n' '$BackupFile'
  printf 'staging_path=%s\n' '$stagePath'
  printf 'activation_performed=false\n'
} > "`$plan"
test -s "`$plan"
printf 'ROLLBACK_READY:active_version=%s:plan=%s' "`$active_version" "`$plan"
"@
    $result = Invoke-SshChecked -Command $cmd -Capture
    if ($result -notmatch '^ROLLBACK_READY:active_version=(?<version>[^:]+):plan=(?<plan>.+)$') {
        throw "Plano de rollback nao foi materializado."
    }

    $script:RollbackPlan = [pscustomobject]@{
        ActiveVersion = $Matches['version']
        TargetVersion = $TargetVersion
        BackupFile = $BackupFile
        StagingPath = $stagePath
        PlanFile = $Matches['plan']
        ActivationPerformed = $false
    }
    Write-Host ("Plano de rollback preparado: " + $script:RollbackPlan.PlanFile) -ForegroundColor Green
    return $script:RollbackPlan
}

function Test-PreActivationHealthReadiness {
    if (-not $script:StagingMetadata -or -not $script:RollbackPlan) {
        throw "Health readiness bloqueado: staging e plano de rollback sao obrigatorios."
    }

    Write-DeployTitle "HEALTH READINESS"
    $stagePath = $script:StagingMetadata.Path
    $cmd = @"
set -e
command -v curl >/dev/null
curl -fsS --max-time 15 '$HealthUrl' >/dev/null
docker compose --env-file '$RemoteRoot/.env' -f '$stagePath/$ComposeFile' config -q
test '$stagePath' != '$RemoteRoot'
printf 'PREACTIVATION_HEALTH_READY'
"@
    $result = Invoke-SshChecked -Command $cmd -Capture
    if ($result -notmatch 'PREACTIVATION_HEALTH_READY') {
        throw "Health readiness pre-ativacao nao foi confirmado."
    }

    $script:PreActivationMetadata = [pscustomobject]@{
        HealthUrl = $HealthUrl
        ActiveHealthOk = $true
        StagedComposeConfigOk = $true
        RollbackReady = $true
        ActivationAllowed = $false
        CheckedAt = (Get-Date).ToString("o")
    }

    Write-Host "Aplicacao ativa respondeu ao healthcheck e Compose staged passou em config -q." -ForegroundColor Green
    Write-Host "Pre-activation gates aprovados; promocao continua bloqueada nesta versao." -ForegroundColor Green
    return $script:PreActivationMetadata
}

function Show-FoundationBoundary {
    Write-DeployTitle "FOUNDATION"

    Write-Host "v0.57.3 adiciona gates pre-ativacao, configuracao preservada e rollback preparado." -ForegroundColor Cyan
    Write-Host "Esta versao ainda NAO executa migration, upload, substituicao da aplicacao, restart ou rollback." -ForegroundColor Yellow
    Write-Host "Essas etapas entram nas proximas entregas da serie v0.57.x." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Garantia ativa:" -ForegroundColor Cyan
    Write-Host "  nenhuma migration ou substituicao da aplicacao antes de backup PostgreSQL concluido e validado." -ForegroundColor Green
}

# -------------------------------------------------------------------------
# Fluxo v0.57.3
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
$remoteMetadata = Get-RemoteEnvironmentMetadata

$backupFile = New-RemotePostgresBackup
Assert-RemoteBackupIntegrity -BackupFile $backupFile
$backupMetadata = Get-RemoteBackupMetadata -BackupFile $backupFile
Assert-MutationGuard

if (-not $script:MutacaoLiberada) {
    throw "Guard de mutacao nao foi liberado apos backup validado."
}

$retentionResult = Invoke-SafeBackupRetention
$packageMetadata = New-LocalApplicationPackage
$stagingMetadata = Send-ApplicationPackageToStaging
Assert-StagingStructureReady
$productionEnvSha256 = Assert-ProductionConfigurationPreserved
$rollbackPlan = New-PreActivationRollbackPlan -BackupFile $backupFile
$preActivationMetadata = Test-PreActivationHealthReadiness
Show-FoundationBoundary

New-Item -ItemType Directory -Path $DeployLogDir -Force | Out-Null
$logFile = Join-Path $DeployLogDir ("deploy-preactivation-v" + $localVersion + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
@(
    "version=v$localVersion",
    "target=$VpsUser@$VpsHost",
    "remoteRoot=$RemoteRoot",
    "composeFile=$ComposeFile",
    "database=$DatabaseName",
    "backup=$backupFile",
    "backupValidated=true",
    "backupBytes=$($backupMetadata.Bytes)",
    "backupSha256=$($backupMetadata.Sha256)",
    "backupEntries=$($backupMetadata.Entries)",
    "backupModifiedUtc=$($backupMetadata.ModifiedUtc)",
    "remoteHostname=$($remoteMetadata.Hostname)",
    "remoteKernel=$($remoteMetadata.Kernel)",
    "remoteDocker=$($remoteMetadata.Docker)",
    "remoteCompose=$($remoteMetadata.Compose)",
    "remoteDiskFreeKb=$($remoteMetadata.DiskFreeKb)",
    "retention=$retentionResult",
    "packagePath=$($packageMetadata.Path)",
    "packageBytes=$($packageMetadata.Bytes)",
    "packageSha256=$($packageMetadata.Sha256)",
    "stagingPath=$($stagingMetadata.Path)",
    "stagingPackage=$($stagingMetadata.Package)",
    "stagingSha256=$($stagingMetadata.Sha256)",
    "stagingVersion=$($stagingMetadata.Version)",
    "stagingActivated=false",
    "productionEnvSha256=$productionEnvSha256",
    "rollbackPlan=$($rollbackPlan.PlanFile)",
    "rollbackActiveVersion=$($rollbackPlan.ActiveVersion)",
    "rollbackTargetVersion=$($rollbackPlan.TargetVersion)",
    "preActivationHealthUrl=$($preActivationMetadata.HealthUrl)",
    "preActivationActiveHealthOk=$($preActivationMetadata.ActiveHealthOk)",
    "preActivationStagedComposeConfigOk=$($preActivationMetadata.StagedComposeConfigOk)",
    "preActivationRollbackReady=$($preActivationMetadata.RollbackReady)",
    "preActivationActivationAllowed=false",
    "mutationGuard=true",
    "foundationOnly=true",
    "completedAt=" + (Get-Date).ToString("o")
) | Set-Content -LiteralPath $logFile -Encoding UTF8

Write-Host ""
Write-Host ("Pre-activation gates validados sem promocao da release. Log local: " + $logFile) -ForegroundColor Green
Write-Host "Nenhuma migration ou substituicao da aplicacao foi executada." -ForegroundColor Green
