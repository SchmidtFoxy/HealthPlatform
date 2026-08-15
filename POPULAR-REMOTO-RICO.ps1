param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [string]$Email = "admin@healthplatform.local",
    [Parameter(Mandatory=$true)][string]$Senha
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$base = $BaseUrl.TrimEnd('/')
$email = $Email

function Json($value, [int]$depth = 12) {
    return ($value | ConvertTo-Json -Depth $depth)
}


function As-Array($value) {
    if ($null -eq $value) { return @() }
    return @($value | Where-Object { $null -ne $_ })
}

function Date-Key($value) {
    if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) { return $null }
    try {
        return ([datetime]$value).ToString("yyyy-MM-dd")
    } catch {
        return $null
    }
}


function Get-PatientByName([string]$name) {
    $q = [uri]::EscapeDataString($name)
    $lista = Invoke-RestMethod -Uri "$base/api/pacientes?busca=$q&tamanhoPagina=20&incluirInativos=true" -Headers $headers
    return ((As-Array $lista.itens) | Where-Object { $_.nome -eq $name } | Select-Object -First 1)
}

function Ensure-Patient($d) {
    $p = Get-PatientByName $d.nome
    if ($null -eq $p) {
        $body = @{
            nome = $d.nome
            cpf = $d.cpf
            dataNascimento = $d.dataNascimento
            sexo = $d.sexo
            telefone = $d.telefone
            email = $d.email
            profissao = $d.profissao
        }
        $p = Invoke-RestMethod -Uri "$base/api/pacientes" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
        Write-Host "      + Paciente criado: $($d.nome)" -ForegroundColor Green
    } else {
        Write-Host "      = Paciente ja existe: $($d.nome)" -ForegroundColor DarkGreen
    }
    return $p
}

function Ensure-Consultation($patientId, $tag, $dateUtc, $status, $motivo) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/consultas" -Headers $headers)
    $existing = $all | Where-Object { $_.motivo -eq $tag } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        dataHoraUtc = $dateUtc
        motivo = $tag
        queixaPrincipal = $motivo
        evolucao = "Registro de demonstracao v0.3.6."
        conduta = "Acompanhamento longitudinal conforme dados do cenario demo."
        orientacoes = "Manter acompanhamento e revisar indicadores na proxima consulta."
        status = $status
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/consultas" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Ensure-Evaluation($patientId, $dateUtc, $weight, $height, $fat, $waist, $sys, $dia, $hr) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/avaliacoes" -Headers $headers)
    $targetDate = Date-Key $dateUtc
    $existing = $all | Where-Object {
        $itemDate = Date-Key $_.dataUtc
        $null -ne $itemDate -and $itemDate -eq $targetDate
    } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $lean = [math]::Round($weight * (1 - ($fat / 100.0)), 1)
    $fatKg = [math]::Round($weight - $lean, 1)
    $body = @{
        consultaId = $null
        dataUtc = $dateUtc
        pesoKg = $weight
        alturaM = $height
        percentualGordura = $fat
        massaMagraKg = $lean
        massaGordaKg = $fatKg
        cinturaCm = $waist
        abdomenCm = $waist + 3
        quadrilCm = $waist + 7
        pressaoSistolica = $sys
        pressaoDiastolica = $dia
        frequenciaCardiaca = $hr
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/avaliacoes" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Ensure-Lab($patientId, $labTag, $dateUtc, $values) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/exames" -Headers $headers)
    $existing = $all | Where-Object { $_.laboratorio -eq $labTag } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $results = @()
    foreach ($x in $values) {
        $marker = $markers[$x.nome]
        if ($null -eq $marker) { continue }
        $r = @{
            marcadorId = $marker.id
            valorNumerico = $x.valor
            unidade = $marker.unidadePadrao
            referenciaMinima = $x.min
            referenciaMaxima = $x.max
            referenciaTexto = $x.refTexto
        }
        $results += $r
    }
    $body = @{
        dataColetaUtc = $dateUtc
        laboratorio = $labTag
        observacoes = "Coleta criada pelo POPULAR.ps1 da v0.3.6."
        resultados = $results
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/exames" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Ensure-Goal($patientId, $goalName, $target, $unit, $frequency, $records) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/metas?incluirEncerradas=true" -Headers $headers)
    $goal = $all | Where-Object { $_.nome -eq $goalName } | Select-Object -First 1
    if ($null -eq $goal) {
        $body = @{
            nome = $goalName
            tipo = "Habito"
            valorObjetivo = $target
            unidade = $unit
            frequencia = $frequency
            dataInicio = "2026-08-01"
            dataFim = "2026-12-31"
            observacoes = "Meta demo v0.3.6."
        }
        $goal = Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/metas" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
    }

    $existing = As-Array (Invoke-RestMethod -Uri "$base/api/metas/$($goal.id)/registros?inicio=2026-08-01&fim=2026-08-31" -Headers $headers)
    foreach ($r in $records) {
        if ($existing | Where-Object { $_.data -eq $r.data }) { continue }
        $body = @{
            data = $r.data
            valor = $r.valor
            concluida = $r.concluida
            observacao = "Registro demo v0.3.6"
        }
        Invoke-RestMethod -Uri "$base/api/metas/$($goal.id)/registros" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body) | Out-Null
    }
}

function Ensure-Diary($patientId, $dateUtc, $type, $description, $value, $unit, $scale) {
    $from = Date-Key $dateUtc
    if ($null -eq $from) { throw "Data invalida no cenario de diario: $dateUtc" }
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/diario?inicio=$from&fim=$from" -Headers $headers)
    $existing = $all | Where-Object { $_.descricao -eq $description } | Select-Object -First 1
    if ($null -ne $existing) { return }

    $body = @{
        dataHoraUtc = $dateUtc
        tipo = $type
        descricao = $description
        valorNumerico = $value
        unidade = $unit
        escala = $scale
        imagemUrl = $null
    }
    Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/diario" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body) | Out-Null
}

function Ensure-Workout($patientId, $name, $objective) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/treinos" -Headers $headers)
    $existing = $all | Where-Object { $_.nome -eq $name } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $ex1 = $exercises | Select-Object -First 1
    $ex2 = $exercises | Select-Object -Skip 1 -First 1
    $ex3 = $exercises | Select-Object -Skip 2 -First 1
    if ($null -eq $ex1 -or $null -eq $ex2) { return $null }

    $body = @{
        nome = $name
        objetivo = $objective
        dataInicio = "2026-08-01"
        dataFim = "2026-12-31"
        status = "Ativo"
        observacoes = "Ficha demonstrativa para alimentar o modulo de treinos."
        sessoes = @(
            @{
                nome = "Treino A"
                diasSemana = "Segunda, quinta"
                ordem = 1
                observacoes = "Controle tecnico e progressao gradual."
                itens = @(
                    @{ exercicioId = $ex1.id; ordem = 1; series = 4; repeticoes = "8-10"; carga = 40; unidadeCarga = "kg"; descansoSegundos = 90; tempoSegundos = $null; observacoes = "RPE 7-8" },
                    @{ exercicioId = $ex2.id; ordem = 2; series = 3; repeticoes = "10-12"; carga = 24; unidadeCarga = "kg"; descansoSegundos = 75; tempoSegundos = $null; observacoes = "Movimento controlado" }
                )
            },
            @{
                nome = "Treino B"
                diasSemana = "Terca, sexta"
                ordem = 2
                observacoes = "Sessao complementar."
                itens = @(
                    @{ exercicioId = $(if ($null -ne $ex3) { $ex3.id } else { $ex1.id }); ordem = 1; series = 3; repeticoes = "8-12"; carga = 30; unidadeCarga = "kg"; descansoSegundos = 90; tempoSegundos = $null; observacoes = $null }
                )
            }
        )
    }

    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/treinos" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Ensure-Pending($patientId, $origin, $severity, $title, $description, $dueUtc) {
    $existing = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/pendencias" -Headers $headers) |
        Where-Object { $_.origemCodigo -eq $origin -and $_.status -ne "Resolvida" } |
        Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        origemCodigo = $origin
        categoria = "Demo"
        severidade = $severity
        titulo = $title
        descricao = $description
        valorReferencia = $null
        acaoSugerida = "Revisar o prontuario e definir a conduta de acompanhamento."
        vencimentoUtc = $dueUtc
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/pendencias" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Ensure-FollowUp($patientId, $channel, $result, $dateUtc, $nextUtc) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/followups?limite=100" -Headers $headers)
    $existing = $all.itens | Where-Object { $_.resultado -eq $result } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        dataHoraUtc = $dateUtc
        canal = $channel
        resultado = $result
        observacoes = "Follow-up demonstrativo v0.3.27."
        proximoContatoUtc = $nextUtc
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/followups" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " HealthPlatform v0.3.41 - POPULAR RENDER DEMO" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Este script e idempotente: os cenarios existentes sao reaproveitados." -ForegroundColor DarkGray
Write-Host ""

Write-Host "[1/13] Login administrativo..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body (Json @{ email = $email; senha = $senha })
$token = $login.accessToken
if ([string]::IsNullOrWhiteSpace($token)) { throw "Login nao retornou accessToken." }
$headers = @{ Authorization = "Bearer $token" }

