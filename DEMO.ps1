$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$base = "http://localhost:5180"
$settings = Get-Content ".\src\HealthPlatform.Api\appsettings.json" -Raw | ConvertFrom-Json
$email = $settings.Seed.AdminEmail
$senha = $settings.Seed.AdminPassword

Write-Host "[1/21] Login..." -ForegroundColor Cyan
$loginBody = @{ email = $email; senha = $senha } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
$token = $login.accessToken
if ([string]::IsNullOrWhiteSpace($token)) { throw "Login nao retornou accessToken." }
$headers = @{ Authorization = "Bearer $token" }

Write-Host "[2/21] Configurando profissional demo..." -ForegroundColor Cyan
$profBody = @{
    nome = "Dr. Demo HealthPlatform"
    registroProfissional = "CRM-DEMO-001"
    especialidade = "Medicina e Performance"
} | ConvertTo-Json
$prof = Invoke-RestMethod -Uri "$base/api/profissionais/me" -Headers $headers -Method Put -ContentType "application/json" -Body $profBody
Write-Host "    Profissional: $($prof.nome) / $($prof.registroProfissional)" -ForegroundColor Green

Write-Host "[3/21] Localizando/criando paciente demo..." -ForegroundColor Cyan
$lista = Invoke-RestMethod -Uri "$base/api/pacientes?busca=Paciente%20Demo%20Clinico&tamanhoPagina=10&incluirInativos=true" -Headers $headers -Method Get
$paciente = $lista.itens | Where-Object { $_.nome -eq "Paciente Demo Clinico" } | Select-Object -First 1
if ($null -eq $paciente) {
    $pacienteBody = @{
        nome = "Paciente Demo Clinico"
        cpf = "90000000001"
        dataNascimento = "1992-05-17"
        sexo = "Masculino"
        telefone = "41999990001"
        email = "paciente.demo@healthplatform.local"
        profissao = "Paciente de demonstracao"
    } | ConvertTo-Json
    $paciente = Invoke-RestMethod -Uri "$base/api/pacientes" -Headers $headers -Method Post -ContentType "application/json" -Body $pacienteBody
    Write-Host "    Paciente criado: $($paciente.nome)" -ForegroundColor Green
} else {
    Write-Host "    Paciente ja existe: $($paciente.nome)" -ForegroundColor DarkGreen
}

Write-Host "[4/21] Localizando/criando consulta demo..." -ForegroundColor Cyan
$consultas = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/consultas" -Headers $headers -Method Get
$consulta = $consultas | Where-Object { $_.motivo -like "Consulta demonstrativa v0.1.*" } | Select-Object -First 1
if ($null -eq $consulta) {
    $consultaBody = @{
        dataHoraUtc = (Get-Date).ToUniversalTime().ToString("o")
        motivo = "Consulta demonstrativa v0.1.5"
        queixaPrincipal = "Paciente busca melhora de saude, composicao corporal e rotina."
        evolucao = "Registro inicial para demonstracao do nucleo clinico."
        conduta = "Acompanhar indicadores clinicos, laboratoriais e definir metas progressivas."
        orientacoes = "Registrar evolucao e retornar para reavaliacao."
        status = "Realizada"
    } | ConvertTo-Json
    $consulta = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/consultas" -Headers $headers -Method Post -ContentType "application/json" -Body $consultaBody
    Write-Host "    Consulta criada." -ForegroundColor Green
} else {
    Write-Host "    Consulta demo ja existe." -ForegroundColor DarkGreen
}

