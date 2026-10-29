// Popula um ambiente LOCAL de desenvolvimento com dados de demonstracao realistas.
// Uso: node scripts/dev/seed-demo.mjs [baseUrl]
// Nunca rode contra producao: o script recusa qualquer host que nao seja localhost.

const base = process.argv[2] ?? 'http://127.0.0.1:5080';
if (!/^https?:\/\/(127\.0\.0\.1|localhost)(:\d+)?$/.test(base)) {
  console.error('Recusado: o seed so roda contra localhost.');
  process.exit(1);
}

const ADMIN = { email: 'admin@healthplatform.local', senha: 'ChangeMe_123!' };
const SENHA_PACIENTE = 'Paciente_123!';

async function api(token, method, path, body) {
  const res = await fetch(base + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  const data = text ? JSON.parse(text) : null;
  if (!res.ok) throw new Error(`${method} ${path} -> ${res.status} ${text.slice(0, 300)}`);
  return data;
}

const login = async (email, senha) => (await api(null, 'POST', '/api/auth/login', { email, senha })).accessToken;
const aceitarTermos = (t) => api(t, 'POST', '/api/configuracoes/minha-conta/consentimentos/aceitar', { aceitarTermos: true, aceitarPoliticaPrivacidade: true });

const iso = (d) => d.toISOString().slice(0, 10);
const diasAtras = (n) => { const d = new Date(); d.setUTCDate(d.getUTCDate() - n); return d; };

// Valores aproximados por 100 g, baseados em tabelas publicas de composicao (TACO).
const ALIMENTOS = [
  ['Arroz branco cozido', 'Cereais', 128, 2.5, 28.1, 0.2, 1.6],
  ['Arroz integral cozido', 'Cereais', 124, 2.6, 25.8, 1.0, 2.7],
  ['Feijão carioca cozido', 'Leguminosas', 76, 4.8, 13.6, 0.5, 8.5],
  ['Peito de frango grelhado', 'Carnes', 159, 32.0, 0, 2.5, 0],
  ['Patinho moído refogado', 'Carnes', 219, 35.9, 0, 7.3, 0],
  ['Tilápia grelhada', 'Pescados', 128, 26.0, 0, 2.7, 0],
  ['Ovo de galinha cozido', 'Ovos', 146, 13.3, 0.6, 9.5, 0],
  ['Clara de ovo cozida', 'Ovos', 59, 13.4, 0, 0.1, 0],
  ['Batata-doce cozida', 'Tubérculos', 77, 0.6, 18.4, 0.1, 2.2],
  ['Mandioca cozida', 'Tubérculos', 125, 0.6, 30.1, 0.3, 1.6],
  ['Pão francês', 'Panificados', 300, 8.0, 58.6, 3.1, 2.3],
  ['Pão integral', 'Panificados', 253, 9.4, 49.9, 3.7, 6.9],
  ['Aveia em flocos', 'Cereais', 394, 13.9, 66.6, 8.5, 9.1],
  ['Tapioca (goma)', 'Cereais', 240, 0, 59.0, 0, 0],
  ['Banana prata', 'Frutas', 98, 1.3, 26.0, 0.1, 2.0],
  ['Maçã fuji', 'Frutas', 56, 0.3, 15.2, 0, 1.3],
  ['Mamão papaia', 'Frutas', 40, 0.5, 10.4, 0.1, 1.0],
  ['Morango', 'Frutas', 30, 0.9, 6.8, 0.3, 1.7],
  ['Iogurte natural desnatado', 'Laticínios', 41, 3.8, 5.8, 0.3, 0],
  ['Queijo minas frescal', 'Laticínios', 264, 17.4, 3.2, 20.2, 0],
  ['Leite desnatado', 'Laticínios', 35, 3.4, 5.0, 0.1, 0],
  ['Whey protein concentrado', 'Suplementos', 400, 80.0, 8.0, 6.0, 0],
  ['Pasta de amendoim', 'Oleaginosas', 588, 25.0, 20.0, 50.0, 6.0],
  ['Castanha-do-pará', 'Oleaginosas', 643, 14.5, 15.1, 63.5, 7.9],
  ['Azeite de oliva', 'Óleos', 884, 0, 0, 100, 0],
  ['Brócolis cozido', 'Hortaliças', 25, 2.1, 4.4, 0.5, 3.4],
  ['Alface crespa', 'Hortaliças', 11, 1.3, 1.7, 0.2, 1.8],
  ['Tomate', 'Hortaliças', 15, 1.1, 3.1, 0.2, 1.2],
  ['Cenoura crua', 'Hortaliças', 34, 1.3, 7.7, 0.2, 3.2],
  ['Abacate', 'Frutas', 96, 1.2, 6.0, 8.4, 6.3],
  ['Granola sem açúcar', 'Cereais', 420, 10.0, 64.0, 14.0, 8.0],
  ['Macarrão integral cozido', 'Cereais', 124, 5.3, 26.5, 0.5, 3.9],
];

const PACIENTES = [
  { nome: 'Marina Albuquerque', sexo: 'Feminino', nasc: '1994-03-12', email: 'marina.demo@aesyn.local', prof: 'Advogada', tags: 'Hipertrofia,Corrida', completo: true },
  { nome: 'Rafael Tavares', sexo: 'Masculino', nasc: '1988-11-02', email: 'rafael.demo@aesyn.local', prof: 'Engenheiro', tags: 'Emagrecimento', completo: true },
  { nome: 'Beatriz Lemos', sexo: 'Feminino', nasc: '2001-07-21', email: 'beatriz.demo@aesyn.local', prof: 'Estudante', tags: 'Performance,Natação', completo: false },
  { nome: 'Carlos Menezes', sexo: 'Masculino', nasc: '1975-01-30', email: 'carlos.demo@aesyn.local', prof: 'Empresário', tags: 'Saúde metabólica', completo: false },
  { nome: 'Juliana Prado', sexo: 'Feminino', nasc: '1990-09-08', email: 'juliana.demo@aesyn.local', prof: 'Designer', tags: 'Retorno ao treino', completo: false },
  { nome: 'Thiago Ramos', sexo: 'Masculino', nasc: '1997-05-17', email: 'thiago.demo@aesyn.local', prof: 'Professor', tags: 'Hipertrofia', completo: false },
];

async function main() {
  let admin = await login(ADMIN.email, ADMIN.senha);
  await aceitarTermos(admin);
  await api(admin, 'PUT', '/api/profissionais/me', { nome: 'Dr. André Schmidt', registroProfissional: 'CRM-SC 00000', especialidade: 'Medicina do Esporte e Nutrologia' });

  await api(admin, 'POST', '/api/exercicios/catalogo-base');
  const exercicios = await api(admin, 'GET', '/api/exercicios?incluirInativos=false');
  const listaEx = Array.isArray(exercicios) ? exercicios : exercicios.itens ?? exercicios.items ?? [];
  const ex = (trecho) => {
    const e = listaEx.find((x) => x.nome.toLowerCase() === trecho.toLowerCase());
    if (!e) throw new Error('Exercicio nao encontrado no catalogo base: ' + trecho);
    return e.id;
  };

  const existentes = await api(admin, 'GET', '/api/alimentos');
  const listaAl = Array.isArray(existentes) ? existentes : existentes.itens ?? existentes.items ?? [];
  const alimentos = new Map(listaAl.map((a) => [a.nome, a.id]));
  for (const [nome, categoria, kcal, p, c, g, f] of ALIMENTOS) {
    if (alimentos.has(nome)) continue;
    const a = await api(admin, 'POST', '/api/alimentos', { nome, categoria, caloriasPor100g: kcal, proteinasPor100g: p, carboidratosPor100g: c, gordurasPor100g: g, fibrasPor100g: f });
    alimentos.set(nome, a.id);
  }
  const al = (nome) => alimentos.get(nome);
  const item = (nome, gramas, unidade = 'g', quantidade = gramas, subs = []) => ({
    alimentoId: al(nome), quantidade, unidade, quantidadeGramas: gramas, observacao: null,
    substituicoes: subs.map(([n, gr]) => ({ alimentoId: al(n), quantidade: gr, unidade: 'g', quantidadeGramas: gr, observacao: null })),
  });

  const lista = await api(admin, 'GET', '/api/pacientes?pageSize=100');
  const pacientesExistentes = lista.itens ?? lista.items ?? lista;

  // Medicos so veem os pacientes da propria equipe: o Dr. Andre entra na equipe de todos.
  const profissionais = await api(admin, 'GET', '/api/admin/profissionais');
  const andre = profissionais.find((x) => x.usuarioId && x.nome === 'Dr. André Schmidt');

  for (const p of PACIENTES) {
    let pac = pacientesExistentes.find((x) => x.nome === p.nome);
    if (!pac) {
      pac = await api(admin, 'POST', '/api/pacientes', { nome: p.nome, cpf: null, dataNascimento: p.nasc, sexo: p.sexo, telefone: '(48) 99999-0000', email: p.email, profissao: p.prof });
      await api(admin, 'PATCH', `/api/pacientes/${pac.id}/tags`, { tags: p.tags.split(',') }).catch(() => {});
    }
    if (andre) await api(admin, 'PUT', `/api/admin/pacientes/${pac.id}/equipe`, { profissionalIds: [andre.id] });
    if (!p.completo) continue;

    const planos = await api(admin, 'GET', `/api/pacientes/${pac.id}/planos-alimentares`);
    if (!planos.length) {
      await api(admin, 'POST', `/api/pacientes/${pac.id}/planos-alimentares`, {
        nome: p.sexo === 'Feminino' ? 'Plano hipertrofia — fase 1' : 'Plano déficit moderado',
        dataInicio: iso(diasAtras(21)), dataFim: null, status: 'Ativo',
        observacoes: 'Beber 35 ml de água por kg ao longo do dia. Ajustar sal nos dias de treino longo.',
        metaCalorias: p.sexo === 'Feminino' ? 2100 : 2200, metaProteinasG: p.sexo === 'Feminino' ? 130 : 180,
        metaCarboidratosG: p.sexo === 'Feminino' ? 240 : 190, metaGordurasG: 65, metaFibrasG: 30,
        refeicoes: [
          { nome: 'Café da manhã', horario: '07:00:00', ordem: 1, observacoes: null, metaCalorias: null, metaProteinasG: null, metaCarboidratosG: null, metaGordurasG: null, metaFibrasG: null,
            itens: [item('Ovo de galinha cozido', 100, 'unidade', 2), item('Pão integral', 50, 'fatia', 2, [['Tapioca (goma)', 40]]), item('Mamão papaia', 150, 'g', 150, [['Morango', 200], ['Banana prata', 80]])] },
          { nome: 'Lanche da manhã', horario: '10:00:00', ordem: 2, observacoes: null, metaCalorias: null, metaProteinasG: null, metaCarboidratosG: null, metaGordurasG: null, metaFibrasG: null,
            itens: [item('Iogurte natural desnatado', 170), item('Granola sem açúcar', 30), item('Castanha-do-pará', 10, 'unidade', 2)] },
          { nome: 'Almoço', horario: '12:30:00', ordem: 3, observacoes: 'Salada à vontade.', metaCalorias: null, metaProteinasG: null, metaCarboidratosG: null, metaGordurasG: null, metaFibrasG: null,
            itens: [item('Arroz branco cozido', 150, 'g', 150, [['Batata-doce cozida', 220], ['Macarrão integral cozido', 150]]), item('Feijão carioca cozido', 100), item('Peito de frango grelhado', 150, 'g', 150, [['Tilápia grelhada', 180], ['Patinho moído refogado', 130]]), item('Brócolis cozido', 80), item('Azeite de oliva', 8, 'colher', 1)] },
          { nome: 'Pré-treino', horario: '16:30:00', ordem: 4, observacoes: '60 a 90 minutos antes do treino.', metaCalorias: null, metaProteinasG: null, metaCarboidratosG: null, metaGordurasG: null, metaFibrasG: null,
            itens: [item('Banana prata', 100, 'unidade', 1), item('Aveia em flocos', 30), item('Pasta de amendoim', 15, 'colher', 1)] },
          { nome: 'Pós-treino', horario: '19:00:00', ordem: 5, observacoes: null, metaCalorias: null, metaProteinasG: null, metaCarboidratosG: null, metaGordurasG: null, metaFibrasG: null,
            itens: [item('Whey protein concentrado', 30, 'scoop', 1), item('Leite desnatado', 200, 'ml', 200)] },
          { nome: 'Jantar', horario: '20:30:00', ordem: 6, observacoes: null, metaCalorias: null, metaProteinasG: null, metaCarboidratosG: null, metaGordurasG: null, metaFibrasG: null,
            itens: [item('Tilápia grelhada', 150), item('Mandioca cozida', 150, 'g', 150, [['Batata-doce cozida', 180]]), item('Alface crespa', 50), item('Tomate', 80), item('Abacate', 60)] },
        ],
        suplementos: [],
      });
    }

    const treinos = await api(admin, 'GET', `/api/pacientes/${pac.id}/treinos`);
    if (!treinos.length) {
      const it = (nome, ordem, series, reps, carga, desc, extra = {}) => ({ exercicioId: ex(nome), ordem, series, repeticoes: reps, carga, unidadeCarga: carga ? 'kg' : null, descansoSegundos: desc, tempoSegundos: null, observacoes: null, rirAlvo: 2, cadencia: '3-0-1', ...extra });
      await api(admin, 'POST', `/api/pacientes/${pac.id}/treinos`, {
        nome: 'Bloco de força — semanas 1 a 6', objetivo: 'Hipertrofia com ênfase em membros inferiores',
        dataInicio: iso(diasAtras(21)), dataFim: null, status: 'Ativo', observacoes: 'Aquecimento articular de 8 minutos antes de cada sessão.',
        sessoes: [
          { nome: 'A — Inferiores', diasSemana: 'Seg, Qui', ordem: 1, observacoes: null, itens: [
            it('Agachamento livre', 1, 4, '8-10', 40, 120), it('Leg press 45°', 2, 4, '10-12', 120, 90), it('Stiff com halteres', 3, 3, '10', 30, 90), it('Cadeira extensora', 4, 3, '12-15', 35, 60), it('Panturrilha em pé', 5, 4, '15', 40, 45)] },
          { nome: 'B — Superiores', diasSemana: 'Ter, Sex', ordem: 2, observacoes: null, itens: [
            it('Supino reto com barra', 1, 4, '8-10', 30, 120), it('Remada curvada com barra', 2, 4, '10', 35, 90), it('Desenvolvimento com halteres', 3, 3, '10-12', 12, 90), it('Puxada alta pronada', 4, 3, '10-12', 40, 90), it('Rosca direta com barra', 5, 3, '12', 10, 60)] },
          { nome: 'C — Core e condicionamento', diasSemana: 'Qua, Sáb', ordem: 3, observacoes: 'Circuito: 3 voltas.', itens: [
            it('Prancha frontal', 1, 3, '40s', null, 45), it('Dead bug', 2, 3, '15', null, 45), it('Afundo com halteres', 3, 3, '12', 10, 60)] },
        ],
      });
    }

    const acesso = await api(admin, 'GET', `/api/pacientes/${pac.id}/acesso`);
    if (!acesso.possuiAcesso) {
      const convite = await api(admin, 'POST', `/api/pacientes/${pac.id}/acesso`, { email: p.email });
      await api(null, 'POST', '/api/auth/paciente/ativar', { email: p.email, token: convite.activationToken, senha: SENHA_PACIENTE });
      const tp = await login(p.email, SENHA_PACIENTE);
      await aceitarTermos(tp);
      await api(tp, 'POST', '/api/configuracoes/minha-conta/onboarding-paciente/concluir').catch(() => {});
    }

    const tp = await login(p.email, SENHA_PACIENTE);
    const historico = await api(tp, 'GET', '/api/portal/me/treinos/historico');
    if (!historico.total) {

      for (let d = 13; d >= 1; d--) {
        await api(tp, 'POST', '/api/portal/me/prontidao', {
          data: iso(diasAtras(d)), sonoHoras: 6 + (d % 3), sonoQualidade: 6 + (d % 4), energiaNivel: 5 + (d % 5),
          dorNivel: d % 4, disposicaoNivel: 6 + (d % 4), recuperacaoNivel: 5 + (d % 5),
        }).catch(() => {});
      }

      const meuTreino = await api(tp, 'GET', '/api/portal/me/treino/todos');
      const sessoes = meuTreino.planos?.[0]?.sessoes ?? [];
      for (const [i, d] of [12, 10, 9, 7, 5, 3, 2].entries()) {
        const s = sessoes[i % sessoes.length];
        if (!s) break;
        const inicio = diasAtras(d); inicio.setUTCHours(21, 0, 0, 0);
        const fim = new Date(inicio.getTime() + 55 * 60000);
        await api(tp, 'POST', '/api/portal/me/treinos/execucoes', {
          sessaoTreinoId: s.id, dataHoraInicioUtc: inicio.toISOString(), dataHoraFimUtc: fim.toISOString(), duracaoMinutos: 55,
          esforcoPercebido: 6 + (i % 3), observacoes: null,
          itens: (s.itens ?? []).map((x) => ({ itemTreinoId: x.id, seriesRealizadas: x.series, repeticoesRealizadas: String(x.repeticoes).split('-')[0], cargaRealizada: x.carga ? Number(x.carga) + (i > 3 ? 2.5 : 0) : null, unidadeCarga: x.unidadeCarga, esforcoPercebido: 7, concluido: true, observacoes: null, exercicioAlternativoId: null, motivoAlternativa: null, rirRealizado: 2 })),
        }).catch((e) => console.warn('execucao:', e.message));
      }
    }
  }
  console.log('Seed concluido.');
  console.log(`Profissional: ${ADMIN.email} / ${ADMIN.senha}`);
  console.log(`Pacientes com portal: ${PACIENTES.filter((p) => p.completo).map((p) => p.email).join(', ')} / ${SENHA_PACIENTE}`);
}

main().catch((e) => { console.error(e); process.exit(1); });
