param(
    [string]$BaseUrl = "https://healthplatform-mvp.onrender.com",
    [string]$AdminEmail = "admin@healthplatform.local",
    [Parameter(Mandatory=$true)][string]$SenhaAdmin,
    [string]$UsuarioEmail = "lucatti12567@hotmail.com",
    [Parameter(Mandatory=$true)][string]$SenhaPaciente,
    [int]$Dias = 180
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd('/')
$hoje = (Get-Date).Date
$inicioSeed = $hoje.AddDays(-[math]::Max($Dias, 90) + 1)
$warnings = New-Object System.Collections.Generic.List[string]

function Json($v, [int]$depth=30) { $v | ConvertTo-Json -Depth $depth }
function Arr($v) { if ($null -eq $v) { return @() }; return @($v | Where-Object { $null -ne $_ }) }
function D([datetime]$d) { $d.ToString('yyyy-MM-dd') }
function Iso([datetime]$d) { $d.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ') }
function DateKey($v) { if ($null -eq $v) { return $null }; try { return ([datetime]$v).ToString('yyyy-MM-dd') } catch { return $null } }

function Api($method, $uri, $headers=$null, $body=$null) {
    $p = @{ Uri="$base$uri"; Method=$method }
    if ($null -ne $headers) { $p.Headers=$headers }
    if ($null -ne $body) { $p.ContentType='application/json'; $p.Body=(Json $body 40) }
    try { return Invoke-RestMethod @p }
    catch {
        $msg = $_.Exception.Message
        try {
            if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $msg += " :: $($_.ErrorDetails.Message)" }
        } catch {}
        throw "Falha API $method $uri :: $msg"
    }
}

function Warn([string]$msg) {
    $warnings.Add($msg)
    Write-Host "    AVISO: $msg" -ForegroundColor Yellow
}

function FindPatientByEmail([string]$email) {
    $q = [uri]::EscapeDataString($email)
    $lista = Api Get "/api/pacientes?busca=$q&tamanhoPagina=50&incluirInativos=true" $admin
    return (Arr $lista.itens | Where-Object { $_.email -and $_.email.ToLowerInvariant() -eq $email.ToLowerInvariant() } | Select-Object -First 1)
}

function EnsureGoal([string]$nome, [decimal]$alvo, [string]$unidade) {
    $metas = Arr (Api Get "/api/pacientes/$pacienteId/metas?incluirEncerradas=true" $admin)
    $m = $metas | Where-Object { $_.nome -eq $nome } | Select-Object -First 1
    if ($null -eq $m) {
        $m = Api Post "/api/pacientes/$pacienteId/metas" $admin @{
            nome=$nome; tipo='Habito'; valorObjetivo=$alvo; unidade=$unidade; frequencia='Diaria';
            dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(120));
            observacoes='Meta criada pelo seed oficial do usuario demo rico.'
        }
    }
    return $m
}

function EnsureConsultation([string]$tag, [datetime]$dt, [string]$status, [string]$queixa) {
    $all = Arr (Api Get "/api/pacientes/$pacienteId/consultas" $admin)
    $x = $all | Where-Object { $_.motivo -eq $tag } | Select-Object -First 1
    if ($null -ne $x) { return $x }
    return Api Post "/api/pacientes/$pacienteId/consultas" $admin @{
        dataHoraUtc=(Iso $dt); motivo=$tag; queixaPrincipal=$queixa;
        evolucao='Acompanhamento longitudinal do usuario demo rico.';
        conduta='Manter acompanhamento de treino, recuperacao, hidratacao e adesao.';
        orientacoes='Revisar indicadores esportivos e ajustar o plano apenas quando necessario.';
        status=$status
    }
}

function EnsureFollowUp([string]$resultado, [datetime]$dt, [string]$canal, [datetime]$proximo) {
    $all = Api Get "/api/pacientes/$pacienteId/followups?limite=200" $admin
    $x = Arr $all.itens | Where-Object { $_.resultado -eq $resultado } | Select-Object -First 1
    if ($null -ne $x) { return $x }
    return Api Post "/api/pacientes/$pacienteId/followups" $admin @{
        dataHoraUtc=(Iso $dt); canal=$canal; resultado=$resultado;
        observacoes='Follow-up do cenario demo rico: adesao, sintomas, carga e rotina.';
        proximoContatoUtc=(Iso $proximo)
    }
}