Write-Host "[5/21] Garantindo anamnese estruturada..." -ForegroundColor Cyan
$anamneses = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/anamneses" -Headers $headers -Method Get
$anamnese = $anamneses | Where-Object { $_.consultaId -eq $consulta.id } | Select-Object -First 1
if ($null -eq $anamnese) {
    $perguntas = Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers -Method Get
    $pergunta = $perguntas | Where-Object { $_.texto -eq "Como voce avalia sua disposicao diaria?" } | Select-Object -First 1
    if ($null -eq $pergunta) {
        $perguntaBody = @{ texto = "Como voce avalia sua disposicao diaria?"; tipoResposta = "Escala"; opcoes = @(); ordem = 1 } | ConvertTo-Json -Depth 5
        $pergunta = Invoke-RestMethod -Uri "$base/api/anamnese/perguntas" -Headers $headers -Method Post -ContentType "application/json" -Body $perguntaBody
    }
    $anamneseBody = @{
        consultaId = $consulta.id
        dataUtc = (Get-Date).ToUniversalTime().ToString("o")
        objetivoAcompanhamento = "Melhorar composicao corporal, energia e consistencia da rotina."
        historicoDoencas = "Sem doencas cronicas conhecidas no demo."
        historicoFamiliar = "Historia familiar demonstrativa de hipertensao."
        cirurgias = "Nega cirurgias relevantes."
        alergias = "Nega alergias medicamentosas conhecidas."
        medicamentos = "Nenhum medicamento de uso continuo no demo."
        suplementos = "Creatina 3 g/dia."
        tabagismo = "Nao fumante"
        etilismo = "Social"
        sonoHorasMedia = 6.5
        sonoQualidade = "Regular"
        despertaDuranteNoite = $true
        estresseNivel = 6
        atividadeFisica = "Musculacao e caminhada"
        atividadeFisicaDiasSemana = 4
        habitoIntestinal = "Regular, 1 vez ao dia"
        aguaLitrosDia = 2.2
        observacoes = "Anamnese demonstrativa reutilizada na v0.1.5."
        respostasPersonalizadas = @( @{ perguntaId = $pergunta.id; resposta = "7/10" } )
    } | ConvertTo-Json -Depth 8
    $anamnese = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/anamneses" -Headers $headers -Method Post -ContentType "application/json" -Body $anamneseBody
    Write-Host "    Anamnese criada." -ForegroundColor Green
} else {
    Write-Host "    Anamnese demo ja existe." -ForegroundColor DarkGreen
}

Write-Host "[6/21] Garantindo avaliacao demo..." -ForegroundColor Cyan
$avaliacoes = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/avaliacoes" -Headers $headers -Method Get
$avaliacao = $avaliacoes | Where-Object { $_.consultaId -eq $consulta.id } | Select-Object -First 1
if ($null -eq $avaliacao) {
    $avaliacaoBody = @{
        consultaId = $consulta.id; dataUtc = (Get-Date).ToUniversalTime().ToString("o")
        pesoKg = 88.4; alturaM = 1.78; percentualGordura = 24.5; massaMagraKg = 66.7; massaGordaKg = 21.7
        cinturaCm = 98.0; abdomenCm = 101.0; quadrilCm = 104.0; pressaoSistolica = 122; pressaoDiastolica = 78; frequenciaCardiaca = 72
    } | ConvertTo-Json
    $avaliacao = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/avaliacoes" -Headers $headers -Method Post -ContentType "application/json" -Body $avaliacaoBody
    Write-Host "    Avaliacao criada. IMC: $($avaliacao.imc)" -ForegroundColor Green
} else {
    Write-Host "    Avaliacao demo ja existe. IMC: $($avaliacao.imc)" -ForegroundColor DarkGreen
}

Write-Host "[7/21] Garantindo catalogo de marcadores..." -ForegroundColor Cyan
$catalogo = @(
    @{ nome = "Glicemia"; categoria = "Metabolico"; unidadePadrao = "mg/dL" },
    @{ nome = "LDL"; categoria = "Perfil lipidico"; unidadePadrao = "mg/dL" },
    @{ nome = "HDL"; categoria = "Perfil lipidico"; unidadePadrao = "mg/dL" },
    @{ nome = "TSH"; categoria = "Tireoide"; unidadePadrao = "uUI/mL" }
)
$marcadoresExistentes = @(Invoke-RestMethod -Uri "$base/api/exames/marcadores?incluirInativos=true" -Headers $headers -Method Get)
$marcadores = @{}
foreach ($m in $catalogo) {
    $existente = $marcadoresExistentes | Where-Object { $_.nome -eq $m.nome } | Select-Object -First 1
    if ($null -eq $existente) {
        $existente = Invoke-RestMethod -Uri "$base/api/exames/marcadores" -Headers $headers -Method Post -ContentType "application/json" -Body ($m | ConvertTo-Json)
        Write-Host "    Marcador criado: $($m.nome)" -ForegroundColor Green
    }
    $marcadores[$m.nome] = $existente
}

