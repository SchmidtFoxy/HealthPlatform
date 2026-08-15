param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [string]$Email = "admin@healthplatform.local",
    [Parameter(Mandatory=$true)][string]$Senha
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$base = $BaseUrl.TrimEnd('/')

function Json($value,[int]$depth=16){ $value | ConvertTo-Json -Depth $depth }
function AsArray($v){ if($null -eq $v){return @()} ; return @($v | Where-Object {$null -ne $_}) }
function DateKey($v){ try { return ([datetime]$v).ToString("yyyy-MM-dd") } catch { return $null } }

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " HealthPlatform v0.3.40-r2 - POPULAR DEMO RICA" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Primeiro garantimos a base demo, depois aprofundamos poucos pacientes." -ForegroundColor DarkGray
Write-Host ""

# Base conhecida/validada
& "$root\POPULAR-REMOTO.ps1" -BaseUrl $base -Email $Email -Senha $Senha

Write-Host ""
Write-Host "[R1/12] Autenticando para enriquecimento..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body (Json @{email=$Email;senha=$Senha})
$headers=@{Authorization="Bearer $($login.accessToken)"}

function GetPatient([string]$name){
    $q=[uri]::EscapeDataString($name)
    $r=Invoke-RestMethod -Uri "$base/api/pacientes?busca=$q&tamanhoPagina=20&incluirInativos=true" -Headers $headers
    return (AsArray $r.itens | Where-Object {$_.nome -eq $name} | Select-Object -First 1)
}

function EnsureMarker($m){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers)
    $x=$all | Where-Object {$_.nome -eq $m.nome} | Select-Object -First 1
    if($null -eq $x){
        $x=Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $m)
    }
    return $x
}

function EnsureFood($f){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers)
    $x=$all | Where-Object {$_.nome -eq $f.nome} | Select-Object -First 1
    if($null -eq $x){
        $x=Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $f)
    }
    return $x
}

function EnsureConsultation($patientId,[string]$tag,[string]$date,[string]$status,[string]$complaint,[string]$evolution,[string]$conduct){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/consultas" -Headers $headers)
    $x=$all | Where-Object {$_.motivo -eq $tag} | Select-Object -First 1
    if($null -ne $x){return $x}
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/consultas" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        dataHoraUtc=$date;motivo=$tag;queixaPrincipal=$complaint;evolucao=$evolution;conduta=$conduct;
        orientacoes="Manter acompanhamento, registrar adesao e retornar conforme planejamento.";status=$status
    })
}

function EnsureSoap($patientId,[string]$tag,[string]$date,[string]$s,[string]$o,[string]$a,[string]$p){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/evolucoes" -Headers $headers)
    if($all | Where-Object {$_.observacoes -eq $tag} | Select-Object -First 1){return}
    Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/evolucoes" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        consultaId=$null;dataHoraUtc=$date;subjetivo=$s;objetivo=$o;avaliacao=$a;plano=$p;observacoes=$tag
    }) | Out-Null
}

function EnsureAnamnese($patientId,[string]$tag,[string]$date,$d){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/anamneses" -Headers $headers)
    if($all | Where-Object {$_.observacoes -eq $tag} | Select-Object -First 1){return}
    Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/anamneses" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        consultaId=$null;dataUtc=$date;objetivoAcompanhamento=$d.objetivo;historicoDoencas=$d.doencas;
        historicoFamiliar=$d.familiar;cirurgias=$d.cirurgias;alergias=$d.alergias;medicamentos=$d.medicamentos;
        suplementos=$d.suplementos;tabagismo=$d.tabagismo;etilismo=$d.etilismo;sonoHorasMedia=$d.sonoHoras;
        sonoQualidade=$d.sonoQualidade;despertaDuranteNoite=$d.desperta;estresseNivel=$d.estresse;
        atividadeFisica=$d.atividade;atividadeFisicaDiasSemana=$d.atividadeDias;habitoIntestinal=$d.intestinal;
        aguaLitrosDia=$d.agua;observacoes=$tag;respostasPersonalizadas=@()
    }) | Out-Null
}

function EnsureEvaluation($patientId,[string]$date,[decimal]$peso,[decimal]$altura,[decimal]$fat,[decimal]$cintura,[int]$sys,[int]$dia,[int]$hr){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/avaliacoes" -Headers $headers)
    $key=DateKey $date
    if($all | Where-Object {(DateKey $_.dataUtc) -eq $key} | Select-Object -First 1){return}
    $lean=[math]::Round($peso*(1-($fat/100)),1)
    Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/avaliacoes" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        consultaId=$null;dataUtc=$date;pesoKg=$peso;alturaM=$altura;percentualGordura=$fat;massaMagraKg=$lean;
        massaGordaKg=[math]::Round($peso-$lean,1);cinturaCm=$cintura;abdomenCm=$cintura+3;quadrilCm=$cintura+7;
        pressaoSistolica=$sys;pressaoDiastolica=$dia;frequenciaCardiaca=$hr
    }) | Out-Null
}

