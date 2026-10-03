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
    [string]$ActivationSnapshotDir = "/opt/healthplatform/activation-snapshots",
    [string]$ActiveReleaseLink = "/opt/healthplatform-current",
    [string]$HealthUrl = "",
    [string]$ApplicationService = "",
    [string]$MigrationSafetyConfirmation = "",
    [string]$MigrationRecoveryConfirmation = "",
    [string]$MigrationCommand = "dotnet ef database update",
    [int]$RestartHealthAttempts = 12,
    [int]$RestartHealthDelaySeconds = 5,
    [int]$BackupRetentionCount = 7,
    [string]$ProductionConfirmation = "",
    [string]$ActivationConfirmation = "",
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
$script:ActivationSnapshot = $null
$script:ControlledActivation = $null
$script:PromotionMetadata = $null
$script:RestartVerificationMetadata = $null
$script:MigrationSafetyMetadata = $null
$script:MigrationExecutionMetadata = $null
$script:MigrationFailureRecoveryMetadata = $null
$script:RecoveryAuditMetadata = $null
$script:EndToEndClosureMetadata = $null
$script:ProductionOperationsMetadata = $null

function Write-DeployTitle([string]$Text) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor DarkGray
    Write-Host (" AESYN DEPLOY PRODUCAO v0.58.40 | " + $Text) -ForegroundColor Cyan
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
    if ($ActivationSnapshotDir -notmatch '^/') {
        throw "ActivationSnapshotDir deve ser um caminho absoluto Linux."
    }
    if ($ActivationSnapshotDir -eq $RemoteRoot -or $ActivationSnapshotDir -eq $ReleaseStagingDir) {
        throw "ActivationSnapshotDir deve ser isolado do RemoteRoot e do ReleaseStagingDir."
    }
    if ($ActiveReleaseLink -notmatch '^/') {
        throw "ActiveReleaseLink deve ser um caminho absoluto Linux."
    }
    if ($ActiveReleaseLink -eq $RemoteRoot -or $ActiveReleaseLink -eq $ReleaseStagingDir -or $ActiveReleaseLink -eq $ActivationSnapshotDir) {
        throw "ActiveReleaseLink deve ser isolado dos diretorios de fonte, staging e snapshot."
    }
    if ([string]::IsNullOrWhiteSpace($ApplicationService)) {
        throw "Informe -ApplicationService para identificar inequivocamente o servico da aplicacao no Compose."
    }
    if ($ApplicationService -notmatch '^[A-Za-z0-9._-]+$') {
        throw "ApplicationService contem caracteres invalidos."
    }
    if ($RestartHealthAttempts -lt 1 -or $RestartHealthAttempts -gt 60) {
        throw "RestartHealthAttempts deve ficar entre 1 e 60."
    }
    if ($RestartHealthDelaySeconds -lt 1 -or $RestartHealthDelaySeconds -gt 60) {
        throw "RestartHealthDelaySeconds deve ficar entre 1 e 60."
    }

    $expectedMigrationSafetyConfirmation = "VALIDAR-MIGRATIONS:$VpsHost:$TargetVersion"
    if ($MigrationSafetyConfirmation -ne $expectedMigrationSafetyConfirmation) {
        throw "Confirmacao de seguranca de migrations invalida. Informe -MigrationSafetyConfirmation '$expectedMigrationSafetyConfirmation'."
    }
    if ([string]::IsNullOrWhiteSpace($MigrationCommand)) {
        throw "MigrationCommand nao pode ser vazio."
    }
    if ($MigrationCommand -match '[
]') {
        throw "MigrationCommand deve ser uma unica linha."
    }

    if (-not [string]::IsNullOrWhiteSpace($MigrationRecoveryConfirmation) -and $MigrationRecoveryConfirmation -notmatch '^RESTORE-BACKUP:.+:.+$') {
        throw "MigrationRecoveryConfirmation possui formato invalido."
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

function Assert-ControlledActivationConfirmation {
    if (-not $script:PreActivationMetadata -or -not $script:PreActivationMetadata.ActiveHealthOk -or -not $script:PreActivationMetadata.RollbackReady) {
        throw "Ativacao controlada bloqueada: gates pre-ativacao ainda nao estao aprovados."
    }

    $expectedActivationConfirmation = "ATIVAR:$VpsHost:$TargetVersion"
    if ($ActivationConfirmation -ne $expectedActivationConfirmation) {
        throw "Confirmacao de ativacao invalida. Informe -ActivationConfirmation '$expectedActivationConfirmation'."
    }

    Write-Host ("Confirmacao adicional de ativacao aceita para v" + $TargetVersion + ".") -ForegroundColor Green
}

function New-ActiveStateSnapshot {
    if (-not $script:BackupValidado -or -not $script:StagingMetadata -or -not $script:RollbackPlan) {
        throw "Snapshot ativo bloqueado: backup, staging e rollback sao obrigatorios."
    }

    Write-DeployTitle "SNAPSHOT DO ESTADO ATIVO"

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $snapshotPath = "$ActivationSnapshotDir/active-before-v$TargetVersion-$timestamp"
    $snapshotCommand = @"
set -e
mkdir -p '$ActivationSnapshotDir'
test '$ActivationSnapshotDir' != '$RemoteRoot'
test '$ActivationSnapshotDir' != '$ReleaseStagingDir'
test -d '$RemoteRoot'
mkdir -p '$snapshotPath'
if [ -f '$RemoteRoot/VERSION.txt' ]; then cp '$RemoteRoot/VERSION.txt' '$snapshotPath/VERSION.txt'; fi
if [ -f '$RemoteRoot/$ComposeFile' ]; then cp '$RemoteRoot/$ComposeFile' '$snapshotPath/$ComposeFile'; fi
if [ -f '$RemoteRoot/.env' ]; then
  env_sha=`$(sha256sum '$RemoteRoot/.env' | awk '{print `$1}')
  printf '%s' "`$env_sha" > '$snapshotPath/ENV-SHA256.txt'
fi
docker compose -f '$RemoteRoot/$ComposeFile' ps > '$snapshotPath/compose-ps.txt'
test -s '$snapshotPath/compose-ps.txt'
printf 'ACTIVE_SNAPSHOT_READY:path=%s' '$snapshotPath'
"@

    $result = Invoke-SshChecked -Command $snapshotCommand -Capture
    if ($result -notmatch '^ACTIVE_SNAPSHOT_READY:path=(?<path>.+)$') {
        throw "Snapshot do estado ativo nao foi confirmado."
    }

    $script:ActivationSnapshot = [pscustomobject]@{
        Path = $Matches['path']
        TargetVersion = $TargetVersion
        DatabaseBackup = $script:RollbackPlan.BackupFile
        CreatedAt = (Get-Date).ToString("o")
    }

    Write-Host ("Snapshot ativo preparado: " + $script:ActivationSnapshot.Path) -ForegroundColor Green
    return $script:ActivationSnapshot
}

