# v0.59.8 - Chat Attachments.
$versionAtual0598 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0598 -ne "0.60.2") { throw "VERSION.txt esperado 0.59.11; atual: $versionAtual0598" }
$roadmapAtual0598 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0598 in @("v0.59.8 — Chat Attachments", "v0.59.9 — PWA Messaging Notifications")) { if (-not $roadmapAtual0598.Contains($token0598)) { throw "Roadmap v0.59.8 incompleto: $token0598" } }
$filesController0598 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/ArquivosPacienteController.cs') -Raw
$appJs0598 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$appCss0598 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
foreach ($token0598Api in @('application/msword','application/vnd.openxmlformats-officedocument.wordprocessingml.document','LimiteBytes = 15 * 1024 * 1024','AssinaturaValida','viaChat','if (!viaChat) await NotificarNovoArquivo')) { if (-not $filesController0598.Contains($token0598Api)) { throw "Chat Attachments API v0.59.8 incompleta: $token0598Api" } }
foreach ($token0598Ui in @('HP_CHAT_ATTACHMENTS_V0598','data-chat-attachment-v0598','hpSendChatAttachmentV0598','hpOpenChatAttachmentV0598','rollback',"referenciaTipo:'Arquivo'")) { if (-not $appJs0598.Contains($token0598Ui)) { throw "Chat Attachments UI v0.59.8 incompleta: $token0598Ui" } }
foreach ($token0598Css in @('.care-chat-attachment-v0598','.care-chat-compose-actions-v0598','@media(max-width:760px)','@media(max-width:430px)','min-height:44px')) { if (-not $appCss0598.Contains($token0598Css)) { throw "PWA Chat Attachments v0.59.8 incompleta: $token0598Css" } }
Write-Host "[Produto] v0.59.8 / Chat Attachments: OK." -ForegroundColor DarkCyan

# v0.59.7 - Chat Read State & Attention Queue.
$versionAtual0597 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0597 -ne "0.60.2") { throw "VERSION.txt esperado 0.59.7; atual: $versionAtual0597" }
$roadmapAtual0597 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0597 in @("v0.59.7 — Chat Read State & Attention Queue", "v0.59.8 — Chat Attachments")) { if (-not $roadmapAtual0597.Contains($token0597)) { throw "Roadmap v0.59.7 incompleto: $token0597" } }
$chatController0597 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/ChatAcompanhamentoController.cs') -Raw
$appJs0597 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$appCss0597 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
foreach ($token0597Api in @('somenteAguardandoResposta','aguardandoResposta','aguardandoDesdeUtc','minutosAguardando','totalAguardandoResposta','totalSlaExcedido')) { if (-not $chatController0597.Contains($token0597Api)) { throw "Chat Read State API v0.59.7 incompleta: $token0597Api" } }
foreach ($token0597Ui in @('HP_CHAT_READ_ATTENTION_V0597','data-chat-filter-v0597="waiting"','chat-attention-queue-v0597','data-chat-attention-open-v0597','SLA 24h excedido')) { if (-not ($appJs0597.Contains($token0597Ui) -or $appCss0597.Contains($token0597Ui))) { throw "Chat Attention UI v0.59.7 incompleta: $token0597Ui" } }
foreach ($token0597Css in @('.professional-chat-kpis-v0597','.chat-attention-item-v0597','@media(max-width:760px)','@media(max-width:430px)','min-height:44px')) { if (-not $appCss0597.Contains($token0597Css)) { throw "PWA Chat Attention v0.59.7 incompleta: $token0597Css" } }
Write-Host "[Produto] v0.59.7 / Chat Read State & Attention Queue: OK." -ForegroundColor DarkCyan

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

