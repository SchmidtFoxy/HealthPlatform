// Teste de fumaca do app: entra como paciente e como profissional, abre as telas principais e
// executa as acoes do dia a dia (check-in, agua, treino, diario, chat, novo paciente, nova dieta).
// Falha se algo quebrar (erro de JavaScript, API com 5xx, aviso "Nao deu para carregar" ou acao sem efeito).
// Uso (com a API local no ar e o seed de demonstracao aplicado): node scripts/e2e/smoke.mjs [baseUrl]
// Precisa do pacote playwright com o Chromium instalado. Nunca aponte para producao.

import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';

const base = process.argv[2] ?? 'http://127.0.0.1:5080';
if (!/^https?:\/\/(127\.0\.0\.1|localhost)(:\d+)?$/.test(base)) {
  console.error('Recusado: o teste so roda contra localhost.');
  process.exit(1);
}
const pastaFotos = process.env.SMOKE_SHOTS ?? 'smoke-shots';
mkdirSync(pastaFotos, { recursive: true });

const ADMIN = { email: 'admin@healthplatform.local', senha: 'ChangeMe_123!' };
const PACIENTE = { email: 'marina.demo@aesyn.local', senha: 'Paciente_123!' };
// As acoes usam o outro paciente do seed, para nao depender do que o percurso das telas fez.
const PACIENTE_ACOES = { email: 'rafael.demo@aesyn.local', senha: 'Paciente_123!' };

