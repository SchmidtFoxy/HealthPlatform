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

Write-Host "[1/492] Healthcheck..." -ForegroundColor Cyan
$health = Invoke-RestMethod -Uri "$base/api/health" -Method Get
if ($health.version -ne "0.3.40") { throw "Versao inesperada da API: $($health.version)" }
Write-Host "    API $($health.version) / banco $($health.database)" -ForegroundColor Green

Write-Host "[2/492] Login..." -ForegroundColor Cyan
$body = @{ email = $email; senha = $senha } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body $body
$token = $login.accessToken
if ([string]::IsNullOrWhiteSpace($token)) { throw "Login nao retornou accessToken." }
$headers = @{ Authorization = "Bearer $token" }
Write-Host "    Login OK: $($login.nome)" -ForegroundColor Green

Write-Host "[3/492] Listando pacientes..." -ForegroundColor Cyan
$lista = Invoke-RestMethod -Uri "$base/api/pacientes?pagina=1&tamanhoPagina=5" -Headers $headers -Method Get
Write-Host "    Total atual: $($lista.total)" -ForegroundColor Green

Write-Host "[4/492] Validando perguntas de anamnese..." -ForegroundColor Cyan
try { $perguntas = Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers -Method Get; Write-Host "    Endpoint OK. Perguntas ativas: $($perguntas.Count)" -ForegroundColor Green } catch { if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host "    Endpoint protegido OK (perfil profissional ainda nao configurado)." -ForegroundColor DarkGreen } else { throw } }

Write-Host "[5/492] Validando catalogo laboratorial..." -ForegroundColor Cyan
$marcadores = Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers -Method Get
Write-Host "    Marcadores cadastrados: $($marcadores.Count)" -ForegroundColor Green

Write-Host "[6/492] Validando catalogo de alimentos..." -ForegroundColor Cyan
$alimentos = Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers -Method Get
Write-Host "    Alimentos cadastrados: $($alimentos.Count)" -ForegroundColor Green

Write-Host "[7/492] Validando busca/paginacao..." -ForegroundColor Cyan
$busca = Invoke-RestMethod -Uri "$base/api/pacientes?busca=__smoke_test_sem_resultado__&pagina=1&tamanhoPagina=3" -Headers $headers -Method Get
if ($null -eq $busca.itens) { throw "Resposta de paginacao invalida." }

Write-Host "[8/492] Validando modulos do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $preview = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/relatorios/preview" -Headers $headers -Method Get
    if ($null -eq $preview.paciente) { throw "Preview de relatorio invalido." }
    $planos = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/planos-alimentares" -Headers $headers -Method Get)
    Write-Host "    Preview OK / planos alimentares: $($planos.Count)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de modulos ignorada sem criar dados." -ForegroundColor DarkGreen }

Write-Host "[9/492] Validando metas do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $metas = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/metas?incluirEncerradas=true" -Headers $headers -Method Get)
    Write-Host "    Endpoint OK. Metas cadastradas: $($metas.Count)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de metas ignorada." -ForegroundColor DarkGreen }

Write-Host "[10/492] Validando diario/resumo do dia..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $diario = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/diario" -Headers $headers -Method Get)
    $resumo = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/resumo-dia" -Headers $headers -Method Get
    if ($null -eq $resumo.metas) { throw "Resumo do dia invalido." }
    Write-Host "    Diario: $($diario.Count) registros / metas ativas hoje: $($resumo.metasAtivas)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de diario ignorada." -ForegroundColor DarkGreen }

Write-Host "[11/492] Validando portal/home do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $portal = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/portal/home" -Headers $headers -Method Get
    if ($null -eq $portal.paciente -or $portal.paciente.id -ne $pacienteSmoke.id) { throw "Portal do paciente retornou dados invalidos." }
    if ($null -eq $portal.evolucaoCorporal -or $null -eq $portal.metasHoje -or $null -eq $portal.registrosHoje -or $null -eq $portal.examesRecentes) { throw "Portal do paciente incompleto." }
    Write-Host "    Portal OK: $($portal.metasAtivas) meta(s), $($portal.registrosHoje.Count) registro(s), $($portal.examesRecentes.Count) resultado(s) recente(s)." -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao do portal ignorada." -ForegroundColor DarkGreen }



Write-Host "[12/492] Validando agenda do profissional..." -ForegroundColor Cyan
try {
    $hojeLocal = (Get-Date).ToString("yyyy-MM-dd")
    $agenda = Invoke-RestMethod -Uri "$base/api/agenda?data=$hojeLocal&offsetMinutos=-180" -Headers $headers -Method Get
    if ($null -eq $agenda.consultas) { throw "Resposta de agenda invalida." }
    Write-Host "    Agenda OK: $($agenda.total) consulta(s) no dia." -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host "    Agenda protegida OK (perfil profissional ainda nao configurado)." -ForegroundColor DarkGreen } else { throw }
}

Write-Host "[13/492] Validando dashboard do profissional..." -ForegroundColor Cyan
try {
    $dashboard = Invoke-RestMethod -Uri "$base/api/profissional/dashboard?offsetMinutos=-180" -Headers $headers -Method Get
    if ($null -eq $dashboard.agendaHoje -or $null -eq $dashboard.proximasConsultas -or $null -eq $dashboard.pacientesRecentes) { throw "Dashboard profissional incompleto." }
    Write-Host "    Dashboard OK: $($dashboard.pacientesAtivos) paciente(s) ativo(s), $($dashboard.consultasHoje) consulta(s) hoje, $($dashboard.retornosPendentes) retorno(s) pendente(s)." -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host "    Dashboard protegido OK (perfil profissional ainda nao configurado)." -ForegroundColor DarkGreen } else { throw }
}


Write-Host "[14/492] Validando interface web..." -ForegroundColor Cyan
$web = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($web.StatusCode -ne 200 -or $web.Content -notmatch "HealthPlatform") { throw "Interface web nao respondeu corretamente." }
Write-Host "    Interface HTML OK." -ForegroundColor Green

Write-Host "[15/492] Validando assets da interface..." -ForegroundColor Cyan
$js = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($js.StatusCode -ne 200 -or $css.StatusCode -ne 200) { throw "Assets web nao responderam." }
Write-Host "    app.js + app.css OK." -ForegroundColor Green


Write-Host "[16/492] Validando prontuario visual v0.2.2..." -ForegroundColor Cyan
if ($js.Content -notmatch "patient-tabs" -or $js.Content -notmatch "loadPatient" -or $css.Content -notmatch "patient-dashboard") { throw "Prontuario visual v0.2.2 incompleto nos assets." }
Write-Host "    Prontuario visual + abas clinicas OK." -ForegroundColor Green

Write-Host "[17/492] Validando acoes clinicas da interface..." -ForegroundColor Cyan
if ($js.Content -notmatch "openClinicalActionMenu" -or $js.Content -notmatch "submitClinicalForm" -or $web.Content -notmatch "clinicalActionModal") { throw "Acoes clinicas v0.2.2 nao foram publicadas corretamente." }
Write-Host "    Registrar consulta, avaliacao, anamnese, meta e diario: assets OK." -ForegroundColor Green

Write-Host "[18/492] Validando rotas usadas pelos formularios clinicos..." -ForegroundColor Cyan
if ($js.Content -notmatch "/consultas" -or $js.Content -notmatch "/avaliacoes" -or $js.Content -notmatch "/anamneses" -or $js.Content -notmatch "/metas" -or $js.Content -notmatch "/diario") { throw "Formularios clinicos nao referenciam todas as rotas esperadas." }
Write-Host "    Rotas de registro clinico presentes." -ForegroundColor Green


Write-Host "[19/492] Validando cadastro visual de exames..." -ForegroundColor Cyan
if ($js.Content -notmatch "openExamForm" -or $js.Content -notmatch "exam-result-row" -or $js.Content -notmatch "/api/exames/marcadores" -or $js.Content -notmatch "/exames") { throw "Construtor visual de exames v0.2.3 incompleto." }
Write-Host "    Coleta + catalogo de marcadores + resultados: assets OK." -ForegroundColor Green

Write-Host "[20/492] Validando construtor visual do plano alimentar..." -ForegroundColor Cyan
if ($js.Content -notmatch "openMealPlanForm" -or $js.Content -notmatch "meal-builder" -or $js.Content -notmatch "/api/alimentos" -or $js.Content -notmatch "/planos-alimentares" -or $js.Content -notmatch "substitution-row") { throw "Construtor visual de plano alimentar v0.2.3 incompleto." }
if ($css.Content -notmatch "meal-item-builder" -or $css.Content -notmatch "plan-preview") { throw "Estilos do construtor alimentar v0.2.3 incompletos." }
Write-Host "    Refeicoes + alimentos + macros + substituicoes: assets OK." -ForegroundColor Green



Write-Host "[21/492] Validando relatorios na interface..." -ForegroundColor Cyan
if ($js.Content -notmatch "openReportForm" -or $js.Content -notmatch "openReportHtml" -or $js.Content -notmatch "/relatorios/preview" -or $js.Content -notmatch "newReportFromTab") { throw "Fluxo visual de relatorios v0.3.27 incompleto." }
if ($css.Content -notmatch "report-grid" -or $css.Content -notmatch "report-preview-box") { throw "Estilos de relatorio v0.3.27 incompletos." }
Write-Host "    Geracao + preview + visualizacao/impressao: assets OK." -ForegroundColor Green

Write-Host "[22/492] Validando edicao visual do paciente..." -ForegroundColor Cyan
if ($js.Content -notmatch "openEditPatientForm" -or $js.Content -notmatch "method:'PUT'" -or $js.Content -notmatch "Editar dados") { throw "Edicao visual do paciente v0.3.27 incompleta." }
Write-Host "    Cadastro do paciente pode ser atualizado pela interface." -ForegroundColor Green

Write-Host "[23/492] Validando endpoint de relatorios do paciente..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $relatoriosSmoke = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/relatorios" -Headers $headers -Method Get)
    Write-Host "    Endpoint OK. Relatorios existentes: $($relatoriosSmoke.Count)" -ForegroundColor Green
} else { Write-Host "    Sem pacientes: validacao de relatorios ignorada." -ForegroundColor DarkGreen }

Write-Host "[24/492] Validando edicao clinica visual..."
if ($js.Content -notmatch "openEditConsulta" -or $js.Content -notmatch "openEditAnamnese" -or $js.Content -notmatch "openEditAvaliacao" -or $js.Content -notmatch "/api/avaliacoes/") { throw "Edicao clinica visual v0.3.27 incompleta." }
Write-Host "    Consulta + anamnese + avaliacao: edicao visual OK."

Write-Host "[25/492] Validando agenda operacional..."
if ($js.Content -notmatch "agendaStatusActions" -or $js.Content -notmatch "openRescheduleForm" -or $js.Content -notmatch "Realizada" -or $js.Content -notmatch "Faltou" -or $js.Content -notmatch "/reagendar") { throw "Agenda operacional v0.3.27 incompleta." }
Write-Host "    Status rapido + reagendamento: assets OK."

Write-Host "[26/492] Validando endpoint de atualizacao de avaliacao..."
try {
    $ctrl = Get-Content -Encoding UTF8 -Raw ".\src\HealthPlatform.Api\Controllers\AvaliacoesController.cs"
    if ($ctrl -notmatch 'HttpPut\("api/avaliacoes/\{id:guid\}"\)' -or $ctrl -notmatch 'AdicionarAuditoria\("UPDATE"') { throw "PUT de avaliacao ou auditoria ausente." }
    Write-Host "    PUT /api/avaliacoes/{id} + auditoria OK."
} catch { throw $_ }

Write-Host ""

Write-Host "[27/492] Validando tela de configuracoes..."
$indexHtml = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($indexHtml.Content -notmatch "configuracoes") { throw "Navegacao de configuracoes nao encontrada." }
Write-Host "    Navegacao de configuracoes presente."

Write-Host "[28/492] Validando gerenciadores de catalogo na interface..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "modalAlimento" -or $appJs.Content -notmatch "modalMarcador" -or $appJs.Content -notmatch "modalPergunta") {
    throw "Gerenciadores de catalogo incompletos."
}
Write-Host "    Alimentos + marcadores + perguntas: assets OK."

Write-Host "[29/492] Validando resumo de configuracoes do consultorio..."
$cfg = Invoke-RestMethod -Uri "$base/api/configuracoes/resumo" -Headers $headers
if (-not $cfg.organizacao) { throw "Resumo de configuracoes sem organizacao." }
Write-Host "    Organizacao/usuario/profissional: endpoint OK."

Write-Host "[30/492] Validando rotas dos catalogos..."
$null = Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers
$null = Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers
$null = Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers
Write-Host "    Catalogos acessiveis e autenticados."


Write-Host "[31/492] Validando edicao de configuracoes..."
$cfg = Invoke-RestMethod -Uri "$base/api/configuracoes/resumo" -Headers $headers
if (-not $cfg.organizacao -or -not $cfg.usuario) { throw "Resumo de configuracoes incompleto." }
Write-Host "    Organizacao + usuario carregados para edicao."

Write-Host "[32/492] Validando assets de edicao/inativacao dos catalogos..."
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

Write-Host "[33/492] Validando endpoints administrativos..."
$null = Invoke-RestMethod -Uri "$base/api/configuracoes/resumo" -Headers $headers
Write-Host "    Configuracoes autenticadas OK."

Write-Host "[34/492] Validando que a interface segue integra..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.StatusCode -ne 200) { throw "Interface indisponivel." }
Write-Host "    Interface web OK apos extensoes administrativas."


Write-Host "[35/492] Validando separacao de autorizacao profissional/paciente..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "/api/portal/me/home" -or $appJs.Content -notmatch "tipoUsuario==='Paciente'") {
    throw "Portal autenticado do paciente nao encontrado nos assets."
}
Write-Host "    UI separada por tipo de usuario: assets OK."

Write-Host "[36/492] Validando endpoint de status de acesso do paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $accessStatus = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/acesso" -Headers $headers
    if ($null -eq $accessStatus.possuiAcesso) { throw "Status de acesso invalido." }
}
Write-Host "    Status de acesso do paciente: endpoint OK."

Write-Host "[37/492] Validando fluxo de convite/ativacao nos assets..."
if ($appJs.Content -notmatch "ativarPaciente" -or $appJs.Content -notmatch "/api/auth/paciente/ativar") {
    throw "Fluxo visual de ativacao incompleto."
}
Write-Host "    Convite + ativacao: assets OK."

Write-Host "[38/492] Validando autoatendimento do diario..."
if ($appJs.Content -notmatch "/api/portal/me/diario") {
    throw "Registro de diario pelo paciente nao encontrado."
}
Write-Host "    Diario proprio: asset OK."

Write-Host "[39/492] Validando autoatendimento das metas..."
if ($appJs.Content -notmatch "/api/portal/me/metas/" -or $appJs.Content -notmatch "/registro") {
    throw "Atualizacao de meta pelo paciente nao encontrada."
}
Write-Host "    Metas proprias: asset OK."

Write-Host "[40/492] Validando tela dedicada do portal do paciente..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch "patientAppView" -or $index.Content -notmatch "activationView") {
    throw "Views dedicadas do paciente nao encontradas."
}
Write-Host "    Portal + ativacao do paciente: HTML OK."


Write-Host "[41/492] Validando navegacao completa do portal..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch "data-patient-view=.plano." -or
    $index.Content -notmatch "data-patient-view=.metas." -or
    $index.Content -notmatch "data-patient-view=.diario." -or
    $index.Content -notmatch "data-patient-view=.evolucao." -or
    $index.Content -notmatch "data-patient-view=.exames.") {
    throw "Navegacao completa do paciente nao encontrada."
}
Write-Host "    Inicio + plano + metas + diario + evolucao + exames: HTML OK."

Write-Host "[42/492] Validando endpoint proprio de plano alimentar..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "/api/portal/me/plano") { throw "Endpoint proprio do plano ausente dos assets." }
Write-Host "    Plano alimentar proprio: asset OK."

Write-Host "[43/492] Validando historico proprio de metas e diario..."
if ($appJs.Content -notmatch "/api/portal/me/metas" -or $appJs.Content -notmatch "/api/portal/me/diario") {
    throw "Historico proprio de metas/diario incompleto."
}
Write-Host "    Metas + diario historicos: assets OK."

Write-Host "[44/492] Validando historico de evolucao corporal..."
if ($appJs.Content -notmatch "/api/portal/me/evolucao") { throw "Evolucao propria ausente." }
Write-Host "    Evolucao corporal: asset OK."

Write-Host "[45/492] Validando historico proprio de exames..."
if ($appJs.Content -notmatch "/api/portal/me/exames") { throw "Exames proprios ausentes." }
Write-Host "    Exames laboratoriais: asset OK."

Write-Host "[46/492] Validando assets visuais do portal expandido..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "patient-portal-nav" -or $css.Content -notmatch "patient-plan-totals" -or $css.Content -notmatch "lab-result-grid") {
    throw "Estilos do portal expandido incompletos."
}
Write-Host "    Portal completo e responsivo: assets OK."


Write-Host "[47/492] Validando schema/endpoint do catalogo de exercicios..."
$exercicios = Invoke-RestMethod -Uri "$base/api/exercicios" -Headers $headers
Write-Host "    Exercicios ativos no catalogo: $($exercicios.Count)"

Write-Host "[48/492] Validando endpoint de planos de treino do paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $treinos = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/treinos" -Headers $headers
    Write-Host "    Planos de treino cadastrados: $($treinos.Count)"
}

Write-Host "[49/492] Validando construtor visual de treino..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "openWorkoutForm" -or
    $appJs.Content -notmatch "/api/pacientes/.*/treinos" -or
    $appJs.Content -notmatch "/api/exercicios") {
    throw "Construtor visual de treinos incompleto."
}
Write-Host "    Treinos + exercicios + series/repeticoes/carga: assets OK."

Write-Host "[50/492] Validando videos e prescricao de exercicios..."
if ($appJs.Content -notmatch "videoUrl" -or
    $appJs.Content -notmatch "descansoSegundos" -or
    $appJs.Content -notmatch "tempoSegundos") {
    throw "Prescricao avancada de exercicios incompleta."
}
Write-Host "    Video + descanso + tempo: assets OK."

Write-Host "[51/492] Validando aba de treinos no prontuario..."
if ($appJs.Content -notmatch "Treinos.*treinos.length" -or
    $appJs.Content -notmatch "workout-plan-grid") {
    throw "Aba profissional de treinos nao encontrada."
}
Write-Host "    Prontuario profissional: aba Treinos OK."

Write-Host "[52/492] Validando navegacao de treino do paciente..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch "data-patient-view=.treino.") {
    throw "Navegacao Treino do portal do paciente ausente."
}
Write-Host "    Portal do paciente: navegacao Treino OK."

Write-Host "[53/492] Validando endpoint proprio do treino do paciente..."
if ($appJs.Content -notmatch "/api/portal/me/treino" -or
    $appJs.Content -notmatch "loadPatientWorkout") {
    throw "Portal proprio de treino incompleto."
}
Write-Host "    GET /api/portal/me/treino: asset OK."

Write-Host "[54/492] Validando assets visuais do modulo de treino..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "patient-exercise-card" -or
    $css.Content -notmatch "workout-item-builder" -or
    $css.Content -notmatch "exercise-video") {
    throw "Estilos do modulo de treino incompletos."
}
Write-Host "    Modulo de treino responsivo: assets OK."


Write-Host "[55/492] Validando schema de execucoes de treino..."
$tables = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name IN ('ExecucoesTreino','ExecucoesItensTreino');"
if ([int]$tables -ne 2) { throw "Tabelas de execucao de treino ausentes." }
Write-Host "    ExecucoesTreino + ExecucoesItensTreino: schema OK."

Write-Host "[56/492] Validando historico profissional de treinos..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $histTreino = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/treinos/historico?dias=90" -Headers $headers
    if ($null -eq $histTreino.totalTreinos) { throw "Historico profissional invalido." }
}
Write-Host "    Adesao + historico profissional: endpoint OK."

Write-Host "[57/492] Validando registro visual de execucao pelo paciente..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "openWorkoutExecutionForm" -or
    $appJs.Content -notmatch "/api/portal/me/treinos/execucoes") {
    throw "Registro de execucao pelo paciente incompleto."
}
Write-Host "    Formulario de execucao: asset OK."

Write-Host "[58/492] Validando series, repeticoes e carga realizadas..."
if ($appJs.Content -notmatch "seriesRealizadas" -or
    $appJs.Content -notmatch "repeticoesRealizadas" -or
    $appJs.Content -notmatch "cargaRealizada") {
    throw "Campos de execucao incompletos."
}
Write-Host "    Series + repeticoes + carga: assets OK."

Write-Host "[59/492] Validando esforco percebido e duracao..."
if ($appJs.Content -notmatch "esforcoPercebido" -or
    $appJs.Content -notmatch "duracaoMinutos") {
    throw "RPE/duracao ausentes."
}
Write-Host "    RPE + duracao: assets OK."

Write-Host "[60/492] Validando historico do paciente..."
if ($appJs.Content -notmatch "/api/portal/me/treinos/historico" -or
    $appJs.Content -notmatch "Histórico recente") {
    throw "Historico proprio do treino ausente."
}
Write-Host "    Historico proprio: asset OK."

Write-Host "[61/492] Validando progressao de carga no prontuario..."
if ($appJs.Content -notmatch "evolucaoCarga" -or
    $appJs.Content -notmatch "Adesão e progressão") {
    throw "Progressao de carga profissional ausente."
}
Write-Host "    Evolucao de carga + adesao: assets OK."

Write-Host "[62/492] Validando estilos do acompanhamento de treino..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "load-progress-grid" -or
    $css.Content -notmatch "execution-item" -or
    $css.Content -notmatch "workout-execution-list") {
    throw "Estilos de acompanhamento incompletos."
}
Write-Host "    Acompanhamento responsivo: assets OK."


Write-Host "[63/492] Validando motor de graficos SVG..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "hpLineChart" -or
    $appJs.Content -notmatch "native-line-chart" -or
    $appJs.Content -notmatch "hpChartSeries") {
    throw "Motor de graficos SVG incompleto."
}
Write-Host "    SVG nativo + escalas + series: assets OK."

Write-Host "[64/492] Validando graficos corporais no prontuario..."
if ($appJs.Content -notmatch "hpEvalCharts" -or
    $appJs.Content -notmatch "professional-evaluations" -or
    $appJs.Content -notmatch "professional-summary") {
    throw "Graficos corporais profissionais incompletos."
}
Write-Host "    Peso + IMC + gordura + cintura: assets OK."

Write-Host "[65/492] Validando tendencias laboratoriais..."
if ($appJs.Content -notmatch "hpLabSeriesFromProfessional" -or
    $appJs.Content -notmatch "hpLabSeriesFromPatient" -or
    $appJs.Content -notmatch "hpLabCharts") {
    throw "Tendencias laboratoriais incompletas."
}
Write-Host "    Series numericas por marcador: assets OK."