Write-Host "[2/13] Garantindo profissional..." -ForegroundColor Cyan
$prof = Invoke-RestMethod -Uri "$base/api/profissionais/me" -Headers $headers -Method Put -ContentType "application/json" -Body (Json @{
    nome = "Dr. Demo HealthPlatform"
    registroProfissional = "CRM-DEMO-001"
    especialidade = "Medicina, Nutricao e Performance"
})

Write-Host "[3/13] Garantindo catalogos..." -ForegroundColor Cyan
$markerSeed = @(
    @{ nome = "Glicemia"; categoria = "Metabolico"; unidadePadrao = "mg/dL" },
    @{ nome = "LDL"; categoria = "Perfil lipidico"; unidadePadrao = "mg/dL" },
    @{ nome = "HDL"; categoria = "Perfil lipidico"; unidadePadrao = "mg/dL" },
    @{ nome = "TSH"; categoria = "Tireoide"; unidadePadrao = "uUI/mL" }
)
$existingMarkers = As-Array (Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers)
$markers = @{}
foreach ($m in $markerSeed) {
    $x = $existingMarkers | Where-Object { $_.nome -eq $m.nome } | Select-Object -First 1
    if ($null -eq $x) {
        $x = Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $m)
    }
    $markers[$m.nome] = $x
}
$foodSeed = @(
    @{ nome="Arroz branco cozido"; categoria="Cereais"; caloriasPor100g=128; proteinasPor100g=2.5; carboidratosPor100g=28.1; gordurasPor100g=0.2; fibrasPor100g=1.6 },
    @{ nome="Feijao carioca cozido"; categoria="Leguminosas"; caloriasPor100g=76; proteinasPor100g=4.8; carboidratosPor100g=13.6; gordurasPor100g=0.5; fibrasPor100g=8.5 },
    @{ nome="Peito de frango grelhado"; categoria="Proteinas"; caloriasPor100g=159; proteinasPor100g=32.0; carboidratosPor100g=0; gordurasPor100g=2.5; fibrasPor100g=0 },
    @{ nome="Banana prata"; categoria="Frutas"; caloriasPor100g=98; proteinasPor100g=1.3; carboidratosPor100g=26.0; gordurasPor100g=0.1; fibrasPor100g=2.0 },
    @{ nome="Ovo inteiro cozido"; categoria="Proteinas"; caloriasPor100g=146; proteinasPor100g=13.3; carboidratosPor100g=0.6; gordurasPor100g=9.5; fibrasPor100g=0 }
)
$existingFoods = As-Array (Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers)
foreach ($food in $foodSeed) {
    if (-not ($existingFoods | Where-Object { $_.nome -eq $food.nome } | Select-Object -First 1)) {
        Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $food) | Out-Null
    }
}

$exerciseSeed = @(
    @{ nome="Agachamento livre"; grupoMuscular="Pernas"; equipamento="Barra"; descricao="Agachamento com barra e tecnica orientada."; videoUrl=$null },
    @{ nome="Supino reto"; grupoMuscular="Peito"; equipamento="Barra ou halteres"; descricao="Pressao horizontal para peitoral."; videoUrl=$null },
    @{ nome="Remada curvada"; grupoMuscular="Costas"; equipamento="Barra"; descricao="Remada para dorsais e musculatura das costas."; videoUrl=$null },
    @{ nome="Desenvolvimento de ombros"; grupoMuscular="Ombros"; equipamento="Halteres"; descricao="Pressao vertical para deltoides."; videoUrl=$null },
    @{ nome="Levantamento terra"; grupoMuscular="Posterior / Costas"; equipamento="Barra"; descricao="Movimento multiarticular de cadeia posterior."; videoUrl=$null },
    @{ nome="Prancha abdominal"; grupoMuscular="Core"; equipamento="Peso corporal"; descricao="Isometria para estabilidade do core."; videoUrl=$null }
)
$existingExercises = As-Array (Invoke-RestMethod -Uri "$base/api/exercicios?incluirInativos=true" -Headers $headers)
foreach ($exercise in $exerciseSeed) {
    if (-not ($existingExercises | Where-Object { $_.nome -eq $exercise.nome } | Select-Object -First 1)) {
        Invoke-RestMethod -Uri "$base/api/exercicios" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $exercise) | Out-Null
    }
}
$exercises = As-Array (Invoke-RestMethod -Uri "$base/api/exercicios" -Headers $headers)

$questions = As-Array (Invoke-RestMethod -Uri "$base/api/anamnese/perguntas?incluirInativas=true" -Headers $headers)
if (-not ($questions | Where-Object { $_.texto -eq "Como voce avalia sua rotina atual de sono?" } | Select-Object -First 1)) {
    Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        texto = "Como voce avalia sua rotina atual de sono?"
        tipoResposta = "Opcao"
        opcoes = @("Boa","Regular","Ruim")
        ordem = 1
    }) | Out-Null
}

$scenarios = @(
    @{
        nome = "Ana Ribeiro"; cpf = "91000000001"; dataNascimento = "1995-03-12"; sexo = "Feminino"; telefone = "41999991001"; email = "ana.ribeiro.demo@healthplatform.local"; profissao = "Arquiteta"
        weights = @(72.0, 69.8, 67.9); fats = @(31.0, 29.2, 27.8); waists = @(88, 84, 81)
        labs1 = @{ Glicemia=94; LDL=126; HDL=48; TSH=2.3 }; labs2 = @{ Glicemia=88; LDL=108; HDL=54; TSH=2.0 }
        goalRecords = 7; diaryRecords = 4; future = "2026-08-14T14:00:00Z"; pending = $false; workout = $true
    },
    @{
        nome = "Bruno Martins"; cpf = "91000000002"; dataNascimento = "1988-11-04"; sexo = "Masculino"; telefone = "41999991002"; email = "bruno.martins.demo@healthplatform.local"; profissao = "Gerente comercial"
        weights = @(91.0, 94.5, 99.0); fats = @(26.0, 28.0, 30.4); waists = @(101, 105, 111)
        labs1 = @{ Glicemia=98; LDL=138; HDL=41; TSH=2.6 }; labs2 = @{ Glicemia=108; LDL=168; HDL=36; TSH=2.8 }
        goalRecords = 1; diaryRecords = 0; future = $null; pending = $true; workout = $true
    },
    @{
        nome = "Carla Souza"; cpf = "91000000003"; dataNascimento = "1991-07-21"; sexo = "Feminino"; telefone = "41999991003"; email = "carla.souza.demo@healthplatform.local"; profissao = "Analista de sistemas"
        weights = @(64.2, 63.8, 63.4); fats = @(24.0, 23.6, 23.2); waists = @(76, 75, 74)
        labs1 = @{ Glicemia=86; LDL=96; HDL=61; TSH=3.2 }; labs2 = @{ Glicemia=84; LDL=92; HDL=64; TSH=5.8 }
        goalRecords = 5; diaryRecords = 3; future = "2026-08-15T16:30:00Z"; pending = $false; workout = $false
    },
    @{
        nome = "Diego Alves"; cpf = "91000000004"; dataNascimento = "1998-01-30"; sexo = "Masculino"; telefone = "41999991004"; email = "diego.alves.demo@healthplatform.local"; profissao = "Professor"
        weights = @(83.0, 81.8, 80.7); fats = @(20.5, 19.2, 18.3); waists = @(91, 88, 86)
        labs1 = @{ Glicemia=90; LDL=112; HDL=50; TSH=1.9 }; labs2 = @{ Glicemia=89; LDL=105; HDL=53; TSH=1.8 }
        goalRecords = 6; diaryRecords = 4; future = "2026-08-18T13:00:00Z"; pending = $false; workout = $true
    },
    @{
        nome = "Elisa Ferreira"; cpf = "91000000005"; dataNascimento = "1982-09-15"; sexo = "Feminino"; telefone = "41999991005"; email = "elisa.ferreira.demo@healthplatform.local"; profissao = "Empresaria"
        weights = @(78.5, 77.9, 77.3); fats = @(34.0, 33.4, 32.9); waists = @(96, 94, 93)
        labs1 = @{ Glicemia=102; LDL=132; HDL=44; TSH=4.0 }; labs2 = @{ Glicemia=104; LDL=142; HDL=42; TSH=4.4 }
        goalRecords = 0; diaryRecords = 0; future = $null; pending = $true; workout = $false
    }
)

Write-Host "[4/13] Criando/reaproveitando 5 pacientes adicionais..." -ForegroundColor Cyan
$patients = @{}
foreach ($d in $scenarios) {
    $patients[$d.nome] = Ensure-Patient $d
}

$portalEmail = "ana.ribeiro.demo@healthplatform.local"
$portalSenha = "PacienteDemo_123!"
$portalPaciente = $patients["Ana Ribeiro"]
if ($null -ne $portalPaciente) {
    $accessStatus = Invoke-RestMethod -Uri "$base/api/pacientes/$($portalPaciente.id)/acesso" -Headers $headers -Method Get
    if (-not $accessStatus.ativado) {
        $invite = Invoke-RestMethod -Uri "$base/api/pacientes/$($portalPaciente.id)/acesso" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{ email = $portalEmail })
        Invoke-RestMethod -Uri "$base/api/auth/paciente/ativar" -Method Post -ContentType "application/json" -Body (Json @{
            email = $portalEmail
            token = $invite.activationToken
            senha = $portalSenha
        }) | Out-Null
    }
}

