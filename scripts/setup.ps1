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

Invoke-NativeStep "[1/35] Restaurando pacotes..." { dotnet restore .\HealthPlatform.slnx }

Write-Host "[2/35] Verificando dotnet-ef..." -ForegroundColor Cyan
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

Invoke-NativeStep "[3/35] Compilando..." { dotnet build .\HealthPlatform.slnx --no-restore }

$migrationsPath = Join-Path $root "src\HealthPlatform.Infrastructure\Migrations"
$initialMigration = $null
if (Test-Path $migrationsPath) {
    $initialMigration = Get-ChildItem $migrationsPath -Filter "*_InitialCreate.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } |
        Select-Object -First 1
}
$initialMigrationExistedBeforeSetup = $null -ne $initialMigration

if (-not $initialMigration) {
    Invoke-NativeStep "[4/35] Gerando migration inicial..." {
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
    Write-Host "[4/35] Migration inicial ja existe; pulando geracao." -ForegroundColor DarkGray
}

# v0.5.1-r1: em instalacoes atualizadas a migration InitialCreate ja existia antes
# desta versao. Nesse caso o ModelSnapshot ainda representa o modelo da v0.5.0.
# Geramos uma migration incremental real para alinhar o snapshot ao novo modelo
# antes de executar `database update`. Em instalacoes novas, a InitialCreate acabou
# de ser gerada a partir do modelo atual e ja contem SolicitacoesClinicas.
$v051MigrationName = "V051SolicitacoesClinicas"
$v051MigrationId = "20260911051000_V051SolicitacoesClinicas"
$v051Timestamp = "20260911051000"
$v051Migration = $null
if (Test-Path $migrationsPath) {
    $v051Migration = Get-ChildItem $migrationsPath -Filter "*_${v051MigrationName}.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } |
        Select-Object -First 1
}

if ($initialMigrationExistedBeforeSetup -and -not $v051Migration) {
    Write-Host "    Modelo evoluiu desde a migration inicial; gerando migration incremental v0.5.1..." -ForegroundColor DarkGray
    Invoke-NativeStep "[4.1/35] Gerando migration incremental v0.5.1..." {
        dotnet ef migrations add $v051MigrationName `
          --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
          --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
          --output-dir Migrations `
          --no-build
    }

    $generatedV051 = Get-ChildItem $migrationsPath -Filter "*_${v051MigrationName}.cs" |
        Where-Object { $_.Name -notlike "*.Designer.cs" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $generatedV051) { throw "Migration $v051MigrationName foi gerada, mas nao foi localizada." }

    $generatedV051Timestamp = ($generatedV051.BaseName -split '_')[0]
    $generatedV051Designer = Join-Path $migrationsPath ($generatedV051Timestamp + "_${v051MigrationName}.Designer.cs")
    $targetV051Main = Join-Path $migrationsPath ($v051Timestamp + "_${v051MigrationName}.cs")
    $targetV051Designer = Join-Path $migrationsPath ($v051Timestamp + "_${v051MigrationName}.Designer.cs")

    if (Test-Path $generatedV051Designer) {
        $designerText = Get-Content $generatedV051Designer -Raw
        $designerText = $designerText.Replace($generatedV051Timestamp + "_${v051MigrationName}", $v051MigrationId)
        Set-Content -Path $generatedV051Designer -Value $designerText -Encoding UTF8
    }

    if ($generatedV051.FullName -ne $targetV051Main) {
        if (Test-Path $targetV051Main) { Remove-Item $targetV051Main -Force }
        Move-Item $generatedV051.FullName $targetV051Main -Force
    }
    if ((Test-Path $generatedV051Designer) -and ($generatedV051Designer -ne $targetV051Designer)) {
        if (Test-Path $targetV051Designer) { Remove-Item $targetV051Designer -Force }
        Move-Item $generatedV051Designer $targetV051Designer -Force
    }

    Write-Host "    Migration incremental normalizada para: $v051MigrationId" -ForegroundColor DarkGray
} elseif ($v051Migration) {
    Write-Host "    Migration incremental v0.5.1 ja existe; reutilizando." -ForegroundColor DarkGray
} else {
    Write-Host "    Instalacao nova: InitialCreate ja representa o modelo v0.5.1; migration incremental dispensada." -ForegroundColor DarkGray
}

# v0.5.8: protocolos configuraveis alteram o modelo EF. Em instalacoes existentes,
# gera uma migration incremental para alinhar o ModelSnapshot antes do database update.
$v058MigrationName = "V058ProtocolosAcompanhamento"
$v058MigrationId = "20260911053000_V058ProtocolosAcompanhamento"
$v058Timestamp = "20260911053000"
$v058Migration = $null
if (Test-Path $migrationsPath) {
    $v058Migration = Get-ChildItem $migrationsPath -Filter "*_${v058MigrationName}.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1
}
if ($initialMigrationExistedBeforeSetup -and -not $v058Migration) {
    Invoke-NativeStep "[4.2/35] Gerando migration incremental v0.5.8..." {
        dotnet ef migrations add $v058MigrationName `
          --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
          --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
          --output-dir Migrations `
          --no-build
    }
    $generatedV058 = Get-ChildItem $migrationsPath -Filter "*_${v058MigrationName}.cs" | Where-Object { $_.Name -notlike "*.Designer.cs" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $generatedV058) { throw "Migration $v058MigrationName foi gerada, mas nao foi localizada." }
    $generatedV058Timestamp = ($generatedV058.BaseName -split '_')[0]
    $generatedV058Designer = Join-Path $migrationsPath ($generatedV058Timestamp + "_${v058MigrationName}.Designer.cs")
    $targetV058Main = Join-Path $migrationsPath ($v058Timestamp + "_${v058MigrationName}.cs")
    $targetV058Designer = Join-Path $migrationsPath ($v058Timestamp + "_${v058MigrationName}.Designer.cs")
    if (Test-Path $generatedV058Designer) {
        $designerText = Get-Content $generatedV058Designer -Raw
        $designerText = $designerText.Replace($generatedV058Timestamp + "_${v058MigrationName}", $v058MigrationId)
        Set-Content -Path $generatedV058Designer -Value $designerText -Encoding UTF8
    }
    if ($generatedV058.FullName -ne $targetV058Main) { if (Test-Path $targetV058Main) { Remove-Item $targetV058Main -Force }; Move-Item $generatedV058.FullName $targetV058Main -Force }
    if ((Test-Path $generatedV058Designer) -and ($generatedV058Designer -ne $targetV058Designer)) { if (Test-Path $targetV058Designer) { Remove-Item $targetV058Designer -Force }; Move-Item $generatedV058Designer $targetV058Designer -Force }
    Write-Host "    Migration incremental normalizada para: $v058MigrationId" -ForegroundColor DarkGray
} elseif ($v058Migration) {
    Write-Host "    Migration incremental v0.5.8 ja existe; reutilizando." -ForegroundColor DarkGray
} else {
    Write-Host "    Instalacao nova: InitialCreate ja representa o modelo v0.5.8; migration incremental dispensada." -ForegroundColor DarkGray
}

# v0.6.0: medicamentos estruturados e registros de adesao alteram o modelo EF.
$v060MigrationName = "V060MedicamentosAdesao"
$v060MigrationId = "20260911060000_V060MedicamentosAdesao"
$v060Timestamp = "20260911060000"
$v060Migration = $null
if (Test-Path $migrationsPath) {
    $v060Migration = Get-ChildItem $migrationsPath -Filter "*_${v060MigrationName}.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1
}
if ($initialMigrationExistedBeforeSetup -and -not $v060Migration) {
    Invoke-NativeStep "[4.3/35] Gerando migration incremental v0.6.0..." {
        dotnet ef migrations add $v060MigrationName `
          --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
          --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
          --output-dir Migrations `
          --no-build
    }
    $generatedV060 = Get-ChildItem $migrationsPath -Filter "*_${v060MigrationName}.cs" | Where-Object { $_.Name -notlike "*.Designer.cs" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $generatedV060) { throw "Migration $v060MigrationName foi gerada, mas nao foi localizada." }
    $generatedV060Timestamp = ($generatedV060.BaseName -split '_')[0]
    $generatedV060Designer = Join-Path $migrationsPath ($generatedV060Timestamp + "_${v060MigrationName}.Designer.cs")
    $targetV060Main = Join-Path $migrationsPath ($v060Timestamp + "_${v060MigrationName}.cs")
    $targetV060Designer = Join-Path $migrationsPath ($v060Timestamp + "_${v060MigrationName}.Designer.cs")
    if (Test-Path $generatedV060Designer) { $designerText = Get-Content $generatedV060Designer -Raw; $designerText = $designerText.Replace($generatedV060Timestamp + "_${v060MigrationName}", $v060MigrationId); Set-Content -Path $generatedV060Designer -Value $designerText -Encoding UTF8 }
    if ($generatedV060.FullName -ne $targetV060Main) { if (Test-Path $targetV060Main) { Remove-Item $targetV060Main -Force }; Move-Item $generatedV060.FullName $targetV060Main -Force }
    if ((Test-Path $generatedV060Designer) -and ($generatedV060Designer -ne $targetV060Designer)) { if (Test-Path $targetV060Designer) { Remove-Item $targetV060Designer -Force }; Move-Item $generatedV060Designer $targetV060Designer -Force }
    Write-Host "    Migration incremental normalizada para: $v060MigrationId" -ForegroundColor DarkGray
} elseif ($v060Migration) { Write-Host "    Migration incremental v0.6.0 ja existe; reutilizando." -ForegroundColor DarkGray }
else { Write-Host "    Instalacao nova: InitialCreate ja representa o modelo v0.6.0; migration incremental dispensada." -ForegroundColor DarkGray }

# v0.6.1: Daily Athlete / prontidao diaria altera o modelo EF.
$v061MigrationName = "V061ProntidaoDiaria"
$v061MigrationId = "20260911061000_V061ProntidaoDiaria"
$v061Timestamp = "20260911061000"
$v061Migration = $null
if (Test-Path $migrationsPath) {
    $v061Migration = Get-ChildItem $migrationsPath -Filter "*_${v061MigrationName}.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1
}
if ($initialMigrationExistedBeforeSetup -and -not $v061Migration) {
    Invoke-NativeStep "[4.4/35] Gerando migration incremental v0.6.1..." {
        dotnet ef migrations add $v061MigrationName `
          --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
          --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
          --output-dir Migrations `
          --no-build
    }
    $generatedV061 = Get-ChildItem $migrationsPath -Filter "*_${v061MigrationName}.cs" | Where-Object { $_.Name -notlike "*.Designer.cs" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $generatedV061) { throw "Migration $v061MigrationName foi gerada, mas nao foi localizada." }
    $generatedV061Timestamp = ($generatedV061.BaseName -split '_')[0]
    $generatedV061Designer = Join-Path $migrationsPath ($generatedV061Timestamp + "_${v061MigrationName}.Designer.cs")
    $targetV061Main = Join-Path $migrationsPath ($v061Timestamp + "_${v061MigrationName}.cs")
    $targetV061Designer = Join-Path $migrationsPath ($v061Timestamp + "_${v061MigrationName}.Designer.cs")
    if (Test-Path $generatedV061Designer) { $designerText = Get-Content $generatedV061Designer -Raw; $designerText = $designerText.Replace($generatedV061Timestamp + "_${v061MigrationName}", $v061MigrationId); Set-Content -Path $generatedV061Designer -Value $designerText -Encoding UTF8 }
    if ($generatedV061.FullName -ne $targetV061Main) { if (Test-Path $targetV061Main) { Remove-Item $targetV061Main -Force }; Move-Item $generatedV061.FullName $targetV061Main -Force }
    if ((Test-Path $generatedV061Designer) -and ($generatedV061Designer -ne $targetV061Designer)) { if (Test-Path $targetV061Designer) { Remove-Item $targetV061Designer -Force }; Move-Item $generatedV061Designer $targetV061Designer -Force }
    Write-Host "    Migration incremental normalizada para: $v061MigrationId" -ForegroundColor DarkGray
} elseif ($v061Migration) { Write-Host "    Migration incremental v0.6.1 ja existe; reutilizando." -ForegroundColor DarkGray }
else { Write-Host "    Instalacao nova: InitialCreate ja representa o modelo v0.6.1; migration incremental dispensada." -ForegroundColor DarkGray }

# v0.6.2: ledger de XP e consistencia altera o modelo EF.
$v062MigrationName = "V062XpConsistencia"
$v062MigrationId = "20260911070000_V062XpConsistencia"
$v062Timestamp = "20260911070000"
$v062Migration = $null
if (Test-Path $migrationsPath) {
    $v062Migration = Get-ChildItem $migrationsPath -Filter "*_${v062MigrationName}.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.Designer.cs" } | Select-Object -First 1
}
if ($initialMigrationExistedBeforeSetup -and -not $v062Migration) {
    Invoke-NativeStep "[4.5/35] Gerando migration incremental v0.6.2..." {
        dotnet ef migrations add $v062MigrationName `
          --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
          --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
          --output-dir Migrations `
          --no-build
    }
    $generatedV062 = Get-ChildItem $migrationsPath -Filter "*_${v062MigrationName}.cs" | Where-Object { $_.Name -notlike "*.Designer.cs" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $generatedV062) { throw "Migration $v062MigrationName foi gerada, mas nao foi localizada." }
    $generatedV062Timestamp = ($generatedV062.BaseName -split '_')[0]
    $generatedV062Designer = Join-Path $migrationsPath ($generatedV062Timestamp + "_${v062MigrationName}.Designer.cs")
    $targetV062Main = Join-Path $migrationsPath ($v062Timestamp + "_${v062MigrationName}.cs")
    $targetV062Designer = Join-Path $migrationsPath ($v062Timestamp + "_${v062MigrationName}.Designer.cs")
    if (Test-Path $generatedV062Designer) { $designerText = Get-Content $generatedV062Designer -Raw; $designerText = $designerText.Replace($generatedV062Timestamp + "_${v062MigrationName}", $v062MigrationId); Set-Content -Path $generatedV062Designer -Value $designerText -Encoding UTF8 }
    if ($generatedV062.FullName -ne $targetV062Main) { if (Test-Path $targetV062Main) { Remove-Item $targetV062Main -Force }; Move-Item $generatedV062.FullName $targetV062Main -Force }
    if ((Test-Path $generatedV062Designer) -and ($generatedV062Designer -ne $targetV062Designer)) { if (Test-Path $targetV062Designer) { Remove-Item $targetV062Designer -Force }; Move-Item $generatedV062Designer $targetV062Designer -Force }
    Write-Host "    Migration incremental normalizada para: $v062MigrationId" -ForegroundColor DarkGray
} elseif ($v062Migration) { Write-Host "    Migration incremental v0.6.2 ja existe; reutilizando." -ForegroundColor DarkGray }
else { Write-Host "    Instalacao nova: InitialCreate ja representa o modelo v0.6.2; migration incremental dispensada." -ForegroundColor DarkGray }