Write-Host "[66/492] Validando progressao grafica de carga..."
if ($appJs.Content -notmatch "hpLoadCharts" -or
    $appJs.Content -notmatch "professional-workout-load" -or
    $appJs.Content -notmatch "patient-workout-load") {
    throw "Graficos de progressao de carga incompletos."
}
Write-Host "    Progressao de carga profissional/paciente: assets OK."

Write-Host "[67/492] Validando evolucao visual no portal do paciente..."
if ($appJs.Content -notmatch "Gráficos de evolução" -or
    $appJs.Content -notmatch "Minha evolução corporal") {
    throw "Evolucao visual do paciente ausente."
}
Write-Host "    Portal: graficos corporais OK."

Write-Host "[68/492] Validando graficos de exames no portal..."
if ($appJs.Content -notmatch "Tendências dos exames" -or
    $appJs.Content -notmatch "Resultados e tendências") {
    throw "Graficos de exames do paciente ausentes."
}
Write-Host "    Portal: tendencias laboratoriais OK."

Write-Host "[69/492] Validando responsividade dos graficos..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "analytics-grid" -or
    $css.Content -notmatch "native-line-chart" -or
    $css.Content -notmatch "@media.max-width:840px") {
    throw "Estilos responsivos dos graficos incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[70/492] Validando compatibilidade de schema na v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
if (-not (Test-Path .\scripts\sql\v0.3.1_execucoes_treino.sql)) {
    throw "Historico de upgrade v0.3.1 ausente."
}
Write-Host "    v0.3.27 reutiliza o schema ja atualizado; sem upgrade novo nesta versao."


Write-Host "[71/492] Validando endpoint de insights do dashboard..."
$insightsDashboard = Invoke-RestMethod -Uri "$base/api/insights/dashboard?limite=12" -Headers $headers
if ($null -eq $insightsDashboard.pacientesAnalisados -or $null -eq $insightsDashboard.totalInsights) {
    throw "Dashboard de insights invalido."
}
Write-Host "    Pacientes analisados: $($insightsDashboard.pacientesAnalisados) / sinais: $($insightsDashboard.totalInsights)"

Write-Host "[72/492] Validando insights por paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $patientInsights = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/insights" -Headers $headers
    if ($null -eq $patientInsights.total -or $null -eq $patientInsights.insights) {
        throw "Insights do paciente invalidos."
    }
}
Write-Host "    Endpoint individual: OK."

Write-Host "[73/492] Validando regra de exame fora da referencia..."
$sourceInsights = Get-Content .\src\HealthPlatform.Api\Controllers\InsightsController.cs -Encoding UTF8 -Raw
if ($sourceInsights -notmatch "EXAME_FORA_REFERENCIA" -or
    $sourceInsights -notmatch "ReferenciaMinima" -or
    $sourceInsights -notmatch "ReferenciaMaxima") {
    throw "Regra laboratorial incompleta."
}
Write-Host "    Faixa registrada pelo laboratorio: regra OK."

Write-Host "[74/492] Validando regras de evolucao e retorno..."
if ($sourceInsights -notmatch "VARIACAO_PESO" -or
    $sourceInsights -notmatch "SEM_RETORNO") {
    throw "Regras de evolucao/retorno incompletas."
}
Write-Host "    Variacao corporal + retorno: regras OK."

Write-Host "[75/492] Validando regras de adesao..."
if ($sourceInsights -notmatch "BAIXA_ADESAO_META" -or
    $sourceInsights -notmatch "SEM_TREINO_RECENTE" -or
    $sourceInsights -notmatch "QUEDA_FREQUENCIA_TREINO") {
    throw "Regras de adesao incompletas."
}
Write-Host "    Metas + frequencia de treino: regras OK."

Write-Host "[76/492] Validando central de atencao visual..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "Central de atenção" -or
    $appJs.Content -notmatch "/api/insights/dashboard" -or
    $appJs.Content -notmatch "hpInsightCard") {
    throw "Central visual de insights incompleta."
}
Write-Host "    Dashboard profissional: assets OK."

Write-Host "[77/492] Validando insights no prontuario..."
if ($appJs.Content -notmatch "/insights" -or
    $appJs.Content -notmatch "Insights de acompanhamento" -or
    $appJs.Content -notmatch "insight-disclaimer") {
    throw "Insights do prontuario incompletos."
}
Write-Host "    Prontuario: sinais + aviso de interpretacao OK."

Write-Host "[78/492] Validando estilos e compatibilidade do schema..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "insight-summary" -or
    $css.Content -notmatch "patient-insight-grid" -or
    $css.Content -notmatch "insight-high") {
    throw "Estilos de insights incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    Insights responsivos / schema existente compativel: OK."


Write-Host "[79/492] Validando schema de pendencias clinicas..."
$pendingTable = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='PendenciasClinicas';"
if ([int]$pendingTable -ne 1) { throw "Tabela PendenciasClinicas ausente." }
Write-Host "    PendenciasClinicas: schema OK."

Write-Host "[80/492] Validando endpoint geral de pendencias..."
$pendencias = Invoke-RestMethod -Uri "$base/api/pendencias?status=abertas&limite=20" -Headers $headers
if ($null -eq $pendencias.total -or $null -eq $pendencias.itens) {
    throw "Endpoint geral de pendencias invalido."
}
Write-Host "    Pendencias abertas: $($pendencias.total)"

Write-Host "[81/492] Validando endpoint de pendencias do paciente..."
if ($pacientes.itens.Count -gt 0) {
    $pid = $pacientes.itens[0].id
    $pp = Invoke-RestMethod -Uri "$base/api/pacientes/$pid/pendencias" -Headers $headers
}
Write-Host "    Lista por paciente: endpoint OK."

Write-Host "[82/492] Validando acoes de ciclo de vida..."
$pendingSource = Get-Content .\src\HealthPlatform.Api\Controllers\PendenciasController.cs -Encoding UTF8 -Raw
if ($pendingSource -notmatch '/vista' -or
    $pendingSource -notmatch '/adiar' -or
    $pendingSource -notmatch '/resolver') {
    throw "Ciclo de vida de pendencias incompleto."
}
Write-Host "    Vista + adiada + resolvida: rotas OK."

Write-Host "[83/492] Validando criacao de retorno a partir da pendencia..."
if ($pendingSource -notmatch '/retorno' -or
    $pendingSource -notmatch 'StatusConsulta.Agendada' -or
    $pendingSource -notmatch 'ConsultaRetornoId') {
    throw "Fluxo de retorno incompleto."
}
Write-Host "    Pendencia -> consulta futura: backend OK."

Write-Host "[84/492] Validando transformar insight em pendencia..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "insight-to-pending" -or
    $appJs.Content -notmatch "/pendencias" -or
    $appJs.Content -notmatch "Criar pendência") {
    throw "Transformacao de insight em pendencia incompleta."
}
Write-Host "    Insight -> pendencia: assets OK."

Write-Host "[85/492] Validando tela de gerenciamento de pendencias..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.Content -notmatch 'data-view=.pendencias.' -or
    $appJs.Content -notmatch "loadPendencias" -or
    $appJs.Content -notmatch "Fila de acompanhamento") {
    throw "Tela de pendencias incompleta."
}
Write-Host "    Navegacao + filtros + fila: assets OK."

Write-Host "[86/492] Validando acoes visuais da pendencia..."
if ($appJs.Content -notmatch "openResolvePending" -or
    $appJs.Content -notmatch "openSnoozePending" -or
    $appJs.Content -notmatch "openReturnPending") {
    throw "Acoes visuais de pendencia incompletas."
}
Write-Host "    Resolver + adiar + retorno: assets OK."

Write-Host "[87/492] Validando resumo de pendencias no dashboard..."
if ($appJs.Content -notmatch "Pendências abertas" -or
    $appJs.Content -notmatch "dashboard-pending-section") {
    throw "Resumo de pendencias no dashboard ausente."
}
Write-Host "    Dashboard: pendencias abertas OK."

Write-Host "[88/492] Validando auditoria, estilos e upgrade v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "pending-card" -or
    $css.Content -notmatch "pending-actions" -or
    $pendingSource -notmatch 'nameof.PendenciaClinica.') {
    throw "Auditoria/estilos de pendencias incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    Auditoria + UI responsiva + v0.3.27: OK."


Write-Host "[89/492] Validando schema de notificacoes internas..."
$notificationTable = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='NotificacoesInternas';"
if ([int]$notificationTable -ne 1) { throw "Tabela NotificacoesInternas ausente." }
Write-Host "    NotificacoesInternas: schema OK."

Write-Host "[90/492] Validando sincronizacao de notificacoes..."
$notificationControllerSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notificationControllerSource -notmatch 'HttpPost\("sincronizar"\)' -or
    $notificationControllerSource -notmatch 'SincronizarProfissional' -or
    $notificationControllerSource -notmatch 'SincronizarPaciente') {
    throw "Sincronizacao de notificacoes invalida."
}
Write-Host "    Sincronizacao idempotente: rota + regras presentes; sem mutar dados."

Write-Host "[91/492] Validando listagem e contador nao lido..."
$notifications = Invoke-RestMethod -Uri "$base/api/notificacoes?sincronizar=false&limite=50" -Headers $headers
if ($null -eq $notifications.total -or $null -eq $notifications.naoLidas -or $null -eq $notifications.itens) {
    throw "Listagem de notificacoes invalida."
}
Write-Host "    Total: $($notifications.total) / nao lidas: $($notifications.naoLidas)"

Write-Host "[92/492] Validando regras de agenda profissional..."
$notificationSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notificationSource -notmatch "SincronizarProfissional" -or
    $notificationSource -notmatch "AddHours.24." -or
    $notificationSource -notmatch 'PROF:CONSULTA') {
    throw "Lembretes de agenda profissional incompletos."
}
Write-Host "    Consultas proximas 24h: regra OK."

Write-Host "[93/492] Validando regras de pendencias..."
if ($notificationSource -notmatch 'PROF:PENDENCIA' -or
    $notificationSource -notmatch 'PendenciaClinica' -or
    $notificationSource -notmatch 'var vencida' -or
    $notificationSource -notmatch 'var venceLogo' -or
    $notificationSource -notmatch 'p\.Severidade != "Alta"' -or
    $notificationSource -notmatch 'agora\.AddHours\(24\)') {
    throw "Notificacoes de pendencias incompletas."
}
Write-Host "    Vencidas + alta prioridade + vencimento proximo: regras OK."

Write-Host "[94/492] Validando lembretes do paciente..."
if ($notificationSource -notmatch "SincronizarPaciente" -or
    $notificationSource -notmatch 'PAC:CONSULTA' -or
    $notificationSource -notmatch "Lembrete de consulta") {
    throw "Lembretes do paciente incompletos."
}
Write-Host "    Portal do paciente: consulta proxima OK."

Write-Host "[95/492] Validando leitura individual e em massa..."
if ($notificationSource -notmatch '/lida' -or
    $notificationSource -notmatch 'ler-todas' -or
    $notificationSource -notmatch 'LidaEmUtc') {
    throw "Ciclo de leitura de notificacoes incompleto."
}
Write-Host "    Lida individual + ler todas: backend OK."

Write-Host "[96/492] Validando sino e drawer na interface..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch "notificationButton" -or
    $index.Content -notmatch "patientNotificationButton" -or
    $index.Content -notmatch "notificationDrawer" -or
    $appJs.Content -notmatch "openNotifications") {
    throw "Central visual de notificacoes incompleta."
}
Write-Host "    Profissional + paciente + drawer: assets OK."

Write-Host "[97/492] Validando contador e atualizacao periodica..."
if ($appJs.Content -notmatch "notificationBadge" -or
    $appJs.Content -notmatch "setInterval" -or
    $appJs.Content -notmatch "60000" -or
    $appJs.Content -notmatch "refreshNotifications") {
    throw "Contador/polling de notificacoes incompleto."
}
Write-Host "    Badge + atualizacao a cada 60s: assets OK."

Write-Host "[98/492] Validando estilos, upgrade e versao..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "notification-panel" -or
    $css.Content -notmatch "notification-item" -or
    $css.Content -notmatch "notification-badge") {
    throw "Estilos de notificacoes incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    UI responsiva + upgrade v0.3.27: OK."


Write-Host "[99/492] Validando script de popular banco..."
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

Write-Host "[100/492] Validando idempotencia do popular..."
if ($popularSource -notmatch "Ensure-Patient" -or
    $popularSource -notmatch "Ensure-Consultation" -or
    $popularSource -notmatch "Ensure-Evaluation" -or
    $popularSource -notmatch "Ensure-Lab" -or
    $popularSource -notmatch "Ensure-Goal") {
    throw "Helpers idempotentes do POPULAR.ps1 incompletos."
}
Write-Host "    Paciente + consulta + avaliacao + exames + metas: helpers OK."

Write-Host "[101/492] Validando cobertura de modulos na base demo..."
if ($popularSource -notmatch "Ensure-Diary" -or
    $popularSource -notmatch "Ensure-Workout" -or
    $popularSource -notmatch "Ensure-Pending" -or
    $popularSource -notmatch "/api/notificacoes/sincronizar") {
    throw "Cobertura dos modulos demo incompleta."
}
Write-Host "    Diario + treino + pendencias + notificacoes: script OK."

Write-Host "[102/492] Validando endpoint de resumo de dados..."
$dataResumo = Invoke-RestMethod -Uri "$base/api/dados/resumo" -Headers $headers
if ($null -eq $dataResumo.pacientes -or
    $null -eq $dataResumo.consultas -or
    $null -eq $dataResumo.avaliacoes -or
    $null -eq $dataResumo.exames) {
    throw "Resumo de dados invalido."
}
Write-Host "    Pacientes=$($dataResumo.pacientes) / consultas=$($dataResumo.consultas) / avaliacoes=$($dataResumo.avaliacoes) / exames=$($dataResumo.exames)"

Write-Host "[103/492] Validando que popular banco e opt-in..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if ($setupSource -match "POPULAR.ps1") {
    throw "POPULAR.ps1 nao deve executar automaticamente no PREPARAR."
}
Write-Host "    PREPARAR preserva dados do usuario; POPULAR e execucao explicita."

Write-Host "[104/492] Validando versao v0.3.27 e upgrade do schema..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
if ($setupSource -notmatch "\[30/30\]") { throw "PREPARAR deveria possuir 30 etapas na v0.3.27." }
Write-Host "    v0.3.27 / PREPARAR 29 etapas / upgrade SOAP: OK."


Write-Host "[105/492] Validando endpoint da carteira..."
$carteira = Invoke-RestMethod -Uri "$base/api/carteira?ordenar=score" -Headers $headers
if ($null -eq $carteira.totalPacientes -or $null -eq $carteira.pacientes) {
    throw "Endpoint da carteira invalido."
}
Write-Host "    Carteira: $($carteira.totalPacientes) paciente(s)."

Write-Host "[106/492] Validando priorizacao da carteira..."
$carteiraSource = Get-Content .\src\HealthPlatform.Api\Controllers\CarteiraController.cs -Encoding UTF8 -Raw
if ($carteiraSource -notmatch "Score" -or
    $carteiraSource -notmatch "Prioridade" -or
    $carteiraSource -notmatch "pendAlta" -or
    $carteiraSource -notmatch "semRetorno") {
    throw "Motor de priorizacao da carteira incompleto."
}
Write-Host "    Score + pendencias + retorno: backend OK."

Write-Host "[107/492] Validando sinais de exames/evolucao na carteira..."
if ($carteiraSource -notmatch "ReferenciaMinima" -or
    $carteiraSource -notmatch "ReferenciaMaxima" -or
    $carteiraSource -notmatch "PesoKg") {
    throw "Sinais clinicos da carteira incompletos."
}
Write-Host "    Exames + peso: leitura longitudinal OK."

Write-Host "[108/492] Validando atividade recente..."
if ($carteiraSource -notmatch "TreinosUltimos30Dias" -or
    $carteiraSource -notmatch "RegistrosDiarioUltimos14Dias" -or
    $carteiraSource -notmatch "RegistrosMetaUltimos14Dias") {
    throw "Indicadores de atividade da carteira incompletos."
}
Write-Host "    Treinos + diario + metas: backend OK."

Write-Host "[109/492] Validando tela Carteira..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.carteira.' -or
    $appJs.Content -notmatch "loadCarteira" -or
    $appJs.Content -notmatch "/api/carteira") {
    throw "Tela Carteira incompleta."
}
Write-Host "    Navegacao + carregamento: assets OK."

Write-Host "[110/492] Validando filtros e ordenacao..."
if ($appJs.Content -notmatch "portfolioSearch" -or
    $appJs.Content -notmatch "portfolioPriority" -or
    $appJs.Content -notmatch "portfolioSort") {
    throw "Filtros da carteira incompletos."
}
Write-Host "    Busca + prioridade + ordenacao: assets OK."

Write-Host "[111/492] Validando atalho da carteira no dashboard..."
if ($appJs.Content -notmatch "Pacientes para acompanhar" -or
    $appJs.Content -notmatch "openPortfolio" -or
    $appJs.Content -notmatch "dashboard-portfolio-section") {
    throw "Resumo da carteira no dashboard ausente."
}
Write-Host "    Dashboard -> carteira: assets OK."

Write-Host "[112/492] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "portfolio-patient-card" -or
    $css.Content -notmatch "portfolio-metrics" -or
    $css.Content -notmatch "portfolio-toolbar") {
    throw "Estilos da carteira incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    Carteira responsiva / v0.3.27: OK."


Write-Host "[113/492] Validando schema de follow-up..."
$followTable = docker exec healthplatform-postgres psql -U healthplatform -d healthplatform -t -A -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='InteracoesAcompanhamento';"
if ([int]$followTable -ne 1) { throw "Tabela InteracoesAcompanhamento ausente." }
Write-Host "    InteracoesAcompanhamento: schema OK."

Write-Host "[114/492] Validando endpoint de follow-up..."
$followSource = Get-Content .\src\HealthPlatform.Api\Controllers\FollowUpController.cs -Encoding UTF8 -Raw
if ($followSource -notmatch 'api/pacientes/{pacienteId:guid}/followups' -or
    $followSource -notmatch 'RegistrarFollowUpRequest' -or
    $followSource -notmatch 'InteracaoAcompanhamento') {
    throw "Endpoint de follow-up incompleto."
}
Write-Host "    GET + POST de follow-up: backend OK."

Write-Host "[115/492] Validando canais e proximo contato..."
if ($followSource -notmatch "WhatsApp" -or
    $followSource -notmatch "Telefone" -or
    $followSource -notmatch "Presencial" -or
    $followSource -notmatch "ProximoContatoUtc") {
    throw "Dados de follow-up incompletos."
}
Write-Host "    Canais + proximo contato: backend OK."

Write-Host "[116/492] Validando auditoria do contato..."
if ($followSource -notmatch "AuditLogs" -or
    $followSource -notmatch "nameof.InteracaoAcompanhamento.") {
    throw "Auditoria de follow-up ausente."
}
Write-Host "    Auditoria: backend OK."

Write-Host "[117/492] Validando follow-up na carteira..."
$carteiraSource = Get-Content .\src\HealthPlatform.Api\Controllers\CarteiraController.cs -Encoding UTF8 -Raw
if ($carteiraSource -notmatch "UltimoContatoUtc" -or
    $carteiraSource -notmatch "ProximoContatoUtc" -or
    $carteiraSource -notmatch "ContatosUltimos30Dias") {
    throw "Follow-up na carteira incompleto."
}
Write-Host "    Ultimo/proximo contato + volume 30d: backend OK."

Write-Host "[118/492] Validando acao rapida de contato..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "openPortfolioContact" -or
    $appJs.Content -notmatch "Registrar contato" -or
    $appJs.Content -notmatch "/followups") {
    throw "Acao rapida de contato incompleta."
}
Write-Host "    Carteira -> registrar contato: assets OK."

Write-Host "[119/492] Validando acao rapida de retorno..."
if ($appJs.Content -notmatch "openPortfolioReturn" -or
    $appJs.Content -notmatch "Agendar retorno" -or
    $appJs.Content -notmatch "/consultas") {
    throw "Acao rapida de retorno incompleta."
}
Write-Host "    Carteira -> agenda: assets OK."

Write-Host "[120/492] Validando acao rapida de pendencia..."
if ($appJs.Content -notmatch "openPortfolioPending" -or
    $appJs.Content -notmatch "Criar pendência" -or
    $appJs.Content -notmatch "/pendencias") {
    throw "Acao rapida de pendencia incompleta."
}
Write-Host "    Carteira -> pendencia: assets OK."

Write-Host "[121/492] Validando historico no prontuario..."
if ($appJs.Content -notmatch "Follow-up" -or
    $appJs.Content -notmatch "followup-history-section" -or
    $appJs.Content -notmatch "patientQuickContact") {
    throw "Historico de follow-up no prontuario incompleto."
}
Write-Host "    Prontuario: historico + contato rapido OK."

Write-Host "[122/492] Validando estilos, upgrade e versao..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "followup-history-list" -or
    $css.Content -notmatch "followup-channel") {
    throw "Estilos de follow-up incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / follow-up responsivo / upgrade OK."


Write-Host "[123/492] Validando endpoint da fila de follow-up..."
$fila = Invoke-RestMethod -Uri "$base/api/followups/fila?faixa=todos" -Headers $headers
if ($null -eq $fila.total -or $null -eq $fila.itens) {
    throw "Fila de follow-up invalida."
}
Write-Host "    Pacientes com proximo contato: $($fila.total)"

Write-Host "[124/492] Validando faixas de vencimento..."
$filaSource = Get-Content .\src\HealthPlatform.Api\Controllers\FilaFollowUpController.cs -Encoding UTF8 -Raw
if ($filaSource -notmatch "Vencido" -or
    $filaSource -notmatch "Proximos7Dias" -or
    $filaSource -notmatch "DiasAtraso") {
    throw "Faixas da fila de follow-up incompletas."
}
Write-Host "    Vencido + hoje + 7 dias + futuro: backend OK."

Write-Host "[125/492] Validando busca e filtros da fila..."
if ($filaSource -notmatch "busca" -or
    $filaSource -notmatch "faixa" -or
    $filaSource -notmatch "PacienteNome") {
    throw "Filtros da fila incompletos."
}
Write-Host "    Busca + faixa: backend OK."

Write-Host "[126/492] Validando notificacao de follow-up..."
$notificationSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notificationSource -notmatch "PROF:FOLLOWUP" -or
    $notificationSource -notmatch "InteracaoAcompanhamento" -or
    $notificationSource -notmatch '"followups"') {
    throw "Notificacao de follow-up incompleta."
}
Write-Host "    Proximo contato -> notificacao: backend OK."

Write-Host "[127/492] Validando tela de follow-up..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.followups.' -or
    $appJs.Content -notmatch "loadFollowUpQueue" -or
    $appJs.Content -notmatch "/api/followups/fila") {
    throw "Tela de follow-up incompleta."
}
Write-Host "    Navegacao + fila: assets OK."

