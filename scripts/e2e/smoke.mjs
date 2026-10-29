// Teste de fumaca do app: entra como paciente e como profissional e abre as telas principais.
// Falha se alguma tela quebrar (erro de JavaScript, API com 5xx ou o aviso "Nao deu para carregar").
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
  const rotas = ['/pro', '/pro/pacientes', '/pro/mensagens', '/pro/agenda', '/pro/central', '/pro/biblioteca', '/pro/anamnese-exames', '/pro/gestao', '/pro/avisos', '/pro/perfil', '/pro/conta', `/pro/pacientes/${id}`, `/pro/pacientes/${id}/arquivos`];
  const dietas = await api(t, `/api/pacientes/${id}/planos-alimentares`).catch(() => []);
  if (dietas[0]?.id) rotas.push(`/pro/pacientes/${id}/dieta/${dietas[0].id}`);
  const treinos = await api(t, `/api/pacientes/${id}/treinos`).catch(() => []);
  const listaTreinos = Array.isArray(treinos) ? treinos : treinos.itens ?? [];
  if (listaTreinos[0]?.id) rotas.push(`/pro/pacientes/${id}/treino/${listaTreinos[0].id}`);
  return rotas;
}

const PUBLICAS = ['/', '/recursos', '/vantagens', '/sobre', '/entrar'];
const DO_ADMIN = ['/admin', '/admin?aba=clinica', '/admin/equipe', '/admin/pacientes', '/admin/atividade', '/admin/clinica', '/admin/perfil', '/admin/conta'];
const DO_PACIENTE = ['/hoje', '/treino', '/dieta', '/progresso', '/perfil', '/conta', '/arquivos', '/cuidados', '/diario', '/jornada', '/explorar', '/chat'];

const falhas = [];
const browser = await chromium.launch();

async function percorrer(nome, rotas, login) {
  const ctx = await browser.newContext({ viewport: { width: 390, height: 844 }, serviceWorkers: 'block' });
  const page = await ctx.newPage();
  let atual = '(inicio)';
  page.on('pageerror', (e) => falhas.push(`${nome} ${atual}: erro de JavaScript: ${e.message}`));
  page.on('response', (r) => {
    if (r.url().includes('/api/') && r.status() >= 500) falhas.push(`${nome} ${atual}: ${r.status()} em ${new URL(r.url()).pathname}`);
  });

  if (login) {
    atual = '/entrar';
    await page.goto(base + '/entrar');
    await page.fill('input[type=email]', login.email);
    await page.fill('input[type=password]', login.senha);
    await page.keyboard.press('Enter');
    await page.waitForURL((u) => !u.pathname.startsWith('/entrar'), { timeout: 20000 });
  }

  for (const rota of rotas) {
    atual = rota;
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
    if (!ok) await page.screenshot({ path: `${pastaFotos}/${nome}${rota.replace(/[^a-z0-9]+/gi, '_')}.png`, fullPage: true });
    console.log(`${ok ? 'ok  ' : 'FALHOU'} ${nome} ${rota}`);
  }
  await ctx.close();
}

try {
  await percorrer('publico', PUBLICAS, null);
  await percorrer('paciente', DO_PACIENTE, PACIENTE);
  await percorrer('profissional', await rotasDoProfissional(), ADMIN);
  await percorrer('admin', DO_ADMIN, ADMIN);
} finally {
  await browser.close();
}

if (falhas.length) {
  console.error(`\n${falhas.length} problema(s):`);
  for (const f of falhas) console.error(' - ' + f);
  process.exit(1);
}
console.log('\nTodas as telas abriram sem erro.');