Write-Host "[5/13] Populando consultas e agenda..." -ForegroundColor Cyan
foreach ($d in $scenarios) {
    $p = $patients[$d.nome]
    Ensure-Consultation $p.id "Demo v0.3.6 - consulta inicial" "2026-06-01T15:00:00Z" "Realizada" "Consulta inicial do cenario demonstrativo." | Out-Null
    if ($null -ne $d.future) {
        Ensure-Consultation $p.id "Demo v0.3.6 - retorno futuro" $d.future "Agendada" "Retorno programado para demonstracao da agenda/notificacoes." | Out-Null
    }
}

Write-Host "[6/13] Populando evolucao corporal (3 avaliacoes por paciente)..." -ForegroundColor Cyan
foreach ($d in $scenarios) {
    $p = $patients[$d.nome]
    Ensure-Evaluation $p.id "2026-05-10T12:00:00Z" $d.weights[0] 1.70 $d.fats[0] $d.waists[0] 122 78 72 | Out-Null
    Ensure-Evaluation $p.id "2026-06-20T12:00:00Z" $d.weights[1] 1.70 $d.fats[1] $d.waists[1] 120 78 70 | Out-Null
    Ensure-Evaluation $p.id "2026-08-10T12:00:00Z" $d.weights[2] 1.70 $d.fats[2] $d.waists[2] 124 80 73 | Out-Null
}

Write-Host "[7/13] Populando historico laboratorial (2 coletas por paciente)..." -ForegroundColor Cyan
foreach ($d in $scenarios) {
    $p = $patients[$d.nome]
    $v1 = @(
        @{nome="Glicemia";valor=$d.labs1.Glicemia;min=70;max=99;refTexto=$null},
        @{nome="LDL";valor=$d.labs1.LDL;min=$null;max=129;refTexto="Meta pode variar conforme risco individual."},
        @{nome="HDL";valor=$d.labs1.HDL;min=40;max=$null;refTexto=$null},
        @{nome="TSH";valor=$d.labs1.TSH;min=0.4;max=4.0;refTexto=$null}
    )
    $v2 = @(
        @{nome="Glicemia";valor=$d.labs2.Glicemia;min=70;max=99;refTexto=$null},
        @{nome="LDL";valor=$d.labs2.LDL;min=$null;max=129;refTexto="Meta pode variar conforme risco individual."},
        @{nome="HDL";valor=$d.labs2.HDL;min=40;max=$null;refTexto=$null},
        @{nome="TSH";valor=$d.labs2.TSH;min=0.4;max=4.0;refTexto=$null}
    )
    Ensure-Lab $p.id "Demo v0.3.6 - coleta 1 - $($d.nome)" "2026-06-15T10:00:00Z" $v1 | Out-Null
    Ensure-Lab $p.id "Demo v0.3.6 - coleta 2 - $($d.nome)" "2026-08-08T10:00:00Z" $v2 | Out-Null
}

Write-Host "[8/13] Populando metas e adesao..." -ForegroundColor Cyan
foreach ($d in $scenarios) {
    $p = $patients[$d.nome]
    $records = @()
    for ($i=0; $i -lt $d.goalRecords; $i++) {
        $day = 7 + $i
        $records += @{
            data = ("2026-08-{0:D2}" -f $day)
            valor = 2.0 + (($i % 3) * 0.35)
            concluida = $null
        }
    }
    Ensure-Goal $p.id "Agua diaria demo v0.3.6" 2.5 "L" "Diaria" $records
}

Write-Host "[9/13] Populando diario do paciente..." -ForegroundColor Cyan
foreach ($d in $scenarios) {
    $p = $patients[$d.nome]
    for ($i=0; $i -lt $d.diaryRecords; $i++) {
        $day = 10 + $i
        Ensure-Diary $p.id ("2026-08-{0:D2}T18:00:00Z" -f $day) "Bem-estar" ("Check-in demo v0.3.6 dia {0:D2}" -f $day) $null $null (7 + ($i % 3))
    }
}

Write-Host "[10/13] Populando planos de treino..." -ForegroundColor Cyan
foreach ($d in $scenarios | Where-Object { $_.workout }) {
    $p = $patients[$d.nome]
    Ensure-Workout $p.id "Plano performance demo v0.3.6" "Melhorar condicionamento, forca e consistencia." | Out-Null
}

Write-Host "[11/13] Criando pendencias demonstrativas..." -ForegroundColor Cyan
foreach ($d in $scenarios | Where-Object { $_.pending }) {
    $p = $patients[$d.nome]
    if ($d.nome -eq "Bruno Martins") {
        Ensure-Pending $p.id "DEMO_BRUNO_LDL" "Alta" "Revisar perfil lipidico" "LDL do cenario demo ficou acima da faixa registrada." "2026-08-12T18:00:00Z" | Out-Null
    } else {
        Ensure-Pending $p.id "DEMO_ELISA_RETORNO" "Media" "Contato para retorno" "Paciente demonstrativa sem retorno futuro registrado." "2026-08-14T18:00:00Z" | Out-Null
    }
}

Write-Host "[12/13] Populando follow-ups demonstrativos..." -ForegroundColor Cyan
Ensure-FollowUp $patients["Ana Ribeiro"].id "WhatsApp" "Confirmou que esta seguindo o plano" "2026-08-12T17:00:00Z" "2026-08-20T17:00:00Z" | Out-Null
Ensure-FollowUp $patients["Bruno Martins"].id "Telefone" "Nao atendeu na primeira tentativa" "2026-08-10T14:00:00Z" "2026-08-13T14:00:00Z" | Out-Null
Ensure-FollowUp $patients["Carla Souza"].id "WhatsApp" "Orientada a levar exames no retorno" "2026-08-13T16:00:00Z" "2026-08-14T16:00:00Z" | Out-Null
Ensure-FollowUp $patients["Diego Alves"].id "Email" "Enviado reforco das orientacoes" "2026-08-11T11:00:00Z" "2026-08-18T11:00:00Z" | Out-Null
Ensure-FollowUp $patients["Elisa Ferreira"].id "Telefone" "Solicitado contato para reagendamento" "2026-08-09T15:00:00Z" "2026-08-12T15:00:00Z" | Out-Null

Write-Host "[13/13] Sincronizando notificacoes e exibindo resumo..." -ForegroundColor Cyan
Invoke-RestMethod -Uri "$base/api/notificacoes/sincronizar" -Headers $headers -Method Post | Out-Null
$allPatients = Invoke-RestMethod -Uri "$base/api/pacientes?tamanhoPagina=100&incluirInativos=true" -Headers $headers
$insights = Invoke-RestMethod -Uri "$base/api/insights/dashboard?limite=50" -Headers $headers
$pend = Invoke-RestMethod -Uri "$base/api/pendencias?status=abertas&limite=100" -Headers $headers
$notif = Invoke-RestMethod -Uri "$base/api/notificacoes?sincronizar=true&limite=100" -Headers $headers

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " BANCO DEMO POPULADO COM SUCESSO" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ("Pacientes totais:              {0}" -f $allPatients.total)
Write-Host ("Pacientes com insights:        {0}" -f $insights.pacientesComInsights)
Write-Host ("Insights calculados:           {0}" -f $insights.totalInsights)
Write-Host ("Pendencias abertas:            {0}" -f $pend.total)
Write-Host ("Notificacoes ativas:           {0}" -f $notif.total)
Write-Host ("Notificacoes nao lidas:        {0}" -f $notif.naoLidas)
Write-Host ""
Write-Host "Cenarios adicionados:" -ForegroundColor Cyan
Write-Host "  Ana Ribeiro    -> boa evolucao + adesao + retorno futuro"
Write-Host "  Bruno Martins  -> ganho de peso + exames alterados + pendencia vencida"
Write-Host "  Carla Souza    -> TSH fora da referencia + acompanhamento ativo"
Write-Host "  Diego Alves    -> boa evolucao + plano de treino ativo"
Write-Host "  Elisa Ferreira -> baixa adesao + sem retorno + pendencia"
Write-Host ""
Write-Host "Pode rodar POPULAR.ps1 novamente: os registros principais sao reaproveitados." -ForegroundColor DarkGreen


Write-Host ""
Write-Host "ACESSOS DA DEMO REMOTA" -ForegroundColor Cyan
Write-Host "Profissional: $email" -ForegroundColor Green
Write-Host "Paciente:     ana.ribeiro.demo@healthplatform.local" -ForegroundColor Green
Write-Host "Senha paciente: PacienteDemo_123!" -ForegroundColor Green
Write-Host "Use somente dados ficticios neste ambiente." -ForegroundColor Yellow
# =====================================================================
# CAMADA RICA V2
# A partir daqui, o script apenas enriquece os 5 pacientes criados
# pelo POPULAR-REMOTO acima. Cada bloco opcional e tolerante a falhas.
# =====================================================================