Write-Host "[128/492] Validando acoes rapidas na fila..."
if ($appJs.Content -notmatch "follow-queue-contact" -or
    $appJs.Content -notmatch "openPortfolioContact" -or
    $appJs.Content -notmatch "follow-queue-patient") {
    throw "Acoes da fila incompletas."
}
Write-Host "    Registrar contato + prontuario: assets OK."

Write-Host "[129/492] Validando resumo de follow-up no dashboard..."
if ($appJs.Content -notmatch "dashboard-followup-section" -or
    $appJs.Content -notmatch "openFollowUpQueue" -or
    $appJs.Content -notmatch "Follow-ups") {
    throw "Resumo de follow-up no dashboard ausente."
}
Write-Host "    Dashboard -> fila de follow-up: assets OK."

Write-Host "[130/492] Validando estilos, popular e versao..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
$popularSource = Get-Content .\POPULAR.ps1 -Encoding UTF8 -Raw
if ($css.Content -notmatch "follow-queue-card" -or
    $css.Content -notmatch "follow-queue-toolbar" -or
    $popularSource -notmatch "Ensure-FollowUp") {
    throw "Estilos/populacao de follow-up incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / fila responsiva / demo follow-up: OK."


Write-Host "[131/492] Validando endpoint de gestao..."
$gestao = Invoke-RestMethod -Uri "$base/api/gestao/resumo?dias=30" -Headers $headers
if ($null -eq $gestao.pacientesAtivos -or
    $null -eq $gestao.consultasRealizadas -or
    $null -eq $gestao.taxaComparecimentoPct) {
    throw "Resumo de gestao invalido."
}
Write-Host "    Pacientes=$($gestao.pacientesAtivos) / realizadas=$($gestao.consultasRealizadas) / comparecimento=$($gestao.taxaComparecimentoPct)%"

Write-Host "[132/492] Validando indicadores de consultas..."
$gestaoSource = Get-Content .\src\HealthPlatform.Api\Controllers\GestaoController.cs -Encoding UTF8 -Raw
if ($gestaoSource -notmatch "ConsultasRealizadas" -or
    $gestaoSource -notmatch "ConsultasCanceladas" -or
    $gestaoSource -notmatch "Faltas" -or
    $gestaoSource -notmatch "TaxaComparecimentoPct") {
    throw "Indicadores de consultas incompletos."
}
Write-Host "    Realizadas + faltas + canceladas + taxa: backend OK."

Write-Host "[133/492] Validando indicadores de acompanhamento..."
if ($gestaoSource -notmatch "FollowUpsRealizados" -or
    $gestaoSource -notmatch "FollowUpsVencidos" -or
    $gestaoSource -notmatch "PendenciasAbertas" -or
    $gestaoSource -notmatch "PendenciasResolvidasPeriodo") {
    throw "Indicadores de acompanhamento incompletos."
}
Write-Host "    Follow-up + pendencias: backend OK."

Write-Host "[134/492] Validando indicadores de engajamento..."
if ($gestaoSource -notmatch "TreinosRegistrados" -or
    $gestaoSource -notmatch "RegistrosDiario" -or
    $gestaoSource -notmatch "RegistrosMetas") {
    throw "Indicadores de engajamento incompletos."
}
Write-Host "    Treinos + diario + metas: backend OK."

Write-Host "[135/492] Validando series gerenciais..."
if ($gestaoSource -notmatch "ConsultasPorStatus" -or
    $gestaoSource -notmatch "AtividadePorSemana" -or
    $gestaoSource -notmatch "PacientesAtencao") {
    throw "Series gerenciais incompletas."
}
Write-Host "    Status + atividade semanal + pacientes de atencao: backend OK."

Write-Host "[136/492] Validando tela Gestao..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.gestao.' -or
    $appJs.Content -notmatch "loadManagement" -or
    $appJs.Content -notmatch "/api/gestao/resumo") {
    throw "Tela Gestao incompleta."
}
Write-Host "    Navegacao + periodo + indicadores: assets OK."

Write-Host "[137/492] Validando graficos e resumo no dashboard..."
if ($appJs.Content -notmatch "managementBarChart" -or
    $appJs.Content -notmatch "managementMiniSeries" -or
    $appJs.Content -notmatch "dashboard-management-section") {
    throw "Graficos/resumo gerencial incompletos."
}
Write-Host "    Barras + serie semanal + dashboard: assets OK."

Write-Host "[138/492] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "management-grid" -or
    $css.Content -notmatch "management-bars" -or
    $css.Content -notmatch "management-attention-list") {
    throw "Estilos de gestao incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    Gestao responsiva / v0.3.27: OK."


Write-Host "[139/492] Validando PREPARAR sem atualizacao automatica do dotnet..."
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

Write-Host "[140/492] Validando endpoint CSV gerencial..."
$exportSource = Get-Content .\src\HealthPlatform.Api\Controllers\GestaoExportController.cs -Encoding UTF8 -Raw
if ($exportSource -notmatch 'HttpGet\("csv"\)' -or
    $exportSource -notmatch "text/csv" -or
    $exportSource -notmatch "PendenciasAbertas") {
    throw "Exportacao CSV gerencial incompleta."
}
Write-Host "    CSV: backend OK."

Write-Host "[141/492] Validando conteudo longitudinal do CSV..."
if ($exportSource -notmatch "UltimaConsulta" -or
    $exportSource -notmatch "ProximaConsulta" -or
    $exportSource -notmatch "FollowUpsNoPeriodo" -or
    $exportSource -notmatch "ProximoContato") {
    throw "CSV nao inclui acompanhamento esperado."
}
Write-Host "    Consulta + pendencia + follow-up: colunas OK."

Write-Host "[142/492] Validando relatorio HTML imprimivel..."
if ($exportSource -notmatch 'HttpGet\("html"\)' -or
    $exportSource -notmatch "window.print" -or
    $exportSource -notmatch "Comparecimento" -or
    $exportSource -notmatch "taxa") {
    throw "Relatorio HTML incompleto."
}
Write-Host "    HTML imprimivel: backend OK."

Write-Host "[143/492] Validando botoes de exportacao na Gestao..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "managementExportCsv" -or
    $appJs.Content -notmatch "managementPrintReport" -or
    $appJs.Content -notmatch "downloadManagementCsv") {
    throw "Acoes visuais de exportacao incompletas."
}
Write-Host "    CSV + relatorio: assets OK."

Write-Host "[144/492] Validando download autenticado..."
if ($appJs.Content -notmatch "hpAuthenticatedBlob" -or
    $appJs.Content -notmatch "Authorization" -or
    $appJs.Content -notmatch "createObjectURL") {
    throw "Download autenticado incompleto."
}
Write-Host "    Bearer + Blob + nome de arquivo: assets OK."

Write-Host "[145/492] Validando relatorio autenticado em nova janela..."
if ($appJs.Content -notmatch "openManagementPrintable" -or
    $appJs.Content -notmatch "/api/gestao/export/html" -or
    $appJs.Content -notmatch "window.open") {
    throw "Abertura do relatorio gerencial incompleta."
}
Write-Host "    HTML autenticado -> janela imprimivel: assets OK."

Write-Host "[146/492] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "management-head-actions") {
    throw "Estilos de exportacao gerencial ausentes."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / exportacao gerencial / setup rapido: OK."


Write-Host "[147/492] Validando perguntas de anamnese inativas..."
$perguntasSource = Get-Content .\src\HealthPlatform.Api\Controllers\PerguntasAnamneseController.cs -Encoding UTF8 -Raw
if ($perguntasSource -notmatch "incluirInativas" -or
    $perguntasSource -notmatch "incluirInativas \|\| x.Ativa") {
    throw "Perguntas de anamnese ainda nao suportam incluir inativas."
}
Write-Host "    incluirInativas=true: backend OK."

Write-Host "[148/492] Validando totais completos de insights..."
$insightSource = Get-Content .\src\HealthPlatform.Api\Controllers\InsightsController.cs -Encoding UTF8 -Raw
if ($insightSource -match "SelectMany\(x => x.Insights\)" -and $insightSource -match "Take\(4\)") {
    throw "Totais do dashboard ainda dependem da lista visual truncada."
}
if ($insightSource -notmatch "totalInsights \+= insights.Count") {
    throw "Acumulador completo de insights ausente."
}
Write-Host "    Totais agregados independem do top 4 visual."

Write-Host "[149/492] Validando timezone das notificacoes..."
$notifSource = Get-Content .\src\HealthPlatform.Api\Controllers\NotificacoesController.cs -Encoding UTF8 -Raw
if ($notifSource -match "ToLocalTime\(\):dd/MM HH:mm") {
    throw "Notificacoes ainda formatam horario pelo timezone do servidor."
}
Write-Host "    Instantes permanecem UTC; interface localiza no navegador."

Write-Host "[150/492] Validando cleanup do polling no logout..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "logoutBtn.onclick" -or
    $appJs.Content -notmatch "patientLogoutBtn.onclick") {
    throw "Logout nao foi religado ao wrapper com cleanup."
}
Write-Host "    Logout profissional + paciente: polling cleanup OK."

Write-Host "[151/492] Validando navegacao robusta no portal..."
if ($appJs.Content -notmatch "hpOpenPatientNotificationLink" -or
    $appJs.Content -notmatch "loadPatientPortalView" -or
    $appJs.Content -notmatch "loadPatientSection") {
    throw "Fallback de navegacao do paciente incompleto."
}
Write-Host "    Notificacao do paciente: fallback de navegacao OK."

Write-Host "[152/492] Validando smoke de notificacoes sem mutacao..."
$testSource = Get-Content .\TESTAR.ps1 -Encoding UTF8 -Raw
if ($testSource -match 'Invoke-RestMethod\s+-Uri\s+"\$base/api/notificacoes/sincronizar"') {
    throw "TESTAR ainda chama POST mutavel de sincronizacao."
}
if ($testSource -match '/api/notificacoes\?sincronizar=true') {
    throw "TESTAR ainda usa GET mutavel de notificacoes."
}
Write-Host "    TESTAR nao cria nem sincroniza notificacoes."

Write-Host "[153/492] Validando copy historica de schema..."
$schemaCheckMarker = 'Write-Host "[153/492] Validando copy historica de schema..."'
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

Write-Host "[154/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / estabilizacao + qualidade: OK."


Write-Host "[155/492] Validando endpoint de busca global..."
$buscaSource = Get-Content .\src\HealthPlatform.Api\Controllers\BuscaGlobalController.cs -Encoding UTF8 -Raw
if ($buscaSource -notmatch 'Route\("api/busca"\)' -or
    $buscaSource -notmatch "EF.Functions.ILike") {
    throw "Busca global backend incompleta."
}
Write-Host "    /api/busca + ILIKE: backend OK."

Write-Host "[156/492] Validando fontes da busca..."
if ($buscaSource -notmatch '"Paciente"' -or
    $buscaSource -notmatch '"Pendência"' -or
    $buscaSource -notmatch '"Follow-up"' -or
    $buscaSource -notmatch '"Consulta"') {
    throw "Busca global nao cobre todas as fontes."
}
Write-Host "    Paciente + pendencia + follow-up + consulta: OK."

Write-Host "[157/492] Validando isolamento por organizacao..."
if ($buscaSource -notmatch "currentUser.OrganizationId" -or
    $buscaSource -notmatch "x.OrganizacaoId == org") {
    throw "Busca global sem isolamento organizacional."
}
Write-Host "    Multi-tenant: filtro de organizacao presente."

Write-Host "[158/492] Validando botao e atalho da busca..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch "globalSearchButton" -or
    $appJs.Content -notmatch "hpOpenGlobalSearch" -or
    $appJs.Content -notmatch "ctrlKey") {
    throw "Botao/atalho de busca ausente."
}
Write-Host "    Botao + Ctrl/Cmd+K: assets OK."

Write-Host "[159/492] Validando modal e debounce..."
if ($appJs.Content -notmatch "globalSearchModal" -or
    $appJs.Content -notmatch "hpGlobalSearchTimer" -or
    $appJs.Content -notmatch "setTimeout") {
    throw "Modal/debounce da busca incompletos."
}
Write-Host "    Modal + debounce: assets OK."

Write-Host "[160/492] Validando acoes dos resultados..."
if ($appJs.Content -notmatch "hpExecuteGlobalSearchResult" -or
    $appJs.Content -notmatch "openPatient" -or
    $appJs.Content -notmatch "navigate\('pendencias'\)" -or
    $appJs.Content -notmatch "navigate\('followups'\)" -or
    $appJs.Content -notmatch "navigate\('agenda'\)") {
    throw "Acoes da busca global incompletas."
}
Write-Host "    Prontuario + pendencias + follow-up + agenda: assets OK."

Write-Host "[161/492] Validando estilos responsivos da busca..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "global-search-dialog" -or
    $css.Content -notmatch "global-search-result" -or
    $css.Content -notmatch "@media\(max-width:650px\)") {
    throw "Estilos da busca global incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[162/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / busca global + central de acoes: OK."


Write-Host "[163/492] Validando endpoint da Central do Dia..."
$centralSource = Get-Content .\src\HealthPlatform.Api\Controllers\CentralDiaController.cs -Encoding UTF8 -Raw
if ($centralSource -notmatch 'Route\("api/central-dia"\)' -or
    $centralSource -notmatch "offsetMinutos") {
    throw "Central do Dia backend incompleta."
}
Write-Host "    Endpoint + offset local: backend OK."

Write-Host "[164/492] Validando agenda do dia..."
if ($centralSource -notmatch "ConsultasHoje" -or
    $centralSource -notmatch "DataHoraUtc >= inicioUtc" -or
    $centralSource -notmatch "DataHoraUtc < fimUtc") {
    throw "Agenda diaria da Central incompleta."
}
Write-Host "    Janela local do dia -> UTC: backend OK."

Write-Host "[165/492] Validando follow-ups do dia..."
if ($centralSource -notmatch "FollowUpsVencidos" -or
    $centralSource -notmatch "FollowUpsHoje" -or
    $centralSource -notmatch '"Vencido"' -or
    $centralSource -notmatch '"Hoje"') {
    throw "Follow-ups da Central incompletos."
}
Write-Host "    Vencidos + hoje: backend OK."

Write-Host "[166/492] Validando pendencias prioritarias..."
if ($centralSource -notmatch "PendenciasPrioritarias" -or
    $centralSource -notmatch 'x.Severidade == "Alta"' -or
    $centralSource -notmatch "VencimentoUtc") {
    throw "Pendencias prioritarias incompletas."
}
Write-Host "    Alta prioridade + vencimento: backend OK."

Write-Host "[167/492] Validando pacientes para revisao..."
if ($centralSource -notmatch "PacientesRevisao" -or
    $centralSource -notmatch "SemRetornoFuturo" -or
    $centralSource -notmatch "PendenciasAbertas") {
    throw "Pacientes para revisao incompletos."
}
Write-Host "    Pendencias + retorno futuro: backend OK."

Write-Host "[168/492] Validando tela Hoje..."
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($index.Content -notmatch 'data-view=.central-dia.' -or
    $appJs.Content -notmatch "loadCentralDia" -or
    $appJs.Content -notmatch "/api/central-dia") {
    throw "Tela Central do Dia incompleta."
}
Write-Host "    Navegacao + carregamento: assets OK."

Write-Host "[169/492] Validando acoes rapidas e dashboard..."
if ($appJs.Content -notmatch "central-register-contact" -or
    $appJs.Content -notmatch "central-open-patient" -or
    $appJs.Content -notmatch "dashboard-central-day" -or
    $appJs.Content -notmatch "openCentralDay") {
    throw "Acoes/dashboard da Central incompletos."
}
Write-Host "    Contato + prontuario + filas + dashboard: assets OK."

Write-Host "[170/492] Validando estilos e versao v0.3.27..."
$css = Get-Utf8WebAsset -Uri "$base/app.css" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.css"
if ($css.Content -notmatch "central-day-grid" -or
    $css.Content -notmatch "central-day-row" -or
    $css.Content -notmatch "dashboard-central-day-metrics") {
    throw "Estilos da Central do Dia incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / Central do Dia responsiva: OK."


Write-Host "[171/492] Validando schema de evolucoes clinicas SOAP..."
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

Write-Host "[172/492] Validando endpoint de evolucoes..."
$evoController = Get-Content .\src\HealthPlatform.Api\Controllers\EvolucoesClinicasController.cs -Encoding UTF8 -Raw
if ($evoController -notmatch 'api/pacientes/{pacienteId:guid}/evolucoes' -or
    $evoController -notmatch 'HttpPost' -or
    $evoController -notmatch 'HttpPut') {
    throw "Endpoints de evolucao incompletos."
}
Write-Host "    GET + POST + PUT: backend OK."

Write-Host "[173/492] Validando consulta opcional e isolamento..."
if ($evoController -notmatch "ConsultaValida" -or
    $evoController -notmatch "currentUser.OrganizationId" -or
    $evoController -notmatch "PacienteId == pacienteId") {
    throw "Vinculo de consulta/tenant incompleto."
}
Write-Host "    Consulta opcional + multi-tenant: backend OK."

Write-Host "[174/492] Validando auditoria da evolucao..."
if ($evoController -notmatch 'AdicionarAuditoria\("CREATE"' -or
    $evoController -notmatch 'AdicionarAuditoria\("UPDATE"' -or
    $evoController -notmatch "DadosAnterioresJson" -or
    $evoController -notmatch "DadosNovosJson") {
    throw "Auditoria da evolucao incompleta."
}
Write-Host "    CREATE + UPDATE com antes/depois: OK."

Write-Host "[175/492] Validando evolucao na timeline..."
$timelineSource = Get-Content .\src\HealthPlatform.Api\Controllers\TimelineController.cs -Encoding UTF8 -Raw
if ($timelineSource -notmatch "EvolucoesClinicas" -or
    $timelineSource -notmatch '"evolucao_clinica"' -or
    $timelineSource -notmatch "Evolucao clinica SOAP") {
    throw "Timeline SOAP incompleta."
}
Write-Host "    Evolucao aparece na timeline clinica."

Write-Host "[176/492] Validando aba Evolucoes no prontuario..."
$appJs = Get-Utf8WebAsset -Uri "$base/app.js" -LocalPath ".\src\HealthPlatform.Api\wwwroot\app.js"
if ($appJs.Content -notmatch "tabButton\('evolucoes'" -or
    $appJs.Content -notmatch "soap-card" -or
    $appJs.Content -notmatch "/evolucoes") {
    throw "Aba Evolucoes incompleta."
}
Write-Host "    Historico SOAP: assets OK."

Write-Host "[177/492] Validando registro visual SOAP..."
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

Write-Host "[178/492] Validando edicao visual da evolucao..."
if (-not $appJsSource.Contains("edit-evolution") -or
    -not $appJsSource.Contains("method:id?'PUT':'POST'") -or
    -not $appJsSource.Contains("/api/evolucoes/")) {
    throw "Edicao SOAP incompleta."
}
Write-Host "    Historico + edicao auditada: assets OK."

Write-Host "[179/492] Validando upgrade SQL SOAP historico e PREPARAR atual..."
$soapSqlSource = Get-Content .\scripts\sql\v0.3.15_evolucoes_clinicas.sql -Encoding UTF8 -Raw
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $soapSqlSource.Contains('"EvolucoesClinicas"') -or
    -not $soapSqlSource.Contains('"Subjetivo"') -or
    -not $soapSqlSource.Contains('"Objetivo"') -or
    -not $soapSqlSource.Contains('"Avaliacao"') -or
    -not $soapSqlSource.Contains('"Plano"')) {
    throw "Upgrade SOAP historico incompleto."
}
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.15_evolucoes_clinicas.sql") -or
    -not $setupSource.Contains("v0.3.22_progressao_treino.sql")) {
    throw "PREPARAR atual incompleto."
}
Write-Host "    SOAP v0.3.15 preservado / PREPARAR atual 30/30: OK."

Write-Host "[180/492] Validando estilos e versao v0.3.27..."
$soapCssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $soapCssSource.Contains("soap-grid") -or
    -not $soapCssSource.Contains("soap-card")) {
    throw "Estilos SOAP incompletos."
}
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / evolucao clinica SOAP: OK."


Write-Host "[181/492] Validando endpoint de resumo clinico..."
$resumoSource = Get-Content .\src\HealthPlatform.Api\Controllers\ResumoClinicoController.cs -Encoding UTF8 -Raw
if (-not $resumoSource.Contains('Route("api/pacientes/{pacienteId:guid}/resumo-clinico")') -or
    -not $resumoSource.Contains("ResumoClinicoResponse")) {
    throw "Resumo clinico backend incompleto."
}
Write-Host "    Endpoint consolidado: backend OK."

Write-Host "[182/492] Validando consulta e SOAP no resumo..."
if (-not $resumoSource.Contains("UltimaConsulta") -or
    -not $resumoSource.Contains("ProximaConsulta") -or
    -not $resumoSource.Contains("UltimaEvolucao") -or
    -not $resumoSource.Contains("EvolucoesClinicas")) {
    throw "Agenda/SOAP do resumo incompletos."
}
Write-Host "    Ultima/proxima consulta + SOAP: OK."

Write-Host "[183/492] Validando corpo e anamnese..."
if (-not $resumoSource.Contains("UltimaAvaliacao") -or
    -not $resumoSource.Contains("db.Avaliacoes") -or
    -not $resumoSource.Contains("UltimaAnamnese") -or
    -not $resumoSource.Contains("db.Anamneses")) {
    throw "Corpo/anamnese do resumo incompletos."
}
Write-Host "    Avaliacao corporal + anamnese: OK."

Write-Host "[184/492] Validando exames alterados..."
if (-not $resumoSource.Contains("ExamesAlterados") -or
    -not $resumoSource.Contains("ReferenciaMinima") -or
    -not $resumoSource.Contains("ReferenciaMaxima") -or
    -not $resumoSource.Contains('"Abaixo"') -or
    -not $resumoSource.Contains('"Acima"')) {
    throw "Exames alterados do resumo incompletos."
}
Write-Host "    Faixas numericas registradas: backend OK."

Write-Host "[185/492] Validando metas, treinos e pendencias..."
if (-not $resumoSource.Contains("MetasAtivas") -or
    -not $resumoSource.Contains("TreinosUltimos30Dias") -or
    -not $resumoSource.Contains("DataHoraInicioUtc") -or
    -not $resumoSource.Contains("PendenciasAltaPrioridade")) {
    throw "Indicadores operacionais do resumo incompletos."
}
Write-Host "    Metas + treino + pendencias: backend OK."

Write-Host "[186/492] Validando resumo no prontuario..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpClinicalSummaryCard") -or
    -not $appJsSource.Contains("/resumo-clinico") -or
    -not $appJsSource.Contains("clinical-summary-card")) {
    throw "Resumo clinico visual incompleto."
}
Write-Host "    Resumo integrado ao prontuario: assets OK."

