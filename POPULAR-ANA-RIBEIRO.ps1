param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$AdminEmail = "admin@healthplatform.local",
    [Parameter(Mandatory=$true)][string]$SenhaAdmin,
    [string]$SenhaPaciente = "PacienteDemo_123!",
    [int]$Dias = 42
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd('/')
$anaNome = "Ana Ribeiro"
$anaEmail = "ana.ribeiro.demo@healthplatform.local"

function Json($v, [int]$depth=20) { $v | ConvertTo-Json -Depth $depth }
function Arr($v) { if ($null -eq $v) { return @() }; return @($v | Where-Object { $null -ne $_ }) }
function D([datetime]$d) { $d.ToString('yyyy-MM-dd') }
function Iso([datetime]$d) { $d.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ') }
function DateKey($v) { if ($null -eq $v) { return $null }; try { return ([datetime]$v).ToString('yyyy-MM-dd') } catch { return $null } }
function Api($method, $uri, $headers=$null, $body=$null) {
    $p = @{ Uri="$base$uri"; Method=$method }
    if ($null -ne $headers) { $p.Headers=$headers }
    if ($null -ne $body) { $p.ContentType='application/json'; $p.Body=(Json $body 30) }
    Invoke-RestMethod @p
}

Write-Host "=== HealthPlatform | Seed esportivo pesado da Ana Ribeiro ===" -ForegroundColor Cyan
Write-Host "Base: $base" -ForegroundColor DarkGray

# 1) Login administrativo
$adminLogin = Api Post '/api/auth/login' $null @{ email=$AdminEmail; senha=$SenhaAdmin }
$admin = @{ Authorization = "Bearer $($adminLogin.accessToken)" }
Write-Host "[1/9] Login administrativo: OK" -ForegroundColor Green

# 2) Localiza/cria Ana
$q = [uri]::EscapeDataString($anaNome)
$lista = Api Get "/api/pacientes?busca=$q&tamanhoPagina=20&incluirInativos=true" $admin
$ana = (Arr $lista.itens | Where-Object { $_.nome -eq $anaNome } | Select-Object -First 1)
if ($null -eq $ana) {
    $ana = Api Post '/api/pacientes' $admin @{
        nome=$anaNome; cpf='91000000001'; dataNascimento='1995-03-12'; sexo='Feminino';
        telefone='41999991001'; email=$anaEmail; profissao='Arquiteta'
    }
    Write-Host "[2/9] Ana criada: $($ana.id)" -ForegroundColor Green
} else { Write-Host "[2/9] Ana localizada: $($ana.id)" -ForegroundColor Green }
$pid = $ana.id

# 3) Garante/reset acesso do portal da Ana
$convite = Api Post "/api/pacientes/$pid/acesso" $admin @{ email=$anaEmail }
Api Post '/api/auth/paciente/ativar' $null @{ email=$anaEmail; token=$convite.activationToken; senha=$SenhaPaciente } | Out-Null
$plogin = Api Post '/api/auth/login' $null @{ email=$anaEmail; senha=$SenhaPaciente }
$patient = @{ Authorization = "Bearer $($plogin.accessToken)" }
Write-Host "[3/9] Portal da Ana ativado/resetado: OK" -ForegroundColor Green

# 4) Garante ciclo esportivo ativo
$ciclos = Arr (Api Get "/api/pacientes/$pid/ciclos-esportivos" $admin)
$ativo = $ciclos | Where-Object { $_.status -eq 'Ativo' } | Select-Object -First 1
$hoje = (Get-Date).Date
$inicioSeed = $hoje.AddDays(-[math]::Max($Dias,42)+7)
if ($null -eq $ativo) {
    Api Post "/api/pacientes/$pid/ciclos-esportivos" $admin @{
        nome='Ciclo Performance Sustentavel'; perfilEsportivo='Hipertrofia';
        objetivo='Evoluir força e composição corporal mantendo boa recuperação e consistência.';
        dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(56)); status='Ativo';
        metaTreinosSemanais=4; metaConsistenciaPercentual=80; metaPesoKg=66.5;
        faseTreinoId=$null; faseNutricionalId=$null;
        observacoes='Ciclo demonstrativo rico para alimentar Daily Athlete, carga, PRs e tendências.'
    } | Out-Null
    Write-Host "[4/9] Ciclo esportivo criado: OK" -ForegroundColor Green
} else { Write-Host "[4/9] Ciclo esportivo já existia: OK" -ForegroundColor DarkGreen }