function EnsureLab($patientId,[string]$tag,[string]$date,$values,$markerMap){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/exames" -Headers $headers)
    if($all | Where-Object {$_.laboratorio -eq $tag} | Select-Object -First 1){return}
    $results=@()
    foreach($kv in $values.GetEnumerator()){
        $m=$markerMap[$kv.Key]
        if($null -eq $m){continue}
        $ref=$refs[$kv.Key]
        $results+=@{
            marcadorId=$m.id;valorNumerico=$kv.Value;valorTexto=$null;unidade=$m.unidadePadrao;
            referenciaMinima=$ref.min;referenciaMaxima=$ref.max;referenciaTexto=$ref.texto;observacao=$null
        }
    }
    Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/exames" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        dataColetaUtc=$date;laboratorio=$tag;observacoes="Painel laboratorial ficticio para demonstracao.";resultados=$results
    }) | Out-Null
}

function EnsureMealPlan($patientId,[string]$name,$profile,$foodMap){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/planos-alimentares" -Headers $headers)
    $existing=$all | Where-Object {$_.nome -eq $name} | Select-Object -First 1
    if($null -ne $existing){return $existing}

    function Item([string]$food,[decimal]$g,[string]$obs){
        return @{alimentoId=$foodMap[$food].id;quantidade=$g;unidade="g";quantidadeGramas=$g;observacao=$obs;substituicoes=@()}
    }

    $body=@{
        nome=$name;dataInicio="2026-07-01";dataFim=$null;status="Ativo";
        observacoes=$profile.obs;metaCalorias=$profile.kcal;metaProteinasG=$profile.protein;
        metaCarboidratosG=$profile.carb;metaGordurasG=$profile.fat;metaFibrasG=$profile.fiber;
        refeicoes=@(
            @{nome="Cafe da manha";horario="07:30:00";ordem=1;observacoes="Refeicao pratica para rotina de trabalho.";
              metaCalorias=[math]::Round($profile.kcal*0.22);metaProteinasG=[math]::Round($profile.protein*0.22);metaCarboidratosG=$null;metaGordurasG=$null;metaFibrasG=$null;
              itens=@((Item "Ovo inteiro cozido" 120 "2 ovos aproximados"),(Item "Pao integral" 60 "2 fatias"),(Item "Banana prata" 100 "1 unidade media"))},
            @{nome="Almoco";horario="12:30:00";ordem=2;observacoes="Base de comida brasileira.";
              metaCalorias=[math]::Round($profile.kcal*0.32);metaProteinasG=[math]::Round($profile.protein*0.32);metaCarboidratosG=$null;metaGordurasG=$null;metaFibrasG=$null;
              itens=@((Item "Arroz branco cozido" 140 "Porcao cozida"),(Item "Feijao carioca cozido" 120 "Concha media"),(Item "Peito de frango grelhado" 160 "Peso pronto"),(Item "Brocolis cozido" 100 "Vegetais livres"))},
            @{nome="Lanche da tarde";horario="16:30:00";ordem=3;observacoes="Priorizar saciedade.";
              metaCalorias=[math]::Round($profile.kcal*0.16);metaProteinasG=[math]::Round($profile.protein*0.16);metaCarboidratosG=$null;metaGordurasG=$null;metaFibrasG=$null;
              itens=@((Item "Iogurte natural" 170 "1 pote"),(Item "Aveia em flocos" 35 "Misturar ao iogurte"),(Item "Morango" 120 "Fruta"))},
            @{nome="Jantar";horario="20:00:00";ordem=4;observacoes="Refeicao completa e simples.";
              metaCalorias=[math]::Round($profile.kcal*0.24);metaProteinasG=[math]::Round($profile.protein*0.24);metaCarboidratosG=$null;metaGordurasG=$null;metaFibrasG=$null;
              itens=@((Item "Batata inglesa cozida" 220 "Peso cozido"),(Item "Patinho moido" 160 "Preparacao magra"),(Item "Salada variada" 150 "Folhas e legumes"))},
            @{nome="Ceia";horario="22:30:00";ordem=5;observacoes="Opcional conforme fome.";
              metaCalorias=[math]::Round($profile.kcal*0.06);metaProteinasG=[math]::Round($profile.protein*0.06);metaCarboidratosG=$null;metaGordurasG=$null;metaFibrasG=$null;
              itens=@((Item "Leite semidesnatado" 200 "1 copo"))}
        )
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/planos-alimentares" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function EnsureNutritionPhases($patientId,$plan,$profile){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers)
    $first=$all | Where-Object {$_.nome -eq "Adaptacao alimentar"} | Select-Object -First 1
    if($null -eq $first){
        $first=Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
            nome="Adaptacao alimentar";tipo="Adaptacao";objetivo="Organizar horarios, fome e qualidade alimentar.";dataInicio="2026-06-01";dataFim="2026-06-30";
            planoAlimentarId=$plan.id;metaPesoKg=$profile.firstWeight;metaAdesaoPercentual=75;duracaoMinimaDias=21;
            criterioTransicao="Rotina alimentar previsivel e boa tolerancia.";observacoes="Primeiro bloco do acompanhamento."
        })
    }
    if($first.status -ne "Concluida"){
        Invoke-RestMethod -Uri "$base/api/fases-nutricionais/$($first.id)" -Headers $headers -Method Put -ContentType "application/json" -Body (Json @{
            nome=$first.nome;tipo=$first.tipo;objetivo=$first.objetivo;dataInicio=$first.dataInicio;dataFim=$first.dataFim;planoAlimentarId=$first.planoAlimentarId;
            status="Concluida";metaPesoKg=$first.metaPesoKg;metaAdesaoPercentual=$first.metaAdesaoPercentual;duracaoMinimaDias=$first.duracaoMinimaDias;
            criterioTransicao=$first.criterioTransicao;observacoes=$first.observacoes
        }) | Out-Null
    }

    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers)
    $current=$all | Where-Object {$_.nome -eq $profile.phaseName} | Select-Object -First 1
    if($null -eq $current){
        $current=Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
            nome=$profile.phaseName;tipo=$profile.phaseType;objetivo=$profile.phaseObjective;dataInicio="2026-07-01";dataFim="2026-09-30";
            planoAlimentarId=$plan.id;metaPesoKg=$profile.targetWeight;metaAdesaoPercentual=$profile.adherence;duracaoMinimaDias=45;
            criterioTransicao=$profile.transition;observacoes="Fase nutricional ativa do cenario demo."
        })
    }
    if($current.status -ne "EmAndamento"){
        Invoke-RestMethod -Uri "$base/api/fases-nutricionais/$($current.id)" -Headers $headers -Method Put -ContentType "application/json" -Body (Json @{
            nome=$current.nome;tipo=$current.tipo;objetivo=$current.objetivo;dataInicio=$current.dataInicio;dataFim=$current.dataFim;planoAlimentarId=$current.planoAlimentarId;
            status="EmAndamento";metaPesoKg=$current.metaPesoKg;metaAdesaoPercentual=$current.metaAdesaoPercentual;duracaoMinimaDias=$current.duracaoMinimaDias;
            criterioTransicao=$current.criterioTransicao;observacoes=$current.observacoes
        }) | Out-Null
    }
    return Invoke-RestMethod -Uri "$base/api/fases-nutricionais/$($current.id)" -Headers $headers -Method Get
}