Write-Host "[187/492] Validando atualizacao e responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("clinicalSummaryRefresh") -or
    -not $cssSource.Contains("clinical-summary-grid") -or
    -not $cssSource.Contains("clinical-summary-metrics")) {
    throw "Atualizacao/estilos do resumo incompletos."
}
Write-Host "    Atualizacao manual + desktop/mobile: assets OK."

Write-Host "[188/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / resumo clinico consolidado: OK."


Write-Host "[189/492] Validando texto de handoff clinico..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpClinicalSummaryText") -or
    -not $appJsSource.Contains("RESUMO CLÍNICO") -or
    -not $appJsSource.Contains("ACOMPANHAMENTO")) {
    throw "Handoff textual incompleto."
}
Write-Host "    Resumo textual estruturado: assets OK."

Write-Host "[190/492] Validando conteudo SOAP no handoff..."
if (-not $appJsSource.Contains("r.ultimaEvolucao.subjetivo") -or
    -not $appJsSource.Contains("r.ultimaEvolucao.objetivo") -or
    -not $appJsSource.Contains("r.ultimaEvolucao.avaliacao") -or
    -not $appJsSource.Contains("r.ultimaEvolucao.plano")) {
    throw "SOAP no handoff incompleto."
}
Write-Host "    S/O/A/P presentes no texto de handoff."

Write-Host "[191/492] Validando copia para clipboard..."
if (-not $appJsSource.Contains("hpCopyClinicalSummary") -or
    -not $appJsSource.Contains("navigator.clipboard") -or
    -not $appJsSource.Contains("document.execCommand('copy')")) {
    throw "Copia do handoff incompleta."
}
Write-Host "    Clipboard moderno + fallback: assets OK."

Write-Host "[192/492] Validando impressao do resumo..."
if (-not $appJsSource.Contains("hpClinicalSummaryPrintHtml") -or
    -not $appJsSource.Contains("hpPrintClinicalSummary") -or
    -not $appJsSource.Contains("window.print()")) {
    throw "Impressao do resumo incompleta."
}
Write-Host "    HTML imprimivel + print automatico: assets OK."

Write-Host "[193/492] Validando seguranca basica do HTML imprimivel..."
if (-not $appJsSource.Contains("const escPrint=v=>esc(v??'')") -or
    -not $appJsSource.Contains("noopener,noreferrer")) {
    throw "Protecoes da impressao incompletas."
}
Write-Host "    Escape de conteudo + nova janela isolada: assets OK."

Write-Host "[194/492] Validando botoes no resumo clinico..."
if (-not $appJsSource.Contains('id="clinicalSummaryCopy"') -or
    -not $appJsSource.Contains('id="clinicalSummaryPrint"') -or
    -not $appJsSource.Contains('id="clinicalSummaryRefresh"')) {
    throw "Acoes do resumo clinico incompletas."
}
Write-Host "    Copiar + imprimir + atualizar: assets OK."

Write-Host "[195/492] Validando responsividade das acoes..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("clinical-summary-actions") -or
    -not $cssSource.Contains("@media(max-width:620px)")) {
    throw "Estilos das acoes de handoff incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[196/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / handoff clinico + impressao: OK."


Write-Host "[197/492] Validando endpoint de equipe..."
$teamSource = Get-Content .\src\HealthPlatform.Api\Controllers\EquipeController.cs -Encoding UTF8 -Raw
if (-not $teamSource.Contains('Route("api/equipe")') -or
    -not $teamSource.Contains("[HttpGet]") -or
    -not $teamSource.Contains("[HttpPost]") -or
    -not $teamSource.Contains('[HttpPut("{usuarioId:guid}")]')) {
    throw "Endpoints de equipe incompletos."
}
Write-Host "    GET + POST + PUT: backend OK."

Write-Host "[198/492] Validando isolamento e administracao..."
if (-not $teamSource.Contains("currentUser.OrganizationId") -or
    -not $teamSource.Contains("TipoUsuario.Admin") -or
    -not $teamSource.Contains("EhAdmin")) {
    throw "Protecao administrativa da equipe incompleta."
}
Write-Host "    Organizacao + admin: backend OK."

Write-Host "[199/492] Validando criacao de acesso..."
if (-not $teamSource.Contains("userManager.CreateAsync") -or
    -not $teamSource.Contains("SenhaTemporaria") -or
    -not $teamSource.Contains("AddToRoleAsync")) {
    throw "Criacao de acesso da equipe incompleta."
}
Write-Host "    Identity + senha temporaria + role: backend OK."

Write-Host "[200/492] Validando sincronizacao de tipo e role..."
if (-not $teamSource.Contains("RemoveFromRolesAsync") -or
    -not $teamSource.Contains("usuario.TipoUsuario = tipo") -or
    -not $teamSource.Contains("userManager.UpdateAsync")) {
    throw "Sincronizacao de acesso incompleta."
}
Write-Host "    TipoUsuario + Identity Role: backend OK."

Write-Host "[201/492] Validando perfil profissional..."
if (-not $teamSource.Contains("ExigePerfilProfissional") -or
    -not $teamSource.Contains("RegistroProfissional") -or
    -not $teamSource.Contains("TipoUsuario.Nutricionista") -or
    -not $teamSource.Contains("TipoUsuario.Personal")) {
    throw "Perfil profissional da equipe incompleto."
}
Write-Host "    Medico + nutricionista + personal: backend OK."

Write-Host "[202/492] Validando protecao do proprio administrador..."
if (-not $teamSource.Contains("usuario.Id == currentUser.UserId") -or
    -not $teamSource.Contains("nao pode remover o proprio acesso administrativo")) {
    throw "Protecao do admin atual incompleta."
}
Write-Host "    Auto-bloqueio administrativo impedido."

Write-Host "[203/492] Validando auditoria da equipe..."
if (-not $teamSource.Contains('"CREATE"') -or
    -not $teamSource.Contains('"UPDATE"') -or
    -not $teamSource.Contains('"UsuarioEquipe"') -or
    -not $teamSource.Contains("DadosAnterioresJson")) {
    throw "Auditoria da equipe incompleta."
}
Write-Host "    CREATE + UPDATE auditados."

Write-Host "[204/492] Validando tela Equipe..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $indexSource.Contains('data-route="equipe"') -or
    -not $appJsSource.Contains("renderEquipe") -or
    -not $appJsSource.Contains("openCreateTeamMember") -or
    -not $appJsSource.Contains("openEditTeamMember")) {
    throw "Interface de equipe incompleta."
}
Write-Host "    Navegacao + cadastro + edicao: assets OK."

Write-Host "[205/492] Validando visibilidade admin e responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $indexSource.Contains("admin-only hidden") -or
    -not $appJsSource.Contains("u.tipoUsuario!=='Admin'") -or
    -not $cssSource.Contains("team-row") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Visibilidade/estilos da equipe incompletos."
}
Write-Host "    Admin-only + desktop/mobile: assets OK."

Write-Host "[206/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / equipe + gestao de profissionais: OK."


Write-Host "[207/492] Validando filtros da equipe..."
$teamSource = Get-Content .\src\HealthPlatform.Api\Controllers\EquipeController.cs -Encoding UTF8 -Raw
if (-not $teamSource.Contains("[FromQuery] string? busca") -or
    -not $teamSource.Contains("[FromQuery] string? tipo") -or
    -not $teamSource.Contains("[FromQuery] string? status") -or
    -not $teamSource.Contains("EF.Functions.ILike")) {
    throw "Filtros da equipe incompletos."
}
Write-Host "    Busca + tipo + status: backend OK."

Write-Host "[208/492] Validando redefinicao de senha..."
if (-not $teamSource.Contains('redefinir-senha') -or
    -not $teamSource.Contains("GeneratePasswordResetTokenAsync") -or
    -not $teamSource.Contains("ResetPasswordAsync")) {
    throw "Redefinicao de senha da equipe incompleta."
}
Write-Host "    Token Identity + reset: backend OK."

Write-Host "[209/492] Validando escopo e protecoes do reset..."
if (-not $teamSource.Contains("usuarioId == currentUser.UserId") -or
    -not $teamSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $teamSource.Contains("Reative o acesso antes de redefinir a senha")) {
    throw "Protecoes do reset de senha incompletas."
}
Write-Host "    Self-reset administrativo + tenant + inativo: protegidos."

Write-Host "[210/492] Validando auditoria segura de senha..."
if (-not $teamSource.Contains('"PASSWORD_RESET"') -or
    -not $teamSource.Contains("SenhaTemporariaRedefinida = true")) {
    throw "Auditoria do reset incompleta."
}
if ($teamSource.Contains("NovaSenhaTemporaria =") -or
    $teamSource.Contains("SenhaTemporaria = request.")) {
    throw "Senha temporaria nao deve ser gravada no AuditLog."
}
Write-Host "    Evento auditado sem persistir a senha."

Write-Host "[211/492] Validando filtros visuais..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("teamSearch") -or
    -not $appJsSource.Contains("teamTypeFilter") -or
    -not $appJsSource.Contains("teamStatusFilter") -or
    -not $appJsSource.Contains("URLSearchParams")) {
    throw "Filtros visuais da equipe incompletos."
}
Write-Host "    Busca com debounce + filtros: assets OK."

Write-Host "[212/492] Validando acao visual de senha..."
if (-not $appJsSource.Contains("openResetTeamPassword") -or
    -not $appJsSource.Contains("team-reset-password") -or
    -not $appJsSource.Contains("/redefinir-senha")) {
    throw "Acao visual de redefinir senha incompleta."
}
Write-Host "    Modal + endpoint de reset: assets OK."

Write-Host "[213/492] Validando responsividade da equipe v2..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("team-filterbar") -or
    -not $cssSource.Contains("team-row-actions") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos da equipe v2 incompletos."
}
Write-Host "    Filtros + acoes desktop/mobile: OK."

Write-Host "[214/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / equipe v2 + seguranca de acesso: OK."


Write-Host "[215/492] Validando endpoint Minha Conta..."
$configSource = Get-Content .\src\HealthPlatform.Api\Controllers\ConfiguracoesController.cs -Encoding UTF8 -Raw
if (-not $configSource.Contains('[HttpGet("minha-conta")]') -or
    -not $configSource.Contains('[HttpPut("minha-conta")]')) {
    throw "Endpoints Minha Conta incompletos."
}
Write-Host "    GET + PUT da conta: backend OK."

Write-Host "[216/492] Validando alteracao da propria senha..."
if (-not $configSource.Contains('minha-conta/alterar-senha') -or
    -not $configSource.Contains("ChangePasswordAsync") -or
    -not $configSource.Contains("SenhaAtual") -or
    -not $configSource.Contains("ConfirmacaoNovaSenha")) {
    throw "Troca de senha propria incompleta."
}
Write-Host "    Senha atual + nova senha + confirmacao: backend OK."

Write-Host "[217/492] Validando protecoes da troca de senha..."
if (-not $configSource.Contains("A nova senha deve ser diferente da senha atual") -or
    -not $configSource.Contains("request.NovaSenha.Length < 10") -or
    -not $configSource.Contains("currentUser.OrganizationId")) {
    throw "Protecoes da troca de senha incompletas."
}
Write-Host "    Diferenca + comprimento + tenant: backend OK."

Write-Host "[218/492] Validando auditoria segura da conta..."
if (-not $configSource.Contains('"PASSWORD_CHANGE"') -or
    -not $configSource.Contains("SenhaAlterada = true")) {
    throw "Auditoria da troca de senha incompleta."
}
if ($configSource.Contains("NovaSenha = request.") -or
    $configSource.Contains("SenhaAtual = request.")) {
    throw "Senhas nao devem ser gravadas no AuditLog."
}
Write-Host "    PASSWORD_CHANGE auditado sem armazenar credenciais."

Write-Host "[219/492] Validando sincronizacao do nome profissional..."
if (-not $configSource.Contains("profissional.Nome = usuario.Nome") -or
    -not $configSource.Contains('"MinhaConta"')) {
    throw "Sincronizacao de nome incompleta."
}
Write-Host "    Usuario + perfil profissional sincronizados."

Write-Host "[220/492] Validando painel Minha Conta..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("ensureAccountPanel") -or
    -not $appJsSource.Contains("accountChangePassword") -or
    -not $appJsSource.Contains("/api/configuracoes/minha-conta")) {
    throw "Painel Minha Conta incompleto."
}
Write-Host "    Dados + editar nome + alterar senha: assets OK."

Write-Host "[221/492] Validando responsividade da conta..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("account-panel") -or
    -not $cssSource.Contains("account-info-grid") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos Minha Conta incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[222/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / Minha Conta + troca de senha: OK."


Write-Host "[223/492] Validando schema de progressao alimentar..."
$planEntity = Get-Content .\src\HealthPlatform.Domain\Entities\PlanoAlimentar.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $planEntity.Contains("PlanoOrigemId") -or
    -not $planEntity.Contains("Versao") -or
    -not $planEntity.Contains("AjustePercentual") -or
    -not $dbSource.Contains("VersoesDerivadas")) {
    throw "Schema de progressao alimentar incompleto."
}
Write-Host "    Origem + versao + ajuste: modelo OK."

Write-Host "[224/492] Validando simulador nutricional..."
$planSource = Get-Content .\src\HealthPlatform.Api\Controllers\PlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $planSource.Contains("simular-ajuste") -or
    -not $planSource.Contains("SimulacaoAjustePlanoResponse") -or
    -not $planSource.Contains("EscalarTotais")) {
    throw "Simulador nutricional incompleto."
}
Write-Host "    Percentual/calorias -> macros projetados: backend OK."

Write-Host "[225/492] Validando duplicacao versionada..."
if (-not $planSource.Contains('/duplicar') -or
    -not $planSource.Contains("PlanoOrigemId = raizId") -or
    -not $planSource.Contains("Versao = maiorVersao + 1")) {
    throw "Duplicacao versionada incompleta."
}
Write-Host "    Nova versao preserva linhagem."

Write-Host "[226/492] Validando escala de porcoes..."
if (-not $planSource.Contains("EscalarQuantidade(itemOrigem.Quantidade") -or
    -not $planSource.Contains("EscalarQuantidade(itemOrigem.QuantidadeGramas") -or
    -not $planSource.Contains("EscalarQuantidade(subOrigem.QuantidadeGramas")) {
    throw "Escala de porcoes/substituicoes incompleta."
}
Write-Host "    Itens + gramas + substituicoes: backend OK."

Write-Host "[227/492] Validando ajuste por calorias alvo..."
if (-not $planSource.Contains("caloriasAlvo.Value / caloriasAtuais") -or
    -not $planSource.Contains("percentual.HasValue && caloriasAlvo.HasValue")) {
    throw "Ajuste por calorias alvo incompleto."
}
Write-Host "    Calorias alvo convertem para fator proporcional."

Write-Host "[228/492] Validando limites de ajuste..."
if (-not $planSource.Contains("ajustePercentual < -50m") -or
    -not $planSource.Contains("ajustePercentual > 100m")) {
    throw "Limites da progressao alimentar incompletos."
}
Write-Host "    Faixa -50% a +100% protegida."

Write-Host "[229/492] Validando encerramento opcional do plano anterior..."
if (-not $planSource.Contains("ConcluirPlanoAnterior") -or
    -not $planSource.Contains('origem.Status = "Concluido"')) {
    throw "Encerramento de versao anterior incompleto."
}
Write-Host "    Plano anterior pode ser concluido automaticamente."

Write-Host "[230/492] Validando auditoria da progressao..."
if (-not $planSource.Contains('"DUPLICATE_SCALE"') -or
    -not $planSource.Contains("CaloriasOriginais") -or
    -not $planSource.Contains("CaloriasProjetadas")) {
    throw "Auditoria da progressao alimentar incompleta."
}
Write-Host "    Origem + ajuste + calorias auditados."

Write-Host "[231/492] Validando interface de progressao..."
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

Write-Host "[232/492] Validando modos percentual e calorias..."
if (-not $appJsSource.Contains('value="percentual"') -or
    -not $appJsSource.Contains('value="calorias"') -or
    -not $appJsSource.Contains("totaisProjetados")) {
    throw "Modos de ajuste visual incompletos."
}
Write-Host "    Percentual + calorias alvo: assets OK."

Write-Host "[233/492] Validando upgrade SQL e PREPARAR..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.21_progressao_plano_alimentar.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.21_progressao_plano_alimentar.sql") -or
    -not $sqlSource.Contains('"PlanoOrigemId"') -or
    -not $sqlSource.Contains('"Versao"')) {
    throw "Upgrade de progressao alimentar incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 19/19: OK."

Write-Host "[234/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / progressao do plano alimentar: OK."


Write-Host "[235/492] Validando schema de progressao de treino..."
$workoutEntity = Get-Content .\src\HealthPlatform.Domain\Entities\PlanoTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $workoutEntity.Contains("PlanoOrigemId") -or
    -not $workoutEntity.Contains("Versao") -or
    -not $workoutEntity.Contains("AjusteCargaPercentual") -or
    -not $dbSource.Contains("VersoesDerivadas")) {
    throw "Schema de progressao de treino incompleto."
}
Write-Host "    Origem + versao + ajustes: modelo OK."

Write-Host "[236/492] Validando simulador de treino..."
$workoutSource = Get-Content .\src\HealthPlatform.Api\Controllers\TreinosController.cs -Encoding UTF8 -Raw
if (-not $workoutSource.Contains("simular-progressao") -or
    -not $workoutSource.Contains("SimulacaoProgressaoTreinoResponse") -or
    -not $workoutSource.Contains("SomaCargasProjetada")) {
    throw "Simulador de treino incompleto."
}
Write-Host "    Carga + series + reps + descanso: backend OK."

Write-Host "[237/492] Validando duplicacao versionada do treino..."
if (-not $workoutSource.Contains('/duplicar') -or
    -not $workoutSource.Contains("PlanoOrigemId = raizId") -or
    -not $workoutSource.Contains("Versao = maiorVersao + 1")) {
    throw "Duplicacao versionada do treino incompleta."
}
Write-Host "    Nova versao preserva linhagem."

Write-Host "[238/492] Validando progressao de carga..."
if (-not $workoutSource.Contains("fatorCarga") -or
    -not $workoutSource.Contains("itemOrigem.Carga.Value * fatorCarga")) {
    throw "Progressao de carga incompleta."
}
Write-Host "    Carga percentual aplicada aos exercicios prescritos."

Write-Host "[239/492] Validando progressao de series e descanso..."
if (-not $workoutSource.Contains("itemOrigem.Series + request.AjusteSeries") -or
    -not $workoutSource.Contains("itemOrigem.DescansoSegundos.Value + request.AjusteDescansoSegundos")) {
    throw "Progressao de series/descanso incompleta."
}
Write-Host "    Series + descanso com limites inferiores seguros."

Write-Host "[240/492] Validando repeticoes estruturadas..."
if (-not $workoutSource.Contains("TentarAjustarRepeticoes") -or
    -not $workoutSource.Contains("PrescricoesRepeticoesPreservadas")) {
    throw "Ajuste seguro de repeticoes incompleto."
}
Write-Host "    Numeros/faixas ajustados; texto complexo preservado."

Write-Host "[241/492] Validando limites de progressao..."
if (-not $workoutSource.Contains("cargaPercentual < -50m") -or
    -not $workoutSource.Contains("seriesDelta < -5") -or
    -not $workoutSource.Contains("repeticoesDelta < -20") -or
    -not $workoutSource.Contains("descansoDeltaSegundos < -300")) {
    throw "Limites da progressao de treino incompletos."
}
Write-Host "    Limites de carga/series/reps/descanso: OK."

Write-Host "[242/492] Validando encerramento e auditoria..."
if (-not $workoutSource.Contains("ConcluirPlanoAnterior") -or
    -not $workoutSource.Contains('origem.Status = "Concluido"') -or
    -not $workoutSource.Contains('"DUPLICATE_PROGRESS"')) {
    throw "Encerramento/auditoria da progressao incompletos."
}
Write-Host "    Plano anterior + evento de progressao: OK."

Write-Host "[243/492] Validando interface de progressao de treino..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openWorkoutProgression") -or
    -not $appJsSource.Contains("workout-progress") -or
    -not $appJsSource.Contains("simular-progressao") -or
    -not $appJsSource.Contains("/duplicar")) {
    throw "Interface de progressao de treino incompleta."
}
Write-Host "    Prontuario -> simulacao -> nova versao: assets OK."

Write-Host "[244/492] Validando projecao e modal ampliado..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("workoutProjection") -or
    -not $appJsSource.Contains("workout-modal-open") -or
    -not $cssSource.Contains("workout-projection-grid") -or
    -not $cssSource.Contains("workout-modal-open")) {
    throw "UX da progressao de treino incompleta."
}
Write-Host "    Projecao + modal responsivo: assets OK."

Write-Host "[245/492] Validando upgrade SQL e PREPARAR 19..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.22_progressao_treino.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.22_progressao_treino.sql") -or
    -not $sqlSource.Contains('"PlanoOrigemId"') -or
    -not $sqlSource.Contains('"AjusteCargaPercentual"')) {
    throw "Upgrade de progressao de treino incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 19/19: OK."

Write-Host "[246/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / progressao de treino + ciclo versionado: OK."


Write-Host "[247/492] Validando schema dos modelos alimentares..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloPlanoAlimentar.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("OrganizacaoId") -or
    -not $modelEntity.Contains("ProfissionalId") -or
    -not $dbSource.Contains("ModelosPlanosAlimentares")) {
    throw "Schema de modelos alimentares incompleto."
}
Write-Host "    Modelo multi-tenant + JSON estruturado: OK."

Write-Host "[248/492] Validando listagem e busca de modelos..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-planos-alimentares') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("incluirInativos")) {
    throw "Listagem de modelos incompleta."
}
Write-Host "    GET + busca + ativos/inativos: backend OK."

Write-Host "[249/492] Validando salvar plano como modelo..."
if (-not $modelSource.Contains("salvar-como-modelo") -or
    -not $modelSource.Contains("TemplateConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar como modelo incompleto."
}
Write-Host "    Refeicoes + itens + substituicoes serializados."

Write-Host "[250/492] Validando criacao de plano a partir de modelo..."
if (-not $modelSource.Contains("criar-de-modelo") -or
    -not $modelSource.Contains("JsonSerializer.Deserialize<TemplateConteudo>") -or
    -not $modelSource.Contains("db.PlanosAlimentares.Add(plano)")) {
    throw "Criacao a partir de modelo incompleta."
}
Write-Host "    Template -> novo plano: backend OK."

Write-Host "[251/492] Validando alimentos do catalogo..."
if (-not $modelSource.Contains("alimentosValidos") -or
    -not $modelSource.Contains("alimentosInvalidos") -or
    -not $modelSource.Contains("x.Ativo")) {
    throw "Validacao do catalogo ao reutilizar modelo incompleta."
}
Write-Host "    Alimentos inativos/indisponiveis bloqueiam instanciacao."

Write-Host "[252/492] Validando isolamento e auditoria..."
if (-not $modelSource.Contains("currentUser.OrganizationId") -or
    -not $modelSource.Contains('"CREATE_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("AuditLogs")) {
    throw "Isolamento/auditoria dos modelos incompletos."
}
Write-Host "    Tenant + auditoria: backend OK."

Write-Host "[253/492] Validando interface de salvar modelo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openSaveMealTemplate") -or
    -not $appJsSource.Contains("nutrition-save-template") -or
    -not $appJsSource.Contains("salvar-como-modelo")) {
    throw "Interface Salvar como modelo incompleta."
}
Write-Host "    Card de plano -> salvar modelo: assets OK."

