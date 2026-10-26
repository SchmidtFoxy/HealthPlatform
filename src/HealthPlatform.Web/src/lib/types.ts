// Formatos das respostas da API usadas pelo novo front (somente os campos consumidos).

export interface Nutrientes {
  calorias: number;
  proteinas: number;
  carboidratos: number;
  gorduras: number;
  fibras: number;
}

export interface Totais {
  calorias: number;
  proteinasG: number;
  carboidratosG: number;
  gordurasG: number;
  fibrasG: number;
}

// ---------- Portal do paciente ----------

export interface ItemTreinoPortal {
  id: string;
  exercicioId: string;
  exercicio: string;
  grupoMuscular: string | null;
  equipamento: string | null;
  descricao: string | null;
  videoUrl: string | null;
  series: number;
  repeticoes: string;
  carga: number | null;
  unidadeCarga: string | null;
  descansoSegundos: number | null;
  tempoSegundos: number | null;
  rirAlvo: number | null;
  cadencia: string | null;
  tecnicaAvancada: string | null;
  observacoes: string | null;
}

export interface SessaoTreinoPortal {
  id: string;
  nome: string;
  diasSemana: string | null;
  ordem: number;
  observacoes: string | null;
  itens: ItemTreinoPortal[];
}

export interface PlanoTreinoPortal {
  id: string;
  nome: string;
  objetivo: string | null;
  dataInicio: string;
  dataFim: string | null;
  status: string;
  observacoes: string | null;
  profissional: string;
  sessoes: SessaoTreinoPortal[];
}

export interface MeusTreinos {
  total: number;
  permiteInicioLivre: boolean;
  planos: PlanoTreinoPortal[];
}

export interface ItemExecucaoHistorico {
  itemTreinoId: string;
  exercicio: string;
  seriesRealizadas: number | null;
  repeticoesRealizadas: string | null;
  cargaRealizada: number | null;
  unidadeCarga: string | null;
  esforcoPercebido: number | null;
  rirRealizado: number | null;
  concluido: boolean;
}

export interface ExecucaoHistorico {
  id: string;
  dataHoraInicioUtc: string;
  dataHoraFimUtc: string | null;
  duracaoMinutos: number | null;
  esforcoPercebido: number | null;
  observacoes: string | null;
  status: string;
  plano: string;
  sessao: string;
  itens: ItemExecucaoHistorico[];
}

export interface HistoricoTreinos {
  dias: number;
  total: number;
  execucoes: ExecucaoHistorico[];
}

export interface ItemRefeicaoPortal {
  id: string;
  alimento: string;
  quantidade: number;
  unidade: string;
  quantidadeGramas: number;
  observacao: string | null;
  nutrientes: Nutrientes;
  substituicoes: Omit<ItemRefeicaoPortal, 'substituicoes'>[];
}

export interface RefeicaoPortal {
  id: string;
  nome: string;
  horario: string | null;
  ordem: number;
  observacoes: string | null;
  itens: ItemRefeicaoPortal[];
}

export interface PlanoAlimentarPortal {
  plano: {
    id: string;
    nome: string;
    dataInicio: string;
    status: string;
    observacoes: string | null;
    profissional: string;
    refeicoes: RefeicaoPortal[];
    metaCalorias?: number | null;
    metaProteinasG?: number | null;
    metaCarboidratosG?: number | null;
    metaGordurasG?: number | null;
  } | null;
}

export type StatusRefeicao = 'Pendente' | 'Realizada' | 'Adaptada' | 'NaoRealizada';

export interface HomePaciente {
  data: string;
  paciente: { id: string; nome: string; dataNascimento: string | null; sexo: string | null };
  proximaConsulta: { dataHoraUtc?: string; profissionalNome?: string } | null;
  prontidaoDiaria: {
    score?: number;
    prontidaoScore?: number;
    sonoHoras?: number;
    energiaNivel?: number;
    dorNivel?: number;
    recuperacaoNivel?: number;
    recomendacaoTreino?: string;
    motivoRecomendacao?: string | null;
  } | null;
  gamificacao: { xpTotal: number; nivel: number; xpNoNivel: number; xpProximoNivel: number; streakDias: number; consistenciaScore: number };
  tendenciaRecuperacao: { prontidaoMedia7: number | null; sonoMedio7: number | null; treinos7: number; tendencia: string; mensagem: string };
  cargaTreino: { treinos7: number; duracaoMinutos7: number; rpeMedio7: number | null; diasDesdeUltimoTreino: number | null; nivelAtencao: string; mensagem: string };
  adesaoNutricional: {
    refeicoesPlanejadas: number;
    realizadas: number;
    adaptadas: number;
    naoRealizadas: number;
    refeicoes: { refeicaoId: string; nome: string; horario: string | null; status: StatusRefeicao; observacao: string | null }[];
  };
  estrategiaDoDia: { perfilDia: string; orientacaoTreino: string; sessoesPrevistas: string[] };
  coachDiario: { titulo: string; resumo: string; prioridades: { codigo: string; nivel: string; titulo: string; motivo: string; acao: string }[] };
  execucaoDoDia: { totalItens: number; concluidos: number; itens: { codigo: string; titulo: string; status: string }[] };
  planoAlimentarAtual: { id: string; nome: string; profissionalNome: string } | null;
}