# v0.59.6 - Professional Chat Inbox.
$versionAtual0596 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0596 -ne "0.60.2") { throw "VERSION.txt esperado 0.59.6; atual: $versionAtual0596" }
$roadmapAtual0596 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0596 in @("Anti-Roadmap-Loop", "v0.59.6 — Professional Chat Inbox", "v0.59.7 — Chat Read State & Attention Queue")) { if (-not $roadmapAtual0596.Contains($token0596)) { throw "Roadmap v0.59.6 incompleto: $token0596" } }
$chatController0596 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/ChatAcompanhamentoController.cs') -Raw
$appJs0596 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$index0596 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/index.html') -Raw
$appCss0596 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
foreach ($token0596Api in @('[HttpGet("inbox")]','InboxProfissional','totalNaoLidas','ultimaMensagemEmUtc','somenteNaoLidas')) { if (-not $chatController0596.Contains($token0596Api)) { throw "Professional Chat Inbox API v0.59.6 incompleta: $token0596Api" } }
foreach ($token0596Ui in @('data-view="chat-profissional"','professional-chat-inbox-v0596','professionalChatSearchV0596','data-chat-filter','data-chat-global-badge')) { if (-not ($appJs0596.Contains($token0596Ui) -or $index0596.Contains($token0596Ui))) { throw "Professional Chat Inbox UI v0.59.6 incompleta: $token0596Ui" } }
foreach ($token0596Css in @('.professional-chat-inbox-v0596','@media(max-width:760px)','@media(max-width:430px)')) { if (-not $appCss0596.Contains($token0596Css)) { throw "Professional Chat Inbox PWA v0.59.6 incompleta: $token0596Css" } }
Write-Host "[Produto] v0.59.6 / Professional Chat Inbox: OK." -ForegroundColor DarkCyan

# v0.59.5 - Supplement Training Context.
$versionAtual0595 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0595 -ne "0.60.2") { throw "VERSION.txt corrente esperado 0.59.6 durante gate v0.59.5; atual: $versionAtual0595" }
$roadmapAtual0595 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0595 in @("Anti-Roadmap-Loop", "v0.59.5 — Supplement Training Context", "v0.59.6 — Professional Chat Inbox")) { if (-not $roadmapAtual0595.Contains($token0595)) { throw "Roadmap v0.59.5 incompleto: $token0595" } }
foreach ($rel0595 in @(
 'src/HealthPlatform.Infrastructure/Migrations/20261006154500_V0595SupplementTrainingContext.cs',
 'src/HealthPlatform.Domain/Entities/SuplementoPlanoAlimentar.cs',
 'src/HealthPlatform.Api/Contracts/PlanosAlimentares/PlanoAlimentarContracts.cs'
)) { if (-not (Test-Path (Join-Path $PSScriptRoot $rel0595))) { throw "Supplement Training Context v0.59.5 incompleto: arquivo ausente $rel0595" } }
$migrationFile0595 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Infrastructure/Migrations/20261006154500_V0595SupplementTrainingContext.cs') -Raw
foreach ($token0595Migration in @('[DbContext(typeof(AppDbContext))]','[Migration("20261006154500_V0595SupplementTrainingContext")]','SessaoTreinoId')) { if (-not $migrationFile0595.Contains($token0595Migration)) { throw "Migration EF Core v0.59.5 incompleta/invisivel: $token0595Migration" } }
Write-Host "[Produto] v0.59.5 / Supplement Training Context: OK." -ForegroundColor DarkCyan