function New-ImmediateRollbackCommandPlan {
    if (-not $script:ActivationSnapshot -or -not $script:RollbackPlan -or -not $script:StagingMetadata) {
        throw "Rollback imediato bloqueado: snapshot, rollback base e staging sao obrigatorios."
    }

    Write-DeployTitle "ROLLBACK IMEDIATO PREPARADO"

    $stagePath = $script:StagingMetadata.Path
    $snapshotPath = $script:ActivationSnapshot.Path
    $rollbackCommand = @"
set -e
plan='$stagePath/IMMEDIATE-ROLLBACK.txt'
{
  printf 'active_root=%s\n' '$RemoteRoot'
  printf 'snapshot_path=%s\n' '$snapshotPath'
  printf 'database_backup=%s\n' '$($script:RollbackPlan.BackupFile)'
  printf 'target_version=%s\n' '$TargetVersion'
  printf 'restore_database_allowed=false\n'
  printf 'activation_executed=false\n'
} > "`$plan"
test -s "`$plan"
printf 'IMMEDIATE_ROLLBACK_READY:plan=%s' "`$plan"
"@

    $result = Invoke-SshChecked -Command $rollbackCommand -Capture
    if ($result -notmatch '^IMMEDIATE_ROLLBACK_READY:plan=(?<plan>.+)$') {
        throw "Plano de rollback imediato nao foi preparado."
    }

    Write-Host ("Rollback imediato preparado: " + $Matches['plan']) -ForegroundColor Green
    return $Matches['plan']
}

function Test-ControlledActivationReadiness {
    if (-not $script:ActivationSnapshot -or -not $script:PreActivationMetadata -or -not $script:StagingMetadata) {
        throw "Readiness de ativacao bloqueado: snapshot, pre-activation e staging sao obrigatorios."
    }

    Write-DeployTitle "CONTROLLED ACTIVATION READINESS"

    $stagePath = $script:StagingMetadata.Path
    $snapshotPath = $script:ActivationSnapshot.Path
    $readinessCommand = @"
set -e
test -d '$stagePath'
test -d '$snapshotPath'
test -s '$($script:RollbackPlan.BackupFile)'
test '$stagePath' != '$RemoteRoot'
test '$snapshotPath' != '$RemoteRoot'
staged_version=`$(tr -d '\r\n ' < '$stagePath/VERSION.txt')
test "`$staged_version" = '$TargetVersion'
printf 'CONTROLLED_ACTIVATION_READY:version=%s' "`$staged_version"
"@

    $result = Invoke-SshChecked -Command $readinessCommand -Capture
    if ($result -notmatch '^CONTROLLED_ACTIVATION_READY:version=(?<version>.+)$') {
        throw "Readiness da ativacao controlada nao foi confirmado."
    }

    $script:ControlledActivation = [pscustomobject]@{
        Version = $Matches['version']
        SnapshotReady = $true
        RollbackReady = $true
        BackupReady = $true
        ActivationExecuted = $false
        DestructiveMigrationsAllowed = $false
        CheckedAt = (Get-Date).ToString("o")
    }

    Write-Host "Controlled activation foundation pronta; ativacao real permanece bloqueada nesta versao." -ForegroundColor Green
    Write-Host "Migrations destrutivas continuam proibidas." -ForegroundColor Green
    return $script:ControlledActivation
}

function Test-AtomicPromotionReadiness {
    if (-not $script:ControlledActivation -or -not $script:ActivationSnapshot -or -not $script:StagingMetadata) {
        throw "Promocao atomica bloqueada: controlled activation, snapshot e staging sao obrigatorios."
    }
    if ($script:ControlledActivation.DestructiveMigrationsAllowed) {
        throw "Promocao atomica bloqueada: migrations destrutivas nao podem estar liberadas."
    }

    Write-DeployTitle "ATOMIC PROMOTION READINESS"

    $stagePath = $script:StagingMetadata.Path
    $readinessCommand = @"
set -e
test -d '$stagePath'
test -f '$stagePath/VERSION.txt'
test -f '$stagePath/$ComposeFile'
test '$stagePath' != '$RemoteRoot'
test '$ActiveReleaseLink' != '$RemoteRoot'
test '$ActiveReleaseLink' != '$ReleaseStagingDir'
staged_version=`$(tr -d '\r\n ' < '$stagePath/VERSION.txt')
test "`$staged_version" = '$TargetVersion'
printf 'ATOMIC_PROMOTION_READY:version=%s' "`$staged_version"
"@

    $result = Invoke-SshChecked -Command $readinessCommand -Capture
    if ($result -notmatch '^ATOMIC_PROMOTION_READY:version=(?<version>.+)$') {
        throw "Readiness da promocao atomica nao foi confirmado."
    }

    Write-Host ("Promocao atomica pronta para v" + $Matches['version']) -ForegroundColor Green
}

function Invoke-AtomicReleasePromotion {
    if (-not $Aplicar) {
        $script:PromotionMetadata = [pscustomobject]@{
            Applied = $false
            Version = $TargetVersion
            ActiveReleaseLink = $ActiveReleaseLink
            PreviousRelease = ""
            NewRelease = $script:StagingMetadata.Path
            PostPromotionHealthOk = $false
            AutoRollbackExecuted = $false
            DestructiveMigrationsAllowed = $false
        }
        Write-Host "Modo de validacao: promocao atomica nao executada." -ForegroundColor Yellow
        return $script:PromotionMetadata
    }

    if (-not $script:BackupValidado -or -not $script:ActivationSnapshot -or -not $script:RollbackPlan) {
        throw "Promocao atomica bloqueada: backup validado, snapshot e rollback sao obrigatorios."
    }

    Write-DeployTitle "ATOMIC RELEASE PROMOTION"

    $stagePath = $script:StagingMetadata.Path
    $promotionCommand = @"
set -e
new_release='$stagePath'
active_link='$ActiveReleaseLink'
previous_release='$RemoteRoot'
if [ -L "`$active_link" ]; then
  previous_release=`$(readlink -f "`$active_link")
elif [ -e "`$active_link" ]; then
  echo 'ACTIVE_LINK_NOT_SYMLINK' >&2
  exit 41
fi

test -d "`$new_release"
test -f "`$new_release/VERSION.txt"
test "`$new_release" != '$RemoteRoot'
test "`$active_link" != '$RemoteRoot'

temp_link="`$active_link.next"
rm -f "`$temp_link"
ln -s "`$new_release" "`$temp_link"
mv -Tf "`$temp_link" "`$active_link"

printf 'PROMOTION_SWITCHED:previous=%s:new=%s' "`$previous_release" "`$new_release"
"@

    $switchResult = Invoke-SshChecked -Command $promotionCommand -Capture
    if ($switchResult -notmatch '^PROMOTION_SWITCHED:previous=(?<previous>.*):new=(?<new>.+)$') {
        throw "Troca atomica da release nao retornou comprovacao valida."
    }

    $previousRelease = $Matches['previous']
    $newRelease = $Matches['new']
    $healthOk = $false
    $rollbackExecuted = $false

    try {
        $postHealthCommand = @"
set -e
test -L '$ActiveReleaseLink'
current_release=`$(readlink -f '$ActiveReleaseLink')
test "`$current_release" = '$stagePath'
curl -fsS --max-time 20 '$HealthUrl' >/dev/null
printf 'POST_PROMOTION_HEALTH_OK'
"@
        $healthResult = Invoke-SshChecked -Command $postHealthCommand -Capture
        if ($healthResult -notmatch 'POST_PROMOTION_HEALTH_OK') {
            throw "Healthcheck pos-promocao nao confirmou sucesso."
        }
        $healthOk = $true
    }
    catch {
        if ([string]::IsNullOrWhiteSpace($previousRelease)) {
            throw "Healthcheck pos-promocao falhou e nao existe release anterior para rollback automatico."
        }

        $rollbackCommand = @"
set -e
test -d '$previousRelease'
temp_link='$ActiveReleaseLink.rollback'
rm -f "`$temp_link"
ln -s '$previousRelease' "`$temp_link"
mv -Tf "`$temp_link" '$ActiveReleaseLink'
current_release=`$(readlink -f '$ActiveReleaseLink')
test "`$current_release" = '$previousRelease'
printf 'AUTO_ROLLBACK_OK:release=%s' "`$current_release"
"@
        $rollbackResult = Invoke-SshChecked -Command $rollbackCommand -Capture
        if ($rollbackResult -notmatch '^AUTO_ROLLBACK_OK:release=') {
            throw "Rollback automatico nao foi confirmado apos falha do healthcheck."
        }
        $rollbackExecuted = $true
        throw "Healthcheck pos-promocao falhou. Rollback automatico executado para a release anterior."
    }

    $script:PromotionMetadata = [pscustomobject]@{
        Applied = $true
        Version = $TargetVersion
        ActiveReleaseLink = $ActiveReleaseLink
        PreviousRelease = $previousRelease
        NewRelease = $newRelease
        PostPromotionHealthOk = $healthOk
        AutoRollbackExecuted = $rollbackExecuted
        DestructiveMigrationsAllowed = $false
    }

    Write-Host ("Release promovida atomicamente: " + $newRelease) -ForegroundColor Green
    Write-Host "Healthcheck pos-promocao aprovado." -ForegroundColor Green
    Write-Host "Migrations destrutivas permanecem bloqueadas." -ForegroundColor Green
    return $script:PromotionMetadata
}