export interface DiaHistorico {
  data: string;
  temCheckIn: boolean;
  prontidaoScore: number | null;
  sonoHoras: number | null;
  energiaNivel: number | null;
  dorNivel: number | null;
  recuperacaoNivel: number | null;
  treinosConcluidos: number;
  esforcoMedio: number | null;
}

export interface HistoricoDiario {
  inicio: string;
  fim: string;
  dias: number;
  itens: DiaHistorico[];
}

// ---------- Area profissional ----------

export interface Paciente {
  id: string;
  nome: string;
  cpf: string | null;
  dataNascimento: string | null;
  sexo: string | null;
  telefone: string | null;
  email: string | null;
  profissao: string | null;
  ativo: boolean;
  statusAcompanhamento: string;
  tags: string[];
  createdAtUtc: string;
}

export interface ListaPacientes {
  itens: Paciente[];
  total?: number;
}

export interface SinalDiario {
  pacienteId: string;
  pacienteNome: string;
  nivel: 'RevisarHoje' | 'Observar' | 'ContextoPendente' | 'Estavel' | string;
  razoes: string[];
  ultimoCheckInData: string | null;
  prontidaoScore: number | null;
  sonoHoras: number | null;
  energiaNivel: number | null;
  dorNivel: number | null;
  recuperacaoNivel: number | null;
  treinosUltimos7Dias: number;
  ultimoTreinoUtc: string | null;
}

export interface SinaisDiarios {
  dataReferencia: string;
  totalPacientesAtivos: number;
  totalComSinal: number;
  revisarHoje: number;
  observar: number;
  contextoPendente: number;
  sinais: SinalDiario[];
}

export interface ConsultaResumo {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  dataHoraUtc: string;
  dataHoraLocal: string;
  status: string;
  motivo: string | null;
}

export interface DashboardProfissional {
  data: string;
  profissionalNome: string;
  pacientesAtivos: number;
  consultasHoje: number;
  agendaHoje: ConsultaResumo[];
  proximasConsultas: ConsultaResumo[];
}

export interface ResumoClinico {
  pacienteId: string;
  pacienteNome: string;
  treinosUltimos30Dias: number;
  pendenciasAbertas: number;
  metasAtivas: number;
  proximaConsulta: { dataHoraInicioUtc?: string } | null;
  ultimaConsulta: { dataHoraInicioUtc?: string } | null;
}

export interface Alimento {
  id: string;
  nome: string;
  categoria: string | null;
  caloriasPor100g: number;
  proteinasPor100g: number;
  carboidratosPor100g: number;
  gordurasPor100g: number;
  fibrasPor100g: number;
  ativo: boolean;
}

export interface Exercicio {
  id: string;
  nome: string;
  grupoMuscular: string | null;
  equipamento: string | null;
  descricao: string | null;
  videoUrl: string | null;
  ativo: boolean;
}

export interface ItemRefeicaoPro {
  id: string;
  alimentoId: string;
  alimentoNome: string;
  quantidade: number;
  unidade: string;
  quantidadeGramas: number;
  observacao: string | null;
  totais: Totais;
  substituicoes: Omit<ItemRefeicaoPro, 'substituicoes'>[];
}

export interface RefeicaoPro {
  id: string;
  nome: string;
  horario: string | null;
  ordem: number;
  observacoes: string | null;
  metas: { calorias: number | null; proteinasG: number | null; carboidratosG: number | null; gordurasG: number | null; fibrasG: number | null };
  totais: Totais;
  itens: ItemRefeicaoPro[];
}

export interface SuplementoPlanoPro {
  suplementoId: string;
  refeicaoId: string | null;
  sessaoTreinoId: string | null;
  quantidadePorcoes: number;
  horario: string | null;
  contexto: string;
  observacoes: string | null;
}

export interface PlanoAlimentarPro {
  id: string;
  pacienteId: string;
  profissionalNome: string;
  nome: string;
  dataInicio: string;
  dataFim: string | null;
  status: string;
  observacoes: string | null;
  versao: number;
  metaCalorias: number | null;
  metaProteinasG: number | null;
  metaCarboidratosG: number | null;
  metaGordurasG: number | null;
  metaFibrasG: number | null;
  totaisDiarios: Totais;
  refeicoes: RefeicaoPro[];
  suplementos: (SuplementoPlanoPro & { id: string; refeicaoNome: string | null })[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface ItemTreinoPro {
  id: string;
  exercicioId: string;
  exercicioNome: string;
  grupoMuscular: string | null;
  equipamento: string | null;
  ordem: number;
  series: number;
  repeticoes: string;
  carga: number | null;
  unidadeCarga: string | null;
  descansoSegundos: number | null;
  tempoSegundos: number | null;
  rirAlvo: number | null;
  cadencia: string | null;
  tecnicaAvancada: string | null;
  tecnicaAvancadaCodigo: string | null;
  tecnicaAvancadaParametros: string | null;
  observacoes: string | null;
}

export interface SessaoTreinoPro {
  id: string;
  nome: string;
  diasSemana: string | null;
  ordem: number;
  observacoes: string | null;
  itens: ItemTreinoPro[];
}

export interface PlanoTreinoPro {
  id: string;
  pacienteId: string;
  profissionalNome: string;
  nome: string;
  objetivo: string | null;
  dataInicio: string;
  dataFim: string | null;
  status: string;
  observacoes: string | null;
  versao: number;
  sessoes: SessaoTreinoPro[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
}
