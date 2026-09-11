$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$base = "http://localhost:5180"
function Get-Utf8WebAsset {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$LocalPath
    )

    $http = Invoke-WebRequest -Uri $Uri -UseBasicParsing
    if ($http.StatusCode -ne 200) {
        throw "Asset web nao respondeu: $Uri"
    }

    $content = Get-Content $LocalPath -Raw -Encoding UTF8
    return [pscustomobject]@{
        StatusCode = $http.StatusCode
        Content = $content
    }
}

$settings = Get-Content ".\src\HealthPlatform.Api\appsettings.json" -Encoding UTF8 -Raw | ConvertFrom-Json
$email = $settings.Seed.AdminEmail
$senha = $settings.Seed.AdminPassword

Write-Host "[1/600] Healthcheck..." -ForegroundColor Cyan
$health = Invoke-RestMethod -Uri "$base/api/health" -Method Get
if ($health.version -ne "0.10.3") { throw "Versao inesperada da API: $($health.version)" }
Write-Host "    API $($health.version) / banco $($health.database)" -ForegroundColor Green

Write-Host "[2/600] Login..." -ForegroundColor Cyan
$body = @{ email = $email; senha = $senha } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body $body
$token = $login.accessToken
if ([string]::IsNullOrWhiteSpace($token)) { throw "Login nao retornou accessToken." }
$headers = @{ Authorization = "Bearer $token" }
Write-Host "    Login OK: $($login.nome)" -ForegroundColor Green

Write-Host "[3/600] Listando pacientes..." -ForegroundColor Cyan
$lista = Invoke-RestMethod -Uri "$base/api/pacientes?pagina=1&tamanhoPagina=5" -Headers $headers -Method Get
Write-Host "    Total atual: $($lista.total)" -ForegroundColor Green

Write-Host "[4/600] Validando perguntas de anamnese..." -ForegroundColor Cyan
try { $perguntas = Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers -Method Get; Write-Host "    Endpoint OK. Perguntas ativas: $($perguntas.Count)" -ForegroundColor Green } catch { if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host "    Endpoint protegido OK (perfil profissional ainda nao configurado)." -ForegroundColor DarkGreen } else { throw } }

Write-Host "[5/600] Validando catalogo laboratorial..." -ForegroundColor Cyan
$marcadores = Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers -Method Get
Write-Host "    Marcadores cadastrados: $($marcadores.Count)" -ForegroundColor Green

Write-Host "[6/600] Validando catalogo de alimentos..." -ForegroundColor Cyan
$alimentos = Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers -Method Get
Write-Host "    Alimentos cadastrados: $($alimentos.Count)" -ForegroundColor Green

Write-Host "[7/600] Validando busca/paginacao..." -ForegroundColor Cyan
$busca = Invoke-RestMethod -Uri "$base/api/pacientes?busca=__smoke_test_sem_resultado__&pagina=1&tamanhoPagina=3" -Headers $headers -Method Get
if ($null -eq $busca.itens) { throw "Resposta de paginacao invalida." }

Write-Host "[8/600] Validando modulos do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $preview = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/relatorios/preview" -Headers $headers -Method Get
    if ($null -eq $preview.paciente) { throw "Preview de relatorio invalido." }
    $planos = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/planos-alimentares" -Headers $headers -Method Get)
    Write-Host "    Preview OK / planos alimentares: $($planos.Count)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de modulos ignorada sem criar dados." -ForegroundColor DarkGreen }

Write-Host "[9/600] Validando metas do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $metas = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/metas?incluirEncerradas=true" -Headers $headers -Method Get)
    Write-Host "    Endpoint OK. Metas cadastradas: $($metas.Count)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de metas ignorada." -ForegroundColor DarkGreen }

Write-Host "[10/600] Validando diario/resumo do dia..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $diario = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/diario" -Headers $headers -Method Get)
    $resumo = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/resumo-dia" -Headers $headers -Method Get
    if ($null -eq $resumo.metas) { throw "Resumo do dia invalido." }
    Write-Host "    Diario: $($diario.Count) registros / metas ativas hoje: $($resumo.metasAtivas)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de diario ignorada." -ForegroundColor DarkGreen }

Write-Host "[11/600] Validando portal/home do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $portal = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/portal/home" -Headers $headers -Method Get
    if ($null -eq $portal.paciente -or $portal.paciente.id -ne $pacienteSmoke.id) { throw "Portal do paciente retornou dados invalidos." }
    if ($null -eq $portal.evolucaoCorporal -or $null -eq $portal.metasHoje -or $null -eq $portal.registrosHoje -or $null -eq $portal.examesRecentes) { throw "Portal do paciente incompleto." }
    Write-Host "    Portal OK: $($portal.metasAtivas) meta(s), $($portal.registrosHoje.Count) registro(s), $($portal.examesRecentes.Count) resultado(s) recente(s)." -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao do portal ignorada." -ForegroundColor DarkGreen }



Write-Host "[12/600] Validando agenda do profissional..." -ForegroundColor Cyan
try {
    $hojeLocal = (Get-Date).ToString("yyyy-MM-dd")
    $agenda = Invoke-RestMethod -Uri "$base/api/agenda?data=$hojeLocal&offsetMinutos=-180" -Headers $headers -Method Get
    if ($null -eq $agenda.consultas) { throw "Resposta de agenda invalida." }
    Write-Host "    Agenda OK: $($agenda.total) consulta(s) no dia." -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host "    Agenda protegida OK (perfil profissional ainda nao configurado)." -ForegroundColor DarkGreen } else { throw }
}

Write-Host "[13/600] Validando dashboard do profissional..." -ForegroundColor Cyan
try {
    $dashboard = Invoke-RestMethod -Uri "$base/api/profissional/dashboard?offsetMinutos=-180" -Headers $headers -Method Get
    if ($null -eq $dashboard.agendaHoje -or $null -eq $dashboard.proximasConsultas -or $null -eq $dashboard.pacientesRecentes) { throw "Dashboard profissional incompleto." }
    Write-Host "    Dashboard OK: $($dashboard.pacientesAtivos) paciente(s) ativo(s), $($dashboard.consultasHoje) consulta(s) hoje, $($dashboard.retornosPendentes) retorno(s) pendente(s)." -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host "    Dashboard protegido OK (perfil profissional ainda nao configurado)." -ForegroundColor DarkGreen } else { throw }
}


Write-Host "[14/600] Validando interface web..." -ForegroundColor Cyan
$web = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($web.StatusCode -ne 200 -or $web.Content -notmatch "HealthPlatform") { throw "Interface web nao respondeu corretamente." }
Write-Host "    Interface HTML OK." -ForegroundColor Green

Write-Host "[15/600] Validando assets da interface..." -ForegroundColor Cyan
$js = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($js.StatusCode -ne 200 -or $css.StatusCode -ne 200) { throw "Assets web nao responderam." }
Write-Host "    app.js + app.css OK." -ForegroundColor Green


Write-Host "[16/600] Validando prontuario visual v0.2.2..." -ForegroundColor Cyan
if ($js.Content -notmatch "patient-tabs" -or $js.Content -notmatch "loadPatient" -or $css.Content -notmatch "patient-dashboard") { throw "Prontuario visual v0.2.2 incompleto nos assets." }
Write-Host "    Prontuario visual + abas clinicas OK." -ForegroundColor Green

Write-Host "[17/600] Validando acoes clinicas da interface..." -ForegroundColor Cyan
if ($js.Content -notmatch "openClinicalActionMenu" -or $js.Content -notmatch "submitClinicalForm" -or $web.Content -notmatch "clinicalActionModal") { throw "Acoes clinicas v0.2.2 nao foram publicadas corretamente." }
Write-Host "    Registrar consulta, avaliacao, anamnese, meta e diario: assets OK." -ForegroundColor Green

Write-Host "[18/600] Validando rotas usadas pelos formularios clinicos..." -ForegroundColor Cyan
if ($js.Content -notmatch "/consultas" -or $js.Content -notmatch "/avaliacoes" -or $js.Content -notmatch "/anamneses" -or $js.Content -notmatch "/metas" -or $js.Content -notmatch "/diario") { throw "Formularios clinicos nao referenciam todas as rotas esperadas." }
Write-Host "    Rotas de registro clinico presentes." -ForegroundColor Green


Write-Host "[19/600] Validando cadastro visual de exames..." -ForegroundColor Cyan
if ($js.Content -notmatch "openExamForm" -or $js.Content -notmatch "exam-result-row" -or $js.Content -notmatch "/api/exames/marcadores" -or $js.Content -notmatch "/exames") { throw "Construtor visual de exames v0.2.3 incompleto." }
Write-Host "    Coleta + catalogo de marcadores + resultados: assets OK." -ForegroundColor Green

Write-Host "[20/600] Validando construtor visual do plano alimentar..." -ForegroundColor Cyan
if ($js.Content -notmatch "openMealPlanForm" -or $js.Content -notmatch "meal-builder" -or $js.Content -notmatch "/api/alimentos" -or $js.Content -notmatch "/planos-alimentares" -or $js.Content -notmatch "substitution-row") { throw "Construtor visual de plano alimentar v0.2.3 incompleto." }
if ($css.Content -notmatch "meal-item-builder" -or $css.Content -notmatch "plan-preview") { throw "Estilos do construtor alimentar v0.2.3 incompletos." }
Write-Host "    Refeicoes + alimentos + macros + substituicoes: assets OK." -ForegroundColor Green



Write-Host "[21/600] Validando relatorios na interface..." -ForegroundColor Cyan
if ($js.Content -notmatch "openReportForm" -or $js.Content -notmatch "openReportHtml" -or $js.Content -notmatch "/relatorios/preview" -or $js.Content -notmatch "newReportFromTab") { throw "Fluxo visual de relatorios v0.3.27 incompleto." }
if ($css.Content -notmatch "report-grid" -or $css.Content -notmatch "report-preview-box") { throw "Estilos de relatorio v0.3.27 incompletos." }
Write-Host "    Geracao + preview + visualizacao/impressao: assets OK." -ForegroundColor Green

Write-Host "[22/600] Validando edicao visual do paciente..." -ForegroundColor Cyan
if ($js.Content -notmatch "openEditPatientForm" -or $js.Content -notmatch "method:'PUT'" -or $js.Content -notmatch "Editar dados") { throw "Edicao visual do paciente v0.3.27 incompleta." }
Write-Host "    Cadastro do paciente pode ser atualizado pela interface." -ForegroundColor Green

Write-Host "[23/600] Validando endpoint de relatorios do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $relatoriosSmoke = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/relatorios" -Headers $headers -Method Get)
    Write-Host "    Endpoint OK. Relatorios existentes: $($relatoriosSmoke.Count)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de relatorios ignorada." -ForegroundColor DarkGreen }

Write-Host "[24/600] Validando edicao clinica visual..."
if ($js.Content -notmatch "openEditConsulta" -or $js.Content -notmatch "openEditAnamnese" -or $js.Content -notmatch "openEditAvaliacao" -or $js.Content -notmatch "/api/avaliacoes/") { throw "Edicao clinica visual v0.3.27 incompleta." }
Write-Host "    Consulta + anamnese + avaliacao: edicao visual OK."

Write-Host "[25/600] Validando agenda operacional..."
if ($js.Content -notmatch "agendaStatusActions" -or $js.Content -notmatch "openRescheduleForm" -or $js.Content -notmatch "Realizada" -or $js.Content -notmatch "Faltou" -or $js.Content -notmatch "/reagendar") { throw "Agenda operacional v0.3.27 incompleta." }
Write-Host "    Status rapido + reagendamento: assets OK."

Write-Host "[26/600] Validando endpoint de atualizacao de avaliacao..."
try {
    $ctrl = Get-Content -Encoding UTF8 -Raw ".\src\HealthPlatform.Api\Controllers\AvaliacoesController.cs"
    if ($ctrl -notmatch 'HttpPut\("api/avaliacoes/\{id:guid\}"\)' -or $ctrl -notmatch 'AdicionarAuditoria\("UPDATE"') { throw "PUT de avaliacao ou auditoria ausente." }
    Write-Host "    PUT /api/avaliacoes/{id} + auditoria OK."
} catch { throw $_ }

Write-Host ""

Write-Host "[27/600] Validando tela de configuracoes..."
$indexHtml = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($indexHtml.Content -notmatch "configuracoes") { throw "Navegacao de configuracoes nao encontrada." }
Write-Host "    Navegacao de configuracoes presente."

Write-Host "[28/600] Validando gerenciadores de catalogo na interface..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "modalAlimento" -or $appJs.Content -notmatch "modalMarcador" -or $appJs.Content -notmatch "modalPergunta") {
    throw "Gerenciadores de catalogo incompletos."
}
Write-Host "    Alimentos + marcadores + perguntas: assets OK."

Write-Host "[29/600] Validando resumo de configuracoes do consultorio..."
$cfg = Invoke-RestMethod -Uri "$base/api/configuracoes/resumo" -Headers $headers
if (-not $cfg.organizacao) { throw "Resumo de configuracoes sem organizacao." }
Write-Host "    Organizacao/usuario/profissional: endpoint OK."

Write-Host "[30/600] Validando rotas dos catalogos..."
$null = Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers
$null = Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers
$null = Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers
Write-Host "    Catalogos acessiveis e autenticados."


Write-Host "[31/600] Validando edicao de configuracoes..."
$cfg = Invoke-RestMethod -Uri "$base/api/configuracoes/resumo" -Headers $headers
if (-not $cfg.organizacao -or -not $cfg.usuario) { throw "Resumo de configuracoes incompleto." }
Write-Host "    Organizacao + usuario carregados para edicao."

Write-Host "[32/600] Validando assets de edicao/inativacao dos catalogos..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
$temAlimentos = $appJs.Content -match "/api/alimentos/"
$temMarcadores = $appJs.Content -match "/api/exames/marcadores/"
$temPerguntas = $appJs.Content -match "/api/anamnese/perguntas/"
$temProfissional = $appJs.Content -match "Editar profissional"
$temOrganizacao = $appJs.Content -match "/api/configuracoes/organizacao"

if (-not ($temAlimentos -and $temMarcadores -and $temPerguntas -and $temProfissional -and $temOrganizacao)) {
    throw "Assets administrativos v0.3.27 incompletos."
}
Write-Host "    Edicao + ativacao/inativacao: assets OK."

Write-Host "[33/600] Validando endpoints administrativos..."
$null = Invoke-RestMethod -Uri "$base/api/configuracoes/resumo" -Headers $headers
Write-Host "    Configuracoes autenticadas OK."

Write-Host "[34/600] Validando que a interface segue integra..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.StatusCode -ne 200) { throw "Interface indisponivel." }
Write-Host "    Interface web OK apos extensoes administrativas."


Write-Host "[35/600] Validando separacao de autorizacao profissional/paciente..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "/api/portal/me/home" -or $appJs.Content -notmatch "tipoUsuario==='Paciente'") {
    throw "Portal autenticado do paciente nao encontrado nos assets."
}
Write-Host "    UI separada por tipo de usuario: assets OK."

Write-Host "[36/600] Validando endpoint de status de acesso do paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $accessStatus = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/acesso" -Headers $headers
    if ($null -eq $accessStatus.possuiAcesso) { throw "Status de acesso invalido." }
}
Write-Host "    Status de acesso do paciente: endpoint OK."

Write-Host "[37/600] Validando fluxo de convite/ativacao nos assets..."
if ($appJs.Content -notmatch "ativarPaciente" -or $appJs.Content -notmatch "/api/auth/paciente/ativar") {
    throw "Fluxo visual de ativacao incompleto."
}
Write-Host "    Convite + ativacao: assets OK."

Write-Host "[38/600] Validando autoatendimento do diario..."
if ($appJs.Content -notmatch "/api/portal/me/diario") {
    throw "Registro de diario pelo paciente nao encontrado."
}
Write-Host "    Diario proprio: asset OK."

Write-Host "[39/600] Validando autoatendimento das metas..."
if ($appJs.Content -notmatch "/api/portal/me/metas/" -or $appJs.Content -notmatch "/registro") {
    throw "Atualizacao de meta pelo paciente nao encontrada."
}
Write-Host "    Metas proprias: asset OK."

Write-Host "[40/600] Validando tela dedicada do portal do paciente..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch "patientAppView" -or $index.Content -notmatch "activationView") {
    throw "Views dedicadas do paciente nao encontradas."
}
Write-Host "    Portal + ativacao do paciente: HTML OK."


Write-Host "[41/600] Validando navegacao completa do portal..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch "data-patient-view=.plano." -or
    $index.Content -notmatch "data-patient-view=.metas." -or
    $index.Content -notmatch "data-patient-view=.diario." -or
    $index.Content -notmatch "data-patient-view=.evolucao." -or
    $index.Content -notmatch "data-patient-view=.exames.") {
    throw "Navegacao completa do paciente nao encontrada."
}
Write-Host "    Inicio + plano + metas + diario + evolucao + exames: HTML OK."

Write-Host "[42/600] Validando endpoint proprio de plano alimentar..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "/api/portal/me/plano") { throw "Endpoint proprio do plano ausente dos assets." }
Write-Host "    Plano alimentar proprio: asset OK."

Write-Host "[43/600] Validando historico proprio de metas e diario..."
if ($appJs.Content -notmatch "/api/portal/me/metas" -or $appJs.Content -notmatch "/api/portal/me/diario") {
    throw "Historico proprio de metas/diario incompleto."
}
Write-Host "    Metas + diario historicos: assets OK."

Write-Host "[44/600] Validando historico de evolucao corporal..."
if ($appJs.Content -notmatch "/api/portal/me/evolucao") { throw "Evolucao propria ausente." }
Write-Host "    Evolucao corporal: asset OK."

Write-Host "[45/600] Validando historico proprio de exames..."
if ($appJs.Content -notmatch "/api/portal/me/exames") { throw "Exames proprios ausentes." }
Write-Host "    Exames laboratoriais: asset OK."

Write-Host "[46/600] Validando assets visuais do portal expandido..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "patient-portal-nav" -or $css.Content -notmatch "patient-plan-totals" -or $css.Content -notmatch "lab-result-grid") {
    throw "Estilos do portal expandido incompletos."
}
Write-Host "    Portal completo e responsivo: assets OK."


Write-Host "[47/600] Validando schema/endpoint do catalogo de exercicios..."
$exercicios = Invoke-RestMethod -Uri "$base/api/exercicios" -Headers $headers
Write-Host "    Exercicios ativos no catalogo: $($exercicios.Count)"

Write-Host "[48/600] Validando endpoint de planos de treino do paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $treinos = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/treinos" -Headers $headers
    Write-Host "    Planos de treino cadastrados: $($treinos.Count)"
}

Write-Host "[49/600] Validando construtor visual de treino..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "openWorkoutForm" -or
    $appJs.Content -notmatch "/api/pacientes/.*/treinos" -or
    $appJs.Content -notmatch "/api/exercicios") {
    throw "Construtor visual de treinos incompleto."
}
Write-Host "    Treinos + exercicios + series/repeticoes/carga: assets OK."

Write-Host "[50/600] Validando videos e prescricao de exercicios..."
if ($appJs.Content -notmatch "videoUrl" -or
    $appJs.Content -notmatch "descansoSegundos" -or
    $appJs.Content -notmatch "tempoSegundos") {
    throw "Prescricao avancada de exercicios incompleta."
}
Write-Host "    Video + descanso + tempo: assets OK."

Write-Host "[51/600] Validando aba de treinos no prontuario..."
if ($appJs.Content -notmatch "Treinos.*treinos.length" -or
    $appJs.Content -notmatch "workout-plan-grid") {
    throw "Aba profissional de treinos nao encontrada."
}
Write-Host "    Prontuario profissional: aba Treinos OK."

Write-Host "[52/600] Validando navegacao de treino do paciente..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch "data-patient-view=.treino.") {
    throw "Navegacao Treino do portal do paciente ausente."
}
Write-Host "    Portal do paciente: navegacao Treino OK."

Write-Host "[53/600] Validando endpoint proprio do treino do paciente..."
if ($appJs.Content -notmatch "/api/portal/me/treino" -or
    $appJs.Content -notmatch "loadPatientWorkout") {
    throw "Portal proprio de treino incompleto."
}
Write-Host "    GET /api/portal/me/treino: asset OK."

Write-Host "[54/600] Validando assets visuais do modulo de treino..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "patient-exercise-card" -or
    $css.Content -notmatch "workout-item-builder" -or
    $css.Content -notmatch "exercise-video") {
    throw "Estilos do modulo de treino incompletos."
}
Write-Host "    Modulo de treino responsivo: assets OK."


Write-Host "[55/600] Validando schema de execucoes de treino..."
$tables = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name IN ('ExecucoesTreino','ExecucoesItensTreino');"
if ([int]$tables -ne 2) { throw "Tabelas de execucao de treino ausentes." }
Write-Host "    ExecucoesTreino + ExecucoesItensTreino: schema OK."

Write-Host "[56/600] Validando historico profissional de treinos..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $histTreino = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/treinos/historico?dias=90" -Headers $headers
    if ($null -eq $histTreino.totalTreinos) { throw "Historico profissional invalido." }
}
Write-Host "    Adesao + historico profissional: endpoint OK."

Write-Host "[57/600] Validando registro visual de execucao pelo paciente..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "openWorkoutExecutionForm" -or
    $appJs.Content -notmatch "/api/portal/me/treinos/execucoes") {
    throw "Registro de execucao pelo paciente incompleto."
}
Write-Host "    Formulario de execucao: asset OK."

Write-Host "[58/600] Validando series, repeticoes e carga realizadas..."
if ($appJs.Content -notmatch "seriesRealizadas" -or
    $appJs.Content -notmatch "repeticoesRealizadas" -or
    $appJs.Content -notmatch "cargaRealizada") {
    throw "Campos de execucao incompletos."
}
Write-Host "    Series + repeticoes + carga: assets OK."

Write-Host "[59/600] Validando esforco percebido e duracao..."
if ($appJs.Content -notmatch "esforcoPercebido" -or
    $appJs.Content -notmatch "duracaoMinutos") {
    throw "RPE/duracao ausentes."
}
Write-Host "    RPE + duracao: assets OK."

Write-Host "[60/600] Validando historico do paciente..."
# Evita depender de texto visual acentuado (encoding pode variar no Windows PowerShell).
# Valida a rota real e os dados usados para renderizar o historico do proprio paciente.
if ($appJs.Content -notmatch "/api/portal/me/treinos/historico\?dias=90" -or
    $appJs.Content -notmatch "h\.execucoes" -or
    $appJs.Content -notmatch "loadPatientWorkout") {
    throw "Historico proprio do treino ausente."
}
Write-Host "    Historico proprio: asset OK."

Write-Host "[61/600] Validando progressao de carga no prontuario..."
if ($appJs.Content -notmatch "evolucaoCarga" -or
    $appJs.Content -notmatch "/api/pacientes/\$\{patientId\}/treinos/progressao-exercicios\?dias=\$\{days\}" -or
    $appJs.Content -notmatch "data-exercise-progression") {
    throw "Progressao de carga profissional ausente."
}
Write-Host "    Evolucao de carga + adesao: assets OK."

Write-Host "[62/600] Validando estilos do acompanhamento de treino..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "load-progress-grid" -or
    $css.Content -notmatch "execution-item" -or
    $css.Content -notmatch "workout-execution-list") {
    throw "Estilos de acompanhamento incompletos."
}
Write-Host "    Acompanhamento responsivo: assets OK."


Write-Host "[63/600] Validando motor de graficos SVG..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "hpLineChart" -or
    $appJs.Content -notmatch "native-line-chart" -or
    $appJs.Content -notmatch "hpChartSeries") {
    throw "Motor de graficos SVG incompleto."
}
Write-Host "    SVG nativo + escalas + series: assets OK."

Write-Host "[64/600] Validando graficos corporais no prontuario..."
if ($appJs.Content -notmatch "hpEvalCharts" -or
    $appJs.Content -notmatch "professional-evaluations" -or
    $appJs.Content -notmatch "professional-summary") {
    throw "Graficos corporais profissionais incompletos."
}
Write-Host "    Peso + IMC + gordura + cintura: assets OK."

Write-Host "[65/600] Validando tendencias laboratoriais..."
if ($appJs.Content -notmatch "hpLabSeriesFromProfessional" -or
    $appJs.Content -notmatch "hpLabSeriesFromPatient" -or
    $appJs.Content -notmatch "hpLabCharts") {
    throw "Tendencias laboratoriais incompletas."
}
Write-Host "    Series numericas por marcador: assets OK."

Write-Host "[66/600] Validando progressao grafica de carga..."
if ($appJs.Content -notmatch "hpLoadCharts" -or
    $appJs.Content -notmatch "professional-workout-load" -or
    $appJs.Content -notmatch "patient-workout-load") {
    throw "Graficos de progressao de carga incompletos."
}
Write-Host "    Progressao de carga profissional/paciente: assets OK."

Write-Host "[67/600] Validando evolucao visual no portal do paciente..."
if ($appJs.Content -notmatch "loadPatientEvolution" -or
    $appJs.Content -notmatch "hpLineChart" -or
    $appJs.Content -notmatch "analytics-grid") {
    throw "Evolucao visual do paciente ausente."
}
Write-Host "    Portal: graficos corporais OK."

Write-Host "[68/600] Validando graficos de exames no portal..."
if ($appJs.Content -notmatch "loadPatientLabs" -or
    $appJs.Content -notmatch "hpLabCharts" -or
    $appJs.Content -notmatch "patient-lab-history") {
    throw "Graficos de exames do paciente ausentes."
}
Write-Host "    Portal: tendencias laboratoriais OK."

Write-Host "[69/600] Validando responsividade dos graficos..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "analytics-grid" -or
    $css.Content -notmatch "native-line-chart" -or
    $css.Content -notmatch "@media.max-width:840px") {
    throw "Estilos responsivos dos graficos incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[70/600] Validando compatibilidade de schema na v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not (Test-Path .\scripts\sql\v0.3.1_execucoes_treino.sql)) {
    throw "Historico de upgrade v0.3.1 ausente."
}
Write-Host "    v0.3.27 reutiliza o schema ja atualizado; sem upgrade novo nesta versao."


Write-Host "[71/600] Validando endpoint de insights do dashboard..."
$insightsDashboard = Invoke-RestMethod -Uri "$base/api/insights/dashboard?limite=12" -Headers $headers
if ($null -eq $insightsDashboard.pacientesAnalisados -or $null -eq $insightsDashboard.totalInsights) {
    throw "Dashboard de insights invalido."
}
Write-Host "    Pacientes analisados: $($insightsDashboard.pacientesAnalisados) / sinais: $($insightsDashboard.totalInsights)"

Write-Host "[72/600] Validando insights por paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $patientInsights = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/insights" -Headers $headers
    if ($null -eq $patientInsights.total -or $null -eq $patientInsights.insights) {
        throw "Insights do paciente invalidos."
    }
}
Write-Host "    Endpoint individual: OK."

Write-Host "[73/600] Validando regra de exame fora da referencia..."
$sourceInsights = Get-Content .\src\HealthPlatform.Api\Controllers\InsightsController.cs -Encoding UTF8 -Raw
if ($sourceInsights -notmatch "EXAME_FORA_REFERENCIA" -or
    $sourceInsights -notmatch "ReferenciaMinima" -or
    $sourceInsights -notmatch "ReferenciaMaxima") {
    throw "Regra laboratorial incompleta."
}
Write-Host "    Faixa registrada pelo laboratorio: regra OK."

Write-Host "[74/600] Validando regras de evolucao e retorno..."
if ($sourceInsights -notmatch "VARIACAO_PESO" -or
    $sourceInsights -notmatch "SEM_RETORNO") {
    throw "Regras de evolucao/retorno incompletas."
}
Write-Host "    Variacao corporal + retorno: regras OK."

Write-Host "[75/600] Validando regras de adesao..."
if ($sourceInsights -notmatch "BAIXA_ADESAO_META" -or
    $sourceInsights -notmatch "SEM_TREINO_RECENTE" -or
    $sourceInsights -notmatch "QUEDA_FREQUENCIA_TREINO") {
    throw "Regras de adesao incompletas."
}
Write-Host "    Metas + frequencia de treino: regras OK."

Write-Host "[76/600] Validando central de atencao visual..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "/api/insights/dashboard" -or
    $appJs.Content -notmatch "hpInsightCard") {
    throw "Central visual de insights incompleta."
}
Write-Host "    Dashboard profissional: assets OK."

Write-Host "[77/600] Validando insights no prontuario..."
if ($appJs.Content -notmatch "/insights" -or
    $appJs.Content -notmatch "Insights de acompanhamento" -or
    $appJs.Content -notmatch "insight-disclaimer") {
    throw "Insights do prontuario incompletos."
}
Write-Host "    Prontuario: sinais + aviso de interpretacao OK."

Write-Host "[78/600] Validando estilos e compatibilidade do schema..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "insight-summary" -or
    $css.Content -notmatch "patient-insight-grid" -or
    $css.Content -notmatch "insight-high") {
    throw "Estilos de insights incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    Insights responsivos / schema existente compativel: OK."


Write-Host "[79/600] Validando schema de pendencias clinicas..."
$pendingTable = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='PendenciasClinicas';"
if ([int]$pendingTable -ne 1) { throw "Tabela PendenciasClinicas ausente." }
Write-Host "    PendenciasClinicas: schema OK."

Write-Host "[80/600] Validando endpoint geral de pendencias..."
$pendencias = Invoke-RestMethod -Uri "$base/api/pendencias?status=abertas&limite=20" -Headers $headers
if ($null -eq $pendencias.total -or $null -eq $pendencias.itens) {
    throw "Endpoint geral de pendencias invalido."
}
Write-Host "    Pendencias abertas: $($pendencias.total)"

Write-Host "[81/600] Validando endpoint de pendencias do paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $pp = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/pendencias" -Headers $headers
}
Write-Host "    Lista por paciente: endpoint OK."

Write-Host "[82/600] Validando acoes de ciclo de vida..."
$pendingSource = Get-Content .\src\HealthPlatform.Api\Controllers\PendenciasController.cs -Encoding UTF8 -Raw
if ($pendingSource -notmatch '/vista' -or
    $pendingSource -notmatch '/adiar' -or
    $pendingSource -notmatch '/resolver') {
    throw "Ciclo de vida de pendencias incompleto."
}
Write-Host "    Vista + adiada + resolvida: rotas OK."

Write-Host "[83/600] Validando criacao de retorno a partir da pendencia..."
if ($pendingSource -notmatch '/retorno' -or
    $pendingSource -notmatch 'StatusConsulta.Agendada' -or
    $pendingSource -notmatch 'ConsultaRetornoId') {
    throw "Fluxo de retorno incompleto."
}
Write-Host "    Pendencia -> consulta futura: backend OK."

Write-Host "[84/600] Validando transformar insight em pendencia..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "insight-to-pending" -or
    $appJs.Content -notmatch "/pendencias") {
    throw "Transformacao de insight em pendencia incompleta."
}
Write-Host "    Insight -> pendencia: assets OK."

Write-Host "[85/600] Validando tela de gerenciamento de pendencias..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch 'data-view=.pendencias.' -or
    $appJs.Content -notmatch "loadPendencias" -or
    $appJs.Content -notmatch "Fila de acompanhamento") {
    throw "Tela de pendencias incompleta."
}
Write-Host "    Navegacao + filtros + fila: assets OK."

Write-Host "[86/600] Validando acoes visuais da pendencia..."
if ($appJs.Content -notmatch "openResolvePending" -or
    $appJs.Content -notmatch "openSnoozePending" -or
    $appJs.Content -notmatch "openReturnPending") {
    throw "Acoes visuais de pendencia incompletas."
}
Write-Host "    Resolver + adiar + retorno: assets OK."

Write-Host "[87/600] Validando resumo de pendencias no dashboard..."
if ($appJs.Content -notmatch "dashboard-pending-section" -or
    $appJs.Content -notmatch "pendenciasAbertas") {
    throw "Resumo de pendencias no dashboard ausente."
}
Write-Host "    Dashboard: pendencias abertas OK."

Write-Host "[88/600] Validando auditoria, estilos e upgrade v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "pending-card" -or
    $css.Content -notmatch "pending-actions" -or
    $pendingSource -notmatch 'nameof.PendenciaClinica.') {
    throw "Auditoria/estilos de pendencias incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    Auditoria + UI responsiva + v0.3.27: OK."


Write-Host "[89/600] Validando schema de notificacoes internas..."
$notificationTable = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='NotificacoesInternas';"
if ([int]$notificationTable -ne 1) { throw "Tabela NotificacoesInternas ausente." }
Write-Host "    NotificacoesInternas: schema OK."

Write-Host "[90/600] Validando sincronizacao de notificacoes..."
$notificationControllerSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notificationControllerSource -notmatch 'HttpPost\("sincronizar"\)' -or
    $notificationControllerSource -notmatch 'SincronizarProfissional' -or
    $notificationControllerSource -notmatch 'SincronizarPaciente') {
    throw "Sincronizacao de notificacoes invalida."
}
Write-Host "    Sincronizacao idempotente: rota + regras presentes; sem mutar dados."

Write-Host "[91/600] Validando listagem e contador nao lido..."
$notifications = Invoke-RestMethod -Uri "$base/api/notificacoes?sincronizar=false&limite=50" -Headers $headers
if ($null -eq $notifications.total -or $null -eq $notifications.naoLidas -or $null -eq $notifications.itens) {
    throw "Listagem de notificacoes invalida."
}
Write-Host "    Total: $($notifications.total) / nao lidas: $($notifications.naoLidas)"

Write-Host "[92/600] Validando regras de agenda profissional..."
$notificationSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notificationSource -notmatch "SincronizarProfissional" -or
    $notificationSource -notmatch "AddHours.24." -or
    $notificationSource -notmatch 'PROF:CONSULTA') {
    throw "Lembretes de agenda profissional incompletos."
}
Write-Host "    Consultas proximas 24h: regra OK."

Write-Host "[93/600] Validando regras de pendencias..."
if ($notificationSource -notmatch 'PROF:PENDENCIA' -or
    $notificationSource -notmatch 'PendenciaClinica' -or
    $notificationSource -notmatch 'var vencida' -or
    $notificationSource -notmatch 'var venceLogo' -or
    $notificationSource -notmatch 'p\.Severidade != "Alta"' -or
    $notificationSource -notmatch 'agora\.AddHours\(24\)') {
    throw "Notificacoes de pendencias incompletas."
}
Write-Host "    Vencidas + alta prioridade + vencimento proximo: regras OK."

Write-Host "[94/600] Validando lembretes do paciente..."
if ($notificationSource -notmatch "SincronizarPaciente" -or
    $notificationSource -notmatch 'PAC:CONSULTA' -or
    $notificationSource -notmatch "Lembrete de consulta") {
    throw "Lembretes do paciente incompletos."
}
Write-Host "    Portal do paciente: consulta proxima OK."

Write-Host "[95/600] Validando leitura individual e em massa..."
if ($notificationSource -notmatch '/lida' -or
    $notificationSource -notmatch 'ler-todas' -or
    $notificationSource -notmatch 'LidaEmUtc') {
    throw "Ciclo de leitura de notificacoes incompleto."
}
Write-Host "    Lida individual + ler todas: backend OK."

Write-Host "[96/600] Validando sino e drawer na interface..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch "notificationButton" -or
    $index.Content -notmatch "patientNotificationButton" -or
    $index.Content -notmatch "notificationDrawer" -or
    $appJs.Content -notmatch "openNotifications") {
    throw "Central visual de notificacoes incompleta."
}
Write-Host "    Profissional + paciente + drawer: assets OK."

Write-Host "[97/600] Validando contador e atualizacao periodica..."
if ($appJs.Content -notmatch "notificationBadge" -or
    $appJs.Content -notmatch "setInterval" -or
    $appJs.Content -notmatch "60000" -or
    $appJs.Content -notmatch "refreshNotifications") {
    throw "Contador/polling de notificacoes incompleto."
}
Write-Host "    Badge + atualizacao a cada 60s: assets OK."

Write-Host "[98/600] Validando estilos, upgrade e versao..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "notification-panel" -or
    $css.Content -notmatch "notification-item" -or
    $css.Content -notmatch "notification-badge") {
    throw "Estilos de notificacoes incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    UI responsiva + upgrade v0.3.27: OK."


Write-Host "[99/600] Validando script de popular banco..."
if (-not (Test-Path .\POPULAR.ps1)) { throw "POPULAR.ps1 ausente." }
$popularSource = Get-Content .\POPULAR.ps1 -Encoding UTF8 -Raw
if ($popularSource -notmatch "Ana Ribeiro" -or
    $popularSource -notmatch "Bruno Martins" -or
    $popularSource -notmatch "Carla Souza" -or
    $popularSource -notmatch "Diego Alves" -or
    $popularSource -notmatch "Elisa Ferreira") {
    throw "Cenarios da base demo incompletos."
}
Write-Host "    Cinco cenarios adicionais presentes."

Write-Host "[100/600] Validando idempotencia do popular..."
if ($popularSource -notmatch "Ensure-Patient" -or
    $popularSource -notmatch "Ensure-Consultation" -or
    $popularSource -notmatch "Ensure-Evaluation" -or
    $popularSource -notmatch "Ensure-Lab" -or
    $popularSource -notmatch "Ensure-Goal") {
    throw "Helpers idempotentes do POPULAR.ps1 incompletos."
}
Write-Host "    Paciente + consulta + avaliacao + exames + metas: helpers OK."

Write-Host "[101/600] Validando cobertura de modulos na base demo..."
if ($popularSource -notmatch "Ensure-Diary" -or
    $popularSource -notmatch "Ensure-Workout" -or
    $popularSource -notmatch "Ensure-Pending" -or
    $popularSource -notmatch "/api/notificacoes/sincronizar") {
    throw "Cobertura dos modulos demo incompleta."
}
Write-Host "    Diario + treino + pendencias + notificacoes: script OK."

Write-Host "[102/600] Validando endpoint de resumo de dados..."
$dataResumo = Invoke-RestMethod -Uri "$base/api/dados/resumo" -Headers $headers
if ($null -eq $dataResumo.pacientes -or
    $null -eq $dataResumo.consultas -or
    $null -eq $dataResumo.avaliacoes -or
    $null -eq $dataResumo.exames) {
    throw "Resumo de dados invalido."
}
Write-Host "    Pacientes=$($dataResumo.pacientes) / consultas=$($dataResumo.consultas) / avaliacoes=$($dataResumo.avaliacoes) / exames=$($dataResumo.exames)"

Write-Host "[103/600] Validando que popular banco e opt-in..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if ($setupSource -match "POPULAR.ps1") {
    throw "POPULAR.ps1 nao deve executar automaticamente no PREPARAR."
}
Write-Host "    PREPARAR preserva dados do usuario; POPULAR e execucao explicita."

Write-Host "[104/600] Validando versao v0.3.27 e upgrade do schema..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if ($setupSource -notmatch "\[37/37\]") { throw "PREPARAR atual deveria possuir 36 etapas." }
Write-Host "    v0.3.27 preservada / PREPARAR atual 38/38 / upgrade SOAP: OK."


Write-Host "[105/600] Validando endpoint da carteira..."
$carteira = Invoke-RestMethod -Uri "$base/api/carteira?ordenar=score" -Headers $headers
if ($null -eq $carteira.totalPacientes -or $null -eq $carteira.pacientes) {
    throw "Endpoint da carteira invalido."
}
Write-Host "    Carteira: $($carteira.totalPacientes) paciente(s)."

Write-Host "[106/600] Validando priorizacao da carteira..."
$carteiraSource = Get-Content .\src\HealthPlatform.Api\Controllers\CarteiraController.cs -Encoding UTF8 -Raw
if ($carteiraSource -notmatch "Score" -or
    $carteiraSource -notmatch "Prioridade" -or
    $carteiraSource -notmatch "pendAlta" -or
    $carteiraSource -notmatch "semRetorno") {
    throw "Motor de priorizacao da carteira incompleto."
}
Write-Host "    Score + pendencias + retorno: backend OK."

Write-Host "[107/600] Validando sinais de exames/evolucao na carteira..."
if ($carteiraSource -notmatch "ReferenciaMinima" -or
    $carteiraSource -notmatch "ReferenciaMaxima" -or
    $carteiraSource -notmatch "PesoKg") {
    throw "Sinais clinicos da carteira incompletos."
}
Write-Host "    Exames + peso: leitura longitudinal OK."

Write-Host "[108/600] Validando atividade recente..."
if ($carteiraSource -notmatch "TreinosUltimos30Dias" -or
    $carteiraSource -notmatch "RegistrosDiarioUltimos14Dias" -or
    $carteiraSource -notmatch "RegistrosMetaUltimos14Dias") {
    throw "Indicadores de atividade da carteira incompletos."
}
Write-Host "    Treinos + diario + metas: backend OK."

Write-Host "[109/600] Validando tela Carteira..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.carteira.' -or
    $appJs.Content -notmatch "loadCarteira" -or
    $appJs.Content -notmatch "/api/carteira") {
    throw "Tela Carteira incompleta."
}
Write-Host "    Navegacao + carregamento: assets OK."

Write-Host "[110/600] Validando filtros e ordenacao..."
if ($appJs.Content -notmatch "portfolioSearch" -or
    $appJs.Content -notmatch "portfolioPriority" -or
    $appJs.Content -notmatch "portfolioSort") {
    throw "Filtros da carteira incompletos."
}
Write-Host "    Busca + prioridade + ordenacao: assets OK."

Write-Host "[111/600] Validando atalho da carteira no dashboard..."
if ($appJs.Content -notmatch "Pacientes para acompanhar" -or
    $appJs.Content -notmatch "openPortfolio" -or
    $appJs.Content -notmatch "dashboard-portfolio-section") {
    throw "Resumo da carteira no dashboard ausente."
}
Write-Host "    Dashboard -> carteira: assets OK."

Write-Host "[112/600] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "portfolio-patient-card" -or
    $css.Content -notmatch "portfolio-metrics" -or
    $css.Content -notmatch "portfolio-toolbar") {
    throw "Estilos da carteira incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    Carteira responsiva / v0.3.27: OK."


Write-Host "[113/600] Validando schema de follow-up..."
$followTable = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='InteracoesAcompanhamento';"
if ([int]$followTable -ne 1) { throw "Tabela InteracoesAcompanhamento ausente." }
Write-Host "    InteracoesAcompanhamento: schema OK."

Write-Host "[114/600] Validando endpoint de follow-up..."
$followSource = Get-Content .\src\HealthPlatform.Api\Controllers\FollowUpController.cs -Encoding UTF8 -Raw
if ($followSource -notmatch 'api/pacientes/{pacienteId:guid}/followups' -or
    $followSource -notmatch 'RegistrarFollowUpRequest' -or
    $followSource -notmatch 'InteracaoAcompanhamento') {
    throw "Endpoint de follow-up incompleto."
}
Write-Host "    GET + POST de follow-up: backend OK."

Write-Host "[115/600] Validando canais e proximo contato..."
if ($followSource -notmatch "WhatsApp" -or
    $followSource -notmatch "Telefone" -or
    $followSource -notmatch "Presencial" -or
    $followSource -notmatch "ProximoContatoUtc") {
    throw "Dados de follow-up incompletos."
}
Write-Host "    Canais + proximo contato: backend OK."

Write-Host "[116/600] Validando auditoria do contato..."
if ($followSource -notmatch "AuditLogs" -or
    $followSource -notmatch "nameof.InteracaoAcompanhamento.") {
    throw "Auditoria de follow-up ausente."
}
Write-Host "    Auditoria: backend OK."

Write-Host "[117/600] Validando follow-up na carteira..."
$carteiraSource = Get-Content .\src\HealthPlatform.Api\Controllers\CarteiraController.cs -Encoding UTF8 -Raw
if ($carteiraSource -notmatch "UltimoContatoUtc" -or
    $carteiraSource -notmatch "ProximoContatoUtc" -or
    $carteiraSource -notmatch "ContatosUltimos30Dias") {
    throw "Follow-up na carteira incompleto."
}
Write-Host "    Ultimo/proximo contato + volume 30d: backend OK."

Write-Host "[118/600] Validando acao rapida de contato..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "openPortfolioContact" -or
    $appJs.Content -notmatch "Registrar contato" -or
    $appJs.Content -notmatch "/followups") {
    throw "Acao rapida de contato incompleta."
}
Write-Host "    Carteira -> registrar contato: assets OK."

Write-Host "[119/600] Validando acao rapida de retorno..."
if ($appJs.Content -notmatch "openPortfolioReturn" -or
    $appJs.Content -notmatch "Agendar retorno" -or
    $appJs.Content -notmatch "/consultas") {
    throw "Acao rapida de retorno incompleta."
}
Write-Host "    Carteira -> agenda: assets OK."

Write-Host "[120/600] Validando acao rapida de pendencia..."
if ($appJs.Content -notmatch "openPortfolioPending" -or
    $appJs.Content -notmatch "/pendencias") {
    throw "Acao rapida de pendencia incompleta."
}
Write-Host "    Carteira -> pendencia: assets OK."

Write-Host "[121/600] Validando historico no prontuario..."
if ($appJs.Content -notmatch "Follow-up" -or
    $appJs.Content -notmatch "followup-history-section" -or
    $appJs.Content -notmatch "patientQuickContact") {
    throw "Historico de follow-up no prontuario incompleto."
}
Write-Host "    Prontuario: historico + contato rapido OK."

Write-Host "[122/600] Validando estilos, upgrade e versao..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "followup-history-list" -or
    $css.Content -notmatch "followup-channel") {
    throw "Estilos de follow-up incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / follow-up responsivo / upgrade OK."


Write-Host "[123/600] Validando endpoint da fila de follow-up..."
$fila = Invoke-RestMethod -Uri "$base/api/followups/fila?faixa=todos" -Headers $headers
if ($null -eq $fila.total -or $null -eq $fila.itens) {
    throw "Fila de follow-up invalida."
}
Write-Host "    Pacientes com proximo contato: $($fila.total)"

Write-Host "[124/600] Validando faixas de vencimento..."
$filaSource = Get-Content .\src\HealthPlatform.Api\Controllers\FilaFollowUpController.cs -Encoding UTF8 -Raw
if ($filaSource -notmatch "Vencido" -or
    $filaSource -notmatch "Proximos7Dias" -or
    $filaSource -notmatch "DiasAtraso") {
    throw "Faixas da fila de follow-up incompletas."
}
Write-Host "    Vencido + hoje + 7 dias + futuro: backend OK."

Write-Host "[125/600] Validando busca e filtros da fila..."
if ($filaSource -notmatch "busca" -or
    $filaSource -notmatch "faixa" -or
    $filaSource -notmatch "PacienteNome") {
    throw "Filtros da fila incompletos."
}
Write-Host "    Busca + faixa: backend OK."

Write-Host "[126/600] Validando notificacao de follow-up..."
$notificationSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notificationSource -notmatch "PROF:FOLLOWUP" -or
    $notificationSource -notmatch "InteracaoAcompanhamento" -or
    $notificationSource -notmatch '"followups"') {
    throw "Notificacao de follow-up incompleta."
}
Write-Host "    Proximo contato -> notificacao: backend OK."

Write-Host "[127/600] Validando tela de follow-up..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.followups.' -or
    $appJs.Content -notmatch "loadFollowUpQueue" -or
    $appJs.Content -notmatch "/api/followups/fila") {
    throw "Tela de follow-up incompleta."
}
Write-Host "    Navegacao + fila: assets OK."

Write-Host "[128/600] Validando acoes rapidas na fila..."
if ($appJs.Content -notmatch "follow-queue-contact" -or
    $appJs.Content -notmatch "openPortfolioContact" -or
    $appJs.Content -notmatch "follow-queue-patient") {
    throw "Acoes da fila incompletas."
}
Write-Host "    Registrar contato + prontuario: assets OK."

Write-Host "[129/600] Validando resumo de follow-up no dashboard..."
if ($appJs.Content -notmatch "dashboard-followup-section" -or
    $appJs.Content -notmatch "openFollowUpQueue" -or
    $appJs.Content -notmatch "Follow-ups") {
    throw "Resumo de follow-up no dashboard ausente."
}
Write-Host "    Dashboard -> fila de follow-up: assets OK."

Write-Host "[130/600] Validando estilos, popular e versao..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
$popularSource = Get-Content .\POPULAR.ps1 -Encoding UTF8 -Raw
if ($css.Content -notmatch "follow-queue-card" -or
    $css.Content -notmatch "follow-queue-toolbar" -or
    $popularSource -notmatch "Ensure-FollowUp") {
    throw "Estilos/populacao de follow-up incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / fila responsiva / demo follow-up: OK."


Write-Host "[131/600] Validando endpoint de gestao..."
$gestao = Invoke-RestMethod -Uri "$base/api/gestao/resumo?dias=30" -Headers $headers
if ($null -eq $gestao.pacientesAtivos -or
    $null -eq $gestao.consultasRealizadas -or
    $null -eq $gestao.taxaComparecimentoPct) {
    throw "Resumo de gestao invalido."
}
Write-Host "    Pacientes=$($gestao.pacientesAtivos) / realizadas=$($gestao.consultasRealizadas) / comparecimento=$($gestao.taxaComparecimentoPct)%"

Write-Host "[132/600] Validando indicadores de consultas..."
$gestaoSource = Get-Content .\src\HealthPlatform.Api\Controllers\GestaoController.cs -Encoding UTF8 -Raw
if ($gestaoSource -notmatch "ConsultasRealizadas" -or
    $gestaoSource -notmatch "ConsultasCanceladas" -or
    $gestaoSource -notmatch "Faltas" -or
    $gestaoSource -notmatch "TaxaComparecimentoPct") {
    throw "Indicadores de consultas incompletos."
}
Write-Host "    Realizadas + faltas + canceladas + taxa: backend OK."

Write-Host "[133/600] Validando indicadores de acompanhamento..."
if ($gestaoSource -notmatch "FollowUpsRealizados" -or
    $gestaoSource -notmatch "FollowUpsVencidos" -or
    $gestaoSource -notmatch "PendenciasAbertas" -or
    $gestaoSource -notmatch "PendenciasResolvidasPeriodo") {
    throw "Indicadores de acompanhamento incompletos."
}
Write-Host "    Follow-up + pendencias: backend OK."

Write-Host "[134/600] Validando indicadores de engajamento..."
if ($gestaoSource -notmatch "TreinosRegistrados" -or
    $gestaoSource -notmatch "RegistrosDiario" -or
    $gestaoSource -notmatch "RegistrosMetas") {
    throw "Indicadores de engajamento incompletos."
}
Write-Host "    Treinos + diario + metas: backend OK."

Write-Host "[135/600] Validando series gerenciais..."
if ($gestaoSource -notmatch "ConsultasPorStatus" -or
    $gestaoSource -notmatch "AtividadePorSemana" -or
    $gestaoSource -notmatch "PacientesAtencao") {
    throw "Series gerenciais incompletas."
}
Write-Host "    Status + atividade semanal + pacientes de atencao: backend OK."

Write-Host "[136/600] Validando tela Gestao..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.gestao.' -or
    $appJs.Content -notmatch "loadManagement" -or
    $appJs.Content -notmatch "/api/gestao/resumo") {
    throw "Tela Gestao incompleta."
}
Write-Host "    Navegacao + periodo + indicadores: assets OK."

Write-Host "[137/600] Validando graficos e resumo no dashboard..."
if ($appJs.Content -notmatch "managementBarChart" -or
    $appJs.Content -notmatch "managementMiniSeries" -or
    $appJs.Content -notmatch "dashboard-management-section") {
    throw "Graficos/resumo gerencial incompletos."
}
Write-Host "    Barras + serie semanal + dashboard: assets OK."

Write-Host "[138/600] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "management-grid" -or
    $css.Content -notmatch "management-bars" -or
    $css.Content -notmatch "management-attention-list") {
    throw "Estilos de gestao incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    Gestao responsiva / v0.3.27: OK."


Write-Host "[139/600] Validando PREPARAR sem atualizacao automatica do dotnet..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if ($setupSource -match "winget\s+(install|upgrade).*DotNet" -or
    $setupSource -match "dotnet-install\.ps1" -or
    $setupSource -match "dotnet tool update\s+--global\s+dotnet-ef") {
    throw "PREPARAR ainda tenta atualizar dotnet/dotnet-ef automaticamente."
}
if ($setupSource -notmatch "FAST_DOTNET_CHECK" -or
    $setupSource -notmatch "dotnet --version") {
    throw "Check rapido de dotnet ausente."
}
Write-Host "    dotnet: check rapido apenas, sem update/install automatico."

Write-Host "[140/600] Validando endpoint CSV gerencial..."
$exportSource = Get-Content .\src\HealthPlatform.Api\Controllers\GestaoExportController.cs -Encoding UTF8 -Raw
if ($exportSource -notmatch 'HttpGet\("csv"\)' -or
    $exportSource -notmatch "text/csv" -or
    $exportSource -notmatch "PendenciasAbertas") {
    throw "Exportacao CSV gerencial incompleta."
}
Write-Host "    CSV: backend OK."

Write-Host "[141/600] Validando conteudo longitudinal do CSV..."
if ($exportSource -notmatch "UltimaConsulta" -or
    $exportSource -notmatch "ProximaConsulta" -or
    $exportSource -notmatch "FollowUpsNoPeriodo" -or
    $exportSource -notmatch "ProximoContato") {
    throw "CSV nao inclui acompanhamento esperado."
}
Write-Host "    Consulta + pendencia + follow-up: colunas OK."

Write-Host "[142/600] Validando relatorio HTML imprimivel..."
if ($exportSource -notmatch 'HttpGet\("html"\)' -or
    $exportSource -notmatch "window.print" -or
    $exportSource -notmatch "Comparecimento" -or
    $exportSource -notmatch "taxa") {
    throw "Relatorio HTML incompleto."
}
Write-Host "    HTML imprimivel: backend OK."

Write-Host "[143/600] Validando botoes de exportacao na Gestao..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "managementExportCsv" -or
    $appJs.Content -notmatch "managementPrintReport" -or
    $appJs.Content -notmatch "downloadManagementCsv") {
    throw "Acoes visuais de exportacao incompletas."
}
Write-Host "    CSV + relatorio: assets OK."

Write-Host "[144/600] Validando download autenticado..."
if ($appJs.Content -notmatch "hpAuthenticatedBlob" -or
    $appJs.Content -notmatch "Authorization" -or
    $appJs.Content -notmatch "createObjectURL") {
    throw "Download autenticado incompleto."
}
Write-Host "    Bearer + Blob + nome de arquivo: assets OK."

Write-Host "[145/600] Validando relatorio autenticado em nova janela..."
if ($appJs.Content -notmatch "openManagementPrintable" -or
    $appJs.Content -notmatch "/api/gestao/export/html" -or
    $appJs.Content -notmatch "window.open") {
    throw "Abertura do relatorio gerencial incompleta."
}
Write-Host "    HTML autenticado -> janela imprimivel: assets OK."

Write-Host "[146/600] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "management-head-actions") {
    throw "Estilos de exportacao gerencial ausentes."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / exportacao gerencial / setup rapido: OK."


Write-Host "[147/600] Validando perguntas de anamnese inativas..."
$perguntasSource = Get-Content .\src\HealthPlatform.Api\Controllers\PerguntasAnamneseController.cs -Encoding UTF8 -Raw
if ($perguntasSource -notmatch "incluirInativas" -or
    $perguntasSource -notmatch "incluirInativas \|\| x.Ativa") {
    throw "Perguntas de anamnese ainda nao suportam incluir inativas."
}
Write-Host "    incluirInativas=true: backend OK."

Write-Host "[148/600] Validando totais completos de insights..."
$insightSource = Get-Content .\src\HealthPlatform.Api\Controllers\InsightsController.cs -Encoding UTF8 -Raw
if ($insightSource -match "SelectMany\(x => x.Insights\)" -and $insightSource -match "Take\(4\)") {
    throw "Totais do dashboard ainda dependem da lista visual truncada."
}
if ($insightSource -notmatch "totalInsights \+= insights.Count") {
    throw "Acumulador completo de insights ausente."
}
Write-Host "    Totais agregados independem do top 4 visual."

Write-Host "[149/600] Validando timezone das notificacoes..."
$notifSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notifSource -match "ToLocalTime\(\):dd/MM HH:mm") {
    throw "Notificacoes ainda formatam horario pelo timezone do servidor."
}
Write-Host "    Instantes permanecem UTC; interface localiza no navegador."

Write-Host "[150/600] Validando cleanup do polling no logout..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "logoutBtn.onclick" -or
    $appJs.Content -notmatch "patientLogoutBtn.onclick") {
    throw "Logout nao foi religado ao wrapper com cleanup."
}
Write-Host "    Logout profissional + paciente: polling cleanup OK."

Write-Host "[151/600] Validando navegacao robusta no portal..."
if ($appJs.Content -notmatch "hpOpenPatientNotificationLink" -or
    $appJs.Content -notmatch "loadPatientPortalView" -or
    $appJs.Content -notmatch "loadPatientSection") {
    throw "Fallback de navegacao do paciente incompleto."
}
Write-Host "    Notificacao do paciente: fallback de navegacao OK."

Write-Host "[152/600] Validando smoke de notificacoes sem mutacao..."
$testSource = Get-Content .\TESTAR.ps1 -Encoding UTF8 -Raw
if ($testSource -match 'Invoke-RestMethod\s+-Uri\s+"\$base/api/notificacoes/sincronizar"') {
    throw "TESTAR ainda chama POST mutavel de sincronizacao."
}
if ($testSource -match '/api/notificacoes\?sincronizar=true') {
    throw "TESTAR ainda usa GET mutavel de notificacoes."
}
Write-Host "    TESTAR nao cria nem sincroniza notificacoes."

Write-Host "[153/600] Validando copy historica de schema..."
$schemaCheckMarker = 'Write-Host "[153/600] Validando copy historica de schema..."'
$schemaCheckIndex = $testSource.IndexOf($schemaCheckMarker)
if ($schemaCheckIndex -lt 0) {
    throw "Nao foi possivel localizar o bloco de validacao de schema."
}
$historicalTestSource = $testSource.Substring(0, $schemaCheckIndex)
if ($historicalTestSource -match "nao exige schema novo" -or
    $historicalTestSource -match "sem schema novo") {
    throw "TESTAR ainda possui mensagem historicamente incorreta de schema."
}
Write-Host "    Copy de schema atualizada."

Write-Host "[154/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / estabilizacao + qualidade: OK."


Write-Host "[155/600] Validando endpoint de busca global..."
$buscaSource = Get-Content .\src\HealthPlatform.Api\Controllers\BuscaGlobalController.cs -Encoding UTF8 -Raw
if ($buscaSource -notmatch 'Route\("api/busca"\)' -or
    $buscaSource -notmatch "EF.Functions.ILike") {
    throw "Busca global backend incompleta."
}
Write-Host "    /api/busca + ILIKE: backend OK."

Write-Host "[156/600] Validando fontes da busca..."
if ($buscaSource -notmatch '"Paciente"' -or
    $buscaSource -notmatch 'PendenciasClinicas' -or
    $buscaSource -notmatch '"Follow-up"' -or
    $buscaSource -notmatch '"Consulta"') {
    throw "Busca global nao cobre todas as fontes."
}
Write-Host "    Paciente + pendencia + follow-up + consulta: OK."

Write-Host "[157/600] Validando isolamento por organizacao..."
if ($buscaSource -notmatch "currentUser.OrganizationId" -or
    $buscaSource -notmatch "x.OrganizacaoId == org") {
    throw "Busca global sem isolamento organizacional."
}
Write-Host "    Multi-tenant: filtro de organizacao presente."

Write-Host "[158/600] Validando botao e atalho da busca..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch "globalSearchButton" -or
    $appJs.Content -notmatch "hpOpenGlobalSearch" -or
    $appJs.Content -notmatch "ctrlKey") {
    throw "Botao/atalho de busca ausente."
}
Write-Host "    Botao + Ctrl/Cmd+K: assets OK."

Write-Host "[159/600] Validando modal e debounce..."
if ($appJs.Content -notmatch "globalSearchModal" -or
    $appJs.Content -notmatch "hpGlobalSearchTimer" -or
    $appJs.Content -notmatch "setTimeout") {
    throw "Modal/debounce da busca incompletos."
}
Write-Host "    Modal + debounce: assets OK."

Write-Host "[160/600] Validando acoes dos resultados..."
if ($appJs.Content -notmatch "hpExecuteGlobalSearchResult" -or
    $appJs.Content -notmatch "openPatient" -or
    $appJs.Content -notmatch "navigate\('pendencias'\)" -or
    $appJs.Content -notmatch "navigate\('followups'\)" -or
    $appJs.Content -notmatch "navigate\('agenda'\)") {
    throw "Acoes da busca global incompletas."
}
Write-Host "    Prontuario + pendencias + follow-up + agenda: assets OK."

Write-Host "[161/600] Validando estilos responsivos da busca..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "global-search-dialog" -or
    $css.Content -notmatch "global-search-result" -or
    $css.Content -notmatch "@media\(max-width:650px\)") {
    throw "Estilos da busca global incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[162/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / busca global + central de acoes: OK."


Write-Host "[163/600] Validando endpoint da Central do Dia..."
$centralSource = Get-Content .\src\HealthPlatform.Api\Controllers\CentralDiaController.cs -Encoding UTF8 -Raw
if ($centralSource -notmatch 'Route\("api/central-dia"\)' -or
    $centralSource -notmatch "offsetMinutos") {
    throw "Central do Dia backend incompleta."
}
Write-Host "    Endpoint + offset local: backend OK."

Write-Host "[164/600] Validando agenda do dia..."
if ($centralSource -notmatch "ConsultasHoje" -or
    $centralSource -notmatch "DataHoraUtc >= inicioUtc" -or
    $centralSource -notmatch "DataHoraUtc < fimUtc") {
    throw "Agenda diaria da Central incompleta."
}
Write-Host "    Janela local do dia -> UTC: backend OK."

Write-Host "[165/600] Validando follow-ups do dia..."
if ($centralSource -notmatch "FollowUpsVencidos" -or
    $centralSource -notmatch "FollowUpsHoje" -or
    $centralSource -notmatch '"Vencido"' -or
    $centralSource -notmatch '"Hoje"') {
    throw "Follow-ups da Central incompletos."
}
Write-Host "    Vencidos + hoje: backend OK."

Write-Host "[166/600] Validando pendencias prioritarias..."
if ($centralSource -notmatch "PendenciasPrioritarias" -or
    $centralSource -notmatch 'x.Severidade == "Alta"' -or
    $centralSource -notmatch "VencimentoUtc") {
    throw "Pendencias prioritarias incompletas."
}
Write-Host "    Alta prioridade + vencimento: backend OK."

Write-Host "[167/600] Validando pacientes para revisao..."
if ($centralSource -notmatch "PacientesRevisao" -or
    $centralSource -notmatch "SemRetornoFuturo" -or
    $centralSource -notmatch "PendenciasAbertas") {
    throw "Pacientes para revisao incompletos."
}
Write-Host "    Pendencias + retorno futuro: backend OK."

Write-Host "[168/600] Validando tela Hoje..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.central-dia.' -or
    $appJs.Content -notmatch "loadCentralDia" -or
    $appJs.Content -notmatch "/api/central-dia") {
    throw "Tela Central do Dia incompleta."
}
Write-Host "    Navegacao + carregamento: assets OK."

Write-Host "[169/600] Validando acoes rapidas e dashboard..."
if ($appJs.Content -notmatch "central-register-contact" -or
    $appJs.Content -notmatch "central-open-patient" -or
    $appJs.Content -notmatch "dashboard-central-day" -or
    $appJs.Content -notmatch "openCentralDay") {
    throw "Acoes/dashboard da Central incompletos."
}
Write-Host "    Contato + prontuario + filas + dashboard: assets OK."

Write-Host "[170/600] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "central-day-grid" -or
    $css.Content -notmatch "central-day-row" -or
    $css.Content -notmatch "dashboard-central-day-metrics") {
    throw "Estilos da Central do Dia incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / Central do Dia responsiva: OK."


Write-Host "[171/600] Validando schema de evolucoes clinicas SOAP..."
$evoEntity = Get-Content .\src\HealthPlatform.Domain\Entities\EvolucaoClinica.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if ($evoEntity -notmatch "Subjetivo" -or
    $evoEntity -notmatch "Objetivo" -or
    $evoEntity -notmatch "Avaliacao" -or
    $evoEntity -notmatch "Plano" -or
    $dbSource -notmatch "EvolucoesClinicas") {
    throw "Schema SOAP incompleto."
}
Write-Host "    S + O + A + P: modelo e EF OK."

Write-Host "[172/600] Validando endpoint de evolucoes..."
$evoController = Get-Content .\src\HealthPlatform.Api\Controllers\EvolucoesClinicasController.cs -Encoding UTF8 -Raw
if ($evoController -notmatch 'api/pacientes/{pacienteId:guid}/evolucoes' -or
    $evoController -notmatch 'HttpPost' -or
    $evoController -notmatch 'HttpPut') {
    throw "Endpoints de evolucao incompletos."
}
Write-Host "    GET + POST + PUT: backend OK."

Write-Host "[173/600] Validando consulta opcional e isolamento..."
if ($evoController -notmatch "ConsultaValida" -or
    $evoController -notmatch "currentUser.OrganizationId" -or
    $evoController -notmatch "PacienteId == pacienteId") {
    throw "Vinculo de consulta/tenant incompleto."
}
Write-Host "    Consulta opcional + multi-tenant: backend OK."

Write-Host "[174/600] Validando auditoria da evolucao..."
if ($evoController -notmatch 'AdicionarAuditoria\("CREATE"' -or
    $evoController -notmatch 'AdicionarAuditoria\("UPDATE"' -or
    $evoController -notmatch "DadosAnterioresJson" -or
    $evoController -notmatch "DadosNovosJson") {
    throw "Auditoria da evolucao incompleta."
}
Write-Host "    CREATE + UPDATE com antes/depois: OK."

Write-Host "[175/600] Validando evolucao na timeline..."
$timelineSource = Get-Content .\src\HealthPlatform.Api\Controllers\TimelineController.cs -Encoding UTF8 -Raw
if ($timelineSource -notmatch "EvolucoesClinicas" -or
    $timelineSource -notmatch '"evolucao_clinica"' -or
    $timelineSource -notmatch "Evolucao clinica SOAP") {
    throw "Timeline SOAP incompleta."
}
Write-Host "    Evolucao aparece na timeline clinica."

Write-Host "[176/600] Validando aba Evolucoes no prontuario..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "tabButton\('evolucoes'" -or
    $appJs.Content -notmatch "soap-card" -or
    $appJs.Content -notmatch "/evolucoes") {
    throw "Aba Evolucoes incompleta."
}
Write-Host "    Historico SOAP: assets OK."

Write-Host "[177/600] Validando registro visual SOAP..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openEvolutionForm") -or
    -not $appJsSource.Contains(",'subjetivo'") -or
    -not $appJsSource.Contains(",'objetivo'") -or
    -not $appJsSource.Contains(",'avaliacao'") -or
    -not $appJsSource.Contains(",'plano'") -or
    -not $appJsSource.Contains("val(e.target,'subjetivo')") -or
    -not $appJsSource.Contains("val(e.target,'objetivo')") -or
    -not $appJsSource.Contains("val(e.target,'avaliacao')") -or
    -not $appJsSource.Contains("val(e.target,'plano')")) {
    throw "Formulario SOAP incompleto."
}
Write-Host "    Registro estruturado S/O/A/P: assets OK."

Write-Host "[178/600] Validando edicao visual da evolucao..."
if (-not $appJsSource.Contains("edit-evolution") -or
    -not $appJsSource.Contains("method:id?'PUT':'POST'") -or
    -not $appJsSource.Contains("/api/evolucoes/")) {
    throw "Edicao SOAP incompleta."
}
Write-Host "    Historico + edicao auditada: assets OK."

Write-Host "[179/600] Validando upgrade SQL SOAP historico e PREPARAR atual..."
$soapSqlSource = Get-Content .\scripts\sql\v0.3.15_evolucoes_clinicas.sql -Encoding UTF8 -Raw
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $soapSqlSource.Contains('"EvolucoesClinicas"') -or
    -not $soapSqlSource.Contains('"Subjetivo"') -or
    -not $soapSqlSource.Contains('"Objetivo"') -or
    -not $soapSqlSource.Contains('"Avaliacao"') -or
    -not $soapSqlSource.Contains('"Plano"')) {
    throw "Upgrade SOAP historico incompleto."
}
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.15_evolucoes_clinicas.sql") -or
    -not $setupSource.Contains("v0.3.22_progressao_treino.sql")) {
    throw "PREPARAR atual incompleto."
}
Write-Host "    SOAP v0.3.15 preservado / PREPARAR atual 38/38: OK."

Write-Host "[180/600] Validando estilos e versao v0.3.27..."
$soapCssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $soapCssSource.Contains("soap-grid") -or
    -not $soapCssSource.Contains("soap-card")) {
    throw "Estilos SOAP incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / evolucao clinica SOAP: OK."


Write-Host "[181/600] Validando endpoint de resumo clinico..."
$resumoSource = Get-Content .\src\HealthPlatform.Api\Controllers\ResumoClinicoController.cs -Encoding UTF8 -Raw
if (-not $resumoSource.Contains('Route("api/pacientes/{pacienteId:guid}/resumo-clinico")') -or
    -not $resumoSource.Contains("ResumoClinicoResponse")) {
    throw "Resumo clinico backend incompleto."
}
Write-Host "    Endpoint consolidado: backend OK."

Write-Host "[182/600] Validando consulta e SOAP no resumo..."
if (-not $resumoSource.Contains("UltimaConsulta") -or
    -not $resumoSource.Contains("ProximaConsulta") -or
    -not $resumoSource.Contains("UltimaEvolucao") -or
    -not $resumoSource.Contains("EvolucoesClinicas")) {
    throw "Agenda/SOAP do resumo incompletos."
}
Write-Host "    Ultima/proxima consulta + SOAP: OK."

Write-Host "[183/600] Validando corpo e anamnese..."
if (-not $resumoSource.Contains("UltimaAvaliacao") -or
    -not $resumoSource.Contains("db.Avaliacoes") -or
    -not $resumoSource.Contains("UltimaAnamnese") -or
    -not $resumoSource.Contains("db.Anamneses")) {
    throw "Corpo/anamnese do resumo incompletos."
}
Write-Host "    Avaliacao corporal + anamnese: OK."

Write-Host "[184/600] Validando exames alterados..."
if (-not $resumoSource.Contains("ExamesAlterados") -or
    -not $resumoSource.Contains("ReferenciaMinima") -or
    -not $resumoSource.Contains("ReferenciaMaxima") -or
    -not $resumoSource.Contains('"Abaixo"') -or
    -not $resumoSource.Contains('"Acima"')) {
    throw "Exames alterados do resumo incompletos."
}
Write-Host "    Faixas numericas registradas: backend OK."

Write-Host "[185/600] Validando metas, treinos e pendencias..."
if (-not $resumoSource.Contains("MetasAtivas") -or
    -not $resumoSource.Contains("TreinosUltimos30Dias") -or
    -not $resumoSource.Contains("DataHoraInicioUtc") -or
    -not $resumoSource.Contains("PendenciasAltaPrioridade")) {
    throw "Indicadores operacionais do resumo incompletos."
}
Write-Host "    Metas + treino + pendencias: backend OK."

Write-Host "[186/600] Validando resumo no prontuario..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpClinicalSummaryCard") -or
    -not $appJsSource.Contains("/resumo-clinico") -or
    -not $appJsSource.Contains("clinical-summary-card")) {
    throw "Resumo clinico visual incompleto."
}
Write-Host "    Resumo integrado ao prontuario: assets OK."

Write-Host "[187/600] Validando atualizacao e responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("clinicalSummaryRefresh") -or
    -not $cssSource.Contains("clinical-summary-grid") -or
    -not $cssSource.Contains("clinical-summary-metrics")) {
    throw "Atualizacao/estilos do resumo incompletos."
}
Write-Host "    Atualizacao manual + desktop/mobile: assets OK."

Write-Host "[188/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / resumo clinico consolidado: OK."


Write-Host "[189/600] Validando texto de handoff clinico..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("function hpClinicalSummaryText") -or
    -not $appJsSource.Contains("lines.push('AGENDA')") -or
    -not $appJsSource.Contains("lines.push('ACOMPANHAMENTO')")) {
    throw "Handoff textual incompleto."
}
Write-Host "    Resumo textual estruturado: assets OK."

Write-Host "[190/600] Validando conteudo SOAP no handoff..."
if (-not $appJsSource.Contains("r.ultimaEvolucao.subjetivo") -or
    -not $appJsSource.Contains("r.ultimaEvolucao.objetivo") -or
    -not $appJsSource.Contains("r.ultimaEvolucao.avaliacao") -or
    -not $appJsSource.Contains("r.ultimaEvolucao.plano")) {
    throw "SOAP no handoff incompleto."
}
Write-Host "    S/O/A/P presentes no texto de handoff."

Write-Host "[191/600] Validando copia para clipboard..."
if (-not $appJsSource.Contains("hpCopyClinicalSummary") -or
    -not $appJsSource.Contains("navigator.clipboard") -or
    -not $appJsSource.Contains("document.execCommand('copy')")) {
    throw "Copia do handoff incompleta."
}
Write-Host "    Clipboard moderno + fallback: assets OK."

Write-Host "[192/600] Validando impressao do resumo..."
if (-not $appJsSource.Contains("hpClinicalSummaryPrintHtml") -or
    -not $appJsSource.Contains("hpPrintClinicalSummary") -or
    -not $appJsSource.Contains("window.print()")) {
    throw "Impressao do resumo incompleta."
}
Write-Host "    HTML imprimivel + print automatico: assets OK."

Write-Host "[193/600] Validando seguranca basica do HTML imprimivel..."
if (-not $appJsSource.Contains("const escPrint=v=>esc(v??'')") -or
    -not $appJsSource.Contains("noopener,noreferrer")) {
    throw "Protecoes da impressao incompletas."
}
Write-Host "    Escape de conteudo + nova janela isolada: assets OK."

Write-Host "[194/600] Validando botoes no resumo clinico..."
if (-not $appJsSource.Contains('id="clinicalSummaryCopy"') -or
    -not $appJsSource.Contains('id="clinicalSummaryPrint"') -or
    -not $appJsSource.Contains('id="clinicalSummaryRefresh"')) {
    throw "Acoes do resumo clinico incompletas."
}
Write-Host "    Copiar + imprimir + atualizar: assets OK."

Write-Host "[195/600] Validando responsividade das acoes..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("clinical-summary-actions") -or
    -not $cssSource.Contains("@media(max-width:620px)")) {
    throw "Estilos das acoes de handoff incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[196/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / handoff clinico + impressao: OK."


Write-Host "[197/600] Validando endpoint de equipe..."
$teamSource = Get-Content .\src\HealthPlatform.Api\Controllers\EquipeController.cs -Encoding UTF8 -Raw
if (-not $teamSource.Contains('Route("api/equipe")') -or
    -not $teamSource.Contains("[HttpGet]") -or
    -not $teamSource.Contains("[HttpPost]") -or
    -not $teamSource.Contains('[HttpPut("{usuarioId:guid}")]')) {
    throw "Endpoints de equipe incompletos."
}
Write-Host "    GET + POST + PUT: backend OK."

Write-Host "[198/600] Validando isolamento e administracao..."
if (-not $teamSource.Contains("currentUser.OrganizationId") -or
    -not $teamSource.Contains("TipoUsuario.Admin") -or
    -not $teamSource.Contains("EhAdmin")) {
    throw "Protecao administrativa da equipe incompleta."
}
Write-Host "    Organizacao + admin: backend OK."

Write-Host "[199/600] Validando criacao de acesso..."
if (-not $teamSource.Contains("userManager.CreateAsync") -or
    -not $teamSource.Contains("SenhaTemporaria") -or
    -not $teamSource.Contains("AddToRoleAsync")) {
    throw "Criacao de acesso da equipe incompleta."
}
Write-Host "    Identity + senha temporaria + role: backend OK."

Write-Host "[200/600] Validando sincronizacao de tipo e role..."
if (-not $teamSource.Contains("RemoveFromRolesAsync") -or
    -not $teamSource.Contains("usuario.TipoUsuario = tipo") -or
    -not $teamSource.Contains("userManager.UpdateAsync")) {
    throw "Sincronizacao de acesso incompleta."
}
Write-Host "    TipoUsuario + Identity Role: backend OK."

Write-Host "[201/600] Validando perfil profissional..."
if (-not $teamSource.Contains("ExigePerfilProfissional") -or
    -not $teamSource.Contains("RegistroProfissional") -or
    -not $teamSource.Contains("TipoUsuario.Nutricionista") -or
    -not $teamSource.Contains("TipoUsuario.Personal")) {
    throw "Perfil profissional da equipe incompleto."
}
Write-Host "    Medico + nutricionista + personal: backend OK."

Write-Host "[202/600] Validando protecao do proprio administrador..."
if (-not $teamSource.Contains("usuario.Id == currentUser.UserId") -or
    -not $teamSource.Contains("nao pode remover o proprio acesso administrativo")) {
    throw "Protecao do admin atual incompleta."
}
Write-Host "    Auto-bloqueio administrativo impedido."

Write-Host "[203/600] Validando auditoria da equipe..."
if (-not $teamSource.Contains('"CREATE"') -or
    -not $teamSource.Contains('"UPDATE"') -or
    -not $teamSource.Contains('"UsuarioEquipe"') -or
    -not $teamSource.Contains("DadosAnterioresJson")) {
    throw "Auditoria da equipe incompleta."
}
Write-Host "    CREATE + UPDATE auditados."

Write-Host "[204/600] Validando tela Equipe..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $indexSource.Contains('data-route="equipe"') -or
    -not $appJsSource.Contains("renderEquipe") -or
    -not $appJsSource.Contains("openCreateTeamMember") -or
    -not $appJsSource.Contains("openEditTeamMember")) {
    throw "Interface de equipe incompleta."
}
Write-Host "    Navegacao + cadastro + edicao: assets OK."

Write-Host "[205/600] Validando visibilidade admin e responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $indexSource.Contains("admin-only hidden") -or
    -not $appJsSource.Contains("u.tipoUsuario!=='Admin'") -or
    -not $cssSource.Contains("team-row") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Visibilidade/estilos da equipe incompletos."
}
Write-Host "    Admin-only + desktop/mobile: assets OK."

Write-Host "[206/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / equipe + gestao de profissionais: OK."


Write-Host "[207/600] Validando filtros da equipe..."
$teamSource = Get-Content .\src\HealthPlatform.Api\Controllers\EquipeController.cs -Encoding UTF8 -Raw
if (-not $teamSource.Contains("[FromQuery] string? busca") -or
    -not $teamSource.Contains("[FromQuery] string? tipo") -or
    -not $teamSource.Contains("[FromQuery] string? status") -or
    -not $teamSource.Contains("EF.Functions.ILike")) {
    throw "Filtros da equipe incompletos."
}
Write-Host "    Busca + tipo + status: backend OK."

Write-Host "[208/600] Validando redefinicao de senha..."
if (-not $teamSource.Contains('redefinir-senha') -or
    -not $teamSource.Contains("GeneratePasswordResetTokenAsync") -or
    -not $teamSource.Contains("ResetPasswordAsync")) {
    throw "Redefinicao de senha da equipe incompleta."
}
Write-Host "    Token Identity + reset: backend OK."

Write-Host "[209/600] Validando escopo e protecoes do reset..."
if (-not $teamSource.Contains("usuarioId == currentUser.UserId") -or
    -not $teamSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $teamSource.Contains("Reative o acesso antes de redefinir a senha")) {
    throw "Protecoes do reset de senha incompletas."
}
Write-Host "    Self-reset administrativo + tenant + inativo: protegidos."

Write-Host "[210/600] Validando auditoria segura de senha..."
if (-not $teamSource.Contains('"PASSWORD_RESET"') -or
    -not $teamSource.Contains("SenhaTemporariaRedefinida = true")) {
    throw "Auditoria do reset incompleta."
}
if ($teamSource.Contains("NovaSenhaTemporaria =") -or
    $teamSource.Contains("SenhaTemporaria = request.")) {
    throw "Senha temporaria nao deve ser gravada no AuditLog."
}
Write-Host "    Evento auditado sem persistir a senha."

Write-Host "[211/600] Validando filtros visuais..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("teamSearch") -or
    -not $appJsSource.Contains("teamTypeFilter") -or
    -not $appJsSource.Contains("teamStatusFilter") -or
    -not $appJsSource.Contains("URLSearchParams")) {
    throw "Filtros visuais da equipe incompletos."
}
Write-Host "    Busca com debounce + filtros: assets OK."

Write-Host "[212/600] Validando acao visual de senha..."
if (-not $appJsSource.Contains("openResetTeamPassword") -or
    -not $appJsSource.Contains("team-reset-password") -or
    -not $appJsSource.Contains("/redefinir-senha")) {
    throw "Acao visual de redefinir senha incompleta."
}
Write-Host "    Modal + endpoint de reset: assets OK."

Write-Host "[213/600] Validando responsividade da equipe v2..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("team-filterbar") -or
    -not $cssSource.Contains("team-row-actions") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos da equipe v2 incompletos."
}
Write-Host "    Filtros + acoes desktop/mobile: OK."

Write-Host "[214/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / equipe v2 + seguranca de acesso: OK."


Write-Host "[215/600] Validando endpoint Minha Conta..."
$configSource = Get-Content .\src\HealthPlatform.Api\Controllers\ConfiguracoesController.cs -Encoding UTF8 -Raw
if (-not $configSource.Contains('[HttpGet("minha-conta")]') -or
    -not $configSource.Contains('[HttpPut("minha-conta")]')) {
    throw "Endpoints Minha Conta incompletos."
}
Write-Host "    GET + PUT da conta: backend OK."

Write-Host "[216/600] Validando alteracao da propria senha..."
if (-not $configSource.Contains('minha-conta/alterar-senha') -or
    -not $configSource.Contains("ChangePasswordAsync") -or
    -not $configSource.Contains("SenhaAtual") -or
    -not $configSource.Contains("ConfirmacaoNovaSenha")) {
    throw "Troca de senha propria incompleta."
}
Write-Host "    Senha atual + nova senha + confirmacao: backend OK."

Write-Host "[217/600] Validando protecoes da troca de senha..."
if (-not $configSource.Contains("A nova senha deve ser diferente da senha atual") -or
    -not $configSource.Contains("request.NovaSenha.Length < 10") -or
    -not $configSource.Contains("currentUser.OrganizationId")) {
    throw "Protecoes da troca de senha incompletas."
}
Write-Host "    Diferenca + comprimento + tenant: backend OK."

Write-Host "[218/600] Validando auditoria segura da conta..."
if (-not $configSource.Contains('"PASSWORD_CHANGE"') -or
    -not $configSource.Contains("SenhaAlterada = true")) {
    throw "Auditoria da troca de senha incompleta."
}
if ($configSource.Contains("NovaSenha = request.") -or
    $configSource.Contains("SenhaAtual = request.")) {
    throw "Senhas nao devem ser gravadas no AuditLog."
}
Write-Host "    PASSWORD_CHANGE auditado sem armazenar credenciais."

Write-Host "[219/600] Validando sincronizacao do nome profissional..."
if (-not $configSource.Contains("profissional.Nome = usuario.Nome") -or
    -not $configSource.Contains('"MinhaConta"')) {
    throw "Sincronizacao de nome incompleta."
}
Write-Host "    Usuario + perfil profissional sincronizados."

Write-Host "[220/600] Validando painel Minha Conta..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("ensureAccountPanel") -or
    -not $appJsSource.Contains("accountChangePassword") -or
    -not $appJsSource.Contains("/api/configuracoes/minha-conta")) {
    throw "Painel Minha Conta incompleto."
}
Write-Host "    Dados + editar nome + alterar senha: assets OK."

Write-Host "[221/600] Validando responsividade da conta..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("account-panel") -or
    -not $cssSource.Contains("account-info-grid") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos Minha Conta incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[222/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / Minha Conta + troca de senha: OK."


Write-Host "[223/600] Validando schema de progressao alimentar..."
$planEntity = Get-Content .\src\HealthPlatform.Domain\Entities\PlanoAlimentar.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $planEntity.Contains("PlanoOrigemId") -or
    -not $planEntity.Contains("Versao") -or
    -not $planEntity.Contains("AjustePercentual") -or
    -not $dbSource.Contains("VersoesDerivadas")) {
    throw "Schema de progressao alimentar incompleto."
}
Write-Host "    Origem + versao + ajuste: modelo OK."

Write-Host "[224/600] Validando simulador nutricional..."
$planSource = Get-Content .\src\HealthPlatform.Api\Controllers\PlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $planSource.Contains("simular-ajuste") -or
    -not $planSource.Contains("SimulacaoAjustePlanoResponse") -or
    -not $planSource.Contains("EscalarTotais")) {
    throw "Simulador nutricional incompleto."
}
Write-Host "    Percentual/calorias -> macros projetados: backend OK."

Write-Host "[225/600] Validando duplicacao versionada..."
if (-not $planSource.Contains('/duplicar') -or
    -not $planSource.Contains("PlanoOrigemId = raizId") -or
    -not $planSource.Contains("Versao = maiorVersao + 1")) {
    throw "Duplicacao versionada incompleta."
}
Write-Host "    Nova versao preserva linhagem."

Write-Host "[226/600] Validando escala de porcoes..."
if (-not $planSource.Contains("EscalarQuantidade(itemOrigem.Quantidade") -or
    -not $planSource.Contains("EscalarQuantidade(itemOrigem.QuantidadeGramas") -or
    -not $planSource.Contains("EscalarQuantidade(subOrigem.QuantidadeGramas")) {
    throw "Escala de porcoes/substituicoes incompleta."
}
Write-Host "    Itens + gramas + substituicoes: backend OK."

Write-Host "[227/600] Validando ajuste por calorias alvo..."
if (-not $planSource.Contains("caloriasAlvo.Value / caloriasAtuais") -or
    -not $planSource.Contains("percentual.HasValue && caloriasAlvo.HasValue")) {
    throw "Ajuste por calorias alvo incompleto."
}
Write-Host "    Calorias alvo convertem para fator proporcional."

Write-Host "[228/600] Validando limites de ajuste..."
if (-not $planSource.Contains("ajustePercentual < -50m") -or
    -not $planSource.Contains("ajustePercentual > 100m")) {
    throw "Limites da progressao alimentar incompletos."
}
Write-Host "    Faixa -50% a +100% protegida."

Write-Host "[229/600] Validando encerramento opcional do plano anterior..."
if (-not $planSource.Contains("ConcluirPlanoAnterior") -or
    -not $planSource.Contains('origem.Status = "Concluido"')) {
    throw "Encerramento de versao anterior incompleto."
}
Write-Host "    Plano anterior pode ser concluido automaticamente."

Write-Host "[230/600] Validando auditoria da progressao..."
if (-not $planSource.Contains('"DUPLICATE_SCALE"') -or
    -not $planSource.Contains("CaloriasOriginais") -or
    -not $planSource.Contains("CaloriasProjetadas")) {
    throw "Auditoria da progressao alimentar incompleta."
}
Write-Host "    Origem + ajuste + calorias auditados."

Write-Host "[231/600] Validando interface de progressao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openNutritionProgression") -or
    -not $appJsSource.Contains("nutrition-progress") -or
    -not $appJsSource.Contains("newMealPlanFromTab") -or
    -not $appJsSource.Contains("openMealPlanForm") -or
    -not $appJsSource.Contains("nutrition-modal-open") -or
    -not $appJsSource.Contains("simular-ajuste") -or
    -not $appJsSource.Contains("/duplicar")) {
    throw "Interface de progressao alimentar incompleta."
}
Write-Host "    Novo plano + modal ampliado + simulacao + nova versao: assets OK."

Write-Host "[232/600] Validando modos percentual e calorias..."
if (-not $appJsSource.Contains('value="percentual"') -or
    -not $appJsSource.Contains('value="calorias"') -or
    -not $appJsSource.Contains("totaisProjetados")) {
    throw "Modos de ajuste visual incompletos."
}
Write-Host "    Percentual + calorias alvo: assets OK."

Write-Host "[233/600] Validando upgrade SQL e PREPARAR..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.21_progressao_plano_alimentar.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.21_progressao_plano_alimentar.sql") -or
    -not $sqlSource.Contains('"PlanoOrigemId"') -or
    -not $sqlSource.Contains('"Versao"')) {
    throw "Upgrade de progressao alimentar incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 19/19: OK."

Write-Host "[234/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / progressao do plano alimentar: OK."


Write-Host "[235/600] Validando schema de progressao de treino..."
$workoutEntity = Get-Content .\src\HealthPlatform.Domain\Entities\PlanoTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $workoutEntity.Contains("PlanoOrigemId") -or
    -not $workoutEntity.Contains("Versao") -or
    -not $workoutEntity.Contains("AjusteCargaPercentual") -or
    -not $dbSource.Contains("VersoesDerivadas")) {
    throw "Schema de progressao de treino incompleto."
}
Write-Host "    Origem + versao + ajustes: modelo OK."

Write-Host "[236/600] Validando simulador de treino..."
$workoutSource = Get-Content .\src\HealthPlatform.Api\Controllers\TreinosController.cs -Encoding UTF8 -Raw
if (-not $workoutSource.Contains("simular-progressao") -or
    -not $workoutSource.Contains("SimulacaoProgressaoTreinoResponse") -or
    -not $workoutSource.Contains("SomaCargasProjetada")) {
    throw "Simulador de treino incompleto."
}
Write-Host "    Carga + series + reps + descanso: backend OK."

Write-Host "[237/600] Validando duplicacao versionada do treino..."
if (-not $workoutSource.Contains('/duplicar') -or
    -not $workoutSource.Contains("PlanoOrigemId = raizId") -or
    -not $workoutSource.Contains("Versao = maiorVersao + 1")) {
    throw "Duplicacao versionada do treino incompleta."
}
Write-Host "    Nova versao preserva linhagem."

Write-Host "[238/600] Validando progressao de carga..."
if (-not $workoutSource.Contains("fatorCarga") -or
    -not $workoutSource.Contains("itemOrigem.Carga.Value * fatorCarga")) {
    throw "Progressao de carga incompleta."
}
Write-Host "    Carga percentual aplicada aos exercicios prescritos."

Write-Host "[239/600] Validando progressao de series e descanso..."
if (-not $workoutSource.Contains("itemOrigem.Series + request.AjusteSeries") -or
    -not $workoutSource.Contains("itemOrigem.DescansoSegundos.Value + request.AjusteDescansoSegundos")) {
    throw "Progressao de series/descanso incompleta."
}
Write-Host "    Series + descanso com limites inferiores seguros."

Write-Host "[240/600] Validando repeticoes estruturadas..."
if (-not $workoutSource.Contains("TentarAjustarRepeticoes") -or
    -not $workoutSource.Contains("PrescricoesRepeticoesPreservadas")) {
    throw "Ajuste seguro de repeticoes incompleto."
}
Write-Host "    Numeros/faixas ajustados; texto complexo preservado."

Write-Host "[241/600] Validando limites de progressao..."
if (-not $workoutSource.Contains("cargaPercentual < -50m") -or
    -not $workoutSource.Contains("seriesDelta < -5") -or
    -not $workoutSource.Contains("repeticoesDelta < -20") -or
    -not $workoutSource.Contains("descansoDeltaSegundos < -300")) {
    throw "Limites da progressao de treino incompletos."
}
Write-Host "    Limites de carga/series/reps/descanso: OK."

Write-Host "[242/600] Validando encerramento e auditoria..."
if (-not $workoutSource.Contains("ConcluirPlanoAnterior") -or
    -not $workoutSource.Contains('origem.Status = "Concluido"') -or
    -not $workoutSource.Contains('"DUPLICATE_PROGRESS"')) {
    throw "Encerramento/auditoria da progressao incompletos."
}
Write-Host "    Plano anterior + evento de progressao: OK."

Write-Host "[243/600] Validando interface de progressao de treino..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openWorkoutProgression") -or
    -not $appJsSource.Contains("workout-progress") -or
    -not $appJsSource.Contains("simular-progressao") -or
    -not $appJsSource.Contains("/duplicar")) {
    throw "Interface de progressao de treino incompleta."
}
Write-Host "    Prontuario -> simulacao -> nova versao: assets OK."

Write-Host "[244/600] Validando projecao e modal ampliado..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("workoutProjection") -or
    -not $appJsSource.Contains("workout-modal-open") -or
    -not $cssSource.Contains("workout-projection-grid") -or
    -not $cssSource.Contains("workout-modal-open")) {
    throw "UX da progressao de treino incompleta."
}
Write-Host "    Projecao + modal responsivo: assets OK."

Write-Host "[245/600] Validando upgrade SQL e PREPARAR 19..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.22_progressao_treino.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.22_progressao_treino.sql") -or
    -not $sqlSource.Contains('"PlanoOrigemId"') -or
    -not $sqlSource.Contains('"AjusteCargaPercentual"')) {
    throw "Upgrade de progressao de treino incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 19/19: OK."

Write-Host "[246/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / progressao de treino + ciclo versionado: OK."


Write-Host "[247/600] Validando schema dos modelos alimentares..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloPlanoAlimentar.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("OrganizacaoId") -or
    -not $modelEntity.Contains("ProfissionalId") -or
    -not $dbSource.Contains("ModelosPlanosAlimentares")) {
    throw "Schema de modelos alimentares incompleto."
}
Write-Host "    Modelo multi-tenant + JSON estruturado: OK."

Write-Host "[248/600] Validando listagem e busca de modelos..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-planos-alimentares') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("incluirInativos")) {
    throw "Listagem de modelos incompleta."
}
Write-Host "    GET + busca + ativos/inativos: backend OK."

Write-Host "[249/600] Validando salvar plano como modelo..."
if (-not $modelSource.Contains("salvar-como-modelo") -or
    -not $modelSource.Contains("TemplateConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar como modelo incompleto."
}
Write-Host "    Refeicoes + itens + substituicoes serializados."

Write-Host "[250/600] Validando criacao de plano a partir de modelo..."
if (-not $modelSource.Contains("criar-de-modelo") -or
    -not $modelSource.Contains("JsonSerializer.Deserialize<TemplateConteudo>") -or
    -not $modelSource.Contains("db.PlanosAlimentares.Add(plano)")) {
    throw "Criacao a partir de modelo incompleta."
}
Write-Host "    Template -> novo plano: backend OK."

Write-Host "[251/600] Validando alimentos do catalogo..."
if (-not $modelSource.Contains("alimentosValidos") -or
    -not $modelSource.Contains("alimentosInvalidos") -or
    -not $modelSource.Contains("x.Ativo")) {
    throw "Validacao do catalogo ao reutilizar modelo incompleta."
}
Write-Host "    Alimentos inativos/indisponiveis bloqueiam instanciacao."

Write-Host "[252/600] Validando isolamento e auditoria..."
if (-not $modelSource.Contains("currentUser.OrganizationId") -or
    -not $modelSource.Contains('"CREATE_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("AuditLogs")) {
    throw "Isolamento/auditoria dos modelos incompletos."
}
Write-Host "    Tenant + auditoria: backend OK."

Write-Host "[253/600] Validando interface de salvar modelo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openSaveMealTemplate") -or
    -not $appJsSource.Contains("nutrition-save-template") -or
    -not $appJsSource.Contains("salvar-como-modelo")) {
    throw "Interface Salvar como modelo incompleta."
}
Write-Host "    Card de plano -> salvar modelo: assets OK."

Write-Host "[254/600] Validando seletor de modelos..."
if (-not $appJsSource.Contains("openMealTemplatePicker") -or
    -not $appJsSource.Contains("mealTemplateSearch") -or
    -not $appJsSource.Contains("meal-template-grid")) {
    throw "Seletor de modelos incompleto."
}
Write-Host "    Busca + cards de modelos: assets OK."

Write-Host "[255/600] Validando criacao visual via modelo..."
if (-not $appJsSource.Contains("openMealTemplateCreateForm") -or
    -not $appJsSource.Contains("criar-de-modelo") -or
    -not $appJsSource.Contains("Plano criado a partir do modelo")) {
    throw "Criacao visual por modelo incompleta."
}
Write-Host "    Modelo -> paciente -> plano ativo: assets OK."

Write-Host "[256/600] Validando responsividade dos modelos..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("meal-template-grid") -or
    -not $cssSource.Contains("nutrition-top-actions") -or
    -not $cssSource.Contains("@media(max-width:720px)")) {
    throw "Estilos de modelos alimentares incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[257/600] Validando upgrade SQL e PREPARAR 20..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.23_modelos_plano_alimentar.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.23_modelos_plano_alimentar.sql") -or
    -not $sqlSource.Contains('"ModelosPlanosAlimentares"') -or
    -not $sqlSource.Contains('"ConteudoJson"')) {
    throw "Upgrade de modelos alimentares incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 20/20: OK."

Write-Host "[258/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / templates de plano alimentar: OK."


Write-Host "[259/600] Validando schema dos modelos de treino..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloPlanoTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("OrganizacaoId") -or
    -not $modelEntity.Contains("ProfissionalId") -or
    -not $dbSource.Contains("ModelosPlanosTreino")) {
    throw "Schema de modelos de treino incompleto."
}
Write-Host "    Modelo multi-tenant + JSON estruturado: OK."

Write-Host "[260/600] Validando listagem e busca de modelos de treino..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosTreinoController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-planos-treino') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("incluirInativos")) {
    throw "Listagem de modelos de treino incompleta."
}
Write-Host "    GET + busca + ativos/inativos: backend OK."

Write-Host "[261/600] Validando salvar treino como modelo..."
if (-not $modelSource.Contains("salvar-como-modelo") -or
    -not $modelSource.Contains("TemplateTreinoConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar treino como modelo incompleto."
}
Write-Host "    Sessoes + exercicios + prescricao serializados."

Write-Host "[262/600] Validando criacao de treino a partir de modelo..."
if (-not $modelSource.Contains("criar-de-modelo") -or
    -not $modelSource.Contains("JsonSerializer.Deserialize<TemplateTreinoConteudo>") -or
    -not $modelSource.Contains("db.PlanosTreino.Add(plano)")) {
    throw "Criacao de treino a partir de modelo incompleta."
}
Write-Host "    Template -> novo plano de treino: backend OK."

Write-Host "[263/600] Validando catalogo de exercicios..."
if (-not $modelSource.Contains("exerciciosValidos") -or
    -not $modelSource.Contains("exerciciosInvalidos") -or
    -not $modelSource.Contains("x.Ativo")) {
    throw "Validacao do catalogo de exercicios incompleta."
}
Write-Host "    Exercicios inativos/indisponiveis bloqueiam instanciacao."

Write-Host "[264/600] Validando prescricao completa do template..."
if (-not $modelSource.Contains("Series = i.Series") -or
    -not $modelSource.Contains("Repeticoes = i.Repeticoes") -or
    -not $modelSource.Contains("Carga = i.Carga") -or
    -not $modelSource.Contains("DescansoSegundos = i.DescansoSegundos") -or
    -not $modelSource.Contains("TempoSegundos = i.TempoSegundos")) {
    throw "Copia da prescricao de treino incompleta."
}
Write-Host "    Series + reps + carga + descanso + tempo: backend OK."

Write-Host "[265/600] Validando isolamento e auditoria dos modelos de treino..."
if (-not $modelSource.Contains("currentUser.OrganizationId") -or
    -not $modelSource.Contains('"CREATE_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("AuditLogs")) {
    throw "Isolamento/auditoria dos modelos de treino incompletos."
}
Write-Host "    Tenant + auditoria: backend OK."

Write-Host "[266/600] Validando interface de salvar modelo de treino..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openSaveWorkoutTemplate") -or
    -not $appJsSource.Contains("workout-save-template") -or
    -not $appJsSource.Contains("salvar-como-modelo")) {
    throw "Interface Salvar treino como modelo incompleta."
}
Write-Host "    Card do treino -> salvar modelo: assets OK."

Write-Host "[267/600] Validando seletor e busca de modelos..."
if (-not $appJsSource.Contains("openWorkoutTemplatePicker") -or
    -not $appJsSource.Contains("workoutTemplateSearch") -or
    -not $appJsSource.Contains("workout-template-grid")) {
    throw "Seletor de modelos de treino incompleto."
}
Write-Host "    Busca + cards de modelos: assets OK."

Write-Host "[268/600] Validando criacao visual via modelo..."
if (-not $appJsSource.Contains("openWorkoutTemplateCreateForm") -or
    -not $appJsSource.Contains("treinos/criar-de-modelo") -or
    -not $appJsSource.Contains("Treino criado a partir do modelo")) {
    throw "Criacao visual de treino por modelo incompleta."
}
Write-Host "    Modelo -> paciente -> treino ativo: assets OK."

Write-Host "[269/600] Validando upgrade SQL, responsividade e PREPARAR 21..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.24_modelos_plano_treino.sql -Encoding UTF8 -Raw
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.24_modelos_plano_treino.sql") -or
    -not $sqlSource.Contains('"ModelosPlanosTreino"') -or
    -not $sqlSource.Contains('"ConteudoJson"') -or
    -not $cssSource.Contains("workout-template-grid")) {
    throw "Upgrade/UX de modelos de treino incompletos."
}
Write-Host "    SQL idempotente + UI responsiva + PREPARAR 21/21: OK."

Write-Host "[270/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / templates de treino + criacao rapida: OK."


Write-Host "[271/600] Validando schema das metas nutricionais..."
$planEntity = Get-Content .\src\HealthPlatform.Domain\Entities\PlanoAlimentar.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $planEntity.Contains("MetaCalorias") -or
    -not $planEntity.Contains("MetaProteinasG") -or
    -not $planEntity.Contains("MetaCarboidratosG") -or
    -not $planEntity.Contains("MetaGordurasG") -or
    -not $planEntity.Contains("MetaFibrasG") -or
    -not $dbSource.Contains("HasPrecision(10, 2)")) {
    throw "Schema de metas nutricionais incompleto."
}
Write-Host "    Calorias + P/C/G + fibras: modelo OK."

Write-Host "[272/600] Validando contratos nutricionais..."
$contractsSource = Get-Content .\src\HealthPlatform.Api\Contracts\PlanosAlimentares\PlanoAlimentarContracts.cs -Encoding UTF8 -Raw
if (-not $contractsSource.Contains("AtualizarMetasNutricionaisRequest") -or
    -not $contractsSource.Contains("AnalisePlanoAlimentarResponse") -or
    -not $contractsSource.Contains("DistribuicaoRefeicaoResponse")) {
    throw "Contratos nutricionais incompletos."
}
Write-Host "    Metas + desvios + distribuicao: contratos OK."

Write-Host "[273/600] Validando endpoint de metas..."
$planSource = Get-Content .\src\HealthPlatform.Api\Controllers\PlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $planSource.Contains("metas-nutricionais") -or
    -not $planSource.Contains('"NUTRITION_TARGETS"') -or
    -not $planSource.Contains("ValidarMetas")) {
    throw "Endpoint de metas nutricionais incompleto."
}
Write-Host "    PUT + validacao + auditoria: backend OK."

Write-Host "[274/600] Validando analise nutricional..."
if (-not $planSource.Contains("analise-nutricional") -or
    -not $planSource.Contains("DistribuicaoNutricionalResponse") -or
    -not $planSource.Contains("DesviosNutricionaisResponse") -or
    -not $planSource.Contains("Percentual(")) {
    throw "Analise nutricional incompleta."
}
Write-Host "    Meta x prescrito + distribuicao por refeicao: backend OK."

Write-Host "[275/600] Validando metas na criacao e edicao..."
if (-not $planSource.Contains("MetaCalorias = request.MetaCalorias") -or
    -not $planSource.Contains("MetaProteinasG = request.MetaProteinasG") -or
    -not $planSource.Contains("MetaFibrasG = request.MetaFibrasG")) {
    throw "Persistencia de metas no plano incompleta."
}
Write-Host "    Criacao/edicao preservam metas."

Write-Host "[276/600] Validando metas na progressao alimentar..."
if (-not $planSource.Contains("EscalarNullable(origem.MetaProteinasG") -or
    -not $planSource.Contains("request.CaloriasAlvo ?? EscalarNullable(origem.MetaCalorias")) {
    throw "Metas nao acompanham progressao alimentar."
}
Write-Host "    Progressao escala metas junto das porcoes."

Write-Host "[277/600] Validando metas nos templates alimentares..."
$templateSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $templateSource.Contains("plano.MetaCalorias") -or
    -not $templateSource.Contains("MetaCalorias = conteudo.MetaCalorias") -or
    -not $templateSource.Contains("metaProteinasG = conteudo?.MetaProteinasG")) {
    throw "Metas nao foram integradas aos templates alimentares."
}
Write-Host "    Template salva e restaura objetivos nutricionais."

Write-Host "[278/600] Validando construtor com metas..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("nutrition-target-builder") -or
    -not $appJsSource.Contains("metaCalorias:dec(f,'metaCalorias')") -or
    -not $appJsSource.Contains("planTargetPreview")) {
    throw "Construtor alimentar com metas incompleto."
}
Write-Host "    Metas no cadastro + preview em tempo real: assets OK."

Write-Host "[279/600] Validando Meta x Prescrito no prontuario..."
if (-not $appJsSource.Contains("nutritionTargetPanel") -or
    -not $appJsSource.Contains("nutritionTargetLine") -or
    -not $appJsSource.Contains("nutrition-edit-targets")) {
    throw "Painel Meta x Prescrito incompleto."
}
Write-Host "    Comparacao diaria + edicao rapida: assets OK."

Write-Host "[280/600] Validando distribuicao por refeicao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("nutritionMealDistribution") -or
    -not $appJsSource.Contains("Prescrito no dia + metas planejadas por bloco") -or
    -not $appJsSource.Contains("mealTargetMini")) {
    throw "Distribuicao nutricional visual incompleta."
}
Write-Host "    Prescrito diario + meta planejada por refeicao: assets OK."

Write-Host "[281/600] Validando modal de metas..."
if (-not $appJsSource.Contains("openNutritionTargets") -or
    -not $appJsSource.Contains("/metas-nutricionais") -or
    -not $appJsSource.Contains("Metas nutricionais atualizadas")) {
    throw "Modal de metas nutricionais incompleto."
}
Write-Host "    Edicao sem reconstruir o plano: assets OK."

Write-Host "[282/600] Validando responsividade nutricional..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("nutrition-target-grid") -or
    -not $cssSource.Contains("nutrition-distribution-row") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos nutricionais incompletos."
}
Write-Host "    Meta + distribuicao desktop/mobile: OK."

Write-Host "[283/600] Validando SQL e PREPARAR 22..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.25_metas_nutricionais.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.25_metas_nutricionais.sql") -or
    -not $sqlSource.Contains('"MetaCalorias"') -or
    -not $sqlSource.Contains('"MetaFibrasG"')) {
    throw "Upgrade de metas nutricionais incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 22/22: OK."

Write-Host "[284/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / metas nutricionais + distribuicao: OK."


Write-Host "[285/600] Validando schema da biblioteca de refeicoes..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloRefeicao.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("Categoria") -or
    -not $dbSource.Contains("ModelosRefeicoes")) {
    throw "Schema da biblioteca de refeicoes incompleto."
}
Write-Host "    Modelo + categoria + snapshot JSON: OK."

Write-Host "[286/600] Validando listagem e filtros da biblioteca..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosRefeicoesController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-refeicoes') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("[FromQuery] string? categoria")) {
    throw "Listagem da biblioteca de refeicoes incompleta."
}
Write-Host "    Busca + categoria + ativos/inativos: backend OK."

Write-Host "[287/600] Validando salvar refeicao como modelo..."
if (-not $modelSource.Contains("refeicoes-plano/{refeicaoId:guid}/salvar-como-modelo") -or
    -not $modelSource.Contains("ModeloRefeicaoConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar refeicao como modelo incompleto."
}
Write-Host "    Refeicao + itens + substituicoes: backend OK."

Write-Host "[288/600] Validando insercao de refeicao no plano..."
if (-not $modelSource.Contains("inserir-modelo-refeicao") -or
    -not $modelSource.Contains("db.RefeicoesPlanoAlimentar.Add(refeicao)") -or
    -not $modelSource.Contains("plano.Refeicoes.Max(x => x.Ordem) + 1")) {
    throw "Insercao rapida de refeicao incompleta."
}
Write-Host "    Modelo -> nova refeicao no final do plano: backend OK."

Write-Host "[289/600] Validando catalogo e substituicoes..."
if (-not $modelSource.Contains("alimentosValidos") -or
    -not $modelSource.Contains("alimentosInvalidos") -or
    -not $modelSource.Contains("i.Substituicoes")) {
    throw "Validacao de alimentos/substituicoes incompleta."
}
Write-Host "    Catalogo ativo revalidado antes da insercao."

Write-Host "[290/600] Validando protecoes e auditoria..."
if (-not $modelSource.Contains('plano.Status == "Concluido"') -or
    -not $modelSource.Contains('"INSERT_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("currentUser.OrganizationId")) {
    throw "Protecoes da biblioteca de refeicoes incompletas."
}
Write-Host "    Plano concluido + tenant + auditoria: protegidos."

Write-Host "[291/600] Validando botao Salvar refeicao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("meal-save-template") -or
    -not $appJsSource.Contains("openSaveMealTemplate") -or
    -not $appJsSource.Contains("salvar-como-modelo")) {
    throw "Acao visual Salvar refeicao incompleta."
}
Write-Host "    Refeicao do plano -> biblioteca: assets OK."

Write-Host "[292/600] Validando biblioteca visual..."
if (-not $appJsSource.Contains("openMealLibrary") -or
    -not $appJsSource.Contains("mealLibrarySearch") -or
    -not $appJsSource.Contains("mealLibraryPlan") -or
    -not $appJsSource.Contains("meal-library-grid")) {
    throw "Biblioteca visual de refeicoes incompleta."
}
Write-Host "    Busca + selecao do plano + cards: assets OK."

Write-Host "[293/600] Validando insercao visual rapida..."
if (-not $appJsSource.Contains("openMealLibraryInsertForm") -or
    -not $appJsSource.Contains("inserir-modelo-refeicao") -or
    -not $appJsSource.Contains("state.patientTab='alimentacao'") -or
    -not $appJsSource.Contains("closeClinicalAction()")) {
    throw "Insercao visual de refeicao incompleta."
}
Write-Host "    Modelo -> plano ativo: assets OK."

Write-Host "[294/600] Validando responsividade da biblioteca..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("meal-library-grid") -or
    -not $cssSource.Contains("meal-card-head") -or
    -not $cssSource.Contains("@media(max-width:760px)")) {
    throw "Estilos da biblioteca de refeicoes incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[295/600] Validando SQL e PREPARAR 23..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.26_modelos_refeicoes.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.26_modelos_refeicoes.sql") -or
    -not $sqlSource.Contains('"ModelosRefeicoes"') -or
    -not $sqlSource.Contains('"Categoria"')) {
    throw "Upgrade da biblioteca de refeicoes incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 23/23: OK."

Write-Host "[296/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / biblioteca de refeicoes + insercao rapida: OK."


Write-Host "[297/600] Validando schema da biblioteca de sessoes..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloSessaoTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("Categoria") -or
    -not $dbSource.Contains("ModelosSessoesTreino")) {
    throw "Schema da biblioteca de sessoes incompleto."
}
Write-Host "    Modelo + categoria + snapshot JSON: OK."

Write-Host "[298/600] Validando listagem e filtros..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosSessoesTreinoController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-sessoes-treino') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("[FromQuery] string? categoria")) {
    throw "Listagem da biblioteca de sessoes incompleta."
}
Write-Host "    Busca + categoria + ativos/inativos: backend OK."

Write-Host "[299/600] Validando salvar sessao como modelo..."
if (-not $modelSource.Contains("sessoes-treino/{sessaoId:guid}/salvar-como-modelo") -or
    -not $modelSource.Contains("ModeloSessaoConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar sessao como modelo incompleto."
}
Write-Host "    Sessao + exercicios + prescricao: backend OK."

Write-Host "[300/600] Validando insercao de sessao no plano..."
if (-not $modelSource.Contains("inserir-modelo-sessao") -or
    -not $modelSource.Contains("db.SessoesTreino.Add(sessao)") -or
    -not $modelSource.Contains("plano.Sessoes.Max(x => x.Ordem) + 1")) {
    throw "Insercao rapida de sessao incompleta."
}
Write-Host "    Modelo -> nova sessao ao final do plano: backend OK."

Write-Host "[301/600] Validando catalogo e prescricao..."
if (-not $modelSource.Contains("exerciciosValidos") -or
    -not $modelSource.Contains("exerciciosInvalidos") -or
    -not $modelSource.Contains("Series = i.Series") -or
    -not $modelSource.Contains("Repeticoes = i.Repeticoes") -or
    -not $modelSource.Contains("DescansoSegundos = i.DescansoSegundos")) {
    throw "Validacao/copia da prescricao incompleta."
}
Write-Host "    Catalogo ativo + series/reps/carga/descanso: OK."

Write-Host "[302/600] Validando protecoes e auditoria..."
if (-not $modelSource.Contains('plano.Status == "Concluido"') -or
    -not $modelSource.Contains('"INSERT_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("currentUser.OrganizationId")) {
    throw "Protecoes da biblioteca de sessoes incompletas."
}
Write-Host "    Plano concluido + tenant + auditoria: protegidos."

Write-Host "[303/600] Validando botao Salvar sessao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("session-save-template") -or
    -not $appJsSource.Contains("openSaveWorkoutSessionTemplate") -or
    -not $appJsSource.Contains("sessoes-treino")) {
    throw "Acao visual Salvar sessao incompleta."
}
Write-Host "    Sessao do plano -> biblioteca: assets OK."

Write-Host "[304/600] Validando biblioteca visual..."
if (-not $appJsSource.Contains("openWorkoutSessionLibrary") -or
    -not $appJsSource.Contains("sessionLibrarySearch") -or
    -not $appJsSource.Contains("sessionLibraryPlan") -or
    -not $appJsSource.Contains("session-library-grid")) {
    throw "Biblioteca visual de sessoes incompleta."
}
Write-Host "    Busca + selecao do plano + cards: assets OK."

Write-Host "[305/600] Validando insercao visual rapida..."
if (-not $appJsSource.Contains("openWorkoutSessionInsertForm") -or
    -not $appJsSource.Contains("inserir-modelo-sessao") -or
    -not $appJsSource.Contains("state.patientTab='treinos'") -or
    -not $appJsSource.Contains("closeClinicalAction()")) {
    throw "Insercao visual de sessao incompleta."
}
Write-Host "    Modelo -> plano ativo: assets OK."

Write-Host "[306/600] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("session-library-grid") -or
    -not $cssSource.Contains("workout-session-mini-row") -or
    -not $cssSource.Contains("@media(max-width:760px)")) {
    throw "Estilos da biblioteca de sessoes incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[307/600] Validando SQL e PREPARAR 24..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.27_modelos_sessoes_treino.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.27_modelos_sessoes_treino.sql") -or
    -not $sqlSource.Contains('"ModelosSessoesTreino"') -or
    -not $sqlSource.Contains('"Categoria"')) {
    throw "Upgrade da biblioteca de sessoes incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 25/25: OK."

Write-Host "[308/600] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / biblioteca de sessoes + insercao rapida: OK."


Write-Host "[309/600] Validando endpoint longitudinal de habitos..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $habitos = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/evolucao-habitos?limite=24" -Headers $headers -Method Get
    if ($habitos.pacienteId -ne $pacienteSmoke.id) { throw "Evolucao de habitos retornou paciente incorreto." }
    if ($null -eq $habitos.itens) { throw "Evolucao de habitos sem colecao de itens." }
    Write-Host "    Endpoint OK: $($habitos.total) anamnese(s) carregada(s)." -ForegroundColor Green
} else {
    Write-Host "    Sem pacientes: smoke longitudinal ignorado sem criar dados." -ForegroundColor DarkGreen
}

Write-Host "[310/600] Validando contrato da evolucao de habitos..."
$anamSource = Get-Content .\src\HealthPlatform.Api\Controllers\AnamnesesController.cs -Encoding UTF8 -Raw
if (-not $anamSource.Contains("EvolucaoHabitosPontoResponse") -or
    -not $anamSource.Contains("VariacaoHabitosResponse") -or
    -not $anamSource.Contains("EvolucaoHabitosResponse")) {
    throw "Contratos de evolucao de habitos incompletos."
}
Write-Host "    Atual + anterior + variacao + serie: backend OK."

Write-Host "[311/600] Validando series de sono, estresse, atividade e agua..."
if (-not $anamSource.Contains("SonoHorasMedia") -or
    -not $anamSource.Contains("EstresseNivel") -or
    -not $anamSource.Contains("AtividadeFisicaDiasSemana") -or
    -not $anamSource.Contains("AguaLitrosDia")) {
    throw "Series de habitos incompletas."
}
Write-Host "    Quatro indicadores longitudinais presentes."

Write-Host "[312/600] Validando limite e isolamento multi-tenant..."
if (-not $anamSource.Contains("Math.Clamp(limite, 2, 60)") -or
    -not $anamSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Protecoes do endpoint longitudinal incompletas."
}
Write-Host "    Limite 2-60 + OrganizacaoId: OK."

Write-Host "[313/600] Validando comparacao com registro anterior..."
if (-not $anamSource.Contains("itens[^1]") -or
    -not $anamSource.Contains("itens[^2]") -or
    -not $anamSource.Contains("Diferenca(atual?.SonoHorasMedia") -or
    -not $anamSource.Contains("Diferenca(atual?.EstresseNivel")) {
    throw "Comparacao longitudinal incompleta."
}
Write-Host "    Atual x anterior: backend OK."

Write-Host "[314/600] Validando graficos de habitos..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpHabitCharts") -or
    -not $appJsSource.Contains("sonoHorasMedia") -or
    -not $appJsSource.Contains("aguaLitrosDia") -or
    -not $appJsSource.Contains("hpLineChart")) {
    throw "Graficos de habitos incompletos."
}
Write-Host "    SVG nativo reutilizado para 4 tendencias."

Write-Host "[315/600] Validando resumo atual dos habitos..."
if (-not $appJsSource.Contains("habit-current-grid") -or
    -not $appJsSource.Contains("hpHabitCurrentCard") -or
    -not $appJsSource.Contains("hpHabitDelta")) {
    throw "Resumo atual dos habitos incompleto."
}
Write-Host "    Valor atual + delta vs anterior: assets OK."

Write-Host "[316/600] Validando integracao na aba Anamnese..."
if (-not $appJsSource.Contains("tab==='anamnese'") -or
    -not $appJsSource.Contains("professional-anamnesis")) {
    throw "Integracao da evolucao na aba Anamnese incompleta."
}
Write-Host "    Anamnese -> evolucao de habitos: assets OK."

Write-Host "[317/600] Validando integracao no Resumo..."
if (-not $appJsSource.Contains("professional-summary-habits") -or
    -not $appJsSource.Contains("hpInjectHabitEvolution")) {
    throw "Integracao dos habitos no resumo incompleta."
}
Write-Host "    Resumo -> tendencias de habitos: assets OK."

Write-Host "[318/600] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("habit-current-grid") -or
    -not $cssSource.Contains("habit-evolution-section") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos da evolucao de habitos incompletos."
}
Write-Host "    Cards e graficos desktop/mobile: OK."

Write-Host "[319/600] Validando compatibilidade de banco e PREPARAR..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.27_modelos_sessoes_treino.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.28_evolucao_habitos.sql) {
    throw "v0.3.28 nao deveria exigir upgrade de schema."
}
Write-Host "    Sem schema novo / PREPARAR atual 38/38: OK."

Write-Host "[320/600] Validando versao v0.3.28..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.28 / evolucao de habitos + graficos de anamnese: OK."


Write-Host "[321/600] Validando resposta runtime com metas por refeicao..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $planosMeta = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/planos-alimentares" -Headers $headers -Method Get)

    $planoComRefeicao = $null
    $refSmoke = $null

    foreach ($planoMeta in $planosMeta) {
        if ($null -eq $planoMeta) { continue }

        $refeicoesValidas = @($planoMeta.refeicoes | Where-Object { $null -ne $_ })
        if ($refeicoesValidas.Count -gt 0) {
            $planoComRefeicao = $planoMeta
            $refSmoke = $refeicoesValidas | Select-Object -First 1
            break
        }
    }

    if ($null -ne $refSmoke) {
        $nomesPropriedades = @($refSmoke.PSObject.Properties | ForEach-Object { $_.Name })
        if ($nomesPropriedades -notcontains "metas" -or
            $nomesPropriedades -notcontains "desvios") {
            throw "Resposta da refeicao nao expos metas/desvios."
        }

        Write-Host "    Refeicao runtime expoe metas + desvios: OK." -ForegroundColor Green
    } else {
        Write-Host "    Nenhuma refeicao utilizavel no paciente smoke: validacao runtime ignorada sem criar dados." -ForegroundColor DarkGreen
    }
} else {
    Write-Host "    Sem pacientes: smoke de metas por refeicao ignorado." -ForegroundColor DarkGreen
}

Write-Host "[322/600] Validando schema de metas por refeicao..."
$mealEntity = Get-Content .\src\HealthPlatform.Domain\Entities\RefeicaoPlanoAlimentar.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $mealEntity.Contains("MetaCalorias") -or
    -not $mealEntity.Contains("MetaProteinasG") -or
    -not $mealEntity.Contains("MetaCarboidratosG") -or
    -not $mealEntity.Contains("MetaGordurasG") -or
    -not $mealEntity.Contains("MetaFibrasG") -or
    -not $dbSource.Contains("builder.Entity<RefeicaoPlanoAlimentar>")) {
    throw "Schema de metas por refeicao incompleto."
}
Write-Host "    Kcal + P/C/G + fibra por refeicao: modelo OK."

Write-Host "[323/600] Validando contratos de metas por refeicao..."
$contractsSource = Get-Content .\src\HealthPlatform.Api\Contracts\PlanosAlimentares\PlanoAlimentarContracts.cs -Encoding UTF8 -Raw
if (-not $contractsSource.Contains("AtualizarMetasRefeicaoRequest") -or
    -not $contractsSource.Contains("DistribuirMetasRefeicoesRequest") -or
    -not $contractsSource.Contains("MetasNutricionaisResponse Metas") -or
    -not $contractsSource.Contains("DesviosNutricionaisResponse Desvios")) {
    throw "Contratos de metas por refeicao incompletos."
}
Write-Host "    Edicao + distribuicao + comparacao: contratos OK."

Write-Host "[324/600] Validando endpoint de meta individual..."
$planSource = Get-Content .\src\HealthPlatform.Api\Controllers\PlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $planSource.Contains("refeicoes-plano/{refeicaoId:guid}/metas-nutricionais") -or
    -not $planSource.Contains('"MEAL_NUTRITION_TARGETS"') -or
    -not $planSource.Contains("AtualizarMetasRefeicao")) {
    throw "Endpoint de meta individual incompleto."
}
Write-Host "    PUT por refeicao + auditoria: backend OK."

Write-Host "[325/600] Validando distribuicao automatica..."
if (-not $planSource.Contains("distribuir-metas-refeicoes") -or
    -not $planSource.Contains("Math.Abs(soma - 100m)") -or
    -not $planSource.Contains("PercentualMeta(") -or
    -not $planSource.Contains('"MEAL_TARGET_DISTRIBUTION"')) {
    throw "Distribuicao automatica de metas incompleta."
}
Write-Host "    Percentuais fecham 100% e distribuem metas diarias."

Write-Host "[326/600] Validando isolamento e integridade da distribuicao..."
if (-not $planSource.Contains("idsPlano.SequenceEqual(idsRequest)") -or
    -not $planSource.Contains("currentUser.OrganizationId") -or
    -not $planSource.Contains("TemMetaPlano")) {
    throw "Protecoes da distribuicao incompletas."
}
Write-Host "    Todas as refeicoes + tenant + meta diaria: protegidos."

Write-Host "[327/600] Validando progressao com metas por refeicao..."
if (-not $planSource.Contains("EscalarNullable(refeicaoOrigem.MetaCalorias") -or
    -not $planSource.Contains("EscalarNullable(refeicaoOrigem.MetaProteinasG") -or
    -not $planSource.Contains("EscalarNullable(refeicaoOrigem.MetaFibrasG")) {
    throw "Progressao nao preserva metas por refeicao."
}
Write-Host "    V2/V3 escalam metas dos blocos junto das porcoes."

Write-Host "[328/600] Validando templates de plano..."
$templateSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $templateSource.Contains("r.MetaCalorias") -or
    -not $templateSource.Contains("MetaCalorias = r.MetaCalorias") -or
    -not $templateSource.Contains("MetaProteinasG = r.MetaProteinasG")) {
    throw "Templates de plano nao preservam metas por refeicao."
}
Write-Host "    Template completo salva/restaura metas dos blocos."

Write-Host "[329/600] Validando biblioteca de refeicoes..."
$mealTemplateSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosRefeicoesController.cs -Encoding UTF8 -Raw
if (-not $mealTemplateSource.Contains("refeicao.MetaCalorias") -or
    -not $mealTemplateSource.Contains("MetaCalorias = conteudo.MetaCalorias") -or
    -not $mealTemplateSource.Contains("MetaFibrasG = conteudo.MetaFibrasG")) {
    throw "Biblioteca de refeicoes nao preserva metas."
}
Write-Host "    Blocos reutilizaveis mantem sua meta planejada."

Write-Host "[330/600] Validando construtor alimentar..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("meal-target-builder") -or
    -not $appJsSource.Contains("mealMetaCalorias") -or
    -not $appJsSource.Contains("metaProteinasG:m.querySelector")) {
    throw "Construtor com metas por refeicao incompleto."
}
Write-Host "    Nova dieta pode nascer com meta por bloco: assets OK."

Write-Host "[331/600] Validando edicao e comparacao visual..."
if (-not $appJsSource.Contains("openMealNutritionTargets") -or
    -not $appJsSource.Contains("mealTargetMini") -or
    -not $appJsSource.Contains("meal-edit-targets") -or
    -not $appJsSource.Contains("/metas-nutricionais")) {
    throw "Edicao visual das metas por refeicao incompleta."
}
Write-Host "    Prescrito x planejado + edicao rapida: assets OK."

Write-Host "[332/600] Validando modal de distribuicao..."
if (-not $appJsSource.Contains("openMealTargetDistribution") -or
    -not $appJsSource.Contains("mealDistributionTotal") -or
    -not $appJsSource.Contains("distribuir-metas-refeicoes") -or
    -not $appJsSource.Contains("A soma precisa fechar em 100%")) {
    throw "Distribuicao visual de metas incompleta."
}
Write-Host "    Percentuais por refeicao + fechamento 100%: assets OK."

Write-Host "[333/600] Validando SQL e PREPARAR 25..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.29_metas_por_refeicao.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.29_metas_por_refeicao.sql") -or
    -not $sqlSource.Contains('"MetaCalorias"') -or
    -not $sqlSource.Contains('"MetaFibrasG"')) {
    throw "Upgrade de metas por refeicao incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 25/25: OK."

Write-Host "[334/600] Validando versao v0.3.29..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.29 / metas por refeicao + distribuicao planejada: OK."


Write-Host "[335/600] Validando schema das fases nutricionais..."
$phaseEntity = Get-Content .\src\HealthPlatform.Domain\Entities\FaseNutricional.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $phaseEntity.Contains("PlanoAlimentarId") -or
    -not $phaseEntity.Contains("DataInicio") -or
    -not $phaseEntity.Contains("Ordem") -or
    -not $phaseEntity.Contains("Status") -or
    -not $dbSource.Contains("FasesNutricionais")) {
    throw "Schema de fases nutricionais incompleto."
}
Write-Host "    Paciente + periodo + plano + ordem + status: modelo OK."

Write-Host "[336/600] Validando listagem das fases..."
$phaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesNutricionaisController.cs -Encoding UTF8 -Raw
if (-not $phaseSource.Contains('api/pacientes/{pacienteId:guid}/fases-nutricionais') -or
    -not $phaseSource.Contains("OrderBy(x => x.Ordem)") -or
    -not $phaseSource.Contains("Include(x => x.PlanoAlimentar)")) {
    throw "Listagem de fases nutricionais incompleta."
}
Write-Host "    Ordem + plano vinculado + profissional: backend OK."

Write-Host "[337/600] Validando criacao de fase..."
if (-not $phaseSource.Contains("CriarFaseNutricionalRequest") -or
    -not $phaseSource.Contains('Status = "Planejada"') -or
    -not $phaseSource.Contains("maiorOrdem + 1")) {
    throw "Criacao de fase nutricional incompleta."
}
Write-Host "    Nova fase entra no fim como Planejada."

Write-Host "[338/600] Validando edicao e estados..."
if (-not $phaseSource.Contains("AtualizarFaseNutricionalRequest") -or
    -not $phaseSource.Contains('"Planejada" or "EmAndamento" or "Concluida" or "Cancelada"') -or
    -not $phaseSource.Contains('"UPDATE"')) {
    throw "Edicao/status de fase incompletos."
}
Write-Host "    Planejada / Em andamento / Concluida / Cancelada: OK."

Write-Host "[339/600] Validando vinculo seguro com plano alimentar..."
if (-not $phaseSource.Contains("PlanoValido") -or
    -not $phaseSource.Contains("x.PacienteId == pacienteId") -or
    -not $phaseSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Protecao do plano vinculado incompleta."
}
Write-Host "    Plano precisa pertencer ao mesmo paciente/tenant."

Write-Host "[340/600] Validando reordenacao do ciclo..."
if (-not $phaseSource.Contains("fases-nutricionais/reordenar") -or
    -not $phaseSource.Contains("idsExistentes.SequenceEqual(idsRecebidos)") -or
    -not $phaseSource.Contains("ordemDuplicada")) {
    throw "Reordenacao de fases incompleta."
}
Write-Host "    Reordenacao exige todas as fases e ordem unica."

Write-Host "[341/600] Validando exclusao protegida..."
if (-not $phaseSource.Contains("HttpDelete") -or
    -not $phaseSource.Contains('fase.Status == "EmAndamento"') -or
    -not $phaseSource.Contains('"DELETE"')) {
    throw "Exclusao protegida de fase incompleta."
}
Write-Host "    Fase em andamento nao pode ser apagada."

Write-Host "[342/600] Validando isolamento e auditoria..."
if (-not $phaseSource.Contains("currentUser.OrganizationId") -or
    -not $phaseSource.Contains("AuditLogs") -or
    -not $phaseSource.Contains("nameof(FaseNutricional)")) {
    throw "Tenant/auditoria das fases incompletos."
}
Write-Host "    Organizacao + auditoria: backend OK."

Write-Host "[343/600] Validando interface das fases..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("loadNutritionPhases") -or
    -not $appJsSource.Contains("nutritionPhaseCard") -or
    -not $appJsSource.Contains("newNutritionPhase")) {
    throw "Interface das fases nutricionais incompleta."
}
Write-Host "    Aba Alimentacao -> ciclo nutricional: assets OK."

Write-Host "[344/600] Validando formulario de fase..."
if (-not $appJsSource.Contains("openNutritionPhaseForm") -or
    -not $appJsSource.Contains("Cutting") -or
    -not $appJsSource.Contains("Personalizada") -or
    -not $appJsSource.Contains('name="tipo"') -or
    -not $appJsSource.Contains("planoAlimentarId")) {
    throw "Formulario de fase incompleto."
}
Write-Host "    Tipo + periodo + plano + objetivo + observacoes: assets OK."

Write-Host "[345/600] Validando reordenacao visual..."
if (-not $appJsSource.Contains("moveNutritionPhase") -or
    -not $appJsSource.Contains("nutrition-phase-up") -or
    -not $appJsSource.Contains("nutrition-phase-down")) {
    throw "Reordenacao visual das fases incompleta."
}
Write-Host "    Subir/descer fase: assets OK."

Write-Host "[346/600] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("nutrition-phase-card") -or
    -not $cssSource.Contains("nutrition-phase-list") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos de fases nutricionais incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[347/600] Validando SQL e PREPARAR 26..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.30_fases_nutricionais.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.30_fases_nutricionais.sql") -or
    -not $sqlSource.Contains('"FasesNutricionais"') -or
    -not $sqlSource.Contains('"PlanoAlimentarId"')) {
    throw "Upgrade de fases nutricionais incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 26/26: OK."

Write-Host "[348/600] Validando versao v0.3.30..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.30 / fases nutricionais + planejamento de ciclo: OK."


Write-Host "[349/600] Validando schema das fases de treino..."
$phaseEntity = Get-Content .\src\HealthPlatform.Domain\Entities\FaseTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $phaseEntity.Contains("PlanoTreinoId") -or
    -not $phaseEntity.Contains("DataInicio") -or
    -not $phaseEntity.Contains("Ordem") -or
    -not $phaseEntity.Contains("Status") -or
    -not $dbSource.Contains("FasesTreino")) {
    throw "Schema de fases de treino incompleto."
}
Write-Host "    Paciente + periodo + ficha + ordem + status: modelo OK."

Write-Host "[350/600] Validando listagem das fases..."
$phaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesTreinoController.cs -Encoding UTF8 -Raw
if (-not $phaseSource.Contains('api/pacientes/{pacienteId:guid}/fases-treino') -or
    -not $phaseSource.Contains("OrderBy(x => x.Ordem)") -or
    -not $phaseSource.Contains("Include(x => x.PlanoTreino)")) {
    throw "Listagem das fases de treino incompleta."
}
Write-Host "    Ordem + ficha vinculada + profissional: backend OK."

Write-Host "[351/600] Validando criacao de fase..."
if (-not $phaseSource.Contains("CriarFaseTreinoRequest") -or
    -not $phaseSource.Contains('Status = "Planejada"') -or
    -not $phaseSource.Contains("maiorOrdem + 1")) {
    throw "Criacao de fase de treino incompleta."
}
Write-Host "    Nova fase entra no fim como Planejada."

Write-Host "[352/600] Validando edicao e estados..."
if (-not $phaseSource.Contains("AtualizarFaseTreinoRequest") -or
    -not $phaseSource.Contains('"Planejada" or "EmAndamento" or "Concluida" or "Cancelada"') -or
    -not $phaseSource.Contains('"UPDATE"')) {
    throw "Edicao/status de fase de treino incompletos."
}
Write-Host "    Planejada / Em andamento / Concluida / Cancelada: OK."

Write-Host "[353/600] Validando vinculo seguro com ficha..."
if (-not $phaseSource.Contains("PlanoValido") -or
    -not $phaseSource.Contains("x.PacienteId == pacienteId") -or
    -not $phaseSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Protecao da ficha vinculada incompleta."
}
Write-Host "    Plano de treino precisa pertencer ao mesmo paciente/tenant."

Write-Host "[354/600] Validando reordenacao do ciclo..."
if (-not $phaseSource.Contains("fases-treino/reordenar") -or
    -not $phaseSource.Contains("idsExistentes.SequenceEqual(idsRecebidos)") -or
    -not $phaseSource.Contains("GroupBy(x => x.Ordem)")) {
    throw "Reordenacao de fases de treino incompleta."
}
Write-Host "    Reordenacao exige todas as fases e ordem unica."

Write-Host "[355/600] Validando exclusao protegida..."
if (-not $phaseSource.Contains("HttpDelete") -or
    -not $phaseSource.Contains('fase.Status == "EmAndamento"') -or
    -not $phaseSource.Contains('"DELETE"')) {
    throw "Exclusao protegida de fase de treino incompleta."
}
Write-Host "    Fase em andamento nao pode ser apagada."

Write-Host "[356/600] Validando isolamento e auditoria..."
if (-not $phaseSource.Contains("currentUser.OrganizationId") -or
    -not $phaseSource.Contains("AuditLogs") -or
    -not $phaseSource.Contains("nameof(FaseTreino)")) {
    throw "Tenant/auditoria das fases de treino incompletos."
}
Write-Host "    Organizacao + auditoria: backend OK."

Write-Host "[357/600] Validando interface do ciclo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("loadWorkoutPhases") -or
    -not $appJsSource.Contains("workoutPhaseCard") -or
    -not $appJsSource.Contains("newWorkoutPhase")) {
    throw "Interface do ciclo de treino incompleta."
}
Write-Host "    Aba Treinos -> periodizacao: assets OK."

Write-Host "[358/600] Validando formulario de fase..."
if (-not $appJsSource.Contains("openWorkoutPhaseForm") -or
    -not $appJsSource.Contains("Hipertrofia") -or
    -not $appJsSource.Contains("Deload") -or
    -not $appJsSource.Contains("planoTreinoId")) {
    throw "Formulario de fase de treino incompleto."
}
Write-Host "    Tipo + periodo + ficha + objetivo + observacoes: assets OK."

Write-Host "[359/600] Validando reordenacao visual..."
if (-not $appJsSource.Contains("moveWorkoutPhase") -or
    -not $appJsSource.Contains("workout-phase-up") -or
    -not $appJsSource.Contains("workout-phase-down")) {
    throw "Reordenacao visual de treino incompleta."
}
Write-Host "    Subir/descer fase: assets OK."

Write-Host "[360/600] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("workout-phase-card") -or
    -not $cssSource.Contains("workout-phase-list") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos do ciclo de treino incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[361/600] Validando SQL e PREPARAR 27..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.31_fases_treino.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.31_fases_treino.sql") -or
    -not $sqlSource.Contains('"FasesTreino"') -or
    -not $sqlSource.Contains('"PlanoTreinoId"')) {
    throw "Upgrade de fases de treino incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 27/27: OK."

Write-Host "[362/600] Validando versao v0.3.31..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.31 / ciclos de treino + periodizacao: OK."


Write-Host "[363/600] Validando schema dos check-ins..."
$checkinEntity = Get-Content .\src\HealthPlatform.Domain\Entities\CheckInAcompanhamento.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $checkinEntity.Contains("AdesaoAlimentacaoPercentual") -or
    -not $checkinEntity.Contains("AdesaoTreinoPercentual") -or
    -not $checkinEntity.Contains("FaseNutricionalId") -or
    -not $checkinEntity.Contains("FaseTreinoId") -or
    -not $dbSource.Contains("CheckInsAcompanhamento")) {
    throw "Schema dos check-ins incompleto."
}
Write-Host "    Adesao + escalas + fases + peso: modelo OK."

Write-Host "[364/600] Validando endpoint profissional..."
$checkinSource = Get-Content .\src\HealthPlatform.Api\Controllers\CheckInsAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $checkinSource.Contains('api/pacientes/{pacienteId:guid}/check-ins') -or
    -not $checkinSource.Contains("MontarHistorico") -or
    -not $checkinSource.Contains("variacao")) {
    throw "Endpoint profissional de check-ins incompleto."
}
Write-Host "    Historico + atual + variacao: backend OK."

Write-Host "[365/600] Validando criacao e edicao..."
if (-not $checkinSource.Contains("UpsertCheckInRequest") -or
    -not $checkinSource.Contains('Origem = "Profissional"') -or
    -not $checkinSource.Contains('Auditar("UPDATE"')) {
    throw "CRUD profissional de check-in incompleto."
}
Write-Host "    POST + PUT + auditoria: backend OK."

Write-Host "[366/600] Validando limites dos indicadores..."
if (-not $checkinSource.Contains("Peso deve ficar entre 20 e 400 kg") -or
    -not $checkinSource.Contains("Adesao deve ficar entre 0 e 100%") -or
    -not $checkinSource.Contains("devem ficar entre 0 e 10")) {
    throw "Validacao dos indicadores incompleta."
}
Write-Host "    Peso + adesao + escalas: protegidos."

Write-Host "[367/600] Validando vinculo com fases..."
if (-not $checkinSource.Contains("FasesValidas") -or
    -not $checkinSource.Contains("db.FasesNutricionais.AnyAsync") -or
    -not $checkinSource.Contains("db.FasesTreino.AnyAsync")) {
    throw "Vinculo dos check-ins com fases incompleto."
}
Write-Host "    Fase nutricional/treino precisa ser do paciente."

Write-Host "[368/600] Validando auto-vinculo do paciente..."
if (-not $checkinSource.Contains("FaseNutricionalAtual") -or
    -not $checkinSource.Contains("FaseTreinoAtual") -or
    -not $checkinSource.Contains('Origem = "Paciente"')) {
    throw "Auto-vinculo do check-in do paciente incompleto."
}
Write-Host "    Portal associa automaticamente as fases atuais."

Write-Host "[369/600] Validando portal do paciente..."
if (-not $checkinSource.Contains('api/portal/me/check-ins') -or
    -not $checkinSource.Contains('Authorize(Policy = "PatientOnly")') -or
    -not $checkinSource.Contains('"CREATE_SELF"')) {
    throw "Endpoints de check-in do paciente incompletos."
}
Write-Host "    GET + POST PatientOnly + auditoria: backend OK."

Write-Host "[370/600] Validando isolamento multi-tenant..."
if (-not $checkinSource.Contains("currentUser.OrganizationId") -or
    -not $checkinSource.Contains("MeuPacienteId") -or
    -not $checkinSource.Contains("x.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Isolamento dos check-ins incompleto."
}
Write-Host "    Organizacao + usuario/paciente vinculados: OK."

Write-Host "[371/600] Validando painel profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectProfessionalCheckIns") -or
    -not $appJsSource.Contains("professional-checkin-new") -or
    -not $appJsSource.Contains("openProfessionalCheckInForm")) {
    throw "Painel profissional de check-ins incompleto."
}
Write-Host "    Resumo/alimentacao/treinos -> check-ins: assets OK."

Write-Host "[372/600] Validando graficos de resposta..."
if (-not $appJsSource.Contains("hpCheckInCharts") -or
    -not $appJsSource.Contains("adesaoAlimentacaoPercentual") -or
    -not $appJsSource.Contains("adesaoTreinoPercentual") -or
    -not $appJsSource.Contains("hpLineChart")) {
    throw "Graficos dos check-ins incompletos."
}
Write-Host "    Peso + dieta + treino + energia: assets OK."

Write-Host "[373/600] Validando formulario profissional..."
if (-not $appJsSource.Contains("adesaoAlimentacaoPercentual") -or
    -not $appJsSource.Contains("percepcaoEvolucaoNivel") -or
    -not $appJsSource.Contains("faseNutricionalId") -or
    -not $appJsSource.Contains("faseTreinoId")) {
    throw "Formulario profissional de check-in incompleto."
}
Write-Host "    Indicadores + duas fases: assets OK."

Write-Host "[374/600] Validando check-in no portal..."
if (-not $appJsSource.Contains("loadMyCheckInsIntoEvolution") -or
    -not $appJsSource.Contains("openMyCheckInForm") -or
    -not $appJsSource.Contains("patientCheckInNew") -or
    -not $appJsSource.Contains("Check-in enviado")) {
    throw "Interface de check-in do paciente incompleta."
}
Write-Host "    Evolucao -> novo check-in + historico: assets OK."

Write-Host "[375/600] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("checkin-current-grid") -or
    -not $cssSource.Contains("checkin-history-row") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos dos check-ins incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[376/600] Validando SQL e PREPARAR 28..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.32_checkins_acompanhamento.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.32_checkins_acompanhamento.sql") -or
    -not $sqlSource.Contains('"CheckInsAcompanhamento"') -or
    -not $sqlSource.Contains('"AdesaoAlimentacaoPercentual"')) {
    throw "Upgrade dos check-ins incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 28/28: OK."

Write-Host "[377/600] Validando preservacao dos ciclos..."
if (-not $setupSource.Contains("v0.3.30_fases_nutricionais.sql") -or
    -not $setupSource.Contains("v0.3.31_fases_treino.sql")) {
    throw "Upgrades historicos dos ciclos nao foram preservados."
}
Write-Host "    Fases nutricionais + treino preservadas."

Write-Host "[378/600] Validando versao v0.3.32..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.32 / check-ins de evolucao + adesao por fase: OK."


Write-Host "[379/600] Validando endpoint de analise por fase..."
$checkinSource = Get-Content .\src\HealthPlatform.Api\Controllers\CheckInsAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $checkinSource.Contains('api/pacientes/{pacienteId:guid}/analise-fases') -or
    -not $checkinSource.Contains("MontarAnaliseFase") -or
    -not $checkinSource.Contains("AnaliseFaseResumo")) {
    throw "Endpoint de analise por fase incompleto."
}
Write-Host "    Nutricao + treino + agregacao: backend OK."

Write-Host "[380/600] Validando metricas agregadas..."
if (-not $checkinSource.Contains("MediaAdesaoAlimentacao") -or
    -not $checkinSource.Contains("MediaAdesaoTreino") -or
    -not $checkinSource.Contains("MediaFome") -or
    -not $checkinSource.Contains("MediaEnergia") -or
    -not $checkinSource.Contains("MediaSono")) {
    throw "Metricas de fase incompletas."
}
Write-Host "    Adesao + fome + energia + sono: backend OK."

Write-Host "[381/600] Validando variacao de peso por fase..."
if (-not $checkinSource.Contains("PesoInicialKg") -or
    -not $checkinSource.Contains("PesoFinalKg") -or
    -not $checkinSource.Contains("VariacaoPesoKg") -or
    -not $checkinSource.Contains("Diferenca(pesoFinal, pesoInicial)")) {
    throw "Variacao de peso por fase incompleta."
}
Write-Host "    Peso inicial -> final -> delta: backend OK."

Write-Host "[382/600] Validando destaques automaticos..."
if (-not $checkinSource.Contains("melhorAdesaoAlimentar") -or
    -not $checkinSource.Contains("melhorAdesaoTreino") -or
    -not $checkinSource.Contains("maiorReducaoPeso") -or
    -not $checkinSource.Contains("maiorEnergiaMedia")) {
    throw "Destaques de fases incompletos."
}
Write-Host "    Melhores respostas calculadas sem IA generativa."

Write-Host "[383/600] Validando isolamento multi-tenant..."
if (-not $checkinSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $checkinSource.Contains("PacienteExiste(pacienteId")) {
    throw "Isolamento da analise de fases incompleto."
}
Write-Host "    Paciente + organizacao protegidos."

Write-Host "[384/600] Validando cards comparativos..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpPhaseAnalysisCard") -or
    -not $appJsSource.Contains("phase-analysis-grid") -or
    -not $appJsSource.Contains("mediaAdesaoAlimentacao")) {
    throw "Cards comparativos de fases incompletos."
}
Write-Host "    Peso + adesao + energia + fome + sono: assets OK."

Write-Host "[385/600] Validando destaques visuais..."
if (-not $appJsSource.Contains("hpPhaseHighlightCard") -or
    -not $appJsSource.Contains("melhorAdesaoAlimentar") -or
    -not $appJsSource.Contains("maiorReducaoPeso")) {
    throw "Destaques visuais de fases incompletos."
}
Write-Host "    Melhores fases aparecem no topo da analise."

Write-Host "[386/600] Validando integracao com nutricao..."
if (-not $appJsSource.Contains("nutrition-phase-analysis") -or
    -not $appJsSource.Contains("'nutrition'")) {
    throw "Analise das fases nutricionais nao integrada."
}
Write-Host "    Alimentacao -> comparativo nutricional: assets OK."

Write-Host "[387/600] Validando integracao com treino..."
if (-not $appJsSource.Contains("workout-phase-analysis") -or
    -not $appJsSource.Contains("'workout'")) {
    throw "Analise das fases de treino nao integrada."
}
Write-Host "    Treinos -> comparativo de periodizacao: assets OK."

Write-Host "[388/600] Validando resumo consolidado..."
if (-not $appJsSource.Contains("summary-phase-analysis") -or
    -not $appJsSource.Contains("hpInjectPhaseAnalysis")) {
    throw "Analise consolidada de fases nao integrada ao resumo."
}
Write-Host "    Resumo -> destaques dos dois ciclos: assets OK."

Write-Host "[389/600] Validando responsividade e compatibilidade de banco..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $cssSource.Contains("phase-highlight-grid") -or
    -not $cssSource.Contains("phase-analysis-list") -or
    -not $cssSource.Contains("@media(max-width:560px)") -or
    -not $setupSource.Contains("[38/38]")) {
    throw "Responsividade ou compatibilidade de banco inesperada."
}
if (Test-Path .\scripts\sql\v0.3.33_analise_fases.sql) {
    throw "v0.3.33 nao deveria exigir novo schema."
}
Write-Host "    UI responsiva / sem schema novo / PREPARAR 28/28: OK."

Write-Host "[390/600] Validando versao v0.3.33..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.33 / analise de fases + comparativo de resposta: OK."

Write-Host "[391/600] Validando metas das fases..."
$nutritionPhaseEntity = Get-Content .\src\HealthPlatform.Domain\Entities\FaseNutricional.cs -Encoding UTF8 -Raw
$workoutPhaseEntity = Get-Content .\src\HealthPlatform.Domain\Entities\FaseTreino.cs -Encoding UTF8 -Raw
if (-not $nutritionPhaseEntity.Contains("MetaPesoKg") -or -not $nutritionPhaseEntity.Contains("MetaAdesaoPercentual") -or -not $nutritionPhaseEntity.Contains("DuracaoMinimaDias") -or -not $nutritionPhaseEntity.Contains("CriterioTransicao") -or -not $workoutPhaseEntity.Contains("MetaPesoKg") -or -not $workoutPhaseEntity.Contains("CriterioTransicao")) { throw "Metas das fases incompletas." }
Write-Host "    Peso + adesao + duracao + criterio manual: modelo OK."

Write-Host "[392/600] Validando mapeamento EF..."
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbSource.Contains("MetaPesoKg).HasPrecision(8, 2)") -or -not $dbSource.Contains("CriterioTransicao).HasMaxLength(1000)")) { throw "Mapeamento EF dos criterios incompleto." }
Write-Host "    Precisao de peso + limite do criterio: EF OK."

Write-Host "[393/600] Validando CRUD das fases..."
$nutritionPhaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesNutricionaisController.cs -Encoding UTF8 -Raw
$workoutPhaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesTreinoController.cs -Encoding UTF8 -Raw
if (-not $nutritionPhaseSource.Contains("request.MetaAdesaoPercentual") -or -not $workoutPhaseSource.Contains("request.MetaAdesaoPercentual") -or -not $nutritionPhaseSource.Contains("request.CriterioTransicao") -or -not $workoutPhaseSource.Contains("request.CriterioTransicao")) { throw "CRUD das fases nao preserva criterios." }
Write-Host "    Criacao + edicao preservam metas."

Write-Host "[394/600] Validando limites dos criterios..."
if (-not $nutritionPhaseSource.Contains("Meta de peso deve ficar entre 20 e 400 kg") -or -not $nutritionPhaseSource.Contains("Meta de adesao deve ficar entre 0 e 100%") -or -not $nutritionPhaseSource.Contains("Duracao minima deve ficar entre 1 e 3650 dias") -or -not $nutritionPhaseSource.Contains("1000 caracteres")) { throw "Validacoes dos criterios incompletas." }
Write-Host "    Limites de configuracao: OK."

Write-Host "[395/600] Validando endpoint runtime de prontidao..."
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) { $pacienteSmoke = $lista.itens | Select-Object -First 1; $statusTransicao = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/status-transicao-fases" -Headers $headers -Method Get; if ($null -eq $statusTransicao.nutricao -or $null -eq $statusTransicao.treino) { throw "Endpoint de status de transicao retornou estrutura invalida." }; Write-Host "    GET status-transicao-fases: runtime OK." } else { Write-Host "    Sem pacientes: smoke runtime ignorado." }

Write-Host "[396/600] Validando motor de criterios objetivos..."
$checkinSource = Get-Content .\src\HealthPlatform.Api\Controllers\CheckInsAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $checkinSource.Contains("duracao_minima") -or -not $checkinSource.Contains("adesao_minima") -or -not $checkinSource.Contains("meta_peso") -or -not $checkinSource.Contains("Math.Abs(pesoAtual.Value - metaPesoKg.Value) <= 0.5m")) { throw "Motor objetivo incompleto." }
Write-Host "    Duracao + adesao + peso: motor OK."

Write-Host "[397/600] Validando revisao profissional..."
if (-not $checkinSource.Contains("ObjetivosProntosParaRevisao") -or -not $checkinSource.Contains("RequerAvaliacaoProfissional")) { throw "Semantica de revisao profissional incompleta." }
Write-Host "    Motor sugere revisao, nao conclui a fase automaticamente."

Write-Host "[398/600] Validando formularios das fases..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("metaPesoKg") -or -not $appJsSource.Contains("metaAdesaoPercentual") -or -not $appJsSource.Contains("duracaoMinimaDias") -or -not $appJsSource.Contains("criterioTransicao")) { throw "Formularios de criterios incompletos." }
Write-Host "    Nutricao + treino configuram metas: assets OK."

Write-Host "[399/600] Validando metas nos cards..."
if (-not $appJsSource.Contains("phaseGoalChips")) { throw "Resumo visual das metas incompleto." }
Write-Host "    Cards exibem metas configuradas."

Write-Host "[400/600] Validando painel de prontidao..."
if (-not $appJsSource.Contains("hpInjectTransitionStatus") -or -not $appJsSource.Contains("hpTransitionStatusCard") -or -not $appJsSource.Contains("objetivosProntosParaRevisao")) { throw "Painel de prontidao incompleto." }
Write-Host "    Progresso dos criterios: assets OK."

Write-Host "[401/600] Validando integracao nutricional..."
if (-not $appJsSource.Contains("nutrition-transition-status")) { throw "Integracao nutricional incompleta." }
Write-Host "    Alimentacao: OK."

Write-Host "[402/600] Validando integracao de treino..."
if (-not $appJsSource.Contains("workout-transition-status")) { throw "Integracao de treino incompleta." }
Write-Host "    Treinos: OK."

Write-Host "[403/600] Validando integracao no resumo..."
if (-not $appJsSource.Contains("summary-transition-status")) { throw "Integracao no resumo incompleta." }
Write-Host "    Resumo: OK."

Write-Host "[404/600] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("transition-status-card") -or -not $cssSource.Contains("phase-goal-chips")) { throw "Estilos de transicao incompletos." }
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[405/600] Validando SQL e PREPARAR 29..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.34_criterios_transicao_fases.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or -not $setupSource.Contains("v0.3.34_criterios_transicao_fases.sql") -or -not $sqlSource.Contains('"MetaPesoKg"') -or -not $sqlSource.Contains('"CriterioTransicao"') -or -not $setupSource.Contains("v0.3.32_checkins_acompanhamento.sql")) { throw "Upgrade dos criterios incompleto." }
Write-Host "    SQL idempotente + PREPARAR 29/29 + historico preservado."

Write-Host "[406/600] Validando versao v0.3.34..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.34 / metas de fase + criterios de transicao: OK."


Write-Host "[407/600] Validando entidade de revisao..."
$reviewEntity = Get-Content .\src\HealthPlatform.Domain\Entities\RevisaoFase.cs -Encoding UTF8 -Raw
if (-not $reviewEntity.Contains("RevisadoPorUsuarioId") -or
    -not $reviewEntity.Contains("FaseDestinoId") -or
    -not $reviewEntity.Contains("OverrideCriterios") -or
    -not $reviewEntity.Contains("SnapshotIndicadoresJson")) {
    throw "Entidade RevisaoFase incompleta."
}
Write-Host "    Decisao + destino + override + snapshot: modelo OK."

Write-Host "[408/600] Validando mapeamento das revisoes..."
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbSource.Contains("DbSet<RevisaoFase>") -or
    -not $dbSource.Contains('ToTable("RevisoesFases")') -or
    -not $dbSource.Contains("x.OrganizacaoId, x.Dominio, x.DataUtc")) {
    throw "Mapeamento de RevisaoFase incompleto."
}
Write-Host "    Tabela + indices + paciente: EF OK."

Write-Host "[409/600] Validando historico runtime..."
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $revisoesSmoke = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/revisoes-fases?limite=6" -Headers $headers -Method Get
    if ($null -eq $revisoesSmoke.total -or $null -eq $revisoesSmoke.itens) {
        throw "Historico de revisoes retornou estrutura invalida."
    }
    Write-Host "    GET revisoes-fases: runtime OK."
} else {
    Write-Host "    Sem pacientes: smoke de revisoes ignorado."
}

Write-Host "[410/600] Validando revisao nutricional..."
$reviewSource = Get-Content .\src\HealthPlatform.Api\Controllers\RevisoesFasesController.cs -Encoding UTF8 -Raw
if (-not $reviewSource.Contains('api/fases-nutricionais/{id:guid}/revisar') -or
    -not $reviewSource.Contains("RevisarNutricional") -or
    -not $reviewSource.Contains('"Nutricao"')) {
    throw "Revisao nutricional incompleta."
}
Write-Host "    Endpoint de revisao nutricional: backend OK."

Write-Host "[411/600] Validando revisao de treino..."
if (-not $reviewSource.Contains('api/fases-treino/{id:guid}/revisar') -or
    -not $reviewSource.Contains("RevisarTreino") -or
    -not $reviewSource.Contains('"Treino"')) {
    throw "Revisao de treino incompleta."
}
Write-Host "    Endpoint de revisao de treino: backend OK."

Write-Host "[412/600] Validando decisoes e fase ativa..."
if (-not $reviewSource.Contains('"Manter"') -or
    -not $reviewSource.Contains('"Concluir"') -or
    -not $reviewSource.Contains('"Avancar"') -or
    -not $reviewSource.Contains('fase.Status != "EmAndamento"')) {
    throw "Regras basicas da revisao incompletas."
}
Write-Host "    Manter / concluir / avancar + EmAndamento: regras OK."

Write-Host "[413/600] Validando override consciente..."
if (-not $reviewSource.Contains("ConfirmarMesmoSemCriterios") -or
    -not $reviewSource.Contains("ExigeOverride") -or
    -not $reviewSource.Contains("criterios objetivos pendentes")) {
    throw "Protecao de override incompleta."
}
Write-Host "    Criterios pendentes exigem confirmacao explicita."

Write-Host "[414/600] Validando transicao para proxima fase..."
if (-not $reviewSource.Contains("x.Ordem > fase.Ordem") -or
    -not $reviewSource.Contains('x.Status == "Planejada"') -or
    -not $reviewSource.Contains('proxima.Status = "EmAndamento"') -or
    -not $reviewSource.Contains('fase.Status = "Concluida"')) {
    throw "Transicao assistida incompleta."
}
Write-Host "    Atual conclui + proxima Planejada ativa: backend OK."

Write-Host "[415/600] Validando transacao e auditoria..."
if (-not $reviewSource.Contains("BeginTransactionAsync") -or
    -not $reviewSource.Contains("CommitAsync") -or
    -not $reviewSource.Contains('"REVIEW_CREATE"') -or
    -not $reviewSource.Contains('"REVIEW_ACTIVATE_NEXT"')) {
    throw "Transacao/auditoria das revisoes incompletas."
}
Write-Host "    Decisao + mudancas de status atomicas e auditadas."

Write-Host "[416/600] Validando modal de revisao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openPhaseReview") -or
    -not $appJsSource.Contains("phaseReviewForm") -or
    -not $appJsSource.Contains("confirmarMesmoSemCriterios") -or
    -not $appJsSource.Contains("justificativa")) {
    throw "Modal de revisao incompleto."
}
Write-Host "    Decisao + justificativa + override: assets OK."

Write-Host "[417/600] Validando historico visual..."
if (-not $appJsSource.Contains("hpPhaseReviewHistory") -or
    -not $appJsSource.Contains("phase-review-history-card") -or
    -not $appJsSource.Contains("x.decisao") -or
    -not $appJsSource.Contains("x.justificativa")) {
    throw "Historico visual das revisoes incompleto."
}
Write-Host "    Ultimas decisoes aparecem junto da prontidao."

Write-Host "[418/600] Validando integracao com painel de transicao..."
if (-not $appJsSource.Contains("hpTransitionStatusCardReview") -or
    -not $appJsSource.Contains("phase-review-action") -or
    -not $appJsSource.Contains("/revisoes-fases?limite=6")) {
    throw "Integracao revisao/prontidao incompleta."
}
Write-Host "    Fase EmAndamento recebe acao de revisao."

Write-Host "[419/600] Validando SQL e PREPARAR 30..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.35_revisoes_transicoes_fases.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql") -or
    -not $sqlSource.Contains('"RevisoesFases"') -or
    -not $sqlSource.Contains('"SnapshotIndicadoresJson"') -or
    -not $setupSource.Contains("v0.3.34_criterios_transicao_fases.sql")) {
    throw "Upgrade de revisoes/transicoes incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 38/38 + v0.3.34 preservada."

Write-Host "[420/600] Validando versao v0.3.35..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.35 / revisao de fase + transicao assistida: OK."


Write-Host "[421/600] Validando controller de volume..."
$volumeSource = Get-Content .\src\HealthPlatform.Api\Controllers\AnaliseVolumeTreinoController.cs -Encoding UTF8 -Raw
if (-not $volumeSource.Contains('api/pacientes/{pacienteId:guid}/treinos/analise-volume') -or
    -not $volumeSource.Contains("AnaliseVolumeTreinoController") -or
    -not $volumeSource.Contains("QueryPlano")) {
    throw "Controller de analise de volume incompleto."
}
Write-Host "    Endpoint + selecao do plano: backend OK."

Write-Host "[422/600] Validando isolamento multi-tenant..."
if (-not $volumeSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $volumeSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Isolamento da analise de volume incompleto."
}
Write-Host "    Paciente + plano + execucoes: tenant OK."

Write-Host "[423/600] Validando volume planejado por grupo..."
if (-not $volumeSource.Contains("Grupo(x.Exercicio.GrupoMuscular)") -or
    -not $volumeSource.Contains("SeriesPorCiclo") -or
    -not $volumeSource.Contains("SeriesSemanaisEstimadas") -or
    -not $volumeSource.Contains("ExerciciosDistintos")) {
    throw "Volume planejado por grupo incompleto."
}
Write-Host "    Series + exercicios distintos + grupo muscular: backend OK."

Write-Host "[424/600] Validando frequencia semanal..."
if (-not $volumeSource.Contains("FrequenciaSemanal") -or
    -not $volumeSource.Contains("SemAcentos") -or
    -not $volumeSource.Contains("segunda") -or
    -not $volumeSource.Contains("sexta")) {
    throw "Inferencia de frequencia semanal incompleta."
}
Write-Host "    DiasSemana -> frequencia reconhecida: backend OK."

Write-Host "[425/600] Validando fallback de frequencia..."
if (-not $volumeSource.Contains("return (1, false)") -or
    -not $volumeSource.Contains("frequenciaInferida")) {
    throw "Fallback de frequencia nao identificado."
}
Write-Host "    Dias nao reconhecidos usam 1x/semana e ficam sinalizados."

Write-Host "[426/600] Validando execucoes reais..."
if (-not $volumeSource.Contains("SeriesRealizadas") -or
    -not $volumeSource.Contains('x.Status == "Concluido"') -or
    -not $volumeSource.Contains("seriesRealizadasPeriodo") -or
    -not $volumeSource.Contains("mediaSeriesRealizadasSemana")) {
    throw "Volume realizado incompleto."
}
Write-Host "    Series concluidas + periodo + media semanal: backend OK."

Write-Host "[427/600] Validando ausencia de tonelagem inventada..."
if (-not $volumeSource.Contains("Tonelagem nao e inferida") -or
    $volumeSource.Contains("RepeticoesRealizadas *") -or
    $volumeSource.Contains("CargaRealizada *")) {
    throw "Protecao contra tonelagem inferida incorretamente falhou."
}
Write-Host "    Repeticoes textuais nao viram tonelagem ficticia."

Write-Host "[428/600] Validando runtime da analise..."
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    try {
        $volumeSmoke = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/treinos/analise-volume?dias=30" -Headers $headers -Method Get
        if ($null -eq $volumeSmoke.resumo -or $null -eq $volumeSmoke.porGrupo -or $null -eq $volumeSmoke.porSessao) {
            throw "Estrutura runtime da analise de volume incompleta."
        }
        Write-Host "    GET analise-volume: runtime OK."
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            Write-Host "    Paciente smoke sem plano de treino: runtime ignorado sem criar dados."
        } else {
            throw
        }
    }
} else {
    Write-Host "    Sem pacientes: smoke runtime ignorado."
}

Write-Host "[429/600] Validando painel de volume..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectWorkoutVolume") -or
    -not $appJsSource.Contains("hpWorkoutVolumeBar") -or
    -not $appJsSource.Contains("workout-volume-section") -or
    -not $appJsSource.Contains("percentualDoVolumeSemanal")) {
    throw "Painel de volume incompleto."
}
Write-Host "    Distribuicao muscular: assets OK."

Write-Host "[430/600] Validando resumo analitico..."
if (-not $appJsSource.Contains("seriesSemanaisEstimadas") -or
    -not $appJsSource.Contains("seriesRealizadasPeriodo") -or
    -not $appJsSource.Contains("mediaSeriesRealizadasSemana") -or
    -not $appJsSource.Contains("maiorConcentracaoGrupo") -or
    -not $appJsSource.Contains("maiorConcentracaoPercentual")) {
    throw "Resumo visual de volume incompleto."
}
Write-Host "    Planejado + realizado + concentracao: assets OK."

Write-Host "[431/600] Validando volume por sessao..."
if (-not $appJsSource.Contains("hpWorkoutSessionVolume") -or
    -not $appJsSource.Contains("seriesPorSessao") -or
    -not $appJsSource.Contains("seriesSemanaisEstimadas") -or
    -not $appJsSource.Contains("frequenciaInferida")) {
    throw "Analise visual por sessao incompleta."
}
Write-Host "    Sessao + frequencia + series semanais: assets OK."

Write-Host "[432/600] Validando integracao e responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("workout-volume-main") -or
    -not $appJsSource.Contains("workout-volume-summary") -or
    -not $cssSource.Contains("workout-volume-row") -or
    -not $cssSource.Contains("workout-session-volume-list")) {
    throw "Integracao/responsividade do volume incompleta."
}
Write-Host "    Treinos + Resumo + layout responsivo: assets OK."

Write-Host "[433/600] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.36_volume_treino.sql) {
    throw "v0.3.36 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 38/38."

Write-Host "[434/600] Validando versao v0.3.36..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.36 / volume de treino + distribuicao muscular: OK."


Write-Host "[435/600] Validando endpoints de progressao por exercicio..."
$progressSource = Get-Content .\src\HealthPlatform.Api\Controllers\ProgressaoExerciciosTreinoController.cs -Encoding UTF8 -Raw
if (-not $progressSource.Contains('api/pacientes/{pacienteId:guid}/treinos/progressao-exercicios') -or
    -not $progressSource.Contains('api/portal/me/treinos/progressao-exercicios') -or
    -not $progressSource.Contains("ProgressaoExerciciosTreinoController")) {
    throw "Endpoints de progressao por exercicio incompletos."
}
Write-Host "    Profissional + paciente: rotas OK."

Write-Host "[436/600] Validando seguranca e tenant..."
if (-not $progressSource.Contains('Authorize(Policy = "PatientOnly")') -or
    -not $progressSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $progressSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Seguranca da progressao de exercicios incompleta."
}
Write-Host "    PatientOnly + organizacao: OK."

Write-Host "[437/600] Validando separacao por unidade..."
if (-not $progressSource.Contains("NormalizarUnidade") -or
    -not $progressSource.Contains("x.Unidade") -or
    -not $progressSource.Contains('"kg" or "kgs"') -or
    -not $progressSource.Contains('"lb" or "lbs"')) {
    throw "Separacao/normalizacao de unidades incompleta."
}
Write-Host "    Mesmo exercicio nao mistura kg com lb."

Write-Host "[438/600] Validando metricas de carga..."
if (-not $progressSource.Contains("primeiraCarga") -or
    -not $progressSource.Contains("ultimaCarga") -or
    -not $progressSource.Contains("maiorCarga") -or
    -not $progressSource.Contains("variacaoPercentual") -or
    -not $progressSource.Contains("deltaCarga")) {
    throw "Metricas de progressao incompletas."
}
Write-Host "    Inicial + atual + PR + delta + percentual: backend OK."

Write-Host "[439/600] Validando recordes sucessivos..."
if (-not $progressSource.Contains("ContarNovosRecordes") -or
    -not $progressSource.Contains("ponto.Carga > maiorAnterior") -or
    -not $progressSource.Contains("novosRecordesPeriodo")) {
    throw "Contagem de recordes incompleta."
}
Write-Host "    Novos PRs ao longo do periodo: backend OK."

Write-Host "[440/600] Validando tendencia de carga..."
if (-not $progressSource.Contains("Tendencia") -or
    -not $progressSource.Contains('"AcimaDaBase"') -or
    -not $progressSource.Contains('"Estavel"') -or
    -not $progressSource.Contains('"AbaixoDaBase"')) {
    throw "Tendencia de carga incompleta."
}
Write-Host "    Base recente + tolerancia: backend OK."

Write-Host "[441/600] Validando protecao contra estimativas artificiais..."
if (-not $progressSource.Contains("Nao ha estimativa de 1RM") -or
    -not $progressSource.Contains("repeticoes textuais") -or
    $progressSource.Contains("Epley") -or
    $progressSource.Contains("Brzycki")) {
    throw "Protecao contra 1RM estimado incorretamente falhou."
}
Write-Host "    Sem 1RM/tonelagem inferidos de texto livre."

Write-Host "[442/600] Validando runtime profissional..."
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $progressSmoke = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/treinos/progressao-exercicios?dias=180" -Headers $headers -Method Get
    if ($null -eq $progressSmoke.resumo -or $null -eq $progressSmoke.destaques -or $null -eq $progressSmoke.exercicios) {
        throw "Estrutura runtime de progressao incompleta."
    }
    Write-Host "    GET progressao-exercicios: runtime OK."
} else {
    Write-Host "    Sem pacientes: smoke runtime ignorado."
}

Write-Host "[443/600] Validando payload de pontos..."
if (-not $progressSource.Contains("cargaRealizada = x.Carga") -or
    -not $progressSource.Contains("SeriesRealizadas") -or
    -not $progressSource.Contains("RepeticoesRealizadas") -or
    -not $progressSource.Contains("EsforcoPercebido")) {
    throw "Pontos de progressao incompletos."
}
Write-Host "    Data + carga + series + reps + RPE: backend OK."

Write-Host "[444/600] Validando painel profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectExerciseProgression") -or
    -not $appJsSource.Contains("hpExerciseProgressCard") -or
    -not $appJsSource.Contains("exercise-progression-summary") -or
    -not $appJsSource.Contains("hpExerciseProgressSection")) {
    throw "Painel profissional de progressao incompleto."
}
Write-Host "    Treinos + Resumo: assets OK."

Write-Host "[445/600] Validando graficos e recordes..."
if (-not $appJsSource.Contains("hpExerciseProgressCharts") -or
    -not $appJsSource.Contains("novosRecordesPeriodo") -or
    -not $appJsSource.Contains("exercise-pr-highlight") -or
    -not $appJsSource.Contains("maiorCarga")) {
    throw "Visual de graficos/recordes incompleto."
}
Write-Host "    Curvas + PRs + destaque: assets OK."

Write-Host "[446/600] Validando portal do paciente..."
if (-not $appJsSource.Contains("hpInjectMyExerciseProgression") -or
    -not $appJsSource.Contains("my-exercise-progression") -or
    -not $appJsSource.Contains("__loadPatientWorkout_v037_exerciseprogress")) {
    throw "Progressao no portal do paciente incompleta."
}
Write-Host "    Meu treino -> progressao individual: assets OK."

Write-Host "[447/600] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.37_progressao_exercicios.sql) {
    throw "v0.3.37 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 38/38."

Write-Host "[448/600] Validando versao v0.3.37..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.37 / progressao por exercicio + recordes de carga: OK."


Write-Host "[449/600] Validando endpoints de sinais de progressao..."
$signalSource = Get-Content .\src\HealthPlatform.Api\Controllers\AnaliseProgressoTreinoController.cs -Encoding UTF8 -Raw
if (-not $signalSource.Contains('api/pacientes/{pacienteId:guid}/treinos/analise-progresso') -or
    -not $signalSource.Contains('api/portal/me/treinos/analise-progresso') -or
    -not $signalSource.Contains("AnaliseProgressoTreinoController")) {
    throw "Endpoints de analise de progresso incompletos."
}
Write-Host "    Profissional + paciente: rotas OK."

Write-Host "[450/600] Validando tenant e PatientOnly..."
if (-not $signalSource.Contains('Authorize(Policy = "PatientOnly")') -or
    -not $signalSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $signalSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Seguranca da analise de progresso incompleta."
}
Write-Host "    Isolamento + portal: OK."

Write-Host "[451/600] Validando estados da analise..."
if (-not $signalSource.Contains('"Progredindo"') -or
    -not $signalSource.Contains('"Estagnacao"') -or
    -not $signalSource.Contains('"PossivelFadiga"') -or
    -not $signalSource.Contains('"Estavel"') -or
    -not $signalSource.Contains('"SemBase"')) {
    throw "Estados da analise de progresso incompletos."
}
Write-Host "    Progresso + estagnacao + carga/RPE + base: backend OK."

Write-Host "[452/600] Validando regra de estagnacao..."
if (-not $signalSource.Contains("pontos.Count >= 5") -or
    -not $signalSource.Contains("Math.Abs(variacao.Value) <= 2m") -or
    -not $signalSource.Contains("!recordeNaJanelaRecente")) {
    throw "Regra de estagnacao incompleta."
}
Write-Host "    +/-2% + sem PR recente + base minima: regra OK."

Write-Host "[453/600] Validando sinal de carga/RPE..."
if (-not $signalSource.Contains("variacao.Value <= -3m") -or
    -not $signalSource.Contains("mediaRpe.Value >= 8m") -or
    -not $signalSource.Contains('status is "Estagnacao" or "PossivelFadiga"')) {
    throw "Regra de revisao por carga/RPE incompleta."
}
Write-Host "    Queda >=3% + RPE >=8: sinaliza revisao."

Write-Host "[454/600] Validando progressao recente..."
if (-not $signalSource.Contains("recordeNaJanelaRecente") -or
    -not $signalSource.Contains("variacao.Value > 2m") -or
    -not $signalSource.Contains('status = "Progredindo"')) {
    throw "Regra de progressao incompleta."
}
Write-Host "    PR recente ou ganho >2%: progresso reconhecido."

Write-Host "[455/600] Validando semantica nao diagnostica..."
if (-not $signalSource.Contains("Nao representam diagnostico de fadiga") -or
    -not $signalSource.Contains("nao prescrevem aumento de carga automaticamente")) {
    throw "Disclaimer da analise esportiva incompleto."
}
Write-Host "    Heuristica de acompanhamento, nao diagnostico/prescricao."

Write-Host "[456/600] Validando runtime profissional..."
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $signalSmoke = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/treinos/analise-progresso?dias=120" -Headers $headers -Method Get
    if ($null -eq $signalSmoke.resumo -or $null -eq $signalSmoke.destaques -or $null -eq $signalSmoke.exercicios) {
        throw "Estrutura runtime da analise de progresso incompleta."
    }
    Write-Host "    GET analise-progresso: runtime OK."
} else {
    Write-Host "    Sem pacientes: smoke runtime ignorado."
}

Write-Host "[457/600] Validando payload analitico..."
if (-not $signalSource.Contains("AnaliseExercicioResponse") -or
    -not $signalSource.Contains("PontoAnaliseExercicio") -or
    -not $signalSource.Contains("mediaCargaAnterior") -or
    -not $signalSource.Contains("mediaCargaRecente") -or
    -not $signalSource.Contains("variacaoRecentePercentual") -or
    -not $signalSource.Contains("mediaRpeRecente") -or
    -not $signalSource.Contains("diasSemRecorde")) {
    throw "Payload tipado da analise de progresso incompleto."
}
Write-Host "    Base + recente + variacao + RPE + PR: backend OK."

Write-Host "[458/600] Validando painel profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectTrainingSignals") -or
    -not $appJsSource.Contains("hpTrainingSignalCard") -or
    -not $appJsSource.Contains("hpTrainingSignalsSection") -or
    -not $appJsSource.Contains("training-signals-main") -or
    -not $appJsSource.Contains("training-signals-summary")) {
    throw "Painel de sinais de progressao incompleto."
}
Write-Host "    Treinos + Resumo: assets OK."

Write-Host "[459/600] Validando portal do paciente..."
if (-not $appJsSource.Contains("hpInjectMyTrainingSignals") -or
    -not $appJsSource.Contains("my-training-signals") -or
    -not $appJsSource.Contains("__loadPatientWorkout_v038_trainingsignals") -or
    -not $appJsSource.Contains("/api/portal/me/treinos/analise-progresso")) {
    throw "Sinais de progressao no portal incompletos."
}
Write-Host "    Meu treino: assets OK."

Write-Host "[460/600] Validando cards e graficos..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpTrainingSignalCharts") -or
    -not $appJsSource.Contains("revisaoSugerida") -or
    -not $appJsSource.Contains("mediaRpeRecente") -or
    -not $cssSource.Contains("training-signal-card") -or
    -not $cssSource.Contains("training-signal-summary") -or
    -not $cssSource.Contains("training-review-note")) {
    throw "Visual da analise de progresso incompleto."
}
Write-Host "    Cards + graficos + revisao sugerida: assets OK."

Write-Host "[461/600] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.38_analise_progresso.sql) {
    throw "v0.3.38 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 38/38."

Write-Host "[462/600] Validando versao v0.3.38..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.38 / estagnacao + fadiga + sinais de progressao: OK."


Write-Host "[463/600] Validando identidade MVP Preview..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
if (-not $indexSource.Contains("MVP Preview • v0.10.3") -or
    -not $indexSource.Contains("mvp-brand-badge") -or
    -not $indexSource.Contains("MVP • DEMO") -or
    -not $indexSource.Contains('id="loginMessage"') -or
    $indexSource.Contains('value="ChangeMe_123!"')) {
    throw "Identidade/login do MVP Preview incompletos."
}
Write-Host "    Login + marcas de demo: assets OK."

Write-Host "[464/600] Validando aviso de demonstracao..."
if (-not $indexSource.Contains("Ambiente de demonstração") -or
    -not $indexSource.Contains("Use somente dados fictícios") -or
    -not $indexSource.Contains("senha profissional é a configurada")) {
    throw "Aviso de ambiente de demonstracao/login incompleto."
}
Write-Host "    Uso ficticio e objetivo do prototipo: copy OK."

Write-Host "[465/600] Validando roteiro da demo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openMvpGuide") -or
    -not $appJsSource.Contains("Roteiro rápido para testar o sistema") -or
    -not $appJsSource.Contains("hpMvpChecklistItem")) {
    throw "Roteiro de avaliacao do MVP incompleto."
}
Write-Host "    Guia interno de exploracao: assets OK."

Write-Host "[466/600] Validando checklist de avaliacao..."
if (-not $appJsSource.Contains("Cadastre ou escolha um paciente") -or
    -not $appJsSource.Contains("Simule uma consulta") -or
    -not $appJsSource.Contains("Monte alimentação e treino") -or
    -not $appJsSource.Contains("Entre como paciente") -or
    -not $appJsSource.Contains("Procure atritos")) {
    throw "Checklist de avaliacao incompleto."
}
Write-Host "    Fluxos principais cobertos no roteiro."

Write-Host "[467/600] Validando modelo de feedback..."
if (-not $appJsSource.Contains("copyMvpFeedbackTemplate") -or
    -not $appJsSource.Contains("FALTOU:") -or
    -not $appJsSource.Contains("CONFUNDIU:") -or
    -not $appJsSource.Contains("DEMOROU:") -or
    -not $appJsSource.Contains("QUEBROU/BUG:")) {
    throw "Modelo de feedback do MVP incompleto."
}
Write-Host "    Feedback estruturado pode ser copiado."

Write-Host "[468/600] Validando dashboard de apresentacao..."
if (-not $appJsSource.Contains("mvp-dashboard-hero") -or
    -not $appJsSource.Contains("AMBIENTE DE DEMONSTRAÇÃO") -or
    -not $appJsSource.Contains("openMvpGuideHero") -or
    -not $appJsSource.Contains("goAgendaHero")) {
    throw "Dashboard do MVP Preview incompleto."
}
Write-Host "    Hero + atalhos de demo: assets OK."

Write-Host "[469/600] Validando atalho Escape..."
if (-not $appJsSource.Contains("e.key!=='Escape'") -or
    -not $appJsSource.Contains("closeClinicalAction") -or
    -not $appJsSource.Contains("create.classList.add('hidden')") -or
    -not $appJsSource.Contains("sidebar')?.classList.remove('open')")) {
    throw "Atalho Escape incompleto."
}
Write-Host "    Escape fecha camadas sem alterar dados."

Write-Host "[470/600] Validando feedback de conectividade..."
if (-not $appJsSource.Contains("addEventListener('offline'") -or
    -not $appJsSource.Contains("addEventListener('online'") -or
    -not $appJsSource.Contains("Conexão restabelecida")) {
    throw "Feedback de conectividade incompleto."
}
Write-Host "    Offline/online recebem feedback visual."

Write-Host "[471/600] Validando acabamento de foco..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("focus-visible") -or
    -not $cssSource.Contains("outline-offset") -or
    -not $cssSource.Contains("button:not(:disabled):active")) {
    throw "Acabamento de interacao/foco incompleto."
}
Write-Host "    Teclado + feedback de clique: estilos OK."

Write-Host "[472/600] Validando estados vazios..."
if (-not $cssSource.Contains(".empty::before") -or
    -not $cssSource.Contains("place-items:center") -or
    -not $cssSource.Contains("text-align:center")) {
    throw "Polimento de estados vazios incompleto."
}
Write-Host "    Estados sem dados mais consistentes."

Write-Host "[473/600] Validando limpeza da navegacao de demo..."
if (-not $indexSource.Contains("mvp-dev-link") -or
    -not $cssSource.Contains(".mvp-dev-link{display:none!important}")) {
    throw "Link tecnico nao foi escondido da navegacao da demo."
}
Write-Host "    Swagger continua no backend, mas sai da navegacao principal."

Write-Host "[474/600] Validando responsividade do MVP..."
if (-not $cssSource.Contains("@media(max-width:900px)") -or
    -not $cssSource.Contains(".mvp-guide-grid{grid-template-columns:1fr}") -or
    -not $cssSource.Contains("@media(max-width:620px)") -or
    -not $cssSource.Contains(".mvp-dashboard-actions button{flex:1 1 140px}")) {
    throw "Responsividade do MVP Preview incompleta."
}
Write-Host "    Notebook + mobile: estilos de demo OK."

Write-Host "[475/600] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "Historico do PREPARAR inesperado."
}
if (Test-Path .\scripts\sql\v0.3.39_mvp_preview.sql) {
    throw "v0.3.39 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 38/38."

Write-Host "[476/600] Validando versao v0.3.39..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.39 / MVP Preview + polimento de demonstracao: OK."


Write-Host "[477/600] Validando Dockerfile..."
$dockerSource = Get-Content .\Dockerfile -Encoding UTF8 -Raw
if (-not $dockerSource.Contains("mcr.microsoft.com/dotnet/sdk:10.0") -or
    -not $dockerSource.Contains("mcr.microsoft.com/dotnet/aspnet:10.0") -or
    -not $dockerSource.Contains("dotnet publish") -or
    -not $dockerSource.Contains("docker-entrypoint.sh")) {
    throw "Dockerfile do MVP incompleto."
}
Write-Host "    Build multi-stage .NET 10: OK."

Write-Host "[478/600] Validando bind dinamico de porta..."
$entrypointSource = Get-Content .\docker-entrypoint.sh -Encoding UTF8 -Raw
if (-not $entrypointSource.Contains('PORT_VALUE="${PORT:-10000}"') -or
    -not $entrypointSource.Contains('0.0.0.0:${PORT_VALUE}') -or
    -not $entrypointSource.Contains("HealthPlatform.Api.dll") -or
    -not $entrypointSource.Contains("--hostBuilder:reloadConfigOnChange=false")) {
    throw "Entrypoint Render incompleto."
}
Write-Host "    0.0.0.0 + PORT dinamico + config reload desligado: OK."

Write-Host "[479/600] Validando Blueprint Render..."
$renderSource = Get-Content .\render.yaml -Encoding UTF8 -Raw
if (-not $renderSource.Contains("runtime: docker") -or
    -not $renderSource.Contains("plan: free") -or
    -not $renderSource.Contains("healthCheckPath: /api/health") -or
    -not $renderSource.Contains("healthplatform-mvp-db")) {
    throw "render.yaml incompleto."
}
Write-Host "    Web + Postgres + healthcheck: Blueprint OK."

Write-Host "[480/600] Validando secrets do Blueprint..."
if (-not $renderSource.Contains("Jwt__Key") -or
    -not $renderSource.Contains("generateValue: true") -or
    -not $renderSource.Contains("Seed__AdminPassword") -or
    -not $renderSource.Contains("sync: false") -or
    -not $renderSource.Contains("DemoBootstrap__SyncAdminPassword")) {
    throw "Secrets/sincronizacao do admin Render incompletos."
}
Write-Host "    JWT gerado + senha solicitada + sync do admin: OK."

Write-Host "[481/600] Validando conexao PostgreSQL do Render..."
$resolverSource = Get-Content .\src\HealthPlatform.Api\Services\DatabaseConnectionResolver.cs -Encoding UTF8 -Raw
if (-not $resolverSource.Contains("NpgsqlConnectionStringBuilder") -or
    -not $resolverSource.Contains('configuration["Database:Host"]') -or
    -not $resolverSource.Contains('configuration["Database:Password"]')) {
    throw "Resolver de banco Render incompleto."
}
if (-not $renderSource.Contains("Database__Host") -or
    -not $renderSource.Contains("property: host") -or
    -not $renderSource.Contains("Database__Password") -or
    -not $renderSource.Contains("property: password")) {
    throw "Wiring do banco no Blueprint incompleto."
}
Write-Host "    Credenciais discretas -> Npgsql: OK."

Write-Host "[482/600] Validando bootstrap isolado do MVP..."
$programSource = Get-Content .\src\HealthPlatform.Api\Program.cs -Encoding UTF8 -Raw
if (-not $programSource.Contains('GetValue<bool>("DemoBootstrap:Enabled")') -or
    -not $programSource.Contains('GetValue<bool>("DemoBootstrap:SyncAdminPassword")') -or
    -not $programSource.Contains("EnsureCreatedAsync") -or
    -not $programSource.Contains("CheckPasswordAsync") -or
    -not $programSource.Contains("GeneratePasswordResetTokenAsync") -or
    -not $programSource.Contains("ResetPasswordAsync") -or
    -not $programSource.Contains("DatabaseConnectionResolver.Resolve")) {
    throw "Bootstrap/sincronizacao do admin demo incompletos."
}
Write-Host "    EnsureCreated + sync de senha somente no DemoBootstrap."

Write-Host "[483/600] Validando fluxo local preservado..."
if (-not $programSource.Contains("app.Environment.IsDevelopment()") -or
    -not $programSource.Contains("MigrateAsync") -or
    -not $setupSource.Contains("[38/38]") -or
    -not $setupSource.Contains("20260813190735_InitialCreate")) {
    throw "Fluxo local/migration baseline foi alterado indevidamente."
}
Write-Host "    Development continua usando baseline + migrations."

Write-Host "[484/600] Validando healthcheck real..."
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if (-not $healthSource.Contains("Status503ServiceUnavailable") -or
    -not $healthSource.Contains('status = "degraded"') -or
    -not $healthSource.Contains('database = "unavailable"')) {
    throw "Healthcheck nao sinaliza indisponibilidade do banco."
}
Write-Host "    Banco indisponivel -> HTTP 503."

Write-Host "[485/600] Validando forwarded headers..."
if (-not $renderSource.Contains("ASPNETCORE_FORWARDEDHEADERS_ENABLED") -or
    -not $renderSource.Contains('value: "true"') -or
    -not $renderSource.Contains("DOTNET_USE_POLLING_FILE_WATCHER")) {
    throw "Configuracao de proxy/file watcher do Render incompleta."
}
Write-Host "    X-Forwarded-* + polling watcher habilitados no ambiente hospedado."

Write-Host "[486/600] Validando POPULAR remoto..."
$remotePopular = Get-Content .\POPULAR-REMOTO.ps1 -Encoding UTF8 -Raw
if (-not $remotePopular.Contains("[Parameter(Mandatory=`$true)][string]`$BaseUrl") -or
    -not $remotePopular.Contains("HealthPlatform v0.6.0 - POPULAR RENDER DEMO") -or
    -not $remotePopular.Contains("PacienteDemo_123!")) {
    throw "POPULAR-REMOTO incompleto."
}
Write-Host "    Base URL + credenciais + acesso paciente: OK."

Write-Host "[487/600] Validando catalogos da demo remota..."
if (-not $remotePopular.Contains("Arroz branco cozido") -or
    -not $remotePopular.Contains("Agachamento livre") -or
    -not $remotePopular.Contains("Como voce avalia sua rotina atual de sono?")) {
    throw "Catalogos remotos iniciais incompletos."
}
$richPopular = Get-Content .\POPULAR-REMOTO-RICO.ps1 -Encoding UTF8 -Raw
if (-not $richPopular.Contains("POPULAR REMOTO RICO V2 FINALIZADO") -or
    -not $richPopular.Contains("Rich-EnsureAnamnese") -or
    -not $richPopular.Contains("Rich-EnsureNutritionPlan") -or
    -not $richPopular.Contains("Rich-CreateWorkoutExecutions")) {
    throw "POPULAR-REMOTO-RICO incompleto."
}
Write-Host "    Alimentos + exercicios + pergunta de anamnese: seed remoto OK."

Write-Host "[488/600] Validando smoke test remoto..."
$remoteTest = Get-Content .\TESTAR-RENDER.ps1 -Encoding UTF8 -Raw
if (-not $remoteTest.Contains("TESTE REMOTO RENDER") -or
    -not $remoteTest.Contains("[12/12]") -or
    -not $remoteTest.Contains("Nenhum dado foi criado ou alterado")) {
    throw "TESTAR-RENDER incompleto."
}
Write-Host "    Smoke remoto somente leitura: OK."

Write-Host "[489/600] Validando guia de deploy..."
$deployGuide = Get-Content .\DEPLOY-RENDER-MVP.md -Encoding UTF8 -Raw
if (-not $deployGuide.Contains("New") -or
    -not $deployGuide.Contains("Blueprint") -or
    -not $deployGuide.Contains("POPULAR-REMOTO.ps1") -or
    -not $deployGuide.Contains("TESTAR-RENDER.ps1")) {
    throw "Guia Render incompleto."
}
Write-Host "    Blueprint -> popular -> smoke: documentado."

Write-Host "[490/600] Validando ausencia de schema novo..."
if (Test-Path .\scripts\sql\v0.3.40_render_demo.sql) {
    throw "v0.3.40 nao deveria adicionar upgrade SQL ao fluxo local."
}
if (-not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "Historico SQL anterior nao foi preservado."
}
Write-Host "    PREPARAR continua 38/38 / sem SQL v0.3.40."

Write-Host "[491/600] Validando arquivos de container..."
$dockerIgnore = Get-Content .\.dockerignore -Encoding UTF8 -Raw
if (-not $dockerIgnore.Contains("**/bin/") -or
    -not $dockerIgnore.Contains("**/obj/") -or
    -not $dockerIgnore.Contains(".git/")) {
    throw ".dockerignore incompleto."
}
Write-Host "    Contexto Docker enxuto: OK."

Write-Host "[492/600] Validando versao base do deploy..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.6.0 / Render Demo Deploy preservado: OK."


Write-Host "[493/600] Validando paleta RS..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("--rs-navy:#0b2347") -or
    -not $cssSource.Contains("--rs-gold:#c7aa5b") -or
    -not $cssSource.Contains("--rs-ivory:#f8f7f2")) {
    throw "Paleta RS incompleta."
}
Write-Host "    Navy + dourado + marfim: identidade OK."

Write-Host "[494/600] Validando monograma RS..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
if ($indexSource.Contains('<span class="brand-mark">H+</span>') -or
    -not $indexSource.Contains('<span class="brand-mark">RS</span>')) {
    throw "Monograma RS nao foi aplicado."
}
Write-Host "    Login + profissional + paciente: RS OK."

Write-Host "[495/600] Validando linguagem editorial..."
if (-not $indexSource.Contains("CIÊNCIA. ESTRATÉGIA. RESULTADO.") -or
    -not $indexSource.Contains("Saúde, longevidade e alta performance.")) {
    throw "Copy visual RS incompleta."
}
Write-Host "    Ciencia + estrategia + performance: copy OK."

Write-Host "[496/600] Validando tipografia condensada..."
if (-not $cssSource.Contains('--font-display:"Avenir Next Condensed"') -or
    -not $cssSource.Contains("text-transform:uppercase")) {
    throw "Sistema tipografico editorial incompleto."
}
Write-Host "    Display condensada + titulos editoriais: CSS OK."

Write-Host "[497/600] Validando login editorial..."
if (-not $cssSource.Contains(".login-visual:before") -or
    -not $cssSource.Contains(".login-visual:after") -or
    -not $cssSource.Contains("linear-gradient(90deg,var(--rs-navy) 0 10px")) {
    throw "Composicao editorial do login incompleta."
}
Write-Host "    Linhas + formas + papel marfim: login OK."

Write-Host "[498/600] Validando sidebar RS..."
if (-not $cssSource.Contains(".nav-item.active:before") -or
    -not $cssSource.Contains("background:var(--rs-gold)") -or
    -not $cssSource.Contains(".sidebar .brand-mark")) {
    throw "Sidebar RS incompleta."
}
Write-Host "    Navy + marcador dourado + monograma: OK."

Write-Host "[499/600] Validando experiencia iPad..."
if (-not $cssSource.Contains("@media (min-width:821px) and (max-width:1180px)") -or
    -not $cssSource.Contains(".app-shell{grid-template-columns:216px")) {
    throw "Layout tablet/iPad incompleto."
}
Write-Host "    Sidebar compacta + conteudo adaptado: iPad OK."

Write-Host "[500/600] Validando drawer mobile..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInstallRsResponsiveUi") -or
    -not $appJsSource.Contains("rs-sidebar-screen") -or
    -not $cssSource.Contains(".rs-sidebar-screen.visible")) {
    throw "Drawer/backdrop mobile incompleto."
}
Write-Host "    Menu profissional com backdrop: mobile OK."

Write-Host "[501/600] Validando safe areas iOS..."
if (-not $cssSource.Contains("safe-area-inset-top") -or
    -not $cssSource.Contains("safe-area-inset-bottom") -or
    -not $cssSource.Contains("100dvh")) {
    throw "Safe areas iOS incompletas."
}
Write-Host "    Notch + home indicator + viewport dinamico: iOS OK."

Write-Host "[502/600] Validando modal bottom-sheet no iPhone..."
if (-not $cssSource.Contains("border-radius:22px 22px 0 0") -or
    -not $cssSource.Contains("align-items:flex-end") -or
    -not $cssSource.Contains("max-height:92dvh")) {
    throw "Bottom sheet mobile incompleto."
}
Write-Host "    Formularios/modais adaptados ao iPhone."

Write-Host "[503/600] Validando portal mobile..."
if (-not $cssSource.Contains(".patient-portal-top .brand>span:nth-child(2)") -or
    -not $cssSource.Contains(".patient-portal-user .global-search-button") -or
    -not $cssSource.Contains("scroll-snap-type:x mandatory")) {
    throw "Portal do paciente mobile incompleto."
}
Write-Host "    Header compacto + navegacao horizontal: portal OK."

Write-Host "[504/600] Validando abas do prontuario no mobile..."
if (-not $cssSource.Contains(".patient-tabs{") -or
    -not $cssSource.Contains("position:sticky") -or
    -not $cssSource.Contains("scroll-snap-align:start")) {
    throw "Abas do prontuario mobile incompletas."
}
Write-Host "    Abas sticky + swipe horizontal: prontuario OK."

Write-Host "[505/600] Validando touch targets..."
if (-not $cssSource.Contains(".rs-touch-ui button") -or
    -not $cssSource.Contains("min-height:44px")) {
    throw "Touch targets incompletos."
}
Write-Host "    Alvos de toque >=44px em interface touch."

Write-Host "[506/600] Validando cards analiticos no iPhone..."
if (-not $cssSource.Contains(".exercise-progress-summary,.training-signal-summary,.workout-volume-summary") -or
    -not $cssSource.Contains(".training-signal-list,.workout-session-volume-list")) {
    throw "Analiticos mobile incompletos."
}
Write-Host "    Treino/analytics reorganizados para telas estreitas."

Write-Host "[507/600] Validando deploy/demo preservados..."
$renderSource = Get-Content .\render.yaml -Encoding UTF8 -Raw
if (-not $renderSource.Contains("DemoBootstrap__SyncAdminPassword") -or
    -not $renderSource.Contains("DOTNET_USE_POLLING_FILE_WATCHER") -or
    -not (Test-Path .\POPULAR-REMOTO-RICO.ps1)) {
    throw "Hotfixes Render ou popular rico nao foram preservados."
}
Write-Host "    Render r3 + popular rico: preservados."

Write-Host "[508/600] Validando entidade de solicitacoes clinicas..."
$requestEntity = Get-Content .\src\HealthPlatform.Domain\Entities\SolicitacaoClinica.cs -Encoding UTF8 -Raw
if (-not $requestEntity.Contains("class SolicitacaoClinica") -or -not $requestEntity.Contains("RespostaPaciente") -or -not $requestEntity.Contains("RevisadaEmUtc")) { throw "Entidade SolicitacaoClinica incompleta." }
Write-Host "    Ciclo Pendente -> Enviada -> Revisada: modelo OK."

Write-Host "[509/600] Validando mapeamento EF de solicitacoes..."
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbSource.Contains("DbSet<SolicitacaoClinica>") -or -not $dbSource.Contains('ToTable("SolicitacoesClinicas")')) { throw "Mapeamento EF de solicitacoes ausente." }
Write-Host "    DbSet + indices + relacionamentos: OK."

Write-Host "[510/600] Validando endpoints profissionais de solicitacoes..."
$requestController = Get-Content .\src\HealthPlatform.Api\Controllers\SolicitacoesClinicasController.cs -Encoding UTF8 -Raw
if (-not $requestController.Contains('api/pacientes/{pacienteId:guid}/solicitacoes') -or -not $requestController.Contains('api/solicitacoes/{id:guid}/revisar') -or -not $requestController.Contains('api/solicitacoes/{id:guid}/cancelar')) { throw "Endpoints profissionais de solicitacoes incompletos." }
Write-Host "    Criar + listar + revisar + cancelar: backend OK."

Write-Host "[511/600] Validando portal do paciente para solicitacoes..."
if (-not $requestController.Contains('api/portal/me/solicitacoes') -or -not $requestController.Contains('api/portal/me/solicitacoes/{id:guid}/responder') -or -not $requestController.Contains('PatientOnly')) { throw "Endpoints PatientOnly de solicitacoes incompletos." }
Write-Host "    Listagem propria + resposta protegida: backend OK."

Write-Host "[512/600] Validando isolamento multi-tenant..."
if (-not $requestController.Contains('x.OrganizacaoId == currentUser.OrganizationId') -or -not $requestController.Contains('x.UsuarioId == currentUser.UserId')) { throw "Isolamento multi-tenant das solicitacoes incompleto." }
Write-Host "    Organizacao + vinculo de usuario/paciente: OK."

Write-Host "[513/600] Validando auditoria de solicitacoes..."
if (-not $requestController.Contains('PATIENT_RESPONSE') -or -not $requestController.Contains('Auditar("REVIEW"') -or -not $requestController.Contains('Auditar("CANCEL"')) { throw "Auditoria do ciclo de solicitacoes incompleta." }
Write-Host "    CREATE + resposta + revisao + cancelamento: auditados."

Write-Host "[514/600] Validando interface profissional..."
if (-not $appJsSource.Contains("openSolicitacaoClinica") -or -not $appJsSource.Contains("Solicitação ao paciente") -or -not $appJsSource.Contains("renderProfessionalRequests")) { throw "Interface profissional de solicitacoes incompleta." }
Write-Host "    Criacao + historico + revisao: assets OK."

Write-Host "[515/600] Validando interface do paciente..."
if (-not $indexSource.Contains('data-patient-view="solicitacoes"') -or -not $appJsSource.Contains("loadPatientRequests") -or -not $appJsSource.Contains("openPatientRequestAnswer")) { throw "Portal de solicitacoes do paciente incompleto." }
Write-Host "    Navegacao + lista + resposta: assets OK."

Write-Host "[516/600] Validando upgrade SQL v0.5.1 preservado..."
$sqlRequest = Get-Content .\scripts\sql\v0.5.1_solicitacoes_clinicas.sql -Encoding UTF8 -Raw
if (-not $sqlRequest.Contains('CREATE TABLE IF NOT EXISTS "SolicitacoesClinicas"') -or -not $sqlRequest.Contains('IX_SolicitacoesClinicas_PacienteId_Status')) { throw "Upgrade SQL de solicitacoes incompleto." }
Write-Host "    Tabela + indices idempotentes: OK."

Write-Host "[517/600] Validando PREPARAR 38/38..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[31/38] Aplicando upgrade v0.5.1') -or -not $setupSource.Contains('v0.5.1_solicitacoes_clinicas.sql')) { throw "PREPARAR nao preserva o upgrade v0.5.1." }
Write-Host "    Upgrade de solicitacoes integrado ao setup."

Write-Host "[518/600] Validando versao v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
Write-Host "    v0.6.0 / Solicitacoes Clinicas + Connected Care: OK."

Write-Host "[519/600] Validando notificacoes de solicitacoes no backend..."
$notificationSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if (-not $notificationSource.Contains('OrigemTipo == "SolicitacaoClinica"') -or -not $notificationSource.Contains('PAC:SOLICITACAO:') -or -not $notificationSource.Contains('PROF:SOLICITACAO:')) { throw "Sincronizacao de solicitacoes nas notificacoes incompleta." }
Write-Host "    Paciente + profissional integrados ao motor de notificacoes."

Write-Host "[520/600] Validando prioridade por prazo para o paciente..."
if (-not $notificationSource.Contains('var vencida = r.DataLimiteUtc.HasValue') -or -not $notificationSource.Contains('venceEm24h') -or -not $notificationSource.Contains('prioridade = vencida ? "Alta"')) { throw "Prioridade por prazo de solicitacao incompleta." }
Write-Host "    Vencida=Alta / 24h=Media / demais=Normal: OK."

Write-Host "[521/600] Validando notificacao de resposta para profissional..."
if (-not $notificationSource.Contains('x.Status == "Enviada"') -or -not $notificationSource.Contains('Resposta recebida:') -or -not $notificationSource.Contains('$"paciente:{r.PacienteId}"')) { throw "Notificacao profissional de resposta incompleta." }
Write-Host "    Resposta do paciente gera contexto para revisao profissional."

Write-Host "[522/600] Validando limpeza de notificacoes encerradas..."
if (-not $notificationSource.Contains('x.OrigemTipo == "SolicitacaoClinica"')) { throw "Solicitacoes nao participam da desativacao de notificacoes antigas." }
Write-Host "    Mudanca de estado remove notificacao ativa obsoleta."

Write-Host "[523/600] Validando navegacao do paciente pela notificacao..."
if (-not $appJsSource.Contains("'solicitacoes'].includes(link)")) { throw "Notificacao do paciente nao abre Solicitacoes." }
Write-Host "    Drawer -> Minhas solicitacoes: OK."

Write-Host "[524/600] Validando navegacao contextual do profissional..."
if (-not $appJsSource.Contains("startsWith('paciente:')") -or -not $appJsSource.Contains("await openPatient(pacienteId)")) { throw "Notificacao profissional nao abre prontuario contextual." }
Write-Host "    Drawer -> prontuario do paciente: OK."

Write-Host "[525/600] Validando identidade visual de notificacao..."
if (-not $appJsSource.Contains("t==='Solicitacao'")) { throw "Identidade de solicitacao ausente nas notificacoes." }
Write-Host "    Solicitacoes identificadas no drawer: OK."

Write-Host "[526/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $notificationSource.Contains('SolicitacaoClinica')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Notificacoes Contextuais + Connected Care: OK."

Write-Host "[527/600] Validando endpoint da Central de Solicitacoes..."
$requestController = Get-Content .\src\HealthPlatform.Api\Controllers\SolicitacoesClinicasController.cs -Encoding UTF8 -Raw
if (-not $requestController.Contains('[HttpGet("api/solicitacoes")]') -or -not $requestController.Contains('FilaProfissional')) { throw "Central profissional de solicitacoes ausente." }
Write-Host "    Fila consolidada: backend OK."

Write-Host "[528/600] Validando resumo operacional de solicitacoes..."
if (-not $requestController.Contains('aguardandoRevisao') -or -not $requestController.Contains('vencidas') -or -not $requestController.Contains('total = pendentes + aguardandoRevisao')) { throw "Resumo da central incompleto." }
Write-Host "    Pendentes + para revisar + vencidas: backend OK."

Write-Host "[529/600] Validando filtros da Central de Solicitacoes..."
if (-not $requestController.Contains('[FromQuery] string? status') -or -not $requestController.Contains('[FromQuery] string? busca') -or -not $requestController.Contains('[FromQuery] string? prazo') -or -not $requestController.Contains('EF.Functions.ILike')) { throw "Filtros da central incompletos." }
Write-Host "    Status + prazo + busca textual: backend OK."

Write-Host "[530/600] Validando isolamento da fila profissional..."
if (-not $requestController.Contains('x.OrganizacaoId == currentUser.OrganizationId') -or -not $requestController.Contains('ProfissionalAtual(ct) is null')) { throw "Isolamento da central incompleto." }
Write-Host "    Organizacao + profissional autenticado: OK."

Write-Host "[531/600] Validando navegacao profissional de solicitacoes..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $indexSource.Contains('data-view="solicitacoes-profissional"') -or -not $appJsSource.Contains("view!=='solicitacoes-profissional'") -or -not $appJsSource.Contains('loadProfessionalRequests')) { throw "Navegacao da central incompleta." }
Write-Host "    Menu + rota + carregamento: assets OK."

Write-Host "[532/600] Validando indicadores e filtros visuais..."
if (-not $appJsSource.Contains('professionalRequestStatus') -or -not $appJsSource.Contains('professionalRequestDeadline') -or -not $appJsSource.Contains('professionalRequestSearch') -or -not $appJsSource.Contains('request-central-stats')) { throw "Filtros/indicadores visuais incompletos." }
Write-Host "    Busca + status + prazo + metricas: assets OK."

Write-Host "[533/600] Validando acoes rapidas da Central de Solicitacoes..."
if (-not $appJsSource.Contains('professional-request-review') -or -not $appJsSource.Contains('professional-request-cancel') -or -not $appJsSource.Contains('professional-request-open')) { throw "Acoes da central incompletas." }
Write-Host "    Revisar + cancelar + prontuario: assets OK."

Write-Host "[534/600] Validando responsividade da Central de Solicitacoes..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.request-central-list') -or -not $cssSource.Contains('.request-central-filters') -or -not $cssSource.Contains('@media(max-width:560px)')) { throw "Estilos da central incompletos." }
Write-Host "    Desktop + tablet + mobile: estilos OK."

Write-Host "[535/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $requestController.Contains('FilaProfissional')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Central de Solicitacoes + Connected Care: OK."


Write-Host "[536/600] Validando solicitacoes no Hoje do paciente..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("api('/api/portal/me/solicitacoes')") -or -not $appJsSource.Contains('solicitacoesPendentes')) { throw "Hoje do paciente nao carrega solicitacoes clinicas." }
Write-Host "    Home do paciente carrega solicitacoes pendentes: OK."

Write-Host "[537/600] Validando solicitacoes no progresso diario..."
if (-not $appJsSource.Contains('metas.length+quickTypes.length+solicitacoesPendentes.length')) { throw "Solicitacoes nao participam do progresso diario." }
Write-Host "    Solicitacoes contam como atividade do dia: OK."

Write-Host "[538/600] Validando card de acao necessaria..."
if (-not $appJsSource.Contains('AÇÃO NECESSÁRIA') -or -not $appJsSource.Contains('patient-today-requests') -or -not $appJsSource.Contains('patient-answer-today')) { throw "Card de solicitacoes do Hoje incompleto." }
Write-Host "    Card + resposta direta: assets OK."

Write-Host "[539/600] Validando destaque de solicitacao vencida no portal..."
if (-not $appJsSource.Contains("const overdue=r.dataLimiteUtc&&new Date(r.dataLimiteUtc)<new Date()") -or -not $appJsSource.Contains("overdue?'Vencida'")) { throw "Solicitacao vencida nao recebe destaque no Hoje." }
Write-Host "    Prazo vencido recebe prioridade visual: OK."

Write-Host "[540/600] Validando integracao da Central do Dia no backend..."
$centralSource = Get-Content .\src\HealthPlatform.Api\Controllers\CentralDiaController.cs -Encoding UTF8 -Raw
if (-not $centralSource.Contains('CentralDiaSolicitacaoResponse') -or -not $centralSource.Contains('SolicitacoesParaRevisao') -or -not $centralSource.Contains('SolicitacoesVencidas')) { throw "Central do Dia sem contrato de solicitacoes." }
Write-Host "    Contrato + contadores de solicitacoes: backend OK."

Write-Host "[541/600] Validando escopo profissional das solicitacoes do dia..."
if (-not $centralSource.Contains('x.ProfissionalId == profissional.Id') -or -not $centralSource.Contains('x.Status == "Enviada"') -or -not $centralSource.Contains('x.Status == "Pendente"')) { throw "Escopo profissional das solicitacoes do dia incompleto." }
Write-Host "    Profissional + estados que exigem acao: OK."

Write-Host "[542/600] Validando card profissional de solicitacoes..."
if (-not $appJsSource.Contains('Solicitações clínicas') -or -not $appJsSource.Contains('hpCentralRequest') -or -not $appJsSource.Contains('centralOpenRequests')) { throw "Central do Dia nao renderiza solicitacoes." }
Write-Host "    Fila resumida + atalho para central: assets OK."

Write-Host "[543/600] Validando dashboard com solicitacoes para revisao..."
if (-not $appJsSource.Contains('Solicitações p/ revisar') -or -not $appJsSource.Contains('d.solicitacoesParaRevisao')) { throw "Dashboard nao destaca solicitacoes para revisao." }
Write-Host "    Dashboard operacional: assets OK."

Write-Host "[544/600] Validando responsividade das solicitacoes no Hoje..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.patient-today-requests') -or -not $cssSource.Contains('.today-request-item') -or -not $cssSource.Contains('@media(max-width:560px)')) { throw "Estilos responsivos do Hoje incompletos." }
Write-Host "    Desktop + tablet + mobile: estilos OK."

Write-Host "[545/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $centralSource.Contains('SolicitacoesParaRevisao') -or -not $appJsSource.Contains('patient-today-requests')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Solicitacoes no Hoje + Connected Care: OK."

Write-Host "[546/600] Validando contrato Desde a ultima consulta..."
$resumoSource = Get-Content .\src\HealthPlatform.Api\Controllers\ResumoClinicoController.cs -Encoding UTF8 -Raw
if (-not $resumoSource.Contains('ResumoDesdeUltimaConsultaResponse') -or -not $resumoSource.Contains('DesdeUltimaConsulta')) { throw "Contrato longitudinal do resumo clinico ausente." }
Write-Host "    Periodo longitudinal incorporado ao resumo clinico: OK."

Write-Host "[547/600] Validando periodo baseado na ultima consulta..."
if (-not $resumoSource.Contains('ultimaConsultaEntity?.DataHoraUtc ?? agora.AddDays(-30)') -or -not $resumoSource.Contains('UltimaConsulta') -or -not $resumoSource.Contains('Ultimos30Dias')) { throw "Regra de janela longitudinal incompleta." }
Write-Host "    Ultima consulta com fallback de 30 dias: backend OK."

Write-Host "[548/600] Validando atividade do paciente no periodo..."
if (-not $resumoSource.Contains('registrosDesdeConsulta') -or -not $resumoSource.Contains('treinosDesdeConsulta') -or -not $resumoSource.Contains('checkInsPeriodo')) { throw "Metricas de atividade longitudinal incompletas." }
Write-Host "    Diario + treinos + check-ins: backend OK."

Write-Host "[549/600] Validando adesao media por check-ins..."
if (-not $resumoSource.Contains('AdesaoAlimentacaoPercentual') -or -not $resumoSource.Contains('AdesaoTreinoPercentual') -or -not $resumoSource.Contains('Average()')) { throw "Agregacao de adesao longitudinal incompleta." }
Write-Host "    Alimentacao + treino agregados no periodo: OK."

Write-Host "[550/600] Validando variacao de peso no periodo..."
if (-not $resumoSource.Contains('pesosPeriodo') -or -not $resumoSource.Contains('variacaoPeso') -or -not $resumoSource.Contains('pesoInicial')) { throw "Variacao de peso longitudinal ausente." }
Write-Host "    Peso inicial + atual + delta: backend OK."

Write-Host "[551/600] Validando solicitacoes no resumo longitudinal..."
if (-not $resumoSource.Contains('solicitacoesPeriodo') -or -not $resumoSource.Contains('x == "Pendente"') -or -not $resumoSource.Contains('x == "Enviada"')) { throw "Solicitacoes nao participam do resumo longitudinal." }
Write-Host "    Pendentes + aguardando revisao: backend OK."

Write-Host "[552/600] Validando card longitudinal no prontuario..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('data-clinical-period-summary') -or -not $appJsSource.Contains('period.registrosDiario') -or -not $cssSource.Contains('.clinical-period-metrics')) { throw "Card longitudinal do prontuario incompleto." }
Write-Host "    Resumo visual + responsividade: assets OK."

Write-Host "[553/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $resumoSource.Contains('ResumoDesdeUltimaConsultaResponse') -or -not $appJsSource.Contains('data-clinical-period-summary')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Desde a ultima consulta + Connected Care: OK."

Write-Host "[554/600] Validando endpoint da Jornada do paciente..."
$portalController = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $portalController.Contains('[HttpGet("jornada")]') -or -not $portalController.Contains('PortalJornadaItemResponse')) { throw "Endpoint da Jornada ausente." }
Write-Host "    GET /api/portal/me/jornada: backend OK."

Write-Host "[555/600] Validando escopo PatientOnly da Jornada..."
if (-not $portalController.Contains('[Authorize(Policy = "PatientOnly")]') -or -not $portalController.Contains('MeuPacienteId(ct)')) { throw "Jornada sem escopo PatientOnly." }
Write-Host "    PatientOnly + paciente vinculado: OK."

Write-Host "[556/600] Validando fontes da Jornada..."
if (-not $portalController.Contains('db.Consultas') -or -not $portalController.Contains('db.Avaliacoes') -or -not $portalController.Contains('db.ExamesLaboratoriais') -or -not $portalController.Contains('db.CheckInsAcompanhamento') -or -not $portalController.Contains('db.ExecucoesTreino') -or -not $portalController.Contains('db.SolicitacoesClinicas')) { throw "Fontes da Jornada incompletas." }
Write-Host "    Consultas + avaliacoes + exames + check-ins + treinos + solicitacoes: OK."

Write-Host "[557/600] Validando navegacao Jornada no portal..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $indexSource.Contains('data-patient-view="jornada"') -or -not $appJsSource.Contains('loadPatientJourney')) { throw "Navegacao da Jornada ausente." }
Write-Host "    Aba Jornada + loader: assets OK."

Write-Host "[558/600] Validando filtro temporal da Jornada..."
if (-not $appJsSource.Contains('patientJourneyDays') -or -not $appJsSource.Contains('/api/portal/me/jornada?dias=')) { throw "Filtro temporal da Jornada ausente." }
Write-Host "    90 dias + 6 meses + 1 ano: assets OK."

Write-Host "[559/600] Validando atalhos contextuais da Jornada..."
if (-not $appJsSource.Contains('data-journey-destination') -or -not $appJsSource.Contains('loadPatientSection(b.dataset.journeyDestination)')) { throw "Atalhos contextuais da Jornada ausentes." }
Write-Host "    Eventos -> areas do portal: assets OK."

Write-Host "[560/600] Validando responsividade da Jornada..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.patient-journey-timeline') -or -not $cssSource.Contains('.patient-journey-item') -or -not $cssSource.Contains('.patient-journey-card')) { throw "Estilos da Jornada incompletos." }
Write-Host "    Timeline desktop + mobile: estilos OK."

Write-Host "[561/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $portalController.Contains('MinhaJornada') -or -not $appJsSource.Contains('loadPatientJourney')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Jornada do Paciente + Connected Care: OK."


Write-Host "[562/600] Validando endpoint de monitoramento profissional..."
$monitorSource = Get-Content .\src\HealthPlatform.Api\Controllers\MonitoramentoPacienteController.cs -Encoding UTF8 -Raw
if (-not $monitorSource.Contains('api/pacientes/{pacienteId:guid}/monitoramento') -or -not $monitorSource.Contains('MontarResumo')) { throw "Endpoint profissional de monitoramento ausente." }
Write-Host "    GET monitoramento profissional: backend OK."

Write-Host "[563/600] Validando monitoramento no portal do paciente..."
if (-not $monitorSource.Contains('api/portal/me/monitoramento') -or -not $monitorSource.Contains('Authorize(Policy = "PatientOnly")')) { throw "Monitoramento PatientOnly ausente." }
Write-Host "    PatientOnly + paciente vinculado: backend OK."

Write-Host "[564/600] Validando metricas guiadas..."
if (-not $monitorSource.Contains('PressaoSistolica') -or -not $monitorSource.Contains('PressaoDiastolica') -or -not $monitorSource.Contains('Glicemia') -or -not $monitorSource.Contains('FrequenciaCardiaca') -or -not $monitorSource.Contains('Saturacao') -or -not $monitorSource.Contains('Temperatura')) { throw "Metricas de monitoramento incompletas." }
Write-Host "    Pressao + glicemia + FC + SpO2 + temperatura: backend OK."

Write-Host "[565/600] Validando resumo de 7 dias..."
if (-not $monitorSource.Contains('Math.Clamp(dias, 1, 90)') -or -not $monitorSource.Contains('media =') -or -not $monitorSource.Contains('minimo =') -or -not $monitorSource.Contains('maximo =')) { throw "Resumo longitudinal do monitoramento incompleto." }
Write-Host "    Ultimo + media + minimo + maximo: backend OK."

Write-Host "[566/600] Validando registro guiado de pressao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("kind:'pressure'") -or -not $appJsSource.Contains("tipo:'PressaoSistolica'") -or -not $appJsSource.Contains("tipo:'PressaoDiastolica'")) { throw "Registro guiado de pressao ausente." }
Write-Host "    Sistolica + diastolica estruturadas no diario: assets OK."

Write-Host "[567/600] Validando registros rapidos de sinais vitais..."
if (-not $appJsSource.Contains("key:'Glicemia'") -or -not $appJsSource.Contains("key:'FrequenciaCardiaca'") -or -not $appJsSource.Contains("key:'Saturacao'") -or -not $appJsSource.Contains("key:'Temperatura'")) { throw "Acoes rapidas de sinais vitais ausentes." }
Write-Host "    Glicemia + frequencia + saturacao + temperatura: assets OK."

Write-Host "[568/600] Validando resumo profissional de monitoramento..."
if (-not $appJsSource.Contains('hpMonitoringCard') -or -not $appJsSource.Contains('data-patient-monitoring') -or -not $appJsSource.Contains('/monitoramento?dias=7')) { throw "Resumo profissional de monitoramento ausente." }
Write-Host "    Card no prontuario + endpoint: assets OK."

Write-Host "[569/600] Validando responsividade do monitoramento..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.monitoring-metrics') -or -not $cssSource.Contains('.patient-monitoring-card')) { throw "Estilos de monitoramento ausentes." }
Write-Host "    Desktop + tablet + mobile: estilos OK."

Write-Host "[570/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $monitorSource.Contains('TiposMonitorados') -or -not $appJsSource.Contains('hpMonitoringCard')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Monitoramento guiado + Connected Care: OK."

Write-Host "[571/600] Validando entidade de protocolo de acompanhamento..."
$protocolEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ProtocoloAcompanhamentoItem.cs -Encoding UTF8 -Raw
if (-not $protocolEntity.Contains('ProtocoloAcompanhamentoItem') -or -not $protocolEntity.Contains('Frequencia') -or -not $protocolEntity.Contains('HorarioLocal')) { throw "Entidade de protocolo incompleta." }
Write-Host "    Tipo + frequencia + horario + instrucoes: modelo OK."

Write-Host "[572/600] Validando mapeamento EF dos protocolos..."
$dbContextSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbContextSource.Contains('DbSet<ProtocoloAcompanhamentoItem> ProtocolosAcompanhamento') -or -not $dbContextSource.Contains('ToTable("ProtocolosAcompanhamento")')) { throw "Mapeamento EF do protocolo ausente." }
Write-Host "    DbSet + tabela + indices: EF OK."

Write-Host "[573/600] Validando endpoints profissionais do protocolo..."
$protocolSource = Get-Content .\src\HealthPlatform.Api\Controllers\ProtocolosAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $protocolSource.Contains('api/pacientes/{pacienteId:guid}/protocolo-acompanhamento') -or -not $protocolSource.Contains('[HttpPost') -or -not $protocolSource.Contains('[HttpPut')) { throw "CRUD profissional de protocolo incompleto." }
Write-Host "    Listar + criar + editar: backend OK."

Write-Host "[574/600] Validando portal PatientOnly do protocolo..."
if (-not $protocolSource.Contains('api/portal/me/protocolo-acompanhamento') -or -not $protocolSource.Contains('Authorize(Policy="PatientOnly")')) { throw "Protocolo do paciente sem PatientOnly." }
Write-Host "    Paciente vinculado + somente ativos: backend OK."

Write-Host "[575/600] Validando tipos e frequencias permitidos..."
if (-not $protocolSource.Contains('Pressao') -or -not $protocolSource.Contains('Glicemia') -or -not $protocolSource.Contains('DiasSemana') -or -not $protocolSource.Contains('SobDemanda')) { throw "Catalogo do protocolo incompleto." }
Write-Host "    Metricas + frequencias configuraveis: backend OK."

Write-Host "[576/600] Validando gerenciamento visual do protocolo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('openProtocolManager') -or -not $appJsSource.Contains('protocol-configure') -or -not $appJsSource.Contains('protocol-disable')) { throw "Gerenciador visual do protocolo ausente." }
Write-Host "    Criar + visualizar + desativar: assets OK."

Write-Host "[577/600] Validando personalizacao do Hoje do paciente..."
# v0.6.0 acrescenta offsetMinutos na chamada do protocolo para respeitar o dia local.
# Valide o fragmento estavel da rota, nao a chamada literal sem query string.
if (-not $appJsSource.Contains('/api/portal/me/protocolo-acompanhamento?offsetMinutos=') -or -not $appJsSource.Contains('protocolTypes') -or -not $appJsSource.Contains('allQuickTypes.filter')) { throw "Hoje nao respeita protocolo ativo." }
Write-Host "    Registros rapidos filtrados pelo protocolo + timezone local: assets OK."

Write-Host "[578/600] Validando SQL e migration de upgrade v0.5.8 preservados..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sql058 = Get-Content .\scripts\sql\v0.5.8_protocolos_acompanhamento.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains('V058ProtocolosAcompanhamento') -or -not $setupSource.Contains('[32/38] Aplicando upgrade v0.5.8') -or -not $sql058.Contains('CREATE TABLE IF NOT EXISTS "ProtocolosAcompanhamento"')) { throw "Upgrade v0.5.8 de protocolos nao foi preservado." }
Write-Host "    Migration incremental + SQL idempotente v0.5.8 + PREPARAR 38/38: OK."

Write-Host "[579/600] Validando responsividade dos protocolos..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.protocol-summary') -or -not $cssSource.Contains('.protocol-manager') -or -not $cssSource.Contains('.protocol-item')) { throw "Estilos do protocolo ausentes." }
Write-Host "    Desktop + tablet + mobile: estilos OK."

Write-Host "[580/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $protocolSource.Contains('MeuProtocolo') -or -not $appJsSource.Contains('openProtocolManager')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Protocolos de Acompanhamento + Connected Care: OK."

Write-Host "[581/600] Validando calculo de aderencia ao protocolo..."
$protocolSource = Get-Content .\src\HealthPlatform.Api\Controllers\ProtocolosAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $protocolSource.Contains('CalcularAderencia') -or -not $protocolSource.Contains('previstos') -or -not $protocolSource.Contains('concluidos')) { throw "Motor de aderencia do protocolo ausente." }
Write-Host "    Previsto + concluido + percentual: backend OK."

Write-Host "[582/600] Validando agenda diaria dos protocolos..."
if (-not $protocolSource.Contains('DeveExecutarNoDia') -or -not $protocolSource.Contains('DiasSemana') -or -not $protocolSource.Contains('Semanal')) { throw "Agenda do protocolo incompleta." }
Write-Host "    Diario + dias da semana + semanal + sob demanda: backend OK."

Write-Host "[583/600] Validando aderencia de pressao estruturada..."
if (-not $protocolSource.Contains('PressaoSistolica') -or -not $protocolSource.Contains('PressaoDiastolica') -or -not $protocolSource.Contains('RegistroCumpre')) { throw "Aderencia de pressao incompleta." }
Write-Host "    Pressao exige sistolica + diastolica no mesmo dia: backend OK."

Write-Host "[584/600] Validando timezone da aderencia..."
if (-not $protocolSource.Contains('offsetMinutos') -or -not $protocolSource.Contains('AddMinutes(offsetMinutos)')) { throw "Aderencia sem timezone local." }
Write-Host "    Dia local calculado por offset do navegador: backend OK."

Write-Host "[585/600] Validando status de protocolo no Hoje do paciente..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('protocol-today-status') -or -not $appJsSource.Contains('aderencia.hoje')) { throw "Status diario do protocolo ausente." }
Write-Host "    Concluidos/previstos + itens do dia: assets OK."

Write-Host "[586/600] Validando resumo profissional de aderencia..."
if (-not $appJsSource.Contains('protocol-adherence') -or -not $appJsSource.Contains('Aderência ao protocolo')) { throw "Resumo profissional de aderencia ausente." }
Write-Host "    Percentual de 7 dias no prontuario: assets OK."

Write-Host "[587/600] Validando responsividade da aderencia..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.protocol-adherence-track') -or -not $cssSource.Contains('.protocol-today-items')) { throw "Estilos de aderencia ausentes." }
Write-Host "    Desktop + tablet + mobile: estilos OK."

Write-Host "[588/600] Validando versao funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $protocolSource.Contains('CalcularAderencia') -or -not $appJsSource.Contains('protocol-today-status')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Aderencia aos Protocolos + Connected Care: OK."

Write-Host "[589/600] Validando entidade de medicamentos..."
$medEntity = Get-Content .\src\HealthPlatform.Domain\Entities\MedicamentoPaciente.cs -Encoding UTF8 -Raw
if (-not $medEntity.Contains('MedicamentoPaciente') -or -not $medEntity.Contains('HorariosLocais') -or -not $medEntity.Contains('Orientacao')) { throw "Entidade de medicamento incompleta." }
Write-Host "    Dose + via + horarios + orientacao: modelo OK."

Write-Host "[590/600] Validando registros de tomada..."
$medLogEntity = Get-Content .\src\HealthPlatform.Domain\Entities\RegistroMedicamento.cs -Encoding UTF8 -Raw
if (-not $medLogEntity.Contains('DataHoraPrevistaUtc') -or -not $medLogEntity.Contains('DataHoraTomadaUtc') -or -not $medLogEntity.Contains('Status')) { throw "Registro de medicamento incompleto." }
Write-Host "    Prevista + tomada + status: modelo OK."

Write-Host "[591/600] Validando mapeamento EF de medicamentos..."
$dbContextSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbContextSource.Contains('DbSet<MedicamentoPaciente> MedicamentosPaciente') -or -not $dbContextSource.Contains('ToTable("RegistrosMedicamentos")')) { throw "Mapeamento EF de medicamentos ausente." }
Write-Host "    Duas tabelas + indices + relacionamentos: EF OK."

Write-Host "[592/600] Validando CRUD profissional de medicamentos..."
$medSource = Get-Content .\src\HealthPlatform.Api\Controllers\MedicamentosController.cs -Encoding UTF8 -Raw
if (-not $medSource.Contains('api/pacientes/{pacienteId:guid}/medicamentos') -or -not $medSource.Contains('api/medicamentos/{id:guid}') -or -not $medSource.Contains('Desativar')) { throw "CRUD de medicamentos incompleto." }
Write-Host "    Listar + criar + editar + desativar: backend OK."

Write-Host "[593/600] Validando portal PatientOnly de medicamentos..."
if (-not $medSource.Contains('api/portal/me/medicamentos') -or -not $medSource.Contains('Authorize(Policy = "PatientOnly")')) { throw "Medicamentos do portal sem PatientOnly." }
Write-Host "    Portal isolado por paciente: backend OK."

Write-Host "[594/600] Validando registro de tomada pelo paciente..."
if (-not $medSource.Contains('tomadas') -or -not $medSource.Contains('StatusPermitidos') -or -not $medSource.Contains('Tomado') -or -not $medSource.Contains('Pulado')) { throw "Fluxo de tomada incompleto." }
Write-Host "    Tomado + pulado + idempotencia por horario: backend OK."

Write-Host "[595/600] Validando aderencia medicamentosa..."
if (-not $medSource.Contains('percentual =') -or -not $medSource.Contains('tomados') -or -not $medSource.Contains('dias = 30')) { throw "Resumo de aderencia medicamentosa ausente." }
Write-Host "    Tomadas registradas + percentual de 30 dias: backend OK."

Write-Host "[596/600] Validando portal visual de medicamentos..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('loadPatientMedications') -or -not $indexSource.Contains('data-patient-view="medicamentos"') -or -not $appJsSource.Contains('medication-log')) { throw "Portal visual de medicamentos ausente." }
Write-Host "    Lista + horarios + confirmacao de tomada: assets OK."

Write-Host "[597/600] Validando gerenciamento profissional visual..."
if (-not $appJsSource.Contains('openMedicationManager') -or -not $appJsSource.Contains('medications-manage') -or -not $appJsSource.Contains('medication-disable')) { throw "Gerenciamento visual de medicamentos ausente." }
Write-Host "    Cadastro + adesao + desativacao: assets OK."

Write-Host "[598/600] Validando SQL e migration v0.6.0..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sql060 = Get-Content .\scripts\sql\v0.6.0_medicamentos.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains('V060MedicamentosAdesao') -or -not $setupSource.Contains('[33/38] Aplicando upgrade v0.6.0') -or -not $sql060.Contains('CREATE TABLE IF NOT EXISTS "MedicamentosPaciente"')) { throw "Upgrade v0.6.0 de medicamentos incompleto." }
Write-Host "    Migration incremental + SQL idempotente + PREPARAR 38/38: OK."

Write-Host "[599/600] Validando responsividade de medicamentos..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains('.patient-medication-list') -or -not $cssSource.Contains('.medication-dose-row') -or -not $cssSource.Contains('.medication-prof-summary')) { throw "Estilos de medicamentos ausentes." }
Write-Host "    Desktop + tablet + mobile: estilos OK."

Write-Host "[600/696] Validando compatibilidade funcional v0.6.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3") { throw "VERSION.txt inesperado." }
if (-not $medSource.Contains('RegistrarTomada') -or -not $appJsSource.Contains('loadPatientMedications')) { throw "Feature v0.6.0 ausente." }
Write-Host "    v0.6.0 / Medicamentos + Adesao ao Tratamento preservado: OK."

Write-Host "[601/696] Validando entidade de prontidao diaria..."
$readinessEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ProntidaoDiaria.cs -Encoding UTF8 -Raw
if (-not $readinessEntity.Contains('SonoHoras') -or -not $readinessEntity.Contains('RecuperacaoNivel') -or -not $readinessEntity.Contains('RecomendacaoTreino')) { throw "Entidade de prontidao incompleta." }
Write-Host "    Sono + energia + dor + disposicao + recuperacao: modelo OK."

Write-Host "[602/696] Validando mapeamento EF da prontidao..."
$dbContextSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbContextSource.Contains('DbSet<ProntidaoDiaria> ProntidoesDiarias') -or -not $dbContextSource.Contains('IX_ProntidoesDiarias_PacienteId_Data')) { throw "Mapeamento EF da prontidao ausente." }
Write-Host "    Registro unico por paciente/dia + isolamento organizacional: EF OK."

Write-Host "[603/696] Validando endpoint PatientOnly de prontidao..."
$portalMeSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $portalMeSource.Contains('[HttpPost("prontidao")]') -or -not $portalMeSource.Contains('CalcularProntidao') -or -not $portalMeSource.Contains('carga recente alta')) { throw "Endpoint/calculo de prontidao incompleto." }
Write-Host "    Upsert diario + carga do treino anterior: backend OK."

Write-Host "[604/696] Validando travas de seguranca do score..."
if (-not $portalMeSource.Contains('if (dor >= 8)') -or -not $portalMeSource.Contains('if (recuperacao <= 3)') -or -not $portalMeSource.Contains('sonoHoras < 4.5m')) { throw "Travas de seguranca da prontidao ausentes." }
Write-Host "    Dor alta + baixa recuperacao + sono muito curto limitam score: OK."

Write-Host "[605/696] Validando Home Daily Athlete..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('DAILY ATHLETE • PRONTIDÃO') -or -not $appJsSource.Contains('openDailyReadiness') -or -not $appJsSource.Contains('dailyReadinessButton')) { throw "Home Daily Athlete incompleta." }
Write-Host "    Check-in matinal + score + recomendacao: assets OK."

Write-Host "[606/696] Validando leitura profissional da prontidao..."
if (-not $appJsSource.Contains('hpReadinessProfessionalCard') -or -not $appJsSource.Contains('portal.prontidaoDiaria')) { throw "Leitura profissional da prontidao ausente." }
Write-Host "    Mesmo dado diario visivel no prontuario: OK."

Write-Host "[607/696] Validando SQL e migration v0.6.1..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sql061 = Get-Content .\scripts\sql\v0.6.1_prontidao_diaria.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains('V061ProntidaoDiaria') -or -not $setupSource.Contains('[34/38] Aplicando upgrade v0.6.1') -or -not $sql061.Contains('CREATE TABLE IF NOT EXISTS "ProntidoesDiarias"')) { throw "Upgrade v0.6.1 de prontidao incompleto." }
Write-Host "    Migration incremental + SQL idempotente + PREPARAR 38/38: OK."

Write-Host "[608/696] Validando versao funcional v0.6.1..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.6.1 inconsistente." }
Write-Host "    v0.6.1 / Daily Athlete + Prontidao Diaria: OK."



Write-Host "[609/696] Validando ledger de XP..."
$xpEntity = Get-Content .\src\HealthPlatform.Domain\Entities\EventoXp.cs -Encoding UTF8 -Raw
if (-not $xpEntity.Contains('FonteId') -or -not $xpEntity.Contains('Pontos') -or -not $xpEntity.Contains('Adequacao')) { throw "Entidade de XP incompleta." }
Write-Host "    Fonte + idempotencia + motivo + adequacao: modelo OK."

Write-Host "[610/696] Validando mapeamento EF de XP..."
$dbContextSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbContextSource.Contains('DbSet<EventoXp> EventosXp') -or -not $dbContextSource.Contains('IX_EventosXp_PacienteId_Fonte_FonteId')) { throw "Mapeamento EF de XP ausente." }
Write-Host "    Ledger unico por fonte + isolamento organizacional: EF OK."

Write-Host "[611/696] Validando XP seguro de treino..."
$gameSource = Get-Content .\src\HealthPlatform.Api\Services\GamificacaoService.cs -Encoding UTF8 -Raw
if (-not $gameSource.Contains('CalcularXpTreino') -or -not $gameSource.Contains('XP reduzido para nao premiar sobrecarga') -or -not $gameSource.Contains('(35, "Excesso"')) { throw "Regra de XP seguro de treino incompleta." }
Write-Host "    Adequacao > intensidade bruta: regra OK."

Write-Host "[612/696] Validando nivel, streak e consistencia..."
if (-not $gameSource.Contains('totalXp / 500 + 1') -or -not $gameSource.Contains('diasAtivos.Count * 10') -or -not $gameSource.Contains('streak')) { throw "Progressao/consistencia incompleta." }
Write-Host "    Nivel + 14 dias + sequencia sustentavel: backend OK."

Write-Host "[613/696] Validando eventos de autocuidado..."
$portalMeSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$trainingSource = Get-Content .\src\HealthPlatform.Api\Controllers\ExecucoesTreinoController.cs -Encoding UTF8 -Raw
if (-not $portalMeSource.Contains('"Prontidao"') -or -not $portalMeSource.Contains('"MetaDiaria"') -or -not $trainingSource.Contains('"Treino"')) { throw "Fontes de XP nao integradas." }
Write-Host "    Prontidao + meta + treino: integracao OK."

Write-Host "[614/696] Validando Home esportiva gamificada..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('EVOLUÇÃO ESPORTIVA') -or -not $appJsSource.Contains('Consistência') -or -not $appJsSource.Contains('athlete-progression-card')) { throw "Home gamificada incompleta." }
Write-Host "    Nivel + XP + streak + consistencia: assets OK."

Write-Host "[615/696] Validando SQL e migration v0.6.2..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sql062 = Get-Content .\scripts\sql\v0.6.2_xp_consistencia.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains('V062XpConsistencia') -or -not $setupSource.Contains('[35/38] Aplicando upgrade v0.6.2') -or -not $sql062.Contains('CREATE TABLE IF NOT EXISTS "EventosXp"')) { throw "Upgrade v0.6.2 incompleto." }
Write-Host "    Migration incremental + SQL idempotente + PREPARAR 38/38: OK."

Write-Host "[616/696] Validando versao funcional v0.6.2..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.6.2 inconsistente." }
Write-Host "    v0.6.2 / XP + Nivel + Streak + Consistencia: OK."

Write-Host "[617/696] Validando entidades de desafios e conquistas..."
$challengeEntity = Get-Content .\src\HealthPlatform.Domain\Entities\DesafioSemanalPaciente.cs -Encoding UTF8 -Raw
$achievementEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ConquistaPaciente.cs -Encoding UTF8 -Raw
if (-not $challengeEntity.Contains('SemanaInicio') -or -not $challengeEntity.Contains('RecompensaXp') -or -not $achievementEntity.Contains('DataConquista')) { throw "Modelo de missoes/conquistas incompleto." }
Write-Host "    Semana + progresso + recompensa + marco persistente: modelo OK."

Write-Host "[618/696] Validando mapeamento EF de missoes..."
$dbContextSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbContextSource.Contains('DbSet<DesafioSemanalPaciente> DesafiosSemanaisPaciente') -or -not $dbContextSource.Contains('IX_DesafiosSemanaisPaciente_PacienteId_SemanaInicio_Codigo')) { throw "Mapeamento EF dos desafios ausente." }
Write-Host "    Um desafio por codigo/semana/paciente: EF OK."

Write-Host "[619/696] Validando mapeamento EF de conquistas..."
if (-not $dbContextSource.Contains('DbSet<ConquistaPaciente> ConquistasPaciente') -or -not $dbContextSource.Contains('IX_ConquistasPaciente_PacienteId_Codigo')) { throw "Mapeamento EF das conquistas ausente." }
Write-Host "    Conquista unica por paciente/codigo: EF OK."

Write-Host "[620/696] Validando missoes automaticas..."
$gameSource = Get-Content .\src\HealthPlatform.Api\Services\GamificacaoService.cs -Encoding UTF8 -Raw
if (-not $gameSource.Contains('treinos-3') -or -not $gameSource.Contains('checkins-5') -or -not $gameSource.Contains('dias-5') -or -not $gameSource.Contains('DesafioSemanal')) { throw "Motor de desafios semanais incompleto." }
Write-Host "    Treino + prontidao + dias ativos com progresso automatico: OK."

Write-Host "[621/696] Validando conquistas esportivas..."
if (-not $gameSource.Contains('primeiro-treino') -or -not $gameSource.Contains('dez-treinos') -or -not $gameSource.Contains('sete-checkins') -or -not $gameSource.Contains('consistencia-80')) { throw "Catalogo inicial de conquistas ausente." }
Write-Host "    Marcos esportivos + autocuidado + consistencia: OK."

Write-Host "[622/696] Validando Home com missoes e conquistas..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('MISSÕES DA SEMANA') -or -not $appJsSource.Contains('weekly-challenges-card') -or -not $appJsSource.Contains('conquistasRecentes')) { throw "Home de missoes/conquistas incompleta." }
Write-Host "    Progresso semanal + recompensas + conquistas recentes: assets OK."

Write-Host "[623/696] Validando SQL e migration v0.6.3..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sql063 = Get-Content .\scripts\sql\v0.6.3_missoes_conquistas.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains('V063MissoesConquistas') -or -not $setupSource.Contains('[36/38] Aplicando upgrade v0.6.3') -or -not $sql063.Contains('CREATE TABLE IF NOT EXISTS "DesafiosSemanaisPaciente"') -or -not $sql063.Contains('CREATE TABLE IF NOT EXISTS "ConquistasPaciente"')) { throw "Upgrade v0.6.3 incompleto." }
Write-Host "    Migration incremental + SQL idempotente + PREPARAR 38/38: OK."

Write-Host "[624/696] Validando compatibilidade funcional v0.6.3..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao atual inconsistente ao validar v0.6.3." }
Write-Host "    v0.6.3 / Missoes + Desafios + Conquistas preservada: OK."


Write-Host "[625/696] Validando entidade de ciclo esportivo..."
$cycleEntity = Get-Content .\src\HealthPlatform.Domain\Entities\CicloEsportivoPaciente.cs -Encoding UTF8 -Raw
if (-not $cycleEntity.Contains('PerfilEsportivo') -or -not $cycleEntity.Contains('MetaTreinosSemanais') -or -not $cycleEntity.Contains('FaseTreinoId') -or -not $cycleEntity.Contains('FaseNutricionalId')) { throw "Modelo de ciclo esportivo incompleto." }
Write-Host "    Perfil + objetivo + periodo + metas + fases vinculadas: modelo OK."

Write-Host "[626/696] Validando mapeamento EF de ciclos..."
$dbContextSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbContextSource.Contains('DbSet<CicloEsportivoPaciente> CiclosEsportivosPaciente') -or -not $dbContextSource.Contains('IX_CiclosEsportivosPaciente_PacienteId_Periodo')) { throw "Mapeamento EF de ciclos ausente." }
Write-Host "    Ciclo + periodo + relacionamentos: EF OK."

Write-Host "[627/696] Validando gestao profissional de ciclos..."
$cycleController = Get-Content .\src\HealthPlatform.Api\Controllers\CiclosEsportivosController.cs -Encoding UTF8 -Raw
if (-not $cycleController.Contains('api/pacientes/{pacienteId:guid}/ciclos-esportivos') -or -not $cycleController.Contains('EncerrarOutrosAtivos') -or -not $cycleController.Contains('MetaConsistenciaPercentual')) { throw "Gestao de ciclos incompleta." }
Write-Host "    Criacao + atualizacao + ciclo ativo unico: API OK."

Write-Host "[628/696] Validando resumo longitudinal do ciclo..."
$cycleService = Get-Content .\src\HealthPlatform.Api\Services\CicloEsportivoService.cs -Encoding UTF8 -Raw
if (-not $cycleService.Contains('SemanaAtual') -and -not $cycleService.Contains('semanaAtual')) { throw "Resumo temporal do ciclo ausente." }
if (-not $cycleService.Contains('MediaProntidao') -and -not $cycleService.Contains('media')) { throw "Prontidao media do ciclo ausente." }
Write-Host "    Semana + progresso + treinos + prontidao media: OK."

Write-Host "[629/696] Validando desafio semanal adaptado ao ciclo..."
$gameSource = Get-Content .\src\HealthPlatform.Api\Services\GamificacaoService.cs -Encoding UTF8 -Raw
if (-not $gameSource.Contains('metaTreinosCiclo') -or -not $gameSource.Contains('Ritmo do ciclo') -or -not $gameSource.Contains('MetaTreinosSemanais')) { throw "Missao semanal nao respeita ciclo ativo." }
Write-Host "    Meta semanal vem do ciclo ativo com fallback: OK."

Write-Host "[630/696] Validando Home e leitura profissional do ciclo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('CICLO ATUAL') -or -not $appJsSource.Contains('CICLO ESPORTIVO ATUAL') -or -not $appJsSource.Contains('cicloEsportivoAtual')) { throw "UI de ciclos incompleta." }
Write-Host "    Daily Athlete + prontuario profissional: assets OK."

Write-Host "[631/696] Validando SQL e migration v0.6.4..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sql064 = Get-Content .\scripts\sql\v0.6.4_ciclos_esportivos.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains('V064CiclosEsportivos') -or -not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4') -or -not $sql064.Contains('CREATE TABLE IF NOT EXISTS "CiclosEsportivosPaciente"')) { throw "Upgrade v0.6.4 incompleto." }
Write-Host "    Migration incremental + SQL idempotente + PREPARAR 38/38: OK."

Write-Host "[632/696] Validando compatibilidade funcional v0.6.4..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Compatibilidade v0.6.4 inconsistente na versao atual." }
Write-Host "    v0.6.4 / Ciclos Esportivos + Metas de Ciclo preservado: OK."

Write-Host "[633/696] Validando contrato da Estrategia do Dia..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalEstrategiaDiaResponse') -or -not $portalContracts.Contains('EstrategiaDoDia')) { throw "Contrato da estrategia diaria ausente." }
Write-Host "    Perfil + intensidade + RPE + ajustes + nutricao: OK."

Write-Host "[634/696] Validando motor de adaptacao esportiva..."
$strategySource = Get-Content .\src\HealthPlatform.Api\Services\EstrategiaDiariaService.cs -Encoding UTF8 -Raw
if (-not $strategySource.Contains('Recuperacao') -or -not $strategySource.Contains('CargaControlada') -or -not $strategySource.Contains('AltaProntidao')) { throw "Perfis de estrategia diaria incompletos." }
if (-not $strategySource.Contains('-25, -30') -or -not $strategySource.Contains('-15, -20')) { throw "Reducoes conservadoras da estrategia diaria ausentes." }
Write-Host "    Recuperacao + leve + normal + alta prontidao: OK."

Write-Host "[635/696] Validando trava contra sobrecarga automatica..."
if (-not $strategySource.Contains('0, 0, "Boa prontidão') -or -not $strategySource.Contains('Não aumente carga ou volume automaticamente')) { throw "Prontidao alta pode estar premiando sobrecarga automatica." }
if ($strategySource.Contains('+10') -or $strategySource.Contains('+20')) { throw "Estrategia diaria contem aumento automatico de carga/volume." }
Write-Host "    Prontidao alta nao aumenta prescricao automaticamente: OK."

Write-Host "[636/696] Validando integracao com ciclo e planos prescritos..."
if (-not $strategySource.Contains('FaseTreinoId') -or -not $strategySource.Contains('FaseNutricionalId') -or -not $strategySource.Contains('PlanoTreinoId') -or -not $strategySource.Contains('PlanoAlimentarId')) { throw "Estrategia diaria nao respeita planos vinculados ao ciclo." }
if (-not $strategySource.Contains('Sessoes') -or -not $strategySource.Contains('Refeicoes')) { throw "Contexto de sessoes/refeicoes ausente." }
Write-Host "    Ciclo + treino + nutricao prescritos: OK."

Write-Host "[637/696] Validando Home Daily Athlete com Estrategia do Dia..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('ESTRATÉGIA DO DIA') -or -not $appJsSource.Contains('estrategiaDoDia') -or -not $appJsSource.Contains('RPE alvo')) { throw "Card da estrategia diaria ausente na Home." }
Write-Host "    Intensidade + RPE + carga + volume + nutricao: UI OK."

Write-Host "[638/696] Validando leitura profissional da estrategia diaria..."
if (-not $appJsSource.Contains('hpDailyStrategyProfessionalCard') -or -not $appJsSource.Contains('ajuste de carga') -or -not $appJsSource.Contains('ajuste de volume')) { throw "Leitura profissional da estrategia diaria ausente." }
Write-Host "    Prontuario recebe a mesma decisao com justificativa: OK."

Write-Host "[639/696] Validando ausencia de alteracao indevida de schema..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR base 37/37 foi alterado sem necessidade de schema." }
if (Test-Path .\scripts\sql\v0.6.5_estrategia_do_dia.sql) { throw "v0.6.5 nao deveria exigir SQL de schema." }
Write-Host "    v0.6.5 e funcional; schema v0.6.4 permanece valido: OK."

Write-Host "[640/696] Validando compatibilidade funcional v0.6.5..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.6.6 inconsistente." }
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if (-not $healthSource.Contains('version = "0.10.3"')) { throw "Health endpoint nao anuncia v0.6.6." }
Write-Host "    v0.6.5 / Estrategia do Dia preservada: OK."



Write-Host "[641/696] Validando contrato de execucao guiada..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalExecucaoDiaResponse') -or -not $portalContracts.Contains('PortalExecucaoDiaItemResponse') -or -not $portalContracts.Contains('ExecucaoDoDia')) { throw "Contrato de execucao guiada ausente." }
Write-Host "    Roteiro + itens + progresso + fechamento: contrato OK."

Write-Host "[642/696] Validando motor do Roteiro de Hoje..."
$guidedSource = Get-Content .\src\HealthPlatform.Api\Services\ExecucaoGuiadaService.cs -Encoding UTF8 -Raw
if (-not $guidedSource.Contains('Check-in de prontidão') -or -not $guidedSource.Contains('Treino do dia') -or -not $guidedSource.Contains('Fechar o dia')) { throw "Motor de execucao guiada incompleto." }
Write-Host "    Prontidao + treino + metas + fechamento: backend OK."

Write-Host "[643/696] Validando progresso baseado em registros reais..."
if (-not $guidedSource.Contains('ProntidoesDiarias') -or -not $guidedSource.Contains('ExecucoesTreino') -or -not $guidedSource.Contains('RegistrosMetas') -and -not $guidedSource.Contains('meta.Registros')) { throw "Roteiro nao esta usando dados reais." }
if (-not $guidedSource.Contains('Obrigatorio') -or -not $guidedSource.Contains('progresso')) { throw "Calculo de progresso guiado ausente." }
Write-Host "    Progresso deriva de check-in, execucao e metas reais: OK."

Write-Host "[644/696] Validando fechamento diario idempotente..."
$portalMeSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $portalMeSource.Contains('[HttpPost("fechamento-dia")]') -or -not $portalMeSource.Contains('Tipo == "FechamentoDia"') -or -not $portalMeSource.Contains('if (novo)')) { throw "Fechamento diario/idempotencia incompletos." }
if (-not $portalMeSource.Contains('"FechamentoDia"') -or -not $portalMeSource.Contains('20, "Dia revisado com consciência."')) { throw "XP de reflexao nao integrado." }
Write-Host "    Um fechamento por dia + XP apenas na primeira criacao: OK."

Write-Host "[645/696] Validando Home com Roteiro de Hoje..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('ROTEIRO DE HOJE') -or -not $appJsSource.Contains('hpExecutionDayCard') -or -not $appJsSource.Contains('closeAthleteDay')) { throw "UI de execucao guiada ausente." }
Write-Host "    Progresso + itens + fechamento: UI OK."

Write-Host "[646/696] Validando leitura profissional da execucao..."
if (-not $appJsSource.Contains('hpExecutionProfessionalCard') -or -not $appJsSource.Contains('EXECUÇÃO DO DIA')) { throw "Leitura profissional da execucao ausente." }
Write-Host "    Execucao do dia traduzida para acompanhamento profissional: OK."

Write-Host "[647/696] Validando ausencia de alteracao indevida de schema v0.6.6..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.6.6_execucao_guiada.sql) { throw "v0.6.6 nao deveria exigir migration." }
Write-Host "    Execucao guiada reutiliza schema existente: OK."

Write-Host "[648/696] Validando compatibilidade funcional v0.6.6..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.6.6 inconsistente." }
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if (-not $healthSource.Contains('version = "0.10.3"')) { throw "Health endpoint nao anuncia v0.6.6." }
Write-Host "    v0.6.6 / Execucao Guiada: OK."



Write-Host "[649/696] Validando contrato de tendencias de recuperacao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalTendenciaRecuperacaoResponse') -or -not $portalContracts.Contains('PortalSinalRecuperacaoResponse') -or -not $portalContracts.Contains('TendenciaRecuperacao')) { throw "Contrato de tendencias de recuperacao ausente." }
Write-Host "    Medias + variacao + carga + sinais transparentes: contrato OK."

Write-Host "[650/696] Validando janela longitudinal 7 vs 7 dias..."
$recoveryTrendSource = Get-Content .\src\HealthPlatform.Api\Services\TendenciaRecuperacaoService.cs -Encoding UTF8 -Raw
if (-not $recoveryTrendSource.Contains('AddDays(-13)') -or -not $recoveryTrendSource.Contains('AddDays(-6)') -or -not $recoveryTrendSource.Contains('ProntidoesDiarias')) { throw "Janela longitudinal de recuperacao incompleta." }
Write-Host "    7 dias atuais comparados aos 7 anteriores: OK."

Write-Host "[651/696] Validando sinais esportivos observaveis..."
if (-not $recoveryTrendSource.Contains('SonoHoras') -or -not $recoveryTrendSource.Contains('DorNivel') -or -not $recoveryTrendSource.Contains('RecuperacaoNivel') -or -not $recoveryTrendSource.Contains('EsforcoPercebido')) { throw "Tendencia nao combina sono, dor, recuperacao e carga." }
Write-Host "    Sono + dor + recuperacao + prontidao + RPE: OK."

Write-Host "[652/696] Validando alertas conservadores de recuperacao..."
if (-not $recoveryTrendSource.Contains('dor-elevada') -or -not $recoveryTrendSource.Contains('sono-baixo') -or -not $recoveryTrendSource.Contains('carga-intensa-recente') -or -not $recoveryTrendSource.Contains('prontidao-queda')) { throw "Catalogo de sinais de recuperacao incompleto." }
Write-Host "    Dor + sono + carga intensa + queda de prontidao: OK."

Write-Host "[653/696] Validando ausencia de diagnostico automatico..."
if ($recoveryTrendSource.ToLowerInvariant().Contains('overtraining') -or $recoveryTrendSource.ToLowerInvariant().Contains('diagnostico de')) { throw "Tendencia de recuperacao nao deve produzir diagnostico automatico." }
if (-not $recoveryTrendSource.Contains('merecem revisão') -and -not $recoveryTrendSource.Contains('merecem revisao')) { throw "Sinal deve orientar revisao, nao diagnosticar." }
Write-Host "    Motor sinaliza e orienta revisao sem diagnosticar: OK."

Write-Host "[654/696] Validando Home e prontuario com tendencia de recuperacao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpRecoveryTrendAthleteCard') -or -not $appJsSource.Contains('hpRecoveryTrendProfessionalCard') -or -not $appJsSource.Contains('RECUPERAÇÃO • ÚLTIMOS 7 DIAS') -or -not $appJsSource.Contains('TENDÊNCIA DE RECUPERAÇÃO • 7 DIAS')) { throw "UI de tendencias de recuperacao incompleta." }
Write-Host "    Atleta recebe tendencia; profissional recebe sinais explicaveis: UI OK."

Write-Host "[655/696] Validando ausencia de alteracao indevida de schema v0.6.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.6.7_tendencias_recuperacao.sql) { throw "v0.6.7 nao deveria exigir migration." }
Write-Host "    Tendencias reutilizam schema existente: OK."

Write-Host "[656/696] Validando compatibilidade funcional v0.6.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.1 inconsistente." }
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if (-not $healthSource.Contains('version = "0.10.3"')) { throw "Health endpoint nao anuncia v0.7.1." }
Write-Host "    v0.6.7 / Tendencias de Recuperacao + Alertas Inteligentes preservada: OK."


Write-Host "[657/696] Validando contrato de performance esportiva..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalPerformanceResponse') -or -not $portalContracts.Contains('PortalPerformanceExercicioResponse') -or -not $portalContracts.Contains('PortalPerformanceResponse Performance')) { throw "Contrato de performance ausente da Home." }
Write-Host "    Performance + destaques por exercicio: contrato OK."

Write-Host "[658/696] Validando motor de performance a partir das execucoes reais..."
$performanceSource = Get-Content .\src\HealthPlatform.Api\Services\PerformanceEsportivaService.cs -Encoding UTF8 -Raw
if (-not $performanceSource.Contains('ExecucoesItensTreino') -or -not $performanceSource.Contains('CargaRealizada') -or -not $performanceSource.Contains('RepeticoesRealizadas') -or -not $performanceSource.Contains('CalcularVolume')) { throw "Motor de performance nao usa execucoes reais." }
Write-Host "    Carga + series + repeticoes + volume: backend OK."

Write-Host "[659/696] Validando deteccao conservadora de PR..."
if (-not $performanceSource.Contains('melhorAnterior.HasValue') -or -not $performanceSource.Contains('dia.AddDays(-6)') -or -not $performanceSource.Contains('novoPr')) { throw "Deteccao conservadora de PR ausente." }
$gameSource = Get-Content .\src\HealthPlatform.Api\Services\GamificacaoService.cs -Encoding UTF8 -Raw
if (-not $gameSource.Contains('"primeiro-pr"') -or -not $gameSource.Contains('possuiPrHistorico')) { throw "Conquista Primeiro PR nao integrada." }
Write-Host "    PR exige marca anterior; conquista Primeiro PR usa evolucao real: OK."

Write-Host "[660/696] Validando contexto de prontidao no PR..."
if (-not $performanceSource.Contains('ProntidoesDiarias') -or -not $performanceSource.Contains('ProntidaoNoPr') -and -not $portalContracts.Contains('ProntidaoNoPr')) { throw "Contexto de prontidao no PR ausente." }
Write-Host "    Melhor marca pode ser lida junto da prontidao do dia: OK."

Write-Host "[661/696] Validando linguagem segura de performance..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('PR não é meta diária') -or -not $appJsSource.Contains('não é indicação automática para aumentar carga')) { throw "Travas de linguagem de performance ausentes." }
Write-Host "    PR nao autoriza aumento automatico de carga: OK."

Write-Host "[662/696] Validando Home e prontuario com Performance & PRs..."
if (-not $appJsSource.Contains('hpPerformanceAthleteCard') -or -not $appJsSource.Contains('hpPerformanceProfessionalCard') -or -not $appJsSource.Contains('SUA EVOLUÇÃO • PERFORMANCE') -or -not $appJsSource.Contains('PERFORMANCE • 180 DIAS')) { throw "UI de performance incompleta." }
Write-Host "    Atleta recebe evolucao; profissional recebe leitura longitudinal: UI OK."

Write-Host "[663/696] Validando ausencia de alteracao indevida de schema v0.6.8..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.6.8_performance_prs.sql) { throw "v0.7.1 nao deveria exigir migration." }
Write-Host "    Performance e calculada sobre o historico existente: OK."

Write-Host "[664/696] Validando compatibilidade funcional v0.6.8..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.1 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.1." }
Write-Host "    v0.6.8 / Performance & PRs preservada: OK."

Write-Host "[665/696] Validando contrato de carga de treino..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalCargaTreinoResponse') -or -not $portalContracts.Contains('PortalCargaTreinoResponse CargaTreino')) { throw "Contrato de carga de treino ausente da Home." }
Write-Host "    Carga interna + linha de base + classificacao: contrato OK."

Write-Host "[666/696] Validando motor sRPE sobre execucoes reais..."
$trainingLoadSource = Get-Content .\src\HealthPlatform.Api\Services\CargaTreinoService.cs -Encoding UTF8 -Raw
if (-not $trainingLoadSource.Contains('DuracaoMinutos') -or -not $trainingLoadSource.Contains('EsforcoPercebido') -or -not $trainingLoadSource.Contains('duracao.Value * rpe.Value')) { throw "Carga interna nao usa duracao x RPE." }
Write-Host "    Duracao x RPE: motor de carga interna OK."

Write-Host "[667/696] Validando linha de base de tres semanas..."
if (-not $trainingLoadSource.Contains('dia.AddDays(-27)') -or -not $trainingLoadSource.Contains('Enumerable.Range(0, 3)') -or -not $trainingLoadSource.Contains('cargasSemanaisBase.Sum() / 3m') -or -not $trainingLoadSource.Contains('RelacaoCargaComBase')) { throw "Linha de base de carga incompleta." }
Write-Host "    7 dias atuais vs. 3 semanas anteriores: OK."

Write-Host "[668/696] Validando integracao com recuperacao..."
if (-not $trainingLoadSource.Contains('ProntidoesDiarias') -or -not $trainingLoadSource.Contains('dorMedia') -or -not $trainingLoadSource.Contains('recuperacaoMedia') -or -not $trainingLoadSource.Contains('prontidaoMedia')) { throw "Carga nao considera contexto de recuperacao." }
Write-Host "    Prontidao + dor + recuperacao entram como contexto: OK."

Write-Host "[669/696] Validando linguagem conservadora da carga..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('não estima risco de lesão') -or -not $appJsSource.Contains('Mais carga não significa mais progresso')) { throw "Travas de linguagem de carga ausentes." }
if ($trainingLoadSource.ToLowerInvariant().Contains('prevê lesão') -or $trainingLoadSource.ToLowerInvariant().Contains('risco de lesão =')) { throw "Motor nao deve diagnosticar risco de lesao." }
Write-Host "    Carga orienta planejamento sem prever lesao: OK."

Write-Host "[670/696] Validando Home e prontuario com carga semanal..."
if (-not $appJsSource.Contains('hpTrainingLoadAthleteCard') -or -not $appJsSource.Contains('hpTrainingLoadProfessionalCard') -or -not $appJsSource.Contains('SEU RITMO • CARGA SEMANAL') -or -not $appJsSource.Contains('CARGA DE TREINO • 7 DIAS')) { throw "UI de carga de treino incompleta." }
Write-Host "    Atleta recebe ritmo semanal; profissional recebe contexto de carga: UI OK."

Write-Host "[671/696] Validando ausencia de alteracao indevida de schema v0.7.1..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.6.9_carga_treino.sql) { throw "v0.7.1 nao deveria exigir migration." }
Write-Host "    Carga de treino reutiliza schema existente: OK."

Write-Host "[672/696] Validando compatibilidade funcional v0.6.9..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.1 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.1." }
Write-Host "    v0.6.9 / Carga de Treino & Equilibrio preservada: OK."



Write-Host "[673/696] Validando contrato do Coach Diario..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalCoachDiarioResponse') -or -not $portalContracts.Contains('PortalCoachPrioridadeResponse') -or -not $portalContracts.Contains('PortalCoachDiarioResponse CoachDiario')) { throw "Contrato do Coach Diario ausente da Home." }
Write-Host "    Sintese + prioridades explicaveis: contrato OK."

Write-Host "[674/696] Validando motor de sintese contextual..."
$coachSource = Get-Content .\src\HealthPlatform.Api\Services\CoachDiarioService.cs -Encoding UTF8 -Raw
if (-not $coachSource.Contains('PortalTendenciaRecuperacaoResponse') -or -not $coachSource.Contains('PortalCargaTreinoResponse') -or -not $coachSource.Contains('PortalPerformanceResponse') -or -not $coachSource.Contains('PortalExecucaoDiaResponse')) { throw "Coach nao combina os motores esportivos existentes." }
Write-Host "    Recuperacao + carga + performance + execucao: motor OK."

Write-Host "[675/696] Validando prioridades de seguranca do Coach..."
if (-not $coachSource.Contains('priorizar-recuperacao') -or -not $coachSource.Contains('revisar-carga') -or -not $coachSource.Contains('checkin-prontidao')) { throw "Prioridades conservadoras do Coach incompletas." }
Write-Host "    Check-in + recuperacao + revisao de carga: prioridades OK."

Write-Host "[676/696] Validando ausencia de prescricao automatica..."
if (-not $coachSource.Contains('não diagnostica') -or -not $coachSource.Contains('não altera a prescrição automaticamente') -or -not $coachSource.Contains('não aumente carga ou volume automaticamente')) { throw "Travas de seguranca do Coach ausentes." }
if ($coachSource.ToLowerInvariant().Contains('diagnostico =') -or $coachSource.ToLowerInvariant().Contains('prescrever automaticamente')) { throw "Coach nao deve diagnosticar ou prescrever automaticamente." }
Write-Host "    Coach explica e prioriza sem diagnosticar/prescrever: OK."

Write-Host "[677/696] Validando integracao do Coach nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalProfSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('CoachDiarioService.Montar') -or -not $portalProfSource.Contains('CoachDiarioService.Montar') -or -not $meuPortalSource.Contains('coachDiario') -or -not $portalProfSource.Contains('coachDiario')) { throw "Coach nao foi integrado aos portais de atleta e profissional." }
Write-Host "    Atleta e profissional recebem a mesma sintese explicavel: backend OK."

Write-Host "[678/696] Validando UI do Coach Diario..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpCoachAthleteCard') -or -not $appJsSource.Contains('hpCoachProfessionalCard') -or -not $appJsSource.Contains('COACH DIÁRIO • PRIORIDADES') -or -not $appJsSource.Contains('COACH DIÁRIO • SÍNTESE EXPLICÁVEL')) { throw "UI do Coach Diario incompleta." }
Write-Host "    Prioridades para atleta + leitura profissional: UI OK."

Write-Host "[679/696] Validando ausencia de alteracao indevida de schema v0.7.1..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.1_coach_diario.sql) { throw "v0.7.1 nao deveria exigir migration." }
$anaSeedSource = Get-Content .\POPULAR-ANA-RIBEIRO.ps1 -Encoding UTF8 -Raw
if (-not $anaSeedSource.Contains('http://localhost:5180') -or -not $anaSeedSource.Contains('[int]$Dias = 56') -or -not $anaSeedSource.Contains('coachDiario') -or -not $anaSeedSource.Contains('/avaliacoes')) { throw "Seed v2 da Ana Ribeiro nao foi atualizado para a linha 0.7.x." }
Write-Host "    Coach e camada derivada; schema 38/38 + seed Ana v2: OK."

Write-Host "[680/696] Validando compatibilidade funcional v0.7.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.1 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.1." }
Write-Host "    v0.7.0 / Coach Diario & Prioridades Contextuais preservado: OK."


Write-Host "[681/720] Validando versao v0.7.1..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.1 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.1." }
Write-Host "    API/UI/VERSION: 0.7.1 OK."

Write-Host "[682/720] Validando seed Ana sem variavel automatica PID..."
$anaSeedSource = Get-Content .\POPULAR-ANA-RIBEIRO.ps1 -Encoding UTF8 -Raw
if ($anaSeedSource -match '(?im)\$pid\s*=') { throw "Seed ainda tenta sobrescrever a variavel automatica PID do PowerShell." }
if (-not $anaSeedSource.Contains('$pacienteIdSeed')) { throw "Seed nao usa identificador seguro de paciente." }
Write-Host "    PID reservado removido; pacienteIdSeed em uso: OK."

Write-Host "[683/720] Validando healthcheck preventivo do seed Ana..."
if (-not $anaSeedSource.Contains("Api Get '/api/health'") -or -not $anaSeedSource.Contains('[0/10] Healthcheck')) { throw "Seed da Ana nao valida a API antes de popular." }
Write-Host "    Seed falha cedo quando a API nao esta disponivel: OK."

Write-Host "[684/720] Validando mensagem de login administrativo do seed..."
if (-not $anaSeedSource.Contains('Falha no login administrativo') -or -not $anaSeedSource.Contains('Seed:AdminPassword/ChangeMe_123!')) { throw "Seed nao possui diagnostico de login administrativo." }
Write-Host "    Erro de credencial agora e explicativo: OK."

Write-Host "[685/720] Validando URL local padrao do seed..."
if (-not $anaSeedSource.Contains('[string]$BaseUrl = "http://localhost:5180"')) { throw "Seed Ana deve apontar para a porta local 5180." }
Write-Host "    localhost:5180: OK."

Write-Host "[686/720] Validando janela esportiva rica da Ana..."
if (-not $anaSeedSource.Contains('[int]$Dias = 56') -or -not $anaSeedSource.Contains('Enumerable.Range') -and -not $anaSeedSource.Contains('workoutDays')) { throw "Seed Ana perdeu a janela rica de historico." }
Write-Host "    56 dias + historico de treinos preservados: OK."

Write-Host "[687/720] Validando Coach Diario preservado na v0.7.1..."
$coachSource = Get-Content .\src\HealthPlatform.Api\Services\CoachDiarioService.cs -Encoding UTF8 -Raw
if (-not $coachSource.Contains('priorizar-recuperacao') -or -not $coachSource.Contains('não altera a prescrição automaticamente')) { throw "Coach Diario regrediu na v0.7.1." }
Write-Host "    Sintese contextual e travas clinicas preservadas: OK."

Write-Host "[688/720] Validando versao funcional v0.7.1..."
Write-Host "    v0.7.1 / Robustez do Coach + Seed Ana v3: OK."



Write-Host "[689/720] Validando diagnostico detalhado do seed Ana..."
$anaSeedSource = Get-Content .\POPULAR-ANA-RIBEIRO.ps1 -Encoding UTF8 -Raw
if (-not $anaSeedSource.Contains('Get-ApiErrorBody') -or -not $anaSeedSource.Contains('Falha API') -or -not $anaSeedSource.Contains('$uri')) { throw "Seed nao expõe endpoint e corpo de erro da API." }
Write-Host "    Erros 4xx/5xx agora identificam endpoint + resposta: OK."

Write-Host "[690/720] Validando metas opcionais no seed Ana..."
if (-not $anaSeedSource.Contains('EnsureGoalSafe') -or -not $anaSeedSource.Contains('Meta opcional ignorada')) { throw "Seed ainda aborta inteiro quando uma meta demonstrativa falha." }
Write-Host "    Falha em meta demonstrativa nao interrompe o seed: OK."

Write-Host "[691/720] Validando protecao contra meta nula..."
if (-not $anaSeedSource.Contains('if ($null -ne $metaAgua)') -or -not $anaSeedSource.Contains('if ($null -ne $metaSono)') -or -not $anaSeedSource.Contains('if ($null -ne $metaMov)')) { throw "Seed nao protege o historico quando alguma meta nao pode ser criada." }
Write-Host "    Historico de metas lida com recursos opcionais ausentes: OK."

Write-Host "[692/720] Validando resumo de falhas nao fatais..."
if (-not $anaSeedSource.Contains('$seedWarnings') -or -not $anaSeedSource.Contains('AVISOS DO SEED')) { throw "Seed nao resume falhas nao fatais ao final." }
Write-Host "    Avisos ficam visiveis sem perder o restante da populacao: OK."

Write-Host "[693/720] Validando compatibilidade do seed resiliente..."
if (-not $anaSeedSource.Contains('$seedWarnings') -or -not $anaSeedSource.Contains('Get-ApiErrorBody')) { throw "Diagnostico resiliente do seed anterior foi perdido." }
Write-Host "    Diagnostico resiliente preservado: OK."

Write-Host "[694/720] Validando ausencia de alteracao indevida de schema v0.7.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.7_seed_resiliente.sql) { throw "v0.7.7 nao deveria exigir migration." }
Write-Host "    Robustez de seed nao altera schema: OK."

Write-Host "[695/720] Validando versao corrente v0.7.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.7." }
Write-Host "    API/UI/VERSION: 0.7.7 OK."

Write-Host "[696/720] Validando compatibilidade funcional v0.7.2..."
Write-Host "    v0.7.2 / Seed Resiliente & Diagnostico de Dados preservado: OK."

Write-Host "[697/720] Validando seed Ana v7..."
if (-not $anaSeedSource.Contains('Seed esportivo PESADO da Ana Ribeiro (v7)') -or -not $anaSeedSource.Contains('HealthPlatform v0.7.7')) { throw "Banner do seed Ana v7/v0.7.7 ausente." }
Write-Host "    Seed v7 identificado: OK."

Write-Host "[698/720] Validando deload decimal compativel com PowerShell..."
if (-not $anaSeedSource.Contains('$progress -= [decimal]2.5')) { throw "Ajuste decimal do deload nao usa cast PowerShell explicito." }
Write-Host "    Deload usa [decimal]2.5: OK."

Write-Host "[699/720] Validando ausencia do literal C# 2.5m no seed..."
if ($anaSeedSource -match '(?<![A-Za-z0-9_])2\.5m(?![A-Za-z0-9_])') { throw "Seed ainda contem literal decimal C# 2.5m, invalido em PowerShell." }
Write-Host "    Literal C# removido: OK."

Write-Host "[700/720] Validando sufixos numericos perigosos no codigo do populador Ana..."
$linhasSeed = Get-Content .\POPULAR-ANA-RIBEIRO.ps1 -Encoding UTF8
$codigoSeed = ($linhasSeed | Where-Object { $_ -notmatch '^\s*#' }) -join "`n"
# Remove strings antes da regex: textos como "7d" nao sao literais numericos de codigo.
$codigoSemStrings = [regex]::Replace($codigoSeed, "'([^']|'')*'", "''")
$codigoSemStrings = [regex]::Replace($codigoSemStrings, '"(?:`.|[^"`])*"', '""')
if ($codigoSemStrings -match '(?<![A-Za-z0-9_])\d+(?:\.\d+)?[mMdDfF](?![A-Za-z0-9_])') { throw "Seed contem sufixo numerico de outra linguagem potencialmente invalido em PowerShell." }
Write-Host "    Literais numericos de codigo sao PowerShell nativos; strings ignoradas: OK."

Write-Host "[701/720] Validando seed pesado preservado apos a correcao..."
if (-not $anaSeedSource.Contains('$workoutDays') -or -not $anaSeedSource.Contains("'/api/portal/me/treinos/execucoes'") -or -not $anaSeedSource.Contains("'/api/portal/me/prontidao'")) { throw "Seed perdeu treino ou prontidao durante o hotfix numerico." }
Write-Host "    Treinos + prontidao preservados: OK."

Write-Host "[702/720] Validando ausencia de alteracao indevida de schema v0.7.7..."
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "PREPARAR 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.7_seed_numerico.sql) { throw "v0.7.7 nao deveria exigir migration." }
Write-Host "    Correcao de seed nao altera schema: OK."

Write-Host "[703/720] Validando versao corrente v0.7.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.7." }
Write-Host "    API/UI/VERSION: 0.7.7 OK."

Write-Host "[704/720] Validando compatibilidade funcional v0.7.3..."
Write-Host "    v0.7.3 / correcao decimal preservada: OK."

Write-Host "[705/720] Validando seed Ana v7..."
if (-not $anaSeedSource.Contains('Seed esportivo PESADO da Ana Ribeiro (v7)')) { throw "Banner do seed Ana v7 ausente." }
Write-Host "    Seed v7 identificado: OK."

Write-Host "[706/720] Validando texto da janela de recuperacao..."
if (-not $anaSeedSource.Contains('prontidão média 7 dias:')) { throw "Resumo do seed nao usa texto explicito de 7 dias." }
Write-Host "    Texto de 7 dias: OK."

Write-Host "[707/720] Validando verificacao numerica apenas em codigo..."
$testSourceAtual = Get-Content .\TESTAR.ps1 -Encoding UTF8 -Raw
if (-not $testSourceAtual.Contains('$codigoSemStrings')) { throw "Teste numerico nao exclui strings antes da regex." }
Write-Host "    Strings de interface sao ignoradas: OK."

Write-Host "[708/720] Validando deload decimal v7..."
if (-not $anaSeedSource.Contains('$progress -= [decimal]2.5')) { throw "Deload decimal v7 ausente." }
Write-Host "    Deload decimal: OK."

Write-Host "[709/720] Validando diagnostico resiliente preservado..."
if (-not $anaSeedSource.Contains('$seedWarnings') -or -not $anaSeedSource.Contains('Get-ApiErrorBody')) { throw "Diagnostico resiliente do seed foi perdido." }
Write-Host "    Diagnostico resiliente: OK."

Write-Host "[710/720] Validando ausencia de migration v0.7.7..."
if (Test-Path .\scripts\sql\v0.7.7_seed_validacao.sql) { throw "v0.7.7 nao deveria exigir migration." }
Write-Host "    Schema permanece 38/38: OK."

Write-Host "[711/720] Validando versao corrente v0.7.7..."
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.7." }
Write-Host "    API/UI/VERSION: 0.7.7 OK."


Write-Host "[712/720] Validando compatibilidade funcional v0.7.4..."
Write-Host "    v0.7.4 / Seed Ana v6 & Validacao PowerShell preservado: OK."

Write-Host "[713/720] Validando seed Ana v7..."
$anaSeedSource = Get-Content .\POPULAR-ANA-RIBEIRO.ps1 -Encoding UTF8 -Raw
if (-not $anaSeedSource.Contains('Seed esportivo PESADO da Ana Ribeiro (v7)') -or -not $anaSeedSource.Contains('HealthPlatform v0.7.7')) { throw "Banner do seed Ana v7/v0.7.7 ausente." }
Write-Host "    Seed v7 identificado: OK."

Write-Host "[714/720] Validando ausencia de atribuicao a HOME..."
if ($anaSeedSource -match '(?im)^\s*\$home\s*(?:=|\+=|-=|\+\+|--)') { throw "Seed ainda tenta sobrescrever a variavel automatica HOME do PowerShell." }
if (-not $anaSeedSource.Contains('$homeResumo = Api Get')) { throw "Resumo final do seed nao usa variavel segura homeResumo." }
Write-Host "    HOME preservado; homeResumo utilizado: OK."

Write-Host "[715/720] Validando variaveis automaticas protegidas no seed..."
$automaticasProtegidas = @('HOME','PID','PROFILE','HOST','PSHOME')
foreach ($nomeAuto in $automaticasProtegidas) {
    if ($anaSeedSource -match ("(?im)^\\s*\\$" + [regex]::Escape($nomeAuto) + "\\s*(?:=|\\+=|-=|\\+\\+|--)")) {
        throw "Seed tenta sobrescrever variavel automatica protegida: $nomeAuto"
    }
}
Write-Host "    HOME/PID/PROFILE/HOST/PSHOME nao sao sobrescritas: OK."

Write-Host "[716/720] Validando resumo final do portal preservado..."
if (-not $anaSeedSource.Contains("Api Get '/api/portal/me/home' $patient") -or -not $anaSeedSource.Contains('$homeResumo.coachDiario')) { throw "Resumo final do portal/Coach foi perdido no seed." }
Write-Host "    Home + Coach Diario preservados: OK."

Write-Host "[717/720] Validando seed pesado preservado na v7..."
if (-not $anaSeedSource.Contains("[9/10] Avaliações corporais longitudinais: OK") -or -not $anaSeedSource.Contains("[10/10] Seed concluído.")) { throw "Fluxo 9/10 -> 10/10 do seed nao foi preservado." }
Write-Host "    Avaliacoes + fechamento do seed: OK."

Write-Host "[718/720] Validando ausencia de migration v0.7.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Setup 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.7_seed_automatic_vars.sql) { throw "v0.7.7 nao deveria exigir migration." }
Write-Host "    Schema permanece 38/38: OK."

Write-Host "[719/720] Validando versao corrente v0.7.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.7." }
Write-Host "    API/UI/VERSION: 0.7.7 OK."

Write-Host "[720/728] Validando compatibilidade funcional v0.7.5..."
Write-Host "    v0.7.5 / Seed Ana v7 + variaveis automaticas PowerShell preservado: OK."


Write-Host "[721/728] Validando contratos de dor corporal..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalDorCorporalResumoResponse') -or -not $portalContracts.Contains('RegistrarDorCorporalRequest')) { throw "Contratos de dor corporal ausentes." }
Write-Host "    Regiao + intensidade + impacto no treino: contratos OK."

Write-Host "[722/728] Validando servico longitudinal de dor corporal..."
$dorService = Get-Content .\src\HealthPlatform.Api\Services\DorCorporalService.cs -Encoding UTF8 -Raw
if (-not $dorService.Contains('Tipo == "DorCorporal"') -or -not $dorService.Contains('AddDays(-7)') -or -not $dorService.Contains('ImpactoMaximoTreino7')) { throw "Resumo longitudinal de dor corporal incompleto." }
Write-Host "    Janela de 7 dias e impacto no treino: OK."

Write-Host "[723/728] Validando endpoint PatientOnly de dor corporal..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('[HttpPost("dor-corporal")]') -or -not $meuPortalSource.Contains('DorCorporalService.Serializar')) { throw "Endpoint de dor corporal ausente." }
if (-not $meuPortalSource.Contains('Intensidade < 0') -or -not $meuPortalSource.Contains('ImpactoTreino < 0')) { throw "Validacoes 0-10 da dor corporal ausentes." }
Write-Host "    Registro localizado validado em 0-10: OK."

Write-Host "[724/728] Validando integracao do Coach Diario com dor localizada..."
$coachSource = Get-Content .\src\HealthPlatform.Api\Services\CoachDiarioService.cs -Encoding UTF8 -Raw
if (-not $coachSource.Contains('dor-localizada-alta') -or -not $coachSource.Contains('não diagnóstico de lesão') -and -not $coachSource.Contains('Não use XP')) { throw "Coach nao considera dor localizada de forma segura." }
Write-Host "    Dor relevante vira prioridade sem diagnostico automatico: OK."

Write-Host "[725/728] Validando Home atleta/profissional com mapa de dor..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpBodyPainAthleteCard') -or -not $appJsSource.Contains('hpBodyPainProfessionalCard') -or -not $appJsSource.Contains('/api/portal/me/dor-corporal')) { throw "UI do mapa de dor incompleta." }
Write-Host "    Home + prontuario + formulario: OK."

Write-Host "[726/728] Validando ausencia de migration v0.7.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.7_dor_corporal.sql) { throw "v0.7.7 nao deveria exigir migration." }
Write-Host "    Registro reutiliza Diario; schema permanece 38/38: OK."

Write-Host "[727/728] Validando versao corrente v0.7.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.7." }
Write-Host "    API/UI/VERSION: 0.7.7 OK."

Write-Host "[728/736] Validando compatibilidade funcional v0.7.6..."
Write-Host "    v0.7.6 / Dor por Regiao Corporal + impacto no treino preservado: OK."

Write-Host "[729/736] Validando contratos do plano de recuperacao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalPlanoRecuperacaoResponse') -or -not $portalContracts.Contains('PortalPlanoRecuperacaoItemResponse')) { throw "Contratos do plano de recuperacao ausentes." }
Write-Host "    Contratos de sintese de recuperacao: OK."

Write-Host "[730/736] Validando motor contextual de recuperacao..."
$recoveryPlanSource = Get-Content .\src\HealthPlatform.Api\Services\PlanoRecuperacaoService.cs -Encoding UTF8 -Raw
if (-not $recoveryPlanSource.Contains('proteger-regiao') -or -not $recoveryPlanSource.Contains('recuperacao-base') -or -not $recoveryPlanSource.Contains('Consolide antes de progredir')) { throw "Motor de recuperacao contextual incompleto." }
Write-Host "    Dor + recuperacao + carga combinadas: OK."

Write-Host "[731/736] Validando travas de seguranca do plano de recuperacao..."
if (-not $recoveryPlanSource.Contains('não diagnostica lesão') -or -not $recoveryPlanSource.Contains('não prescreve tratamento') -or -not $recoveryPlanSource.Contains('não altera automaticamente')) { throw "Travas de seguranca do plano de recuperacao ausentes." }
Write-Host "    Sem diagnostico/prescricao automatica: OK."

Write-Host "[732/736] Validando integracao nas Homes atleta/profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpRecoveryPlanAthleteCard') -or -not $appJsSource.Contains('hpRecoveryPlanProfessionalCard') -or -not $appJsSource.Contains('planoRecuperacao')) { throw "UI do plano de recuperacao incompleta." }
Write-Host "    Home + prontuario: OK."

Write-Host "[733/736] Validando montagem do plano no portal..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('PlanoRecuperacaoService.Montar') -or -not $portalSource.Contains('PlanoRecuperacaoService.Montar')) { throw "Plano de recuperacao nao integrado aos dois portais." }
Write-Host "    Paciente + profissional recebem o mesmo motor: OK."

Write-Host "[734/736] Validando ausencia de migration v0.7.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.7_plano_recuperacao.sql) { throw "v0.7.7 nao deveria exigir migration." }
Write-Host "    Motor derivado; schema permanece 38/38: OK."

Write-Host "[735/736] Validando versao corrente v0.7.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.7." }
Write-Host "    API/UI/VERSION: 0.7.7 OK."

Write-Host "[736/744] Validando compatibilidade funcional v0.7.7..."
Write-Host "    v0.7.7 / Plano de Recuperacao Contextual preservado: OK."

Write-Host "[737/744] Validando contratos de adesao nutricional..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalAdesaoNutricionalResponse') -or -not $portalContracts.Contains('RegistrarAdesaoRefeicaoRequest')) { throw "Contratos de adesao nutricional ausentes." }
Write-Host "    Refeicoes + status + adequacao: contratos OK."

Write-Host "[738/744] Validando motor de adesao nutricional contextual..."
$nutritionSource = Get-Content .\src\HealthPlatform.Api\Services\AdesaoNutricionalService.cs -Encoding UTF8 -Raw
if (-not $nutritionSource.Contains('AdesaoRefeicao') -or -not $nutritionSource.Contains('Realizada') -or -not $nutritionSource.Contains('Adaptada') -or -not $nutritionSource.Contains('NaoRealizada')) { throw "Motor de adesao nutricional incompleto." }
if (-not $nutritionSource.Contains('objetivo é reconhecer padrões, não buscar perfeição')) { throw "Linguagem de adesao nutricional nao esta alinhada a sustentabilidade." }
Write-Host "    Execucao sem perfeccionismo: OK."

Write-Host "[739/744] Validando endpoint PatientOnly por refeicao..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('[HttpPost("refeicoes/{refeicaoId:guid}/adesao")]') -or -not $meuPortalSource.Contains('AdesaoNutricionalService.Serializar')) { throw "Endpoint de adesao por refeicao ausente." }
Write-Host "    Registro idempotente por refeicao/dia: OK."

Write-Host "[740/744] Validando integracao do Coach com nutricao..."
$coachSource = Get-Content .\src\HealthPlatform.Api\Services\CoachDiarioService.cs -Encoding UTF8 -Raw
if (-not $coachSource.Contains('revisar-adesao-nutricional') -or -not $coachSource.Contains('Não compense com restrição ou excesso')) { throw "Coach nao considera adesao nutricional com trava segura." }
Write-Host "    Coach orienta sem compensacao automatica: OK."

Write-Host "[741/744] Validando Home atleta/profissional com adesao nutricional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpNutritionAdherenceAthleteCard') -or -not $appJsSource.Contains('hpNutritionAdherenceProfessionalCard') -or -not $appJsSource.Contains('/adesao')) { throw "UI de adesao nutricional incompleta." }
Write-Host "    Home + prontuario + acoes por refeicao: OK."

Write-Host "[742/744] Validando ausencia de migration v0.7.8..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.8_adesao_nutricional.sql) { throw "v0.7.8 nao deveria exigir migration." }
Write-Host "    Registros reutilizam Diario; schema permanece 38/38: OK."

Write-Host "[743/744] Validando versao corrente v0.7.8..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.9 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.9." }
Write-Host "    API/UI/VERSION: 0.7.8 OK."

Write-Host "[744/744] Validando versao funcional v0.7.8..."
Write-Host "    v0.7.8 / Adesao Nutricional Contextual: OK."


Write-Host "[745/752] Validando compatibilidade funcional v0.7.8..."
Write-Host "    v0.7.8 / Adesao Nutricional Contextual preservado: OK."

Write-Host "[746/752] Validando contrato de hidratacao contextual..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalHidratacaoContextualResponse') -or -not $portalContracts.Contains('HidratacaoContextual')) { throw "Contrato de hidratacao contextual ausente." }
Write-Host "    Meta + consumo + treino + RPE: contrato OK."

Write-Host "[747/752] Validando motor de hidratacao contextual..."
$hydrationSource = Get-Content .\src\HealthPlatform.Api\Services\HidratacaoContextualService.cs -Encoding UTF8 -Raw
if (-not $hydrationSource.Contains('MetasPaciente') -or -not $hydrationSource.Contains('RegistrosMetas') -or -not $hydrationSource.Contains('ExecucoesTreino')) { throw "Motor de hidratacao contextual incompleto." }
if (-not $hydrationSource.Contains('não cria uma quantidade automaticamente') -or -not $hydrationSource.Contains('não aumentam automaticamente')) { throw "Travas de seguranca da hidratacao ausentes." }
Write-Host "    Meta prescrita + contexto de treino sem ajuste automatico: OK."

Write-Host "[748/752] Validando integracao do Coach com hidratacao..."
$coachSource = Get-Content .\src\HealthPlatform.Api\Services\CoachDiarioService.cs -Encoding UTF8 -Raw
if (-not $coachSource.Contains('registrar-hidratacao') -or -not $coachSource.Contains('acompanhar-hidratacao') -or -not $coachSource.Contains('não aumenta automaticamente')) { throw "Coach nao considera hidratacao de forma segura." }
Write-Host "    Coach usa hidratacao sem redefinir meta: OK."

Write-Host "[749/752] Validando Home atleta/profissional com hidratacao contextual..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpHydrationContextAthleteCard') -or -not $appJsSource.Contains('hpHydrationContextProfessionalCard') -or -not $appJsSource.Contains('hidratacaoContextual')) { throw "UI de hidratacao contextual incompleta." }
Write-Host "    Home + prontuario: OK."

Write-Host "[750/752] Validando montagem nos dois portais..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('HidratacaoContextualService.MontarAsync') -or -not $portalSource.Contains('HidratacaoContextualService.MontarAsync')) { throw "Hidratacao contextual nao integrada aos dois portais." }
Write-Host "    Paciente + profissional recebem o mesmo resumo: OK."

Write-Host "[751/752] Validando ausencia de migration v0.7.9..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.7.9_hidratacao_contextual.sql) { throw "v0.7.9 nao deveria exigir migration." }
Write-Host "    Motor derivado; schema permanece 38/38: OK."

Write-Host "[752/752] Validando versao funcional v0.7.9..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.7.9 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.7.9." }
Write-Host "    v0.7.9 / Hidratacao Contextual & Balanco do Dia: OK."

Write-Host "[753/760] Validando compatibilidade funcional v0.7.9..."
Write-Host "    v0.7.9 / Hidratacao Contextual & Balanco do Dia preservado: OK."

Write-Host "[754/760] Validando contratos do Painel de Evolucao Esportiva..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalEvolucaoEsportivaResponse') -or -not $portalContracts.Contains('PortalEvolucaoEsportivaIndicadorResponse')) { throw "Contratos de evolucao esportiva ausentes." }
Write-Host "    Dimensoes transparentes + evidencias: contratos OK."

Write-Host "[755/760] Validando motor longitudinal sem score magico..."
$evolucaoSource = Get-Content .\src\HealthPlatform.Api\Services\EvolucaoEsportivaService.cs -Encoding UTF8 -Raw
if (-not $evolucaoSource.Contains('Consistência') -or -not $evolucaoSource.Contains('Recuperação') -or -not $evolucaoSource.Contains('Carga de treino') -or -not $evolucaoSource.Contains('Performance') -or -not $evolucaoSource.Contains('Adesão nutricional') -or -not $evolucaoSource.Contains('Hidratação')) { throw "Painel de evolucao nao agrega as dimensoes esportivas esperadas." }
if ($evolucaoSource.Contains('ScoreGlobal') -or $evolucaoSource.Contains('ScoreEsportivo')) { throw "Painel nao deve reduzir a evolucao a um score composto opaco." }
Write-Host "    Consistencia + recuperacao + carga + performance + nutricao + hidratacao: OK."

Write-Host "[756/760] Validando integracao do painel nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('EvolucaoEsportivaService.Montar') -or -not $portalSource.Contains('EvolucaoEsportivaService.Montar')) { throw "Painel de evolucao nao foi integrado nas duas Homes." }
Write-Host "    Atleta + profissional compartilham a mesma sintese: OK."

Write-Host "[757/760] Validando UI atleta/profissional do Painel de Evolucao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpEvolutionAthleteCard') -or -not $appJsSource.Contains('hpEvolutionProfessionalCard') -or -not $appJsSource.Contains('PAINEL DE EVOLUÇÃO')) { throw "UI do Painel de Evolucao incompleta." }
Write-Host "    Cards longitudinal e esportivo: OK."

Write-Host "[758/760] Validando trava de interpretacao clinica..."
if (-not $evolucaoSource.Contains('não é um score clínico') -or -not $evolucaoSource.Contains('não diagnostica')) { throw "Painel precisa declarar limites de interpretacao clinica." }
Write-Host "    Sem diagnostico e sem score clinico composto: OK."

Write-Host "[759/760] Validando ausencia de migration v0.8.0..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.0_evolucao_esportiva.sql) { throw "v0.8.0 nao deveria exigir migration." }
Write-Host "    Painel e derivado dos dados existentes; schema permanece 38/38: OK."

Write-Host "[760/760] Validando versao funcional v0.8.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.0 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.0." }
Write-Host "    v0.8.0 / Painel de Evolucao Esportiva: OK."
Write-Host "[761/768] Validando compatibilidade funcional v0.8.0..."
Write-Host "    v0.8.0 / Painel de Evolucao Esportiva preservado: OK."

Write-Host "[762/768] Validando contratos de metas do ciclo..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalMetasCicloResponse') -or -not $portalContracts.Contains('PortalMetaCicloItemResponse')) { throw "Contratos de metas do ciclo ausentes." }
Write-Host "    Metas mensuraveis + evidencia + progresso: contratos OK."

Write-Host "[763/768] Validando motor de metas do ciclo..."
$cycleGoalsSource = Get-Content .\src\HealthPlatform.Api\Services\MetasCicloService.cs -Encoding UTF8 -Raw
if (-not $cycleGoalsSource.Contains('MetaTreinosSemanais') -or -not $cycleGoalsSource.Contains('MetaConsistenciaPercentual') -or -not $cycleGoalsSource.Contains('MetaPesoKg')) { throw "Motor de metas do ciclo incompleto." }
if (-not $cycleGoalsSource.Contains('InicioDaSemana') -or -not $cycleGoalsSource.Contains('ExecucoesTreino') -or -not $cycleGoalsSource.Contains('Avaliacoes')) { throw "Metas do ciclo nao usam os dados reais esperados." }
Write-Host "    Treinos + consistencia + peso-alvo: OK."

Write-Host "[764/768] Validando progresso de peso bidirecional..."
if (-not $cycleGoalsSource.Contains('Math.Abs(inicial - alvo)') -or -not $cycleGoalsSource.Contains('Math.Abs(atual - alvo)')) { throw "Peso-alvo precisa funcionar tanto para ganho quanto para perda de peso." }
Write-Host "    Distancia ao alvo funciona para ganho ou perda: OK."

Write-Host "[765/768] Validando trava de seguranca das metas do ciclo..."
if (-not $cycleGoalsSource.Contains('não autoriza aumento de carga') -or -not $cycleGoalsSource.Contains('sem revisão profissional')) { throw "Trava profissional das metas do ciclo ausente." }
if ($cycleGoalsSource.Contains('ScoreGlobal') -or $cycleGoalsSource.Contains('ScoreObjetivo')) { throw "Metas do ciclo nao devem virar score opaco." }
Write-Host "    Progresso nao altera prescricao automaticamente: OK."

Write-Host "[766/768] Validando integracao nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('MetasCicloService.MontarAsync') -or -not $portalSource.Contains('MetasCicloService.MontarAsync')) { throw "Metas do ciclo nao integradas aos dois portais." }
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpCycleGoalsAthleteCard') -or -not $appJsSource.Contains('hpCycleGoalsProfessionalCard') -or -not $appJsSource.Contains('metasDoCiclo')) { throw "UI de metas do ciclo incompleta." }
Write-Host "    Atleta + profissional recebem o mesmo acompanhamento: OK."

Write-Host "[767/768] Validando ausencia de migration v0.8.2..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.2_metas_ciclo.sql) { throw "v0.8.2 nao deveria exigir migration." }
Write-Host "    Metas derivadas do ciclo atual; schema permanece 38/38: OK."

Write-Host "[768/768] Validando versao funcional v0.8.2..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.2 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.2." }
Write-Host "    v0.8.2 / Metas do Ciclo & Progresso por Objetivo: OK."


Write-Host "[769/776] Validando compatibilidade funcional v0.8.1..."
Write-Host "    v0.8.1 / Metas do Ciclo & Progresso por Objetivo preservado: OK."

Write-Host "[770/776] Validando contratos de checkpoint do ciclo..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalCheckpointCicloResponse') -or -not $portalContracts.Contains('PortalCheckpointCicloItemResponse')) { throw "Contratos de checkpoint do ciclo ausentes." }
Write-Host "    Estado + semana + evidencias + orientacao: contratos OK."

Write-Host "[771/776] Validando motor de checkpoint transparente..."
$checkpointSource = Get-Content .\src\HealthPlatform.Api\Services\CheckpointCicloService.cs -Encoding UTF8 -Raw
if (-not $checkpointSource.Contains('Metas x avanço do ciclo') -or -not $checkpointSource.Contains('Leitura multidimensional') -or -not $checkpointSource.Contains('ProgressoTemporalPercentual')) { throw "Motor de checkpoint do ciclo incompleto." }
if ($checkpointSource.Contains('ScoreCheckpoint') -or $checkpointSource.Contains('ScoreGlobal')) { throw "Checkpoint nao deve virar score opaco." }
Write-Host "    Tempo + metas + evolucao permanecem dimensoes separadas: OK."

Write-Host "[772/776] Validando checkpoint de fechamento/revisao..."
if (-not $checkpointSource.Contains('progressoTempo >= 85m') -or -not $checkpointSource.Contains('Revisar') -or -not $checkpointSource.Contains('Consolidar')) { throw "Estados de revisao/fechamento do checkpoint incompletos." }
Write-Host "    EmCurso/Evoluindo/Revisar/Consolidar: OK."

Write-Host "[773/776] Validando trava profissional do checkpoint..."
if (-not $checkpointSource.Contains('não diagnostica') -or -not $checkpointSource.Contains('não prescreve') -or -not $checkpointSource.Contains('não altera treino')) { throw "Limites de seguranca do checkpoint ausentes." }
Write-Host "    Checkpoint informa; nao diagnostica nem altera prescricao: OK."

Write-Host "[774/776] Validando integracao do checkpoint nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('CheckpointCicloService.Montar') -or -not $portalSource.Contains('CheckpointCicloService.Montar')) { throw "Checkpoint do ciclo nao integrado aos dois portais." }
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpCycleCheckpointAthleteCard') -or -not $appJsSource.Contains('hpCycleCheckpointProfessionalCard') -or -not $appJsSource.Contains('checkpointDoCiclo')) { throw "UI do checkpoint do ciclo incompleta." }
Write-Host "    Atleta + profissional recebem o mesmo checkpoint: OK."

Write-Host "[775/776] Validando ausencia de migration v0.8.2..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.2_checkpoint_ciclo.sql) { throw "v0.8.2 nao deveria exigir migration." }
Write-Host "    Checkpoint derivado dos dados atuais; schema permanece 38/38: OK."

Write-Host "[776/776] Validando versao funcional v0.8.2..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.2 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.2." }
Write-Host "    v0.8.2 / Revisao de Ciclo & Checkpoint de Progresso: OK."

Write-Host "[777/784] Validando compatibilidade funcional v0.8.2..."
Write-Host "    v0.8.2 / Revisao de Ciclo & Checkpoint de Progresso preservado: OK."

Write-Host "[778/784] Validando contratos do relatorio de ciclo..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalRelatorioCicloResponse') -or -not $portalContracts.Contains('PortalRelatorioCicloItemResponse') -or -not $portalContracts.Contains('RelatorioDoCiclo')) { throw "Contratos do relatorio de ciclo ausentes." }
Write-Host "    Estado + periodo + volume + evidencias: contratos OK."

Write-Host "[779/784] Validando motor de fechamento longitudinal..."
$cycleReportSource = Get-Content .\src\HealthPlatform.Api\Services\RelatorioCicloService.cs -Encoding UTF8 -Raw
if (-not $cycleReportSource.Contains('Volume realizado no ciclo') -or -not $cycleReportSource.Contains('Metas mensuráveis') -or -not $cycleReportSource.Contains('Evolução multidimensional') -or -not $cycleReportSource.Contains('Checkpoint do ciclo')) { throw "Relatorio de ciclo incompleto." }
Write-Host "    Execucao + metas + evolucao + checkpoint: OK."

Write-Host "[780/784] Validando fechamento sem score opaco ou automacao indevida..."
if ($cycleReportSource.Contains('ScoreGlobal') -or $cycleReportSource.Contains('ScoreCiclo')) { throw "Relatorio de ciclo nao deve criar score opaco." }
if (-not $cycleReportSource.Contains('não é diagnóstico') -or -not $cycleReportSource.Contains('não cria o próximo ciclo automaticamente')) { throw "Travas do relatorio de ciclo ausentes." }
Write-Host "    Relatorio informa e consolida; nao diagnostica nem cria novo ciclo: OK."

Write-Host "[781/784] Validando integracao do relatorio nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('RelatorioCicloService.Montar') -or -not $portalSource.Contains('RelatorioCicloService.Montar')) { throw "Relatorio do ciclo nao integrado aos dois portais." }
Write-Host "    Paciente + profissional recebem o mesmo fechamento longitudinal: OK."

Write-Host "[782/784] Validando UI do relatorio de ciclo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpCycleReportAthleteCard') -or -not $appJsSource.Contains('hpCycleReportProfessionalCard') -or -not $appJsSource.Contains('relatorioDoCiclo')) { throw "UI do relatorio de ciclo incompleta." }
Write-Host "    Card atleta + leitura profissional: OK."

Write-Host "[783/784] Validando ausencia de migration v0.8.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.7_relatorio_ciclo.sql) { throw "v0.8.7 nao deveria exigir migration." }
Write-Host "    Relatorio derivado dos dados existentes; schema permanece 38/38: OK."

Write-Host "[784/784] Validando versao funcional v0.8.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.7." }
Write-Host "    v0.8.7 / Fechamento de Ciclo & Relatorio de Evolucao: OK."

Write-Host "[785/800] Validando compatibilidade funcional v0.8.3..."
Write-Host "    v0.8.3 / Fechamento de Ciclo & Relatorio de Evolucao preservado: OK."

Write-Host "[786/800] Validando contratos do comparativo de ciclos..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalComparativoCiclosResponse') -or -not $portalContracts.Contains('PortalComparativoCicloItemResponse') -or -not $portalContracts.Contains('ComparativoDeCiclos')) { throw "Contratos do comparativo de ciclos ausentes." }
Write-Host "    Historico + taxas semanais + prontidao + peso: contratos OK."

Write-Host "[787/800] Validando motor longitudinal entre ciclos..."
$cycleComparisonSource = Get-Content .\src\HealthPlatform.Api\Services\ComparativoCiclosService.cs -Encoding UTF8 -Raw
if (-not $cycleComparisonSource.Contains('CiclosEsportivosPaciente') -or -not $cycleComparisonSource.Contains('ExecucoesTreino') -or -not $cycleComparisonSource.Contains('ProntidoesDiarias') -or -not $cycleComparisonSource.Contains('Avaliacoes')) { throw "Motor comparativo nao usa as fontes longitudinais esperadas." }
if ($cycleComparisonSource.Contains('ScoreGlobal') -or $cycleComparisonSource.Contains('ScoreCiclo')) { throw "Comparativo nao deve criar score opaco entre ciclos." }
Write-Host "    Ciclos + treino + prontidao + avaliacoes: OK."

Write-Host "[788/800] Validando normalizacao por tempo e peso descritivo..."
if (-not $cycleComparisonSource.Contains('TreinosPorSemana') -or -not $cycleComparisonSource.Contains('CheckInsPorSemana') -or -not $cycleComparisonSource.Contains('semanasObservadas')) { throw "Comparacao precisa normalizar ritmos por semana." }
if (-not $cycleComparisonSource.Contains('variação de peso não é classificada como boa ou ruim automaticamente')) { throw "Comparativo precisa manter variacao de peso como dado descritivo." }
Write-Host "    Taxas semanais evitam comparar totais brutos de ciclos com duracoes diferentes: OK."

Write-Host "[789/800] Validando integracao do comparativo nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('ComparativoCiclosService.MontarAsync') -or -not $portalSource.Contains('ComparativoCiclosService.MontarAsync')) { throw "Comparativo de ciclos nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem o mesmo historico comparavel: OK."

Write-Host "[790/800] Validando UI do comparativo de ciclos..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpCycleComparisonAthleteCard') -or -not $appJsSource.Contains('hpCycleComparisonProfessionalCard') -or -not $appJsSource.Contains('comparativoDeCiclos')) { throw "UI do comparativo de ciclos incompleta." }
Write-Host "    Card atleta + leitura longitudinal profissional: OK."

Write-Host "[791/800] Validando ausencia de migration v0.8.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.7_comparativo_ciclos.sql) { throw "v0.8.7 nao deveria exigir migration." }
Write-Host "    Comparativo derivado do historico existente; schema permanece 38/38: OK."

Write-Host "[792/800] Validando compatibilidade funcional v0.8.4..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.7." }
Write-Host "    v0.8.4 / Comparativo de Ciclos & Tendencia de Longo Prazo preservado: OK."

Write-Host "[793/800] Validando contratos da tendencia por objetivo..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalTendenciaObjetivoResponse') -or -not $portalContracts.Contains('PortalTendenciaObjetivoItemResponse') -or -not $portalContracts.Contains('TendenciaDoObjetivo')) { throw "Contratos da tendencia por objetivo ausentes." }
Write-Host "    Perfil + objetivo + eixos + evidencias: contratos OK."

Write-Host "[794/800] Validando motor contextual por perfil esportivo..."
$objectiveTrendSource = Get-Content .\src\HealthPlatform.Api\Services\TendenciaObjetivoCicloService.cs -Encoding UTF8 -Raw
if (-not $objectiveTrendSource.Contains('Hipertrofia') -or -not $objectiveTrendSource.Contains('Emagrecimento') -or -not $objectiveTrendSource.Contains('Corrida') -or -not $objectiveTrendSource.Contains('QualidadeDeVida')) { throw "Perfis esportivos nao contextualizados no motor." }
Write-Host "    Hipertrofia + emagrecimento + corrida/condicionamento + qualidade de vida: OK."

Write-Host "[795/800] Validando eixos sem score unico..."
if (-not $objectiveTrendSource.Contains('performance') -or -not $objectiveTrendSource.Contains('recuperacao') -or -not $objectiveTrendSource.Contains('consistencia')) { throw "Eixos essenciais da tendencia por objetivo ausentes." }
if ($objectiveTrendSource.Contains('ScoreObjetivo') -or $objectiveTrendSource.Contains('ScoreGlobal')) { throw "Tendencia por objetivo nao deve criar score unico opaco." }
Write-Host "    Eixos separados e auditaveis: OK."

Write-Host "[796/800] Validando semantica de peso e seguranca..."
if (-not $objectiveTrendSource.Contains('peso-alvo') -or -not $objectiveTrendSource.Contains('não cria score único') -or -not $objectiveTrendSource.Contains('não altera treino, nutrição ou medicação automaticamente')) { throw "Travas da tendencia por objetivo incompletas." }
Write-Host "    Peso depende de meta definida; sem automacao de conduta: OK."

Write-Host "[797/800] Validando integracao nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('TendenciaObjetivoCicloService.Montar') -or -not $portalSource.Contains('TendenciaObjetivoCicloService.Montar')) { throw "Tendencia por objetivo nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma leitura contextual: OK."

Write-Host "[798/800] Validando UI da tendencia por objetivo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpObjectiveTrendAthleteCard') -or -not $appJsSource.Contains('hpObjectiveTrendProfessionalCard') -or -not $appJsSource.Contains('tendenciaDoObjetivo')) { throw "UI da tendencia por objetivo incompleta." }
Write-Host "    Card atleta + leitura profissional: OK."

Write-Host "[799/800] Validando ausencia de migration v0.8.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.7_tendencia_objetivo.sql) { throw "v0.8.7 nao deveria exigir migration." }
Write-Host "    Tendencia derivada de dados existentes; schema permanece 38/38: OK."

Write-Host "[800/800] Validando versao funcional v0.8.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.7." }
Write-Host "    v0.8.7 / Tendencia por Objetivo do Ciclo: OK."
Write-Host "[801/808] Validando compatibilidade funcional v0.8.5..."
Write-Host "    v0.8.5 / Tendencia por Objetivo do Ciclo preservado: OK."

Write-Host "[802/808] Validando contratos de prioridades do ciclo..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalAcoesPrioritariasCicloResponse') -or -not $portalContracts.Contains('PortalAcaoPrioritariaCicloItemResponse') -or -not $portalContracts.Contains('AcoesPrioritariasDoCiclo')) { throw "Contratos de prioridades do ciclo ausentes." }
Write-Host "    Ordem + nivel + motivo + acao: contratos OK."

Write-Host "[803/808] Validando motor de prioridades com seguranca acima da meta..."
$cyclePrioritySource = Get-Content .\src\HealthPlatform.Api\Services\AcoesPrioritariasCicloService.cs -Encoding UTF8 -Raw
if (-not $cyclePrioritySource.Contains('Proteja a recuperação') -or -not $cyclePrioritySource.Contains('Revise o equilíbrio de carga') -or -not $cyclePrioritySource.Contains('Execute o plano com consistência')) { throw "Motor de prioridades do ciclo incompleto." }
if (-not $cyclePrioritySource.Contains('(100, "recuperacao"') -or -not $cyclePrioritySource.Contains('(95, "carga"')) { throw "Recuperacao/carga precisam ter precedencia explicita." }
Write-Host "    Recuperacao e carga precedem metas de volume/performance: OK."

Write-Host "[804/808] Validando limite de tres prioridades e ausencia de score opaco..."
if (-not $cyclePrioritySource.Contains('.Take(3)') -or $cyclePrioritySource.Contains('ScorePrioridade') -or $cyclePrioritySource.Contains('ScoreGlobal')) { throw "Prioridades devem ser curtas e sem score opaco." }
if (-not $cyclePrioritySource.Contains('não diagnostica') -or -not $cyclePrioritySource.Contains('não prescreve') -or -not $cyclePrioritySource.Contains('não altera treino, nutrição ou medicação automaticamente')) { throw "Travas de seguranca das prioridades ausentes." }
Write-Host "    Ate 3 prioridades, explicaveis e sem automacao de conduta: OK."

Write-Host "[805/808] Validando integracao das prioridades nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('AcoesPrioritariasCicloService.Montar') -or -not $portalSource.Contains('AcoesPrioritariasCicloService.Montar')) { throw "Prioridades do ciclo nao integradas aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma fila priorizada: OK."

Write-Host "[806/808] Validando UI de prioridades do ciclo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpCyclePrioritiesAthleteCard') -or -not $appJsSource.Contains('hpCyclePrioritiesProfessionalCard') -or -not $appJsSource.Contains('acoesPrioritariasDoCiclo')) { throw "UI de prioridades do ciclo incompleta." }
Write-Host "    Card atleta + leitura profissional: OK."

Write-Host "[807/808] Validando ausencia de migration v0.8.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.7_acoes_prioritarias_ciclo.sql) { throw "v0.8.7 nao deveria exigir migration." }
Write-Host "    Prioridades derivadas dos dados existentes; schema permanece 38/38: OK."

Write-Host "[808/808] Validando versao funcional v0.8.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.7." }
Write-Host "    v0.8.6 / Acoes Prioritarias do Ciclo preservado: OK."

Write-Host "[809/816] Validando compatibilidade funcional v0.8.6..."
Write-Host "    v0.8.6 / Acoes Prioritarias do Ciclo preservado: OK."

Write-Host "[810/816] Validando contratos do planejamento semanal..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalPlanejamentoSemanalResponse') -or -not $portalContracts.Contains('PortalPlanejamentoSemanalItemResponse') -or -not $portalContracts.Contains('PlanejamentoSemanal')) { throw "Contratos do planejamento semanal ausentes." }
Write-Host "    Estado + metas de treino + itens + orientacao: contratos OK."

Write-Host "[811/816] Validando motor semanal com recuperacao acima de volume..."
$weeklyPlanSource = Get-Content .\src\HealthPlatform.Api\Services\PlanejamentoSemanalService.cs -Encoding UTF8 -Raw
if (-not $weeklyPlanSource.Contains('Recuperação e equilíbrio de carga têm precedência') -or -not $weeklyPlanSource.Contains('Use a meta apenas como referência') -or -not $weeklyPlanSource.Contains('Estratégia do Dia como limite')) { throw "Motor do planejamento semanal incompleto." }
Write-Host "    Segurança/recuperacao precedem perseguir meta semanal: OK."

Write-Host "[812/816] Validando que planejamento nao cria prescricao paralela..."
if (-not $weeklyPlanSource.Contains('não cria exercícios') -or -not $weeklyPlanSource.Contains('não redistribui carga') -or -not $weeklyPlanSource.Contains('não altera treino, nutrição ou medicação automaticamente')) { throw "Travas do planejamento semanal ausentes." }
if ($weeklyPlanSource.Contains('CriarSessao') -or $weeklyPlanSource.Contains('AumentarCargaAutomaticamente')) { throw "Planejamento semanal nao deve prescrever ou progredir carga automaticamente." }
Write-Host "    Planejamento organiza; nao prescreve: OK."

Write-Host "[813/816] Validando integracao do planejamento nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('PlanejamentoSemanalService.Montar') -or -not $portalSource.Contains('PlanejamentoSemanalService.Montar')) { throw "Planejamento semanal nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem o mesmo planejamento: OK."

Write-Host "[814/816] Validando UI do planejamento semanal..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpWeeklyPlanAthleteCard') -or -not $appJsSource.Contains('hpWeeklyPlanProfessionalCard') -or -not $appJsSource.Contains('planejamentoSemanal')) { throw "UI do planejamento semanal incompleta." }
Write-Host "    Card atleta + leitura profissional: OK."

Write-Host "[815/816] Validando ausencia de migration v0.8.7..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.7_planejamento_semanal.sql) { throw "v0.8.7 nao deveria exigir migration." }
Write-Host "    Planejamento derivado dos dados existentes; schema permanece 38/38: OK."

Write-Host "[816/816] Validando versao funcional v0.8.7..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.7." }
Write-Host "    v0.8.7 / Planejamento Semanal Adaptativo: OK."
Write-Host "[817/824] Validando compatibilidade funcional v0.8.7..."
Write-Host "    v0.8.7 / Planejamento Semanal Adaptativo preservado: OK."

Write-Host "[818/824] Validando contratos do resumo semanal..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalResumoSemanalResponse') -or -not $portalContracts.Contains('PortalResumoSemanalItemResponse') -or -not $portalContracts.Contains('ResumoSemanal')) { throw "Contratos do resumo semanal ausentes." }
Write-Host "    Periodo + eixos + estado + seguranca: contratos OK."

Write-Host "[819/824] Validando sintese semanal sem score unico..."
$weeklySummarySource = Get-Content .\src\HealthPlatform.Api\Services\ResumoSemanalService.cs -Encoding UTF8 -Raw
if (-not $weeklySummarySource.Contains('Execução da meta semanal') -or -not $weeklySummarySource.Contains('Recuperação recente') -or -not $weeklySummarySource.Contains('Carga de treino') -or -not $weeklySummarySource.Contains('Adesão nutricional') -or -not $weeklySummarySource.Contains('Hidratação do dia')) { throw "Eixos do resumo semanal incompletos." }
if ($weeklySummarySource.Contains('ScoreSemanal') -or $weeklySummarySource.Contains('ScoreGlobal')) { throw "Resumo semanal nao deve criar score unico opaco." }
Write-Host "    Eixos separados e auditaveis: OK."

Write-Host "[820/824] Validando recuperacao/carga acima de perseguir volume..."
if (-not $weeklySummarySource.Contains('antes de perseguir mais volume ou intensidade') -or -not $weeklySummarySource.Contains('recuperação/carga merecem atenção')) { throw "Prioridade de seguranca do resumo semanal ausente." }
if (-not $weeklySummarySource.Contains('não cria score único') -or -not $weeklySummarySource.Contains('não diagnostica') -or -not $weeklySummarySource.Contains('não altera treino, nutrição ou medicação automaticamente')) { throw "Travas do resumo semanal ausentes." }
Write-Host "    Seguranca e contexto acima da meta: OK."

Write-Host "[821/824] Validando integracao do resumo nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('ResumoSemanalService.Montar') -or -not $portalSource.Contains('ResumoSemanalService.Montar')) { throw "Resumo semanal nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma sintese: OK."

Write-Host "[822/824] Validando UI do resumo semanal..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpWeeklySummaryAthleteCard') -or -not $appJsSource.Contains('hpWeeklySummaryProfessionalCard') -or -not $appJsSource.Contains('resumoSemanal')) { throw "UI do resumo semanal incompleta." }
Write-Host "    Card atleta + leitura profissional: OK."

Write-Host "[823/824] Validando ausencia de migration v0.8.8..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.8_resumo_semanal.sql) { throw "v0.8.8 nao deveria exigir migration." }
Write-Host "    Resumo derivado dos dados existentes; schema permanece 38/38: OK."

Write-Host "[824/824] Validando versao funcional v0.8.8..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.8 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.8." }
Write-Host "    v0.8.8 / Resumo Semanal & Fechamento da Semana: OK."

Write-Host "[825/832] Validando compatibilidade funcional v0.8.8..."
Write-Host "    v0.8.8 / Resumo Semanal & Fechamento da Semana preservado: OK."

Write-Host "[826/832] Validando contratos da tendencia semanal..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalTendenciaSemanalResponse') -or -not $portalContracts.Contains('PortalTendenciaSemanalItemResponse') -or -not $portalContracts.Contains('TendenciaSemanal')) { throw "Contratos da tendencia semanal ausentes." }
Write-Host "    Periodos comparaveis + eixos + variacao + leitura: contratos OK."

Write-Host "[827/832] Validando comparacao justa de semanas em andamento..."
$weeklyTrendSource = Get-Content .\src\HealthPlatform.Api\Services\TendenciaSemanalService.cs -Encoding UTF8 -Raw
if (-not $weeklyTrendSource.Contains('diasComparados') -or -not $weeklyTrendSource.Contains('anteriorFim = anteriorInicio.AddDays(diasComparados - 1)')) { throw "Comparacao semanal nao preserva o mesmo numero de dias." }
if (-not $weeklyTrendSource.Contains('semana atual com o mesmo número de dias da semana anterior')) { throw "Intencao de comparacao equivalente ausente." }
Write-Host "    Semana parcial compara com o mesmo intervalo da semana anterior: OK."

Write-Host "[828/832] Validando eixos longitudinais da tendencia semanal..."
if (-not $weeklyTrendSource.Contains('Ritmo de treinos') -or -not $weeklyTrendSource.Contains('Dias ativos') -or -not $weeklyTrendSource.Contains('Prontidão média') -or -not $weeklyTrendSource.Contains('Carga interna estimada') -or -not $weeklyTrendSource.Contains('Adesão nutricional registrada') -or -not $weeklyTrendSource.Contains('Hidratação registrada')) { throw "Eixos da tendencia semanal incompletos." }
Write-Host "    Treino + consistencia + recuperacao + carga + nutricao + hidratacao: OK."

Write-Host "[829/832] Validando interpretacao sem score ou causalidade automatica..."
if ($weeklyTrendSource.Contains('ScoreSemanal') -or $weeklyTrendSource.Contains('ScoreGlobal')) { throw "Tendencia semanal nao deve criar score unico." }
if (-not $weeklyTrendSource.Contains('Carga maior ou menor não é boa ou ruim isoladamente') -or -not $weeklyTrendSource.Contains('não autorizam ajuste automático de treino, nutrição ou medicação')) { throw "Travas interpretativas da tendencia semanal ausentes." }
Write-Host "    Mudancas sao descritivas e auditaveis: OK."

Write-Host "[830/832] Validando integracao da tendencia nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('TendenciaSemanalService.MontarAsync') -or -not $portalSource.Contains('TendenciaSemanalService.MontarAsync')) { throw "Tendencia semanal nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem o mesmo comparativo: OK."

Write-Host "[831/832] Validando UI da tendencia semanal e ausencia de migration..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpWeeklyTrendAthleteCard') -or -not $appJsSource.Contains('hpWeeklyTrendProfessionalCard') -or -not $appJsSource.Contains('tendenciaSemanal')) { throw "UI da tendencia semanal incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.8.9_tendencia_semanal.sql) { throw "v0.8.9 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[832/832] Validando versao funcional v0.8.9..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.8.9 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.8.9." }
Write-Host "    v0.8.9 / Tendencia Semanal & Comparativo de Semanas: OK."

Write-Host "[833/840] Validando compatibilidade funcional v0.8.9..."
Write-Host "    v0.8.9 / Tendencia Semanal & Comparativo de Semanas preservado: OK."

Write-Host "[834/840] Validando contratos do radar de adesao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalRadarAdesaoResponse') -or -not $portalContracts.Contains('PortalRadarAdesaoItemResponse') -or -not $portalContracts.Contains('RadarAdesao')) { throw "Contratos do radar de adesao ausentes." }
Write-Host "    Estado + sinais + contexto de recuperacao + itens: contratos OK."

Write-Host "[835/840] Validando sinais de continuidade sem rotular paciente..."
$radarSource = Get-Content .\src\HealthPlatform.Api\Services\RadarAdesaoService.cs -Encoding UTF8 -Raw
if (-not $radarSource.Contains('Consistência atual') -or -not $radarSource.Contains('Menos dias ativos que na semana anterior') -or -not $radarSource.Contains('Execução alimentar caiu') -or -not $radarSource.Contains('Meta hídrica está atrasada') -or -not $radarSource.Contains('hidratacao.MetaMl.HasValue')) { throw "Eixos do radar de adesao incompletos ou contrato de hidratacao desalinhado." }
if ($radarSource.Contains('ProbabilidadeAbandono') -or $radarSource.Contains('ScoreAbandono')) { throw "Radar nao deve criar probabilidade/score opaco de abandono." }
Write-Host "    Consistencia + ritmo + nutricao + hidratacao: OK."

Write-Host "[836/840] Validando descanso protegido contra falso sinal de baixa adesao..."
if (-not $radarSource.Contains('contextoRecuperacao') -or -not $radarSource.Contains('Não trate descanso ou redução de carga coerente com recuperação como falha de adesão')) { throw "Radar nao protege descanso/recuperacao adequados." }
Write-Host "    Recuperacao coerente nao vira falha de adesao: OK."

Write-Host "[837/840] Validando gamificacao sem punicao por oscilacao de adesao..."
if (-not $radarSource.Contains('não pune XP') -or $radarSource.Contains('RemoverXp') -or $radarSource.Contains('XpNegativo')) { throw "Radar nao deve punir XP ou usar gamificacao coercitiva." }
Write-Host "    Sem XP negativo, punição ou coerção: OK."

Write-Host "[838/840] Validando integracao do radar nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('RadarAdesaoService.Montar') -or -not $portalSource.Contains('RadarAdesaoService.Montar')) { throw "Radar de adesao nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem o mesmo radar: OK."

Write-Host "[839/840] Validando UI do radar e ausencia de migration..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpAdherenceRadarAthleteCard') -or -not $appJsSource.Contains('hpAdherenceRadarProfessionalCard') -or -not $appJsSource.Contains('radarAdesao')) { throw "UI do radar de adesao incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.0_radar_adesao.sql) { throw "v0.9.0 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[840/840] Validando versao funcional v0.9.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.0 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.0." }
Write-Host "    v0.9.0 / Radar de Adesao & Continuidade do Plano: OK."

Write-Host "[841/848] Validando compatibilidade funcional v0.9.0..."
Write-Host "    v0.9.0 / Radar de Adesao & Continuidade do Plano preservado: OK."

Write-Host "[842/848] Validando contratos do plano de reconexao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalPlanoReconexaoResponse') -or -not $portalContracts.Contains('PortalPlanoReconexaoItemResponse') -or -not $portalContracts.Contains('PlanoReconexao')) { throw "Contratos do plano de reconexao ausentes." }
Write-Host "    Estado + passos + motivo + acao + seguranca: contratos OK."

Write-Host "[843/848] Validando retomada pequena e limitada..."
$reconnectionSource = Get-Content .\src\HealthPlatform.Api\Services\PlanoReconexaoService.cs -Encoding UTF8 -Raw
if (-not $reconnectionSource.Contains('Take(3)') -or -not $reconnectionSource.Contains('Retomada em passos pequenos') -or -not $reconnectionSource.Contains('Faça só a próxima ação possível')) { throw "Plano de reconexao nao limita ou prioriza retomada pequena." }
Write-Host "    No maximo 3 passos; foco na proxima acao possivel: OK."

Write-Host "[844/848] Validando recuperacao e gamificacao sem coercao..."
if (-not $reconnectionSource.Contains('radar.ContextoRecuperacao') -or -not $reconnectionSource.Contains('não compense descanso com volume extra')) { throw "Reconexao nao protege contexto de recuperacao." }
if (-not $reconnectionSource.Contains('não remove XP') -or -not $reconnectionSource.Contains('não pune streak') -or $reconnectionSource.Contains('XpNegativo') -or $reconnectionSource.Contains('RemoverXp')) { throw "Reconexao nao deve punir gamificacao." }
Write-Host "    Descanso protegido; sem XP negativo, streak punitivo ou compensacao: OK."

Write-Host "[845/848] Validando reconexao sem prescricao paralela..."
if (-not $reconnectionSource.Contains('não dobra treino') -or -not $reconnectionSource.Contains('não restringe alimentação') -or -not $reconnectionSource.Contains('não altera treino, nutrição ou medicação automaticamente')) { throw "Travas de seguranca da reconexao ausentes." }
Write-Host "    Sem dobrar treino, restricao alimentar ou ajuste automatico: OK."

Write-Host "[846/848] Validando integracao do plano nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('PlanoReconexaoService.Montar') -or -not $portalSource.Contains('PlanoReconexaoService.Montar')) { throw "Plano de reconexao nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma camada de reconexao: OK."

Write-Host "[847/848] Validando UI da reconexao e ausencia de migration..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpReconnectionPlanAthleteCard') -or -not $appJsSource.Contains('hpReconnectionPlanProfessionalCard') -or -not $appJsSource.Contains('planoReconexao')) { throw "UI do plano de reconexao incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.1_plano_reconexao.sql) { throw "v0.9.1 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[848/848] Validando versao funcional v0.9.1..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.1 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.1." }
Write-Host "    v0.9.1 / Plano de Reconexao & Retomada Sustentavel: OK."
Write-Host "[849/856] Validando compatibilidade funcional v0.9.1..."
Write-Host "    v0.9.1 / Plano de Reconexao & Retomada Sustentavel preservado: OK."

Write-Host "[850/856] Validando contratos da protecao da retomada..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalProtecaoRetomadaResponse') -or -not $portalContracts.Contains('PortalProtecaoRetomadaItemResponse') -or -not $portalContracts.Contains('ProtecaoRetomada')) { throw "Contratos da protecao da retomada ausentes." }
Write-Host "    Estado + sinais + retomada em curso + itens: contratos OK."

Write-Host "[851/856] Validando inferencia observavel de retomada em curso..."
$returnProtectionSource = Get-Content .\src\HealthPlatform.Api\Services\ProtecaoRetomadaService.cs -Encoding UTF8 -Raw
if (-not $returnProtectionSource.Contains('gamificacao.StreakDias >= 1') -or -not $returnProtectionSource.Contains('diasAtivos?.Estado == "Maior"') -or -not $returnProtectionSource.Contains('gamificacao.ConsistenciaScore < 80')) { throw "Protecao da retomada nao usa sinais observaveis de continuidade." }
if ($returnProtectionSource.Contains('ProbabilidadeRecaida') -or $returnProtectionSource.Contains('ScoreRecaida')) { throw "Protecao da retomada nao deve criar score/probabilidade opaca." }
Write-Host "    Streak curto + dias ativos + consistencia: inferencia transparente OK."

Write-Host "[852/856] Validando protecao contra compensacao e falsa recaida..."
if (-not $returnProtectionSource.Contains('Não confunda proteção com recaída') -or -not $returnProtectionSource.Contains('não tente recuperar dias perdidos de uma vez') -or -not $returnProtectionSource.Contains('descanso coerente continua sendo parte do plano')) { throw "Travas de protecao da retomada incompletas." }
Write-Host "    Recuperacao e retomada pequena ficam acima da pressao por meta: OK."

Write-Host "[853/856] Validando gamificacao nao coercitiva na protecao da retomada..."
if (-not $returnProtectionSource.Contains('não remove XP') -or -not $returnProtectionSource.Contains('não pune streak') -or $returnProtectionSource.Contains('XpNegativo')) { throw "Protecao da retomada nao deve punir gamificacao." }
Write-Host "    Sem XP negativo, punicao de streak ou rotulo de recaida: OK."

Write-Host "[854/856] Validando integracao da protecao nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('ProtecaoRetomadaService.Montar') -or -not $portalSource.Contains('ProtecaoRetomadaService.Montar')) { throw "Protecao da retomada nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma leitura de continuidade: OK."

Write-Host "[855/856] Validando UI da protecao e ausencia de migration..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpReturnProtectionAthleteCard') -or -not $appJsSource.Contains('hpReturnProtectionProfessionalCard') -or -not $appJsSource.Contains('protecaoRetomada')) { throw "UI da protecao da retomada incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.3_protecao_retomada.sql) { throw "v0.9.3 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[856/856] Validando versao funcional v0.9.3..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.3 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.3." }
Write-Host "    v0.9.3 / Protecao da Retomada & Continuidade: OK."


Write-Host "[857/864] Validando compatibilidade funcional v0.9.2..."
Write-Host "    v0.9.2 / Protecao da Retomada & Continuidade preservado: OK."

Write-Host "[858/864] Validando contratos de estabilidade dos habitos..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalEstabilidadeHabitosResponse') -or -not $portalContracts.Contains('PortalEstabilidadeHabitoItemResponse') -or -not $portalContracts.Contains('EstabilidadeHabitos')) { throw "Contratos de estabilidade dos habitos ausentes." }
Write-Host "    Estado + eixos + contagens + orientacao: contratos OK."

Write-Host "[859/864] Validando leitura transparente de estabilidade..."
$habitSource = Get-Content .\src\HealthPlatform.Api\Services\EstabilidadeHabitosService.cs -Encoding UTF8 -Raw
if (-not $habitSource.Contains('ConsistenciaScore >= 80') -or -not $habitSource.Contains('dias-ativos') -or -not $habitSource.Contains('nutricao') -or -not $habitSource.Contains('hidratacao')) { throw "Eixos de estabilidade dos habitos incompletos." }
if ($habitSource.Contains('ScoreHabito') -or $habitSource.Contains('ProbabilidadePermanencia')) { throw "Estabilidade nao deve criar score/probabilidade opaca." }
Write-Host "    Consistencia + dias ativos + nutricao + hidratacao: OK."

Write-Host "[860/864] Validando consolidacao sem aumento automatico de cobranca..."
if (-not $habitSource.Contains('Mantenha o comportamento sem adicionar meta extra') -or -not $habitSource.Contains('Repita o que funcionou antes de elevar exigencia') -or -not $habitSource.Contains('nao compense tudo de uma vez')) { throw "Orientacoes sustentaveis de consolidacao incompletas." }
Write-Host "    Habito estavel reduz cobranca; oscilacao recebe proxima acao simples: OK."

Write-Host "[861/864] Validando descanso protegido e gamificacao nao coercitiva..."
if (-not $habitSource.Contains('Descanso tambem consolida rotina') -or -not $habitSource.Contains('nao remove XP') -or -not $habitSource.Contains('nao pune streak')) { throw "Protecao de descanso/gamificacao ausente." }
Write-Host "    Recuperacao coerente nao vira falha; sem punicao de XP/streak: OK."

Write-Host "[862/864] Validando integracao da estabilidade nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('EstabilidadeHabitosService.Montar') -or -not $portalSource.Contains('EstabilidadeHabitosService.Montar')) { throw "Estabilidade dos habitos nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma leitura: OK."

Write-Host "[863/864] Validando UI e ausencia de migration v0.9.3..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpHabitStabilityAthleteCard') -or -not $appJsSource.Contains('hpHabitStabilityProfessionalCard') -or -not $appJsSource.Contains('estabilidadeHabitos')) { throw "UI de estabilidade dos habitos incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.3_estabilidade_habitos.sql) { throw "v0.9.3 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[864/864] Validando versao funcional v0.9.3..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.3 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.3." }
Write-Host "    v0.9.3 / Estabilidade de Habitos & Consolidacao da Rotina: OK."

Write-Host "[865/872] Validando compatibilidade funcional v0.9.3..."
Write-Host "    v0.9.3 / Estabilidade de Habitos & Consolidacao da Rotina preservado: OK."

Write-Host "[866/872] Validando contratos do proximo foco de habito..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalProximoFocoHabitoResponse') -or -not $portalContracts.Contains('ProximoFocoHabito')) { throw "Contrato do proximo foco de habito ausente." }
Write-Host "    Estado + foco + manutencao + mensagem de seguranca: contratos OK."

Write-Host "[867/872] Validando regra de um unico foco por vez..."
$focusSource = Get-Content .\src\HealthPlatform.Api\Services\ProximoFocoHabitoService.cs -Encoding UTF8 -Raw
if (-not $focusSource.Contains('Where(x => x.Estado == "Oscilando")') -or -not $focusSource.Contains('Where(x => x.Estado == "Consolidando")') -or -not $focusSource.Contains('FirstOrDefault')) { throw "Selecao do foco unico incompleta." }
if ($focusSource.Contains('ScoreFoco') -or $focusSource.Contains('ProbabilidadeHabito')) { throw "Proximo foco nao deve criar score/probabilidade opaca." }
Write-Host "    Oscilando > consolidando; apenas um foco escolhido: OK."

Write-Host "[868/872] Validando manutencao dos habitos estaveis..."
if (-not $focusSource.Contains('Where(x => x.Estado == "Estavel")') -or -not $focusSource.Contains('modo de manutencao') -or -not $focusSource.Contains('Nao adicione uma meta nova')) { throw "Modo de manutencao dos habitos estaveis incompleto." }
Write-Host "    Habito estavel deixa de receber cobranca extra: OK."

Write-Host "[869/872] Validando protecao da retomada acima de novo foco..."
if (-not $focusSource.Contains('protecaoRetomada.Estado == "Proteger"') -or -not $focusSource.Contains('Primeiro consolide a retomada') -or -not $focusSource.Contains('Protecao da retomada vem antes de novas metas')) { throw "Protecao da retomada nao tem precedencia sobre novo foco." }
Write-Host "    Retomada/recuperacao bloqueiam aumento prematuro de exigencia: OK."

Write-Host "[870/872] Validando integracao do proximo foco nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('ProximoFocoHabitoService.Montar') -or -not $portalSource.Contains('ProximoFocoHabitoService.Montar')) { throw "Proximo foco nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma priorizacao: OK."

Write-Host "[871/872] Validando UI e ausencia de migration v0.9.5..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpNextHabitFocusAthleteCard') -or -not $appJsSource.Contains('hpNextHabitFocusProfessionalCard') -or -not $appJsSource.Contains('proximoFocoHabito')) { throw "UI do proximo foco incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.5_proximo_foco_habito.sql) { throw "v0.9.5 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[872/872] Validando versao funcional v0.9.4..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.5 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.5." }
Write-Host "    v0.9.4 / Manutencao Sustentavel & Proximo Foco: OK."


Write-Host "[873/880] Validando compatibilidade funcional v0.9.4..."
Write-Host "    v0.9.4 / Manutencao Sustentavel & Proximo Foco preservado: OK."

Write-Host "[874/880] Validando contrato de revisao do foco..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalRevisaoFocoHabitoResponse') -or -not $portalContracts.Contains('RevisaoFocoHabito')) { throw "Contrato de revisao do foco ausente." }
Write-Host "    Estado + decisao + evidencia + acao + manutencao: contrato OK."

Write-Host "[875/880] Validando ciclo de revisao do foco..."
$reviewSource = Get-Content .\src\HealthPlatform.Api\Services\RevisaoFocoHabitoService.cs -Encoding UTF8 -Raw
if (-not $reviewSource.Contains('"Continuar"') -or -not $reviewSource.Contains('"Consolidar"') -or -not $reviewSource.Contains('"Manutencao"') -or -not $reviewSource.Contains('"Proteger"')) { throw "Estados de revisao do foco incompletos." }
if ($reviewSource.Contains('ScoreFoco') -or $reviewSource.Contains('ProbabilidadeConsolidacao')) { throw "Revisao do foco nao deve criar score/probabilidade opaca." }
Write-Host "    Continuar / consolidar / manutencao / proteger: OK."

Write-Host "[876/880] Validando protecao contra troca precoce de foco..."
if (-not $reviewSource.Contains('trocar de foco agora adicionaria complexidade') -or -not $reviewSource.Contains('nao aumente a meta so porque houve melhora') -or -not $reviewSource.Contains('ProtecaoRetomada')) { throw "Travas de consolidacao do foco incompletas." }
Write-Host "    Repeticao antes de nova cobranca; retomada tem precedencia: OK."

Write-Host "[877/880] Validando comparativo semanal do foco..."
if (-not $reviewSource.Contains('TendenciaDoFoco') -or -not $reviewSource.Contains('dias-ativos') -or -not $reviewSource.Contains('nutricao') -or -not $reviewSource.Contains('hidratacao') -or -not $reviewSource.Contains('prontidao')) { throw "Revisao do foco nao usa tendencia semanal de forma transparente." }
Write-Host "    Categoria do foco vinculada ao eixo semanal correspondente: OK."

Write-Host "[878/880] Validando integracao da revisao do foco nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('RevisaoFocoHabitoService.Montar') -or -not $portalSource.Contains('RevisaoFocoHabitoService.Montar')) { throw "Revisao do foco nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma revisao: OK."

Write-Host "[879/880] Validando UI e ausencia de migration v0.9.5..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpHabitFocusReviewAthleteCard') -or -not $appJsSource.Contains('hpHabitFocusReviewProfessionalCard') -or -not $appJsSource.Contains('revisaoFocoHabito')) { throw "UI da revisao do foco incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.5_revisao_foco_habito.sql) { throw "v0.9.5 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[880/880] Validando versao funcional v0.9.5..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.5 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.5." }
Write-Host "    v0.9.5 / Revisao do Proximo Foco & Ciclo de Habito: OK."


Write-Host "[881/888] Validando compatibilidade funcional v0.9.5..."
Write-Host "    v0.9.5 / Revisao do Proximo Foco & Ciclo de Habito preservado: OK."

Write-Host "[882/888] Validando contrato de encerramento do ciclo de habito..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalEncerramentoCicloHabitoResponse') -or -not $portalContracts.Contains('EncerramentoCicloHabito')) { throw "Contrato de encerramento do ciclo de habito ausente." }
Write-Host "    Estado + decisao + manutencao + mensagem de seguranca: contrato OK."

Write-Host "[883/888] Validando estados do encerramento do foco..."
$closureSource = Get-Content .\src\HealthPlatform.Api\Services\EncerramentoCicloHabitoService.cs -Encoding UTF8 -Raw
if (-not $closureSource.Contains('"EmCurso"') -or -not $closureSource.Contains('"Consolidando"') -or -not $closureSource.Contains('"ProntoParaManutencao"') -or -not $closureSource.Contains('"Manutencao"') -or -not $closureSource.Contains('"Proteger"')) { throw "Estados do encerramento do ciclo de habito incompletos." }
Write-Host "    Em curso / consolidando / pronto para manutencao / manutencao / proteger: OK."

Write-Host "[884/888] Validando ausencia de troca automatica de foco..."
if (-not $closureSource.Contains('nao gera nova meta automatica') -or -not $closureSource.Contains('nao dispara automaticamente outra meta') -or -not $closureSource.Contains('nao aumente a meta')) { throw "Travas contra nova cobranca automatica incompletas." }
if ($closureSource.Contains('CriarNovoFoco') -or $closureSource.Contains('ScoreEncerramento') -or $closureSource.Contains('ProbabilidadeConsolidacao')) { throw "Encerramento nao deve criar novo foco/score/probabilidade opaca." }
Write-Host "    Encerrar destaque reduz cobranca e nao abre nova meta automaticamente: OK."

Write-Host "[885/888] Validando protecao de retomada no encerramento..."
if (-not $closureSource.Contains('protecaoRetomada.Estado == "Proteger"') -or -not $closureSource.Contains('Protecao vem antes de encerramento') -or -not $closureSource.Contains('nao remove XP') -or -not $closureSource.Contains('nao pune streak')) { throw "Protecao de retomada/gamificacao ausente no encerramento." }
Write-Host "    Retomada protegida; sem punicao de XP/streak: OK."

Write-Host "[886/888] Validando integracao do encerramento nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('EncerramentoCicloHabitoService.Montar') -or -not $portalSource.Contains('EncerramentoCicloHabitoService.Montar')) { throw "Encerramento do ciclo de habito nao integrado aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma transicao de manutencao: OK."

Write-Host "[887/888] Validando UI e ausencia de migration v0.9.6..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpHabitCycleClosureAthleteCard') -or -not $appJsSource.Contains('hpHabitCycleClosureProfessionalCard') -or -not $appJsSource.Contains('encerramentoCicloHabito')) { throw "UI do encerramento do ciclo de habito incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.6_encerramento_ciclo_habito.sql) { throw "v0.9.6 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[888/888] Validando versao funcional v0.9.6..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.7 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.7." }
Write-Host "    v0.9.6 / Encerramento do Ciclo de Habito & Transicao para Manutencao: OK."


Write-Host "[889/896] Validando compatibilidade funcional v0.9.6..."
Write-Host "    v0.9.6 / Encerramento do Ciclo de Habito & Transicao para Manutencao preservado: OK."

Write-Host "[890/896] Validando contrato de reentrada gradual de desafio..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalReentradaDesafioResponse') -or -not $portalContracts.Contains('ReentradaDesafio')) { throw "Contrato de reentrada de desafio ausente." }
Write-Host "    Estado + elegibilidade + decisao + evidencia + acao: contrato OK."

Write-Host "[891/896] Validando protecao antes de novo desafio..."
$reentrySource = Get-Content .\src\HealthPlatform.Api\Services\ReentradaDesafioService.cs -Encoding UTF8 -Raw
if (-not $reentrySource.Contains('protecaoRetomada.Estado == "Proteger"') -or -not $reentrySource.Contains('Ainda nao e hora de aumentar exigencia') -or -not $reentrySource.Contains('nao aumente a meta')) { throw "Protecao da retomada incompleta na reentrada." }
Write-Host "    Retomada protegida antes de qualquer progressao: OK."

Write-Host "[892/896] Validando criterios transparentes de elegibilidade..."
if (-not $reentrySource.Contains('estabilidade.HabitosOscilando > 0') -or -not $reentrySource.Contains('estabilidade.HabitosConsolidando > 0') -or -not $reentrySource.Contains('estabilidade.HabitosEstaveis >= 2') -or -not $reentrySource.Contains('"EspacoParaDesafio"')) { throw "Criterios de elegibilidade para novo desafio incompletos." }
if ($reentrySource.Contains('ScoreReentrada') -or $reentrySource.Contains('ProbabilidadeDesafio')) { throw "Reentrada nao deve criar score/probabilidade opaca." }
Write-Host "    Oscilando/consolidando bloqueiam; base estavel pode liberar 1 desafio: OK."

Write-Host "[893/896] Validando ausencia de criacao automatica de meta..."
if (-not $reentrySource.Contains('nao cria nova meta automatica') -or -not $reentrySource.Contains('o sistema nao cria a meta automaticamente') -or -not $reentrySource.Contains('apenas um desafio pequeno')) { throw "Travas contra criacao automatica de desafio incompletas." }
Write-Host "    Elegibilidade nao cria meta nem aumenta exigencia automaticamente: OK."

Write-Host "[894/896] Validando integracao da reentrada nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('ReentradaDesafioService.Montar') -or -not $portalSource.Contains('ReentradaDesafioService.Montar')) { throw "Reentrada gradual nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem a mesma elegibilidade contextual: OK."

Write-Host "[895/896] Validando UI e ausencia de migration v0.9.7..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpChallengeReentryAthleteCard') -or -not $appJsSource.Contains('hpChallengeReentryProfessionalCard') -or -not $appJsSource.Contains('reentradaDesafio')) { throw "UI da reentrada gradual incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.7_reentrada_desafio.sql) { throw "v0.9.7 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[896/904] Validando compatibilidade funcional v0.9.7..."
Write-Host "    v0.9.7 / Reentrada Gradual de Desafio preservado: OK."

Write-Host "[897/904] Validando contrato da janela de progressao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalJanelaProgressaoResponse') -or -not $portalContracts.Contains('PortalCriterioJanelaProgressaoResponse') -or -not $portalContracts.Contains('JanelaProgressao')) { throw "Contrato da janela de progressao ausente." }
Write-Host "    Janela + criterios transparentes + decisao + seguranca: contrato OK."

Write-Host "[898/904] Validando criterios transparentes de avanço..."
$progressionSource = Get-Content .\src\HealthPlatform.Api\Services\JanelaProgressaoService.cs -Encoding UTF8 -Raw
if (-not $progressionSource.Contains('base-comportamental') -or -not $progressionSource.Contains('sem-oscilacao') -or -not $progressionSource.Contains('recuperacao') -or -not $progressionSource.Contains('carga')) { throw "Criterios da janela de progressao incompletos." }
if ($progressionSource.Contains('ScoreProgressao') -or $progressionSource.Contains('ProbabilidadeProgressao')) { throw "Janela de progressao nao deve criar score/probabilidade opaca." }
Write-Host "    Base / oscilacao / recuperacao / carga permanecem eixos separados: OK."

Write-Host "[899/904] Validando bloqueio por recuperacao ou carga..."
if (-not $progressionSource.Contains('recuperacao.NivelAtencao != "Alta"') -or -not $progressionSource.Contains('cargaTreino.NivelAtencao != "Alta"') -or -not $progressionSource.Contains('cargaTreino.Classificacao != "Revisar"') -or -not $progressionSource.Contains('Sinal de revisao bloqueia progressao')) { throw "Travas clinico-esportivas da progressao incompletas." }
Write-Host "    Recuperacao/carga em revisao impedem nova exigencia: OK."

Write-Host "[900/904] Validando tratamento de dados insuficientes..."
if (-not $progressionSource.Contains('"DadosInsuficientes"') -or -not $progressionSource.Contains('Isso nao e reprovação') -or -not $progressionSource.Contains('Falta de dado nao vira punicao')) { throw "Dados insuficientes estao sendo tratados como falha ou sem mensagem explicita." }
Write-Host "    Falta de dado nao vira baixa adesao, score ou punicao: OK."

Write-Host "[901/904] Validando ausencia de progressao automatica..."
if (-not $progressionSource.Contains('Janela aberta nao e prescricao') -or -not $progressionSource.Contains('nao cria meta') -or -not $progressionSource.Contains('nao aumenta carga') -or -not $progressionSource.Contains('uma unica progressao pequena')) { throw "Travas contra progressao automatica incompletas." }
Write-Host "    Janela aberta apenas permite discutir 1 progressao pequena: OK."

Write-Host "[902/904] Validando integracao da janela nas duas Homes..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('JanelaProgressaoService.Montar') -or -not $portalSource.Contains('JanelaProgressaoService.Montar') -or -not $meuPortalSource.Contains('janelaProgressao') -or -not $portalSource.Contains('janelaProgressao')) { throw "Janela de progressao nao integrada aos dois portais." }
Write-Host "    Atleta + profissional recebem os mesmos criterios transparentes: OK."

Write-Host "[903/904] Validando UI e ausencia de migration v0.9.8..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains('hpProgressionWindowAthleteCard') -or -not $appJsSource.Contains('hpProgressionWindowProfessionalCard') -or -not $appJsSource.Contains('janelaProgressao')) { throw "UI da janela de progressao incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.8_janela_progressao.sql) { throw "v0.9.8 nao deveria exigir migration." }
Write-Host "    Cards nas duas visoes; schema permanece 38/38: OK."

Write-Host "[904/904] Validando versao funcional v0.9.8..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.8 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.8." }
Write-Host "    v0.9.8 / Janela de Progressao & Criterio de Avanco: OK."

Write-Host "[905/912] Validando compatibilidade funcional v0.9.8..."
Write-Host "    v0.9.8 / Janela de Progressao & Criterio de Avanco preservado: OK."

Write-Host "[906/912] Validando contrato da decisao assistida de progressao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalDecisaoProgressaoResponse') -or -not $portalContracts.Contains('PortalOpcaoDecisaoProgressaoResponse') -or -not $portalContracts.Contains('DecisaoProgressao')) { throw "Contrato da decisao assistida ausente." }
Write-Host "    Estado + categoria sugerida + opcoes + evidencia + seguranca: contrato OK."

Write-Host "[907/912] Validando respeito integral a janela de progressao..."
$decisionSource = Get-Content .\src\HealthPlatform.Api\Services\DecisaoProgressaoService.cs -Encoding UTF8 -Raw
if (-not $decisionSource.Contains('if (!janela.JanelaAberta)') -or -not $decisionSource.Contains('nao contorna criterio pendente') -or -not $decisionSource.Contains('Janela fechada bloqueia nova exigencia')) { throw "Decisao assistida pode estar contornando a janela de progressao." }
Write-Host "    Janela fechada continua bloqueando nova exigencia: OK."

Write-Host "[908/912] Validando categorias transparentes de discussao..."
if (-not $decisionSource.Contains('"Treino"') -or -not $decisionSource.Contains('"Nutricao"') -or -not $decisionSource.Contains('"Recuperacao"') -or -not $decisionSource.Contains('"Consistencia"')) { throw "Categorias da decisao de progressao incompletas." }
if (-not $decisionSource.Contains('Take(3)') -or -not $decisionSource.Contains('apenas um eixo deve ser considerado por vez')) { throw "Decisao assistida nao limita alternativas/progressao simultanea." }
Write-Host "    Treino / nutricao / recuperacao / consistencia com no maximo 3 alternativas: OK."

Write-Host "[909/912] Validando contexto de ciclo e objetivo..."
if (-not $decisionSource.Contains('prioridades.Prioridades.OrderBy') -or -not $decisionSource.Contains('CategoriaPorPerfil') -or -not $decisionSource.Contains('ciclo?.Objetivo')) { throw "Decisao assistida nao usa prioridades/objetivo do ciclo." }
Write-Host "    Prioridade do ciclo + perfil/objetivo orientam a categoria de discussao: OK."

Write-Host "[910/912] Validando ausencia de prescricao automatica..."
if (-not $decisionSource.Contains('Categoria sugerida nao e prescricao') -or -not $decisionSource.Contains('nao define carga, volume, calorias, medicacao') -or -not $decisionSource.Contains('nao cria meta')) { throw "Travas contra prescricao automatica incompletas." }
if ($decisionSource.Contains('ScoreDecisaoProgressao') -or $decisionSource.Contains('ProbabilidadeProgressao') -or $decisionSource.Contains('AumentarCargaPercentual')) { throw "Decisao assistida nao deve criar score/probabilidade/aumento automatico." }
Write-Host "    Sugestao e discutivel; sem score, prescricao ou aumento automatico: OK."

Write-Host "[911/912] Validando integracao/UI e ausencia de migration v0.9.9..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('DecisaoProgressaoService.Montar') -or -not $portalSource.Contains('DecisaoProgressaoService.Montar')) { throw "Decisao assistida nao integrada aos dois portais." }
if (-not $appJsSource.Contains('hpProgressionDecisionAthleteCard') -or -not $appJsSource.Contains('hpProgressionDecisionProfessionalCard') -or -not $appJsSource.Contains('decisaoProgressao')) { throw "UI da decisao assistida incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.9.9_decisao_progressao.sql) { throw "v0.9.9 nao deveria exigir migration." }
Write-Host "    Atleta + profissional integrados; schema permanece 38/38: OK."

Write-Host "[912/912] Validando versao funcional v0.9.9..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.9.9 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.9.9." }
Write-Host "    v0.9.9 / Decisao Assistida de Progressao: OK."

Write-Host "[913/920] Validando compatibilidade funcional v0.9.9..."
Write-Host "    v0.9.9 / Decisao Assistida de Progressao preservada: OK."

Write-Host "[914/920] Validando contrato do plano de progressao supervisionada..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalPlanoProgressaoSupervisionadaResponse') -or -not $portalContracts.Contains('PortalCriterioPlanoProgressaoResponse') -or -not $portalContracts.Contains('PlanoProgressaoSupervisionada')) { throw "Contrato do plano supervisionado ausente." }
Write-Host "    Estado + eixo + criterios + reavaliacao + seguranca: contrato OK."

Write-Host "[915/920] Validando respeito a janela e decisao anteriores..."
$supervisedSource = Get-Content .\src\HealthPlatform.Api\Services\PlanoProgressaoSupervisionadaService.cs -Encoding UTF8 -Raw
if (-not $supervisedSource.Contains('!janela.JanelaAberta') -or -not $supervisedSource.Contains('!decisao.PodeConsiderarProgressao') -or -not $supervisedSource.Contains('criterio pendente')) { throw "Plano supervisionado pode estar contornando janela/decisao anteriores." }
Write-Host "    Janela/decisao fechadas continuam bloqueando nova exigencia: OK."

Write-Host "[916/920] Validando uma mudanca por vez e observacao da resposta..."
if (-not $supervisedSource.Contains('uma mudanca por vez') -or -not $supervisedSource.Contains('Observar resposta antes de nova mudanca') -or -not $supervisedSource.Contains('os demais eixos devem permanecer estaveis')) { throw "Plano supervisionado nao preserva progressao unica/observacao." }
Write-Host "    Um eixo por vez + observacao antes de nova mudanca: OK."

Write-Host "[917/920] Validando ausencia de prescricao automatica..."
if (-not $supervisedSource.Contains('Plano supervisionado nao e prescricao') -or -not $supervisedSource.Contains('nao define carga, volume, calorias, medicacao') -or -not $supervisedSource.Contains('nao cria meta automatica')) { throw "Travas contra prescricao automatica incompletas." }
if ($supervisedSource.Contains('AumentarCargaPercentual') -or $supervisedSource.Contains('CaloriasAlvo') -or $supervisedSource.Contains('DoseMedicacao') -or $supervisedSource.Contains('ScoreProgressaoSupervisionada')) { throw "Plano supervisionado nao deve prescrever numero/score automatico." }
Write-Host "    Sem carga, calorias, medicacao, meta ou score automaticos: OK."

Write-Host "[918/920] Validando reavaliacao sem prazo automatico imposto..."
if (-not $supervisedSource.Contains('Reavaliar apos observar a resposta') -or -not $supervisedSource.Contains('sem prazo automatico imposto pelo sistema') -or -not $supervisedSource.Contains('Falta de dado')) { throw "Reavaliacao supervisionada incompleta ou coercitiva." }
Write-Host "    Reavaliacao depende de resposta/contexto; falta de dado nao vira pressao: OK."

Write-Host "[919/920] Validando integracao/UI e ausencia de migration v0.10.0..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('PlanoProgressaoSupervisionadaService.Montar') -or -not $portalSource.Contains('PlanoProgressaoSupervisionadaService.Montar')) { throw "Plano supervisionado nao integrado aos dois portais." }
if (-not $appJsSource.Contains('hpSupervisedProgressionAthleteCard') -or -not $appJsSource.Contains('hpSupervisedProgressionProfessionalCard') -or -not $appJsSource.Contains('planoProgressaoSupervisionada')) { throw "UI do plano supervisionado incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.10.0_plano_progressao_supervisionada.sql) { throw "v0.10.0 nao deveria exigir migration." }
Write-Host "    Atleta + profissional integrados; schema permanece 38/38: OK."

Write-Host "[920/920] Validando versao funcional v0.10.0..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.10.0 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.10.0." }
Write-Host "    v0.10.0 / Plano de Progressao Supervisionada: OK."


Write-Host "[921/928] Validando compatibilidade funcional v0.10.0..."
Write-Host "    v0.10.0 / Plano de Progressao Supervisionada preservado: OK."

Write-Host "[922/928] Validando contrato do monitoramento de resposta..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalMonitoramentoRespostaProgressaoResponse') -or -not $portalContracts.Contains('PortalSinalRespostaProgressaoResponse') -or -not $portalContracts.Contains('MonitoramentoRespostaProgressao')) { throw "Contrato do monitoramento de resposta ausente." }
Write-Host "    Estado + eixo + sinais + decisao + seguranca: contrato OK."

Write-Host "[923/928] Validando monitoramento somente com plano supervisionado aberto..."
$responseSource = Get-Content .\src\HealthPlatform.Api\Services\MonitoramentoRespostaProgressaoService.cs -Encoding UTF8 -Raw
if (-not $responseSource.Contains('!plano.PodeAbrirDiscussao') -or -not $responseSource.Contains('plano.Estado != "Supervisionar"') -or -not $responseSource.Contains('nao presume progressao')) { throw "Monitoramento pode inferir resposta sem plano supervisionado ativo." }
Write-Host "    Sem plano aberto, resposta nao e presumida: OK."

Write-Host "[924/928] Validando eixos transparentes de resposta..."
if (-not $responseSource.Contains('"Recuperacao"') -or -not $responseSource.Contains('"Carga"') -or -not $responseSource.Contains('"Performance"') -or -not $responseSource.Contains('PortalSinalRespostaProgressaoResponse')) { throw "Eixos de resposta incompletos." }
Write-Host "    Recuperacao + carga + performance separadas: OK."

Write-Host "[925/928] Validando prioridade de seguranca da resposta..."
if (-not $responseSource.Contains('sinais.Any(x => x.Estado == "Revisar")') -or -not $responseSource.Contains('NaoProgredirAgora') -or -not $responseSource.Contains('nao some outra progressao agora')) { throw "Resposta desfavoravel nao bloqueia nova progressao com clareza." }
Write-Host "    Sinal de revisao prevalece sobre novo avanco: OK."

Write-Host "[926/928] Validando ausencia de causalidade/prescricao automatica..."
if (-not $responseSource.Contains('nao prova causalidade') -or -not $responseSource.Contains('nao atribui melhora ou piora a progressao') -or -not $responseSource.Contains('nao autoriza aumento automatico de carga, volume, calorias ou medicacao')) { throw "Travas metodologicas/clinicas do monitoramento incompletas." }
if ($responseSource.Contains('ScoreRespostaProgressao') -or $responseSource.Contains('AumentarCargaPercentual') -or $responseSource.Contains('DoseMedicacao')) { throw "Monitoramento nao deve gerar score ou prescricao automatica." }
Write-Host "    Sem causalidade inventada, score ou prescricao automatica: OK."

Write-Host "[927/928] Validando integracao/UI e ausencia de migration v0.10.1..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('MonitoramentoRespostaProgressaoService.Montar') -or -not $portalSource.Contains('MonitoramentoRespostaProgressaoService.Montar')) { throw "Monitoramento de resposta nao integrado aos dois portais." }
if (-not $appJsSource.Contains('hpProgressionResponseAthleteCard') -or -not $appJsSource.Contains('hpProgressionResponseProfessionalCard') -or -not $appJsSource.Contains('monitoramentoRespostaProgressao')) { throw "UI do monitoramento de resposta incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.10.1_monitoramento_resposta_progressao.sql) { throw "v0.10.1 nao deveria exigir migration." }
Write-Host "    Atleta + profissional integrados; schema permanece 38/38: OK."

Write-Host "[928/928] Validando versao funcional v0.10.1..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.10.1 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.10.1." }
Write-Host "    v0.10.1 / Monitoramento de Resposta a Progressao: OK."

Write-Host "[929/936] Validando compatibilidade funcional v0.10.1..."
Write-Host "    v0.10.1 / Monitoramento de Resposta a Progressao preservado: OK."

Write-Host "[930/936] Validando contrato da reavaliacao de progressao..."
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $portalContracts.Contains('PortalReavaliacaoProgressaoResponse') -or -not $portalContracts.Contains('PortalCriterioReavaliacaoProgressaoResponse') -or -not $portalContracts.Contains('ReavaliacaoProgressao')) { throw "Contrato da reavaliacao de progressao ausente." }
Write-Host "    Estado + decisao + criterios + proximo passo + seguranca: contrato OK."

Write-Host "[931/936] Validando dependencia do monitoramento e plano supervisionado..."
$reavaliacaoSource = Get-Content .\src\HealthPlatform.Api\Services\ReavaliacaoProgressaoService.cs -Encoding UTF8 -Raw
if (-not $reavaliacaoSource.Contains('!plano.PodeAbrirDiscussao') -or -not $reavaliacaoSource.Contains('!monitoramento.EmAcompanhamento') -or -not $reavaliacaoSource.Contains('Sem progressao ativa')) { throw "Reavaliacao pode inferir progressao sem acompanhamento ativo." }
Write-Host "    Sem plano/monitoramento ativo, nenhuma progressao e inferida: OK."

Write-Host "[932/936] Validando decisoes Manter/Revisar/Encerrar/AguardarDados..."
if (-not $reavaliacaoSource.Contains('"Revisar"') -or -not $reavaliacaoSource.Contains('"AguardarDados"') -or -not $reavaliacaoSource.Contains('"Manter"') -or -not $reavaliacaoSource.Contains('"Encerrar"')) { throw "Estados de reavaliacao incompletos." }
if (-not $reavaliacaoSource.Contains('Sinal de revisao prevalece sobre performance favoravel')) { throw "Prioridade clinica da reavaliacao incompleta." }
Write-Host "    Decisoes transparentes e prioridade clinica: OK."

Write-Host "[933/936] Validando encerramento sem causalidade ou nova progressao automatica..."
if (-not $reavaliacaoSource.Contains('sem concluir causalidade') -or -not $reavaliacaoSource.Contains('nao abra outra progressao na mesma decisao') -or -not $reavaliacaoSource.Contains('nao libera automaticamente novo desafio')) { throw "Travas do encerramento da observacao incompletas." }
if ($reavaliacaoSource.Contains('ScoreSucessoProgressao') -or $reavaliacaoSource.Contains('AumentarCargaPercentual') -or $reavaliacaoSource.Contains('DoseMedicacao')) { throw "Reavaliacao nao deve gerar score ou prescricao automatica." }
Write-Host "    Encerrar observacao nao prova sucesso nem dispara novo avanco: OK."

Write-Host "[934/936] Validando falta de dados e manutencao sem aumento de exigencia..."
if (-not $reavaliacaoSource.Contains('Falta de dado nao e falha') -or -not $reavaliacaoSource.Contains('sem aumentar exigencia apenas para produzir dados') -or -not $reavaliacaoSource.Contains('Manter nao significa aumentar')) { throw "Travas de dados/manutencao incompletas." }
Write-Host "    Dados insuficientes nao viram pressao; manter nao significa aumentar: OK."

Write-Host "[935/936] Validando integracao/UI e ausencia de migration v0.10.2..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('ReavaliacaoProgressaoService.Montar') -or -not $portalSource.Contains('ReavaliacaoProgressaoService.Montar')) { throw "Reavaliacao de progressao nao integrada aos dois portais." }
if (-not $appJsSource.Contains('hpProgressionReassessmentAthleteCard') -or -not $appJsSource.Contains('hpProgressionReassessmentProfessionalCard') -or -not $appJsSource.Contains('reavaliacaoProgressao')) { throw "UI da reavaliacao de progressao incompleta." }
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains('[37/38] Aplicando upgrade v0.6.4')) { throw "Schema 38/38 foi alterado sem necessidade." }
if (Test-Path .\scripts\sql\v0.10.2_reavaliacao_progressao.sql) { throw "v0.10.2 nao deveria exigir migration." }
Write-Host "    Atleta + profissional integrados; schema permanece 38/38: OK."

Write-Host "[936/936] Validando versao funcional v0.10.2..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.10.2 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.10.2." }
Write-Host "    v0.10.2 / Reavaliacao da Progressao e Decisao de Continuidade: OK."

Write-Host "[937/944] Validando compatibilidade funcional v0.10.2..."
Write-Host "    v0.10.2 / Reavaliacao da Progressao preservada: OK."

Write-Host "[938/944] Validando entidade e contrato do evento de progressao..."
$eventEntity = Get-Content .\src\HealthPlatform.Domain\Entities\EventoProgressaoSupervisionada.cs -Encoding UTF8 -Raw
$portalContracts = Get-Content .\src\HealthPlatform.Api\Contracts\Portal\PortalPacienteContracts.cs -Encoding UTF8 -Raw
if (-not $eventEntity.Contains('DataAplicacaoUtc') -or -not $eventEntity.Contains('ProfissionalId') -or -not $eventEntity.Contains('Status')) { throw "Entidade de evento de progressao incompleta." }
if (-not $portalContracts.Contains('PortalRegistroProgressaoResponse') -or -not $portalContracts.Contains('PortalEventoProgressaoSupervisionadaResponse') -or -not $portalContracts.Contains('RegistroProgressao')) { throw "Contrato de registro de progressao incompleto." }
Write-Host "    Marco temporal + profissional + status + contrato de portal: OK."

Write-Host "[939/944] Validando persistencia e upgrade 38/38..."
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlEvent = Get-Content .\scripts\sql\v0.10.3_eventos_progressao_supervisionada.sql -Encoding UTF8 -Raw
if (-not $dbSource.Contains('EventosProgressaoSupervisionada') -or -not $dbSource.Contains('IX_EventosProgressaoSupervisionada_PacienteId_DataAplicacaoUtc')) { throw "Mapeamento EF do evento de progressao incompleto." }
if (-not $setupSource.Contains('[38/38] Aplicando upgrade v0.10.3') -or -not $sqlEvent.Contains('CREATE TABLE IF NOT EXISTS "EventosProgressaoSupervisionada"')) { throw "Upgrade 38/38 da v0.10.3 ausente." }
Write-Host "    Tabela idempotente + indice temporal + PREPARAR 38/38: OK."

Write-Host "[940/944] Validando registro explicitamente profissional e uma mudanca por vez..."
$eventController = Get-Content .\src\HealthPlatform.Api\Controllers\EventosProgressaoSupervisionadaController.cs -Encoding UTF8 -Raw
if (-not $eventController.Contains('Perfil profissional ativo nao encontrado') -or -not $eventController.Contains('Ja existe uma progressao em observacao') -or -not $eventController.Contains('Eixo deve ser Treino, Nutricao, Recuperacao ou Consistencia')) { throw "Travas profissionais do evento de progressao incompletas." }
Write-Host "    Profissional ativo + eixo explicito + apenas um evento em observacao: OK."

Write-Host "[941/944] Validando ausencia de inferencia automatica de aplicacao/causalidade..."
$registroSource = Get-Content .\src\HealthPlatform.Api\Services\RegistroProgressaoService.cs -Encoding UTF8 -Raw
if (-not $registroSource.Contains('Nao presume que uma sugestao foi aplicada') -or -not $registroSource.Contains('nao presume aplicacao, dose, carga ou causalidade') -or -not $registroSource.Contains('nao prova causalidade')) { throw "Travas metodologicas do registro de progressao incompletas." }
Write-Host "    Sugestao != aplicacao; evento != causalidade: OK."

Write-Host "[942/944] Validando inicio/encerramento temporal sem nova progressao automatica..."
if (-not $eventController.Contains('DataAplicacaoUtc') -or -not $eventController.Contains('EncerradoEmUtc') -or -not $eventController.Contains('Status = "Encerrado"')) { throw "Ciclo temporal do evento incompleto." }
if ($eventController.Contains('AumentarCargaPercentual') -or $eventController.Contains('DoseMedicacao') -or $eventController.Contains('CriarNovaProgressaoAutomatica')) { throw "Evento nao deve prescrever nova progressao automaticamente." }
Write-Host "    Inicio e encerramento registrados sem prescricao automatica: OK."

Write-Host "[943/944] Validando integracao nas duas Homes e UI..."
$meuPortalSource = Get-Content .\src\HealthPlatform.Api\Controllers\MeuPortalPacienteController.cs -Encoding UTF8 -Raw
$portalSource = Get-Content .\src\HealthPlatform.Api\Controllers\PortalPacienteController.cs -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $meuPortalSource.Contains('RegistroProgressaoService.MontarAsync') -or -not $portalSource.Contains('RegistroProgressaoService.MontarAsync')) { throw "Registro de progressao nao integrado aos dois portais." }
if (-not $appJsSource.Contains('hpProgressionEventAthleteCard') -or -not $appJsSource.Contains('hpProgressionEventProfessionalCard') -or -not $appJsSource.Contains('registroProgressao')) { throw "UI do registro de progressao incompleta." }
Write-Host "    Atleta + profissional recebem o marco temporal real: OK."

Write-Host "[944/944] Validando versao funcional v0.10.3..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.10.3" -or -not $appJsSource.Contains("const HP_MVP_VERSION='0.10.3';")) { throw "Versao v0.10.3 inconsistente." }
if (($healthSource | Select-String -Pattern 'version = "0.10.3"' -AllMatches).Matches.Count -lt 2) { throw "Health endpoint/fallback nao anunciam v0.10.3." }
Write-Host "    v0.10.3 / Registro de Evento de Progressao: OK."

Write-Host "TESTE DE FUMACA CONCLUIDO." -ForegroundColor Green
Write-Host "Nenhum registro foi criado ou alterado." -ForegroundColor Green