Write-Host ""
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host " CAMADA RICA V2 - PRONTUARIOS DENSOS" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "Base remota concluida. Agora vamos alimentar as telas avancadas." -ForegroundColor DarkGray
Write-Host ""

$script:RichWarnings = @()

function Run-RichStep([string]$label, [scriptblock]$action) {
    Write-Host $label -ForegroundColor Cyan
    try {
        & $action
        Write-Host "    OK" -ForegroundColor DarkGreen
    }
    catch {
        $msg = $_.Exception.Message
        $script:RichWarnings += "$label -> $msg"
        Write-Host "    AVISO: $msg" -ForegroundColor Yellow
        Write-Host "    O enriquecimento continua nos proximos modulos." -ForegroundColor DarkYellow
    }
}

function Rich-GetPatient([string]$name) {
    return Get-PatientByName $name
}

function Rich-EnsureMarker($m) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers)
    $item = $all | Where-Object { $_.nome -eq $m.nome } | Select-Object -First 1
    if ($null -eq $item) {
        $item = Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $m)
    }
    return $item
}

function Rich-EnsureFood($f) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers)
    $item = $all | Where-Object { $_.nome -eq $f.nome } | Select-Object -First 1
    if ($null -eq $item) {
        $item = Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $f)
    }
    return $item
}

function Rich-EnsureAnamnese($patientId, [string]$tag, [string]$dateUtc, $profile, [int]$offset) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/anamneses" -Headers $headers)
    $existing = $all | Where-Object { $_.observacoes -eq $tag } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $sleep = [math]::Max(4.0, [decimal]$profile.sonoHoras + $offset * 0.25)
    $stress = [math]::Min(10, [math]::Max(0, [int]$profile.estresse - $offset))
    $water = [math]::Max(0.8, [decimal]$profile.agua + $offset * 0.15)
    $days = [math]::Max(0, [int]$profile.atividadeDias + $offset)

    $body = @{
        consultaId = $null
        dataUtc = $dateUtc
        objetivoAcompanhamento = $profile.objetivo
        historicoDoencas = $profile.doencas
        historicoFamiliar = $profile.familiar
        cirurgias = $profile.cirurgias
        alergias = $profile.alergias
        medicamentos = $profile.medicamentos
        suplementos = $profile.suplementos
        tabagismo = $profile.tabagismo
        etilismo = $profile.etilismo
        sonoHorasMedia = $sleep
        sonoQualidade = $(if ($sleep -ge 7) { "Boa" } elseif ($sleep -ge 6) { "Regular" } else { "Ruim" })
        despertaDuranteNoite = $(if ($sleep -lt 6.5) { $true } else { $false })
        estresseNivel = $stress
        atividadeFisica = $profile.atividade
        atividadeFisicaDiasSemana = $days
        habitoIntestinal = $profile.intestinal
        aguaLitrosDia = $water
        observacoes = $tag
        respostasPersonalizadas = @()
    }

    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/anamneses" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Rich-EnsureSoap($patientId, [string]$tag, [string]$dateUtc, [string]$subjective, [string]$assessment) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/evolucoes" -Headers $headers)
    $existing = $all | Where-Object { $_.observacoes -eq $tag } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        consultaId = $null
        dataHoraUtc = $dateUtc
        subjetivo = $subjective
        objetivo = "Peso, medidas, exames, adesao e rotina foram revisados em conjunto."
        avaliacao = $assessment
        plano = "Manter acompanhamento longitudinal, revisar adesao e reavaliar criterios da fase no proximo retorno."
        observacoes = $tag
    }

    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/evolucoes" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Rich-FoodItem([string]$name, [decimal]$grams, [string]$note) {
    $food = $script:RichFoods[$name]
    if ($null -eq $food) { throw "Alimento ausente no mapa rico: $name" }
    return @{
        alimentoId = $food.id
        quantidade = $grams
        unidade = "g"
        quantidadeGramas = $grams
        observacao = $note
        substituicoes = @()
    }
}

function Rich-EnsureNutritionPlan($patientId, [string]$patientName, $profile) {
    $planName = "Plano completo demo rica - $patientName"
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/planos-alimentares" -Headers $headers)
    $existing = $all | Where-Object { $_.nome -eq $planName } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        nome = $planName
        dataInicio = "2026-07-01"
        dataFim = "2026-12-31"
        status = "Ativo"
        observacoes = $profile.nutritionNote
        metaCalorias = $profile.kcal
        metaProteinasG = $profile.protein
        metaCarboidratosG = $profile.carb
        metaGordurasG = $profile.fat
        metaFibrasG = $profile.fiber
        refeicoes = @(
            @{
                nome = "Cafe da manha"; horario = "07:30:00"; ordem = 1; observacoes = "Refeicao simples e repetivel."
                metaCalorias = [math]::Round($profile.kcal * 0.22); metaProteinasG = [math]::Round($profile.protein * 0.22)
                metaCarboidratosG = [math]::Round($profile.carb * 0.22); metaGordurasG = [math]::Round($profile.fat * 0.22); metaFibrasG = [math]::Round($profile.fiber * 0.22)
                itens = @(
                    (Rich-FoodItem "Ovo inteiro cozido" 120 "Aproximadamente 2 ovos"),
                    (Rich-FoodItem "Pao integral" 60 "Aproximadamente 2 fatias"),
                    (Rich-FoodItem "Banana prata" 100 "1 unidade media")
                )
            },
            @{
                nome = "Almoco"; horario = "12:30:00"; ordem = 2; observacoes = "Base de comida brasileira."
                metaCalorias = [math]::Round($profile.kcal * 0.32); metaProteinasG = [math]::Round($profile.protein * 0.32)
                metaCarboidratosG = [math]::Round($profile.carb * 0.32); metaGordurasG = [math]::Round($profile.fat * 0.32); metaFibrasG = [math]::Round($profile.fiber * 0.32)
                itens = @(
                    (Rich-FoodItem "Arroz branco cozido" 140 "Peso cozido"),
                    (Rich-FoodItem "Feijao carioca cozido" 120 "1 concha media"),
                    (Rich-FoodItem "Peito de frango grelhado" 160 "Peso pronto"),
                    (Rich-FoodItem "Brocolis cozido" 100 "Vegetais")
                )
            },
            @{
                nome = "Lanche"; horario = "16:30:00"; ordem = 3; observacoes = "Lanche de boa saciedade."
                metaCalorias = [math]::Round($profile.kcal * 0.16); metaProteinasG = [math]::Round($profile.protein * 0.16)
                metaCarboidratosG = [math]::Round($profile.carb * 0.16); metaGordurasG = [math]::Round($profile.fat * 0.16); metaFibrasG = [math]::Round($profile.fiber * 0.16)
                itens = @(
                    (Rich-FoodItem "Iogurte natural" 170 "1 pote"),
                    (Rich-FoodItem "Aveia em flocos" 35 "Misturar ao iogurte"),
                    (Rich-FoodItem "Morango" 120 "Fruta")
                )
            },
            @{
                nome = "Jantar"; horario = "20:00:00"; ordem = 4; observacoes = "Refeicao completa."
                metaCalorias = [math]::Round($profile.kcal * 0.24); metaProteinasG = [math]::Round($profile.protein * 0.24)
                metaCarboidratosG = [math]::Round($profile.carb * 0.24); metaGordurasG = [math]::Round($profile.fat * 0.24); metaFibrasG = [math]::Round($profile.fiber * 0.24)
                itens = @(
                    (Rich-FoodItem "Batata inglesa cozida" 220 "Peso cozido"),
                    (Rich-FoodItem "Patinho moido" 160 "Preparacao magra"),
                    (Rich-FoodItem "Salada variada" 150 "Folhas e legumes")
                )
            },
            @{
                nome = "Ceia"; horario = "22:30:00"; ordem = 5; observacoes = "Opcional conforme fome."
                metaCalorias = [math]::Round($profile.kcal * 0.06); metaProteinasG = [math]::Round($profile.protein * 0.06)
                metaCarboidratosG = [math]::Round($profile.carb * 0.06); metaGordurasG = [math]::Round($profile.fat * 0.06); metaFibrasG = [math]::Round($profile.fiber * 0.06)
                itens = @(
                    (Rich-FoodItem "Leite semidesnatado" 200 "1 copo")
                )
            }
        )
    }

    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/planos-alimentares" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body 20)
}

function Rich-EnsureNutritionPhase($patientId, $planId, [string]$name, [string]$type, [string]$objective, [string]$start, [string]$end, [string]$status, [decimal]$weightTarget, [int]$adherence, [string]$transition) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers)
    $phase = $all | Where-Object { $_.nome -eq $name } | Select-Object -First 1

    $createBody = @{
        nome = $name; tipo = $type; objetivo = $objective; dataInicio = $start; dataFim = $end
        planoAlimentarId = $planId; metaPesoKg = $weightTarget; metaAdesaoPercentual = $adherence
        duracaoMinimaDias = 21; criterioTransicao = $transition; observacoes = "Fase demonstrativa rica."
    }

    if ($null -eq $phase) {
        $phase = Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $createBody)
    }

    if ($phase.status -ne $status) {
        $updateBody = @{
            nome = $name; tipo = $type; objetivo = $objective; dataInicio = $start; dataFim = $end
            planoAlimentarId = $planId; status = $status; metaPesoKg = $weightTarget
            metaAdesaoPercentual = $adherence; duracaoMinimaDias = 21
            criterioTransicao = $transition; observacoes = "Fase demonstrativa rica."
        }
        Invoke-RestMethod -Uri "$base/api/fases-nutricionais/$($phase.id)" -Headers $headers -Method Put -ContentType "application/json" -Body (Json $updateBody) | Out-Null
    }

    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-nutricionais" -Headers $headers)
    return ($all | Where-Object { $_.nome -eq $name } | Select-Object -First 1)
}