# v0.59.4 - Supplement Scheduling & Nutrition Integration.
$versionAtual0594 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0594 -ne "0.60.2") { throw "VERSION.txt esperado 0.59.4; atual: $versionAtual0594" }
$roadmapAtual0594 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0594 in @("Anti-Roadmap-Loop", "v0.59.4 — Supplement Scheduling & Nutrition Integration", "v0.59.5 — Supplement Training Context")) {
    if (-not $roadmapAtual0594.Contains($token0594)) { throw "Roadmap v0.59.4 incompleto: $token0594" }
}
foreach ($rel0594 in @(
    'src/HealthPlatform.Domain/Entities/SuplementoPlanoAlimentar.cs',
    'src/HealthPlatform.Infrastructure/Migrations/20261006150000_V0594SupplementScheduling.cs',
    'src/HealthPlatform.Api/Contracts/PlanosAlimentares/PlanoAlimentarContracts.cs'
)) { if (-not (Test-Path (Join-Path $PSScriptRoot $rel0594))) { throw "Supplement Scheduling v0.59.4 incompleto: arquivo ausente $rel0594" } }
$planController0594 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/PlanosAlimentaresController.cs') -Raw
$appJs0594 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
foreach ($token0594Api in @('MontarSuplementos','SuplementosPlanoAlimentar','CalcularSuplemento','QuantidadePorcoes','RefeicaoOrdem')) { if (-not $planController0594.Contains($token0594Api)) { throw "Supplement Scheduling API v0.59.4 incompleta: $token0594Api" } }
foreach ($token0594Ui in @("HP_SUPPLEMENT_SCHEDULING_V0594='v0.59.4'",'supplementScheduleListV0594','suppMealOrder','suppContext','supplement-schedule-builder-v0594')) { if (-not $appJs0594.Contains($token0594Ui)) { throw "Supplement Scheduling UI v0.59.4 incompleta: $token0594Ui" } }

$migrationFile0594 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Infrastructure/Migrations/20261006150000_V0594SupplementScheduling.cs') -Raw
foreach ($token0594Migration in @('[DbContext(typeof(AppDbContext))]','[Migration("20261006150000_V0594SupplementScheduling")]')) { if (-not $migrationFile0594.Contains($token0594Migration)) { throw "Migration EF Core v0.59.4 invisivel: $token0594Migration" } }
Write-Host "[Produto] v0.59.4 / Supplement Scheduling & Nutrition Integration: OK." -ForegroundColor DarkCyan

# v0.59.3 - Product Brain gate: Supplement Catalog Foundation + proxima etapa funcional.
$versionAtual0593 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0593 -ne "0.60.2") { throw "VERSION.txt esperado 0.59.3; atual: $versionAtual0593" }
$roadmapAtual0593 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0593 in @("Anti-Roadmap-Loop", "v0.59.3 — Supplement Catalog Foundation", "v0.59.4 — Supplement Scheduling & Nutrition Integration")) {
    if (-not $roadmapAtual0593.Contains($token0593)) { throw "Roadmap v0.59.3 incompleto: $token0593" }
}
Write-Host "[Produto] v0.59.3 / Supplement Catalog Foundation: roadmap vivo OK." -ForegroundColor DarkCyan

$suplementoArquivos0593 = @(
    'src/HealthPlatform.Domain/Entities/Suplemento.cs',
    'src/HealthPlatform.Api/Contracts/Suplementos/SuplementoContracts.cs',
    'src/HealthPlatform.Api/Controllers/SuplementosController.cs',
    'src/HealthPlatform.Infrastructure/Migrations/20261006143000_V0593SupplementCatalog.cs'
)
foreach ($rel0593 in $suplementoArquivos0593) { if (-not (Test-Path (Join-Path $PSScriptRoot $rel0593))) { throw "Supplement Catalog v0.59.3 incompleto: arquivo ausente $rel0593" } }
$controllerAtual0593 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/SuplementosController.cs') -Raw
$migrationAtual0593 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Infrastructure/Migrations/20261006143000_V0593SupplementCatalog.cs') -Raw
foreach ($token0593Api in @('[Route("api/suplementos")]', 'x.OrganizacaoId == currentUser.OrganizationId', '[HttpPost]', '[HttpPut("{id:guid}")]', '[HttpPost("{id:guid}/reativar")]')) { if (-not $controllerAtual0593.Contains($token0593Api)) { throw "Supplement API v0.59.3 incompleta: $token0593Api" } }
foreach ($token0593Db in @('CreateTable(', 'name: "Suplementos"', 'IX_Suplementos_OrganizacaoId_NomeNormalizado_MarcaNormalizada')) { if (-not $migrationAtual0593.Contains($token0593Db)) { throw "Migration suplemento v0.59.3 incompleta: $token0593Db" } }
Write-Host "[Produto] v0.59.3 / entidade + API + migration: OK." -ForegroundColor DarkCyan

