param(
    [string]$BaseUrl = "http://localhost:5180",
    [string]$AdminEmail = "admin@healthplatform.local",
    [Parameter(Mandatory=$true)][string]$SenhaAdmin,
    [string]$SenhaPaciente = "PacienteDemo_123!",
    [int]$Dias = 56
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd('/')
$lucattiNome = "Lucatti Demo"
$lucattiEmail = "lucatti.demo@healthplatform.local"

function Json($v, [int]$depth=20) { $v | ConvertTo-Json -Depth $depth }
function Arr($v) { if ($null -eq $v) { return @() }; return @($v | Where-Object { $null -ne $_ }) }
function D([datetime]$d) { $d.ToString('yyyy-MM-dd') }
function Iso([datetime]$d) { $d.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ') }
function DateKey($v) { if ($null -eq $v) { return $null }; try { return ([datetime]$v).ToString('yyyy-MM-dd') } catch { return $null } }
$seedWarnings = New-Object System.Collections.Generic.List[string]

function Get-ApiErrorBody($err) {
    try {
        $response = $err.Exception.Response
        if ($null -eq $response) { return $err.Exception.Message }
        $stream = $response.GetResponseStream()
        if ($null -eq $stream) { return $err.Exception.Message }
        $reader = New-Object System.IO.StreamReader($stream)
        $text = $reader.ReadToEnd()
        $reader.Dispose()
        if ([string]::IsNullOrWhiteSpace($text)) { return $err.Exception.Message }
        return $text
    } catch { return $err.Exception.Message }
}
function Api($method, $uri, $headers=$null, $body=$null) {
    $p = @{ Uri="$base$uri"; Method=$method }
    if ($null -ne $headers) { $p.Headers=$headers }
    if ($null -ne $body) {
        $jsonBody = Json $body 30
        $p.ContentType='application/json; charset=utf-8'
        $p.Body=[System.Text.Encoding]::UTF8.GetBytes($jsonBody)
    }
    try {
        return Invoke-RestMethod @p
    } catch {
        $detail = Get-ApiErrorBody $_
        throw "Falha API $method $uri :: $detail"
    }
}

Write-Host "=== HealthPlatform v0.14.3-r4 | Seed esportivo RICO Lucatti Demo ===" -ForegroundColor Cyan
Write-Host "Base: $base" -ForegroundColor DarkGray

Write-Host "Cenario 100% ficticio para avaliacao funcional/visual; nao representa orientacao clinica." -ForegroundColor DarkGray

# 0) Healthcheck antes de alterar qualquer dado
try {
    $health = Api Get '/api/health'
    Write-Host ("[0/10] Healthcheck: API {0} / banco {1}" -f $health.version,$health.database) -ForegroundColor Green
} catch {
    throw "API indisponivel em $base. Rode .\RODAR.ps1 e confirme que ela esta ouvindo nessa URL. Detalhe: $($_.Exception.Message)"
}

# 1) Login administrativo
try {
    $adminLogin = Api Post '/api/auth/login' $null @{ email=$AdminEmail; senha=$SenhaAdmin }
} catch {
    throw "Falha no login administrativo de $AdminEmail. Confirme a senha Seed:AdminPassword/ChangeMe_123! no ambiente local. Detalhe: $($_.Exception.Message)"
}
$admin = @{ Authorization = "Bearer $($adminLogin.accessToken)" }
Write-Host "[1/10] Login administrativo: OK" -ForegroundColor Green

# 2) Localiza/cria o atleta ficticio Lucatti Demo
$q = [uri]::EscapeDataString($lucattiNome)
$lista = Api Get "/api/pacientes?busca=$q&tamanhoPagina=20&incluirInativos=true" $admin
$lucatti = (Arr $lista.itens | Where-Object { $_.nome -eq $lucattiNome } | Select-Object -First 1)
if ($null -eq $lucatti) {
    $lucatti = Api Post '/api/pacientes' $admin @{
        nome=$lucattiNome; cpf='91000000006'; dataNascimento='1996-06-15'; sexo='Masculino';
        telefone='41999991006'; email=$lucattiEmail; profissao='Atleta demonstrativo'
    }
    Write-Host "[2/10] Lucatti Demo criado: $($lucatti.id)" -ForegroundColor Green
} else { Write-Host "[2/10] Lucatti Demo localizado: $($lucatti.id)" -ForegroundColor Green }
$pacienteIdSeed = $lucatti.id

# 3) Garante/reset acesso do portal do Lucatti Demo
$convite = Api Post "/api/pacientes/$pacienteIdSeed/acesso" $admin @{ email=$lucattiEmail }
Api Post '/api/auth/paciente/ativar' $null @{ email=$lucattiEmail; token=$convite.activationToken; senha=$SenhaPaciente } | Out-Null
$plogin = Api Post '/api/auth/login' $null @{ email=$lucattiEmail; senha=$SenhaPaciente }
$patient = @{ Authorization = "Bearer $($plogin.accessToken)" }
Write-Host "[3/10] Portal do Lucatti Demo ativado/resetado: OK" -ForegroundColor Green

# 4) Garante ciclo esportivo ativo
$ciclos = Arr (Api Get "/api/pacientes/$pacienteIdSeed/ciclos-esportivos" $admin)
$ativo = $ciclos | Where-Object { $_.status -eq 'Ativo' } | Select-Object -First 1
$hoje = (Get-Date).Date
$inicioSeed = $hoje.AddDays(-([math]::Max($Dias,1)-1))
if ($null -eq $ativo) {
    Api Post "/api/pacientes/$pacienteIdSeed/ciclos-esportivos" $admin @{
        nome='Ciclo Performance Sustentavel'; perfilEsportivo='Hipertrofia';
        objetivo='Evoluir forca e composicao corporal mantendo boa recuperacao e consistencia.';
        dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(56)); status='Ativo';
        metaTreinosSemanais=4; metaConsistenciaPercentual=80; metaPesoKg=66.5;
        faseTreinoId=$null; faseNutricionalId=$null;
        observacoes='Ciclo demonstrativo rico para alimentar Daily Athlete, carga, PRs e tendencias.'
    } | Out-Null
    Write-Host "[4/10] Ciclo esportivo criado: OK" -ForegroundColor Green
} else { Write-Host "[4/10] Ciclo esportivo ja existia: OK" -ForegroundColor DarkGreen }

# 5) Garante 3 metas diarias e popula historico.
# Metas sao enriquecimento demonstrativo: uma falha nelas nao deve impedir treinos,
# prontidao, diario, avaliacoes, performance e Coach de serem populados.
function EnsureGoalSafe([string]$nome,[decimal]$alvo,[string]$unidade) {
    try {
        $metas = Arr (Api Get "/api/pacientes/$pacienteIdSeed/metas?incluirEncerradas=true" $admin)
        $m = $metas | Where-Object { $_.nome -eq $nome } | Select-Object -First 1
        $payload = @{
            nome=$nome; tipo='Habito'; valorObjetivo=$alvo; unidade=$unidade; frequencia='Diaria';
            dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(90));
            observacoes='Meta criada pelo seed esportivo do Lucatti Demo.'
        }
        if ($null -eq $m) {
            $m = Api Post "/api/pacientes/$pacienteIdSeed/metas" $admin $payload
        } else {
            # Corrige metas deixadas por execucoes anteriores cujo periodo nao cobre o historico.
            $metaInicio = DateKey $m.dataInicio
            $metaFim = DateKey $m.dataFim
            if ($metaInicio -ne (D $inicioSeed) -or $metaFim -ne (D $hoje.AddDays(90))) {
                $m = Api Put "/api/metas/$($m.id)" $admin $payload
            }
        }
        return $m
    } catch {
        $msg = "Meta opcional ignorada [$nome]: $($_.Exception.Message)"
        $seedWarnings.Add($msg)
        Write-Host "    AVISO: $msg" -ForegroundColor Yellow
        return $null
    }
}