function EnsureTrainingPhases($patientId,$plan,$profile){
    if($null -eq $plan){return $null}
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-treino" -Headers $headers)
    $current=$all | Where-Object {$_.nome -eq $profile.trainingPhase} | Select-Object -First 1
    if($null -eq $current){
        $current=Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-treino" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
            nome=$profile.trainingPhase;tipo=$profile.trainingType;objetivo=$profile.trainingObjective;dataInicio="2026-07-01";dataFim="2026-09-30";
            planoTreinoId=$plan.id;metaPesoKg=$profile.targetWeight;metaAdesaoPercentual=80;duracaoMinimaDias=42;
            criterioTransicao="Manter tecnica, adesao e recuperacao adequadas antes de progredir o bloco.";observacoes="Ciclo ativo do cenario demo."
        })
    }
    if($current.status -ne "EmAndamento"){
        Invoke-RestMethod -Uri "$base/api/fases-treino/$($current.id)" -Headers $headers -Method Put -ContentType "application/json" -Body (Json @{
            nome=$current.nome;tipo=$current.tipo;objetivo=$current.objetivo;dataInicio=$current.dataInicio;dataFim=$current.dataFim;planoTreinoId=$current.planoTreinoId;
            status="EmAndamento";metaPesoKg=$current.metaPesoKg;metaAdesaoPercentual=$current.metaAdesaoPercentual;duracaoMinimaDias=$current.duracaoMinimaDias;
            criterioTransicao=$current.criterioTransicao;observacoes=$current.observacoes
        }) | Out-Null
    }
    return Invoke-RestMethod -Uri "$base/api/fases-treino/$($current.id)" -Headers $headers -Method Get
}

function EnsureCheckin($patientId,[string]$date,[decimal]$peso,[int]$food,[int]$training,[int]$hunger,[int]$energy,[int]$sleep,[int]$perception,$nutritionPhase,$trainingPhase,[string]$note){
    $history=Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/check-ins?limite=100" -Headers $headers
    $key=DateKey $date
    if(AsArray $history.itens | Where-Object {(DateKey $_.dataUtc) -eq $key} | Select-Object -First 1){return}
    Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/check-ins" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        dataUtc=$date;pesoKg=$peso;adesaoAlimentacaoPercentual=$food;adesaoTreinoPercentual=$training;fomeNivel=$hunger;
        energiaNivel=$energy;sonoNivel=$sleep;percepcaoEvolucaoNivel=$perception;
        faseNutricionalId=$(if($null -ne $nutritionPhase){$nutritionPhase.id}else{$null});
        faseTreinoId=$(if($null -ne $trainingPhase){$trainingPhase.id}else{$null});observacoes=$note
    }) | Out-Null
}