# v0.59.2 - Product Brain gate: Nutrition Target Guidance 2.0 + proxima etapa funcional.
$versionAtual0592 = (Get-Content (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($versionAtual0592 -ne "0.60.2") { throw "VERSION.txt esperado 0.59.3; atual: $versionAtual0592" }
$roadmapAtual0592 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0592 in @("Anti-Roadmap-Loop", "v0.59.2 — Nutrition Target Guidance 2.0", "v0.59.3 — Supplement Catalog Foundation")) {
    if (-not $roadmapAtual0592.Contains($token0592)) { throw "Roadmap v0.59.2 incompleto: $token0592" }
}
Write-Host "[Produto] v0.59.2 / Nutrition Target Guidance 2.0: roadmap vivo OK." -ForegroundColor DarkCyan

# v0.59.1 - Product Brain gate: Meal Templates Quick Apply + proxima etapa funcional.
$roadmapAtual0591 = Get-Content -Encoding UTF8 (Join-Path $root "ROADMAP.md") -Raw
foreach ($token0591 in @("Anti-Roadmap-Loop", "Nutrition & Communication Experience", "v0.59.1 — Meal Templates Quick Apply", "v0.59.2 — Nutrition Target Guidance 2.0")) {
    if (-not $roadmapAtual0591.Contains($token0591)) { throw "Roadmap v0.59.1 incompleto: $token0591" }
}
Write-Host "[Produto] v0.59.1 / Meal Templates Quick Apply: roadmap vivo OK." -ForegroundColor DarkCyan

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


# ===== v0.59.9 — PWA Messaging Notifications =====
Write-Host "[v0.59.9] Validando PWA Messaging Notifications..." -ForegroundColor Cyan
$version0599 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
$chat0599 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/Controllers/ChatAcompanhamentoController.cs') -Raw
$app0599 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$sw0599 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/sw.js') -Raw
$roadmap0599 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0599 in @('chat-profissional:{paciente.Id}','pushLink','NotificacoesInternas')) { if (-not $chat0599.Contains($token0599)) { throw "Messaging push backend v0.59.9 incompleto: $token0599" } }
foreach ($token0599 in @('HP_PWA_MESSAGING_NOTIFICATIONS_V0599','hpOpenMessagingDeepLinkV0599','aesynPushLink','hpRefreshMessagingBadgesV0599','chat-profissional:')) { if (-not $app0599.Contains($token0599)) { throw "Messaging deep-link UI v0.59.9 incompleta: $token0599" } }
foreach ($token0599 in @('chatTag','link.startsWith(''chat'')','AESYN_PUSH_OPEN')) { if (-not $sw0599.Contains($token0599)) { throw "Service Worker messaging v0.59.9 incompleto: $token0599" } }
foreach ($token0599 in @('v0.59.9 — PWA Messaging Notifications — IMPLEMENTADA','v0.59.10 — Messaging Mobile Polish')) { if (-not $roadmap0599.Contains($token0599)) { throw "ROADMAP v0.59.9 incompleto: $token0599" } }
Write-Host "    v0.59.9 / PWA Messaging Notifications: OK." -ForegroundColor Green