function Invoke-ControlledServiceRestartAndVersionVerification {
    if (-not $script:PromotionMetadata) {
        throw "Restart controlado bloqueado: metadados da promocao ausentes."
    }

    if (-not $script:PromotionMetadata.Applied) {
        $script:RestartVerificationMetadata = [pscustomobject]@{
            Executed = $false
            Service = $ApplicationService
            ExpectedVersion = $TargetVersion
            ServedVersion = ""
            HealthVerified = $false
            VersionVerified = $false
            RollbackExecuted = $false
            DestructiveMigrationsAllowed = $false
        }
        Write-Host "Modo de validacao: restart controlado e verificacao de versao nao executados." -ForegroundColor Yellow
        return $script:RestartVerificationMetadata
    }

    if (-not $script:BackupValidado -or -not $script:ActivationSnapshot -or -not $script:PromotionMetadata.PostPromotionHealthOk) {
        throw "Restart controlado bloqueado: backup, snapshot e health pos-promocao sao obrigatorios."
    }

    Write-DeployTitle "SERVICE RESTART & VERSION VERIFICATION"

    $restartCommand = @"
set -e
test -L '$ActiveReleaseLink'
active_release=`$(readlink -f '$ActiveReleaseLink')
test "`$active_release" = '$($script:PromotionMetadata.NewRelease)'
test -f '$ActiveReleaseLink/$ComposeFile'
docker compose --env-file '$RemoteRoot/.env' -f '$ActiveReleaseLink/$ComposeFile' config --services | grep -Fx '$ApplicationService' >/dev/null
docker compose --env-file '$RemoteRoot/.env' -f '$ActiveReleaseLink/$ComposeFile' up -d --build --no-deps --force-recreate '$ApplicationService'
printf 'SERVICE_RESTARTED:%s' '$ApplicationService'
"@

    $restartResult = Invoke-SshChecked -Command $restartCommand -Capture
    if ($restartResult -notmatch '^SERVICE_RESTARTED:') {
        throw "Restart controlado do servico nao foi confirmado."
    }

    $verified = $false
    $servedVersion = ""
    $lastError = ""

    for ($attempt = 1; $attempt -le $RestartHealthAttempts; $attempt++) {
        try {
            $verifyCommand = @"
set -e
body=`$(curl -fsS --max-time 20 '$HealthUrl')
printf '%s' "`$body" | grep -Eq '"version"[[:space:]]*:[[:space:]]*"$TargetVersion"'
printf 'VERSION_VERIFIED:%s' '$TargetVersion'
"@
            $verifyResult = Invoke-SshChecked -Command $verifyCommand -Capture
            if ($verifyResult -match '^VERSION_VERIFIED:(?<version>.+)$') {
                $servedVersion = $Matches['version']
                $verified = $true
                break
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }

        if ($attempt -lt $RestartHealthAttempts) {
            Start-Sleep -Seconds $RestartHealthDelaySeconds
        }
    }

    $rollbackExecuted = $false
    if (-not $verified) {
        $previousRelease = $script:PromotionMetadata.PreviousRelease
        if ([string]::IsNullOrWhiteSpace($previousRelease)) {
            throw "Verificacao de versao falhou e nao existe release anterior para rollback."
        }

        $rollbackCommand = @"
set -e
test -d '$previousRelease'
temp_link='$ActiveReleaseLink.restart-rollback'
rm -f "`$temp_link"
ln -s '$previousRelease' "`$temp_link"
mv -Tf "`$temp_link" '$ActiveReleaseLink'
docker compose --env-file '$RemoteRoot/.env' -f '$ActiveReleaseLink/$ComposeFile' config --services | grep -Fx '$ApplicationService' >/dev/null
docker compose --env-file '$RemoteRoot/.env' -f '$ActiveReleaseLink/$ComposeFile' up -d --build --no-deps --force-recreate '$ApplicationService'
curl -fsS --max-time 20 '$HealthUrl' >/dev/null
printf 'RESTART_VERSION_ROLLBACK_OK:%s' '$previousRelease'
"@
        $rollbackResult = Invoke-SshChecked -Command $rollbackCommand -Capture
        if ($rollbackResult -notmatch '^RESTART_VERSION_ROLLBACK_OK:') {
            throw "Rollback automatico apos falha de restart/versao nao foi confirmado."
        }
        $rollbackExecuted = $true
        throw "Restart ou verificacao de versao falhou. Rollback automatico executado. Ultimo erro: $lastError"
    }

    $script:RestartVerificationMetadata = [pscustomobject]@{
        Executed = $true
        Service = $ApplicationService
        ExpectedVersion = $TargetVersion
        ServedVersion = $servedVersion
        HealthVerified = $true
        VersionVerified = $true
        RollbackExecuted = $rollbackExecuted
        DestructiveMigrationsAllowed = $false
    }

    Write-Host ("Servico reiniciado controladamente: " + $ApplicationService) -ForegroundColor Green
    Write-Host ("Versao servida confirmada: v" + $servedVersion) -ForegroundColor Green
    Write-Host "Migrations destrutivas permanecem bloqueadas." -ForegroundColor Green
    return $script:RestartVerificationMetadata
}

function Test-MigrationSafetyGate {
    if (-not $script:BackupValidado -or -not $script:StagingMetadata) {
        throw "Migration safety gate bloqueado: backup validado e staging sao obrigatorios."
    }

    Write-DeployTitle "MIGRATION SAFETY GATE"

    $stagePath = $script:StagingMetadata.Path
    $migrationCommand = @"
set -e
test -d '$stagePath'
test -s '$($script:RollbackPlan.BackupFile)'

migration_files=`$(find '$stagePath' -type f \( -name '*Migration*.cs' -o -name '*Migrations*.cs' \) | sort || true)
migration_count=0
migration_hash='none'
destructive_hits=0

if [ -n "`$migration_files" ]; then
  migration_count=`$(printf '%s\n' "`$migration_files" | sed '/^$/d' | wc -l)
  migration_hash=`$(printf '%s\n' "`$migration_files" | while IFS= read -r file; do sha256sum "`$file"; done | sha256sum | awk '{print `$1}')

  if grep -E -n \
    'DropTable[[:space:]]*\(|DropColumn[[:space:]]*\(|DropForeignKey[[:space:]]*\(|DropPrimaryKey[[:space:]]*\(|DropIndex[[:space:]]*\(|RenameColumn[[:space:]]*\(|RenameTable[[:space:]]*\(|AlterColumn[[:space:]]*<|DeleteData[[:space:]]*\(|Sql[[:space:]]*\(' \
    `$migration_files >/dev/null 2>&1; then
    destructive_hits=1
  fi
fi

if [ "`$destructive_hits" -ne 0 ]; then
  printf 'MIGRATION_SAFETY_BLOCKED:count=%s:hash=%s' "`$migration_count" "`$migration_hash"
  exit 42
fi

printf 'MIGRATION_SAFETY_OK:count=%s:hash=%s:destructive=0' "`$migration_count" "`$migration_hash"
"@

    $result = Invoke-SshChecked -Command $migrationCommand -Capture
    if ($result -notmatch '^MIGRATION_SAFETY_OK:count=(?<count>\d+):hash=(?<hash>[a-f0-9]{64}|none):destructive=0$') {
        throw "Migration safety gate nao confirmou conjunto nao destrutivo."
    }

    $count = [int]$Matches['count']
    $hash = [string]$Matches['hash']

    $script:MigrationSafetyMetadata = [pscustomobject]@{
        MigrationCount = $count
        MigrationHash = $hash
        DestructiveDetected = $false
        SafeMigrationsAllowed = $true
        DestructiveMigrationsAllowed = $false
        ExecutionPerformed = $false
        CheckedAt = (Get-Date).ToString("o")
    }

    Write-Host ("Migration safety gate aprovado. Arquivos analisados: " + $count) -ForegroundColor Green
    Write-Host ("Migration set hash: " + $hash) -ForegroundColor Green
    Write-Host "Somente migrations classificadas como nao destrutivas podem avancar; nenhuma migration foi executada nesta versao." -ForegroundColor Green
    return $script:MigrationSafetyMetadata
}