Write-Host "[8/21] Criando coleta laboratorial demo quando necessario..." -ForegroundColor Cyan
$exames = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/exames" -Headers $headers -Method Get)
$exame = $exames | Where-Object { $_.laboratorio -like "Laboratorio Demo v0.1.*" } | Sort-Object dataColetaUtc -Descending | Select-Object -First 1
if ($null -eq $exame) {
    $exameBody = @{
        dataColetaUtc = (Get-Date).AddDays(-3).ToUniversalTime().ToString("o")
        laboratorio = "Laboratorio Demo v0.1.5"
        observacoes = "Coleta demonstrativa para validar historico laboratorial."
        resultados = @(
            @{ marcadorId = $marcadores["Glicemia"].id; valorNumerico = 92; unidade = "mg/dL"; referenciaMinima = 70; referenciaMaxima = 99 },
            @{ marcadorId = $marcadores["LDL"].id; valorNumerico = 118; unidade = "mg/dL"; referenciaMaxima = 129; referenciaTexto = "Meta individual pode variar conforme risco cardiovascular." },
            @{ marcadorId = $marcadores["HDL"].id; valorNumerico = 55; unidade = "mg/dL"; referenciaMinima = 40 },
            @{ marcadorId = $marcadores["TSH"].id; valorNumerico = 2.1; unidade = "uUI/mL"; referenciaMinima = 0.4; referenciaMaxima = 4.0 }
        )
    } | ConvertTo-Json -Depth 8
    try {
        $exame = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/exames" -Headers $headers -Method Post -ContentType "application/json" -Body $exameBody
        Write-Host "    Exame criado com $($exame.resultados.Count) resultados." -ForegroundColor Green
    } catch {
        Write-Host "    Falha ao criar coleta laboratorial demo." -ForegroundColor Red
        if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $bodyErro = $reader.ReadToEnd()
            if (-not [string]::IsNullOrWhiteSpace($bodyErro)) { Write-Host "    Resposta da API: $bodyErro" -ForegroundColor Red }
        }
        throw
    }
} else {
    Write-Host "    Reaproveitando exame demo existente: $($exame.laboratorio) / $($exame.resultados.Count) resultados." -ForegroundColor DarkGreen
}

Write-Host "[9/21] Evolucao da glicemia..." -ForegroundColor Cyan
# Use o marcador efetivamente gravado na coleta reaproveitada. Isso evita depender
# de uma entrada de catalogo diferente quando o banco veio de uma versao anterior.
$glicemiaResultado = $exame.resultados | Where-Object { $_.marcadorNome -eq "Glicemia" } | Select-Object -First 1
if ($null -eq $glicemiaResultado -or [string]::IsNullOrWhiteSpace([string]$glicemiaResultado.marcadorId)) {
    throw "A coleta demo nao possui um resultado de Glicemia com marcadorId valido."
}
$glicemiaMarcadorId = $glicemiaResultado.marcadorId
try {
    $evolucao = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/exames/evolucao/$glicemiaMarcadorId" -Headers $headers -Method Get
} catch {
    Write-Host "    Falha ao consultar evolucao da glicemia." -ForegroundColor Red
    if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $bodyErro = $reader.ReadToEnd()
        if (-not [string]::IsNullOrWhiteSpace($bodyErro)) { Write-Host "    Resposta da API: $bodyErro" -ForegroundColor Red }
    }
    throw
}
Write-Host "    Pontos historicos: $($evolucao.pontos.Count)" -ForegroundColor Green
if ($evolucao.pontos.Count -gt 0) {
    $ultimo = $evolucao.pontos | Select-Object -Last 1
    Write-Host "    Ultimo valor: $($ultimo.valor) $($ultimo.unidade) / $($ultimo.situacao)" -ForegroundColor Green
}