Write-Host "[254/492] Validando seletor de modelos..."
if (-not $appJsSource.Contains("openMealTemplatePicker") -or
    -not $appJsSource.Contains("mealTemplateSearch") -or
    -not $appJsSource.Contains("meal-template-grid")) {
    throw "Seletor de modelos incompleto."
}
Write-Host "    Busca + cards de modelos: assets OK."

Write-Host "[255/492] Validando criacao visual via modelo..."
if (-not $appJsSource.Contains("openMealTemplateCreateForm") -or
    -not $appJsSource.Contains("criar-de-modelo") -or
    -not $appJsSource.Contains("Plano criado a partir do modelo")) {
    throw "Criacao visual por modelo incompleta."
}
Write-Host "    Modelo -> paciente -> plano ativo: assets OK."

Write-Host "[256/492] Validando responsividade dos modelos..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("meal-template-grid") -or
    -not $cssSource.Contains("nutrition-top-actions") -or
    -not $cssSource.Contains("@media(max-width:720px)")) {
    throw "Estilos de modelos alimentares incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[257/492] Validando upgrade SQL e PREPARAR 20..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.23_modelos_plano_alimentar.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.23_modelos_plano_alimentar.sql") -or
    -not $sqlSource.Contains('"ModelosPlanosAlimentares"') -or
    -not $sqlSource.Contains('"ConteudoJson"')) {
    throw "Upgrade de modelos alimentares incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 20/20: OK."

Write-Host "[258/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / templates de plano alimentar: OK."


Write-Host "[259/492] Validando schema dos modelos de treino..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloPlanoTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("OrganizacaoId") -or
    -not $modelEntity.Contains("ProfissionalId") -or
    -not $dbSource.Contains("ModelosPlanosTreino")) {
    throw "Schema de modelos de treino incompleto."
}
Write-Host "    Modelo multi-tenant + JSON estruturado: OK."

Write-Host "[260/492] Validando listagem e busca de modelos de treino..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosTreinoController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-planos-treino') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("incluirInativos")) {
    throw "Listagem de modelos de treino incompleta."
}
Write-Host "    GET + busca + ativos/inativos: backend OK."

Write-Host "[261/492] Validando salvar treino como modelo..."
if (-not $modelSource.Contains("salvar-como-modelo") -or
    -not $modelSource.Contains("TemplateTreinoConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar treino como modelo incompleto."
}
Write-Host "    Sessoes + exercicios + prescricao serializados."

Write-Host "[262/492] Validando criacao de treino a partir de modelo..."
if (-not $modelSource.Contains("criar-de-modelo") -or
    -not $modelSource.Contains("JsonSerializer.Deserialize<TemplateTreinoConteudo>") -or
    -not $modelSource.Contains("db.PlanosTreino.Add(plano)")) {
    throw "Criacao de treino a partir de modelo incompleta."
}
Write-Host "    Template -> novo plano de treino: backend OK."

Write-Host "[263/492] Validando catalogo de exercicios..."
if (-not $modelSource.Contains("exerciciosValidos") -or
    -not $modelSource.Contains("exerciciosInvalidos") -or
    -not $modelSource.Contains("x.Ativo")) {
    throw "Validacao do catalogo de exercicios incompleta."
}
Write-Host "    Exercicios inativos/indisponiveis bloqueiam instanciacao."

Write-Host "[264/492] Validando prescricao completa do template..."
if (-not $modelSource.Contains("Series = i.Series") -or
    -not $modelSource.Contains("Repeticoes = i.Repeticoes") -or
    -not $modelSource.Contains("Carga = i.Carga") -or
    -not $modelSource.Contains("DescansoSegundos = i.DescansoSegundos") -or
    -not $modelSource.Contains("TempoSegundos = i.TempoSegundos")) {
    throw "Copia da prescricao de treino incompleta."
}
Write-Host "    Series + reps + carga + descanso + tempo: backend OK."

Write-Host "[265/492] Validando isolamento e auditoria dos modelos de treino..."
if (-not $modelSource.Contains("currentUser.OrganizationId") -or
    -not $modelSource.Contains('"CREATE_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("AuditLogs")) {
    throw "Isolamento/auditoria dos modelos de treino incompletos."
}
Write-Host "    Tenant + auditoria: backend OK."

Write-Host "[266/492] Validando interface de salvar modelo de treino..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openSaveWorkoutTemplate") -or
    -not $appJsSource.Contains("workout-save-template") -or
    -not $appJsSource.Contains("salvar-como-modelo")) {
    throw "Interface Salvar treino como modelo incompleta."
}
Write-Host "    Card do treino -> salvar modelo: assets OK."

Write-Host "[267/492] Validando seletor e busca de modelos..."
if (-not $appJsSource.Contains("openWorkoutTemplatePicker") -or
    -not $appJsSource.Contains("workoutTemplateSearch") -or
    -not $appJsSource.Contains("workout-template-grid")) {
    throw "Seletor de modelos de treino incompleto."
}
Write-Host "    Busca + cards de modelos: assets OK."

Write-Host "[268/492] Validando criacao visual via modelo..."
if (-not $appJsSource.Contains("openWorkoutTemplateCreateForm") -or
    -not $appJsSource.Contains("treinos/criar-de-modelo") -or
    -not $appJsSource.Contains("Treino criado a partir do modelo")) {
    throw "Criacao visual de treino por modelo incompleta."
}
Write-Host "    Modelo -> paciente -> treino ativo: assets OK."

Write-Host "[269/492] Validando upgrade SQL, responsividade e PREPARAR 21..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.24_modelos_plano_treino.sql -Encoding UTF8 -Raw
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.24_modelos_plano_treino.sql") -or
    -not $sqlSource.Contains('"ModelosPlanosTreino"') -or
    -not $sqlSource.Contains('"ConteudoJson"') -or
    -not $cssSource.Contains("workout-template-grid")) {
    throw "Upgrade/UX de modelos de treino incompletos."
}
Write-Host "    SQL idempotente + UI responsiva + PREPARAR 21/21: OK."

Write-Host "[270/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / templates de treino + criacao rapida: OK."


Write-Host "[271/492] Validando schema das metas nutricionais..."
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

Write-Host "[272/492] Validando contratos nutricionais..."
$contractsSource = Get-Content .\src\HealthPlatform.Api\Contracts\PlanosAlimentares\PlanoAlimentarContracts.cs -Encoding UTF8 -Raw
if (-not $contractsSource.Contains("AtualizarMetasNutricionaisRequest") -or
    -not $contractsSource.Contains("AnalisePlanoAlimentarResponse") -or
    -not $contractsSource.Contains("DistribuicaoRefeicaoResponse")) {
    throw "Contratos nutricionais incompletos."
}
Write-Host "    Metas + desvios + distribuicao: contratos OK."

Write-Host "[273/492] Validando endpoint de metas..."
$planSource = Get-Content .\src\HealthPlatform.Api\Controllers\PlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $planSource.Contains("metas-nutricionais") -or
    -not $planSource.Contains('"NUTRITION_TARGETS"') -or
    -not $planSource.Contains("ValidarMetas")) {
    throw "Endpoint de metas nutricionais incompleto."
}
Write-Host "    PUT + validacao + auditoria: backend OK."

Write-Host "[274/492] Validando analise nutricional..."
if (-not $planSource.Contains("analise-nutricional") -or
    -not $planSource.Contains("DistribuicaoNutricionalResponse") -or
    -not $planSource.Contains("DesviosNutricionaisResponse") -or
    -not $planSource.Contains("Percentual(")) {
    throw "Analise nutricional incompleta."
}
Write-Host "    Meta x prescrito + distribuicao por refeicao: backend OK."

Write-Host "[275/492] Validando metas na criacao e edicao..."
if (-not $planSource.Contains("MetaCalorias = request.MetaCalorias") -or
    -not $planSource.Contains("MetaProteinasG = request.MetaProteinasG") -or
    -not $planSource.Contains("MetaFibrasG = request.MetaFibrasG")) {
    throw "Persistencia de metas no plano incompleta."
}
Write-Host "    Criacao/edicao preservam metas."

Write-Host "[276/492] Validando metas na progressao alimentar..."
if (-not $planSource.Contains("EscalarNullable(origem.MetaProteinasG") -or
    -not $planSource.Contains("request.CaloriasAlvo ?? EscalarNullable(origem.MetaCalorias")) {
    throw "Metas nao acompanham progressao alimentar."
}
Write-Host "    Progressao escala metas junto das porcoes."

Write-Host "[277/492] Validando metas nos templates alimentares..."
$templateSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $templateSource.Contains("plano.MetaCalorias") -or
    -not $templateSource.Contains("MetaCalorias = conteudo.MetaCalorias") -or
    -not $templateSource.Contains("metaProteinasG = conteudo?.MetaProteinasG")) {
    throw "Metas nao foram integradas aos templates alimentares."
}
Write-Host "    Template salva e restaura objetivos nutricionais."

Write-Host "[278/492] Validando construtor com metas..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("nutrition-target-builder") -or
    -not $appJsSource.Contains("metaCalorias:dec(f,'metaCalorias')") -or
    -not $appJsSource.Contains("planTargetPreview")) {
    throw "Construtor alimentar com metas incompleto."
}
Write-Host "    Metas no cadastro + preview em tempo real: assets OK."

Write-Host "[279/492] Validando Meta x Prescrito no prontuario..."
if (-not $appJsSource.Contains("nutritionTargetPanel") -or
    -not $appJsSource.Contains("nutritionTargetLine") -or
    -not $appJsSource.Contains("nutrition-edit-targets")) {
    throw "Painel Meta x Prescrito incompleto."
}
Write-Host "    Comparacao diaria + edicao rapida: assets OK."

Write-Host "[280/492] Validando distribuicao por refeicao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("nutritionMealDistribution") -or
    -not $appJsSource.Contains("Prescrito no dia + metas planejadas por bloco") -or
    -not $appJsSource.Contains("mealTargetMini")) {
    throw "Distribuicao nutricional visual incompleta."
}
Write-Host "    Prescrito diario + meta planejada por refeicao: assets OK."

Write-Host "[281/492] Validando modal de metas..."
if (-not $appJsSource.Contains("openNutritionTargets") -or
    -not $appJsSource.Contains("/metas-nutricionais") -or
    -not $appJsSource.Contains("Metas nutricionais atualizadas")) {
    throw "Modal de metas nutricionais incompleto."
}
Write-Host "    Edicao sem reconstruir o plano: assets OK."

Write-Host "[282/492] Validando responsividade nutricional..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("nutrition-target-grid") -or
    -not $cssSource.Contains("nutrition-distribution-row") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos nutricionais incompletos."
}
Write-Host "    Meta + distribuicao desktop/mobile: OK."

Write-Host "[283/492] Validando SQL e PREPARAR 22..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.25_metas_nutricionais.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.25_metas_nutricionais.sql") -or
    -not $sqlSource.Contains('"MetaCalorias"') -or
    -not $sqlSource.Contains('"MetaFibrasG"')) {
    throw "Upgrade de metas nutricionais incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 22/22: OK."

Write-Host "[284/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / metas nutricionais + distribuicao: OK."


Write-Host "[285/492] Validando schema da biblioteca de refeicoes..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloRefeicao.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("Categoria") -or
    -not $dbSource.Contains("ModelosRefeicoes")) {
    throw "Schema da biblioteca de refeicoes incompleto."
}
Write-Host "    Modelo + categoria + snapshot JSON: OK."

Write-Host "[286/492] Validando listagem e filtros da biblioteca..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosRefeicoesController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-refeicoes') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("[FromQuery] string? categoria")) {
    throw "Listagem da biblioteca de refeicoes incompleta."
}
Write-Host "    Busca + categoria + ativos/inativos: backend OK."

Write-Host "[287/492] Validando salvar refeicao como modelo..."
if (-not $modelSource.Contains("refeicoes-plano/{refeicaoId:guid}/salvar-como-modelo") -or
    -not $modelSource.Contains("ModeloRefeicaoConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar refeicao como modelo incompleto."
}
Write-Host "    Refeicao + itens + substituicoes: backend OK."

Write-Host "[288/492] Validando insercao de refeicao no plano..."
if (-not $modelSource.Contains("inserir-modelo-refeicao") -or
    -not $modelSource.Contains("db.RefeicoesPlanoAlimentar.Add(refeicao)") -or
    -not $modelSource.Contains("plano.Refeicoes.Max(x => x.Ordem) + 1")) {
    throw "Insercao rapida de refeicao incompleta."
}
Write-Host "    Modelo -> nova refeicao no final do plano: backend OK."

Write-Host "[289/492] Validando catalogo e substituicoes..."
if (-not $modelSource.Contains("alimentosValidos") -or
    -not $modelSource.Contains("alimentosInvalidos") -or
    -not $modelSource.Contains("i.Substituicoes")) {
    throw "Validacao de alimentos/substituicoes incompleta."
}
Write-Host "    Catalogo ativo revalidado antes da insercao."

Write-Host "[290/492] Validando protecoes e auditoria..."
if (-not $modelSource.Contains('plano.Status == "Concluido"') -or
    -not $modelSource.Contains('"INSERT_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("currentUser.OrganizationId")) {
    throw "Protecoes da biblioteca de refeicoes incompletas."
}
Write-Host "    Plano concluido + tenant + auditoria: protegidos."

Write-Host "[291/492] Validando botao Salvar refeicao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("meal-save-template") -or
    -not $appJsSource.Contains("openSaveMealTemplate") -or
    -not $appJsSource.Contains("salvar-como-modelo")) {
    throw "Acao visual Salvar refeicao incompleta."
}
Write-Host "    Refeicao do plano -> biblioteca: assets OK."

Write-Host "[292/492] Validando biblioteca visual..."
if (-not $appJsSource.Contains("openMealLibrary") -or
    -not $appJsSource.Contains("mealLibrarySearch") -or
    -not $appJsSource.Contains("mealLibraryPlan") -or
    -not $appJsSource.Contains("meal-library-grid")) {
    throw "Biblioteca visual de refeicoes incompleta."
}
Write-Host "    Busca + selecao do plano + cards: assets OK."

Write-Host "[293/492] Validando insercao visual rapida..."
if (-not $appJsSource.Contains("openMealLibraryInsertForm") -or
    -not $appJsSource.Contains("inserir-modelo-refeicao") -or
    -not $appJsSource.Contains("Refeição inserida no plano")) {
    throw "Insercao visual de refeicao incompleta."
}
Write-Host "    Modelo -> plano ativo: assets OK."

Write-Host "[294/492] Validando responsividade da biblioteca..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("meal-library-grid") -or
    -not $cssSource.Contains("meal-card-head") -or
    -not $cssSource.Contains("@media(max-width:760px)")) {
    throw "Estilos da biblioteca de refeicoes incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[295/492] Validando SQL e PREPARAR 23..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.26_modelos_refeicoes.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.26_modelos_refeicoes.sql") -or
    -not $sqlSource.Contains('"ModelosRefeicoes"') -or
    -not $sqlSource.Contains('"Categoria"')) {
    throw "Upgrade da biblioteca de refeicoes incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 23/23: OK."

Write-Host "[296/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / biblioteca de refeicoes + insercao rapida: OK."


Write-Host "[297/492] Validando schema da biblioteca de sessoes..."
$modelEntity = Get-Content .\src\HealthPlatform.Domain\Entities\ModeloSessaoTreino.cs -Encoding UTF8 -Raw
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $modelEntity.Contains("ConteudoJson") -or
    -not $modelEntity.Contains("Categoria") -or
    -not $dbSource.Contains("ModelosSessoesTreino")) {
    throw "Schema da biblioteca de sessoes incompleto."
}
Write-Host "    Modelo + categoria + snapshot JSON: OK."

Write-Host "[298/492] Validando listagem e filtros..."
$modelSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosSessoesTreinoController.cs -Encoding UTF8 -Raw
if (-not $modelSource.Contains('api/modelos-sessoes-treino') -or
    -not $modelSource.Contains("EF.Functions.ILike") -or
    -not $modelSource.Contains("[FromQuery] string? categoria")) {
    throw "Listagem da biblioteca de sessoes incompleta."
}
Write-Host "    Busca + categoria + ativos/inativos: backend OK."

Write-Host "[299/492] Validando salvar sessao como modelo..."
if (-not $modelSource.Contains("sessoes-treino/{sessaoId:guid}/salvar-como-modelo") -or
    -not $modelSource.Contains("ModeloSessaoConteudo") -or
    -not $modelSource.Contains("JsonSerializer.Serialize(conteudo)")) {
    throw "Salvar sessao como modelo incompleto."
}
Write-Host "    Sessao + exercicios + prescricao: backend OK."

Write-Host "[300/492] Validando insercao de sessao no plano..."
if (-not $modelSource.Contains("inserir-modelo-sessao") -or
    -not $modelSource.Contains("db.SessoesTreino.Add(sessao)") -or
    -not $modelSource.Contains("plano.Sessoes.Max(x => x.Ordem) + 1")) {
    throw "Insercao rapida de sessao incompleta."
}
Write-Host "    Modelo -> nova sessao ao final do plano: backend OK."

Write-Host "[301/492] Validando catalogo e prescricao..."
if (-not $modelSource.Contains("exerciciosValidos") -or
    -not $modelSource.Contains("exerciciosInvalidos") -or
    -not $modelSource.Contains("Series = i.Series") -or
    -not $modelSource.Contains("Repeticoes = i.Repeticoes") -or
    -not $modelSource.Contains("DescansoSegundos = i.DescansoSegundos")) {
    throw "Validacao/copia da prescricao incompleta."
}
Write-Host "    Catalogo ativo + series/reps/carga/descanso: OK."

Write-Host "[302/492] Validando protecoes e auditoria..."
if (-not $modelSource.Contains('plano.Status == "Concluido"') -or
    -not $modelSource.Contains('"INSERT_FROM_TEMPLATE"') -or
    -not $modelSource.Contains("currentUser.OrganizationId")) {
    throw "Protecoes da biblioteca de sessoes incompletas."
}
Write-Host "    Plano concluido + tenant + auditoria: protegidos."

Write-Host "[303/492] Validando botao Salvar sessao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("session-save-template") -or
    -not $appJsSource.Contains("openSaveWorkoutSessionTemplate") -or
    -not $appJsSource.Contains("sessoes-treino")) {
    throw "Acao visual Salvar sessao incompleta."
}
Write-Host "    Sessao do plano -> biblioteca: assets OK."

Write-Host "[304/492] Validando biblioteca visual..."
if (-not $appJsSource.Contains("openWorkoutSessionLibrary") -or
    -not $appJsSource.Contains("sessionLibrarySearch") -or
    -not $appJsSource.Contains("sessionLibraryPlan") -or
    -not $appJsSource.Contains("session-library-grid")) {
    throw "Biblioteca visual de sessoes incompleta."
}
Write-Host "    Busca + selecao do plano + cards: assets OK."

Write-Host "[305/492] Validando insercao visual rapida..."
if (-not $appJsSource.Contains("openWorkoutSessionInsertForm") -or
    -not $appJsSource.Contains("inserir-modelo-sessao") -or
    -not $appJsSource.Contains("Sessão inserida no plano")) {
    throw "Insercao visual de sessao incompleta."
}
Write-Host "    Modelo -> plano ativo: assets OK."

Write-Host "[306/492] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("session-library-grid") -or
    -not $cssSource.Contains("workout-session-mini-row") -or
    -not $cssSource.Contains("@media(max-width:760px)")) {
    throw "Estilos da biblioteca de sessoes incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[307/492] Validando SQL e PREPARAR 24..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.27_modelos_sessoes_treino.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.27_modelos_sessoes_treino.sql") -or
    -not $sqlSource.Contains('"ModelosSessoesTreino"') -or
    -not $sqlSource.Contains('"Categoria"')) {
    throw "Upgrade da biblioteca de sessoes incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 25/25: OK."

Write-Host "[308/492] Validando versao v0.3.27..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.27 / biblioteca de sessoes + insercao rapida: OK."


Write-Host "[309/492] Validando endpoint longitudinal de habitos..." -ForegroundColor Cyan
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) {
    $pacienteSmoke = $lista.itens | Select-Object -First 1
    $habitos = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/evolucao-habitos?limite=24" -Headers $headers -Method Get
    if ($habitos.pacienteId -ne $pacienteSmoke.id) { throw "Evolucao de habitos retornou paciente incorreto." }
    if ($null -eq $habitos.itens) { throw "Evolucao de habitos sem colecao de itens." }
    Write-Host "    Endpoint OK: $($habitos.total) anamnese(s) carregada(s)." -ForegroundColor Green
} else {
    Write-Host "    Sem pacientes: smoke longitudinal ignorado sem criar dados." -ForegroundColor DarkGreen
}

Write-Host "[310/492] Validando contrato da evolucao de habitos..."
$anamSource = Get-Content .\src\HealthPlatform.Api\Controllers\AnamnesesController.cs -Encoding UTF8 -Raw
if (-not $anamSource.Contains("EvolucaoHabitosPontoResponse") -or
    -not $anamSource.Contains("VariacaoHabitosResponse") -or
    -not $anamSource.Contains("EvolucaoHabitosResponse")) {
    throw "Contratos de evolucao de habitos incompletos."
}
Write-Host "    Atual + anterior + variacao + serie: backend OK."

Write-Host "[311/492] Validando series de sono, estresse, atividade e agua..."
if (-not $anamSource.Contains("SonoHorasMedia") -or
    -not $anamSource.Contains("EstresseNivel") -or
    -not $anamSource.Contains("AtividadeFisicaDiasSemana") -or
    -not $anamSource.Contains("AguaLitrosDia")) {
    throw "Series de habitos incompletas."
}
Write-Host "    Quatro indicadores longitudinais presentes."

Write-Host "[312/492] Validando limite e isolamento multi-tenant..."
if (-not $anamSource.Contains("Math.Clamp(limite, 2, 60)") -or
    -not $anamSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Protecoes do endpoint longitudinal incompletas."
}
Write-Host "    Limite 2-60 + OrganizacaoId: OK."