function Write-MigrationFailureDiagnostic([string]$FailureMessage) {
    New-Item -ItemType Directory -Path $DeployLogDir -Force | Out-Null
    $diagnosticFile = Join-Path $DeployLogDir ("migration-failure-v" + $TargetVersion + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")

    @(
        "targetVersion=v$TargetVersion",
        "target=$VpsUser@$VpsHost",
        "database=$DatabaseName",
        "backup=$($script:RollbackPlan.BackupFile)",
        "backupValidated=$script:BackupValidado",
        "migrationCount=$($script:MigrationSafetyMetadata.MigrationCount)",
        "migrationApprovedHash=$($script:MigrationSafetyMetadata.MigrationHash)",
        "promotionBlocked=true",
        "restartBlocked=true",
        "failure=" + $FailureMessage,
        "recordedAt=" + (Get-Date).ToString("o")
    ) | Set-Content -LiteralPath $diagnosticFile -Encoding UTF8

    Write-Host ("Diagnostico de falha de migration salvo em: " + $diagnosticFile) -ForegroundColor Yellow
    return $diagnosticFile
}

function Invoke-MigrationFailureRecovery([string]$FailureMessage) {
    if (-not $script:BackupValidado -or -not $script:RollbackPlan) {
        throw "Recovery bloqueado: backup validado e plano de rollback sao obrigatorios."
    }

    $expectedRecoveryConfirmation = "RESTORE-BACKUP:$VpsHost:$TargetVersion"
    if ($MigrationRecoveryConfirmation -ne $expectedRecoveryConfirmation) {
        throw "Recovery bloqueado por confirmacao. Informe -MigrationRecoveryConfirmation '$expectedRecoveryConfirmation'. Backup preservado em $($script:RollbackPlan.BackupFile)."
    }

    Write-DeployTitle "MIGRATION FAILURE RECOVERY"

    $backupFile = $script:RollbackPlan.BackupFile
    $restoreCommand = @"
set -e
test -s '$backupFile'
cd '$RemoteRoot'
docker compose -f '$ComposeFile' exec -T '$PostgresService' pg_restore --list < '$backupFile' >/dev/null
docker compose -f '$ComposeFile' stop '$ApplicationService'
restore_ok=0
if docker compose -f '$ComposeFile' exec -T '$PostgresService' pg_restore --clean --if-exists --no-owner --no-privileges -d '$DatabaseName' < '$backupFile'; then
  restore_ok=1
fi
if [ "`$restore_ok" -ne 1 ]; then
  docker compose -f '$ComposeFile' up -d '$ApplicationService' || true
  exit 43
fi
db_probe=`$(docker compose -f '$ComposeFile' exec -T '$PostgresService' psql -d '$DatabaseName' -Atqc 'SELECT 1')
test "`$db_probe" = '1'
docker compose -f '$ComposeFile' up -d '$ApplicationService'
printf 'MIGRATION_RECOVERY_RESTORED:probe=%s' "`$db_probe"
"@

    $restoreResult = Invoke-SshChecked -Command $restoreCommand -Capture
    if ($restoreResult -notmatch '^MIGRATION_RECOVERY_RESTORED:probe=1$') {
        throw "Restore do backup nao retornou confirmacao valida."
    }

    $healthRecovered = $false
    $lastHealthError = ""
    for ($attempt = 1; $attempt -le $RestartHealthAttempts; $attempt++) {
        try {
            $healthCommand = @"
set -e
curl -fsS --max-time 20 '$HealthUrl' >/dev/null
printf 'RECOVERY_HEALTH_OK'
"@
            $healthResult = Invoke-SshChecked -Command $healthCommand -Capture
            if ($healthResult -match 'RECOVERY_HEALTH_OK') {
                $healthRecovered = $true
                break
            }
        }
        catch {
            $lastHealthError = $_.Exception.Message
        }

        if ($attempt -lt $RestartHealthAttempts) {
            Start-Sleep -Seconds $RestartHealthDelaySeconds
        }
    }

    if (-not $healthRecovered) {
        throw "Backup restaurado, mas healthcheck de recovery falhou. Intervencao manual obrigatoria. Ultimo erro: $lastHealthError"
    }

    $script:MigrationFailureRecoveryMetadata = [pscustomobject]@{
        RecoveryExecuted = $true
        BackupFile = $backupFile
        DatabaseProbeOk = $true
        HealthRecovered = $true
        PromotionBlocked = $true
        RestartOfTargetReleaseBlocked = $true
        FailureMessage = $FailureMessage
        RecoveredAt = (Get-Date).ToString("o")
    }

    Write-Host "Backup PostgreSQL restaurado e banco revalidado apos falha de migration." -ForegroundColor Green
    Write-Host "Promocao da release alvo permanece bloqueada. Uma nova tentativa exige novo ciclo completo." -ForegroundColor Yellow
    return $script:MigrationFailureRecoveryMetadata
}

function Invoke-ApprovedNonDestructiveMigrations {
    if (-not $script:MigrationSafetyMetadata) {
        throw "Execucao de migrations bloqueada: migration safety gate ainda nao foi executado."
    }
    if (-not $script:MigrationSafetyMetadata.SafeMigrationsAllowed -or $script:MigrationSafetyMetadata.DestructiveMigrationsAllowed) {
        throw "Execucao de migrations bloqueada: conjunto nao foi aprovado como exclusivamente nao destrutivo."
    }
    if (-not $script:BackupValidado -or -not $script:RollbackPlan) {
        throw "Execucao de migrations bloqueada: backup validado e plano de rollback sao obrigatorios."
    }

    if (-not $Aplicar) {
        $script:MigrationExecutionMetadata = [pscustomobject]@{
            Executed = $false
            Skipped = $true
            MigrationCount = $script:MigrationSafetyMetadata.MigrationCount
            ApprovedHash = $script:MigrationSafetyMetadata.MigrationHash
            RevalidatedHash = $script:MigrationSafetyMetadata.MigrationHash
            HashMatched = $true
            ExitCode = 0
            DestructiveMigrationsAllowed = $false
        }
        Write-Host "Modo de validacao: migrations nao destrutivas aprovadas, mas nao executadas." -ForegroundColor Yellow
        return $script:MigrationExecutionMetadata
    }

    Write-DeployTitle "NON-DESTRUCTIVE MIGRATION EXECUTION"

    $stagePath = $script:StagingMetadata.Path
    $approvedHash = $script:MigrationSafetyMetadata.MigrationHash
    $approvedCount = $script:MigrationSafetyMetadata.MigrationCount

    $rehashCommand = @"
set -e
migration_files=`$(find '$stagePath' -type f \( -name '*Migration*.cs' -o -name '*Migrations*.cs' \) | sort || true)
migration_count=0
migration_hash='none'
if [ -n "`$migration_files" ]; then
  migration_count=`$(printf '%s\n' "`$migration_files" | sed '/^$/d' | wc -l)
  migration_hash=`$(printf '%s\n' "`$migration_files" | while IFS= read -r file; do sha256sum "`$file"; done | sha256sum | awk '{print `$1}')
fi
test "`$migration_count" -eq '$approvedCount'
test "`$migration_hash" = '$approvedHash'
printf 'MIGRATION_HASH_REVALIDATED:count=%s:hash=%s' "`$migration_count" "`$migration_hash"
"@

    $rehashResult = Invoke-SshChecked -Command $rehashCommand -Capture
    if ($rehashResult -notmatch '^MIGRATION_HASH_REVALIDATED:count=(?<count>\d+):hash=(?<hash>[a-f0-9]{64}|none)$') {
        throw "Revalidacao do conjunto de migrations falhou."
    }
    if ([int]$Matches['count'] -ne $approvedCount -or $Matches['hash'] -ne $approvedHash) {
        throw "Conjunto de migrations mudou apos aprovacao do safety gate."
    }

    if ($approvedCount -eq 0) {
        $script:MigrationExecutionMetadata = [pscustomobject]@{
            Executed = $false
            Skipped = $true
            MigrationCount = 0
            ApprovedHash = $approvedHash
            RevalidatedHash = $approvedHash
            HashMatched = $true
            ExitCode = 0
            DestructiveMigrationsAllowed = $false
        }
        Write-Host "Nenhuma migration staged para executar; etapa concluida como no-op seguro." -ForegroundColor Green
        return $script:MigrationExecutionMetadata
    }

    $escapedMigrationCommand = $MigrationCommand.Replace("'", "'\"'\"'")
    $executionCommand = @"
set -e
test -s '$($script:RollbackPlan.BackupFile)'
test -f '$stagePath/$ComposeFile'
docker compose --env-file '$RemoteRoot/.env' -f '$stagePath/$ComposeFile' config --services | grep -Fx '$ApplicationService' >/dev/null
docker compose --env-file '$RemoteRoot/.env' -f '$stagePath/$ComposeFile' run --rm --no-deps --entrypoint sh '$ApplicationService' -lc '$escapedMigrationCommand'
printf 'NON_DESTRUCTIVE_MIGRATIONS_EXECUTED:count=%s:hash=%s' '$approvedCount' '$approvedHash'
"@

    try {
        $executionResult = Invoke-SshChecked -Command $executionCommand -Capture
    }
    catch {
        $migrationFailureMessage = $_.Exception.Message
        $migrationFailureDiagnostic = Write-MigrationFailureDiagnostic -FailureMessage $migrationFailureMessage

        try {
            $recoveryMetadata = Invoke-MigrationFailureRecovery -FailureMessage $migrationFailureMessage
        }
        catch {
            throw "Execucao de migration nao destrutiva falhou. Promocao/restart bloqueados. Diagnostico: $migrationFailureDiagnostic. Recovery nao concluido: $($_.Exception.Message)"
        }

        throw "Execucao de migration nao destrutiva falhou. Recovery concluido com restore + revalidacao. Promocao/restart permanecem bloqueados. Diagnostico: $migrationFailureDiagnostic"
    }

    if ($executionResult -notmatch '^NON_DESTRUCTIVE_MIGRATIONS_EXECUTED:count=(?<count>\d+):hash=(?<hash>[a-f0-9]{64}|none)$') {
        throw "Execucao de migrations nao retornou comprovacao valida."
    }

    $script:MigrationExecutionMetadata = [pscustomobject]@{
        Executed = $true
        Skipped = $false
        MigrationCount = [int]$Matches['count']
        ApprovedHash = $approvedHash
        RevalidatedHash = $Matches['hash']
        HashMatched = ($Matches['hash'] -eq $approvedHash)
        ExitCode = 0
        DestructiveMigrationsAllowed = $false
    }

    Write-Host ("Migrations nao destrutivas executadas: " + $script:MigrationExecutionMetadata.MigrationCount) -ForegroundColor Green
    Write-Host ("Hash revalidado: " + $script:MigrationExecutionMetadata.RevalidatedHash) -ForegroundColor Green
    Write-Host "Migrations destrutivas permanecem bloqueadas." -ForegroundColor Green
    return $script:MigrationExecutionMetadata
}