function EnsureMarker([string]$nome,[string]$categoria,[string]$unidade) {
    $q=[uri]::EscapeDataString($nome)
    $all=Arr (Api Get "/api/exames/marcadores?busca=$q&incluirInativos=true" $admin)
    $m=$all | Where-Object { $_.nome -eq $nome } | Select-Object -First 1
    if ($null -eq $m) {
        $m=Api Post '/api/exames/marcadores' $admin @{nome=$nome;categoria=$categoria;unidadePadrao=$unidade}
    }
    return $m
}

function EnsureLab([string]$tag,[datetime]$dt,$values) {
    $all=Arr (Api Get "/api/pacientes/$pacienteId/exames" $admin)
    if ($all | Where-Object { $_.laboratorio -eq $tag } | Select-Object -First 1) { return }
    $results=@()
    foreach($v in $values) {
        $m=$markers[$v.nome]
        if($null -eq $m){ continue }
        $results += @{ marcadorId=$m.id; valorNumerico=$v.valor; unidade=$m.unidadePadrao; referenciaMinima=$v.min; referenciaMaxima=$v.max; referenciaTexto=$null }
    }
    if($results.Count -eq 0){ return }
    Api Post "/api/pacientes/$pacienteId/exames" $admin @{ dataColetaUtc=(Iso $dt); laboratorio=$tag; observacoes='Coleta historica do usuario demo rico.'; resultados=$results } | Out-Null
}

Write-Host "" 
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " HealthPlatform v0.14.3 | USUARIO DEMO MUITO RICO" -ForegroundColor Cyan
Write-Host " $UsuarioEmail | janela de $Dias dias" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Este script NAO reseta senha e e idempotente sempre que possivel." -ForegroundColor DarkGray
Write-Host ""

$health=Api Get '/api/health'
Write-Host "[0/14] API $($health.version) | banco $($health.database)" -ForegroundColor Green

$adminLogin=Api Post '/api/auth/login' $null @{email=$AdminEmail;senha=$SenhaAdmin}
$admin=@{Authorization="Bearer $($adminLogin.accessToken)"}
Write-Host "[1/14] Login administrativo: OK" -ForegroundColor Green

$paciente=FindPatientByEmail $UsuarioEmail
if($null -eq $paciente){ throw "Paciente $UsuarioEmail nao encontrado. Este seed nao cria usuario automaticamente." }
$pacienteId=$paciente.id
Write-Host "[2/14] Paciente localizado: $($paciente.nome) / $pacienteId" -ForegroundColor Green

$patientLogin=Api Post '/api/auth/login' $null @{email=$UsuarioEmail;senha=$SenhaPaciente}
$patient=@{Authorization="Bearer $($patientLogin.accessToken)"}
Write-Host "[3/14] Login do portal do paciente: OK" -ForegroundColor Green

# Ciclo esportivo
$ciclos=Arr (Api Get "/api/pacientes/$pacienteId/ciclos-esportivos" $admin)
$ativo=$ciclos | Where-Object { $_.status -eq 'Ativo' } | Select-Object -First 1
if($null -eq $ativo){
    Api Post "/api/pacientes/$pacienteId/ciclos-esportivos" $admin @{
        nome='Ciclo Demo Performance 2026'; perfilEsportivo='Hipertrofia e condicionamento';
        objetivo='Evoluir forca, condicionamento, composicao corporal e consistencia com boa recuperacao.';
        dataInicio=(D $inicioSeed); dataFim=(D $hoje.AddDays(90)); status='Ativo';
        metaTreinosSemanais=4; metaConsistenciaPercentual=82; metaPesoKg=78.0;
        faseTreinoId=$null; faseNutricionalId=$null;
        observacoes='Ciclo oficial do usuario demo rico. Dados historicos coerentes para demonstracao.'
    } | Out-Null
}
Write-Host "[4/14] Ciclo esportivo: OK" -ForegroundColor Green