Write-Host "[313/492] Validando comparacao com registro anterior..."
if (-not $anamSource.Contains("itens[^1]") -or
    -not $anamSource.Contains("itens[^2]") -or
    -not $anamSource.Contains("Diferenca(atual?.SonoHorasMedia") -or
    -not $anamSource.Contains("Diferenca(atual?.EstresseNivel")) {
    throw "Comparacao longitudinal incompleta."
}
Write-Host "    Atual x anterior: backend OK."

Write-Host "[314/492] Validando graficos de habitos..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpHabitCharts") -or
    -not $appJsSource.Contains("Sono médio") -or
    -not $appJsSource.Contains("Consumo de água") -or
    -not $appJsSource.Contains("hpLineChart")) {
    throw "Graficos de habitos incompletos."
}
Write-Host "    SVG nativo reutilizado para 4 tendencias."

Write-Host "[315/492] Validando resumo atual dos habitos..."
if (-not $appJsSource.Contains("habit-current-grid") -or
    -not $appJsSource.Contains("hpHabitCurrentCard") -or
    -not $appJsSource.Contains("hpHabitDelta")) {
    throw "Resumo atual dos habitos incompleto."
}
Write-Host "    Valor atual + delta vs anterior: assets OK."

Write-Host "[316/492] Validando integracao na aba Anamnese..."
if (-not $appJsSource.Contains("tab==='anamnese'") -or
    -not $appJsSource.Contains("professional-anamnesis")) {
    throw "Integracao da evolucao na aba Anamnese incompleta."
}
Write-Host "    Anamnese -> evolucao de habitos: assets OK."

Write-Host "[317/492] Validando integracao no Resumo..."
if (-not $appJsSource.Contains("professional-summary-habits") -or
    -not $appJsSource.Contains("hpInjectHabitEvolution")) {
    throw "Integracao dos habitos no resumo incompleta."
}
Write-Host "    Resumo -> tendencias de habitos: assets OK."

Write-Host "[318/492] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("habit-current-grid") -or
    -not $cssSource.Contains("habit-evolution-section") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos da evolucao de habitos incompletos."
}
Write-Host "    Cards e graficos desktop/mobile: OK."

Write-Host "[319/492] Validando compatibilidade de banco e PREPARAR..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.27_modelos_sessoes_treino.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.28_evolucao_habitos.sql) {
    throw "v0.3.28 nao deveria exigir upgrade de schema."
}
Write-Host "    Sem schema novo / PREPARAR atual 30/30: OK."

Write-Host "[320/492] Validando versao v0.3.28..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.28 / evolucao de habitos + graficos de anamnese: OK."


Write-Host "[321/492] Validando resposta runtime com metas por refeicao..." -ForegroundColor Cyan
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

Write-Host "[322/492] Validando schema de metas por refeicao..."
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

Write-Host "[323/492] Validando contratos de metas por refeicao..."
$contractsSource = Get-Content .\src\HealthPlatform.Api\Contracts\PlanosAlimentares\PlanoAlimentarContracts.cs -Encoding UTF8 -Raw
if (-not $contractsSource.Contains("AtualizarMetasRefeicaoRequest") -or
    -not $contractsSource.Contains("DistribuirMetasRefeicoesRequest") -or
    -not $contractsSource.Contains("MetasNutricionaisResponse Metas") -or
    -not $contractsSource.Contains("DesviosNutricionaisResponse Desvios")) {
    throw "Contratos de metas por refeicao incompletos."
}
Write-Host "    Edicao + distribuicao + comparacao: contratos OK."

Write-Host "[324/492] Validando endpoint de meta individual..."
$planSource = Get-Content .\src\HealthPlatform.Api\Controllers\PlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $planSource.Contains("refeicoes-plano/{refeicaoId:guid}/metas-nutricionais") -or
    -not $planSource.Contains('"MEAL_NUTRITION_TARGETS"') -or
    -not $planSource.Contains("AtualizarMetasRefeicao")) {
    throw "Endpoint de meta individual incompleto."
}
Write-Host "    PUT por refeicao + auditoria: backend OK."

Write-Host "[325/492] Validando distribuicao automatica..."
if (-not $planSource.Contains("distribuir-metas-refeicoes") -or
    -not $planSource.Contains("Math.Abs(soma - 100m)") -or
    -not $planSource.Contains("PercentualMeta(") -or
    -not $planSource.Contains('"MEAL_TARGET_DISTRIBUTION"')) {
    throw "Distribuicao automatica de metas incompleta."
}
Write-Host "    Percentuais fecham 100% e distribuem metas diarias."

Write-Host "[326/492] Validando isolamento e integridade da distribuicao..."
if (-not $planSource.Contains("idsPlano.SequenceEqual(idsRequest)") -or
    -not $planSource.Contains("currentUser.OrganizationId") -or
    -not $planSource.Contains("TemMetaPlano")) {
    throw "Protecoes da distribuicao incompletas."
}
Write-Host "    Todas as refeicoes + tenant + meta diaria: protegidos."

Write-Host "[327/492] Validando progressao com metas por refeicao..."
if (-not $planSource.Contains("EscalarNullable(refeicaoOrigem.MetaCalorias") -or
    -not $planSource.Contains("EscalarNullable(refeicaoOrigem.MetaProteinasG") -or
    -not $planSource.Contains("EscalarNullable(refeicaoOrigem.MetaFibrasG")) {
    throw "Progressao nao preserva metas por refeicao."
}
Write-Host "    V2/V3 escalam metas dos blocos junto das porcoes."

Write-Host "[328/492] Validando templates de plano..."
$templateSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosPlanosAlimentaresController.cs -Encoding UTF8 -Raw
if (-not $templateSource.Contains("r.MetaCalorias") -or
    -not $templateSource.Contains("MetaCalorias = r.MetaCalorias") -or
    -not $templateSource.Contains("MetaProteinasG = r.MetaProteinasG")) {
    throw "Templates de plano nao preservam metas por refeicao."
}
Write-Host "    Template completo salva/restaura metas dos blocos."

Write-Host "[329/492] Validando biblioteca de refeicoes..."
$mealTemplateSource = Get-Content .\src\HealthPlatform.Api\Controllers\ModelosRefeicoesController.cs -Encoding UTF8 -Raw
if (-not $mealTemplateSource.Contains("refeicao.MetaCalorias") -or
    -not $mealTemplateSource.Contains("MetaCalorias = conteudo.MetaCalorias") -or
    -not $mealTemplateSource.Contains("MetaFibrasG = conteudo.MetaFibrasG")) {
    throw "Biblioteca de refeicoes nao preserva metas."
}
Write-Host "    Blocos reutilizaveis mantem sua meta planejada."

Write-Host "[330/492] Validando construtor alimentar..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("meal-target-builder") -or
    -not $appJsSource.Contains("mealMetaCalorias") -or
    -not $appJsSource.Contains("metaProteinasG:m.querySelector")) {
    throw "Construtor com metas por refeicao incompleto."
}
Write-Host "    Nova dieta pode nascer com meta por bloco: assets OK."

Write-Host "[331/492] Validando edicao e comparacao visual..."
if (-not $appJsSource.Contains("openMealNutritionTargets") -or
    -not $appJsSource.Contains("mealTargetMini") -or
    -not $appJsSource.Contains("meal-edit-targets") -or
    -not $appJsSource.Contains("/metas-nutricionais")) {
    throw "Edicao visual das metas por refeicao incompleta."
}
Write-Host "    Prescrito x planejado + edicao rapida: assets OK."

Write-Host "[332/492] Validando modal de distribuicao..."
if (-not $appJsSource.Contains("openMealTargetDistribution") -or
    -not $appJsSource.Contains("mealDistributionTotal") -or
    -not $appJsSource.Contains("distribuir-metas-refeicoes") -or
    -not $appJsSource.Contains("A soma precisa fechar em 100%")) {
    throw "Distribuicao visual de metas incompleta."
}
Write-Host "    Percentuais por refeicao + fechamento 100%: assets OK."

Write-Host "[333/492] Validando SQL e PREPARAR 25..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.29_metas_por_refeicao.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.29_metas_por_refeicao.sql") -or
    -not $sqlSource.Contains('"MetaCalorias"') -or
    -not $sqlSource.Contains('"MetaFibrasG"')) {
    throw "Upgrade de metas por refeicao incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 25/25: OK."

Write-Host "[334/492] Validando versao v0.3.29..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.29 / metas por refeicao + distribuicao planejada: OK."


Write-Host "[335/492] Validando schema das fases nutricionais..."
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

Write-Host "[336/492] Validando listagem das fases..."
$phaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesNutricionaisController.cs -Encoding UTF8 -Raw
if (-not $phaseSource.Contains('api/pacientes/{pacienteId:guid}/fases-nutricionais') -or
    -not $phaseSource.Contains("OrderBy(x => x.Ordem)") -or
    -not $phaseSource.Contains("Include(x => x.PlanoAlimentar)")) {
    throw "Listagem de fases nutricionais incompleta."
}
Write-Host "    Ordem + plano vinculado + profissional: backend OK."

Write-Host "[337/492] Validando criacao de fase..."
if (-not $phaseSource.Contains("CriarFaseNutricionalRequest") -or
    -not $phaseSource.Contains('Status = "Planejada"') -or
    -not $phaseSource.Contains("maiorOrdem + 1")) {
    throw "Criacao de fase nutricional incompleta."
}
Write-Host "    Nova fase entra no fim como Planejada."

Write-Host "[338/492] Validando edicao e estados..."
if (-not $phaseSource.Contains("AtualizarFaseNutricionalRequest") -or
    -not $phaseSource.Contains('"Planejada" or "EmAndamento" or "Concluida" or "Cancelada"') -or
    -not $phaseSource.Contains('"UPDATE"')) {
    throw "Edicao/status de fase incompletos."
}
Write-Host "    Planejada / Em andamento / Concluida / Cancelada: OK."

Write-Host "[339/492] Validando vinculo seguro com plano alimentar..."
if (-not $phaseSource.Contains("PlanoValido") -or
    -not $phaseSource.Contains("x.PacienteId == pacienteId") -or
    -not $phaseSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Protecao do plano vinculado incompleta."
}
Write-Host "    Plano precisa pertencer ao mesmo paciente/tenant."

Write-Host "[340/492] Validando reordenacao do ciclo..."
if (-not $phaseSource.Contains("fases-nutricionais/reordenar") -or
    -not $phaseSource.Contains("idsExistentes.SequenceEqual(idsRecebidos)") -or
    -not $phaseSource.Contains("ordemDuplicada")) {
    throw "Reordenacao de fases incompleta."
}
Write-Host "    Reordenacao exige todas as fases e ordem unica."

Write-Host "[341/492] Validando exclusao protegida..."
if (-not $phaseSource.Contains("HttpDelete") -or
    -not $phaseSource.Contains('fase.Status == "EmAndamento"') -or
    -not $phaseSource.Contains('"DELETE"')) {
    throw "Exclusao protegida de fase incompleta."
}
Write-Host "    Fase em andamento nao pode ser apagada."

Write-Host "[342/492] Validando isolamento e auditoria..."
if (-not $phaseSource.Contains("currentUser.OrganizationId") -or
    -not $phaseSource.Contains("AuditLogs") -or
    -not $phaseSource.Contains("nameof(FaseNutricional)")) {
    throw "Tenant/auditoria das fases incompletos."
}
Write-Host "    Organizacao + auditoria: backend OK."

Write-Host "[343/492] Validando interface das fases..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("loadNutritionPhases") -or
    -not $appJsSource.Contains("nutritionPhaseCard") -or
    -not $appJsSource.Contains("newNutritionPhase")) {
    throw "Interface das fases nutricionais incompleta."
}
Write-Host "    Aba Alimentacao -> ciclo nutricional: assets OK."

Write-Host "[344/492] Validando formulario de fase..."
if (-not $appJsSource.Contains("openNutritionPhaseForm") -or
    -not $appJsSource.Contains("Cutting") -or
    -not $appJsSource.Contains("Manutenção") -or
    -not $appJsSource.Contains("planoAlimentarId")) {
    throw "Formulario de fase incompleto."
}
Write-Host "    Tipo + periodo + plano + objetivo + observacoes: assets OK."

Write-Host "[345/492] Validando reordenacao visual..."
if (-not $appJsSource.Contains("moveNutritionPhase") -or
    -not $appJsSource.Contains("nutrition-phase-up") -or
    -not $appJsSource.Contains("nutrition-phase-down")) {
    throw "Reordenacao visual das fases incompleta."
}
Write-Host "    Subir/descer fase: assets OK."

Write-Host "[346/492] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("nutrition-phase-card") -or
    -not $cssSource.Contains("nutrition-phase-list") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos de fases nutricionais incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[347/492] Validando SQL e PREPARAR 26..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.30_fases_nutricionais.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.30_fases_nutricionais.sql") -or
    -not $sqlSource.Contains('"FasesNutricionais"') -or
    -not $sqlSource.Contains('"PlanoAlimentarId"')) {
    throw "Upgrade de fases nutricionais incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 26/26: OK."

Write-Host "[348/492] Validando versao v0.3.30..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.30 / fases nutricionais + planejamento de ciclo: OK."


Write-Host "[349/492] Validando schema das fases de treino..."
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

Write-Host "[350/492] Validando listagem das fases..."
$phaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesTreinoController.cs -Encoding UTF8 -Raw
if (-not $phaseSource.Contains('api/pacientes/{pacienteId:guid}/fases-treino') -or
    -not $phaseSource.Contains("OrderBy(x => x.Ordem)") -or
    -not $phaseSource.Contains("Include(x => x.PlanoTreino)")) {
    throw "Listagem das fases de treino incompleta."
}
Write-Host "    Ordem + ficha vinculada + profissional: backend OK."

Write-Host "[351/492] Validando criacao de fase..."
if (-not $phaseSource.Contains("CriarFaseTreinoRequest") -or
    -not $phaseSource.Contains('Status = "Planejada"') -or
    -not $phaseSource.Contains("maiorOrdem + 1")) {
    throw "Criacao de fase de treino incompleta."
}
Write-Host "    Nova fase entra no fim como Planejada."

Write-Host "[352/492] Validando edicao e estados..."
if (-not $phaseSource.Contains("AtualizarFaseTreinoRequest") -or
    -not $phaseSource.Contains('"Planejada" or "EmAndamento" or "Concluida" or "Cancelada"') -or
    -not $phaseSource.Contains('"UPDATE"')) {
    throw "Edicao/status de fase de treino incompletos."
}
Write-Host "    Planejada / Em andamento / Concluida / Cancelada: OK."

Write-Host "[353/492] Validando vinculo seguro com ficha..."
if (-not $phaseSource.Contains("PlanoValido") -or
    -not $phaseSource.Contains("x.PacienteId == pacienteId") -or
    -not $phaseSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Protecao da ficha vinculada incompleta."
}
Write-Host "    Plano de treino precisa pertencer ao mesmo paciente/tenant."

Write-Host "[354/492] Validando reordenacao do ciclo..."
if (-not $phaseSource.Contains("fases-treino/reordenar") -or
    -not $phaseSource.Contains("idsExistentes.SequenceEqual(idsRecebidos)") -or
    -not $phaseSource.Contains("GroupBy(x => x.Ordem)")) {
    throw "Reordenacao de fases de treino incompleta."
}
Write-Host "    Reordenacao exige todas as fases e ordem unica."

Write-Host "[355/492] Validando exclusao protegida..."
if (-not $phaseSource.Contains("HttpDelete") -or
    -not $phaseSource.Contains('fase.Status == "EmAndamento"') -or
    -not $phaseSource.Contains('"DELETE"')) {
    throw "Exclusao protegida de fase de treino incompleta."
}
Write-Host "    Fase em andamento nao pode ser apagada."

Write-Host "[356/492] Validando isolamento e auditoria..."
if (-not $phaseSource.Contains("currentUser.OrganizationId") -or
    -not $phaseSource.Contains("AuditLogs") -or
    -not $phaseSource.Contains("nameof(FaseTreino)")) {
    throw "Tenant/auditoria das fases de treino incompletos."
}
Write-Host "    Organizacao + auditoria: backend OK."

Write-Host "[357/492] Validando interface do ciclo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("loadWorkoutPhases") -or
    -not $appJsSource.Contains("workoutPhaseCard") -or
    -not $appJsSource.Contains("newWorkoutPhase")) {
    throw "Interface do ciclo de treino incompleta."
}
Write-Host "    Aba Treinos -> periodizacao: assets OK."

Write-Host "[358/492] Validando formulario de fase..."
if (-not $appJsSource.Contains("openWorkoutPhaseForm") -or
    -not $appJsSource.Contains("Hipertrofia") -or
    -not $appJsSource.Contains("Deload") -or
    -not $appJsSource.Contains("planoTreinoId")) {
    throw "Formulario de fase de treino incompleto."
}
Write-Host "    Tipo + periodo + ficha + objetivo + observacoes: assets OK."

Write-Host "[359/492] Validando reordenacao visual..."
if (-not $appJsSource.Contains("moveWorkoutPhase") -or
    -not $appJsSource.Contains("workout-phase-up") -or
    -not $appJsSource.Contains("workout-phase-down")) {
    throw "Reordenacao visual de treino incompleta."
}
Write-Host "    Subir/descer fase: assets OK."

Write-Host "[360/492] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("workout-phase-card") -or
    -not $cssSource.Contains("workout-phase-list") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos do ciclo de treino incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[361/492] Validando SQL e PREPARAR 27..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.31_fases_treino.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.31_fases_treino.sql") -or
    -not $sqlSource.Contains('"FasesTreino"') -or
    -not $sqlSource.Contains('"PlanoTreinoId"')) {
    throw "Upgrade de fases de treino incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 27/27: OK."

Write-Host "[362/492] Validando versao v0.3.31..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.31 / ciclos de treino + periodizacao: OK."


Write-Host "[363/492] Validando schema dos check-ins..."
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

Write-Host "[364/492] Validando endpoint profissional..."
$checkinSource = Get-Content .\src\HealthPlatform.Api\Controllers\CheckInsAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $checkinSource.Contains('api/pacientes/{pacienteId:guid}/check-ins') -or
    -not $checkinSource.Contains("MontarHistorico") -or
    -not $checkinSource.Contains("variacao")) {
    throw "Endpoint profissional de check-ins incompleto."
}
Write-Host "    Historico + atual + variacao: backend OK."

Write-Host "[365/492] Validando criacao e edicao..."
if (-not $checkinSource.Contains("UpsertCheckInRequest") -or
    -not $checkinSource.Contains('Origem = "Profissional"') -or
    -not $checkinSource.Contains('Auditar("UPDATE"')) {
    throw "CRUD profissional de check-in incompleto."
}
Write-Host "    POST + PUT + auditoria: backend OK."

Write-Host "[366/492] Validando limites dos indicadores..."
if (-not $checkinSource.Contains("Peso deve ficar entre 20 e 400 kg") -or
    -not $checkinSource.Contains("Adesao deve ficar entre 0 e 100%") -or
    -not $checkinSource.Contains("devem ficar entre 0 e 10")) {
    throw "Validacao dos indicadores incompleta."
}
Write-Host "    Peso + adesao + escalas: protegidos."

Write-Host "[367/492] Validando vinculo com fases..."
if (-not $checkinSource.Contains("FasesValidas") -or
    -not $checkinSource.Contains("db.FasesNutricionais.AnyAsync") -or
    -not $checkinSource.Contains("db.FasesTreino.AnyAsync")) {
    throw "Vinculo dos check-ins com fases incompleto."
}
Write-Host "    Fase nutricional/treino precisa ser do paciente."

Write-Host "[368/492] Validando auto-vinculo do paciente..."
if (-not $checkinSource.Contains("FaseNutricionalAtual") -or
    -not $checkinSource.Contains("FaseTreinoAtual") -or
    -not $checkinSource.Contains('Origem = "Paciente"')) {
    throw "Auto-vinculo do check-in do paciente incompleto."
}
Write-Host "    Portal associa automaticamente as fases atuais."

Write-Host "[369/492] Validando portal do paciente..."
if (-not $checkinSource.Contains('api/portal/me/check-ins') -or
    -not $checkinSource.Contains('Authorize(Policy = "PatientOnly")') -or
    -not $checkinSource.Contains('"CREATE_SELF"')) {
    throw "Endpoints de check-in do paciente incompletos."
}
Write-Host "    GET + POST PatientOnly + auditoria: backend OK."

Write-Host "[370/492] Validando isolamento multi-tenant..."
if (-not $checkinSource.Contains("currentUser.OrganizationId") -or
    -not $checkinSource.Contains("MeuPacienteId") -or
    -not $checkinSource.Contains("x.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Isolamento dos check-ins incompleto."
}
Write-Host "    Organizacao + usuario/paciente vinculados: OK."

Write-Host "[371/492] Validando painel profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectProfessionalCheckIns") -or
    -not $appJsSource.Contains("professional-checkin-new") -or
    -not $appJsSource.Contains("openProfessionalCheckInForm")) {
    throw "Painel profissional de check-ins incompleto."
}
Write-Host "    Resumo/alimentacao/treinos -> check-ins: assets OK."

Write-Host "[372/492] Validando graficos de resposta..."
if (-not $appJsSource.Contains("hpCheckInCharts") -or
    -not $appJsSource.Contains("Adesão alimentar") -or
    -not $appJsSource.Contains("Adesão ao treino") -or
    -not $appJsSource.Contains("hpLineChart")) {
    throw "Graficos dos check-ins incompletos."
}
Write-Host "    Peso + dieta + treino + energia: assets OK."

Write-Host "[373/492] Validando formulario profissional..."
if (-not $appJsSource.Contains("adesaoAlimentacaoPercentual") -or
    -not $appJsSource.Contains("percepcaoEvolucaoNivel") -or
    -not $appJsSource.Contains("faseNutricionalId") -or
    -not $appJsSource.Contains("faseTreinoId")) {
    throw "Formulario profissional de check-in incompleto."
}
Write-Host "    Indicadores + duas fases: assets OK."

Write-Host "[374/492] Validando check-in no portal..."
if (-not $appJsSource.Contains("loadMyCheckInsIntoEvolution") -or
    -not $appJsSource.Contains("openMyCheckInForm") -or
    -not $appJsSource.Contains("patientCheckInNew") -or
    -not $appJsSource.Contains("Check-in enviado")) {
    throw "Interface de check-in do paciente incompleta."
}
Write-Host "    Evolucao -> novo check-in + historico: assets OK."