function New-RecoveryAuditBundle {
    Write-DeployTitle "RECOVERY AUDIT"

    New-Item -ItemType Directory -Path $DeployLogDir -Force | Out-Null
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $auditDir = Join-Path $DeployLogDir ("recovery-audit-v" + $TargetVersion + "-" + $timestamp)
    New-Item -ItemType Directory -Path $auditDir -Force | Out-Null

    $summaryFile = Join-Path $auditDir "SUMMARY.txt"
    $backupEvidenceFile = Join-Path $auditDir "BACKUP-EVIDENCE.txt"
    $migrationEvidenceFile = Join-Path $auditDir "MIGRATION-EVIDENCE.txt"
    $operatorNextStepsFile = Join-Path $auditDir "OPERATOR-NEXT-STEPS.txt"

    @(
        "targetVersion=v$TargetVersion",
        "target=$VpsUser@$VpsHost",
        "remoteRoot=$RemoteRoot",
        "stagingPath=$($script:StagingMetadata.Path)",
        "activeReleaseLink=$ActiveReleaseLink",
        "backupValidated=$script:BackupValidado",
        "mutationGuard=$script:MutacaoLiberada",
        "recoveryConfirmationProvided=$(-not [string]::IsNullOrWhiteSpace($MigrationRecoveryConfirmation))",
        "destructiveMigrationsAllowed=false",
        "generatedAt=" + (Get-Date).ToString("o")
    ) | Set-Content -LiteralPath $summaryFile -Encoding UTF8

    @(
        "backupFile=$($script:RollbackPlan.BackupFile)",
        "backupBytes=$($script:BackupMetadata.Bytes)",
        "backupSha256=$($script:BackupMetadata.Sha256)",
        "backupEntries=$($script:BackupMetadata.Entries)",
        "backupModifiedUtc=$($script:BackupMetadata.ModifiedUtc)",
        "backupValidated=$script:BackupValidado"
    ) | Set-Content -LiteralPath $backupEvidenceFile -Encoding UTF8

    @(
        "migrationCount=$($script:MigrationSafetyMetadata.MigrationCount)",
        "migrationApprovedHash=$($script:MigrationSafetyMetadata.MigrationHash)",
        "safeMigrationsAllowed=$($script:MigrationSafetyMetadata.SafeMigrationsAllowed)",
        "destructiveDetected=$($script:MigrationSafetyMetadata.DestructiveDetected)",
        "destructiveMigrationsAllowed=false",
        "executionPerformed=$($script:MigrationExecutionMetadata.Executed)",
        "executionSkipped=$($script:MigrationExecutionMetadata.Skipped)",
        "revalidatedHash=$($script:MigrationExecutionMetadata.RevalidatedHash)",
        "hashMatched=$($script:MigrationExecutionMetadata.HashMatched)"
    ) | Set-Content -LiteralPath $migrationEvidenceFile -Encoding UTF8

    @(
        "1. Se houve falha de migration, nao promover a release alvo.",
        "2. Ler o diagnostico migration-failure-v*.log antes de qualquer acao.",
        "3. Confirmar o backup atual, SHA-256 e pg_restore --list.",
        "4. Restore exige RESTORE-BACKUP:<host>:<versao>.",
        "5. Depois do restore, confirmar SELECT 1 e healthcheck.",
        "6. Nao reutilizar o ciclo falho: iniciar novo backup, staging, safety gate e validacoes.",
        "7. Nunca liberar migration destrutiva por este fluxo."
    ) | Set-Content -LiteralPath $operatorNextStepsFile -Encoding UTF8

    foreach ($requiredFile in @($summaryFile,$backupEvidenceFile,$migrationEvidenceFile,$operatorNextStepsFile)) {
        if (-not (Test-Path -LiteralPath $requiredFile)) {
            throw "Recovery audit incompleto: arquivo obrigatorio ausente: $requiredFile"
        }
        if ((Get-Item -LiteralPath $requiredFile).Length -le 0) {
            throw "Recovery audit incompleto: arquivo vazio: $requiredFile"
        }
    }

    $script:RecoveryAuditMetadata = [pscustomobject]@{
        AuditDir = $auditDir
        SummaryFile = $summaryFile
        BackupEvidenceFile = $backupEvidenceFile
        MigrationEvidenceFile = $migrationEvidenceFile
        OperatorNextStepsFile = $operatorNextStepsFile
        Complete = $true
        GeneratedAt = (Get-Date).ToString("o")
    }

    Write-Host ("Recovery audit bundle criado: " + $auditDir) -ForegroundColor Green
    return $script:RecoveryAuditMetadata
}