# Metas e 6 meses de registros
$metaAgua=EnsureGoal 'Hidratacao diaria' 3.2 'L'
$metaSono=EnsureGoal 'Sono reparador' 7.5 'h'
$metaPassos=EnsureGoal 'Movimento diario' 8500 'passos'
$metaMobilidade=EnsureGoal 'Mobilidade / alongamento' 15 'min'
$metas=@($metaAgua,$metaSono,$metaPassos,$metaMobilidade)
$goalHistory=@{}
foreach($m in $metas){
    try { $goalHistory[$m.id]=Arr (Api Get "/api/metas/$($m.id)/registros?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $admin) }
    catch { $goalHistory[$m.id]=@(); Warn "Historico da meta $($m.nome) indisponivel: $($_.Exception.Message)" }
}
for($i=$Dias-1;$i -ge 0;$i--){
    $dt=$hoje.AddDays(-$i); $key=D $dt
    $phase=($Dias-1-$i)
    $agua=[math]::Round(2.55 + (($phase % 6)*0.14),2); if(($phase%19)-eq 0){$agua=2.1}
    $sono=[math]::Round(6.7 + (($phase % 7)*0.17),1); if(($phase%23)-eq 0){$sono=5.8}
    $passos=6800 + (($phase*877)%5200)
    $mob=if(($phase%5)-eq 0){8}else{15 + (($phase%3)*5)}
    $rows=@(
        @{m=$metaAgua;v=$agua}, @{m=$metaSono;v=$sono}, @{m=$metaPassos;v=$passos}, @{m=$metaMobilidade;v=$mob}
    )
    foreach($r in $rows){
        $existing=$goalHistory[$r.m.id] | Where-Object { "$(DateKey $_.data)" -eq $key } | Select-Object -First 1
        if($null -ne $existing){ continue }
        try { Api Post "/api/portal/me/metas/$($r.m.id)/registro" $patient @{data=$key;valor=$r.v;concluida=$null;observacao='Seed demo rico'} | Out-Null }
        catch { Warn "Meta $($r.m.nome) em ${key}: $($_.Exception.Message)" }
    }
}
Write-Host "[5/14] 4 metas + ate $Dias dias de historico: OK" -ForegroundColor Green

# Plano de treino
$treinoAtual=Api Get '/api/portal/me/treino' $patient
if($null -eq $treinoAtual.plano){
    $ex=Arr (Api Get '/api/exercicios' $admin) | Select-Object -First 8
    if($ex.Count -lt 5){ throw 'Catalogo de exercicios insuficiente para o seed rico.' }
    $body=@{
        nome='Performance Demo 4x Semana'; objetivo='Forca, hipertrofia e condicionamento com progressao sustentavel';
        dataInicio=(D $inicioSeed);dataFim=(D $hoje.AddDays(120));status='Ativo';
        observacoes='Plano principal do usuario demo rico.';
        sessoes=@(
            @{nome='Upper A';diasSemana='Segunda';ordem=1;observacoes='Forca e tecnica';itens=@(
                @{exercicioId=$ex[0].id;ordem=1;series=4;repeticoes='6-8';carga=48;unidadeCarga='kg';descansoSegundos=120;tempoSegundos=$null;observacoes='RPE 7-8'},
                @{exercicioId=$ex[1].id;ordem=2;series=4;repeticoes='8-10';carga=36;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes='Controle tecnico'})},
            @{nome='Lower A';diasSemana='Terca';ordem=2;observacoes='Base de pernas';itens=@(
                @{exercicioId=$ex[2].id;ordem=1;series=4;repeticoes='6-8';carga=72;unidadeCarga='kg';descansoSegundos=120;tempoSegundos=$null;observacoes='Progressao gradual'},
                @{exercicioId=$ex[3].id;ordem=2;series=3;repeticoes='10-12';carga=42;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes=$null})},
            @{nome='Upper B';diasSemana='Quinta';ordem=3;observacoes='Volume moderado';itens=@(
                @{exercicioId=$ex[4].id;ordem=1;series=4;repeticoes='8-10';carga=44;unidadeCarga='kg';descansoSegundos=90;tempoSegundos=$null;observacoes=$null},
                @{exercicioId=$ex[5].id;ordem=2;series=3;repeticoes='10-12';carga=28;unidadeCarga='kg';descansoSegundos=75;tempoSegundos=$null;observacoes=$null})},
            @{nome='Lower B';diasSemana='Sexta';ordem=4;observacoes='Hipertrofia e posterior';itens=@(
                @{exercicioId=$ex[6].id;ordem=1;series=4;repeticoes='8-10';carga=62;unidadeCarga='kg';descansoSegundos=105;tempoSegundos=$null;observacoes=$null},
                @{exercicioId=$ex[7].id;ordem=2;series=3;repeticoes='12-15';carga=32;unidadeCarga='kg';descansoSegundos=75;tempoSegundos=$null;observacoes=$null})}
        )
    }
    Api Post "/api/pacientes/$pacienteId/treinos" $admin $body | Out-Null
    $treinoAtual=Api Get '/api/portal/me/treino' $patient
}
Write-Host "[6/14] Plano de treino ativo: OK" -ForegroundColor Green

# Historico de treino: seg/ter/qui/sex, com blocos, deload e progressao.
$hist=Api Get '/api/portal/me/treinos/historico?dias=365' $patient
$histExec=Arr $hist.execucoes
$sessoes=Arr $treinoAtual.plano.sessoes
$sessionIndex=0
for($i=$Dias-1;$i -ge 0;$i--){
    $dt=$hoje.AddDays(-$i)
    $dow=[int]$dt.DayOfWeek
    if($dow -notin @(1,2,4,5)){ continue }
    $sess=$sessoes[$sessionIndex % $sessoes.Count]
    $sessionIndex++
    $key=D $dt
    if($histExec | Where-Object { (DateKey $_.dataHoraInicioUtc) -eq $key -and $_.sessao -eq $sess.nome } | Select-Object -First 1){ continue }
    $week=[math]::Floor(($Dias-1-$i)/7)
    $deload=(($week % 6)-eq 5)
    $rpe=if($deload){6}else{7 + ($week % 2)}
    if((($Dias-1-$i)%37)-eq 0){$rpe=9}
    $dur=48 + (($week+$sessionIndex)%4)*6
    $items=@();$j=0
    foreach($it in (Arr $sess.itens)){
        $baseLoad=if($null -ne $it.carga){[decimal]$it.carga}else{20}
        $prog=[decimal]([math]::Floor($week/2)*1.25 + $j*0.75)
        if($deload){$prog-=5}
        $items+=@{
            itemTreinoId=$it.id;seriesRealizadas=$it.series;
            repeticoesRealizadas=$(if(($week%3)-eq 0){'8'}elseif(($week%3)-eq 1){'9'}else{'10'});
            cargaRealizada=[math]::Max(0,[math]::Round($baseLoad+$prog,1));unidadeCarga=$(if($it.unidadeCarga){$it.unidadeCarga}else{'kg'});
            esforcoPercebido=$rpe;concluido=$true;
            observacoes=$(if($deload){'Semana de deload demonstrativa.'}else{'Execucao historica do usuario demo.'})
        };$j++
    }
    try {
        Api Post '/api/portal/me/treinos/execucoes' $patient @{
            sessaoTreinoId=$sess.id;dataHoraInicioUtc=(Iso $dt.AddHours(18));dataHoraFimUtc=(Iso $dt.AddHours(18).AddMinutes($dur));
            duracaoMinutos=$dur;esforcoPercebido=$rpe;
            observacoes=$(if($deload){'Deload planejado; tecnica e recuperacao priorizadas.'}else{'Sessao concluida com boa aderencia ao plano.'});itens=$items
        } | Out-Null
    } catch { Warn "Treino $key / $($sess.nome): $($_.Exception.Message)" }
}
Write-Host "[7/14] Historico de treino (~4x/semana, com progressao e deload): OK" -ForegroundColor Green

# Prontidao + diario rico
$diario=Arr (Api Get "/api/portal/me/diario?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $patient)
for($i=$Dias-1;$i -ge 0;$i--){
    $dt=$hoje.AddDays(-$i);$key=D $dt;$phase=$Dias-1-$i
    $sono=[math]::Round(6.6+(($phase%8)*0.16),1);$qual=7;$energia=7;$dor=2;$disp=7;$rec=7
    if(($phase%21)-eq 0){$sono=5.8;$qual=5;$energia=5;$dor=4;$disp=5;$rec=5}
    if(($phase%29)-eq 0){$dor=6;$energia=5;$disp=5;$rec=4}
    if(($phase%11)-eq 0){$energia=8;$disp=8;$rec=8;$qual=8}
    try { Api Post '/api/portal/me/prontidao' $patient @{data=$key;sonoHoras=$sono;sonoQualidade=$qual;energiaNivel=$energia;dorNivel=$dor;disposicaoNivel=$disp;recuperacaoNivel=$rec} | Out-Null }
    catch { Warn "Prontidao ${key}: $($_.Exception.Message)" }

    $litros=[math]::Round(2.5+(($phase%6)*0.16),1)
    if(-not ($diario | Where-Object {$_.tipo -eq 'Hidratacao' -and (DateKey $_.dataHoraUtc)-eq $key}|Select-Object -First 1)){
        try{Api Post '/api/portal/me/diario' $patient @{dataHoraUtc=(Iso $dt.AddHours(21));tipo='Hidratacao';descricao='Agua total do dia';valorNumerico=$litros;unidade='L';escala=$null;imagemUrl=$null}|Out-Null}catch{Warn "Diario hidratacao $key"}
    }
    if(($phase%2)-eq 0 -and -not ($diario | Where-Object {$_.tipo -eq 'Energia' -and (DateKey $_.dataHoraUtc)-eq $key}|Select-Object -First 1)){
        try{Api Post '/api/portal/me/diario' $patient @{dataHoraUtc=(Iso $dt.AddHours(16));tipo='Energia';descricao='Energia ao longo do dia';valorNumerico=$energia;unidade=$null;escala=$energia;imagemUrl=$null}|Out-Null}catch{Warn "Diario energia $key"}
    }
    if(($phase%3)-eq 0 -and -not ($diario | Where-Object {$_.tipo -eq 'Dor' -and (DateKey $_.dataHoraUtc)-eq $key}|Select-Object -First 1)){
        try{Api Post '/api/portal/me/diario' $patient @{dataHoraUtc=(Iso $dt.AddHours(20));tipo='Dor';descricao='Desconforto muscular pos treino';valorNumerico=$dor;unidade=$null;escala=$dor;imagemUrl=$null}|Out-Null}catch{Warn "Diario dor $key"}
    }
    if(($phase%7)-eq 0 -and -not ($diario | Where-Object {$_.tipo -eq 'Peso' -and (DateKey $_.dataHoraUtc)-eq $key}|Select-Object -First 1)){
        $peso=[math]::Round(82.4-($phase/7.0)*0.08 + [math]::Sin($phase/9.0)*0.25,1)
        try{Api Post '/api/portal/me/diario' $patient @{dataHoraUtc=(Iso $dt.AddHours(7));tipo='Peso';descricao='Peso ao acordar';valorNumerico=$peso;unidade='kg';escala=$null;imagemUrl=$null}|Out-Null}catch{Warn "Diario peso $key"}
    }
}
Write-Host "[8/14] Prontidao + diario (agua, energia, dor, peso): OK" -ForegroundColor Green

# Avaliacoes corporais mensais
$avaliacoes=Arr (Api Get "/api/pacientes/$pacienteId/avaliacoes" $admin)
$meses=[math]::Min(7,[math]::Floor($Dias/28)+1)
for($k=$meses-1;$k -ge 0;$k--){
    $dt=$hoje.AddDays(-($k*28)).AddHours(10);$key=D $dt
    if($avaliacoes|Where-Object{(DateKey $_.dataUtc)-eq $key}|Select-Object -First 1){continue}
    $idx=$meses-1-$k;$peso=[math]::Round(82.4-($idx*0.55),1);$fat=[math]::Round(22.8-($idx*0.65),1);$waist=[math]::Round(91.0-($idx*1.05),1)
    try{Api Post "/api/pacientes/$pacienteId/avaliacoes" $admin @{
        consultaId=$null;dataUtc=(Iso $dt);pesoKg=$peso;alturaM=1.78;percentualGordura=$fat;
        massaMagraKg=[math]::Round($peso*(1-$fat/100),1);massaGordaKg=[math]::Round($peso*($fat/100),1);
        cinturaCm=$waist;abdomenCm=[math]::Round($waist+4.5,1);quadrilCm=[math]::Round(101.5-($idx*0.35),1);
        pressaoSistolica=(122-[math]::Min($idx,4));pressaoDiastolica=78;frequenciaCardiaca=(72-[math]::Min($idx*2,10))
    }|Out-Null}catch{Warn "Avaliacao ${key}: $($_.Exception.Message)"}
}
Write-Host "[9/14] Avaliacoes corporais longitudinais: OK" -ForegroundColor Green

# Consultas e agenda
$consultas=@(
    @{tag='DEMO | Avaliacao inicial';d=-168;s='Concluida';q='Inicio do acompanhamento esportivo e definicao de objetivos.'},
    @{tag='DEMO | Revisao 1';d=-120;s='Concluida';q='Revisao de aderencia, treino, sono e composicao corporal.'},
    @{tag='DEMO | Revisao 2';d=-75;s='Concluida';q='Boa evolucao; ajuste de progressao e recuperacao.'},
    @{tag='DEMO | Revisao 3';d=-35;s='Concluida';q='Monitoramento de carga, dor e resposta ao ciclo.'},
    @{tag='DEMO | Retorno atual';d=-7;s='Concluida';q='Revisao do bloco atual e consolidacao da rotina.'},
    @{tag='DEMO | Proxima revisao';d=14;s='Agendada';q='Reavaliacao de performance e metas do ciclo.'},
    @{tag='DEMO | Reavaliacao corporal';d=42;s='Agendada';q='Avaliacao corporal e planejamento do proximo bloco.'}
)
foreach($c in $consultas){try{EnsureConsultation $c.tag $hoje.AddDays($c.d).AddHours(14) $c.s $c.q|Out-Null}catch{Warn "Consulta $($c.tag): $($_.Exception.Message)"}}
Write-Host "[10/14] Consultas passadas + agenda futura: OK" -ForegroundColor Green

# Follow-ups
for($k=0;$k -lt 8;$k++){
    $dt=$hoje.AddDays(-($k*16+3)).AddHours(12)
    $canal=@('WhatsApp','Email','Telefone','WhatsApp')[$k%4]
    $result="DEMO | Follow-up #$($k+1) | aderencia e recuperacao acompanhadas"
    try{EnsureFollowUp $result $dt $canal $dt.AddDays(14)|Out-Null}catch{Warn "Follow-up $($k+1): $($_.Exception.Message)"}
}
Write-Host "[11/14] Follow-ups longitudinais: OK" -ForegroundColor Green

# Exames laboratoriais - opcionais, mas muito bons para a demo.
try{
    $markers=@{}
    foreach($spec in @(
        @{n='Glicemia';c='Metabolico';u='mg/dL'},@{n='Hemoglobina';c='Hematologico';u='g/dL'},@{n='Ferritina';c='Hematologico';u='ng/mL'},
        @{n='Vitamina D';c='Vitaminas';u='ng/mL'},@{n='Creatinina';c='Renal';u='mg/dL'},@{n='Colesterol LDL';c='Lipidico';u='mg/dL'},
        @{n='Colesterol HDL';c='Lipidico';u='mg/dL'},@{n='Triglicerides';c='Lipidico';u='mg/dL'}
    )){$markers[$spec.n]=EnsureMarker $spec.n $spec.c $spec.u}
    $labs=@(
        @{tag='DEMO-LAB-2026-04';d=-150;vals=@(
            @{nome='Glicemia';valor=96;min=70;max=99},@{nome='Hemoglobina';valor=14.6;min=13;max=17.5},@{nome='Ferritina';valor=88;min=30;max=400},@{nome='Vitamina D';valor=27;min=30;max=100},@{nome='Creatinina';valor=1.04;min=.7;max=1.3},@{nome='Colesterol LDL';valor=128;min=0;max=130},@{nome='Colesterol HDL';valor=44;min=40;max=90},@{nome='Triglicerides';valor=148;min=0;max=150})},
        @{tag='DEMO-LAB-2026-07';d=-70;vals=@(
            @{nome='Glicemia';valor=91;min=70;max=99},@{nome='Hemoglobina';valor=14.8;min=13;max=17.5},@{nome='Ferritina';valor=94;min=30;max=400},@{nome='Vitamina D';valor=34;min=30;max=100},@{nome='Creatinina';valor=1.08;min=.7;max=1.3},@{nome='Colesterol LDL';valor=116;min=0;max=130},@{nome='Colesterol HDL';valor=48;min=40;max=90},@{nome='Triglicerides';valor=124;min=0;max=150})},
        @{tag='DEMO-LAB-2026-09';d=-8;vals=@(
            @{nome='Glicemia';valor=88;min=70;max=99},@{nome='Hemoglobina';valor=15.0;min=13;max=17.5},@{nome='Ferritina';valor=102;min=30;max=400},@{nome='Vitamina D';valor=39;min=30;max=100},@{nome='Creatinina';valor=1.07;min=.7;max=1.3},@{nome='Colesterol LDL';valor=108;min=0;max=130},@{nome='Colesterol HDL';valor=51;min=40;max=90},@{nome='Triglicerides';valor=109;min=0;max=150})}
    )
    foreach($l in $labs){EnsureLab $l.tag $hoje.AddDays($l.d).AddHours(8) $l.vals}
    Write-Host "[12/14] 3 coletas laboratoriais + evolucao de marcadores: OK" -ForegroundColor Green
}catch{Warn "Camada laboratorial ignorada: $($_.Exception.Message)";Write-Host "[12/14] Exames laboratoriais: ignorados com aviso" -ForegroundColor Yellow}

# Resumo final e verificacao das camadas esportivas
$home=Api Get '/api/portal/me/home' $patient
$histFinal=Api Get '/api/portal/me/treinos/historico?dias=365' $patient
$diarioFinal=Arr (Api Get "/api/portal/me/diario?inicio=$(D $inicioSeed)&fim=$(D $hoje)" $patient)
Write-Host "[13/14] Recalculando Home / Coach / carga / performance: OK" -ForegroundColor Green

Write-Host "[14/14] SEED CONCLUIDO" -ForegroundColor Green
Write-Host ""
Write-Host "USUARIO DEMO RICO AGORA TEM:" -ForegroundColor Cyan
Write-Host "  - ate $Dias dias de prontidao diaria"
Write-Host "  - aproximadamente 4 treinos por semana com progressao + deload"
Write-Host "  - 4 metas com ate $Dias dias de historico"
Write-Host "  - diario com hidratacao, energia, dor e peso"
Write-Host "  - avaliacoes corporais longitudinais"
Write-Host "  - consultas concluidas e futuras"
Write-Host "  - follow-ups longitudinais"
Write-Host "  - exames laboratoriais seriados (quando suportados pelo ambiente)"
Write-Host "  - dados suficientes para XP, streak, missoes, carga, Recovery Pulse, tendencias, PRs e Coach Diario"
Write-Host ""
Write-Host ("Execucoes de treino no historico: {0}" -f (Arr $histFinal.execucoes).Count) -ForegroundColor Cyan
Write-Host ("Registros de diario no periodo: {0}" -f $diarioFinal.Count) -ForegroundColor Cyan
if($null -ne $home.gamificacao){Write-Host ("Nivel {0} | XP {1} | streak {2} | consistencia {3}/100" -f $home.gamificacao.nivel,$home.gamificacao.xpTotal,$home.gamificacao.streakDias,$home.gamificacao.consistenciaScore) -ForegroundColor Magenta}
if($null -ne $home.cargaTreino){Write-Host ("Carga: {0} | 7d={1} | relacao base={2}" -f $home.cargaTreino.classificacao,$home.cargaTreino.carga7Dias,$home.cargaTreino.relacaoCargaComBase) -ForegroundColor Magenta}
if($null -ne $home.performance){Write-Host ("Performance: {0} | PRs recentes={1}" -f $home.performance.tendencia,$home.performance.prsRecentes) -ForegroundColor Magenta}
if($null -ne $home.tendenciaRecuperacao){Write-Host ("Recuperacao: {0} | prontidao media 7d={1}" -f $home.tendenciaRecuperacao.tendencia,$home.tendenciaRecuperacao.prontidaoMedia7) -ForegroundColor Magenta}
Write-Host ""
Write-Host "Login demo: $UsuarioEmail" -ForegroundColor Yellow
Write-Host "A senha NAO foi alterada pelo script." -ForegroundColor Yellow
if($warnings.Count -gt 0){
    Write-Host ""
    Write-Host "AVISOS ($($warnings.Count))" -ForegroundColor Yellow
    $warnings | Select-Object -First 30 | ForEach-Object { Write-Host "  - $_" -ForegroundColor DarkYellow }
    if($warnings.Count -gt 30){Write-Host "  ... e mais $($warnings.Count-30) aviso(s)." -ForegroundColor DarkYellow}
}