function EnsurePatientLogin($patientId,[string]$patientEmail,[string]$password){
    try{
        $ok=Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body (Json @{email=$patientEmail;senha=$password})
        if($ok.accessToken){return $ok}
    }catch{}
    $invite=Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/acesso" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{email=$patientEmail})
    Invoke-RestMethod -Uri "$base/api/auth/paciente/ativar" -Method Post -ContentType "application/json" -Body (Json @{email=$patientEmail;token=$invite.activationToken;senha=$password}) | Out-Null
    return Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body (Json @{email=$patientEmail;senha=$password})
}

function EnsureWorkoutHistory($patientId,[string]$patientEmail,[string]$password,[string]$pattern){
    $plogin=EnsurePatientLogin $patientId $patientEmail $password
    $ph=@{Authorization="Bearer $($plogin.accessToken)"}
    $current=Invoke-RestMethod -Uri "$base/api/portal/me/treino" -Headers $ph
    if($null -eq $current.plano){return}
    $history=Invoke-RestMethod -Uri "$base/api/portal/me/treinos/historico?dias=365" -Headers $ph
    $dates=@("2026-06-24T18:00:00Z","2026-07-04T18:00:00Z","2026-07-15T18:00:00Z","2026-07-27T18:00:00Z","2026-08-06T18:00:00Z","2026-08-13T18:00:00Z")
    for($i=0;$i -lt $dates.Count;$i++){
        $key=DateKey $dates[$i]
        if(AsArray $history.execucoes | Where-Object {(DateKey $_.dataHoraInicioUtc) -eq $key} | Select-Object -First 1){continue}
        $session=(AsArray $current.plano.sessoes)[$i % (AsArray $current.plano.sessoes).Count]
        $items=@()
        $j=0
        foreach($item in AsArray $session.itens){
            $baseLoad=if($null -ne $item.carga){[decimal]$item.carga}else{0}
            $increment=if($pattern -eq "progress"){$i*2}else{[math]::Floor($i/2)}
            $load=[math]::Round($baseLoad+$increment+$j,1)
            $items+=@{
                itemTreinoId=$item.id;seriesRealizadas=$item.series;repeticoesRealizadas=$(if($i -lt 2){"8"}elseif($i -lt 4){"9"}else{"10"});
                cargaRealizada=$load;unidadeCarga=$(if($item.unidadeCarga){$item.unidadeCarga}else{"kg"});
                esforcoPercebido=$(if($pattern -eq "progress"){7+($i%2)}else{7+([math]::Min(2,[math]::Floor($i/2)))});
                concluido=$true;observacoes="Execucao ficticia para historico de progressao."
            }
            $j++
        }
        Invoke-RestMethod -Uri "$base/api/portal/me/treinos/execucoes" -Headers $ph -Method Post -ContentType "application/json" -Body (Json @{
            sessaoTreinoId=$session.id;dataHoraInicioUtc=$dates[$i];dataHoraFimUtc=([datetime]$dates[$i]).AddMinutes(58).ToString("o");
            duracaoMinutos=58;esforcoPercebido=$(if($pattern -eq "progress"){7}else{8});observacoes="Treino demo concluido.";itens=$items
        }) | Out-Null
    }
}