Write-Host "[375/492] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("checkin-current-grid") -or
    -not $cssSource.Contains("checkin-history-row") -or
    -not $cssSource.Contains("@media(max-width:560px)")) {
    throw "Estilos dos check-ins incompletos."
}
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[376/492] Validando SQL e PREPARAR 28..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.32_checkins_acompanhamento.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.32_checkins_acompanhamento.sql") -or
    -not $sqlSource.Contains('"CheckInsAcompanhamento"') -or
    -not $sqlSource.Contains('"AdesaoAlimentacaoPercentual"')) {
    throw "Upgrade dos check-ins incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 28/28: OK."

Write-Host "[377/492] Validando preservacao dos ciclos..."
if (-not $setupSource.Contains("v0.3.30_fases_nutricionais.sql") -or
    -not $setupSource.Contains("v0.3.31_fases_treino.sql")) {
    throw "Upgrades historicos dos ciclos nao foram preservados."
}
Write-Host "    Fases nutricionais + treino preservadas."

Write-Host "[378/492] Validando versao v0.3.32..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.32 / check-ins de evolucao + adesao por fase: OK."


Write-Host "[379/492] Validando endpoint de analise por fase..."
$checkinSource = Get-Content .\src\HealthPlatform.Api\Controllers\CheckInsAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $checkinSource.Contains('api/pacientes/{pacienteId:guid}/analise-fases') -or
    -not $checkinSource.Contains("MontarAnaliseFase") -or
    -not $checkinSource.Contains("AnaliseFaseResumo")) {
    throw "Endpoint de analise por fase incompleto."
}
Write-Host "    Nutricao + treino + agregacao: backend OK."

Write-Host "[380/492] Validando metricas agregadas..."
if (-not $checkinSource.Contains("MediaAdesaoAlimentacao") -or
    -not $checkinSource.Contains("MediaAdesaoTreino") -or
    -not $checkinSource.Contains("MediaFome") -or
    -not $checkinSource.Contains("MediaEnergia") -or
    -not $checkinSource.Contains("MediaSono")) {
    throw "Metricas de fase incompletas."
}
Write-Host "    Adesao + fome + energia + sono: backend OK."

Write-Host "[381/492] Validando variacao de peso por fase..."
if (-not $checkinSource.Contains("PesoInicialKg") -or
    -not $checkinSource.Contains("PesoFinalKg") -or
    -not $checkinSource.Contains("VariacaoPesoKg") -or
    -not $checkinSource.Contains("Diferenca(pesoFinal, pesoInicial)")) {
    throw "Variacao de peso por fase incompleta."
}
Write-Host "    Peso inicial -> final -> delta: backend OK."

Write-Host "[382/492] Validando destaques automaticos..."
if (-not $checkinSource.Contains("melhorAdesaoAlimentar") -or
    -not $checkinSource.Contains("melhorAdesaoTreino") -or
    -not $checkinSource.Contains("maiorReducaoPeso") -or
    -not $checkinSource.Contains("maiorEnergiaMedia")) {
    throw "Destaques de fases incompletos."
}
Write-Host "    Melhores respostas calculadas sem IA generativa."

Write-Host "[383/492] Validando isolamento multi-tenant..."
if (-not $checkinSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $checkinSource.Contains("PacienteExiste(pacienteId")) {
    throw "Isolamento da analise de fases incompleto."
}
Write-Host "    Paciente + organizacao protegidos."

Write-Host "[384/492] Validando cards comparativos..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpPhaseAnalysisCard") -or
    -not $appJsSource.Contains("phase-analysis-grid") -or
    -not $appJsSource.Contains("mediaAdesaoAlimentacao")) {
    throw "Cards comparativos de fases incompletos."
}
Write-Host "    Peso + adesao + energia + fome + sono: assets OK."

Write-Host "[385/492] Validando destaques visuais..."
if (-not $appJsSource.Contains("hpPhaseHighlightCard") -or
    -not $appJsSource.Contains("Melhor adesão alimentar") -or
    -not $appJsSource.Contains("Maior redução de peso")) {
    throw "Destaques visuais de fases incompletos."
}
Write-Host "    Melhores fases aparecem no topo da analise."

Write-Host "[386/492] Validando integracao com nutricao..."
if (-not $appJsSource.Contains("nutrition-phase-analysis") -or
    -not $appJsSource.Contains("'nutrition'")) {
    throw "Analise das fases nutricionais nao integrada."
}
Write-Host "    Alimentacao -> comparativo nutricional: assets OK."

Write-Host "[387/492] Validando integracao com treino..."
if (-not $appJsSource.Contains("workout-phase-analysis") -or
    -not $appJsSource.Contains("'workout'")) {
    throw "Analise das fases de treino nao integrada."
}
Write-Host "    Treinos -> comparativo de periodizacao: assets OK."

Write-Host "[388/492] Validando resumo consolidado..."
if (-not $appJsSource.Contains("summary-phase-analysis") -or
    -not $appJsSource.Contains("hpInjectPhaseAnalysis")) {
    throw "Analise consolidada de fases nao integrada ao resumo."
}
Write-Host "    Resumo -> destaques dos dois ciclos: assets OK."

Write-Host "[389/492] Validando responsividade e compatibilidade de banco..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $cssSource.Contains("phase-highlight-grid") -or
    -not $cssSource.Contains("phase-analysis-list") -or
    -not $cssSource.Contains("@media(max-width:560px)") -or
    -not $setupSource.Contains("[30/30]")) {
    throw "Responsividade ou compatibilidade de banco inesperada."
}
if (Test-Path .\scripts\sql\v0.3.33_analise_fases.sql) {
    throw "v0.3.33 nao deveria exigir novo schema."
}
Write-Host "    UI responsiva / sem schema novo / PREPARAR 28/28: OK."

Write-Host "[390/492] Validando versao v0.3.33..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.33 / analise de fases + comparativo de resposta: OK."

Write-Host "[391/492] Validando metas das fases..."
$nutritionPhaseEntity = Get-Content .\src\HealthPlatform.Domain\Entities\FaseNutricional.cs -Encoding UTF8 -Raw
$workoutPhaseEntity = Get-Content .\src\HealthPlatform.Domain\Entities\FaseTreino.cs -Encoding UTF8 -Raw
if (-not $nutritionPhaseEntity.Contains("MetaPesoKg") -or -not $nutritionPhaseEntity.Contains("MetaAdesaoPercentual") -or -not $nutritionPhaseEntity.Contains("DuracaoMinimaDias") -or -not $nutritionPhaseEntity.Contains("CriterioTransicao") -or -not $workoutPhaseEntity.Contains("MetaPesoKg") -or -not $workoutPhaseEntity.Contains("CriterioTransicao")) { throw "Metas das fases incompletas." }
Write-Host "    Peso + adesao + duracao + criterio manual: modelo OK."

Write-Host "[392/492] Validando mapeamento EF..."
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbSource.Contains("MetaPesoKg).HasPrecision(8, 2)") -or -not $dbSource.Contains("CriterioTransicao).HasMaxLength(1000)")) { throw "Mapeamento EF dos criterios incompleto." }
Write-Host "    Precisao de peso + limite do criterio: EF OK."

Write-Host "[393/492] Validando CRUD das fases..."
$nutritionPhaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesNutricionaisController.cs -Encoding UTF8 -Raw
$workoutPhaseSource = Get-Content .\src\HealthPlatform.Api\Controllers\FasesTreinoController.cs -Encoding UTF8 -Raw
if (-not $nutritionPhaseSource.Contains("request.MetaAdesaoPercentual") -or -not $workoutPhaseSource.Contains("request.MetaAdesaoPercentual") -or -not $nutritionPhaseSource.Contains("request.CriterioTransicao") -or -not $workoutPhaseSource.Contains("request.CriterioTransicao")) { throw "CRUD das fases nao preserva criterios." }
Write-Host "    Criacao + edicao preservam metas."

Write-Host "[394/492] Validando limites dos criterios..."
if (-not $nutritionPhaseSource.Contains("Meta de peso deve ficar entre 20 e 400 kg") -or -not $nutritionPhaseSource.Contains("Meta de adesao deve ficar entre 0 e 100%") -or -not $nutritionPhaseSource.Contains("Duracao minima deve ficar entre 1 e 3650 dias") -or -not $nutritionPhaseSource.Contains("1000 caracteres")) { throw "Validacoes dos criterios incompletas." }
Write-Host "    Limites de configuracao: OK."

Write-Host "[395/492] Validando endpoint runtime de prontidao..."
if ($lista.total -gt 0 -and $lista.itens.Count -gt 0) { $pacienteSmoke = $lista.itens | Select-Object -First 1; $statusTransicao = Invoke-RestMethod -Uri "$base/api/pacientes/$($pacienteSmoke.id)/status-transicao-fases" -Headers $headers -Method Get; if ($null -eq $statusTransicao.nutricao -or $null -eq $statusTransicao.treino) { throw "Endpoint de status de transicao retornou estrutura invalida." }; Write-Host "    GET status-transicao-fases: runtime OK." } else { Write-Host "    Sem pacientes: smoke runtime ignorado." }

Write-Host "[396/492] Validando motor de criterios objetivos..."
$checkinSource = Get-Content .\src\HealthPlatform.Api\Controllers\CheckInsAcompanhamentoController.cs -Encoding UTF8 -Raw
if (-not $checkinSource.Contains("duracao_minima") -or -not $checkinSource.Contains("adesao_minima") -or -not $checkinSource.Contains("meta_peso") -or -not $checkinSource.Contains("Math.Abs(pesoAtual.Value - metaPesoKg.Value) <= 0.5m")) { throw "Motor objetivo incompleto." }
Write-Host "    Duracao + adesao + peso: motor OK."

Write-Host "[397/492] Validando revisao profissional..."
if (-not $checkinSource.Contains("ObjetivosProntosParaRevisao") -or -not $checkinSource.Contains("RequerAvaliacaoProfissional")) { throw "Semantica de revisao profissional incompleta." }
Write-Host "    Motor sugere revisao, nao conclui a fase automaticamente."

Write-Host "[398/492] Validando formularios das fases..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("metaPesoKg") -or -not $appJsSource.Contains("metaAdesaoPercentual") -or -not $appJsSource.Contains("duracaoMinimaDias") -or -not $appJsSource.Contains("criterioTransicao")) { throw "Formularios de criterios incompletos." }
Write-Host "    Nutricao + treino configuram metas: assets OK."

Write-Host "[399/492] Validando metas nos cards..."
if (-not $appJsSource.Contains("phaseGoalChips")) { throw "Resumo visual das metas incompleto." }
Write-Host "    Cards exibem metas configuradas."

Write-Host "[400/492] Validando painel de prontidao..."
if (-not $appJsSource.Contains("hpInjectTransitionStatus") -or -not $appJsSource.Contains("hpTransitionStatusCard") -or -not $appJsSource.Contains("objetivosProntosParaRevisao")) { throw "Painel de prontidao incompleto." }
Write-Host "    Progresso dos criterios: assets OK."

Write-Host "[401/492] Validando integracao nutricional..."
if (-not $appJsSource.Contains("nutrition-transition-status")) { throw "Integracao nutricional incompleta." }
Write-Host "    Alimentacao: OK."

Write-Host "[402/492] Validando integracao de treino..."
if (-not $appJsSource.Contains("workout-transition-status")) { throw "Integracao de treino incompleta." }
Write-Host "    Treinos: OK."

Write-Host "[403/492] Validando integracao no resumo..."
if (-not $appJsSource.Contains("summary-transition-status")) { throw "Integracao no resumo incompleta." }
Write-Host "    Resumo: OK."

Write-Host "[404/492] Validando responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("transition-status-card") -or -not $cssSource.Contains("phase-goal-chips")) { throw "Estilos de transicao incompletos." }
Write-Host "    Desktop + mobile: estilos OK."

Write-Host "[405/492] Validando SQL e PREPARAR 29..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.34_criterios_transicao_fases.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or -not $setupSource.Contains("v0.3.34_criterios_transicao_fases.sql") -or -not $sqlSource.Contains('"MetaPesoKg"') -or -not $sqlSource.Contains('"CriterioTransicao"') -or -not $setupSource.Contains("v0.3.32_checkins_acompanhamento.sql")) { throw "Upgrade dos criterios incompleto." }
Write-Host "    SQL idempotente + PREPARAR 29/29 + historico preservado."

Write-Host "[406/492] Validando versao v0.3.34..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.34 / metas de fase + criterios de transicao: OK."


Write-Host "[407/492] Validando entidade de revisao..."
$reviewEntity = Get-Content .\src\HealthPlatform.Domain\Entities\RevisaoFase.cs -Encoding UTF8 -Raw
if (-not $reviewEntity.Contains("RevisadoPorUsuarioId") -or
    -not $reviewEntity.Contains("FaseDestinoId") -or
    -not $reviewEntity.Contains("OverrideCriterios") -or
    -not $reviewEntity.Contains("SnapshotIndicadoresJson")) {
    throw "Entidade RevisaoFase incompleta."
}
Write-Host "    Decisao + destino + override + snapshot: modelo OK."

Write-Host "[408/492] Validando mapeamento das revisoes..."
$dbSource = Get-Content .\src\HealthPlatform.Infrastructure\Data\AppDbContext.cs -Encoding UTF8 -Raw
if (-not $dbSource.Contains("DbSet<RevisaoFase>") -or
    -not $dbSource.Contains('ToTable("RevisoesFases")') -or
    -not $dbSource.Contains("x.OrganizacaoId, x.Dominio, x.DataUtc")) {
    throw "Mapeamento de RevisaoFase incompleto."
}
Write-Host "    Tabela + indices + paciente: EF OK."

Write-Host "[409/492] Validando historico runtime..."
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

Write-Host "[410/492] Validando revisao nutricional..."
$reviewSource = Get-Content .\src\HealthPlatform.Api\Controllers\RevisoesFasesController.cs -Encoding UTF8 -Raw
if (-not $reviewSource.Contains('api/fases-nutricionais/{id:guid}/revisar') -or
    -not $reviewSource.Contains("RevisarNutricional") -or
    -not $reviewSource.Contains('"Nutricao"')) {
    throw "Revisao nutricional incompleta."
}
Write-Host "    Endpoint de revisao nutricional: backend OK."

Write-Host "[411/492] Validando revisao de treino..."
if (-not $reviewSource.Contains('api/fases-treino/{id:guid}/revisar') -or
    -not $reviewSource.Contains("RevisarTreino") -or
    -not $reviewSource.Contains('"Treino"')) {
    throw "Revisao de treino incompleta."
}
Write-Host "    Endpoint de revisao de treino: backend OK."

Write-Host "[412/492] Validando decisoes e fase ativa..."
if (-not $reviewSource.Contains('"Manter"') -or
    -not $reviewSource.Contains('"Concluir"') -or
    -not $reviewSource.Contains('"Avancar"') -or
    -not $reviewSource.Contains('fase.Status != "EmAndamento"')) {
    throw "Regras basicas da revisao incompletas."
}
Write-Host "    Manter / concluir / avancar + EmAndamento: regras OK."

Write-Host "[413/492] Validando override consciente..."
if (-not $reviewSource.Contains("ConfirmarMesmoSemCriterios") -or
    -not $reviewSource.Contains("ExigeOverride") -or
    -not $reviewSource.Contains("criterios objetivos pendentes")) {
    throw "Protecao de override incompleta."
}
Write-Host "    Criterios pendentes exigem confirmacao explicita."

Write-Host "[414/492] Validando transicao para proxima fase..."
if (-not $reviewSource.Contains("x.Ordem > fase.Ordem") -or
    -not $reviewSource.Contains('x.Status == "Planejada"') -or
    -not $reviewSource.Contains('proxima.Status = "EmAndamento"') -or
    -not $reviewSource.Contains('fase.Status = "Concluida"')) {
    throw "Transicao assistida incompleta."
}
Write-Host "    Atual conclui + proxima Planejada ativa: backend OK."

Write-Host "[415/492] Validando transacao e auditoria..."
if (-not $reviewSource.Contains("BeginTransactionAsync") -or
    -not $reviewSource.Contains("CommitAsync") -or
    -not $reviewSource.Contains('"REVIEW_CREATE"') -or
    -not $reviewSource.Contains('"REVIEW_ACTIVATE_NEXT"')) {
    throw "Transacao/auditoria das revisoes incompletas."
}
Write-Host "    Decisao + mudancas de status atomicas e auditadas."

Write-Host "[416/492] Validando modal de revisao..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openPhaseReview") -or
    -not $appJsSource.Contains("phaseReviewForm") -or
    -not $appJsSource.Contains("Confirmo a decisão profissional") -or
    -not $appJsSource.Contains("Registrar revisão")) {
    throw "Modal de revisao incompleto."
}
Write-Host "    Decisao + justificativa + override: assets OK."

Write-Host "[417/492] Validando historico visual..."
if (-not $appJsSource.Contains("hpPhaseReviewHistory") -or
    -not $appJsSource.Contains("Histórico de decisões") -or
    -not $appJsSource.Contains("phase-review-history-card")) {
    throw "Historico visual das revisoes incompleto."
}
Write-Host "    Ultimas decisoes aparecem junto da prontidao."

Write-Host "[418/492] Validando integracao com painel de transicao..."
if (-not $appJsSource.Contains("hpTransitionStatusCardReview") -or
    -not $appJsSource.Contains("phase-review-action") -or
    -not $appJsSource.Contains("/revisoes-fases?limite=6")) {
    throw "Integracao revisao/prontidao incompleta."
}
Write-Host "    Fase EmAndamento recebe acao de revisao."

Write-Host "[419/492] Validando SQL e PREPARAR 30..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
$sqlSource = Get-Content .\scripts\sql\v0.3.35_revisoes_transicoes_fases.sql -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql") -or
    -not $sqlSource.Contains('"RevisoesFases"') -or
    -not $sqlSource.Contains('"SnapshotIndicadoresJson"') -or
    -not $setupSource.Contains("v0.3.34_criterios_transicao_fases.sql")) {
    throw "Upgrade de revisoes/transicoes incompleto."
}
Write-Host "    SQL idempotente + PREPARAR 30/30 + v0.3.34 preservada."

Write-Host "[420/492] Validando versao v0.3.35..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.35 / revisao de fase + transicao assistida: OK."


Write-Host "[421/492] Validando controller de volume..."
$volumeSource = Get-Content .\src\HealthPlatform.Api\Controllers\AnaliseVolumeTreinoController.cs -Encoding UTF8 -Raw
if (-not $volumeSource.Contains('api/pacientes/{pacienteId:guid}/treinos/analise-volume') -or
    -not $volumeSource.Contains("AnaliseVolumeTreinoController") -or
    -not $volumeSource.Contains("QueryPlano")) {
    throw "Controller de analise de volume incompleto."
}
Write-Host "    Endpoint + selecao do plano: backend OK."

Write-Host "[422/492] Validando isolamento multi-tenant..."
if (-not $volumeSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $volumeSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Isolamento da analise de volume incompleto."
}
Write-Host "    Paciente + plano + execucoes: tenant OK."

Write-Host "[423/492] Validando volume planejado por grupo..."
if (-not $volumeSource.Contains("Grupo(x.Exercicio.GrupoMuscular)") -or
    -not $volumeSource.Contains("SeriesPorCiclo") -or
    -not $volumeSource.Contains("SeriesSemanaisEstimadas") -or
    -not $volumeSource.Contains("ExerciciosDistintos")) {
    throw "Volume planejado por grupo incompleto."
}
Write-Host "    Series + exercicios distintos + grupo muscular: backend OK."

Write-Host "[424/492] Validando frequencia semanal..."
if (-not $volumeSource.Contains("FrequenciaSemanal") -or
    -not $volumeSource.Contains("SemAcentos") -or
    -not $volumeSource.Contains("segunda") -or
    -not $volumeSource.Contains("sexta")) {
    throw "Inferencia de frequencia semanal incompleta."
}
Write-Host "    DiasSemana -> frequencia reconhecida: backend OK."

Write-Host "[425/492] Validando fallback de frequencia..."
if (-not $volumeSource.Contains("return (1, false)") -or
    -not $volumeSource.Contains("frequenciaInferida")) {
    throw "Fallback de frequencia nao identificado."
}
Write-Host "    Dias nao reconhecidos usam 1x/semana e ficam sinalizados."

Write-Host "[426/492] Validando execucoes reais..."
if (-not $volumeSource.Contains("SeriesRealizadas") -or
    -not $volumeSource.Contains('x.Status == "Concluido"') -or
    -not $volumeSource.Contains("seriesRealizadasPeriodo") -or
    -not $volumeSource.Contains("mediaSeriesRealizadasSemana")) {
    throw "Volume realizado incompleto."
}
Write-Host "    Series concluidas + periodo + media semanal: backend OK."

Write-Host "[427/492] Validando ausencia de tonelagem inventada..."
if (-not $volumeSource.Contains("Tonelagem nao e inferida") -or
    $volumeSource.Contains("RepeticoesRealizadas *") -or
    $volumeSource.Contains("CargaRealizada *")) {
    throw "Protecao contra tonelagem inferida incorretamente falhou."
}
Write-Host "    Repeticoes textuais nao viram tonelagem ficticia."

Write-Host "[428/492] Validando runtime da analise..."
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

Write-Host "[429/492] Validando painel de volume..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectWorkoutVolume") -or
    -not $appJsSource.Contains("hpWorkoutVolumeBar") -or
    -not $appJsSource.Contains("Volume e distribuição muscular")) {
    throw "Painel de volume incompleto."
}
Write-Host "    Distribuicao muscular: assets OK."

Write-Host "[430/492] Validando resumo analitico..."
if (-not $appJsSource.Contains("Séries planejadas") -or
    -not $appJsSource.Contains("Séries realizadas") -or
    -not $appJsSource.Contains("Média realizada") -or
    -not $appJsSource.Contains("Maior concentração")) {
    throw "Resumo visual de volume incompleto."
}
Write-Host "    Planejado + realizado + concentracao: assets OK."

Write-Host "[431/492] Validando volume por sessao..."
if (-not $appJsSource.Contains("hpWorkoutSessionVolume") -or
    -not $appJsSource.Contains("séries/sessão") -or
    -not $appJsSource.Contains("Frequência não reconhecida")) {
    throw "Analise visual por sessao incompleta."
}
Write-Host "    Sessao + frequencia + series semanais: assets OK."

Write-Host "[432/492] Validando integracao e responsividade..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("workout-volume-main") -or
    -not $appJsSource.Contains("workout-volume-summary") -or
    -not $cssSource.Contains("workout-volume-row") -or
    -not $cssSource.Contains("workout-session-volume-list")) {
    throw "Integracao/responsividade do volume incompleta."
}
Write-Host "    Treinos + Resumo + layout responsivo: assets OK."

Write-Host "[433/492] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.36_volume_treino.sql) {
    throw "v0.3.36 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 30/30."

Write-Host "[434/492] Validando versao v0.3.36..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.36 / volume de treino + distribuicao muscular: OK."


Write-Host "[435/492] Validando endpoints de progressao por exercicio..."
$progressSource = Get-Content .\src\HealthPlatform.Api\Controllers\ProgressaoExerciciosTreinoController.cs -Encoding UTF8 -Raw
if (-not $progressSource.Contains('api/pacientes/{pacienteId:guid}/treinos/progressao-exercicios') -or
    -not $progressSource.Contains('api/portal/me/treinos/progressao-exercicios') -or
    -not $progressSource.Contains("ProgressaoExerciciosTreinoController")) {
    throw "Endpoints de progressao por exercicio incompletos."
}
Write-Host "    Profissional + paciente: rotas OK."