# ===== v0.59.10 — Messaging Mobile Polish =====
Write-Host "[v0.59.10] Validando Messaging Mobile Polish..." -ForegroundColor Cyan
$version05910 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($version05910 -ne '0.60.2') { throw "VERSION.txt inesperado na v0.59.10: $version05910" }
$app05910 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css05910 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$roadmap05910 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token05910 in @('HP_MESSAGING_MOBILE_POLISH_V05910','hpChatSyncKeyboardV05910','hpChatComposeStatusV05910','data-chat-camera-v05910','data-chat-back-inbox-v05910','requestSubmit')) { if (-not $app05910.Contains($token05910)) { throw "Messaging Mobile UI v0.59.10 incompleta: $token05910" } }
foreach ($token05910 in @('care-chat-shell-v05910','safe-area-inset-bottom','hp-chat-keyboard-open-v05910','@media(max-width:760px)','@media(max-width:430px)')) { if (-not $css05910.Contains($token05910)) { throw "Messaging Mobile CSS v0.59.10 incompleto: $token05910" } }
foreach ($token05910 in @('v0.59.10 — Messaging Mobile Polish — IMPLEMENTADA','v0.59.11 — Messaging Presence & Delivery Polish')) { if (-not $roadmap05910.Contains($token05910)) { throw "ROADMAP v0.59.10 incompleto: $token05910" } }
Write-Host "    v0.59.10 / Messaging Mobile Polish: OK." -ForegroundColor Green