function Test-EndToEndClosureGate {
    Write-DeployTitle "END-TO-END CLOSURE GATE"

    if (-not $script:BackupConcluido -or -not $script:BackupValidado -or -not $script:BackupMetadata) {
        throw "Closure gate bloqueado: backup PostgreSQL nao esta concluido, validado e catalogado."
    }
    if (-not $script:MutacaoLiberada) {
        throw "Closure gate bloqueado: mutation guard nao foi liberado."
    }
    if (-not $script:StagingMetadata -or [string]::IsNullOrWhiteSpace([string]$script:StagingMetadata.Path)) {
        throw "Closure gate bloqueado: staging metadata ausente."
    }
    if (-not $script:RollbackPlan -or [string]::IsNullOrWhiteSpace([string]$script:RollbackPlan.BackupFile)) {
        throw "Closure gate bloqueado: rollback plan ausente."
    }
    if (-not $script:PreActivationMetadata -or -not $script:PreActivationMetadata.ActiveHealthOk -or -not $script:PreActivationMetadata.StagedComposeConfigOk) {
        throw "Closure gate bloqueado: pre-activation gates incompletos."
    }
    if (-not $script:ActivationSnapshot -or -not $script:ControlledActivation) {
        throw "Closure gate bloqueado: snapshot e controlled activation sao obrigatorios."
    }
    if (-not $script:MigrationSafetyMetadata -or -not $script:MigrationSafetyMetadata.SafeMigrationsAllowed) {
        throw "Closure gate bloqueado: migration safety gate nao aprovado."
    }
    if ($script:MigrationSafetyMetadata.DestructiveMigrationsAllowed -or $script:MigrationSafetyMetadata.DestructiveDetected) {
        throw "Closure gate bloqueado: migration destrutiva detectada ou liberada."
    }
    if (-not $script:MigrationExecutionMetadata -or -not $script:MigrationExecutionMetadata.HashMatched) {
        throw "Closure gate bloqueado: execucao/revalidacao de migrations incompleta."
    }
    if (-not $script:PromotionMetadata -or -not $script:RestartVerificationMetadata) {
        throw "Closure gate bloqueado: promotion/restart metadata ausentes."
    }
    if (-not $script:RecoveryAuditMetadata -or -not $script:RecoveryAuditMetadata.Complete) {
        throw "Closure gate bloqueado: recovery audit bundle incompleto."
    }

    if ($Aplicar) {
        if (-not $script:PromotionMetadata.Applied -or -not $script:PromotionMetadata.PostPromotionHealthOk) {
            throw "Closure gate bloqueado: promocao aplicada sem comprovacao de health."
        }
        if (-not $script:RestartVerificationMetadata.Executed -or -not $script:RestartVerificationMetadata.HealthVerified -or -not $script:RestartVerificationMetadata.VersionVerified) {
            throw "Closure gate bloqueado: restart/version verification aplicada incompleta."
        }
        if ($script:RestartVerificationMetadata.ServedVersion -ne $TargetVersion) {
            throw "Closure gate bloqueado: versao servida diverge da TargetVersion."
        }
    }
    else {
        if ($script:PromotionMetadata.Applied -or $script:RestartVerificationMetadata.Executed) {
            throw "Closure gate bloqueado: modo validacao nao pode aplicar promocao/restart."
        }
    }

    $closureFile = Join-Path $script:RecoveryAuditMetadata.AuditDir "END-TO-END-CLOSURE.txt"
    @(
        "version=v$TargetVersion",
        "mode=" + $(if ($Aplicar) { "apply" } else { "validate-only" }),
        "backupValidated=$script:BackupValidado",
        "mutationGuard=$script:MutacaoLiberada",
        "stagingReady=true",
        "preActivationReady=true",
        "migrationSafetyApproved=$($script:MigrationSafetyMetadata.SafeMigrationsAllowed)",
        "migrationHashMatched=$($script:MigrationExecutionMetadata.HashMatched)",
        "destructiveMigrationsAllowed=false",
        "promotionApplied=$($script:PromotionMetadata.Applied)",
        "postPromotionHealthOk=$($script:PromotionMetadata.PostPromotionHealthOk)",
        "restartExecuted=$($script:RestartVerificationMetadata.Executed)",
        "restartHealthVerified=$($script:RestartVerificationMetadata.HealthVerified)",
        "restartVersionVerified=$($script:RestartVerificationMetadata.VersionVerified)",
        "servedVersion=$($script:RestartVerificationMetadata.ServedVersion)",
        "recoveryAuditComplete=$($script:RecoveryAuditMetadata.Complete)",
        "series057Closed=true",
        "nextSeries=v0.58.x",
        "closedAt=" + (Get-Date).ToString("o")
    ) | Set-Content -LiteralPath $closureFile -Encoding UTF8

    if (-not (Test-Path -LiteralPath $closureFile) -or (Get-Item -LiteralPath $closureFile).Length -le 0) {
        throw "Closure gate nao conseguiu materializar evidencia final."
    }

    $script:EndToEndClosureMetadata = [pscustomobject]@{
        Complete = $true
        EvidenceFile = $closureFile
        Mode = $(if ($Aplicar) { "apply" } else { "validate-only" })
        Series057Closed = $true
        NextSeries = "v0.58.x"
        DestructiveMigrationsAllowed = $false
        ClosedAt = (Get-Date).ToString("o")
    }

    Write-Host ("End-to-end closure gate aprovado: " + $closureFile) -ForegroundColor Green
    Write-Host "Serie v0.57.x fechada com todos os guards preservados." -ForegroundColor Green
    return $script:EndToEndClosureMetadata
}