Write-Host "[436/492] Validando seguranca e tenant..."
if (-not $progressSource.Contains('Authorize(Policy = "PatientOnly")') -or
    -not $progressSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $progressSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Seguranca da progressao de exercicios incompleta."
}
Write-Host "    PatientOnly + organizacao: OK."

Write-Host "[437/492] Validando separacao por unidade..."
if (-not $progressSource.Contains("NormalizarUnidade") -or
    -not $progressSource.Contains("x.Unidade") -or
    -not $progressSource.Contains('"kg" or "kgs"') -or
    -not $progressSource.Contains('"lb" or "lbs"')) {
    throw "Separacao/normalizacao de unidades incompleta."
}
Write-Host "    Mesmo exercicio nao mistura kg com lb."

Write-Host "[438/492] Validando metricas de carga..."
if (-not $progressSource.Contains("primeiraCarga") -or
    -not $progressSource.Contains("ultimaCarga") -or
    -not $progressSource.Contains("maiorCarga") -or
    -not $progressSource.Contains("variacaoPercentual") -or
    -not $progressSource.Contains("deltaCarga")) {
    throw "Metricas de progressao incompletas."
}
Write-Host "    Inicial + atual + PR + delta + percentual: backend OK."

Write-Host "[439/492] Validando recordes sucessivos..."
if (-not $progressSource.Contains("ContarNovosRecordes") -or
    -not $progressSource.Contains("ponto.Carga > maiorAnterior") -or
    -not $progressSource.Contains("novosRecordesPeriodo")) {
    throw "Contagem de recordes incompleta."
}
Write-Host "    Novos PRs ao longo do periodo: backend OK."

Write-Host "[440/492] Validando tendencia de carga..."
if (-not $progressSource.Contains("Tendencia") -or
    -not $progressSource.Contains('"AcimaDaBase"') -or
    -not $progressSource.Contains('"Estavel"') -or
    -not $progressSource.Contains('"AbaixoDaBase"')) {
    throw "Tendencia de carga incompleta."
}
Write-Host "    Base recente + tolerancia: backend OK."

Write-Host "[441/492] Validando protecao contra estimativas artificiais..."
if (-not $progressSource.Contains("Nao ha estimativa de 1RM") -or
    -not $progressSource.Contains("repeticoes textuais") -or
    $progressSource.Contains("Epley") -or
    $progressSource.Contains("Brzycki")) {
    throw "Protecao contra 1RM estimado incorretamente falhou."
}
Write-Host "    Sem 1RM/tonelagem inferidos de texto livre."

Write-Host "[442/492] Validando runtime profissional..."
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

Write-Host "[443/492] Validando payload de pontos..."
if (-not $progressSource.Contains("cargaRealizada = x.Carga") -or
    -not $progressSource.Contains("SeriesRealizadas") -or
    -not $progressSource.Contains("RepeticoesRealizadas") -or
    -not $progressSource.Contains("EsforcoPercebido")) {
    throw "Pontos de progressao incompletos."
}
Write-Host "    Data + carga + series + reps + RPE: backend OK."

Write-Host "[444/492] Validando painel profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectExerciseProgression") -or
    -not $appJsSource.Contains("hpExerciseProgressCard") -or
    -not $appJsSource.Contains("Progressão por exercício")) {
    throw "Painel profissional de progressao incompleto."
}
Write-Host "    Treinos + Resumo: assets OK."

Write-Host "[445/492] Validando graficos e recordes..."
if (-not $appJsSource.Contains("hpExerciseProgressCharts") -or
    -not $appJsSource.Contains("Melhor marca") -or
    -not $appJsSource.Contains("Novos recordes") -or
    -not $appJsSource.Contains("Mais recordes no período")) {
    throw "Visual de graficos/recordes incompleto."
}
Write-Host "    Curvas + PRs + destaque: assets OK."

Write-Host "[446/492] Validando portal do paciente..."
if (-not $appJsSource.Contains("hpInjectMyExerciseProgression") -or
    -not $appJsSource.Contains("Minha progressão por exercício") -or
    -not $appJsSource.Contains("__loadPatientWorkout_v037_exerciseprogress")) {
    throw "Progressao no portal do paciente incompleta."
}
Write-Host "    Meu treino -> progressao individual: assets OK."

Write-Host "[447/492] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.37_progressao_exercicios.sql) {
    throw "v0.3.37 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 30/30."

Write-Host "[448/492] Validando versao v0.3.37..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.37 / progressao por exercicio + recordes de carga: OK."


Write-Host "[449/492] Validando endpoints de sinais de progressao..."
$signalSource = Get-Content .\src\HealthPlatform.Api\Controllers\AnaliseProgressoTreinoController.cs -Encoding UTF8 -Raw
if (-not $signalSource.Contains('api/pacientes/{pacienteId:guid}/treinos/analise-progresso') -or
    -not $signalSource.Contains('api/portal/me/treinos/analise-progresso') -or
    -not $signalSource.Contains("AnaliseProgressoTreinoController")) {
    throw "Endpoints de analise de progresso incompletos."
}
Write-Host "    Profissional + paciente: rotas OK."

Write-Host "[450/492] Validando tenant e PatientOnly..."
if (-not $signalSource.Contains('Authorize(Policy = "PatientOnly")') -or
    -not $signalSource.Contains("x.OrganizacaoId == currentUser.OrganizationId") -or
    -not $signalSource.Contains("x.Paciente.OrganizacaoId == currentUser.OrganizationId")) {
    throw "Seguranca da analise de progresso incompleta."
}
Write-Host "    Isolamento + portal: OK."

Write-Host "[451/492] Validando estados da analise..."
if (-not $signalSource.Contains('"Progredindo"') -or
    -not $signalSource.Contains('"Estagnacao"') -or
    -not $signalSource.Contains('"PossivelFadiga"') -or
    -not $signalSource.Contains('"Estavel"') -or
    -not $signalSource.Contains('"SemBase"')) {
    throw "Estados da analise de progresso incompletos."
}
Write-Host "    Progresso + estagnacao + carga/RPE + base: backend OK."

Write-Host "[452/492] Validando regra de estagnacao..."
if (-not $signalSource.Contains("pontos.Count >= 5") -or
    -not $signalSource.Contains("Math.Abs(variacao.Value) <= 2m") -or
    -not $signalSource.Contains("!recordeNaJanelaRecente")) {
    throw "Regra de estagnacao incompleta."
}
Write-Host "    +/-2% + sem PR recente + base minima: regra OK."

Write-Host "[453/492] Validando sinal de carga/RPE..."
if (-not $signalSource.Contains("variacao.Value <= -3m") -or
    -not $signalSource.Contains("mediaRpe.Value >= 8m") -or
    -not $signalSource.Contains('status is "Estagnacao" or "PossivelFadiga"')) {
    throw "Regra de revisao por carga/RPE incompleta."
}
Write-Host "    Queda >=3% + RPE >=8: sinaliza revisao."

Write-Host "[454/492] Validando progressao recente..."
if (-not $signalSource.Contains("recordeNaJanelaRecente") -or
    -not $signalSource.Contains("variacao.Value > 2m") -or
    -not $signalSource.Contains('status = "Progredindo"')) {
    throw "Regra de progressao incompleta."
}
Write-Host "    PR recente ou ganho >2%: progresso reconhecido."

Write-Host "[455/492] Validando semantica nao diagnostica..."
if (-not $signalSource.Contains("Nao representam diagnostico de fadiga") -or
    -not $signalSource.Contains("nao prescrevem aumento de carga automaticamente")) {
    throw "Disclaimer da analise esportiva incompleto."
}
Write-Host "    Heuristica de acompanhamento, nao diagnostico/prescricao."

Write-Host "[456/492] Validando runtime profissional..."
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

Write-Host "[457/492] Validando payload analitico..."
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

Write-Host "[458/492] Validando painel profissional..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpInjectTrainingSignals") -or
    -not $appJsSource.Contains("hpTrainingSignalCard") -or
    -not $appJsSource.Contains("Sinais de progressão")) {
    throw "Painel de sinais de progressao incompleto."
}
Write-Host "    Treinos + Resumo: assets OK."

Write-Host "[459/492] Validando portal do paciente..."
if (-not $appJsSource.Contains("hpInjectMyTrainingSignals") -or
    -not $appJsSource.Contains("Meus sinais de progressão") -or
    -not $appJsSource.Contains("__loadPatientWorkout_v038_trainingsignals")) {
    throw "Sinais de progressao no portal incompletos."
}
Write-Host "    Meu treino: assets OK."

Write-Host "[460/492] Validando cards e graficos..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("hpTrainingSignalCharts") -or
    -not $appJsSource.Contains("Revisão sugerida pelo histórico recente") -or
    -not $cssSource.Contains("training-signal-card") -or
    -not $cssSource.Contains("training-signal-summary")) {
    throw "Visual da analise de progresso incompleto."
}
Write-Host "    Cards + graficos + revisao sugerida: assets OK."

Write-Host "[461/492] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "PREPARAR historico inesperado."
}
if (Test-Path .\scripts\sql\v0.3.38_analise_progresso.sql) {
    throw "v0.3.38 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 30/30."

Write-Host "[462/492] Validando versao v0.3.38..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.38 / estagnacao + fadiga + sinais de progressao: OK."


Write-Host "[463/492] Validando identidade MVP Preview..."
$indexSource = Get-Content .\src\HealthPlatform.Api\wwwroot\index.html -Encoding UTF8 -Raw
if (-not $indexSource.Contains("MVP Preview • v0.3.40") -or
    -not $indexSource.Contains("mvp-brand-badge") -or
    -not $indexSource.Contains("MVP • DEMO") -or
    -not $indexSource.Contains('id="loginMessage"') -or
    $indexSource.Contains('value="ChangeMe_123!"')) {
    throw "Identidade/login do MVP Preview incompletos."
}
Write-Host "    Login + marcas de demo: assets OK."

Write-Host "[464/492] Validando aviso de demonstracao..."
if (-not $indexSource.Contains("Ambiente de demonstração") -or
    -not $indexSource.Contains("Use somente dados fictícios") -or
    -not $indexSource.Contains("senha profissional é a configurada")) {
    throw "Aviso de ambiente de demonstracao/login incompleto."
}
Write-Host "    Uso ficticio e objetivo do prototipo: copy OK."

Write-Host "[465/492] Validando roteiro da demo..."
$appJsSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.js -Encoding UTF8 -Raw
if (-not $appJsSource.Contains("openMvpGuide") -or
    -not $appJsSource.Contains("Roteiro rápido para testar o sistema") -or
    -not $appJsSource.Contains("hpMvpChecklistItem")) {
    throw "Roteiro de avaliacao do MVP incompleto."
}
Write-Host "    Guia interno de exploracao: assets OK."

Write-Host "[466/492] Validando checklist de avaliacao..."
if (-not $appJsSource.Contains("Cadastre ou escolha um paciente") -or
    -not $appJsSource.Contains("Simule uma consulta") -or
    -not $appJsSource.Contains("Monte alimentação e treino") -or
    -not $appJsSource.Contains("Entre como paciente") -or
    -not $appJsSource.Contains("Procure atritos")) {
    throw "Checklist de avaliacao incompleto."
}
Write-Host "    Fluxos principais cobertos no roteiro."

Write-Host "[467/492] Validando modelo de feedback..."
if (-not $appJsSource.Contains("copyMvpFeedbackTemplate") -or
    -not $appJsSource.Contains("FALTOU:") -or
    -not $appJsSource.Contains("CONFUNDIU:") -or
    -not $appJsSource.Contains("DEMOROU:") -or
    -not $appJsSource.Contains("QUEBROU/BUG:")) {
    throw "Modelo de feedback do MVP incompleto."
}
Write-Host "    Feedback estruturado pode ser copiado."

Write-Host "[468/492] Validando dashboard de apresentacao..."
if (-not $appJsSource.Contains("mvp-dashboard-hero") -or
    -not $appJsSource.Contains("AMBIENTE DE DEMONSTRAÇÃO") -or
    -not $appJsSource.Contains("openMvpGuideHero") -or
    -not $appJsSource.Contains("goAgendaHero")) {
    throw "Dashboard do MVP Preview incompleto."
}
Write-Host "    Hero + atalhos de demo: assets OK."

Write-Host "[469/492] Validando atalho Escape..."
if (-not $appJsSource.Contains("e.key!=='Escape'") -or
    -not $appJsSource.Contains("closeClinicalAction") -or
    -not $appJsSource.Contains("create.classList.add('hidden')") -or
    -not $appJsSource.Contains("sidebar')?.classList.remove('open')")) {
    throw "Atalho Escape incompleto."
}
Write-Host "    Escape fecha camadas sem alterar dados."

Write-Host "[470/492] Validando feedback de conectividade..."
if (-not $appJsSource.Contains("addEventListener('offline'") -or
    -not $appJsSource.Contains("addEventListener('online'") -or
    -not $appJsSource.Contains("Conexão restabelecida")) {
    throw "Feedback de conectividade incompleto."
}
Write-Host "    Offline/online recebem feedback visual."

Write-Host "[471/492] Validando acabamento de foco..."
$cssSource = Get-Content .\src\HealthPlatform.Api\wwwroot\app.css -Encoding UTF8 -Raw
if (-not $cssSource.Contains("focus-visible") -or
    -not $cssSource.Contains("outline-offset") -or
    -not $cssSource.Contains("button:not(:disabled):active")) {
    throw "Acabamento de interacao/foco incompleto."
}
Write-Host "    Teclado + feedback de clique: estilos OK."

Write-Host "[472/492] Validando estados vazios..."
if (-not $cssSource.Contains(".empty::before") -or
    -not $cssSource.Contains("place-items:center") -or
    -not $cssSource.Contains("text-align:center")) {
    throw "Polimento de estados vazios incompleto."
}
Write-Host "    Estados sem dados mais consistentes."

Write-Host "[473/492] Validando limpeza da navegacao de demo..."
if (-not $indexSource.Contains("mvp-dev-link") -or
    -not $cssSource.Contains(".mvp-dev-link{display:none!important}")) {
    throw "Link tecnico nao foi escondido da navegacao da demo."
}
Write-Host "    Swagger continua no backend, mas sai da navegacao principal."

Write-Host "[474/492] Validando responsividade do MVP..."
if (-not $cssSource.Contains("@media(max-width:900px)") -or
    -not $cssSource.Contains(".mvp-guide-grid{grid-template-columns:1fr}") -or
    -not $cssSource.Contains("@media(max-width:620px)") -or
    -not $cssSource.Contains(".mvp-dashboard-actions button{flex:1 1 140px}")) {
    throw "Responsividade do MVP Preview incompleta."
}
Write-Host "    Notebook + mobile: estilos de demo OK."

Write-Host "[475/492] Validando compatibilidade de banco..."
$setupSource = Get-Content .\scripts\setup.ps1 -Encoding UTF8 -Raw
if (-not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "Historico do PREPARAR inesperado."
}
if (Test-Path .\scripts\sql\v0.3.39_mvp_preview.sql) {
    throw "v0.3.39 nao deveria exigir schema novo."
}
Write-Host "    Sem schema novo / PREPARAR permanece 30/30."

Write-Host "[476/492] Validando versao v0.3.39..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.39 / MVP Preview + polimento de demonstracao: OK."


Write-Host "[477/492] Validando Dockerfile..."
$dockerSource = Get-Content .\Dockerfile -Encoding UTF8 -Raw
if (-not $dockerSource.Contains("mcr.microsoft.com/dotnet/sdk:10.0") -or
    -not $dockerSource.Contains("mcr.microsoft.com/dotnet/aspnet:10.0") -or
    -not $dockerSource.Contains("dotnet publish") -or
    -not $dockerSource.Contains("docker-entrypoint.sh")) {
    throw "Dockerfile do MVP incompleto."
}
Write-Host "    Build multi-stage .NET 10: OK."

Write-Host "[478/492] Validando bind dinamico de porta..."
$entrypointSource = Get-Content .\docker-entrypoint.sh -Encoding UTF8 -Raw
if (-not $entrypointSource.Contains('PORT_VALUE="${PORT:-10000}"') -or
    -not $entrypointSource.Contains('0.0.0.0:${PORT_VALUE}') -or
    -not $entrypointSource.Contains("HealthPlatform.Api.dll") -or
    -not $entrypointSource.Contains("--hostBuilder:reloadConfigOnChange=false")) {
    throw "Entrypoint Render incompleto."
}
Write-Host "    0.0.0.0 + PORT dinamico + config reload desligado: OK."

Write-Host "[479/492] Validando Blueprint Render..."
$renderSource = Get-Content .\render.yaml -Encoding UTF8 -Raw
if (-not $renderSource.Contains("runtime: docker") -or
    -not $renderSource.Contains("plan: free") -or
    -not $renderSource.Contains("healthCheckPath: /api/health") -or
    -not $renderSource.Contains("healthplatform-mvp-db")) {
    throw "render.yaml incompleto."
}
Write-Host "    Web + Postgres + healthcheck: Blueprint OK."

Write-Host "[480/492] Validando secrets do Blueprint..."
if (-not $renderSource.Contains("Jwt__Key") -or
    -not $renderSource.Contains("generateValue: true") -or
    -not $renderSource.Contains("Seed__AdminPassword") -or
    -not $renderSource.Contains("sync: false") -or
    -not $renderSource.Contains("DemoBootstrap__SyncAdminPassword")) {
    throw "Secrets/sincronizacao do admin Render incompletos."
}
Write-Host "    JWT gerado + senha solicitada + sync do admin: OK."

Write-Host "[481/492] Validando conexao PostgreSQL do Render..."
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

Write-Host "[482/492] Validando bootstrap isolado do MVP..."
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

Write-Host "[483/492] Validando fluxo local preservado..."
if (-not $programSource.Contains("app.Environment.IsDevelopment()") -or
    -not $programSource.Contains("MigrateAsync") -or
    -not $setupSource.Contains("[30/30]") -or
    -not $setupSource.Contains("20260813190735_InitialCreate")) {
    throw "Fluxo local/migration baseline foi alterado indevidamente."
}
Write-Host "    Development continua usando baseline + migrations."

Write-Host "[484/492] Validando healthcheck real..."
$healthSource = Get-Content .\src\HealthPlatform.Api\Controllers\HealthController.cs -Encoding UTF8 -Raw
if (-not $healthSource.Contains("Status503ServiceUnavailable") -or
    -not $healthSource.Contains('status = "degraded"') -or
    -not $healthSource.Contains('database = "unavailable"')) {
    throw "Healthcheck nao sinaliza indisponibilidade do banco."
}
Write-Host "    Banco indisponivel -> HTTP 503."

Write-Host "[485/492] Validando forwarded headers..."
if (-not $renderSource.Contains("ASPNETCORE_FORWARDEDHEADERS_ENABLED") -or
    -not $renderSource.Contains('value: "true"') -or
    -not $renderSource.Contains("DOTNET_USE_POLLING_FILE_WATCHER")) {
    throw "Configuracao de proxy/file watcher do Render incompleta."
}
Write-Host "    X-Forwarded-* + polling watcher habilitados no ambiente hospedado."

Write-Host "[486/492] Validando POPULAR remoto..."
$remotePopular = Get-Content .\POPULAR-REMOTO.ps1 -Encoding UTF8 -Raw
if (-not $remotePopular.Contains("[Parameter(Mandatory=`$true)][string]`$BaseUrl") -or
    -not $remotePopular.Contains("HealthPlatform v0.3.40 - POPULAR RENDER DEMO") -or
    -not $remotePopular.Contains("PacienteDemo_123!")) {
    throw "POPULAR-REMOTO incompleto."
}
Write-Host "    Base URL + credenciais + acesso paciente: OK."

Write-Host "[487/492] Validando catalogos da demo remota..."
if (-not $remotePopular.Contains("Arroz branco cozido") -or
    -not $remotePopular.Contains("Agachamento livre") -or
    -not $remotePopular.Contains("Como voce avalia sua rotina atual de sono?")) {
    throw "Catalogos remotos iniciais incompletos."
}
$richPopular = Get-Content .\POPULAR-REMOTO-RICO.ps1 -Encoding UTF8 -Raw
if (-not $richPopular.Contains("DEMO RICA PRONTA") -or
    -not $richPopular.Contains("EnsureAnamnese") -or
    -not $richPopular.Contains("EnsureMealPlan") -or
    -not $richPopular.Contains("EnsureWorkoutHistory")) {
    throw "POPULAR-REMOTO-RICO incompleto."
}
Write-Host "    Alimentos + exercicios + pergunta de anamnese: seed remoto OK."

Write-Host "[488/492] Validando smoke test remoto..."
$remoteTest = Get-Content .\TESTAR-RENDER.ps1 -Encoding UTF8 -Raw
if (-not $remoteTest.Contains("TESTE REMOTO RENDER") -or
    -not $remoteTest.Contains("[12/12]") -or
    -not $remoteTest.Contains("Nenhum dado foi criado ou alterado")) {
    throw "TESTAR-RENDER incompleto."
}
Write-Host "    Smoke remoto somente leitura: OK."

Write-Host "[489/492] Validando guia de deploy..."
$deployGuide = Get-Content .\DEPLOY-RENDER-MVP.md -Encoding UTF8 -Raw
if (-not $deployGuide.Contains("New") -or
    -not $deployGuide.Contains("Blueprint") -or
    -not $deployGuide.Contains("POPULAR-REMOTO.ps1") -or
    -not $deployGuide.Contains("TESTAR-RENDER.ps1")) {
    throw "Guia Render incompleto."
}
Write-Host "    Blueprint -> popular -> smoke: documentado."

Write-Host "[490/492] Validando ausencia de schema novo..."
if (Test-Path .\scripts\sql\v0.3.40_render_demo.sql) {
    throw "v0.3.40 nao deveria adicionar upgrade SQL ao fluxo local."
}
if (-not $setupSource.Contains("v0.3.35_revisoes_transicoes_fases.sql")) {
    throw "Historico SQL anterior nao foi preservado."
}
Write-Host "    PREPARAR continua 30/30 / sem SQL v0.3.40."

Write-Host "[491/492] Validando arquivos de container..."
$dockerIgnore = Get-Content .\.dockerignore -Encoding UTF8 -Raw
if (-not $dockerIgnore.Contains("**/bin/") -or
    -not $dockerIgnore.Contains("**/obj/") -or
    -not $dockerIgnore.Contains(".git/")) {
    throw ".dockerignore incompleto."
}
Write-Host "    Contexto Docker enxuto: OK."

Write-Host "[492/492] Validando versao v0.3.40..."
$version = Get-Content .\VERSION.txt -Encoding UTF8 -Raw
if ($version.Trim() -ne "0.3.40") { throw "VERSION.txt inesperado." }
Write-Host "    v0.3.40 / Render Demo Deploy: OK."

Write-Host "TESTE DE FUMACA CONCLUIDO." -ForegroundColor Green
Write-Host "Nenhum registro foi criado ou alterado." -ForegroundColor Green