Write-Host "[R2/12] Ampliando catalogos..." -ForegroundColor Cyan
$markerSeed=@(
    @{nome="Hemoglobina glicada";categoria="Metabolico";unidadePadrao="%"},
    @{nome="Triglicerideos";categoria="Perfil lipidico";unidadePadrao="mg/dL"},
    @{nome="Ferritina";categoria="Micronutrientes";unidadePadrao="ng/mL"},
    @{nome="Vitamina D";categoria="Micronutrientes";unidadePadrao="ng/mL"}
)
$markerMap=@{}
foreach($m in $markerSeed){$x=EnsureMarker $m;$markerMap[$m.nome]=$x}
foreach($name in @("Glicemia","LDL","HDL","TSH")){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers)
    $markerMap[$name]=$all | Where-Object {$_.nome -eq $name} | Select-Object -First 1
}
$refs=@{
    "Glicemia"=@{min=70;max=99;texto=$null};"Hemoglobina glicada"=@{min=4.0;max=5.6;texto=$null};
    "LDL"=@{min=$null;max=129;texto="Meta individual conforme risco."};"HDL"=@{min=40;max=$null;texto=$null};
    "Triglicerideos"=@{min=$null;max=149;texto=$null};"TSH"=@{min=0.4;max=4.0;texto=$null};
    "Ferritina"=@{min=30;max=300;texto="Faixa demonstrativa."};"Vitamina D"=@{min=30;max=100;texto="Faixa demonstrativa."}
}
$foodSeed=@(
    @{nome="Pao integral";categoria="Cereais";caloriasPor100g=247;proteinasPor100g=9.4;carboidratosPor100g=41.2;gordurasPor100g=3.7;fibrasPor100g=6.9},
    @{nome="Brocolis cozido";categoria="Vegetais";caloriasPor100g=35;proteinasPor100g=2.4;carboidratosPor100g=7.2;gordurasPor100g=0.4;fibrasPor100g=3.3},
    @{nome="Iogurte natural";categoria="Laticinios";caloriasPor100g=61;proteinasPor100g=3.5;carboidratosPor100g=4.7;gordurasPor100g=3.3;fibrasPor100g=0},
    @{nome="Aveia em flocos";categoria="Cereais";caloriasPor100g=394;proteinasPor100g=13.9;carboidratosPor100g=66.6;gordurasPor100g=8.5;fibrasPor100g=9.1},
    @{nome="Morango";categoria="Frutas";caloriasPor100g=32;proteinasPor100g=0.7;carboidratosPor100g=7.7;gordurasPor100g=0.3;fibrasPor100g=2.0},
    @{nome="Batata inglesa cozida";categoria="Tuberculos";caloriasPor100g=87;proteinasPor100g=1.9;carboidratosPor100g=20.1;gordurasPor100g=0.1;fibrasPor100g=1.8},
    @{nome="Patinho moido";categoria="Proteinas";caloriasPor100g=219;proteinasPor100g=35.9;carboidratosPor100g=0;gordurasPor100g=7.3;fibrasPor100g=0},
    @{nome="Salada variada";categoria="Vegetais";caloriasPor100g=30;proteinasPor100g=1.5;carboidratosPor100g=5;gordurasPor100g=0.3;fibrasPor100g=2.5},
    @{nome="Leite semidesnatado";categoria="Laticinios";caloriasPor100g=46;proteinasPor100g=3.2;carboidratosPor100g=4.8;gordurasPor100g=1.6;fibrasPor100g=0}
)
$foodMap=@{}
foreach($f in $foodSeed){$x=EnsureFood $f;$foodMap[$f.nome]=$x}
foreach($name in @("Arroz branco cozido","Feijao carioca cozido","Peito de frango grelhado","Banana prata","Ovo inteiro cozido")){
    $all=AsArray (Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers)
    $foodMap[$name]=$all | Where-Object {$_.nome -eq $name} | Select-Object -First 1
}