Write-Host "[10/21] Gerando relatorio clinico snapshot..." -ForegroundColor Cyan
$relatorios = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/relatorios" -Headers $headers -Method Get)
$relatorio = $relatorios | Where-Object { $_.titulo -eq "Relatorio Demo v0.1.5" } | Select-Object -First 1
if ($null -eq $relatorio) {
    $relBody = @{
        dataInicioUtc = (Get-Date).AddDays(-30).ToUniversalTime().ToString("o")
        dataFimUtc = (Get-Date).ToUniversalTime().ToString("o")
        titulo = "Relatorio Demo v0.1.5"
        conclusaoMedica = "Snapshot demonstrativo: manter acompanhamento longitudinal e correlacionar os dados clinicos com a evolucao do paciente."
    } | ConvertTo-Json
    $relatorio = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/relatorios" -Headers $headers -Method Post -ContentType "application/json" -Body $relBody
    Write-Host "    Relatorio criado. Marcadores recentes: $($relatorio.conteudo.examesRecentes.Count)" -ForegroundColor Green
} else { Write-Host "    Relatorio demo ja existe." -ForegroundColor DarkGreen }

Write-Host "[11/21] Validando HTML imprimivel..." -ForegroundColor Cyan
$html = Invoke-WebRequest -Uri "$base/api/relatorios/$($relatorio.id)/html" -Headers $headers -Method Get -UseBasicParsing
if ($html.StatusCode -ne 200 -or $html.Content -notlike "*Imprimir / salvar em PDF*") { throw "HTML do relatorio invalido." }
Write-Host "    HTML OK. Pronto para imprimir/salvar como PDF no navegador." -ForegroundColor Green

Write-Host "[12/21] Timeline clinica..." -ForegroundColor Cyan
$timeline = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/timeline" -Headers $headers -Method Get
$tipos = ($timeline | ForEach-Object { $_.tipo }) -join ", "
Write-Host "    Eventos na timeline: $($timeline.Count)" -ForegroundColor Green
Write-Host "    Tipos: $tipos" -ForegroundColor Green

Write-Host "[13/21] Garantindo catalogo de alimentos..." -ForegroundColor Cyan
$catalogoAlimentos = @(
    @{ nome="Peito de frango"; categoria="Proteinas"; caloriasPor100g=165; proteinasPor100g=31; carboidratosPor100g=0; gordurasPor100g=3.6; fibrasPor100g=0 },
    @{ nome="Arroz branco cozido"; categoria="Carboidratos"; caloriasPor100g=128; proteinasPor100g=2.5; carboidratosPor100g=28.1; gordurasPor100g=0.2; fibrasPor100g=1.6 },
    @{ nome="Feijao carioca cozido"; categoria="Carboidratos"; caloriasPor100g=76; proteinasPor100g=4.8; carboidratosPor100g=13.6; gordurasPor100g=0.5; fibrasPor100g=8.5 },
    @{ nome="Patinho grelhado"; categoria="Proteinas"; caloriasPor100g=219; proteinasPor100g=35.9; carboidratosPor100g=0; gordurasPor100g=7.3; fibrasPor100g=0 },
    @{ nome="Ovo inteiro"; categoria="Proteinas"; caloriasPor100g=143; proteinasPor100g=12.6; carboidratosPor100g=0.7; gordurasPor100g=9.5; fibrasPor100g=0 }
)
$existentesAlimentos = @(Invoke-RestMethod -Uri "$base/api/alimentos?incluirInativos=true" -Headers $headers -Method Get)
$alimentos = @{}
foreach ($a in $catalogoAlimentos) {
    $existente = $existentesAlimentos | Where-Object { $_.nome -eq $a.nome } | Select-Object -First 1
    if ($null -eq $existente) { $existente = Invoke-RestMethod -Uri "$base/api/alimentos" -Headers $headers -Method Post -ContentType "application/json" -Body ($a | ConvertTo-Json); Write-Host "    Alimento criado: $($a.nome)" -ForegroundColor Green }
    $alimentos[$a.nome] = $existente
}