function Rich-EnsureWorkout($patientId, [string]$patientName, $profile) {
    $name = "Treino completo demo rica - $patientName"
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/treinos" -Headers $headers)
    $existing = $all | Where-Object { $_.nome -eq $name } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $required = @("Agachamento livre","Supino reto","Remada curvada","Desenvolvimento de ombros","Levantamento terra","Prancha abdominal")
    foreach ($n in $required) {
        if ($null -eq $script:RichExercises[$n]) { throw "Exercicio ausente no mapa rico: $n" }
    }

    $sessions = @(
        @{
            nome = "Treino A - Pernas e peito"; diasSemana = "Segunda, quinta"; ordem = 1; observacoes = "Base de forca e tecnica."
            itens = @(
                @{ exercicioId=$script:RichExercises["Agachamento livre"].id; ordem=1; series=4; repeticoes="6-8"; carga=70; unidadeCarga="kg"; descansoSegundos=120; tempoSegundos=$null; observacoes="Tecnica antes de carga" },
                @{ exercicioId=$script:RichExercises["Supino reto"].id; ordem=2; series=4; repeticoes="8-10"; carga=50; unidadeCarga="kg"; descansoSegundos=90; tempoSegundos=$null; observacoes="RPE alvo 7-8" }
            )
        },
        @{
            nome = "Treino B - Costas e ombros"; diasSemana = "Terca, sexta"; ordem = 2; observacoes = "Controle de volume."
            itens = @(
                @{ exercicioId=$script:RichExercises["Remada curvada"].id; ordem=1; series=4; repeticoes="8-10"; carga=45; unidadeCarga="kg"; descansoSegundos=90; tempoSegundos=$null; observacoes=$null },
                @{ exercicioId=$script:RichExercises["Desenvolvimento de ombros"].id; ordem=2; series=3; repeticoes="10-12"; carga=22; unidadeCarga="kg"; descansoSegundos=75; tempoSegundos=$null; observacoes=$null }
            )
        },
        @{
            nome = "Treino C - Posterior e core"; diasSemana = "Quarta, sabado"; ordem = 3; observacoes = "Posterior de cadeia e estabilidade."
            itens = @(
                @{ exercicioId=$script:RichExercises["Levantamento terra"].id; ordem=1; series=3; repeticoes="5-6"; carga=90; unidadeCarga="kg"; descansoSegundos=150; tempoSegundos=$null; observacoes="Sem perder tecnica" },
                @{ exercicioId=$script:RichExercises["Prancha abdominal"].id; ordem=2; series=3; repeticoes="40 s"; carga=10; unidadeCarga="kg"; descansoSegundos=60; tempoSegundos=40; observacoes="Prancha com carga demonstrativa" }
            )
        }
    )

    $body = @{
        nome = $name
        objetivo = $profile.trainingObjective
        dataInicio = "2026-08-01"
        dataFim = "2026-12-31"
        status = "Ativo"
        observacoes = "Plano rico para volume, progressao, RPE e analise por grupo muscular."
        sessoes = $sessions
    }

    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/treinos" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body 20)
}

function Rich-EnsureTrainingPhase($patientId, $planId, [string]$name, [string]$status, $profile) {
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-treino" -Headers $headers)
    $phase = $all | Where-Object { $_.nome -eq $name } | Select-Object -First 1

    $start = $(if ($name -match "Base") { "2026-06-01" } else { "2026-07-01" })
    $end = $(if ($name -match "Base") { "2026-06-30" } else { "2026-10-31" })
    $type = $(if ($name -match "Base") { "Adaptacao" } else { $profile.trainingType })

    $createBody = @{
        nome=$name; tipo=$type; objetivo=$profile.trainingObjective; dataInicio=$start; dataFim=$end
        planoTreinoId=$planId; metaPesoKg=$profile.targetWeight; metaAdesaoPercentual=80
        duracaoMinimaDias=21; criterioTransicao="Revisar tecnica, adesao, recuperacao e tendencia de carga."; observacoes="Fase de treino demo rica."
    }

    if ($null -eq $phase) {
        $phase = Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-treino" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $createBody)
    }

    if ($phase.status -ne $status) {
        $updateBody = @{
            nome=$name; tipo=$type; objetivo=$profile.trainingObjective; dataInicio=$start; dataFim=$end
            planoTreinoId=$planId; status=$status; metaPesoKg=$profile.targetWeight; metaAdesaoPercentual=80
            duracaoMinimaDias=21; criterioTransicao="Revisar tecnica, adesao, recuperacao e tendencia de carga."; observacoes="Fase de treino demo rica."
        }
        Invoke-RestMethod -Uri "$base/api/fases-treino/$($phase.id)" -Headers $headers -Method Put -ContentType "application/json" -Body (Json $updateBody) | Out-Null
    }

    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/fases-treino" -Headers $headers)
    return ($all | Where-Object { $_.nome -eq $name } | Select-Object -First 1)
}

function Rich-EnsureCheckIn($patientId, [string]$dateUtc, [decimal]$weight, [int]$foodAdherence, [int]$trainingAdherence, [int]$hunger, [int]$energy, [int]$sleep, [int]$perception, $nutritionPhase, $trainingPhase, [string]$note) {
    $history = Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/check-ins?limite=100" -Headers $headers
    $target = Date-Key $dateUtc
    $existing = As-Array $history.itens | Where-Object { (Date-Key $_.dataUtc) -eq $target } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        dataUtc = $dateUtc
        pesoKg = $weight
        adesaoAlimentacaoPercentual = $foodAdherence
        adesaoTreinoPercentual = $trainingAdherence
        fomeNivel = $hunger
        energiaNivel = $energy
        sonoNivel = $sleep
        percepcaoEvolucaoNivel = $perception
        faseNutricionalId = $(if ($null -ne $nutritionPhase) { $nutritionPhase.id } else { $null })
        faseTreinoId = $(if ($null -ne $trainingPhase) { $trainingPhase.id } else { $null })
        observacoes = $note
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/check-ins" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Rich-EnsureReport($patientId, [string]$patientName) {
    $title = "Resumo longitudinal demo rica - $patientName"
    $all = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/relatorios" -Headers $headers)
    $existing = $all | Where-Object { $_.titulo -eq $title } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $body = @{
        dataInicioUtc = "2026-05-01T00:00:00Z"
        dataFimUtc = "2026-08-15T23:59:59Z"
        titulo = $title
        conclusaoMedica = "Relatorio ficticio para demonstrar o resumo longitudinal do prontuario e a evolucao dos principais indicadores."
    }
    return Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/relatorios" -Headers $headers -Method Post -ContentType "application/json" -Body (Json $body)
}

function Rich-EnsurePhaseReview($patientId, [string]$domain, $phase) {
    if ($null -eq $phase -or $phase.status -ne "EmAndamento") { return }

    $domainQuery = $(if ($domain -eq "Nutricao") { "Nutricao" } else { "Treino" })
    $all = Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/revisoes-fases?dominio=$domainQuery&limite=50" -Headers $headers
    $existing = As-Array $all.itens | Where-Object { $_.faseId -eq $phase.id -and $_.decisao -eq "Manter" } | Select-Object -First 1
    if ($null -ne $existing) { return $existing }

    $route = $(if ($domain -eq "Nutricao") { "fases-nutricionais" } else { "fases-treino" })
    return Invoke-RestMethod -Uri "$base/api/$route/$($phase.id)/revisar" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
        decisao = "Manter"
        justificativa = "Revisao demo: manter a fase para observar mais dados de adesao, sintomas e tendencia antes de nova transicao."
        confirmarMesmoSemCriterios = $false
    })
}

function Rich-ResetPatientAccess($patientId, [string]$patientEmail, [string]$password) {
    $invite = Invoke-RestMethod -Uri "$base/api/pacientes/$patientId/acesso" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{ email = $patientEmail })
    Invoke-RestMethod -Uri "$base/api/auth/paciente/ativar" -Method Post -ContentType "application/json" -Body (Json @{
        email = $patientEmail
        token = $invite.activationToken
        senha = $password
    }) | Out-Null

    return Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body (Json @{
        email = $patientEmail
        senha = $password
    })
}

