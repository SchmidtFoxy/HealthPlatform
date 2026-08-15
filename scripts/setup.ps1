$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# v0.3.11 FAST_DOTNET_CHECK
# Em ciclos de desenvolvimento, nao instala nem atualiza o SDK automaticamente.
# Apenas confirma que o dotnet existe; upgrades ficam a cargo do desenvolvedor quando uma versao futura realmente exigir.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK nao encontrado. Instale manualmente o .NET SDK exigido pelo projeto antes de continuar."
}
$dotnetVersion = (& dotnet --version).Trim()
Write-Host "dotnet SDK detectado: $dotnetVersion (sem verificacao/atualizacao automatica)." -ForegroundColor DarkGray


function Invoke-NativeStep {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][scriptblock]$Command
    )

    Write-Host $Label -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Falha na etapa: $Label (codigo $LASTEXITCODE)"
    }
}

# IMPORTANTE: o r6 aplicou esta migration com sucesso no banco de desenvolvimento.
# A partir daqui ela vira a identidade ESTAVEL da migration inicial. Como os ZIPs
# anteriores geravam timestamp novo em cada pasta, normalizamos o arquivo gerado
# para o mesmo ID e evitamos recriar as tabelas existentes.
$baselineMigrationId = "20260813190735_InitialCreate"
$baselineTimestamp = "20260813190735"

Invoke-NativeStep "[1/30] Restaurando pacotes..." { dotnet restore .\HealthPlatform.slnx }

Write-Host "[2/30] Verificando dotnet-ef..." -ForegroundColor Cyan
$dotnetEfOk = $false
try {
    $dotnetEfVersion = (& dotnet ef --version 2>$null).Trim()
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($dotnetEfVersion)) {
        $dotnetEfOk = $true
        Write-Host "dotnet-ef detectado: $dotnetEfVersion (sem atualizacao automatica)." -ForegroundColor DarkGray
    }
} catch {
    $dotnetEfOk = $false
}

if (-not $dotnetEfOk) {
    Write-Host "dotnet-ef nao encontrado; instalando 10.* uma unica vez..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef --version 10.*
    if ($LASTEXITCODE -ne 0) { throw "Nao foi possivel instalar dotnet-ef." }
}

Invoke-NativeStep "[3/30] Compilando..." { dotnet build .\HealthPlatform.slnx --no-restore }

$migrationsPath = Join-Path $root "src\HealthPlatform.Infrastructure\Migrations"
$initialMigration = $null
if (Test-Path $migrationsPath) {
    $initialMigration = Get-ChildItem $migrationsPath -Filter "*_InitialCreate.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } |
        Select-Object -First 1
}

if (-not $initialMigration) {
    Invoke-NativeStep "[4/30] Gerando migration inicial..." {
        dotnet ef migrations add InitialCreate `
          --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
          --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
          --output-dir Migrations `
          --no-build
    }

    # O EF usa timestamp no nome. Para que todos os ZIPs reconhecam a mesma
    # migration inicial, trocamos apenas o ID/timestamp pelo baseline do r6.
    $generated = Get-ChildItem $migrationsPath -Filter "*_InitialCreate.cs" |
        Where-Object { $_.Name -notlike "*.Designer.cs" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $generated) { throw "Migration InitialCreate foi gerada, mas nao foi localizada." }

    $generatedTimestamp = ($generated.BaseName -split '_')[0]
    $generatedDesigner = Join-Path $migrationsPath ($generatedTimestamp + "_InitialCreate.Designer.cs")
    $targetMain = Join-Path $migrationsPath ($baselineTimestamp + "_InitialCreate.cs")
    $targetDesigner = Join-Path $migrationsPath ($baselineTimestamp + "_InitialCreate.Designer.cs")

    # Atualiza o atributo [Migration("...")] no Designer antes do rename.
    if (Test-Path $generatedDesigner) {
        $designerText = Get-Content $generatedDesigner -Raw
        $designerText = $designerText.Replace($generatedTimestamp + "_InitialCreate", $baselineMigrationId)
        Set-Content -Path $generatedDesigner -Value $designerText -Encoding UTF8
    }

    if ($generated.FullName -ne $targetMain) {
        if (Test-Path $targetMain) { Remove-Item $targetMain -Force }
        Move-Item $generated.FullName $targetMain -Force
    }
    if ((Test-Path $generatedDesigner) -and ($generatedDesigner -ne $targetDesigner)) {
        if (Test-Path $targetDesigner) { Remove-Item $targetDesigner -Force }
        Move-Item $generatedDesigner $targetDesigner -Force
    }

    Write-Host "    Migration normalizada para: $baselineMigrationId" -ForegroundColor DarkGray
} else {
    Write-Host "[4/30] Migration inicial ja existe; pulando geracao." -ForegroundColor DarkGray
}