async function api(token, path) {
  const r = await fetch(base + path, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
  if (!r.ok) throw new Error(`GET ${path} -> ${r.status}`);
  return r.json();
}
async function tokenDe({ email, senha }) {
  const r = await fetch(base + '/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, senha }) });
  if (!r.ok) throw new Error(`login ${email} -> ${r.status}`);
  return (await r.json()).accessToken;
}

// Rotas da ficha do paciente usam ids reais do seed.
async function rotasDoProfissional() {
  const t = await tokenDe(ADMIN);
  const lista = await api(t, '/api/pacientes?busca=Marina&pagina=1&tamanhoPagina=5');
  const id = lista.itens?.[0]?.id;
  if (!id) throw new Error('Seed sem a paciente Marina.');
  const rotas = ['/pro', '/pro/pacientes', '/pro/mensagens', '/pro/agenda', '/pro/central', '/pro/biblioteca', '/pro/anamnese-exames', '/pro/gestao', '/pro/avisos', '/pro/perfil', '/pro/conta', `/pro/pacientes/${id}`, `/pro/pacientes/${id}?aba=evolucao`, `/pro/pacientes/${id}/arquivos`];
  const dietas = await api(t, `/api/pacientes/${id}/planos-alimentares`).catch(() => []);
  if (dietas[0]?.id) rotas.push(`/pro/pacientes/${id}/dieta/${dietas[0].id}`);
  const treinos = await api(t, `/api/pacientes/${id}/treinos`).catch(() => []);
  const listaTreinos = Array.isArray(treinos) ? treinos : treinos.itens ?? [];
  if (listaTreinos[0]?.id) rotas.push(`/pro/pacientes/${id}/treino/${listaTreinos[0].id}`);
  return rotas;
}

const PUBLICAS = ['/', '/recursos', '/vantagens', '/sobre', '/entrar'];
const DO_ADMIN = ['/admin', '/admin?aba=clinica', '/admin/usuarios', '/admin/usuarios?aba=pacientes', '/admin/usuarios?aba=pacientes&equipe=sem', '/admin/atividade', '/admin/clinica', '/admin/perfil', '/admin/conta'];
const DO_PACIENTE = ['/hoje', '/treino', '/dieta', '/progresso', '/perfil', '/conta', '/arquivos', '/cuidados', '/diario', '/jornada', '/explorar', '/chat'];

const falhas = [];
const browser = await chromium.launch();

async function abrirSessao(nome, login) {
  const ctx = await browser.newContext({ viewport: { width: 390, height: 844 }, serviceWorkers: 'block' });
  const page = await ctx.newPage();
  const estado = { atual: '(inicio)' };
  page.on('pageerror', (e) => falhas.push(`${nome} ${estado.atual}: erro de JavaScript: ${e.message}`));
  page.on('response', (r) => {
    if (r.url().includes('/api/') && r.status() >= 500) falhas.push(`${nome} ${estado.atual}: ${r.status()} em ${new URL(r.url()).pathname}`);
  });
  if (login) {
    estado.atual = '/entrar';
    await page.goto(base + '/entrar');
    await page.fill('input[type=email]', login.email);
    await page.fill('input[type=password]', login.senha);
    await page.keyboard.press('Enter');
    await page.waitForURL((u) => !u.pathname.startsWith('/entrar'), { timeout: 20000 });
  }
  return { ctx, page, estado };
}

const fotografar = (page, nome, passo) => page.screenshot({ path: `${pastaFotos}/${nome}_${passo.replace(/[^a-z0-9]+/gi, '_')}.png`, fullPage: true }).catch(() => {});

async function percorrer(nome, rotas, login) {
  const { ctx, page, estado } = await abrirSessao(nome, login);
  for (const rota of rotas) {
    estado.atual = rota;
    const antes = falhas.length;
    await page.goto(base + rota, { waitUntil: 'networkidle', timeout: 30000 }).catch((e) => falhas.push(`${nome} ${rota}: nao carregou (${e.message.split('\n')[0]})`));
    await page.waitForTimeout(500);
    const caminho = new URL(page.url()).pathname;
    if (login && caminho.startsWith('/entrar')) falhas.push(`${nome} ${rota}: caiu na tela de login`);
    const erroNaTela = await page.getByText('Não deu para carregar.').count();
    if (erroNaTela) falhas.push(`${nome} ${rota}: aviso "Não deu para carregar" na tela`);
    const texto = (await page.locator('#root').innerText().catch(() => '')).trim();
    if (!texto) falhas.push(`${nome} ${rota}: tela em branco`);
    const ok = falhas.length === antes;
    if (!ok) await fotografar(page, nome, rota);
    console.log(`${ok ? 'ok  ' : 'FALHOU'} ${nome} ${rota}`);
  }
  await ctx.close();
}

// Cada passo roda isolado: se um falhar, os seguintes continuam e o relatorio mostra todos.
async function acoes(nome, login, passos) {
  const { ctx, page, estado } = await abrirSessao(nome, login);
  for (const [passo, fazer] of passos) {
    estado.atual = passo;
    const antes = falhas.length;
    try {
      await fazer(page);
    } catch (e) {
      falhas.push(`${nome} ${passo}: ${e.message.split('\n')[0]}`);
    }
    const ok = falhas.length === antes;
    if (!ok) await fotografar(page, nome, passo);
    console.log(`${ok ? 'ok  ' : 'FALHOU'} ${nome} acao: ${passo}`);
  }
  await ctx.close();
}

const ir = (page, rota) => page.goto(base + rota, { waitUntil: 'networkidle', timeout: 30000 });
const aviso = (page, texto) => page.getByText(texto).first().waitFor({ timeout: 10000 });
const marca = `E2E ${Date.now().toString(36)}`;

// O texto digitado tambem aparece na tela antes de enviar, entao o que vale e a resposta da API.
async function enviarMensagem(page) {
  const [r] = await Promise.all([
    page.waitForResponse((x) => x.request().method() === 'POST' && /\/api\/chat\/.+\/mensagens$/.test(new URL(x.url()).pathname), { timeout: 15000 }),
    page.getByRole('button', { name: 'Enviar' }).click(),
  ]);
  if (!r.ok()) throw new Error(`envio da mensagem respondeu ${r.status()}`);
}

const PASSOS_PACIENTE = [
  ['check-in do dia', async (page) => {
    await ir(page, '/hoje');
    await page.getByRole('button', { name: /Como você acordou hoje\?|prontidão/ }).first().click();
    await page.getByRole('button', { name: 'Salvar check-in' }).click();
    await aviso(page, 'Check-in registrado');
  }],
  ['registrar água', async (page) => {
    await ir(page, '/hoje');
    const agua = page.locator('#agua');
    const antes = await agua.innerText();
    await agua.getByRole('button', { name: /\+ 250 ml/ }).click();
    await page.waitForFunction((t) => document.getElementById('agua')?.innerText !== t, antes, { timeout: 10000 });
  }],
  ['treino: série e finalizar', async (page) => {
    await ir(page, '/treino');
    // Sessao ja feita hoje mostra "Treinar de novo" no lugar de "Começar".
    await page.getByRole('button', { name: /^(Começar( [A-Z])?|Treinar de novo)$/ }).first().click();
    await page.waitForURL(/\/treino\/[0-9a-f-]{36}/, { timeout: 15000 });
    await page.getByRole('button', { name: 'Concluir série 1' }).click();
    await page.getByRole('button', { name: 'Finalizar' }).click();
    await page.getByRole('button', { name: 'Salvar treino' }).click();
    await page.waitForFunction(() => !document.body.innerText.includes('Salvar treino'), null, { timeout: 15000 });
  }],
  ['diário', async (page) => {
    await ir(page, '/diario');
    await page.getByRole('button', { name: 'Registrar', exact: true }).click();
    await page.getByPlaceholder('Ex.: 7,5').fill('7,5');
    await page.getByRole('button', { name: 'Salvar no diário' }).click();
    await aviso(page, 'Registrado no diário.');
  }],
  ['mensagem para o profissional', async (page) => {
    await ir(page, '/chat');
    await page.getByPlaceholder('Escreva sua mensagem').fill(`Mensagem ${marca}`);
    await enviarMensagem(page);
  }],
];

const PASSOS_PROFISSIONAL = [
  ['cadastrar paciente', async (page) => {
    await ir(page, '/pro/pacientes');
    await page.getByRole('button', { name: 'Novo', exact: true }).click();
    const d = page.getByRole('dialog');
    await d.getByRole('textbox').first().fill(`Paciente ${marca}`);
    await d.locator('input[type=date]').fill('1990-05-10');
    await d.locator('select').selectOption({ index: 1 });
    await d.getByRole('button', { name: 'Cadastrar' }).click();
    await page.waitForURL(/\/pro\/pacientes\/[0-9a-f-]{36}$/, { timeout: 15000 });
    await page.getByText(`Paciente ${marca}`).first().waitFor({ timeout: 10000 });
  }],
  ['criar dieta com alimento', async (page) => {
    const id = new URL(page.url()).pathname.split('/').pop();
    await ir(page, `/pro/pacientes/${id}/dieta/novo`);
    await page.getByRole('textbox', { name: 'Nome do plano' }).fill(`Dieta ${marca}`);
    await page.getByRole('textbox', { name: 'Nome da refeição' }).first().fill('Café da manhã');
    await page.getByRole('button', { name: 'Adicionar alimento' }).first().click();
    await page.getByRole('dialog').getByRole('button', { name: /^Aveia em flocos/ }).click();
    await page.keyboard.press('Escape').catch(() => {});
    await page.getByRole('button', { name: 'Salvar', exact: true }).click();
    await aviso(page, 'Dieta salva');
    await page.waitForURL(/\/dieta\/[0-9a-f-]{36}$/, { timeout: 15000 });
    await ir(page, `/pro/pacientes/${id}?aba=dieta`);
    await page.getByText(`Dieta ${marca}`).first().waitFor({ timeout: 10000 });
  }],
  ['responder no chat', async (page) => {
    await ir(page, '/pro/mensagens');
    await page.getByText('Rafael Tavares').first().click();
    await page.waitForURL(/\/pro\/mensagens\/[0-9a-f-]{36}/, { timeout: 15000 });
    await page.getByText(`Mensagem ${marca}`).last().waitFor({ timeout: 10000 });
    await page.getByPlaceholder('Responder').fill(`Resposta ${marca}`);
    await enviarMensagem(page);
  }],
];

try {
  await percorrer('publico', PUBLICAS, null);
  await percorrer('paciente', DO_PACIENTE, PACIENTE);
  await percorrer('profissional', await rotasDoProfissional(), ADMIN);
  await percorrer('admin', DO_ADMIN, ADMIN);
  await acoes('paciente', PACIENTE_ACOES, PASSOS_PACIENTE);
  await acoes('profissional', ADMIN, PASSOS_PROFISSIONAL);
} finally {
  await browser.close();
}

if (falhas.length) {
  console.error(`\n${falhas.length} problema(s):`);
  for (const f of falhas) console.error(' - ' + f);
  process.exit(1);
}
console.log('\nTodas as telas e ações funcionaram.');