Write-Host "[R3/12] Preparando cenarios clinicos detalhados..." -ForegroundColor Cyan
$profiles=@{
"Ana Ribeiro"=@{
    objetivo="Reducao de gordura com melhora de energia e rotina.";doencas="Sem doencas cronicas conhecidas.";familiar="Mae com hipertensao; pai com dislipidemia.";
    cirurgias="Apendicectomia na adolescencia.";alergias="Nega alergias conhecidas.";medicamentos="Nenhum uso continuo.";suplementos="Creatina 3 g/dia.";
    tabagismo="Nao";etilismo="Social, 1-2 ocasioes por mes.";sonoHoras=7.4;sonoQualidade="Boa";desperta=$false;estresse=5;atividade="Musculacao e caminhada";atividadeDias=4;intestinal="Regular";agua=2.5;
    firstWeight=72;targetWeight=66.5;adherence=85;phaseName="Reducao gradual";phaseType="Deficit moderado";phaseObjective="Reduzir gordura mantendo massa magra.";transition="Peso entre 66-67 kg com energia e adesao sustentadas.";
    trainingPhase="Base de hipertrofia";trainingType="Hipertrofia";trainingObjective="Ganhar consistencia e progredir cargas com tecnica.";
    kcal=1850;protein=135;carb=190;fat=58;fiber=28;obs="Plano com deficit moderado, alta proteina e refeicoes simples.";
    labs3=@{"Glicemia"=86;"Hemoglobina glicada"=5.2;"LDL"=102;"HDL"=57;"Triglicerideos"=92;"TSH"=1.9;"Ferritina"=62;"Vitamina D"=38}
},
"Bruno Martins"=@{
    objetivo="Reduzir peso e risco cardiometabolico com adesao progressiva.";doencas="Hipertensao limítrofe em acompanhamento.";familiar="Pai com diabetes tipo 2; irmao com obesidade.";
    cirurgias="Nega.";alergias="Nega.";medicamentos="Losartana prescrita previamente, adesao irregular.";suplementos="Nenhum.";
    tabagismo="Ex-tabagista";etilismo="Finais de semana, acima do planejado.";sonoHoras=5.8;sonoQualidade="Ruim";desperta=$true;estresse=8;atividade="Caminhadas esporadicas";atividadeDias=2;intestinal="Regular";agua=1.4;
    firstWeight=91;targetWeight=92;adherence=70;phaseName="Retomada de adesao";phaseType="Reeducacao";phaseObjective="Interromper ganho de peso e recuperar rotina.";transition="Duas semanas com adesao acima de 75% e retomada de atividade.";
    trainingPhase="Recondicionamento";trainingType="Base";trainingObjective="Reintroduzir treino com baixa barreira e melhora de tolerancia.";
    kcal=2250;protein=160;carb=230;fat=72;fiber=30;obs="Plano estruturado para alta saciedade e praticidade durante viagens.";
    labs3=@{"Glicemia"=112;"Hemoglobina glicada"=5.9;"LDL"=171;"HDL"=35;"Triglicerideos"=188;"TSH"=2.9;"Ferritina"=155;"Vitamina D"=24}
},
"Carla Souza"=@{
    objetivo="Melhora de composicao corporal e investigacao de cansaco.";doencas="Sem diagnosticos cronicos.";familiar="Mae com hipotireoidismo.";
    cirurgias="Cesarea ha 6 anos.";alergias="Intolerancia leve a lactose relatada.";medicamentos="Nega uso continuo.";suplementos="Vitamina D em uso irregular.";
    tabagismo="Nao";etilismo="Raro.";sonoHoras=6.6;sonoQualidade="Regular";desperta=$true;estresse=6;atividade="Pilates e corrida leve";atividadeDias=3;intestinal="Regular";agua=2.0;
    firstWeight=64.2;targetWeight=62.5;adherence=82;phaseName="Manutencao ativa";phaseType="Manutencao";phaseObjective="Manter composicao enquanto investiga sintomas.";transition="Reavaliar energia, TSH e adesao antes de novo deficit.";
    trainingPhase="Condicionamento misto";trainingType="Misto";trainingObjective="Manter capacidade aerobica e forca basica.";
    kcal=1950;protein=120;carb=220;fat=62;fiber=27;obs="Plano de manutencao com distribuicao regular de carboidratos.";
    labs3=@{"Glicemia"=83;"Hemoglobina glicada"=5.1;"LDL"=91;"HDL"=65;"Triglicerideos"=78;"TSH"=5.6;"Ferritina"=38;"Vitamina D"=31}
},
"Diego Alves"=@{
    objetivo="Performance, forca e reducao leve de gordura.";doencas="Nega.";familiar="Sem antecedentes relevantes.";cirurgias="Reconstrucao de LCA ha 7 anos, sem limitacao atual.";
    alergias="Nega.";medicamentos="Nenhum.";suplementos="Creatina 5 g/dia e whey conforme necessidade.";tabagismo="Nao";etilismo="Social.";
    sonoHoras=7.8;sonoQualidade="Boa";desperta=$false;estresse=4;atividade="Musculacao estruturada";atividadeDias=5;intestinal="Regular";agua=3.2;
    firstWeight=83;targetWeight=80;adherence=90;phaseName="Performance com recomposicao";phaseType="Recomposicao";phaseObjective="Reduzir gordura preservando desempenho.";
    transition="Cintura abaixo de 85 cm com cargas principais preservadas.";trainingPhase="Progressao de forca";trainingType="Forca";trainingObjective="Progredir movimentos base mantendo volume moderado.";
    kcal=2550;protein=175;carb=305;fat=70;fiber=32;obs="Plano de performance com carboidrato concentrado ao redor do treino.";
    labs3=@{"Glicemia"=88;"Hemoglobina glicada"=5.0;"LDL"=101;"HDL"=55;"Triglicerideos"=74;"TSH"=1.7;"Ferritina"=110;"Vitamina D"=42}
},
"Elisa Ferreira"=@{
    objetivo="Retomar acompanhamento, sono e rotina alimentar.";doencas="Esteatose hepatica previamente relatada.";familiar="Mae com diabetes e hipertensao.";cirurgias="Colecistectomia.";
    alergias="Nega.";medicamentos="Metformina relatada em uso irregular.";suplementos="Nenhum.";tabagismo="Nao";etilismo="2-3 vezes/semana.";sonoHoras=5.5;sonoQualidade="Ruim";desperta=$true;estresse=8;atividade="Sedentaria";atividadeDias=0;intestinal="Irregular";agua=1.2;
    firstWeight=78.5;targetWeight=74;adherence=65;phaseName="Retorno ao basico";phaseType="Reeducacao";phaseObjective="Reconstruir adesao antes de metas agressivas.";transition="Contato regular e adesao acima de 70% por 3 semanas.";
    trainingPhase="Movimento inicial";trainingType="Adaptacao";trainingObjective="Iniciar rotina de movimento sem foco em performance.";
    kcal=1750;protein=115;carb=175;fat=62;fiber=30;obs="Plano simples com foco em saciedade e previsibilidade.";
    labs3=@{"Glicemia"=106;"Hemoglobina glicada"=5.8;"LDL"=145;"HDL"=41;"Triglicerideos"=166;"TSH"=4.5;"Ferritina"=92;"Vitamina D"=22}
}
}

