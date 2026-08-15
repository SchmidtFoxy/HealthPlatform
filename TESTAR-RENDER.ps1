param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [string]$Email = "admin@healthplatform.local",
    [Parameter(Mandatory=$true)][string]$Senha
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd('/')

function Json($value) {
    return ($value | ConvertTo-Json -Depth 10)
}

function Invoke-OptionalGet([string]$Uri, $Headers) {
    try {
        return Invoke-RestMethod -Uri $Uri -Headers $Headers -Method Get
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            return $null
        }
        throw
    }
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " HealthPlatform v0.3.40 - TESTE REMOTO RENDER" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Base: $base" -ForegroundColor DarkGray

Write-Host "[1/12] Healthcheck..." -ForegroundColor Cyan
$health = Invoke-RestMethod -Uri "$base/api/health" -Method Get
if ($health.status -ne "ok" -or $health.version -ne "0.3.40" -or $health.database -ne "connected") {
    throw "Healthcheck remoto inesperado."
}
Write-Host "    API $($health.version) / banco $($health.database)"

Write-Host "[2/12] SPA..." -ForegroundColor Cyan
$index = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
if ($index.StatusCode -ne 200 -or $index.Content -notmatch "HealthPlatform") { throw "SPA nao carregou." }

Write-Host "[3/12] Assets..." -ForegroundColor Cyan
$js = Invoke-WebRequest -Uri "$base/app.js" -UseBasicParsing
$css = Invoke-WebRequest -Uri "$base/app.css" -UseBasicParsing
if ($js.StatusCode -ne 200 -or $css.StatusCode -ne 200) { throw "Assets remotos indisponiveis." }

Write-Host "[4/12] Login profissional..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body (Json @{ email=$Email; senha=$Senha })
if ([string]::IsNullOrWhiteSpace($login.accessToken)) { throw "Login remoto nao retornou token." }
$headers = @{ Authorization = "Bearer $($login.accessToken)" }

Write-Host "[5/12] Pacientes..." -ForegroundColor Cyan
$patients = Invoke-RestMethod -Uri "$base/api/pacientes?tamanhoPagina=20&incluirInativos=true" -Headers $headers
if ($null -eq $patients.total -or $null -eq $patients.itens) { throw "Listagem de pacientes invalida." }
Write-Host "    Pacientes: $($patients.total)"

Write-Host "[6/12] Dashboard..." -ForegroundColor Cyan
$dashboard = Invoke-RestMethod -Uri "$base/api/profissional/dashboard?offsetMinutos=0" -Headers $headers
if ($null -eq $dashboard.totalPacientesAtivos) { throw "Dashboard remoto invalido." }

Write-Host "[7/12] Catalogos..." -ForegroundColor Cyan
$foods = @(Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers)
$exercises = @(Invoke-RestMethod -Uri "$base/api/exercicios" -Headers $headers)
$markers = @(Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers)
Write-Host "    Alimentos: $($foods.Count) / exercicios: $($exercises.Count) / marcadores: $($markers.Count)"

Write-Host "[8/12] Agenda..." -ForegroundColor Cyan
$today = (Get-Date).ToString("yyyy-MM-dd")
$agenda = Invoke-RestMethod -Uri "$base/api/consultas/agenda?data=$today&offsetMinutos=0" -Headers $headers
if ($null -eq $agenda) { throw "Agenda remota invalida." }

Write-Host "[9/12] Prontuario..." -ForegroundColor Cyan
$first = @($patients.itens) | Select-Object -First 1
if ($null -ne $first) {
    $preview = Invoke-RestMethod -Uri "$base/api/pacientes/$($first.id)/preview" -Headers $headers
    if ($null -eq $preview.paciente) { throw "Preview do paciente invalido." }
} else {
    Write-Host "    Sem pacientes: ignorado."
}

Write-Host "[10/12] Analise esportiva..." -ForegroundColor Cyan
if ($null -ne $first) {
    $volume = Invoke-OptionalGet "$base/api/pacientes/$($first.id)/treinos/analise-volume?dias=30" $headers
    $progress = Invoke-OptionalGet "$base/api/pacientes/$($first.id)/treinos/progressao-exercicios?dias=180" $headers
    $signals = Invoke-OptionalGet "$base/api/pacientes/$($first.id)/treinos/analise-progresso?dias=120" $headers
    Write-Host "    Endpoints responderam (404 permitido quando o paciente nao possui treino)."
}

Write-Host "[11/12] MVP Preview..." -ForegroundColor Cyan
if ($index.Content -notmatch "MVP Preview" -or $index.Content -notmatch "Ambiente de demonstra") {
    throw "Identidade MVP Preview nao encontrada."
}

Write-Host "[12/12] Final..." -ForegroundColor Cyan
$health2 = Invoke-RestMethod -Uri "$base/api/health" -Method Get
if ($health2.version -ne "0.3.40") { throw "Versao remota mudou durante o teste." }

Write-Host ""
Write-Host "TESTE REMOTO CONCLUIDO: 12/12" -ForegroundColor Green
Write-Host "Nenhum dado foi criado ou alterado por este script." -ForegroundColor Green