# ===== v0.59.11 — Messaging Presence & Delivery Polish / Lista 04 Closure =====
Write-Host "[v0.59.11] Validando Messaging Presence & Delivery Polish..." -ForegroundColor Cyan
$version05911 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($version05911 -ne '0.60.2') { throw "VERSION.txt inesperado na v0.59.11: $version05911" }
$app05911 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css05911 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$roadmap05911 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token05911 in @('HP_MESSAGING_PRESENCE_DELIVERY_V05911','hpChatReceiptTextV05911','hpChatConversationActivityV05911','hpRefreshChatReceiptsV05911','setInterval(hpRefreshChatReceiptsV05911,20000)','O AESYN não exibe presença online estimada.')) { if (-not $app05911.Contains($token05911)) { throw "Messaging Presence v0.59.11 incompleta: $token05911" } }
foreach ($token05911 in @('care-chat-presence-v05911','care-chat-status-v05911','@media(max-width:760px)','@media(max-width:430px)')) { if (-not $css05911.Contains($token05911)) { throw "Messaging Presence CSS v0.59.11 incompleto: $token05911" } }
foreach ($token05911 in @('v0.59.11 — Messaging Presence & Delivery Polish — IMPLEMENTADA','LISTA 04 — CONCLUÍDA','12/12 etapas concluídas','Lista 05 fornecida pelo usuário')) { if (-not $roadmap05911.Contains($token05911)) { throw "Fechamento Lista 04 incompleto: $token05911" } }
Write-Host "    v0.59.11 / Messaging Presence & Delivery Polish / Lista 04: OK." -ForegroundColor Green
# ===== v0.60.0 — PWA Session & Mobile Search Reliability =====
Write-Host "[v0.60.0] Validando PWA Session & Mobile Search Reliability..." -ForegroundColor Cyan
$version0600 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($version0600 -ne '0.60.2') { throw "VERSION.txt inesperado na v0.60.0: $version0600" }
$app0600 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css0600 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
$index0600 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/index.html') -Raw
$sw0600 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/sw.js') -Raw
$roadmap0600 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'ROADMAP.md') -Raw
foreach ($token0600 in @('HP_PWA_SESSION_MOBILE_SEARCH_V0600','hpTryRefreshSessionV0600','__sessionRetry:true','hpBootstrapSessionV0600','global-search-close-v0600','hpSyncMobileViewportV0600')) {
    if (-not $app0600.Contains($token0600)) { throw "PWA/session/search v0.60.0 incompleto: $token0600" }
}
foreach ($token0600 in @('hp-global-search-open-v0600','global-search-glyph-v0600','--hp-mobile-viewport-height-v0600','overflow-x:hidden','@media(max-width:430px)')) {
    if (-not $css0600.Contains($token0600)) { throw "CSS mobile v0.60.0 incompleto: $token0600" }
}
foreach ($asset0600 in @('/app.css?v=0.60.2','/operations.css?v=0.60.2','/app.js?v=0.60.2')) {
    if (-not $index0600.Contains($asset0600)) { throw "Cache busting v0.60.0 ausente: $asset0600" }
}
if (-not $sw0600.Contains('aesyn-static-v0.60.2')) { throw 'Service Worker nao anuncia cache v0.60.0.' }
foreach ($token0600 in @('v0.60.0 — PWA Session & Mobile Search Reliability — IMPLEMENTADA','v0.60.1 — Patient Files Mobile Recovery','Montar dieta modelo','Nova dieta')) {
    if (-not $roadmap0600.Contains($token0600)) { throw "ROADMAP Lista 05 incompleto: $token0600" }
}
Write-Host "    v0.60.0 / PWA Session & Mobile Search Reliability: OK." -ForegroundColor Green
# ===== v0.60.1 — Patient Files Mobile Recovery =====
Write-Host "[v0.60.1] Validando Patient Files Mobile Recovery..." -ForegroundColor Cyan
$version0601 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($version0601 -ne '0.60.2') { throw "VERSION.txt inesperado na v0.60.1: $version0601" }
$app0601 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css0601 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
foreach ($token0601 in @('HP_PATIENT_FILES_MOBILE_RECOVERY_V0601','hpFilesFetchWithSessionV0601','hpTryRefreshSessionV0600','patient-file-upload-sheet-v0601','data-files-retry-v0601','capture="environment"','loadPatientFilesV0601')) { if (-not $app0601.Contains($token0601)) { throw "Arquivos mobile v0.60.1 incompleto: $token0601" } }
foreach ($token0601 in @('patient-files-shell-v0601','patient-file-actions-v0601','patient-file-picker-grid-v0601','@media(max-width:720px)','@media(max-width:430px)','--hp-mobile-viewport-height-v0600')) { if (-not $css0601.Contains($token0601)) { throw "CSS Arquivos mobile v0.60.1 incompleto: $token0601" } }
Write-Host "    v0.60.1 / Patient Files Mobile Recovery: OK." -ForegroundColor Green
# ===== v0.60.2 — Mobile Navigation & Profile/More 2.0 =====
Write-Host "[v0.60.2] Validando Mobile Navigation & Profile/More 2.0..." -ForegroundColor Cyan
$version0602 = (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'VERSION.txt') -Raw).Trim()
if ($version0602 -ne '0.60.2') { throw "VERSION.txt inesperado na v0.60.2: $version0602" }
$index0602 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/index.html') -Raw
$app0602 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.js') -Raw
$css0602 = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'src/HealthPlatform.Api/wwwroot/app.css') -Raw
foreach ($token0602 in @('patient-profile-more-trigger-v0602','PERFIL & MAIS','patient-more-group-v0602','patientMoreLogoutV0602','Abrir Perfil e Mais')) { if (-not $index0602.Contains($token0602)) { throw "Navegacao mobile v0.60.2 incompleta em index.html: $token0602" } }
if ($index0602.Contains('class="patient-more-trigger" id="patientMoreTrigger"')) { throw 'v0.60.2 ainda contem gatilho Mais no dock inferior.' }
foreach ($token0602 in @('HP_MOBILE_NAV_PROFILE_MORE_V0602','hpPatientPrimaryViewsV0602','patient-more-open-v0602','patientMoreLogoutV0602')) { if (-not $app0602.Contains($token0602)) { throw "Navegacao mobile v0.60.2 incompleta em app.js: $token0602" } }
foreach ($token0602 in @('.patient-profile-more-trigger-v0602','.patient-more-close-v0602','.patient-more-groups-v0602','grid-template-columns:repeat(5,minmax(0,1fr))','@media(max-width:390px)','@media(max-width:360px)')) { if (-not $css0602.Contains($token0602)) { throw "Navegacao mobile v0.60.2 incompleta em app.css: $token0602" } }
Write-Host "    v0.60.2 / Mobile Navigation & Profile/More 2.0: OK." -ForegroundColor Green