Invoke-NativeStep "[5/30] Recompilando com as migrations..." { dotnet build .\HealthPlatform.slnx --no-restore }

Invoke-NativeStep "[6/30] Atualizando banco..." {
    dotnet ef database update `
      --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
      --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
      --no-build
}

Invoke-NativeStep "[7/30] Aplicando upgrade v0.1.3 (anamnese)..." {
    Get-Content .\scripts\sql\v0.1.3_anamnese.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[8/30] Aplicando upgrade v0.1.4 (exames laboratoriais)..." {
    Get-Content .\scripts\sql\v0.1.4_exames.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[9/30] Aplicando upgrade v0.1.5 (relatorios clinicos)..." {
    Get-Content .\scripts\sql\v0.1.5_relatorios.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[10/30] Aplicando upgrade v0.1.6 (plano alimentar)..." {
    Get-Content .\scripts\sql\v0.1.6_plano_alimentar.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[11/30] Aplicando upgrade v0.1.7 (metas e diario)..." {
    Get-Content .\scripts\sql\v0.1.7_metas_diario.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[12/30] Aplicando upgrade v0.3.0 (treinos)..." {
    Get-Content .\scripts\sql\v0.3.0_treinos.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[13/30] Aplicando upgrade v0.3.1 (execucoes de treino)..." {
    Get-Content .\scripts\sql\v0.3.1_execucoes_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[14/30] Aplicando upgrade v0.3.4 (pendencias clinicas)..." {
    Get-Content .\scripts\sql\v0.3.4_pendencias.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[15/30] Aplicando upgrade v0.3.5 (notificacoes internas)..." {
    Get-Content .\scripts\sql\v0.3.5_notificacoes.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[16/30] Aplicando upgrade v0.3.8 (follow-up)..." {
    Get-Content .\scripts\sql\v0.3.8_followup.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[17/30] Aplicando upgrade v0.3.15 (evolucoes clinicas SOAP)..." {
    Get-Content .\scripts\sql\v0.3.15_evolucoes_clinicas.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[18/30] Aplicando upgrade v0.3.21 (progressao de plano alimentar)..." {
    Get-Content .\scripts\sql\v0.3.21_progressao_plano_alimentar.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[19/30] Aplicando upgrade v0.3.22 (progressao de treino)..." {
    Get-Content .\scripts\sql\v0.3.22_progressao_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[20/30] Aplicando upgrade v0.3.23 (modelos de plano alimentar)..." {
    Get-Content .\scripts\sql\v0.3.23_modelos_plano_alimentar.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[21/30] Aplicando upgrade v0.3.24 (modelos de plano de treino)..." {
    Get-Content .\scripts\sql\v0.3.24_modelos_plano_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[22/30] Aplicando upgrade v0.3.25 (metas nutricionais)..." {
    Get-Content .\scripts\sql\v0.3.25_metas_nutricionais.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[23/30] Aplicando upgrade v0.3.26 (biblioteca de refeicoes)..." {
    Get-Content .\scripts\sql\v0.3.26_modelos_refeicoes.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[24/30] Aplicando upgrade v0.3.27 (biblioteca de sessoes de treino)..." {
    Get-Content .\scripts\sql\v0.3.27_modelos_sessoes_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[25/30] Aplicando upgrade v0.3.29 (metas por refeicao)..." {
    Get-Content .\scripts\sql\v0.3.29_metas_por_refeicao.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[26/30] Aplicando upgrade v0.3.30 (fases nutricionais)..." {
    Get-Content .\scripts\sql\v0.3.30_fases_nutricionais.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[27/30] Aplicando upgrade v0.3.31 (fases de treino)..." {
    Get-Content .\scripts\sql\v0.3.31_fases_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[28/30] Aplicando upgrade v0.3.32 (check-ins de acompanhamento)..." {
    Get-Content .\scripts\sql\v0.3.32_checkins_acompanhamento.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[29/30] Aplicando upgrade v0.3.34 (criterios de transicao das fases)..." {
    Get-Content .\scripts\sql\v0.3.34_criterios_transicao_fases.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[30/30] Aplicando upgrade v0.3.35 (revisoes e transicoes de fases)..." {
    Get-Content .\scripts\sql\v0.3.35_revisoes_transicoes_fases.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Write-Host ""
Write-Host "PREPARACAO CONCLUIDA." -ForegroundColor Green
Write-Host "Migration baseline: $baselineMigrationId" -ForegroundColor DarkGreen
Write-Host "Agora rode: .\RODAR.ps1" -ForegroundColor Green