$metaAgua = EnsureGoalSafe 'Hidratacao diaria' 3.0 'L'
$metaSono = EnsureGoalSafe 'Sono reparador' 7.5 'h'
$metaMov  = EnsureGoalSafe 'Movimento diario' 8000 'passos'

$existingAgua=@(); $existingSono=@(); $existingMov=@()
if ($null -ne $metaAgua) { try { $existingAgua = Arr (Api Get "/api/metas/$($metaAgua.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) } catch { $seedWarnings.Add("Historico da meta agua indisponivel: $($_.Exception.Message)") } }
if ($null -ne $metaSono) { try { $existingSono = Arr (Api Get "/api/metas/$($metaSono.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) } catch { $seedWarnings.Add("Historico da meta sono indisponivel: $($_.Exception.Message)") } }
if ($null -ne $metaMov)  { try { $existingMov  = Arr (Api Get "/api/metas/$($metaMov.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) } catch { $seedWarnings.Add("Historico da meta movimento indisponivel: $($_.Exception.Message)") } }

for ($i=$Dias-1; $i -ge 0; $i--) {
    $dt=$hoje.AddDays(-$i); $key=D $dt
    $agua = [math]::Round(2.45 + (($i % 5) * 0.18),2)
    if (($i % 11) -eq 0) { $agua = 1.9 }
    $sono = [math]::Round(6.7 + (($i % 6) * 0.18),2)
    if (($i % 13) -eq 0) { $sono = 5.9 }
    $passos = 6500 + (($i * 913) % 5200)

    $goalRows=@()
    if ($null -ne $metaAgua) { $goalRows += @{m=$metaAgua; e=$existingAgua; v=$agua} }
    if ($null -ne $metaSono) { $goalRows += @{m=$metaSono; e=$existingSono; v=$sono} }
    if ($null -ne $metaMov)  { $goalRows += @{m=$metaMov;  e=$existingMov;  v=$passos} }
    foreach ($x in $goalRows) {
        if (-not ($x.e | Where-Object { "$($_.data)" -eq $key } | Select-Object -First 1)) {
            try {
                Api Post "/api/portal/me/metas/$($x.m.id)/registro" $patient @{
                    data=$key; valor=$x.v; concluida=$null; observacao='Seed historico esportivo Lucatti Demo'
                } | Out-Null
            } catch {
                $msg="Registro opcional de meta ignorado [$($x.m.nome) / $key]: $($_.Exception.Message)"
                $seedWarnings.Add($msg)
                Write-Host "    AVISO: $msg" -ForegroundColor DarkYellow
            }
        }
    }
}
$metasCriadas = @(@($metaAgua,$metaSono,$metaMov) | Where-Object { $null -ne $_ }).Count
Write-Host "[5/10] Metas demonstrativas disponiveis: $metasCriadas/3 | historico tentado por ate $Dias dias" -ForegroundColor Green

# 6) Garante plano ativo. Se nao existir, cria um plano simples com exercicios existentes.
$treinoAtual = Api Get '/api/portal/me/treino' $patient
if ($null -eq $treinoAtual.plano) {
    $ex = Arr (Api Get '/api/exercicios' $admin) | Select-Object -First 5
    if ($ex.Count -lt 3) { throw 'Nao ha exercicios suficientes cadastrados. Rode POPULAR-REMOTO-RICO.ps1 uma vez antes deste seed.' }
    $body=@{
        nome='Lucatti - Hipertrofia Sustentavel'; objetivo='Hipertrofia com progressao tecnica e recuperacao adequada';
        dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(90)); status='Ativo'; observacoes='Plano criado pelo seed rico do Lucatti Demo.';
        sessoes=@(
            @{ nome='Treino A - Inferiores'; diasSemana='Segunda, quinta'; ordem=1; observacoes='Progressao gradual'; itens=@(
                @{exercicioId=$ex[0].id;ordem=1;series=4;repeticoes='8-10';carga=42;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes='RPE 7-8'},
                @{exercicioId=$ex[1].id;ordem=2;series=3;repeticoes='10-12';carga=28;unidadeCarga='kg';descansoSegundos=75;tempoSegundos=$null;observacoes='Controle tecnico'})},
            @{ nome='Treino B - Superiores'; diasSemana='Terca, sexta'; ordem=2; observacoes='Progressao gradual'; itens=@(
                @{exercicioId=$ex[2].id;ordem=1;series=4;repeticoes='8-10';carga=30;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes='RPE 7-8'},
                @{exercicioId=$ex[3].id;ordem=2;series=3;repeticoes='10-12';carga=20;unidadeCarga='kg';descansoSegundos=75;tempoSegundos=$null;observacoes=$null})}
        )
    }
    Api Post "/api/pacientes/$pacienteIdSeed/treinos" $admin $body | Out-Null
    $treinoAtual = Api Get '/api/portal/me/treino' $patient
}
Write-Host "[6/10] Plano ativo para gerar performance/carga: OK" -ForegroundColor Green

# 7) Popula ~4 treinos/semana ao longo de toda a janela, incluindo a semana atual.
# Isso e importante para que CargaTreinoService forme base anterior e carga recente comparaveis,
# e para que PerformanceEsportivaService consiga reconhecer PRs realmente recentes.
$hist = Api Get '/api/portal/me/treinos/historico?dias=365' $patient
$histExec = Arr $hist.execucoes
$sessoes = Arr $treinoAtual.plano.sessoes
if ($sessoes.Count -gt 0) {
    $workoutDays = @(1,3,5,6,8,10,12,13,15,17,19,20,22,24,26,27,29,31,33,34,36,38,40,41,43,45,47,48,50,52,54)
    $n=0
    foreach ($offset in $workoutDays) {
        if ($offset -ge $Dias) { continue }
        $dt=$hoje.AddDays(-($Dias-1-$offset)).AddHours(18)
        if ($dt -gt (Get-Date)) { continue }
        $sess=$sessoes[$n % $sessoes.Count]
        $key=D $dt
        $exists=$histExec | Where-Object { (DateKey $_.dataHoraInicioUtc) -eq $key -and $_.sessao -eq $sess.nome } | Select-Object -First 1
        if ($null -ne $exists) { $n++; continue }

        # 3 semanas de base estaveis, uma mini queda/deload, e semana atual retomando.
        $week=[math]::Floor($offset/7)
        $rpe = switch ($week) { 0 {7}; 1 {7}; 2 {8}; 3 {6}; 4 {7}; default {8} }
        if (($n % 7) -eq 6) { $rpe=9 }
        $dur = 50 + (($n % 4) * 5)
        $items=@(); $j=0
        foreach ($it in (Arr $sess.itens)) {
            $baseLoad = if ($null -ne $it.carga) { [decimal]$it.carga } else { [decimal]20 }
            $progress = [decimal]([math]::Floor($n/3) * 1.25 + ($j * 0.5))
            if ($week -eq 3) { $progress -= [decimal]2.5 }
            $items += @{
                itemTreinoId=$it.id; seriesRealizadas=$it.series; repeticoesRealizadas=$(if (($n%3)-eq 0){'8'}elseif(($n%3)-eq 1){'9'}else{'10'});
                cargaRealizada=[math]::Max(0,[math]::Round($baseLoad+$progress,1)); unidadeCarga=$(if($it.unidadeCarga){$it.unidadeCarga}else{'kg'});
                esforcoPercebido=$rpe; concluido=$true; observacoes='Seed Lucatti Demo: execucao historica para carga, PR e performance.'
            }
            $j++
        }
        Api Post '/api/portal/me/treinos/execucoes' $patient @{
            sessaoTreinoId=$sess.id; dataHoraInicioUtc=(Iso $dt); dataHoraFimUtc=(Iso $dt.AddMinutes($dur));
            duracaoMinutos=$dur; esforcoPercebido=$rpe; observacoes='Treino historico do cenario rico da Lucatti Demo.'; itens=$items
        } | Out-Null
        $n++
    }
}
Write-Host "[7/10] Historico de treinos, RPE, carga, volume e PRs: OK" -ForegroundColor Green

# 8) Popula prontidao diaria e diario (hidratacao/peso/dor subjetiva) com padrao coerente.
# POST de prontidao e upsert por data, portanto pode rodar novamente sem duplicar o check-in.
$diarioExistente = Arr (Api Get "/api/portal/me/diario?inicio=$(D $hoje.AddDays(-$Dias+1))&fim=$(D $hoje)" $patient)
for ($i=$Dias-1; $i -ge 0; $i--) {
    $dt=$hoje.AddDays(-$i); $key=D $dt
    $sono=[math]::Round(6.6 + (($i % 7)*0.17),1)
    $energia=7; $dor=2; $disp=7; $rec=7; $qual=7
    if (($i % 12)-eq 0) { $sono=5.8; $energia=5; $dor=4; $disp=5; $rec=5; $qual=5 }
    if (($i % 17)-eq 0) { $dor=6; $energia=5; $disp=5; $rec=4 }
    if (($i % 9)-eq 0) { $energia=8; $disp=8; $rec=8; $qual=8 }
    Api Post '/api/portal/me/prontidao' $patient @{
        data=$key; sonoHoras=$sono; sonoQualidade=$qual; energiaNivel=$energia; dorNivel=$dor; disposicaoNivel=$disp; recuperacaoNivel=$rec
    } | Out-Null

    # Hidratacao no diario: um registro por dia, evitando duplicacao.
    $hasAgua = $diarioExistente | Where-Object { $_.tipo -eq 'Hidratacao' -and (DateKey $_.dataHoraUtc) -eq $key } | Select-Object -First 1
    if ($null -eq $hasAgua) {
        $litros=[math]::Round(2.4 + (($i%5)*0.18),1)
        Api Post '/api/portal/me/diario' $patient @{
            dataHoraUtc=(Iso $dt.AddHours(21)); tipo='Hidratacao'; descricao='Agua total do dia'; valorNumerico=$litros; unidade='L'; escala=$null; imagemUrl=$null
        } | Out-Null
    }
    # Peso a cada 7 dias, com tendencia gradual.
    if (($i % 7)-eq 0) {
        $hasPeso = $diarioExistente | Where-Object { $_.tipo -eq 'Peso' -and (DateKey $_.dataHoraUtc) -eq $key } | Select-Object -First 1
        if ($null -eq $hasPeso) {
            $peso=[math]::Round(68.4 - (($Dias-1-$i)/7.0)*0.18,1)
            Api Post '/api/portal/me/diario' $patient @{ dataHoraUtc=(Iso $dt.AddHours(7)); tipo='Peso'; descricao='Peso ao acordar'; valorNumerico=$peso; unidade='kg'; escala=$null; imagemUrl=$null } | Out-Null
        }
    }
}
Write-Host "[8/10] Prontidao + diario + hidratacao + peso: OK" -ForegroundColor Green

# 9) Garante serie de avaliacoes corporais para evolucao longitudinal.
$avaliacoes = Arr (Api Get "/api/pacientes/$pacienteIdSeed/avaliacoes" $admin)
$datasAval = @(42,35,28,21,14,7,0)
$idxAval = 0
foreach ($diasAtras in $datasAval) {
    if ($diasAtras -ge $Dias) { continue }
    $dt = $hoje.AddDays(-$diasAtras).AddHours(10)
    $key = D $dt
    $exists = $avaliacoes | Where-Object { (DateKey $_.dataUtc) -eq $key } | Select-Object -First 1
    if ($null -ne $exists) { $idxAval++; continue }
    $peso = [math]::Round(68.6 - ($idxAval * 0.28),1)
    $gordura = [math]::Round(27.8 - ($idxAval * 0.35),1)
    $cintura = [math]::Round(78.5 - ($idxAval * 0.45),1)
    Api Post "/api/pacientes/$pacienteIdSeed/avaliacoes" $admin @{
        consultaId=$null; dataUtc=(Iso $dt); pesoKg=$peso; alturaM=1.66;
        percentualGordura=$gordura; massaMagraKg=[math]::Round($peso*(1-$gordura/100),1);
        massaGordaKg=[math]::Round($peso*($gordura/100),1); cinturaCm=$cintura;
        abdomenCm=[math]::Round($cintura+5.5,1); quadrilCm=[math]::Round(101.0-($idxAval*0.25),1);
        pressaoSistolica=118; pressaoDiastolica=76; frequenciaCardiaca=(66-([math]::Min($idxAval,4)))
    } | Out-Null
    $idxAval++
}
Write-Host "[9/10] Avaliacoes corporais longitudinais: OK" -ForegroundColor Green

# 10) Auditoria do cenario final + resumo da camada de produto.
# Reconsulta os dominios depois dos upserts para mostrar o que realmente ficou persistido.
$histFinal = Api Get '/api/portal/me/treinos/historico?dias=365' $patient
$diarioFinal = Arr (Api Get "/api/portal/me/diario?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $patient)
$avaliacoesFinal = Arr (Api Get "/api/pacientes/$pacienteIdSeed/avaliacoes" $admin)
$metaAguaFinal = if ($null -ne $metaAgua) { Arr (Api Get "/api/metas/$($metaAgua.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) } else { @() }
$metaSonoFinal = if ($null -ne $metaSono) { Arr (Api Get "/api/metas/$($metaSono.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) } else { @() }
$metaMovFinal  = if ($null -ne $metaMov)  { Arr (Api Get "/api/metas/$($metaMov.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) } else { @() }
$homeResumo = Api Get '/api/portal/me/home' $patient
Write-Host "[10/10] Seed concluido." -ForegroundColor Green
Write-Host "" 
Write-Host "LUCATTI DEMO AGORA TEM:" -ForegroundColor Cyan
Write-Host "  - ate $Dias dias de prontidao"
Write-Host "  - 3 metas diarias com historico"
Write-Host "  - historico esportivo suficiente para base de 3 semanas"
Write-Host "  - volume/carga/RPE e progressao de exercicios"
Write-Host "  - dados para PRs, tendencias, consistencia, XP e missoes"
Write-Host "  - hidratacao e peso no diario"
Write-Host "  - avaliacoes corporais seriadas para evolucao"
Write-Host "  - contexto suficiente para o Coach Diario gerar prioridades explicaveis"
Write-Host ""
Write-Host "AUDITORIA DO SEED:" -ForegroundColor Cyan
Write-Host ("  - Treinos concluidos no historico: {0}" -f (Arr $histFinal.execucoes).Count)
Write-Host ("  - Registros de metas: agua {0}, sono {1}, movimento {2}" -f $metaAguaFinal.Count,$metaSonoFinal.Count,$metaMovFinal.Count)
Write-Host ("  - Registros de diario na janela: {0}" -f $diarioFinal.Count)
Write-Host ("  - Avaliacoes corporais totais: {0}" -f $avaliacoesFinal.Count)
Write-Host ("  - Janela historica: {0} a {1} ({2} dias)" -f (D $inicioSeed),(D $hoje),$Dias)
Write-Host "" 
Write-Host "Login paciente: $lucattiEmail" -ForegroundColor Yellow
Write-Host "Senha paciente: $SenhaPaciente" -ForegroundColor Yellow
Write-Host "" 
if ($null -ne $homeResumo.gamificacao) {
    Write-Host ("Nivel atual: {0} | XP total: {1} | Consistencia: {2}/100" -f $homeResumo.gamificacao.nivel,$homeResumo.gamificacao.xpTotal,$homeResumo.gamificacao.consistenciaScore) -ForegroundColor Cyan
}
if ($null -ne $homeResumo.tendenciaRecuperacao) {
    Write-Host ("Recuperacao: {0} | prontidao media 7 dias: {1}" -f $homeResumo.tendenciaRecuperacao.tendencia,$homeResumo.tendenciaRecuperacao.prontidaoMedia7) -ForegroundColor Cyan
}
if ($null -ne $homeResumo.cargaTreino) {
    Write-Host ("Carga: {0} | relacao com base: {1}x" -f $homeResumo.cargaTreino.classificacao,$homeResumo.cargaTreino.relacaoCargaComBase) -ForegroundColor Cyan
}
if ($null -ne $homeResumo.performance) {
    Write-Host ("Performance: {0} | PRs recentes: {1}" -f $homeResumo.performance.tendencia,$homeResumo.performance.prsRecentes) -ForegroundColor Cyan
}
Write-Host ""
Write-Host "DIAGNOSTICO ESPORTIVO DO SEED:" -ForegroundColor Cyan
if ($null -ne $homeResumo.cargaTreino) {
    Write-Host ("  Carga: treinos7={0}; duracao7={1}; carga7={2}; baseSemanal={3}; relacao={4}; RPE7={5}; classificacao={6}" -f `
        $homeResumo.cargaTreino.treinos7,$homeResumo.cargaTreino.duracaoMinutos7,$homeResumo.cargaTreino.cargaInterna7,`
        $homeResumo.cargaTreino.cargaMediaSemanalBase,$homeResumo.cargaTreino.relacaoCargaComBase,`
        $homeResumo.cargaTreino.rpeMedio7,$homeResumo.cargaTreino.classificacao)
    if ($homeResumo.cargaTreino.classificacao -eq 'DadosInsuficientes') {
        if ([int]$homeResumo.cargaTreino.treinos7 -lt 1) { Write-Host "    Motivo: nenhum treino concluido caiu nos ultimos 7 dias." -ForegroundColor Yellow }
        elseif ($null -eq $homeResumo.cargaTreino.cargaInterna7) { Write-Host "    Motivo: faltou duracao e/ou RPE valido nas sessoes recentes." -ForegroundColor Yellow }
        else { Write-Host "    Motivo: revisar dados de duracao/RPE retornados pelo servico de carga." -ForegroundColor Yellow }
    } elseif ($homeResumo.cargaTreino.classificacao -eq 'ConstruindoBase') {
        Write-Host "    Leitura: a semana atual existe, mas ainda nao ha base semanal anterior comparavel." -ForegroundColor DarkYellow
    } else {
        Write-Host "    Leitura: carga recente e linha de base estao comparaveis." -ForegroundColor DarkGreen
    }
}
if ($null -ne $homeResumo.performance) {
    Write-Host ("  Performance: treinos180={0}; exercicios={1}; volume28={2}; volumeAnterior28={3}; variacao={4}%; PRsRecentes={5}; tendencia={6}" -f `
        $homeResumo.performance.treinosPeriodo,$homeResumo.performance.exerciciosAcompanhados,$homeResumo.performance.volumeEstimado28,`
        $homeResumo.performance.volumeEstimadoAnterior28,$homeResumo.performance.variacaoVolumePercentual,`
        $homeResumo.performance.prsRecentes,$homeResumo.performance.tendencia)
    if ([int]$homeResumo.performance.prsRecentes -eq 0) {
        Write-Host "    PR: nenhum melhor carregamento superou marca anterior dentro dos ultimos 7 dias; isso pode ser esperado se a progressao recente nao criou nova maxima." -ForegroundColor DarkYellow
    } else {
        Write-Host "    PR: o seed formou pelo menos uma nova melhor marca recente de maneira natural pela progressao das cargas." -ForegroundColor DarkGreen
    }
}
if ($null -ne $homeResumo.coachDiario) {
    Write-Host ("Coach Diario: {0} | {1}" -f $homeResumo.coachDiario.estado,$homeResumo.coachDiario.titulo) -ForegroundColor Magenta
    $pos=1
    foreach ($prio in (Arr $homeResumo.coachDiario.prioridades)) {
        Write-Host ("  #{0} {1}: {2}" -f $pos,$prio.categoria,$prio.titulo) -ForegroundColor DarkMagenta
        $pos++
    }
}


if ($seedWarnings.Count -gt 0) {
    Write-Host "" 
    Write-Host "AVISOS DO SEED ($($seedWarnings.Count))" -ForegroundColor Yellow
    $seedWarnings | Select-Object -First 20 | ForEach-Object { Write-Host "  - $_" -ForegroundColor DarkYellow }
    if ($seedWarnings.Count -gt 20) { Write-Host "  ... e mais $($seedWarnings.Count-20) aviso(s)." -ForegroundColor DarkYellow }
    Write-Host "O seed continuou porque essas etapas foram classificadas como nao fatais." -ForegroundColor Yellow
}