function Rich-CreateWorkoutExecutions($patientId, [string]$patientEmail, [string]$password, [string]$pattern) {
    $loginPatient = Rich-ResetPatientAccess $patientId $patientEmail $password
    $patientHeaders = @{ Authorization = "Bearer $($loginPatient.accessToken)" }

    $current = Invoke-RestMethod -Uri "$base/api/portal/me/treino" -Headers $patientHeaders
    if ($null -eq $current.plano) { throw "Paciente $patientEmail nao possui plano ativo no portal." }

    $history = Invoke-RestMethod -Uri "$base/api/portal/me/treinos/historico?dias=365" -Headers $patientHeaders
    $sessions = As-Array $current.plano.sessoes
    if ($sessions.Count -lt 1) { throw "Plano ativo sem sessoes." }

    for ($s = 0; $s -lt $sessions.Count; $s++) {
        $session = $sessions[$s]

        for ($r = 0; $r -lt 5; $r++) {
            $day = 1 + $s + ($r * 3)
            if ($day -gt 15) { continue }

            $dateUtc = ("2026-08-{0:D2}T18:00:00Z" -f $day)
            $targetDate = Date-Key $dateUtc
            $exists = As-Array $history.execucoes | Where-Object {
                (Date-Key $_.dataHoraInicioUtc) -eq $targetDate -and $_.sessao -eq $session.nome
            } | Select-Object -First 1

            if ($null -ne $exists) { continue }

            $items = @()
            $itemIndex = 0

            foreach ($item in (As-Array $session.itens)) {
                $baseLoad = 0
                if ($null -ne $item.carga) { $baseLoad = [decimal]$item.carga }

                $delta = 0
                $rpe = 7

                if ($pattern -eq "progress") {
                    $delta = [decimal]($r * 2 + $itemIndex)
                    $rpe = 7 + ($r % 2)
                }
                elseif ($pattern -eq "fatigue") {
                    $seq = @(0, 1, 0, -1, -2)
                    $delta = [decimal]$seq[$r]
                    $rpe = @(7, 8, 8, 9, 9)[$r]
                }
                else {
                    $seq = @(0, 0.2, 0, 0, 0)
                    $delta = [decimal]$seq[$r]
                    $rpe = @(7, 7, 7, 7, 7)[$r]
                }

                $load = [math]::Max(0, [math]::Round($baseLoad + $delta, 1))

                $items += @{
                    itemTreinoId = $item.id
                    seriesRealizadas = $item.series
                    repeticoesRealizadas = $(if ($r -lt 2) { "8" } elseif ($r -lt 4) { "9" } else { "10" })
                    cargaRealizada = $load
                    unidadeCarga = $(if ([string]::IsNullOrWhiteSpace([string]$item.unidadeCarga)) { "kg" } else { $item.unidadeCarga })
                    esforcoPercebido = $rpe
                    concluido = $true
                    observacoes = "Historico ficticio da demo rica."
                }

                $itemIndex++
            }

            $end = ([datetime]$dateUtc).AddMinutes(58).ToString("o")
            Invoke-RestMethod -Uri "$base/api/portal/me/treinos/execucoes" -Headers $patientHeaders -Method Post -ContentType "application/json" -Body (Json @{
                sessaoTreinoId = $session.id
                dataHoraInicioUtc = $dateUtc
                dataHoraFimUtc = $end
                duracaoMinutos = 58
                esforcoPercebido = $(if ($pattern -eq "fatigue" -and $r -ge 2) { 9 } else { 7 })
                observacoes = "Treino concluido para alimentar volume, progressao e sinais."
                itens = $items
            } 20) | Out-Null
        }
    }
}

# Perfis ricos - poucos pacientes, cada um com uma historia diferente.
$script:RichProfiles = @{
    "Ana Ribeiro" = @{
        objetivo="Reducao de gordura com melhora de energia e rotina."
        doencas="Sem doencas cronicas conhecidas."
        familiar="Mae com hipertensao; pai com dislipidemia."
        cirurgias="Apendicectomia na adolescencia."
        alergias="Nega alergias conhecidas."
        medicamentos="Nenhum uso continuo."
        suplementos="Creatina 3 g/dia."
        tabagismo="Nao"; etilismo="Social"; sonoHoras=7.2; estresse=5
        atividade="Musculacao e caminhada"; atividadeDias=4; intestinal="Regular"; agua=2.4
        startWeight=72.0; currentWeight=67.4; targetWeight=66.5
        kcal=1850; protein=135; carb=190; fat=58; fiber=28
        nutritionNote="Deficit moderado, alta proteina e rotina simples."
        trainingObjective="Hipertrofia com boa tecnica e progressao sustentavel."
        trainingType="Hipertrofia"
        lab=@{Glicemia=86;HbA1c=5.2;LDL=102;HDL=57;TG=92;TSH=1.9;Ferritina=62;VitD=38}
        pattern="plateau"
    }
    "Bruno Martins" = @{
        objetivo="Reduzir risco cardiometabolico e recuperar adesao."
        doencas="Pressao limítrofe em acompanhamento."
        familiar="Pai com diabetes tipo 2; irmao com obesidade."
        cirurgias="Nega cirurgias relevantes."
        alergias="Nega alergias."
        medicamentos="Uso irregular de anti-hipertensivo relatado."
        suplementos="Nenhum."
        tabagismo="Ex-tabagista"; etilismo="Finais de semana"; sonoHoras=5.8; estresse=8
        atividade="Caminhadas e musculacao irregular"; atividadeDias=2; intestinal="Regular"; agua=1.5
        startWeight=91.0; currentWeight=99.2; targetWeight=92.0
        kcal=2250; protein=160; carb=230; fat=72; fiber=30
        nutritionNote="Alta saciedade, praticidade e controle de ambiente."
        trainingObjective="Retomar consistencia com intensidade controlada."
        trainingType="Recondicionamento"
        lab=@{Glicemia=112;HbA1c=5.9;LDL=171;HDL=35;TG=188;TSH=2.9;Ferritina=155;VitD=24}
        pattern="fatigue"
    }
    "Carla Souza" = @{
        objetivo="Melhora de composicao corporal e investigacao de cansaco."
        doencas="Sem diagnosticos cronicos."
        familiar="Mae com hipotireoidismo."
        cirurgias="Cesarea ha 6 anos."
        alergias="Intolerancia leve a lactose relatada."
        medicamentos="Nega uso continuo."
        suplementos="Vitamina D em uso irregular."
        tabagismo="Nao"; etilismo="Raro"; sonoHoras=6.6; estresse=6
        atividade="Pilates e corrida leve"; atividadeDias=3; intestinal="Regular"; agua=2.0
        startWeight=64.2; currentWeight=63.3; targetWeight=62.5
        kcal=1950; protein=120; carb=220; fat=62; fiber=27
        nutritionNote="Manutencao ativa com carboidratos bem distribuidos."
        trainingObjective="Manter condicionamento geral."
        trainingType="Misto"
        lab=@{Glicemia=83;HbA1c=5.1;LDL=91;HDL=65;TG=78;TSH=5.6;Ferritina=38;VitD=31}
        pattern="plateau"
    }
    "Diego Alves" = @{
        objetivo="Performance, forca e recomposicao corporal."
        doencas="Nega doencas cronicas."
        familiar="Sem antecedentes relevantes."
        cirurgias="Reconstrucao de LCA ha 7 anos, sem limitacao atual."
        alergias="Nega alergias."
        medicamentos="Nenhum."
        suplementos="Creatina 5 g/dia e whey conforme necessidade."
        tabagismo="Nao"; etilismo="Social"; sonoHoras=7.8; estresse=4
        atividade="Musculacao estruturada"; atividadeDias=5; intestinal="Regular"; agua=3.2
        startWeight=83.0; currentWeight=80.4; targetWeight=80.0
        kcal=2550; protein=175; carb=305; fat=70; fiber=32
        nutritionNote="Performance com carboidratos ao redor do treino."
        trainingObjective="Progredir movimentos base mantendo volume de qualidade."
        trainingType="Forca"
        lab=@{Glicemia=88;HbA1c=5.0;LDL=101;HDL=55;TG=74;TSH=1.7;Ferritina=110;VitD=42}
        pattern="progress"
    }
    "Elisa Ferreira" = @{
        objetivo="Retomar sono, rotina alimentar e acompanhamento."
        doencas="Esteatose hepatica previamente relatada."
        familiar="Mae com diabetes e hipertensao."
        cirurgias="Colecistectomia."
        alergias="Nega alergias."
        medicamentos="Metformina relatada em uso irregular."
        suplementos="Nenhum."
        tabagismo="Nao"; etilismo="2-3 vezes por semana"; sonoHoras=5.5; estresse=8
        atividade="Sedentaria"; atividadeDias=0; intestinal="Irregular"; agua=1.2
        startWeight=78.5; currentWeight=77.5; targetWeight=74.0
        kcal=1750; protein=115; carb=175; fat=62; fiber=30
        nutritionNote="Plano simples, previsivel e com foco em saciedade."
        trainingObjective="Introduzir movimento com baixa barreira."
        trainingType="Adaptacao"
        lab=@{Glicemia=106;HbA1c=5.8;LDL=145;HDL=41;TG=166;TSH=4.5;Ferritina=92;VitD=22}
        pattern="plateau"
    }
}

# Estado compartilhado entre etapas.
$script:RichFoods = @{}
$script:RichMarkers = @{}
$script:RichExercises = @{}
$script:RichNutritionPlans = @{}
$script:RichNutritionBase = @{}
$script:RichNutritionCurrent = @{}
$script:RichWorkoutPlans = @{}
$script:RichTrainingBase = @{}
$script:RichTrainingCurrent = @{}