$patients=@{}
foreach($name in $profiles.Keys){$patients[$name]=GetPatient $name}

Write-Host "[R4/12] Enriquecendo consultas e evolucoes SOAP..." -ForegroundColor Cyan
foreach($name in $profiles.Keys){
    $p=$patients[$name];$profile=$profiles[$name]
    EnsureConsultation $p.id "Demo rica - retorno 1" "2026-07-05T14:00:00Z" "Realizada" "Revisao de adesao, sintomas e evolucao." "Revisados registros, peso e rotina." "Ajustar prioridades e manter acompanhamento." | Out-Null
    EnsureConsultation $p.id "Demo rica - retorno 2" "2026-08-09T14:00:00Z" "Realizada" "Reavaliacao do plano e indicadores." "Comparacao com baseline e adesao recente." "Manter ou ajustar fase conforme resposta." | Out-Null
    EnsureSoap $p.id "SOAP DEMO RICA 01" "2026-07-05T14:30:00Z" "Paciente relata adaptacao progressiva ao plano e identifica principais barreiras na rotina." "Peso, medidas e registros revisados; sem sinais de intolerancia ao plano." "Evolucao compativel com o nivel de adesao registrado." "Manter estrategia por mais 3-4 semanas e reforcar registro de sono, fome e treino."
    EnsureSoap $p.id "SOAP DEMO RICA 02" "2026-08-09T14:30:00Z" "Relata percepcao mais clara de energia, fome e consistencia ao longo da semana." "Comparados peso, cintura, exames e adesao do periodo." "Resposta global revisada considerando dados objetivos e relato subjetivo." "Prosseguir fase atual e reavaliar criterios de transicao no proximo retorno."
}

Write-Host "[R5/12] Criando linha do tempo de anamnese/habitos..." -ForegroundColor Cyan
foreach($name in $profiles.Keys){
    $p=$patients[$name];$profile=$profiles[$name]
    $d1=@{}+$profile;$d1.sonoHoras=[math]::Max(4,[decimal]$profile.sonoHoras-0.6);$d1.estresse=[math]::Min(10,[int]$profile.estresse+1);$d1.agua=[math]::Max(0.8,[decimal]$profile.agua-0.5);$d1.atividadeDias=[math]::Max(0,[int]$profile.atividadeDias-1);$d1.sonoQualidade="Regular"
    $d2=@{}+$profile;$d2.sonoHoras=[math]::Max(4,[decimal]$profile.sonoHoras-0.2);$d2.estresse=[math]::Max(0,[int]$profile.estresse);$d2.agua=[math]::Max(0.8,[decimal]$profile.agua-0.2)
    EnsureAnamnese $p.id "ANAMNESE DEMO RICA - BASELINE" "2026-06-03T12:00:00Z" $d1
    EnsureAnamnese $p.id "ANAMNESE DEMO RICA - INTERMEDIARIA" "2026-07-10T12:00:00Z" $d2
    EnsureAnamnese $p.id "ANAMNESE DEMO RICA - ATUAL" "2026-08-12T12:00:00Z" $profile
}

Write-Host "[R6/12] Complementando evolucao corporal..." -ForegroundColor Cyan
$extra=@{
"Ana Ribeiro"=@(@("2026-07-15T12:00:00Z",68.8,28.5,83),@("2026-08-14T12:00:00Z",67.4,27.5,80));
"Bruno Martins"=@(@("2026-07-15T12:00:00Z",96.2,29.1,108),@("2026-08-14T12:00:00Z",99.2,30.6,112));
"Carla Souza"=@(@("2026-07-15T12:00:00Z",63.6,23.5,75),@("2026-08-14T12:00:00Z",63.3,23.1,74));
"Diego Alves"=@(@("2026-07-15T12:00:00Z",81.3,18.8,87),@("2026-08-14T12:00:00Z",80.4,18.0,85));
"Elisa Ferreira"=@(@("2026-07-15T12:00:00Z",77.6,33.2,94),@("2026-08-14T12:00:00Z",77.5,33.0,93))
}
foreach($name in $extra.Keys){
    $p=$patients[$name]
    foreach($x in $extra[$name]){EnsureEvaluation $p.id $x[0] $x[1] 1.70 $x[2] $x[3] 122 78 72}
}

Write-Host "[R7/12] Adicionando painel laboratorial ampliado..." -ForegroundColor Cyan
foreach($name in $profiles.Keys){
    $p=$patients[$name]
    EnsureLab $p.id "Demo rica - painel completo - $name" "2026-08-11T09:00:00Z" $profiles[$name].labs3 $markerMap
}

Write-Host "[R8/12] Criando planos alimentares completos e fases..." -ForegroundColor Cyan
$nutritionPhases=@{}
foreach($name in $profiles.Keys){
    $p=$patients[$name];$profile=$profiles[$name]
    $plan=EnsureMealPlan $p.id "Plano alimentar completo - $name" $profile $foodMap
    $nutritionPhases[$name]=EnsureNutritionPhases $p.id $plan $profile
}

