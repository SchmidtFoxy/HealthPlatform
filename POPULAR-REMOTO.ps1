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