Run-RichStep "[R1/10] Ampliando catalogos de alimentos e exames..." {
    $markerSeed = @(
        @{nome="Hemoglobina glicada";categoria="Metabolico";unidadePadrao="%"},
        @{nome="Triglicerideos";categoria="Perfil lipidico";unidadePadrao="mg/dL"},
        @{nome="Ferritina";categoria="Micronutrientes";unidadePadrao="ng/mL"},
        @{nome="Vitamina D";categoria="Micronutrientes";unidadePadrao="ng/mL"}
    )
    foreach ($m in $markerSeed) { $script:RichMarkers[$m.nome] = Rich-EnsureMarker $m }

    # Reaproveita os quatro marcadores que o POPULAR-REMOTO ja garantiu.
    foreach ($n in @("Glicemia","LDL","HDL","TSH")) {
        $all = As-Array (Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers)
        $script:RichMarkers[$n] = $all | Where-Object { $_.nome -eq $n } | Select-Object -First 1
    }

    $foodSeed = @(
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
    foreach ($f in $foodSeed) { $script:RichFoods[$f.nome] = Rich-EnsureFood $f }

    foreach ($n in @("Arroz branco cozido","Feijao carioca cozido","Peito de frango grelhado","Banana prata","Ovo inteiro cozido")) {
        $all = As-Array (Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers)
        $script:RichFoods[$n] = $all | Where-Object { $_.nome -eq $n } | Select-Object -First 1
    }

    $allExercises = As-Array (Invoke-RestMethod -Uri "$base/api/exercicios?incluirInativos=true" -Headers $headers)
    foreach ($e in $allExercises) { $script:RichExercises[$e.nome] = $e }

    Write-Host ("    Marcadores ricos: {0} | alimentos no mapa: {1} | exercicios: {2}" -f $script:RichMarkers.Count,$script:RichFoods.Count,$script:RichExercises.Count)
}

Run-RichStep "[R2/10] Enriquecendo consultas, SOAP, metas e diario..." {
    foreach ($name in $script:RichProfiles.Keys) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }
        $profile = $script:RichProfiles[$name]

        Ensure-Consultation $patient.id "Demo rica - retorno julho" "2026-07-08T14:00:00Z" "Realizada" "Revisao de adesao, sintomas e evolucao do primeiro bloco." | Out-Null
        Ensure-Consultation $patient.id "Demo rica - retorno agosto" "2026-08-12T14:00:00Z" "Realizada" "Reavaliacao do plano, rotina, medidas e marcadores." | Out-Null

        Rich-EnsureSoap $patient.id "SOAP RICO - JULHO" "2026-07-08T14:30:00Z" "Paciente relata adaptacao ao plano e identifica barreiras praticas da rotina." "Resposta clinica compativel com adesao e comportamento registrados." | Out-Null
        Rich-EnsureSoap $patient.id "SOAP RICO - AGOSTO" "2026-08-12T14:30:00Z" "Paciente percebe mudancas em energia, fome, sono e desempenho ao longo das semanas." "Evolucao revisada de forma integrada com medidas, exames e check-ins." | Out-Null

        $sleepRecords = @()
        $activityRecords = @()
        for ($i=0; $i -lt 7; $i++) {
            $day = 7 + $i
            $sleepRecords += @{data=("2026-08-{0:D2}" -f $day);valor=([math]::Round($profile.sonoHoras + (($i % 3) * 0.15),1));concluida=$null}
            $activityRecords += @{data=("2026-08-{0:D2}" -f $day);valor=$(if ($i % 2 -eq 0) { 1 } else { 0 });concluida=$(if ($i % 2 -eq 0) { $true } else { $false })}
        }
        Ensure-Goal $patient.id "Sono regular demo rica" 7.5 "h" "Diaria" $sleepRecords
        Ensure-Goal $patient.id "Movimento planejado demo rica" 1 "sessao" "Diaria" $activityRecords

        for ($i=0; $i -lt 5; $i++) {
            $day = 9 + $i
            Ensure-Diary $patient.id ("2026-08-{0:D2}T08:00:00Z" -f $day) "Energia" ("Energia demo rica {0:D2}" -f $day) $null $null (6 + ($i % 4))
            Ensure-Diary $patient.id ("2026-08-{0:D2}T20:00:00Z" -f $day) "Fome" ("Fome demo rica {0:D2}" -f $day) $null $null (4 + ($i % 3))
        }
    }
}

Run-RichStep "[R3/10] Criando evolucao de anamnese e habitos..." {
    foreach ($name in $script:RichProfiles.Keys) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }
        $profile = $script:RichProfiles[$name]

        Rich-EnsureAnamnese $patient.id "ANAMNESE RICA - BASELINE" "2026-06-03T12:00:00Z" $profile -1 | Out-Null
        Rich-EnsureAnamnese $patient.id "ANAMNESE RICA - INTERMEDIARIA" "2026-07-10T12:00:00Z" $profile 0 | Out-Null
        Rich-EnsureAnamnese $patient.id "ANAMNESE RICA - ATUAL" "2026-08-13T12:00:00Z" $profile 1 | Out-Null
    }
}

Run-RichStep "[R4/10] Complementando evolucao corporal e exames..." {
    $weightsExtra = @{
        "Ana Ribeiro"=@(68.8,67.4)
        "Bruno Martins"=@(96.2,99.2)
        "Carla Souza"=@(63.6,63.3)
        "Diego Alves"=@(81.3,80.4)
        "Elisa Ferreira"=@(77.6,77.5)
    }
    $fatExtra = @{
        "Ana Ribeiro"=@(28.5,27.5)
        "Bruno Martins"=@(29.1,30.6)
        "Carla Souza"=@(23.5,23.1)
        "Diego Alves"=@(18.8,18.0)
        "Elisa Ferreira"=@(33.2,33.0)
    }
    $waistExtra = @{
        "Ana Ribeiro"=@(83,80)
        "Bruno Martins"=@(108,112)
        "Carla Souza"=@(75,74)
        "Diego Alves"=@(87,85)
        "Elisa Ferreira"=@(94,93)
    }

    foreach ($name in $script:RichProfiles.Keys) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }

        Ensure-Evaluation $patient.id "2026-07-15T12:00:00Z" $weightsExtra[$name][0] 1.70 $fatExtra[$name][0] $waistExtra[$name][0] 122 78 72 | Out-Null
        Ensure-Evaluation $patient.id "2026-08-14T12:00:00Z" $weightsExtra[$name][1] 1.70 $fatExtra[$name][1] $waistExtra[$name][1] 120 76 70 | Out-Null

        $profile = $script:RichProfiles[$name]
        $labName = "Demo rica - painel ampliado - $name"
        $allLabs = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/exames" -Headers $headers)
        if (-not ($allLabs | Where-Object { $_.laboratorio -eq $labName } | Select-Object -First 1)) {
            $labValues = @(
                @{nome="Glicemia";valor=$profile.lab.Glicemia;min=70;max=99;refTexto=$null},
                @{nome="Hemoglobina glicada";valor=$profile.lab.HbA1c;min=4.0;max=5.6;refTexto=$null},
                @{nome="LDL";valor=$profile.lab.LDL;min=$null;max=129;refTexto="Meta individual conforme risco."},
                @{nome="HDL";valor=$profile.lab.HDL;min=40;max=$null;refTexto=$null},
                @{nome="Triglicerideos";valor=$profile.lab.TG;min=$null;max=149;refTexto=$null},
                @{nome="TSH";valor=$profile.lab.TSH;min=0.4;max=4.0;refTexto=$null},
                @{nome="Ferritina";valor=$profile.lab.Ferritina;min=30;max=300;refTexto="Faixa demonstrativa."},
                @{nome="Vitamina D";valor=$profile.lab.VitD;min=30;max=100;refTexto="Faixa demonstrativa."}
            )

            $results = @()
            foreach ($v in $labValues) {
                $marker = $script:RichMarkers[$v.nome]
                if ($null -eq $marker) { continue }
                $results += @{
                    marcadorId=$marker.id;valorNumerico=$v.valor;valorTexto=$null;unidade=$marker.unidadePadrao
                    referenciaMinima=$v.min;referenciaMaxima=$v.max;referenciaTexto=$v.refTexto;observacao=$null
                }
            }

            Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/exames" -Headers $headers -Method Post -ContentType "application/json" -Body (Json @{
                dataColetaUtc="2026-08-11T09:00:00Z";laboratorio=$labName
                observacoes="Painel ampliado ficticio para demonstracao.";resultados=$results
            } 20) | Out-Null
        }
    }
}