Write-Host "[14/21] Criando plano alimentar demo quando necessario..." -ForegroundColor Cyan
$planos = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/planos-alimentares" -Headers $headers -Method Get)
$plano = $planos | Where-Object { $_.nome -eq "Plano alimentar Demo v0.1.6" } | Select-Object -First 1
if ($null -eq $plano) {
    $planoBody = @{
        nome = "Plano alimentar Demo v0.1.6"; dataInicio = (Get-Date).ToString("yyyy-MM-dd"); status = "Ativo"; observacoes = "Plano demonstrativo para validar refeicoes, macros e substituicoes."
        refeicoes = @(
            @{ nome="Cafe da manha"; horario="08:00:00"; ordem=1; observacoes="Refeicao demonstrativa"; itens=@(
                @{ alimentoId=$alimentos["Ovo inteiro"].id; quantidade=2; unidade="unidades"; quantidadeGramas=100; observacao="Preparacao conforme preferencia"; substituicoes=@() }
            )},
            @{ nome="Almoco"; horario="12:30:00"; ordem=2; observacoes=$null; itens=@(
                @{ alimentoId=$alimentos["Peito de frango"].id; quantidade=150; unidade="g"; quantidadeGramas=150; observacao=$null; substituicoes=@(
                    @{ alimentoId=$alimentos["Patinho grelhado"].id; quantidade=130; unidade="g"; quantidadeGramas=130; observacao="Opcao de substituicao proteica" }
                )},
                @{ alimentoId=$alimentos["Arroz branco cozido"].id; quantidade=150; unidade="g"; quantidadeGramas=150; observacao=$null; substituicoes=@() },
                @{ alimentoId=$alimentos["Feijao carioca cozido"].id; quantidade=100; unidade="g"; quantidadeGramas=100; observacao=$null; substituicoes=@() }
            )}
        )
    } | ConvertTo-Json -Depth 12
    $plano = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/planos-alimentares" -Headers $headers -Method Post -ContentType "application/json" -Body $planoBody
    Write-Host "    Plano criado com $($plano.refeicoes.Count) refeicoes." -ForegroundColor Green
} else { Write-Host "    Plano alimentar demo ja existe." -ForegroundColor DarkGreen }
Write-Host "    Total diario demo: $($plano.totaisDiarios.calorias) kcal / P $($plano.totaisDiarios.proteinasG) g / C $($plano.totaisDiarios.carboidratosG) g / G $($plano.totaisDiarios.gordurasG) g" -ForegroundColor Green

Write-Host "[15/21] Validando timeline com plano alimentar..." -ForegroundColor Cyan
$timelineFinal = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/timeline" -Headers $headers -Method Get)
$planoEvento = $timelineFinal | Where-Object { $_.tipo -eq "plano_alimentar" } | Select-Object -First 1
if ($null -eq $planoEvento) { throw "Plano alimentar nao apareceu na timeline." }
Write-Host "    Timeline OK. Eventos: $($timelineFinal.Count)" -ForegroundColor Green

Write-Host "[16/21] Criando metas e registrando progresso de hoje..." -ForegroundColor Cyan
$metasExistentes = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/metas?incluirEncerradas=true" -Headers $headers -Method Get)
$metaAgua = $metasExistentes | Where-Object { $_.nome -eq "Agua 3L por dia" } | Select-Object -First 1
if ($null -eq $metaAgua) {
    $metaBody = @{ nome="Agua 3L por dia"; tipo="Hidratacao"; valorObjetivo=3; unidade="L"; frequencia="Diaria"; dataInicio=(Get-Date).ToString("yyyy-MM-dd"); observacoes="Meta demonstrativa de hidratacao." } | ConvertTo-Json
    $metaAgua = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/metas" -Headers $headers -Method Post -ContentType "application/json" -Body $metaBody
    Write-Host "    Meta criada: $($metaAgua.nome)" -ForegroundColor Green
} else { Write-Host "    Meta de hidratacao ja existe." -ForegroundColor DarkGreen }
$registroMetaBody = @{ data=(Get-Date).ToString("yyyy-MM-dd"); valor=2.2; observacao="Progresso demo do dia." } | ConvertTo-Json
$registroMeta = Invoke-RestMethod -Uri "$base/api/metas/$($metaAgua.id)/registros" -Headers $headers -Method Post -ContentType "application/json" -Body $registroMetaBody
Write-Host "    Progresso registrado: $($registroMeta.valor) L" -ForegroundColor Green