function New-ProductionOperationsSnapshot {
    Write-DeployTitle "PRODUCTION OPERATIONS"

    if (-not $script:EndToEndClosureMetadata -or -not $script:EndToEndClosureMetadata.Complete) {
        throw "Production operations bloqueado: end-to-end closure gate precisa estar completo."
    }
    if (-not $script:BackupMetadata -or -not $script:StagingMetadata -or -not $script:RecoveryAuditMetadata) {
        throw "Production operations bloqueado: evidencias de backup, staging e recovery audit sao obrigatorias."
    }

    $operationsDir = Join-Path $DeployLogDir "operations"
    New-Item -ItemType Directory -Path $operationsDir -Force | Out-Null

    $latestFile = Join-Path $operationsDir "latest-production-state.json"
    $historyFile = Join-Path $operationsDir "production-deploy-history.jsonl"
    $timestamp = (Get-Date).ToString("o")

    $mode = $(if ($Aplicar) { "apply" } else { "validate-only" })
    $runtimeHealthy = $false
    $versionHealthy = $false
    if ($Aplicar) {
        $runtimeHealthy = [bool]$script:RestartVerificationMetadata.HealthVerified
        $versionHealthy = [bool]$script:RestartVerificationMetadata.VersionVerified
    }
    else {
        $runtimeHealthy = [bool]$script:PreActivationMetadata.ActiveHealthOk
        $versionHealthy = $true
    }

    $operationState = [ordered]@{
        schemaVersion = 1
        version = "v$TargetVersion"
        mode = $mode
        target = "$VpsUser@$VpsHost"
        remoteRoot = $RemoteRoot
        activeReleaseLink = $ActiveReleaseLink
        backupValidated = [bool]$script:BackupValidado
        backupSha256 = [string]$script:BackupMetadata.Sha256
        stagingPath = [string]$script:StagingMetadata.Path
        migrationSafetyApproved = [bool]$script:MigrationSafetyMetadata.SafeMigrationsAllowed
        migrationHashMatched = [bool]$script:MigrationExecutionMetadata.HashMatched
        destructiveMigrationsAllowed = $false
        promotionApplied = [bool]$script:PromotionMetadata.Applied
        runtimeHealthy = $runtimeHealthy
        versionHealthy = $versionHealthy
        servedVersion = [string]$script:RestartVerificationMetadata.ServedVersion
        rollbackExecuted = [bool]$script:PromotionMetadata.AutoRollbackExecuted -or [bool]$script:RestartVerificationMetadata.RollbackExecuted
        recoveryAuditComplete = [bool]$script:RecoveryAuditMetadata.Complete
        closureComplete = [bool]$script:EndToEndClosureMetadata.Complete
        operationsStatus = $(if ($runtimeHealthy -and $versionHealthy) { "healthy" } else { "validated" })
        recordedAt = $timestamp
    }

    $json = $operationState | ConvertTo-Json -Depth 5 -Compress
    if ([string]::IsNullOrWhiteSpace($json)) {
        throw "Production operations nao conseguiu serializar o estado operacional."
    }

    $json | Set-Content -LiteralPath $latestFile -Encoding UTF8
    $json | Add-Content -LiteralPath $historyFile -Encoding UTF8

    foreach ($requiredFile in @($latestFile,$historyFile)) {
        if (-not (Test-Path -LiteralPath $requiredFile)) {
            throw "Production operations incompleto: arquivo obrigatorio ausente: $requiredFile"
        }
        if ((Get-Item -LiteralPath $requiredFile).Length -le 0) {
            throw "Production operations incompleto: arquivo vazio: $requiredFile"
        }
    }

    $historyCount = @(Get-Content -LiteralPath $historyFile -Encoding UTF8 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    if ($historyCount -lt 1) {
        throw "Production operations history nao possui entradas."
    }

    $script:ProductionOperationsMetadata = [pscustomobject]@{
        LatestFile = $latestFile
        HistoryFile = $historyFile
        HistoryCount = $historyCount
        RuntimeHealthy = $runtimeHealthy
        VersionHealthy = $versionHealthy
        Status = [string]$operationState.operationsStatus
        RecordedAt = $timestamp
    }

    Write-Host ("Production operations snapshot: " + $latestFile) -ForegroundColor Green
    Write-Host ("Production deploy history entries: " + $historyCount) -ForegroundColor Green
    Write-Host ("Production operations status: " + $script:ProductionOperationsMetadata.Status) -ForegroundColor Green
    return $script:ProductionOperationsMetadata
}

function Show-FoundationBoundary {
    Write-DeployTitle "FOUNDATION"

    Write-Host "v0.58.0 inicia observabilidade operacional e historico continuo de producao." -ForegroundColor Cyan
    Write-Host "Migrations destrutivas continuam bloqueadas; promocao e restart so ocorrem em modo -Aplicar apos todos os gates." -ForegroundColor Yellow
    Write-Host "O pipeline seguro da v0.57.x permanece como base obrigatoria da serie v0.58.x." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Garantia ativa:" -ForegroundColor Cyan
    Write-Host "  nenhuma migration ou substituicao da aplicacao antes de backup PostgreSQL concluido e validado." -ForegroundColor Green
}

# -------------------------------------------------------------------------
# Fluxo v0.58.40
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
Assert-ControlledActivationConfirmation
$activationSnapshot = New-ActiveStateSnapshot
$immediateRollbackPlan = New-ImmediateRollbackCommandPlan
$controlledActivation = Test-ControlledActivationReadiness
$migrationSafetyMetadata = Test-MigrationSafetyGate
$migrationExecutionMetadata = Invoke-ApprovedNonDestructiveMigrations
Test-AtomicPromotionReadiness
$promotionMetadata = Invoke-AtomicReleasePromotion
$restartVerificationMetadata = Invoke-ControlledServiceRestartAndVersionVerification
$recoveryAuditMetadata = New-RecoveryAuditBundle
$endToEndClosureMetadata = Test-EndToEndClosureGate
$productionOperationsMetadata = New-ProductionOperationsSnapshot
Show-FoundationBoundary

New-Item -ItemType Directory -Path $DeployLogDir -Force | Out-Null
$logFile = Join-Path $DeployLogDir ("deploy-production-operations-v" + $localVersion + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
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
    "activationSnapshot=$($activationSnapshot.Path)",
    "immediateRollbackPlan=$immediateRollbackPlan",
    "controlledActivationVersion=$($controlledActivation.Version)",
    "controlledActivationSnapshotReady=$($controlledActivation.SnapshotReady)",
    "controlledActivationRollbackReady=$($controlledActivation.RollbackReady)",
    "controlledActivationBackupReady=$($controlledActivation.BackupReady)",
    "controlledActivationExecuted=$($promotionMetadata.Applied)",
    "atomicPromotionActiveLink=$($promotionMetadata.ActiveReleaseLink)",
    "atomicPromotionPreviousRelease=$($promotionMetadata.PreviousRelease)",
    "atomicPromotionNewRelease=$($promotionMetadata.NewRelease)",
    "postPromotionHealthOk=$($promotionMetadata.PostPromotionHealthOk)",
    "autoRollbackExecuted=$($promotionMetadata.AutoRollbackExecuted)",
    "restartVerificationExecuted=$($restartVerificationMetadata.Executed)",
    "applicationService=$($restartVerificationMetadata.Service)",
    "expectedServedVersion=$($restartVerificationMetadata.ExpectedVersion)",
    "servedVersion=$($restartVerificationMetadata.ServedVersion)",
    "restartHealthVerified=$($restartVerificationMetadata.HealthVerified)",
    "restartVersionVerified=$($restartVerificationMetadata.VersionVerified)",
    "restartRollbackExecuted=$($restartVerificationMetadata.RollbackExecuted)",
    "migrationCount=$($migrationSafetyMetadata.MigrationCount)",
    "migrationHash=$($migrationSafetyMetadata.MigrationHash)",
    "migrationDestructiveDetected=$($migrationSafetyMetadata.DestructiveDetected)",
    "safeMigrationsAllowed=$($migrationSafetyMetadata.SafeMigrationsAllowed)",
    "migrationExecutionPerformed=$($migrationExecutionMetadata.Executed)",
    "migrationExecutionSkipped=$($migrationExecutionMetadata.Skipped)",
    "migrationApprovedHash=$($migrationExecutionMetadata.ApprovedHash)",
    "migrationRevalidatedHash=$($migrationExecutionMetadata.RevalidatedHash)",
    "migrationHashMatched=$($migrationExecutionMetadata.HashMatched)",
    "migrationExecutionCount=$($migrationExecutionMetadata.MigrationCount)",
    "migrationFailureRecoveryRequired=false",
    "migrationFailureRecoveryExecuted=false",
    "recoveryAuditDir=$($recoveryAuditMetadata.AuditDir)",
    "recoveryAuditComplete=$($recoveryAuditMetadata.Complete)",
    "recoveryAuditSummary=$($recoveryAuditMetadata.SummaryFile)",
    "recoveryAuditBackupEvidence=$($recoveryAuditMetadata.BackupEvidenceFile)",
    "recoveryAuditMigrationEvidence=$($recoveryAuditMetadata.MigrationEvidenceFile)",
    "recoveryAuditOperatorNextSteps=$($recoveryAuditMetadata.OperatorNextStepsFile)",
    "endToEndClosureComplete=$($endToEndClosureMetadata.Complete)",
    "endToEndClosureEvidence=$($endToEndClosureMetadata.EvidenceFile)",
    "endToEndClosureMode=$($endToEndClosureMetadata.Mode)",
    "series057Closed=$($endToEndClosureMetadata.Series057Closed)",
    "nextSeries=$($endToEndClosureMetadata.NextSeries)",
    "operationsLatestState=$($productionOperationsMetadata.LatestFile)",
    "operationsHistory=$($productionOperationsMetadata.HistoryFile)",
    "operationsHistoryCount=$($productionOperationsMetadata.HistoryCount)",
    "operationsRuntimeHealthy=$($productionOperationsMetadata.RuntimeHealthy)",
    "operationsVersionHealthy=$($productionOperationsMetadata.VersionHealthy)",
    "operationsStatus=$($productionOperationsMetadata.Status)",
    "destructiveMigrationsAllowed=false",
    "mutationGuard=true",
    "foundationOnly=true",
    "completedAt=" + (Get-Date).ToString("o")
) | Set-Content -LiteralPath $logFile -Encoding UTF8

Write-Host ""
Write-Host ("Production Operations Foundation concluida. Log local: " + $logFile) -ForegroundColor Green
Write-Host "Nenhuma migration destrutiva foi executada. A promocao, quando aplicada, usa troca atomica reversivel." -ForegroundColor Green