Run-RichStep "[R5/10] Montando alimentacao completa e duas fases nutricionais..." {
    foreach ($name in $script:RichProfiles.Keys) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }
        $profile = $script:RichProfiles[$name]

        $plan = Rich-EnsureNutritionPlan $patient.id $name $profile
        $script:RichNutritionPlans[$name] = $plan

        $basePhase = Rich-EnsureNutritionPhase $patient.id $plan.id "Fase demo rica - estrutura" "Adaptacao" "Organizar horarios, fome e qualidade alimentar." "2026-06-01" "2026-06-30" "Concluida" $profile.startWeight 70 "Rotina previsivel e tolerancia adequada."
        $currentPhase = Rich-EnsureNutritionPhase $patient.id $plan.id "Fase demo rica - atual" "Acompanhamento" $profile.objetivo "2026-07-01" "2026-10-31" "EmAndamento" $profile.targetWeight 82 "Revisar peso, adesao, energia e exames antes da transicao."

        $script:RichNutritionBase[$name] = $basePhase
        $script:RichNutritionCurrent[$name] = $currentPhase
    }
}

Run-RichStep "[R6/10] Criando treinos ricos e fases de periodizacao..." {
    foreach ($name in @("Ana Ribeiro","Bruno Martins","Diego Alves")) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }
        $profile = $script:RichProfiles[$name]

        $plan = Rich-EnsureWorkout $patient.id $name $profile
        $script:RichWorkoutPlans[$name] = $plan

        $basePhase = Rich-EnsureTrainingPhase $patient.id $plan.id "Base tecnica demo rica" "Concluida" $profile
        $currentPhase = Rich-EnsureTrainingPhase $patient.id $plan.id "Bloco atual demo rica" "EmAndamento" $profile

        $script:RichTrainingBase[$name] = $basePhase
        $script:RichTrainingCurrent[$name] = $currentPhase
    }
}

Run-RichStep "[R7/10] Populando check-ins longitudinais e comparacao de fases..." {
    $dates = @(
        "2026-06-10T11:00:00Z",
        "2026-06-24T11:00:00Z",
        "2026-07-10T11:00:00Z",
        "2026-07-24T11:00:00Z",
        "2026-08-07T11:00:00Z",
        "2026-08-14T11:00:00Z"
    )

    foreach ($name in $script:RichProfiles.Keys) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }

        $profile = $script:RichProfiles[$name]
        $npBase = $script:RichNutritionBase[$name]
        $npCurrent = $script:RichNutritionCurrent[$name]
        $tpBase = $script:RichTrainingBase[$name]
        $tpCurrent = $script:RichTrainingCurrent[$name]

        $deltaTotal = $profile.currentWeight - $profile.startWeight

        for ($i=0; $i -lt $dates.Count; $i++) {
            $factor = $i / 5.0
            $weight = [math]::Round($profile.startWeight + ($deltaTotal * $factor),1)

            if ($name -eq "Bruno Martins") {
                $food = @(68,64,58,55,60,62)[$i]
                $train = @(62,60,55,50,52,55)[$i]
                $energy = @(6,6,5,5,4,5)[$i]
                $sleep = @(5,5,5,4,4,5)[$i]
            }
            elseif ($name -eq "Elisa Ferreira") {
                $food = @(60,62,58,61,63,65)[$i]
                $train = @(20,20,25,25,30,30)[$i]
                $energy = @(5,5,5,6,5,6)[$i]
                $sleep = @(4,4,5,5,5,5)[$i]
            }
            else {
                $food = @(72,76,80,84,87,90)[$i]
                $train = @(70,75,78,82,86,90)[$i]
                $energy = @(6,6,7,7,8,8)[$i]
                $sleep = @(6,6,7,7,7,8)[$i]
            }

            $nphase = $(if ($i -lt 2) { $npBase } else { $npCurrent })
            $tphase = $(if ($i -lt 2) { $tpBase } else { $tpCurrent })

            Rich-EnsureCheckIn $patient.id $dates[$i] $weight $food $train (4 + ($i % 2)) $energy $sleep (5 + $i) $nphase $tphase "Check-in rico: peso, adesao, fome, energia, sono e percepcao." | Out-Null
        }
    }
}

Run-RichStep "[R8/10] Criando historico de treino, progressao e sinais..." {
    $patientPassword = "PacienteDemo_123!"

    foreach ($name in @("Ana Ribeiro","Bruno Martins","Diego Alves")) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }
        $emailPatient = $patient.email
        if ([string]::IsNullOrWhiteSpace([string]$emailPatient)) {
            $emailPatient = ($name.ToLower().Replace(" ",".")) + ".demo@healthplatform.local"
        }

        Rich-CreateWorkoutExecutions $patient.id $emailPatient $patientPassword $script:RichProfiles[$name].pattern
        Write-Host "    Execucoes criadas/reaproveitadas para $name"
    }
}

Run-RichStep "[R9/10] Registrando revisoes de fase e relatorios..." {
    foreach ($name in $script:RichProfiles.Keys) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }

        Rich-EnsurePhaseReview $patient.id "Nutricao" $script:RichNutritionCurrent[$name] | Out-Null

        if ($null -ne $script:RichTrainingCurrent[$name]) {
            Rich-EnsurePhaseReview $patient.id "Treino" $script:RichTrainingCurrent[$name] | Out-Null
        }

        Rich-EnsureReport $patient.id $name | Out-Null
    }
}

Run-RichStep "[R10/10] Finalizando pendencias, follow-up, notificacoes e resumo..." {
    # Mais alguns contrastes para Carteira / Central do Dia / Pendencias.
    Ensure-Pending $patients["Carla Souza"].id "RICH_CARLA_TSH" "Media" "Revisar funcao tireoidiana" "TSH do painel ampliado ficou acima da referencia demonstrativa." "2026-08-18T18:00:00Z" | Out-Null
    Ensure-Pending $patients["Diego Alves"].id "RICH_DIEGO_PERFORMANCE" "Baixa" "Revisar progressao de carga" "Paciente com historico suficiente para revisar progressao do bloco." "2026-08-22T18:00:00Z" | Out-Null

    Ensure-FollowUp $patients["Ana Ribeiro"].id "WhatsApp" "Relatou boa adaptacao e pediu manutencao do plano" "2026-08-14T17:00:00Z" "2026-08-28T17:00:00Z" | Out-Null
    Ensure-FollowUp $patients["Bruno Martins"].id "Telefone" "Reforcado retorno por baixa adesao e exames alterados" "2026-08-14T15:00:00Z" "2026-08-17T15:00:00Z" | Out-Null
    Ensure-FollowUp $patients["Elisa Ferreira"].id "WhatsApp" "Paciente respondeu e combinou retomada gradual" "2026-08-14T16:00:00Z" "2026-08-21T16:00:00Z" | Out-Null

    Invoke-RestMethod -Uri "$base/api/notificacoes/sincronizar" -Headers $headers -Method Post | Out-Null

    Write-Host ""
    Write-Host "RESUMO DOS 5 PRONTUARIOS RICOS" -ForegroundColor Magenta
    foreach ($name in @("Ana Ribeiro","Bruno Martins","Carla Souza","Diego Alves","Elisa Ferreira")) {
        $patient = Rich-GetPatient $name
        if ($null -eq $patient) { continue }

        $consultas = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/consultas" -Headers $headers)
        $soap = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/evolucoes" -Headers $headers)
        $anam = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/anamneses" -Headers $headers)
        $evals = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/avaliacoes" -Headers $headers)
        $labs = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/exames" -Headers $headers)
        $nutrition = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/planos-alimentares" -Headers $headers)
        $workouts = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/treinos" -Headers $headers)
        $checks = Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/check-ins?limite=100" -Headers $headers
        $reports = As-Array (Invoke-RestMethod -Uri "$base/api/pacientes/$($patient.id)/relatorios" -Headers $headers)

        Write-Host ("  {0,-16} cons {1,2} | SOAP {2,2} | anam {3,2} | aval {4,2} | exames {5,2} | dietas {6,2} | treinos {7,2} | checkins {8,2} | rel {9,2}" -f `
            $name,$consultas.Count,$soap.Count,$anam.Count,$evals.Count,$labs.Count,$nutrition.Count,$workouts.Count,(As-Array $checks.itens).Count,$reports.Count)
    }
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " POPULAR REMOTO RICO V2 FINALIZADO" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Telas alimentadas pela camada rica:" -ForegroundColor Cyan
Write-Host "  Dashboard / Hoje / Carteira / Agenda / Pendencias / Follow-up / Gestao"
Write-Host "  Resumo / Consultas / Evolucoes SOAP / Anamnese / Avaliacoes / Exames"
Write-Host "  Plano alimentar / Metas / Diario / Relatorios / Timeline"
Write-Host "  Fases nutricionais / check-ins / comparativo de fases / revisoes"
Write-Host "  Treinos / volume muscular / progressao de carga / recordes / sinais"
Write-Host "  Portal do paciente para Ana, Bruno e Diego"
Write-Host ""
Write-Host "Senha dos pacientes demo: PacienteDemo_123!" -ForegroundColor Green

if ($script:RichWarnings.Count -gt 0) {
    Write-Host ""
    Write-Host "AVISOS DE ENRIQUECIMENTO (nao interromperam o restante):" -ForegroundColor Yellow
    foreach ($w in $script:RichWarnings) {
        Write-Host "  - $w" -ForegroundColor DarkYellow
    }
} else {
    Write-Host "Todas as 10 camadas extras terminaram sem aviso." -ForegroundColor Green
}

Write-Host ""
Write-Host "O script pode ser executado novamente; os principais registros sao identificados por nome, data ou tag." -ForegroundColor DarkGreen