Write-Host "[17/21] Registrando diario do paciente..." -ForegroundColor Cyan
$hoje = (Get-Date).ToString("yyyy-MM-dd")
$diarioHoje = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/diario?inicio=$hoje&fim=$hoje" -Headers $headers -Method Get)
$sonoHoje = $diarioHoje | Where-Object { $_.tipo -eq "Sono" } | Select-Object -First 1
if ($null -eq $sonoHoje) {
    $sonoBody = @{ dataHoraUtc=(Get-Date).ToUniversalTime().ToString("o"); tipo="Sono"; descricao="Sono demo registrado pelo paciente."; valorNumerico=7.5; unidade="h"; escala=8 } | ConvertTo-Json
    $sonoHoje = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/diario" -Headers $headers -Method Post -ContentType "application/json" -Body $sonoBody
    Write-Host "    Sono registrado: $($sonoHoje.valorNumerico) h / qualidade $($sonoHoje.escala)/10" -ForegroundColor Green
} else { Write-Host "    Registro de sono demo ja existe hoje." -ForegroundColor DarkGreen }
$aguaHoje = $diarioHoje | Where-Object { $_.tipo -eq "Hidratacao" } | Select-Object -First 1
if ($null -eq $aguaHoje) {
    $aguaBody = @{ dataHoraUtc=(Get-Date).ToUniversalTime().ToString("o"); tipo="Hidratacao"; descricao="Consumo acumulado no momento."; valorNumerico=2.2; unidade="L" } | ConvertTo-Json
    $aguaHoje = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/diario" -Headers $headers -Method Post -ContentType "application/json" -Body $aguaBody
    Write-Host "    Hidratacao registrada no diario." -ForegroundColor Green
}