Invoke-NativeStep "[5/35] Recompilando com as migrations..." { dotnet build .\HealthPlatform.slnx --no-restore }

Invoke-NativeStep "[6/35] Atualizando banco..." {
    dotnet ef database update `
      --project .\src\HealthPlatform.Infrastructure\HealthPlatform.Infrastructure.csproj `
      --startup-project .\src\HealthPlatform.Api\HealthPlatform.Api.csproj `
      --no-build
}

Invoke-NativeStep "[7/35] Aplicando upgrade v0.1.3 (anamnese)..." {
    Get-Content .\scripts\sql\v0.1.3_anamnese.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[8/35] Aplicando upgrade v0.1.4 (exames laboratoriais)..." {
    Get-Content .\scripts\sql\v0.1.4_exames.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[9/35] Aplicando upgrade v0.1.5 (relatorios clinicos)..." {
    Get-Content .\scripts\sql\v0.1.5_relatorios.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[10/35] Aplicando upgrade v0.1.6 (plano alimentar)..." {
    Get-Content .\scripts\sql\v0.1.6_plano_alimentar.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[11/35] Aplicando upgrade v0.1.7 (metas e diario)..." {
    Get-Content .\scripts\sql\v0.1.7_metas_diario.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[12/35] Aplicando upgrade v0.3.0 (treinos)..." {
    Get-Content .\scripts\sql\v0.3.0_treinos.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[13/35] Aplicando upgrade v0.3.1 (execucoes de treino)..." {
    Get-Content .\scripts\sql\v0.3.1_execucoes_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[14/35] Aplicando upgrade v0.3.4 (pendencias clinicas)..." {
    Get-Content .\scripts\sql\v0.3.4_pendencias.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[15/35] Aplicando upgrade v0.3.5 (notificacoes internas)..." {
    Get-Content .\scripts\sql\v0.3.5_notificacoes.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[16/35] Aplicando upgrade v0.3.8 (follow-up)..." {
    Get-Content .\scripts\sql\v0.3.8_followup.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[17/35] Aplicando upgrade v0.3.15 (evolucoes clinicas SOAP)..." {
    Get-Content .\scripts\sql\v0.3.15_evolucoes_clinicas.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[18/35] Aplicando upgrade v0.3.21 (progressao de plano alimentar)..." {
    Get-Content .\scripts\sql\v0.3.21_progressao_plano_alimentar.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[19/35] Aplicando upgrade v0.3.22 (progressao de treino)..." {
    Get-Content .\scripts\sql\v0.3.22_progressao_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[20/35] Aplicando upgrade v0.3.23 (modelos de plano alimentar)..." {
    Get-Content .\scripts\sql\v0.3.23_modelos_plano_alimentar.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[21/35] Aplicando upgrade v0.3.24 (modelos de plano de treino)..." {
    Get-Content .\scripts\sql\v0.3.24_modelos_plano_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[22/35] Aplicando upgrade v0.3.25 (metas nutricionais)..." {
    Get-Content .\scripts\sql\v0.3.25_metas_nutricionais.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[23/35] Aplicando upgrade v0.3.26 (biblioteca de refeicoes)..." {
    Get-Content .\scripts\sql\v0.3.26_modelos_refeicoes.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[24/35] Aplicando upgrade v0.3.27 (biblioteca de sessoes de treino)..." {
    Get-Content .\scripts\sql\v0.3.27_modelos_sessoes_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[25/35] Aplicando upgrade v0.3.29 (metas por refeicao)..." {
    Get-Content .\scripts\sql\v0.3.29_metas_por_refeicao.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[26/35] Aplicando upgrade v0.3.30 (fases nutricionais)..." {
    Get-Content .\scripts\sql\v0.3.30_fases_nutricionais.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[27/35] Aplicando upgrade v0.3.31 (fases de treino)..." {
    Get-Content .\scripts\sql\v0.3.31_fases_treino.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[28/35] Aplicando upgrade v0.3.32 (check-ins de acompanhamento)..." {
    Get-Content .\scripts\sql\v0.3.32_checkins_acompanhamento.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[29/35] Aplicando upgrade v0.3.34 (criterios de transicao das fases)..." {
    Get-Content .\scripts\sql\v0.3.34_criterios_transicao_fases.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[30/35] Aplicando upgrade v0.3.35 (revisoes e transicoes de fases)..." {
    Get-Content .\scripts\sql\v0.3.35_revisoes_transicoes_fases.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[31/35] Aplicando upgrade v0.5.1 (solicitacoes clinicas)..." {
    Get-Content .\scripts\sql\v0.5.1_solicitacoes_clinicas.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[32/35] Aplicando upgrade v0.5.8 (protocolos de acompanhamento)..." {
    Get-Content .\scripts\sql\v0.5.8_protocolos_acompanhamento.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[33/35] Aplicando upgrade v0.6.0 (medicamentos e adesao)..." {
    Get-Content .\scripts\sql\v0.6.0_medicamentos.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[34/35] Aplicando upgrade v0.6.1 (prontidao diaria)..." {
    Get-Content .\scripts\sql\v0.6.1_prontidao_diaria.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Invoke-NativeStep "[35/35] Aplicando upgrade v0.6.2 (xp e consistencia)..." {
    Get-Content .\scripts\sql\v0.6.2_xp_consistencia.sql -Raw | docker exec -i healthplatform-postgres psql -U healthplatform -d healthplatform
}

Write-Host ""
Write-Host "PREPARACAO CONCLUIDA." -ForegroundColor Green
Write-Host "Migration baseline: $baselineMigrationId" -ForegroundColor DarkGreen
Write-Host "Agora rode: .\RODAR.ps1" -ForegroundColor Green