# 5) Garante 3 metas diárias e popula histórico
function EnsureGoal([string]$nome,[decimal]$alvo,[string]$unidade) {
    $metas = Arr (Api Get "/api/pacientes/$pid/metas?incluirEncerradas=true" $admin)
    $m = $metas | Where-Object { $_.nome -eq $nome } | Select-Object -First 1
    if ($null -eq $m) {
        $m = Api Post "/api/pacientes/$pid/metas" $admin @{
            nome=$nome; tipo='Habito'; valorObjetivo=$alvo; unidade=$unidade; frequencia='Diaria';
            dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(90));
            observacoes='Meta criada pelo seed esportivo da Ana Ribeiro.'
        }
    }
    return $m
}
$metaAgua = EnsureGoal 'Hidratação diária' 3.0 'L'
$metaSono = EnsureGoal 'Sono reparador' 7.5 'h'
$metaMov = EnsureGoal 'Movimento diário' 8000 'passos'

$existingAgua = Arr (Api Get "/api/metas/$($metaAgua.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin)
$existingSono = Arr (Api Get "/api/metas/$($metaSono.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin)
$existingMov = Arr (Api Get "/api/metas/$($metaMov.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin)

for ($i=$Dias-1; $i -ge 0; $i--) {
    $dt=$hoje.AddDays(-$i); $key=D $dt
    # Oscilações determinísticas: bons dias + alguns dias de adesão parcial.
    $agua = [math]::Round(2.45 + (($i % 5) * 0.18),2)
    if (($i % 11) -eq 0) { $agua = 1.9 }
    $sono = [math]::Round(6.7 + (($i % 6) * 0.18),2)
    if (($i % 13) -eq 0) { $sono = 5.9 }
    $passos = 6500 + (($i * 913) % 5200)
    foreach ($x in @(
        @{m=$metaAgua; e=$existingAgua; v=$agua},
        @{m=$metaSono; e=$existingSono; v=$sono},
        @{m=$metaMov; e=$existingMov; v=$passos}
    )) {
        if (-not ($x.e | Where-Object { "$($_.data)" -eq $key } | Select-Object -First 1)) {
            Api Post "/api/portal/me/metas/$($x.m.id)/registro" $patient @{
                data=$key; valor=$x.v; concluida=$null; observacao='Seed histórico esportivo Ana Ribeiro'
            } | Out-Null
        }
    }
}
Write-Host "[5/9] Metas + até $Dias dias de histórico: OK" -ForegroundColor Green

# 6) Garante plano ativo. Se não existir, cria um plano simples com exercícios existentes.
$treinoAtual = Api Get '/api/portal/me/treino' $patient
if ($null -eq $treinoAtual.plano) {
    $ex = Arr (Api Get '/api/exercicios' $admin) | Select-Object -First 5
    if ($ex.Count -lt 3) { throw 'Não há exercícios suficientes cadastrados. Rode POPULAR-REMOTO-RICO.ps1 uma vez antes deste seed.' }
    $body=@{
        nome='Ana • Hipertrofia Sustentável'; objetivo='Hipertrofia com progressão técnica e recuperação adequada';
        dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(90)); status='Ativo'; observacoes='Plano criado pelo seed rico da Ana.';
        sessoes=@(
            @{ nome='Treino A • Inferiores'; diasSemana='Segunda, quinta'; ordem=1; observacoes='Progressão gradual'; itens=@(
                @{exercicioId=$ex[0].id;ordem=1;series=4;repeticoes='8-10';carga=42;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes='RPE 7-8'},
                @{exercicioId=$ex[1].id;ordem=2;series=3;repeticoes='10-12';carga=28;unidadeCarga='kg';descansoSegundos=75;tempoSegundos=$null;observacoes='Controle técnico'})},
            @{ nome='Treino B • Superiores'; diasSemana='Terça, sexta'; ordem=2; observacoes='Progressão gradual'; itens=@(
                @{exercicioId=$ex[2].id;ordem=1;series=4;repeticoes='8-10';carga=30;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes='RPE 7-8'},
                @{exercicioId=$ex[3].id;ordem=2;series=3;repeticoes='10-12';carga=20;unidadeCarga='kg';descansoSegundos=75;tempoSegundos=$null;observacoes=$null})}
        )
    }
    Api Post "/api/pacientes/$pid/treinos" $admin $body | Out-Null
    $treinoAtual = Api Get '/api/portal/me/treino' $patient
}
Write-Host "[6/9] Plano ativo para gerar performance/carga: OK" -ForegroundColor Green

# 7) Popula ~4 treinos/semana nos últimos 42 dias, com progressão + deload + alguns RPE altos.
$hist = Api Get '/api/portal/me/treinos/historico?dias=365' $patient
$histExec = Arr $hist.execucoes
$sessoes = Arr $treinoAtual.plano.sessoes
if ($sessoes.Count -gt 0) {
    $workoutDays = @(1,3,5,8,10,12,15,17,19,22,24,26,29,31,33,36,38,40)
    $n=0
    foreach ($offset in $workoutDays) {
        if ($offset -ge $Dias) { continue }
        $dt=$hoje.AddDays(-($Dias-1-$offset)).AddHours(18)
        if ($dt -gt (Get-Date)) { continue }
        $sess=$sessoes[$n % $sessoes.Count]
        $key=D $dt
        $exists=$histExec | Where-Object { (DateKey $_.dataHoraInicioUtc) -eq $key -and $_.sessao -eq $sess.nome } | Select-Object -First 1
        if ($null -ne $exists) { $n++; continue }

        # 3 semanas de base estáveis, uma mini queda/deload, e semana atual retomando.
        $week=[math]::Floor($offset/7)
        $rpe = switch ($week) { 0 {7}; 1 {7}; 2 {8}; 3 {6}; 4 {7}; default {8} }
        if (($n % 7) -eq 6) { $rpe=9 }
        $dur = 50 + (($n % 4) * 5)
        $items=@(); $j=0
        foreach ($it in (Arr $sess.itens)) {
            $baseLoad = if ($null -ne $it.carga) { [decimal]$it.carga } else { [decimal]20 }
            $progress = [decimal]([math]::Floor($n/3) * 1.25 + ($j * 0.5))
            if ($week -eq 3) { $progress -= 2.5m }
            $items += @{
                itemTreinoId=$it.id; seriesRealizadas=$it.series; repeticoesRealizadas=$(if (($n%3)-eq 0){'8'}elseif(($n%3)-eq 1){'9'}else{'10'});
                cargaRealizada=[math]::Max(0,[math]::Round($baseLoad+$progress,1)); unidadeCarga=$(if($it.unidadeCarga){$it.unidadeCarga}else{'kg'});
                esforcoPercebido=$rpe; concluido=$true; observacoes='Seed Ana: execução histórica para carga, PR e performance.'
            }
            $j++
        }
        Api Post '/api/portal/me/treinos/execucoes' $patient @{
            sessaoTreinoId=$sess.id; dataHoraInicioUtc=(Iso $dt); dataHoraFimUtc=(Iso $dt.AddMinutes($dur));
            duracaoMinutos=$dur; esforcoPercebido=$rpe; observacoes='Treino histórico do cenário rico da Ana Ribeiro.'; itens=$items
        } | Out-Null
        $n++
    }
}
Write-Host "[7/9] Histórico de treinos, RPE, carga, volume e PRs: OK" -ForegroundColor Green

# 8) Popula prontidão diária e diário (hidratação/peso/dor subjetiva) com padrão coerente.
# POST de prontidão é upsert por data, portanto pode rodar novamente sem duplicar o check-in.
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

    # Hidratação no diário: um registro por dia, evitando duplicação.
    $hasAgua = $diarioExistente | Where-Object { $_.tipo -eq 'Hidratacao' -and (DateKey $_.dataHoraUtc) -eq $key } | Select-Object -First 1
    if ($null -eq $hasAgua) {
        $litros=[math]::Round(2.4 + (($i%5)*0.18),1)
        Api Post '/api/portal/me/diario' $patient @{
            dataHoraUtc=(Iso $dt.AddHours(21)); tipo='Hidratacao'; descricao='Água total do dia'; valorNumerico=$litros; unidade='L'; escala=$null; imagemUrl=$null
        } | Out-Null
    }
    # Peso a cada 7 dias, com tendência gradual.
    if (($i % 7)-eq 0) {
        $hasPeso = $diarioExistente | Where-Object { $_.tipo -eq 'Peso' -and (DateKey $_.dataHoraUtc) -eq $key } | Select-Object -First 1
        if ($null -eq $hasPeso) {
            $peso=[math]::Round(68.4 - (($Dias-1-$i)/7.0)*0.18,1)
            Api Post '/api/portal/me/diario' $patient @{ dataHoraUtc=(Iso $dt.AddHours(7)); tipo='Peso'; descricao='Peso ao acordar'; valorNumerico=$peso; unidade='kg'; escala=$null; imagemUrl=$null } | Out-Null
        }
    }
}
Write-Host "[8/9] Prontidão + diário + hidratação + peso: OK" -ForegroundColor Green

# 9) Mostra resumo final
$home = Api Get '/api/portal/me/home' $patient
Write-Host "[9/9] Seed concluído." -ForegroundColor Green
Write-Host "" 
Write-Host "ANA RIBEIRO AGORA TEM:" -ForegroundColor Cyan
Write-Host "  - até $Dias dias de prontidão"
Write-Host "  - 3 metas diárias com histórico"
Write-Host "  - histórico esportivo suficiente para base de 3 semanas"
Write-Host "  - volume/carga/RPE e progressão de exercícios"
Write-Host "  - dados para PRs, tendências, consistência, XP e missões"
Write-Host "  - hidratação e peso no diário"
Write-Host "" 
Write-Host "Login paciente: $anaEmail" -ForegroundColor Yellow
Write-Host "Senha paciente: $SenhaPaciente" -ForegroundColor Yellow
Write-Host "" 
if ($null -ne $home.gamificacao) {
    Write-Host ("Nível atual: {0} | XP total: {1} | Consistência: {2}%" -f $home.gamificacao.nivel,$home.gamificacao.xpTotal,$home.gamificacao.consistenciaPercentual) -ForegroundColor Cyan
}