Write-Host "[18/21] Validando resumo do dia e timeline..." -ForegroundColor Cyan
$resumoDia = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/resumo-dia?data=$hoje" -Headers $headers -Method Get
if ($resumoDia.metasAtivas -lt 1) { throw "Resumo do dia nao retornou a meta criada." }
$timelineFinal = @(Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/timeline" -Headers $headers -Method Get)
if ($null -eq ($timelineFinal | Where-Object { $_.tipo -eq "meta" } | Select-Object -First 1)) { throw "Meta nao apareceu na timeline." }
if ($null -eq ($timelineFinal | Where-Object { $_.tipo -eq "registro_diario" } | Select-Object -First 1)) { throw "Diario nao apareceu na timeline." }
Write-Host "    Resumo OK: $($resumoDia.metasAtivas) meta(s), $($resumoDia.registros.Count) registro(s), $($resumoDia.percentualConclusao)% concluidas." -ForegroundColor Green
Write-Host "    Timeline OK. Eventos: $($timelineFinal.Count)" -ForegroundColor Green

Write-Host "[19/21] Validando portal/home do paciente..." -ForegroundColor Cyan
$portal = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/portal/home?data=$hoje" -Headers $headers -Method Get
if ($portal.paciente.id -ne $paciente.id) { throw "Portal retornou paciente incorreto." }
if ($null -eq $portal.planoAlimentarAtual) { throw "Portal nao retornou plano alimentar ativo." }
if ($portal.metasAtivas -lt 1) { throw "Portal nao retornou metas ativas." }
if ($portal.registrosHoje.Count -lt 1) { throw "Portal nao retornou registros do diario de hoje." }
if ($portal.examesRecentes.Count -lt 1) { throw "Portal nao retornou exames recentes." }
Write-Host "    Home OK: plano '$($portal.planoAlimentarAtual.nome)', $($portal.metasAtivas) meta(s), $($portal.registrosHoje.Count) registro(s), $($portal.examesRecentes.Count) exame(s)." -ForegroundColor Green
Write-Host "    Evolucao: peso $($portal.evolucaoCorporal.pesoKg) kg / IMC $($portal.evolucaoCorporal.imc)." -ForegroundColor Green


Write-Host "[20/21] Garantindo consulta futura na agenda..." -ForegroundColor Cyan
$offsetAgenda = -180
$agoraLocal = Get-Date
$amanhaLocal = [DateTime]::SpecifyKind($agoraLocal.Date.AddDays(1).AddHours(14), [DateTimeKind]::Unspecified)
$inicioBusca = $agoraLocal.ToString("yyyy-MM-dd")
$fimBusca = $agoraLocal.Date.AddDays(7).ToString("yyyy-MM-dd")
$agendaPeriodo = @(Invoke-RestMethod -Uri "$base/api/agenda/periodo?inicio=$inicioBusca&fim=$fimBusca&offsetMinutos=$offsetAgenda" -Headers $headers -Method Get)
$consultaAgenda = $agendaPeriodo | Where-Object { $_.motivo -eq "Retorno Demo Agenda v0.1.9" } | Select-Object -First 1
if (-not $consultaAgenda) {
    $utcAgenda = [DateTime]::SpecifyKind($amanhaLocal.AddMinutes(-$offsetAgenda), [DateTimeKind]::Utc).ToString("o")
    $bodyAgenda = @{
        dataHoraUtc = $utcAgenda
        motivo = "Retorno Demo Agenda v0.1.9"
        queixaPrincipal = $null
        evolucao = $null
        conduta = $null
        orientacoes = "Retorno programado pelo demo da agenda."
        status = "Confirmada"
    } | ConvertTo-Json
    $consultaAgenda = Invoke-RestMethod -Uri "$base/api/pacientes/$($paciente.id)/consultas" -Headers $headers -Method Post -ContentType "application/json" -Body $bodyAgenda
    Write-Host "    Consulta futura criada: $($amanhaLocal.ToString('dd/MM/yyyy HH:mm'))." -ForegroundColor Green
} else { Write-Host "    Consulta futura demo ja existe." -ForegroundColor DarkGreen }
$agendaAmanha = Invoke-RestMethod -Uri "$base/api/agenda?data=$($amanhaLocal.ToString('yyyy-MM-dd'))&offsetMinutos=$offsetAgenda" -Headers $headers -Method Get
if (@($agendaAmanha.consultas | Where-Object { $_.motivo -eq "Retorno Demo Agenda v0.1.9" }).Count -lt 1) { throw "Consulta demo nao apareceu na agenda de amanha." }
Write-Host "    Agenda amanha OK: $($agendaAmanha.total) consulta(s)." -ForegroundColor Green

Write-Host "[21/21] Validando dashboard do profissional..." -ForegroundColor Cyan
$dashboardProf = Invoke-RestMethod -Uri "$base/api/profissional/dashboard?offsetMinutos=$offsetAgenda" -Headers $headers -Method Get
if ($null -eq $dashboardProf.proximasConsultas) { throw "Dashboard profissional invalido." }
Write-Host "    Dashboard OK: $($dashboardProf.pacientesAtivos) paciente(s), $($dashboardProf.proximasConsultas.Count) proxima(s), $($dashboardProf.retornosPendentes) retorno(s) pendente(s)." -ForegroundColor Green

Write-Host ""
Write-Host "DEMO CLINICA v0.1.9 PRONTA." -ForegroundColor Green
Write-Host "PacienteId: $($paciente.id)" -ForegroundColor DarkGreen
Write-Host "ExameId: $($exame.id)" -ForegroundColor DarkGreen
Write-Host "RelatorioId: $($relatorio.id)" -ForegroundColor DarkGreen
Write-Host "PlanoAlimentarId: $($plano.id)" -ForegroundColor DarkGreen
Write-Host "MetaId: $($metaAgua.id)" -ForegroundColor DarkGreen
Write-Host "HTML: $base/api/relatorios/$($relatorio.id)/html" -ForegroundColor DarkGreen
Write-Host "Abra o Swagger e explore agenda, reagendamento, status rapido e dashboard profissional." -ForegroundColor DarkGreen