Write-Host "[R9/12] Ligando ciclos de treino e check-ins..." -ForegroundColor Cyan
$trainingPhases=@{}
foreach($name in @("Ana Ribeiro","Bruno Martins","Diego Alves")){
    $p=$patients[$name];$profile=$profiles[$name]
    $plans=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/treinos" -Headers $headers)
    $plan=$plans | Where-Object {$_.status -eq "Ativo"} | Select-Object -First 1
    if($null -eq $plan){$plan=$plans | Select-Object -First 1}
    $trainingPhases[$name]=EnsureTrainingPhases $p.id $plan $profile
}
foreach($name in $profiles.Keys){
    $p=$patients[$name];$profile=$profiles[$name]
    $tp=$trainingPhases[$name];$np=$nutritionPhases[$name]
    $weights=if($name -eq "Ana Ribeiro"){@(69.8,68.8,67.9,67.4)}elseif($name -eq "Bruno Martins"){@(94.5,96.2,99.0,99.2)}elseif($name -eq "Carla Souza"){@(63.8,63.6,63.4,63.3)}elseif($name -eq "Diego Alves"){@(81.8,81.3,80.7,80.4)}else{@(77.9,77.6,77.3,77.5)}
    $adherence=if($name -eq "Bruno Martins"){@(62,58,55,61)}elseif($name -eq "Elisa Ferreira"){@(55,60,58,63)}else{@(78,82,86,88)}
    for($i=0;$i -lt 4;$i++){
        $date=@("2026-07-12T11:00:00Z","2026-07-26T11:00:00Z","2026-08-08T11:00:00Z","2026-08-14T11:00:00Z")[$i]
        EnsureCheckin $p.id $date $weights[$i] $adherence[$i] $(if($null -ne $tp){$adherence[$i]}else{0}) (4+($i%2)) (6+($i%3)) (6+($i%2)) (6+$i) $np $tp "Check-in ficticio rico: fome, energia, sono e adesao revisados."
    }
}

Write-Host "[R10/12] Criando historico realista de execucoes de treino..." -ForegroundColor Cyan
$patientPassword="PacienteDemo_123!"
EnsureWorkoutHistory $patients["Ana Ribeiro"].id "ana.ribeiro.demo@healthplatform.local" $patientPassword "steady"
EnsureWorkoutHistory $patients["Diego Alves"].id "diego.alves.demo@healthplatform.local" $patientPassword "progress"

Write-Host "[R11/12] Sincronizando insights/notificacoes..." -ForegroundColor Cyan
Invoke-RestMethod -Uri "$base/api/notificacoes/sincronizar" -Headers $headers -Method Post | Out-Null

Write-Host "[R12/12] Conferindo riqueza dos prontuarios..." -ForegroundColor Cyan
foreach($name in @("Ana Ribeiro","Bruno Martins","Carla Souza","Diego Alves","Elisa Ferreira")){
    $p=$patients[$name]
    $consultas=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/consultas" -Headers $headers)
    $avaliacoes=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/avaliacoes" -Headers $headers)
    $anamneses=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/anamneses" -Headers $headers)
    $evolucoes=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/evolucoes" -Headers $headers)
    $exames=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/exames" -Headers $headers)
    $planos=AsArray (Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/planos-alimentares" -Headers $headers)
    $checkins=Invoke-RestMethod -Uri "$base/api/pacientes/$($p.id)/check-ins?limite=100" -Headers $headers
    Write-Host ("  {0,-16} consultas {1} | avaliacoes {2} | anamneses {3} | SOAP {4} | exames {5} | dietas {6} | check-ins {7}" -f $name,$consultas.Count,$avaliacoes.Count,$anamneses.Count,$evolucoes.Count,$exames.Count,$planos.Count,(AsArray $checkins.itens).Count)
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " DEMO RICA PRONTA" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Poucos pacientes, mas prontuarios densos e cenarios diferentes:" -ForegroundColor Cyan
Write-Host "  Ana   -> boa resposta + dieta + treino + portal + progressao"
Write-Host "  Bruno -> piora cardiometabolica + baixa adesao + alerta"
Write-Host "  Carla -> estabilidade corporal + TSH alterado + investigacao"
Write-Host "  Diego -> performance + progressao de carga + portal"
Write-Host "  Elisa -> retorno dificil + sono ruim + baixa adesao"
Write-Host ""
Write-Host "Acesso profissional: $Email" -ForegroundColor Green
Write-Host "Paciente Ana:   ana.ribeiro.demo@healthplatform.local / PacienteDemo_123!" -ForegroundColor Green
Write-Host "Paciente Diego: diego.alves.demo@healthplatform.local / PacienteDemo_123!" -ForegroundColor Green
Write-Host ""
Write-Host "Pode rodar novamente: os principais registros sao reaproveitados por nome/data/tag." -ForegroundColor DarkGreen
