const state={token:localStorage.getItem('hp_token'),user:JSON.parse(localStorage.getItem('hp_user')||'null'),view:'dashboard',offset:-new Date().getTimezoneOffset(),selectedDate:new Date(),patientId:null,patientTab:'resumo'};
const $=s=>document.querySelector(s), $$=s=>[...document.querySelectorAll(s)], content=$('#content');
const esc=(v='')=>String(v??'').replace(/[&<>'"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));
const initials=(n='')=>n.split(/\s+/).filter(Boolean).slice(0,2).map(x=>x[0]).join('').toUpperCase()||'HP';
const todayISO=(d=new Date())=>`${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
const fmtDate=v=>v?new Intl.DateTimeFormat('pt-BR',{dateStyle:'medium'}).format(new Date(v)):'—';
const fmtDateTime=v=>v?new Intl.DateTimeFormat('pt-BR',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'}).format(new Date(v)):'—';
const fmtTime=v=>v?String(v).substring(11,16):'--:--';
const num=(v,d=1)=>v==null?'—':Number(v).toLocaleString('pt-BR',{maximumFractionDigits:d});
const localDateTimeValue=(d=new Date())=>{const x=new Date(d.getTime()-d.getTimezoneOffset()*60000);return x.toISOString().slice(0,16)};
const val=(form,name)=>{const e=form.elements[name];return e&&e.value!==''?e.value:null};
const dec=(form,name)=>{const v=val(form,name);return v==null?null:Number(String(v).replace(',','.'))};
const integer=(form,name)=>{const v=val(form,name);return v==null?null:Number.parseInt(v,10)};
function toast(message,error=false){const e=$('#toast');e.textContent=message;e.className=`toast show${error?' error':''}`;clearTimeout(window.__toast);window.__toast=setTimeout(()=>e.className='toast',3000)}
async function api(path,options={}){const headers={'Content-Type':'application/json',...(options.headers||{})};if(state.token)headers.Authorization=`Bearer ${state.token}`;const r=await fetch(path,{...options,headers});const t=await r.text();let d=null;try{d=t?JSON.parse(t):null}catch{d=t}if(r.status===401&&path!=='/api/auth/login'){logout();throw new Error('Sua sessão expirou.')}if(!r.ok)throw new Error(d?.message||`Erro HTTP ${r.status}`);return d}
function setLoading(){content.innerHTML='<div class="card"><div class="skeleton" style="width:35%;margin-bottom:18px"></div><div class="skeleton" style="height:180px"></div></div>'}
function showApp(){
  $('#loginView').classList.add('hidden');
  $('#activationView')?.classList.add('hidden');
  const u=state.user||{};
  if(u.tipoUsuario==='Paciente'){
    $('#appView').classList.add('hidden');
    $('#patientAppView').classList.remove('hidden');
    $('#patientUserName').textContent=u.nome||'Paciente';
    $('#patientAvatar').textContent=initials(u.nome);
    loadPatientSection('inicio').catch(e=>{toast(e.message,true);if(String(e.message).includes('sessão'))logout()});
    return;
  }
  $('#patientAppView').classList.add('hidden');
  $('#appView').classList.remove('hidden');
  $('#userName').textContent=u.nome||'Profissional';
  $('#userType').textContent=u.tipoUsuario||'Usuário';
  $('#avatar').textContent=initials(u.nome);
  $$('.admin-only').forEach(x=>x.classList.toggle('hidden',u.tipoUsuario!=='Admin'));
  navigate(state.view);
}
function logout(){
  localStorage.removeItem('hp_token');
  localStorage.removeItem('hp_user');
  state.token=null;state.user=null;
  $('#appView').classList.add('hidden');
  $('#patientAppView')?.classList.add('hidden');
  $('#activationView')?.classList.add('hidden');
  $('#loginView').classList.remove('hidden');
}
$('#loginForm').addEventListener('submit',async e=>{
  e.preventDefault();
  const b=$('#loginButton'),msg=$('#loginMessage');
  msg?.classList.add('hidden');
  if(msg)msg.textContent='';
  b.disabled=true;b.textContent='Entrando...';
  try{
    const d=await api('/api/auth/login',{method:'POST',body:JSON.stringify({email:$('#email').value.trim(),senha:$('#senha').value})});
    state.token=d.accessToken;state.user={nome:d.nome,tipoUsuario:d.tipoUsuario};
    localStorage.setItem('hp_token',state.token);localStorage.setItem('hp_user',JSON.stringify(state.user));
    showApp();
  }catch(x){
    const text=String(x?.message||'Não foi possível entrar.');
    if(msg){
      msg.textContent=text==='Email ou senha invalidos.'
        ? 'E-mail ou senha inválidos. No Render, use a senha configurada em Seed__AdminPassword.'
        : text;
      msg.classList.remove('hidden');
    }
    toast(text,true);
  }finally{b.disabled=false;b.textContent='Entrar'}
});
$('#logoutButton').onclick=logout;$('#menuButton').onclick=()=>$('.sidebar').classList.toggle('open');$$('.nav-item[data-view]').forEach(b=>b.onclick=()=>navigate(b.dataset.view));$$('[data-close-create]').forEach(x=>x.onclick=()=>$('#createPatientModal').classList.add('hidden'));
function closeClinicalAction(){$('#clinicalActionModal').classList.add('hidden');$('#clinicalActionModal').classList.remove('nutrition-modal-open','workout-modal-open');$('#clinicalActionContent').innerHTML=''}
$$('[data-close-clinical]').forEach(x=>x.onclick=closeClinicalAction);
function navigate(view){state.view=view;$$('.nav-item[data-view]').forEach(x=>x.classList.toggle('active',x.dataset.view===view));$('.sidebar').classList.remove('open');const titles={dashboard:['ÁREA PROFISSIONAL','Visão geral'],pacientes:['PRONTUÁRIO','Pacientes'],agenda:['ORGANIZAÇÃO','Agenda'],paciente:['PRONTUÁRIO','Paciente']};$('#pageEyebrow').textContent=titles[view][0];$('#pageTitle').textContent=titles[view][1];setLoading();({dashboard:loadDashboard,pacientes:loadPatients,agenda:loadAgenda,paciente:loadPatient}[view])().catch(e=>{content.innerHTML=`<div class="card empty">${esc(e.message)}</div>`;toast(e.message,true)})}
function stat(label,value,hint){return `<div class="stat-card"><div class="label">${label}</div><div class="value">${value??0}</div><div class="hint">${hint}</div></div>`}
function agendaRow(x){return `<div class="list-row clickable" data-patient="${x.pacienteId}"><div class="time-badge">${fmtTime(x.dataHoraLocal)}</div><div class="row-main"><strong>${esc(x.pacienteNome)}</strong><small>${esc(x.motivo||'Consulta')}</small></div><span class="pill ${esc(x.status)}">${esc(x.status)}</span></div>`}
async function loadDashboard(){const d=await api(`/api/profissional/dashboard?offsetMinutos=${state.offset}`),agenda=d.agendaHoje||[],proximas=d.proximasConsultas||[],atencao=d.pacientesQuePrecisamAtencao||[];content.innerHTML=`<section class="mvp-dashboard-hero"><div><span class="eyebrow">MVP PREVIEW • AMBIENTE DE DEMONSTRAÇÃO</span><h3>Olá, ${esc((d.profissionalNome||'Profissional').split(' ')[0])} 👋</h3><p>Explore os fluxos como se fosse um dia real de atendimento. A ideia desta versão é descobrir o que funciona, o que incomoda e o que ainda está faltando.</p></div><div class="mvp-dashboard-actions"><button class="primary" id="goPatients">+ Novo paciente</button><button class="secondary" id="goAgendaHero">Abrir agenda</button><button class="ghost" id="openMvpGuideHero">Roteiro da demo</button></div></section><div class="stats-grid">${stat('Pacientes ativos',d.pacientesAtivos,'na organização')}${stat('Consultas hoje',d.consultasHoje,`${d.confirmadasHoje} confirmada(s)`)}${stat('Atendidos / 30 dias',d.pacientesAtendidosUltimos30Dias,'pacientes distintos')}${stat('Retornos pendentes',d.retornosPendentes,'sem consulta futura')}${stat('Faltas hoje',d.faltasHoje,'acompanhamento')}</div><div class="dashboard-grid"><div class="stack"><section class="card"><div class="card-head"><h3>Agenda de hoje</h3><button class="ghost" id="goAgenda">Ver agenda →</button></div>${agenda.length?`<div class="list">${agenda.map(agendaRow).join('')}</div>`:'<div class="empty">Nenhuma consulta para hoje.</div>'}</section><section class="card"><div class="card-head"><h3>Próximas consultas</h3><small>${proximas.length} agendada(s)</small></div>${proximas.length?`<div class="list">${proximas.map(agendaRow).join('')}</div>`:'<div class="empty">Nenhuma próxima consulta.</div>'}</section></div><div class="stack"><section class="card"><div class="card-head"><h3>Precisam de atenção</h3><small>acompanhamento</small></div>${atencao.length?atencao.map(x=>`<div class="attention-row clickable" data-patient="${x.pacienteId}"><div><strong>${esc(x.nome)}</strong><small>${x.retornoPendente?'Retorno pendente • ':''}${x.diasSemRegistroDiario>=999?'Sem registros no diário':`${x.diasSemRegistroDiario} dia(s) sem registro`}</small></div><span class="attention-dot"></span></div>`).join(''):'<div class="empty">Nenhuma pendência importante.</div>'}</section><section class="card"><div class="card-head"><h3>Pacientes recentes</h3><small>novos cadastros</small></div>${(d.pacientesRecentes||[]).map(x=>`<div class="list-row clickable" data-patient="${x.pacienteId}"><div class="mini-avatar">${initials(x.nome)}</div><div class="row-main"><strong>${esc(x.nome)}</strong><small>Cadastrado em ${fmtDate(x.dataCadastroUtc)}</small></div><span>›</span></div>`).join('')||'<div class="empty">Sem pacientes recentes.</div>'}</section></div></div>`;$('#goAgenda').onclick=()=>navigate('agenda');$('#goPatients').onclick=openCreatePatient;if($('#goAgendaHero'))$('#goAgendaHero').onclick=()=>navigate('agenda');if($('#openMvpGuideHero'))$('#openMvpGuideHero').onclick=openMvpGuide;$$('[data-patient]').forEach(x=>x.onclick=()=>openPatient(x.dataset.patient))}
async function loadPatients(search=''){const d=await api(`/api/pacientes?pagina=1&tamanhoPagina=50${search?`&busca=${encodeURIComponent(search)}`:''}`);content.innerHTML=`<div class="section-head"><div><h3>Pacientes</h3><p>${d.total} paciente(s) encontrado(s).</p></div><div class="toolbar"><div class="search-wrap"><input id="patientSearch" class="search-input" placeholder="Buscar nome, CPF, e-mail ou telefone" value="${esc(search)}"></div><button class="primary" id="newPatient">+ Novo paciente</button></div></div><div class="table-wrap">${d.itens.length?`<table class="data-table"><thead><tr><th>Paciente</th><th>Contato</th><th>CPF</th><th>Nascimento</th><th>Status</th></tr></thead><tbody>${d.itens.map(p=>`<tr data-patient="${p.id}"><td><div class="person-cell"><div class="mini-avatar">${initials(p.nome)}</div><div><strong>${esc(p.nome)}</strong><div class="muted-mini">${esc(p.profissao||'Profissão não informada')}</div></div></div></td><td>${esc(p.telefone||p.email||'—')}</td><td>${esc(p.cpf||'—')}</td><td>${p.dataNascimento?fmtDate(p.dataNascimento):'—'}</td><td><span class="pill ${p.ativo?'Ativa':'Cancelada'}">${p.ativo?'Ativo':'Inativo'}</span></td></tr>`).join('')}</tbody></table>`:'<div class="empty">Nenhum paciente encontrado.</div>'}</div>`;let timer;$('#patientSearch').oninput=e=>{clearTimeout(timer);timer=setTimeout(()=>loadPatients(e.target.value),300)};$('#newPatient').onclick=openCreatePatient;$$('[data-patient]').forEach(x=>x.onclick=()=>openPatient(x.dataset.patient))}
function openCreatePatient(){$('#createPatientModal').classList.remove('hidden')}
$('#createPatientForm').addEventListener('submit',async e=>{e.preventDefault();const obj=Object.fromEntries(new FormData(e.target).entries());Object.keys(obj).forEach(k=>{if(obj[k]==='')obj[k]=null});try{const p=await api('/api/pacientes',{method:'POST',body:JSON.stringify(obj)});$('#createPatientModal').classList.add('hidden');e.target.reset();toast('Paciente cadastrado com sucesso.');openPatient(p.id)}catch(x){toast(x.message,true)}});
function openPatient(id){state.patientId=id;state.patientTab='resumo';navigate('paciente')}
function tabButton(id,label){return `<button class="patient-tab ${state.patientTab===id?'active':''}" data-tab="${id}">${label}</button>`}
function info(label,value){return `<div class="info-box"><small>${label}</small><strong>${esc(value||'—')}</strong></div>`}
function metric(value,suffix,label){return `<div class="metric"><strong>${value??'—'}${value!=null?suffix:''}</strong><small>${label}</small></div>`}
function sectionEmpty(msg){return `<div class="empty compact">${msg}</div>`}
async function loadPatient(){if(!state.patientId){navigate('pacientes');return}const id=state.patientId;const [p,timeline,portal,resumoClinico,consultas,evolucoes,anamneses,avaliacoes,exames,planos,metas,diario,relatorios,treinos,treinosHistorico]=await Promise.all([api(`/api/pacientes/${id}`),api(`/api/pacientes/${id}/timeline?limite=50`),api(`/api/pacientes/${id}/portal/home`),api(`/api/pacientes/${id}/resumo-clinico`),api(`/api/pacientes/${id}/consultas`),api(`/api/pacientes/${id}/evolucoes`),api(`/api/pacientes/${id}/anamneses`),api(`/api/pacientes/${id}/avaliacoes`),api(`/api/pacientes/${id}/exames`),api(`/api/pacientes/${id}/planos-alimentares`),api(`/api/pacientes/${id}/metas?incluirEncerradas=true`),api(`/api/pacientes/${id}/diario`),api(`/api/pacientes/${id}/relatorios`),api(`/api/pacientes/${id}/treinos`),api(`/api/pacientes/${id}/treinos/historico?dias=90`)]);content.innerHTML=`<div class="patient-page"><div class="patient-page-head"><button class="back-link" id="backPatients">← Pacientes</button><div class="patient-hero page"><div class="big-avatar">${initials(p.nome)}</div><div class="patient-title"><div class="eyebrow">PRONTUÁRIO DIGITAL</div><h2>${esc(p.nome)}</h2><p>${esc(p.profissao||'Paciente')} • ${p.dataNascimento?fmtDate(p.dataNascimento):'Nascimento não informado'}</p></div><div class="patient-head-actions"><span class="pill ${p.ativo?'Ativa':'Cancelada'}">${p.ativo?'Ativo':'Inativo'}</span><button class="secondary" id="patientAccess">Acesso do paciente</button><button class="secondary" id="editPatient">Editar dados</button><button class="primary" id="registerClinical">+ Registrar</button></div></div><div class="patient-info-grid">${info('Telefone',p.telefone)}${info('E-mail',p.email)}${info('CPF',p.cpf)}${info('Sexo',p.sexo)}</div></div><div class="patient-tabs">${tabButton('resumo','Resumo')}${tabButton('consultas',`Consultas ${consultas.length}`)}${tabButton('evolucoes',`Evoluções ${evolucoes.length}`)}${tabButton('anamnese',`Anamnese ${anamneses.length}`)}${tabButton('avaliacoes',`Avaliações ${avaliacoes.length}`)}${tabButton('exames',`Exames ${exames.length}`)}${tabButton('alimentacao',`Plano alimentar ${planos.length}`)}${tabButton('treinos',`Treinos ${treinos.length}`)}${tabButton('metas',`Metas ${metas.length}`)}${tabButton('diario',`Diário ${diario.length}`)}${tabButton('relatorios',`Relatórios ${relatorios.length}`)}${tabButton('timeline','Timeline')}</div><div id="patientTabContent"></div></div>`;$('#backPatients').onclick=()=>navigate('pacientes');$('#registerClinical').onclick=()=>openClinicalActionMenu(p);$('#patientAccess').onclick=()=>openPatientAccess(p);$('#editPatient').onclick=()=>openEditPatientForm(p);const data={p,timeline,portal,resumoClinico,consultas,evolucoes,anamneses,avaliacoes,exames,planos,metas,diario,relatorios,treinos,treinosHistorico};$$('.patient-tab').forEach(b=>b.onclick=()=>{state.patientTab=b.dataset.tab;$$('.patient-tab').forEach(x=>x.classList.toggle('active',x.dataset.tab===state.patientTab));renderPatientTab(data)});renderPatientTab(data)}
function hpClinicalSummaryCard(r){
  if(!r)return '';
  const soap=r.ultimaEvolucao;
  const body=r.ultimaAvaliacao;
  const anam=r.ultimaAnamnese;
  const examCount=r.examesAlterados?.length||0;
  return `<section class="card full-card clinical-summary-card">
    <div class="card-head">
      <div><span class="eyebrow">VISÃO CONSOLIDADA</span><h3>Resumo clínico</h3><small>Leitura rápida do estado atual do prontuário.</small></div>
      <div class="clinical-summary-actions"><button class="ghost" id="clinicalSummaryCopy">Copiar handoff</button><button class="ghost" id="clinicalSummaryPrint">Imprimir</button><button class="ghost" id="clinicalSummaryRefresh">Atualizar</button></div>
    </div>
    <div class="clinical-summary-metrics">
      <div><strong>${r.pendenciasAbertas}</strong><span>Pendências abertas</span></div>
      <div><strong>${r.pendenciasAltaPrioridade}</strong><span>Alta prioridade</span></div>
      <div><strong>${r.metasAtivas}</strong><span>Metas ativas</span></div>
      <div><strong>${r.treinosUltimos30Dias}</strong><span>Treinos / 30 dias</span></div>
      <div><strong>${examCount}</strong><span>Exames alterados</span></div>
    </div>
    <div class="clinical-summary-grid">
      <article><b>Agenda</b>
        <p>${r.ultimaConsulta?`Última: ${fmtDateTime(r.ultimaConsulta.dataHoraUtc)} • ${esc(r.ultimaConsulta.status)}`:'Sem consulta anterior.'}</p>
        <p>${r.proximaConsulta?`Próxima: ${fmtDateTime(r.proximaConsulta.dataHoraUtc)} • ${esc(r.proximaConsulta.motivo||'Consulta')}`:'Sem retorno futuro agendado.'}</p>
      </article>
      <article><b>Última evolução SOAP</b>
        ${soap?`<p><strong>${fmtDateTime(soap.dataHoraUtc)}</strong> • ${esc(soap.profissionalNome)}</p><p>${esc(soap.avaliacao||soap.plano||soap.subjetivo||'Evolução registrada.')}</p>`:'<p>Nenhuma evolução SOAP registrada.</p>'}
      </article>
      <article><b>Última avaliação corporal</b>
        ${body?`<p>${fmtDate(body.dataUtc)}${body.pesoKg!=null?` • ${num(body.pesoKg)} kg`:''}${body.imc!=null?` • IMC ${num(body.imc,2)}`:''}</p><p>${body.percentualGordura!=null?`Gordura ${num(body.percentualGordura)}%`:''}${body.cinturaCm!=null?` • Cintura ${num(body.cinturaCm)} cm`:''}</p>`:'<p>Nenhuma avaliação corporal registrada.</p>'}
      </article>
      <article><b>Anamnese recente</b>
        ${anam?`<p>${fmtDate(anam.dataUtc)}${anam.objetivoAcompanhamento?` • ${esc(anam.objetivoAcompanhamento)}`:''}</p><p>${anam.alergias?`Alergias: ${esc(anam.alergias)}`:'Alergias não registradas.'}</p><p>${anam.medicamentos?`Medicamentos: ${esc(anam.medicamentos)}`:'Medicamentos não registrados.'}</p>`:'<p>Nenhuma anamnese registrada.</p>'}
      </article>
    </div>
    <div class="clinical-summary-exams">
      <div class="record-top"><div><b>Exames fora da referência</b><small>${examCount} resultado(s) recente(s)</small></div></div>
      ${examCount?`<div class="clinical-summary-exam-list">${r.examesAlterados.map(x=>`<div><strong>${esc(x.marcador)}</strong><span>${num(x.valorNumerico,2)} ${esc(x.unidade||'')}</span><em>${esc(x.classificacao)}</em></div>`).join('')}</div>`:'<div class="central-day-empty">Nenhum resultado numérico recente fora da referência registrada.</div>'}
    </div>
  </section>`;
}

function renderPatientTab(d){const box=$('#patientTabContent'),{timeline,portal,resumoClinico,consultas,evolucoes,anamneses,avaliacoes,exames,planos,metas,diario,relatorios}=d,e=portal.evolucaoCorporal||{};if(state.patientTab==='resumo'){const plano=portal.planoAlimentarAtual,prox=portal.proximaConsulta;box.innerHTML=`${hpClinicalSummaryCard(resumoClinico)}<div class="patient-dashboard"><div class="stack"><section class="card"><div class="card-head"><h3>Evolução atual</h3><small>última avaliação</small></div><div class="portal-metrics wide">${metric(num(e.pesoKg),' kg','Peso')}${metric(num(e.imc,2),'','IMC')}${metric(num(e.percentualGordura),'%','Gordura')}${metric(num(e.cinturaCm),' cm','Cintura')}</div>${e.variacaoPesoKg!=null?`<div class="trend-note">Variação de peso: <strong>${e.variacaoPesoKg>0?'+':''}${num(e.variacaoPesoKg)} kg</strong></div>`:''}</section><section class="card"><div class="card-head"><h3>Plano alimentar atual</h3><small>${plano?`${plano.refeicoes} refeições`:'sem plano ativo'}</small></div>${plano?`<div class="feature-title">${esc(plano.nome)}</div><div class="meal-strip">${(plano.rotinaHoje||[]).map(r=>`<div><strong>${r.horario?String(r.horario).slice(0,5):'--:--'}</strong><span>${esc(r.nome)}</span><small>${r.itens} item(ns)</small></div>`).join('')}</div>`:sectionEmpty('Nenhum plano alimentar ativo.')}</section></div><div class="stack"><section class="card"><div class="card-head"><h3>Próxima consulta</h3><small>agenda</small></div>${prox?`<div class="next-appointment"><div class="date-block"><strong>${new Date(prox.dataHoraUtc).getDate()}</strong><span>${new Intl.DateTimeFormat('pt-BR',{month:'short'}).format(new Date(prox.dataHoraUtc))}</span></div><div><strong>${fmtDateTime(prox.dataHoraUtc)}</strong><p>${esc(prox.motivo||'Consulta')} • ${esc(prox.profissionalNome)}</p></div></div>`:sectionEmpty('Nenhuma consulta futura.')}</section><section class="card"><div class="card-head"><h3>Metas de hoje</h3><small>${portal.metasConcluidas}/${portal.metasAtivas}</small></div>${portal.metasHoje?.length?portal.metasHoje.map(m=>`<div class="goal-row"><div><strong>${esc(m.nome)}</strong><small>${num(m.valorHoje)} ${esc(m.unidade||'')} de ${num(m.valorObjetivo)} ${esc(m.unidade||'')}</small></div><div class="goal-progress"><span style="width:${Math.min(100,Number(m.progressoPercentual||0))}%"></span></div><b>${num(m.progressoPercentual,0)}%</b></div>`).join(''):sectionEmpty('Nenhuma meta ativa hoje.')}</section><section class="card"><div class="card-head"><h3>Exames recentes</h3><small>${portal.examesRecentes?.length||0} resultado(s)</small></div>${portal.examesRecentes?.length?portal.examesRecentes.slice(0,5).map(x=>`<div class="lab-row"><div><strong>${esc(x.marcador)}</strong><small>${fmtDate(x.dataColetaUtc)}</small></div><div><b>${x.valorNumerico!=null?num(x.valorNumerico,2):esc(x.valorTexto||'—')} ${esc(x.unidade||'')}</b><span class="pill ${x.classificacao==='DentroDaReferencia'?'Ativa':x.classificacao}">${esc(x.classificacao)}</span></div></div>`).join(''):sectionEmpty('Sem resultados recentes.')}</section></div></div>`;return}
if(state.patientTab==='consultas'){box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Histórico de consultas</h3><small>${consultas.length} registro(s)</small></div>${consultas.length?`<div class="records-grid">${consultas.map(c=>`<article class="record-card"><div class="record-top"><div><span class="eyebrow">${fmtDateTime(c.dataHoraUtc)}</span><h4>${esc(c.motivo||'Consulta')}</h4></div><span class="pill ${esc(c.status)}">${esc(c.status)}</span></div><dl><dt>Queixa</dt><dd>${esc(c.queixaPrincipal||'—')}</dd><dt>Evolução</dt><dd>${esc(c.evolucao||'—')}</dd><dt>Conduta</dt><dd>${esc(c.conduta||'—')}</dd><dt>Orientações</dt><dd>${esc(c.orientacoes||'—')}</dd></dl></article>`).join('')}</div>`:sectionEmpty('Nenhuma consulta registrada.')}</section>`;return}
if(state.patientTab==='evolucoes'){box.innerHTML=`<section class="card full-card"><div class="card-head"><div><h3>Evolução clínica SOAP</h3><small>${evolucoes.length} registro(s)</small></div><button class="primary" id="newEvolutionFromTab">+ Nova evolução</button></div>${evolucoes.length?`<div class="soap-list">${evolucoes.map(x=>`<article class="soap-card"><div class="record-top"><div><span class="eyebrow">${fmtDateTime(x.dataHoraUtc)}</span><h4>Evolução clínica</h4><small>${esc(x.profissionalNome)}${x.consultaDataHoraUtc?` • consulta ${fmtDateTime(x.consultaDataHoraUtc)}`:''}</small></div><button class="secondary edit-evolution" data-id="${x.id}">Editar</button></div><div class="soap-grid"><div><b>S • Subjetivo</b><p>${esc(x.subjetivo||'—')}</p></div><div><b>O • Objetivo</b><p>${esc(x.objetivo||'—')}</p></div><div><b>A • Avaliação</b><p>${esc(x.avaliacao||'—')}</p></div><div><b>P • Plano</b><p>${esc(x.plano||'—')}</p></div></div>${x.observacoes?`<div class="soap-notes"><b>Observações</b><p>${esc(x.observacoes)}</p></div>`:''}</article>`).join('')}</div>`:sectionEmpty('Nenhuma evolução clínica registrada.')}</section>`;$('#newEvolutionFromTab').onclick=()=>openEvolutionForm(d.p);$$('.edit-evolution').forEach(b=>b.onclick=()=>openEvolutionForm(d.p,b.dataset.id));return}
if(state.patientTab==='anamnese'){box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Anamneses</h3><small>${anamneses.length} registro(s)</small></div>${anamneses.length?anamneses.map(a=>`<article class="clinical-sheet"><div class="record-top"><div><span class="eyebrow">${fmtDate(a.dataUtc)}</span><h4>${esc(a.objetivoAcompanhamento||'Anamnese clínica')}</h4></div><small>${esc(a.profissionalNome)}</small></div><div class="clinical-grid">${clinical('Histórico de doenças',a.historicoDoencas)}${clinical('Histórico familiar',a.historicoFamiliar)}${clinical('Alergias',a.alergias)}${clinical('Medicamentos',a.medicamentos)}${clinical('Suplementos',a.suplementos)}${clinical('Sono',`${a.sonoHorasMedia??'—'} h • ${a.sonoQualidade||'não informado'}`)}${clinical('Estresse',a.estresseNivel!=null?`${a.estresseNivel}/10`:'—')}${clinical('Atividade física',a.atividadeFisica)}${clinical('Água/dia',a.aguaLitrosDia!=null?`${a.aguaLitrosDia} L`:'—')}${clinical('Observações',a.observacoes)}</div></article>`).join(''):sectionEmpty('Nenhuma anamnese registrada.')}</section>`;return}
if(state.patientTab==='avaliacoes'){box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Avaliações corporais</h3><small>${avaliacoes.length} registro(s)</small></div>${avaliacoes.length?`<div class="measurement-table"><div class="measurement-row header"><span>Data</span><span>Peso</span><span>IMC</span><span>Gordura</span><span>Cintura</span><span>Pressão</span></div>${avaliacoes.map(a=>`<div class="measurement-row"><span>${fmtDate(a.dataUtc)}</span><span>${num(a.pesoKg)} kg</span><span>${num(a.imc,2)}</span><span>${num(a.percentualGordura)}%</span><span>${num(a.cinturaCm)} cm</span><span>${a.pressaoSistolica&&a.pressaoDiastolica?`${a.pressaoSistolica}/${a.pressaoDiastolica}`:'—'}</span></div>`).join('')}</div>`:sectionEmpty('Nenhuma avaliação registrada.')}</section>`;return}
if(state.patientTab==='exames'){box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Exames laboratoriais</h3><small>${exames.length} coleta(s)</small></div>${exames.length?exames.map(x=>`<article class="exam-block"><div class="record-top"><div><span class="eyebrow">COLETA • ${fmtDate(x.dataColetaUtc)}</span><h4>${esc(x.laboratorio||'Laboratório não informado')}</h4></div><small>${esc(x.profissionalNome)}</small></div><div class="lab-table">${(x.resultados||[]).map(r=>`<div class="lab-line"><strong>${esc(r.marcadorNome)}</strong><span>${r.valorNumerico!=null?num(r.valorNumerico,2):esc(r.valorTexto||'—')} ${esc(r.unidade||'')}</span><span class="pill ${r.classificacao==='DentroDaReferencia'?'Ativa':r.classificacao}">${esc(r.classificacao)}</span></div>`).join('')}</div></article>`).join(''):sectionEmpty('Nenhum exame registrado.')}</section>`;return}
if(state.patientTab==='alimentacao'){box.innerHTML=`<section class="card full-card"><div class="card-head"><div><h3>Planos alimentares</h3><small>${planos.length} plano(s) • progressão preserva histórico</small></div><div class="nutrition-top-actions"><button class="ghost" id="mealLibraryButton">Biblioteca de refeições</button><button class="secondary" id="mealPlanFromTemplate">Usar modelo</button><button class="primary" id="newMealPlanFromTab">+ Novo plano</button></div></div>${planos.length?planos.map(p=>`<article class="food-plan nutrition-version-card"><div class="record-top"><div><span class="eyebrow">${esc(p.status)} • V${p.versao||1} • início ${fmtDate(p.dataInicio)}${p.ajustePercentual?` • ${p.ajustePercentual>0?'+':''}${num(p.ajustePercentual,1)}%`:''}</span><h4>${esc(p.nome)}</h4><small>${esc(p.profissionalNome||'')}</small></div><div class="macro-summary"><b>${num(p.totaisDiarios?.calorias,0)} kcal</b><span>P ${num(p.totaisDiarios?.proteinasG)}g</span><span>C ${num(p.totaisDiarios?.carboidratosG)}g</span><span>G ${num(p.totaisDiarios?.gordurasG)}g</span></div></div>${nutritionTargetPanel(p)}${nutritionMealDistribution(p)}<div class="nutrition-plan-actions"><button class="ghost nutrition-save-template" data-plan-id="${p.id}">Salvar como modelo</button><button class="secondary nutrition-progress" data-plan-id="${p.id}">Criar progressão</button></div><div class="meal-cards">${(p.refeicoes||[]).map(r=>`<div class="meal-card"><div class="meal-card-head"><div><strong>${r.horario?String(r.horario).slice(0,5):'--:--'} • ${esc(r.nome)}</strong><small>${num(r.totais?.calorias,0)} kcal</small></div><button class="ghost meal-save-template" data-meal-id="${r.id}" data-plan-id="${p.id}">Salvar refeição</button></div>${(r.itens||[]).map(i=>`<p>${num(i.quantidade)} ${esc(i.unidade)} ${esc(i.alimentoNome)}${i.substituicoes?.length?` <em>+ ${i.substituicoes.length} substituição(ões)</em>`:''}</p>`).join('')}</div>`).join('')}</div></article>`).join(''):sectionEmpty('Nenhum plano alimentar registrado.')}</section>`;const patientForNutrition=d.p||d.portal?.paciente||{id:state.patientId,nome:'Paciente'};
const newMealPlanButton=$('#newMealPlanFromTab');
if(newMealPlanButton)newMealPlanButton.onclick=()=>openMealPlanForm(patientForNutrition);
const templateButton=$('#mealPlanFromTemplate');
if(templateButton)templateButton.onclick=()=>openMealTemplatePicker(patientForNutrition);
const mealLibraryButton=$('#mealLibraryButton');
if(mealLibraryButton)mealLibraryButton.onclick=()=>openMealLibrary(planos);
$$('.meal-save-template').forEach(b=>b.onclick=()=>{const plan=planos.find(x=>x.id===b.dataset.planId);const meal=plan?.refeicoes?.find(x=>x.id===b.dataset.mealId);openSaveMealTemplate(meal)});
$$('.nutrition-save-template').forEach(b=>b.onclick=()=>openSaveMealTemplate(planos.find(x=>x.id===b.dataset.planId)));
$$('.nutrition-edit-targets').forEach(b=>b.onclick=()=>openNutritionTargets(planos.find(x=>x.id===b.dataset.planId)));
$$('.nutrition-distribute-targets').forEach(b=>b.onclick=()=>openMealTargetDistribution(planos.find(x=>x.id===b.dataset.planId)));
$$('.meal-edit-targets').forEach(b=>b.onclick=()=>{const plan=planos.find(p=>(p.refeicoes||[]).some(r=>r.id===b.dataset.mealId));const meal=plan?.refeicoes?.find(r=>r.id===b.dataset.mealId);openMealNutritionTargets(plan,meal)});
$$('.nutrition-progress').forEach(b=>b.onclick=()=>openNutritionProgression(patientForNutrition,planos.find(x=>x.id===b.dataset.planId)));
loadNutritionPhases(patientForNutrition,planos).catch(x=>console.warn('Fases nutricionais:',x));
return}
if(state.patientTab==='metas'){box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Metas</h3><small>${metas.length} registro(s)</small></div>${metas.length?`<div class="goals-grid">${metas.map(m=>`<article class="goal-card"><div class="record-top"><div><span class="eyebrow">${esc(m.tipo)} • ${esc(m.frequencia)}</span><h4>${esc(m.nome)}</h4></div><span class="pill ${m.status==='Ativa'?'Ativa':''}">${esc(m.status)}</span></div><div class="goal-big">${m.registroHoje?.valor!=null?num(m.registroHoje.valor):'—'} <small>${esc(m.unidade||'')}</small></div><p>Objetivo: ${num(m.valorObjetivo)} ${esc(m.unidade||'')} • progresso de hoje: ${num(m.progressoHojePercentual,0)}%</p></article>`).join('')}</div>`:sectionEmpty('Nenhuma meta registrada.')}</section>`;return}
if(state.patientTab==='diario'){box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Diário do paciente</h3><small>${diario.length} registro(s)</small></div>${diario.length?`<div class="diary-list">${diario.map(r=>`<article><div class="diary-icon">${diaryIcon(r.tipo)}</div><div><strong>${esc(r.tipo)}</strong><small>${fmtDateTime(r.dataHoraUtc)}</small><p>${esc(r.descricao||'')}</p></div><div class="diary-value">${r.valorNumerico!=null?`${num(r.valorNumerico)} ${esc(r.unidade||'')}`:''}${r.escala!=null?`<small>${r.escala}/10</small>`:''}</div></article>`).join('')}</div>`:sectionEmpty('Nenhum registro de diário.')}</section>`;return}
if(state.patientTab==='relatorios'){box.innerHTML=`<section class="card full-card"><div class="card-head"><div><h3>Relatórios clínicos</h3><small>${relatorios.length} snapshot(s) gerado(s)</small></div><button class="primary" id="newReportFromTab">+ Gerar relatório</button></div>${relatorios.length?`<div class="report-grid">${relatorios.map(r=>{const i=r.conteudo?.indicadores||{};return `<article class="report-card"><div class="report-card-head"><div><span class="eyebrow">${fmtDateTime(r.dataGeracaoUtc)}</span><h4>${esc(r.titulo)}</h4><small>${esc(r.profissionalNome)}</small></div><span class="pill Ativa">Snapshot</span></div><div class="report-metrics"><span><b>${i.consultas??0}</b> consultas</span><span><b>${i.avaliacoes??0}</b> avaliações</span><span><b>${i.exames??0}</b> exames</span><span><b>${num(i.pesoAtualKg)}</b> kg</span></div>${r.conclusaoMedica?`<p class="report-conclusion">${esc(r.conclusaoMedica)}</p>`:''}<div class="report-actions"><button class="secondary view-report" data-report-id="${r.id}">Visualizar / imprimir</button></div></article>`}).join('')}</div>`:sectionEmpty('Nenhum relatório gerado para este paciente.')}</section>`;$('#newReportFromTab').onclick=()=>openReportForm(d.p);$$('.view-report').forEach(b=>b.onclick=()=>openReportHtml(b.dataset.reportId));return}
box.innerHTML=`<section class="card full-card"><div class="card-head"><h3>Timeline clínica completa</h3><small>${timeline.length} evento(s)</small></div><div class="timeline expanded">${timeline.length?timeline.map(x=>`<div class="timeline-item"><strong>${esc(String(x.tipo||'evento').replaceAll('_',' '))}</strong><b>${esc(x.titulo||'')}</b><small>${fmtDateTime(x.dataUtc)}${x.resumo?' • '+esc(x.resumo):''}</small></div>`).join(''):sectionEmpty('Sem eventos clínicos.')}</div></section>`}
function nutritionTargetLine(label,prescrito,meta,unit='g'){
  if(meta==null)return `<div class="nutrition-target-line"><span>${label}</span><strong>${num(prescrito,unit==='kcal'?0:1)} ${unit}</strong><small>Sem meta definida</small></div>`;
  const diff=Number(prescrito)-Number(meta);
  const pct=Number(meta)>0?Math.min(140,Math.max(0,(Number(prescrito)/Number(meta))*100)):0;
  return `<div class="nutrition-target-line"><span>${label}</span><strong>${num(prescrito,unit==='kcal'?0:1)} / ${num(meta,unit==='kcal'?0:1)} ${unit}</strong><div class="nutrition-target-track"><i style="width:${pct}%"></i></div><small>${diff===0?'Na meta':`${diff>0?'+':''}${num(diff,unit==='kcal'?0:1)} ${unit} da meta`}</small></div>`;
}
function nutritionTargetPanel(p){
  const t=p.totaisDiarios||{};
  const has=[p.metaCalorias,p.metaProteinasG,p.metaCarboidratosG,p.metaGordurasG,p.metaFibrasG].some(x=>x!=null);
  return `<div class="nutrition-target-panel"><div class="nutrition-target-head"><div><strong>Meta × prescrito</strong><small>${has?'Comparação diária do plano':'Defina metas para acompanhar a prescrição'}</small></div><div class="nutrition-target-actions"><button class="ghost nutrition-distribute-targets" data-plan-id="${p.id}">Distribuir por refeição</button><button class="ghost nutrition-edit-targets" data-plan-id="${p.id}">Editar metas</button></div></div><div class="nutrition-target-grid">
    ${nutritionTargetLine('Calorias',t.calorias||0,p.metaCalorias,'kcal')}
    ${nutritionTargetLine('Proteína',t.proteinasG||0,p.metaProteinasG)}
    ${nutritionTargetLine('Carboidrato',t.carboidratosG||0,p.metaCarboidratosG)}
    ${nutritionTargetLine('Gordura',t.gordurasG||0,p.metaGordurasG)}
    ${nutritionTargetLine('Fibra',t.fibrasG||0,p.metaFibrasG)}
  </div></div>`;
}
function mealTargetMini(r){
  const t=r.totais||{},m=r.metas||{};
  const has=[m.calorias,m.proteinasG,m.carboidratosG,m.gordurasG,m.fibrasG].some(x=>x!=null);
  if(!has)return `<div class="meal-target-mini empty-target"><span>Sem meta por refeição</span><button class="ghost meal-edit-targets" data-meal-id="${r.id}">Definir meta</button></div>`;
  const item=(label,current,target,unit='g')=>`<span><b>${label}</b> ${num(current,unit==='kcal'?0:1)} / ${target==null?'—':num(target,unit==='kcal'?0:1)} ${unit}</span>`;
  return `<div class="meal-target-mini"><div>${item('Kcal',t.calorias||0,m.calorias,'kcal')}${item('P',t.proteinasG||0,m.proteinasG)}${item('C',t.carboidratosG||0,m.carboidratosG)}${item('G',t.gordurasG||0,m.gordurasG)}</div><button class="ghost meal-edit-targets" data-meal-id="${r.id}">Editar meta</button></div>`;
}
function nutritionMealDistribution(p){
  const total=p.totaisDiarios||{};
  const pct=(v,t)=>Number(t)>0?Math.round((Number(v||0)/Number(t))*100):0;
  return `<div class="nutrition-distribution"><div class="nutrition-distribution-head"><strong>Distribuição por refeição</strong><small>Prescrito no dia + metas planejadas por bloco</small></div>${(p.refeicoes||[]).map(r=>`<div class="nutrition-distribution-block"><div class="nutrition-distribution-row"><div><strong>${esc(r.nome)}</strong><small>${r.horario?String(r.horario).slice(0,5):'--:--'}</small></div><span>${pct(r.totais?.calorias,total.calorias)}% kcal</span><span>${pct(r.totais?.proteinasG,total.proteinasG)}% P</span><span>${pct(r.totais?.carboidratosG,total.carboidratosG)}% C</span><span>${pct(r.totais?.gordurasG,total.gordurasG)}% G</span></div>${mealTargetMini(r)}</div>`).join('')}</div>`;
}

function openClinicalActionMenu(p){
  const box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">REGISTRO CLÍNICO</span><h2>${esc(p.nome)}</h2><p>Escolha o que deseja registrar no prontuário.</p></div><div class="action-grid"><button data-action="consulta"><b>Consulta</b><span>Atendimento, queixa, evolução e conduta</span></button><button data-action="evolucao"><b>Evolução SOAP</b><span>Subjetivo, objetivo, avaliação e plano</span></button><button data-action="avaliacao"><b>Avaliação</b><span>Peso, medidas e sinais vitais</span></button><button data-action="anamnese"><b>Anamnese</b><span>Histórico, hábitos e objetivos</span></button><button data-action="exame"><b>Exame laboratorial</b><span>Coleta, marcadores, valores e referências</span></button><button data-action="plano"><b>Plano alimentar</b><span>Refeições, alimentos, macros e substituições</span></button><button data-action="treino"><b>Plano de treino</b><span>Treinos, exercícios, séries, repetições e carga</span></button><button data-action="meta"><b>Meta</b><span>Objetivo e acompanhamento</span></button><button data-action="diario"><b>Diário</b><span>Registro rápido do paciente</span></button><button data-action="relatorio"><b>Relatório clínico</b><span>Snapshot do período, conclusão e impressão</span></button></div>`;
  $('#clinicalActionModal').classList.remove('hidden');
  $$('#clinicalActionContent [data-action]').forEach(b=>b.onclick=()=>openClinicalForm(b.dataset.action,p));
}
function field(label,name,type='text',extra=''){return `<label>${label}<input name="${name}" type="${type}" ${extra}></label>`}
function area(label,name,extra=''){return `<label class="span-2">${label}<textarea name="${name}" ${extra}></textarea></label>`}
function clinicalFormShell(title,subtitle,body){return `<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PRONTUÁRIO</span><h2>${title}</h2><p>${subtitle}</p></div><form id="clinicalForm" class="form-grid clinical-form">${body}<div class="span-2 form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar registro</button></div></form>`}
function openClinicalForm(type,p){
  if(type==='exame'){openExamForm(p);return}
  if(type==='plano'){openMealPlanForm(p);return}
  if(type==='relatorio'){openReportForm(p);return}
  if(type==='evolucao'){openEvolutionForm(p);return}
  const box=$('#clinicalActionContent');let html='';
  if(type==='consulta')html=clinicalFormShell('Nova consulta',p.nome,`${field('Data e hora','dataHora','datetime-local',`value="${localDateTimeValue()}" required`)}${field('Status','status','text','value="Agendada"')}${field('Motivo','motivo')}${area('Queixa principal','queixaPrincipal')}${area('Evolução','evolucao')}${area('Conduta','conduta')}${area('Orientações','orientacoes')}`);
  if(type==='avaliacao')html=clinicalFormShell('Nova avaliação',p.nome,`${field('Data','dataUtc','datetime-local',`value="${localDateTimeValue()}"`)}${field('Peso (kg)','pesoKg','number','step="0.01"')}${field('Altura (m)','alturaM','number','step="0.01"')}${field('Gordura (%)','percentualGordura','number','step="0.01"')}${field('Massa magra (kg)','massaMagraKg','number','step="0.01"')}${field('Massa gorda (kg)','massaGordaKg','number','step="0.01"')}${field('Cintura (cm)','cinturaCm','number','step="0.01"')}${field('Abdômen (cm)','abdomenCm','number','step="0.01"')}${field('Quadril (cm)','quadrilCm','number','step="0.01"')}${field('Pressão sistólica','pressaoSistolica','number')}${field('Pressão diastólica','pressaoDiastolica','number')}${field('Frequência cardíaca','frequenciaCardiaca','number')}`);
  if(type==='anamnese')html=clinicalFormShell('Nova anamnese',p.nome,`${field('Data','dataUtc','datetime-local',`value="${localDateTimeValue()}"`)}${field('Objetivo do acompanhamento','objetivoAcompanhamento')}${area('Histórico de doenças','historicoDoencas')}${area('Histórico familiar','historicoFamiliar')}${area('Cirurgias','cirurgias')}${area('Alergias','alergias')}${area('Medicamentos','medicamentos')}${area('Suplementos','suplementos')}${field('Tabagismo','tabagismo')}${field('Etilismo','etilismo')}${field('Sono médio (h)','sonoHorasMedia','number','step="0.1"')}${field('Qualidade do sono','sonoQualidade')}${field('Estresse (0-10)','estresseNivel','number','min="0" max="10"')}${field('Atividade física','atividadeFisica')}${field('Dias/semana','atividadeFisicaDiasSemana','number','min="0" max="7"')}${field('Hábito intestinal','habitoIntestinal')}${field('Água (L/dia)','aguaLitrosDia','number','step="0.1"')}${area('Observações','observacoes')}`);
  if(type==='meta')html=clinicalFormShell('Nova meta',p.nome,`${field('Nome da meta','nome','text','required')}${field('Tipo','tipo','text','value="Hidratacao" required')}${field('Valor objetivo','valorObjetivo','number','step="0.01"')}${field('Unidade','unidade','text','placeholder="L, h, vezes..."')}${field('Frequência','frequencia','text','value="Diaria" required')}${field('Data de início','dataInicio','date',`value="${todayISO()}" required`)}${field('Data final','dataFim','date')}${area('Observações','observacoes')}`);
  if(type==='diario')html=clinicalFormShell('Novo registro de diário',p.nome,`${field('Data e hora','dataHoraUtc','datetime-local',`value="${localDateTimeValue()}" required`)}${field('Tipo','tipo','text','value="Observacao" required')}${field('Valor','valorNumerico','number','step="0.01"')}${field('Unidade','unidade')}${field('Escala (0-10)','escala','number','min="0" max="10"')}${area('Descrição','descricao')}`);
  box.innerHTML=html;$('.clinical-back').onclick=()=>openClinicalActionMenu(p);$('[data-close-clinical-form]').onclick=closeClinicalAction;$('#clinicalForm').onsubmit=e=>submitClinicalForm(e,type,p.id);
}
async function submitClinicalForm(e,type,pacienteId){
  e.preventDefault();const f=e.target,b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Salvando...';
  try{let path='',body={};
    if(type==='consulta'){path=`/api/pacientes/${pacienteId}/consultas`;body={dataHoraUtc:new Date(val(f,'dataHora')).toISOString(),motivo:val(f,'motivo'),queixaPrincipal:val(f,'queixaPrincipal'),evolucao:val(f,'evolucao'),conduta:val(f,'conduta'),orientacoes:val(f,'orientacoes'),status:val(f,'status')||'Agendada'};state.patientTab='consultas'}
    if(type==='avaliacao'){path=`/api/pacientes/${pacienteId}/avaliacoes`;body={consultaId:null,dataUtc:val(f,'dataUtc')?new Date(val(f,'dataUtc')).toISOString():null,pesoKg:dec(f,'pesoKg'),alturaM:dec(f,'alturaM'),percentualGordura:dec(f,'percentualGordura'),massaMagraKg:dec(f,'massaMagraKg'),massaGordaKg:dec(f,'massaGordaKg'),cinturaCm:dec(f,'cinturaCm'),abdomenCm:dec(f,'abdomenCm'),quadrilCm:dec(f,'quadrilCm'),pressaoSistolica:integer(f,'pressaoSistolica'),pressaoDiastolica:integer(f,'pressaoDiastolica'),frequenciaCardiaca:integer(f,'frequenciaCardiaca')};state.patientTab='avaliacoes'}
    if(type==='anamnese'){path=`/api/pacientes/${pacienteId}/anamneses`;body={consultaId:null,dataUtc:val(f,'dataUtc')?new Date(val(f,'dataUtc')).toISOString():null,objetivoAcompanhamento:val(f,'objetivoAcompanhamento'),historicoDoencas:val(f,'historicoDoencas'),historicoFamiliar:val(f,'historicoFamiliar'),cirurgias:val(f,'cirurgias'),alergias:val(f,'alergias'),medicamentos:val(f,'medicamentos'),suplementos:val(f,'suplementos'),tabagismo:val(f,'tabagismo'),etilismo:val(f,'etilismo'),sonoHorasMedia:dec(f,'sonoHorasMedia'),sonoQualidade:val(f,'sonoQualidade'),despertaDuranteNoite:null,estresseNivel:integer(f,'estresseNivel'),atividadeFisica:val(f,'atividadeFisica'),atividadeFisicaDiasSemana:integer(f,'atividadeFisicaDiasSemana'),habitoIntestinal:val(f,'habitoIntestinal'),aguaLitrosDia:dec(f,'aguaLitrosDia'),observacoes:val(f,'observacoes'),respostasPersonalizadas:[]};state.patientTab='anamnese'}
    if(type==='meta'){path=`/api/pacientes/${pacienteId}/metas`;body={nome:val(f,'nome'),tipo:val(f,'tipo'),valorObjetivo:dec(f,'valorObjetivo'),unidade:val(f,'unidade'),frequencia:val(f,'frequencia'),dataInicio:val(f,'dataInicio'),dataFim:val(f,'dataFim'),observacoes:val(f,'observacoes')};state.patientTab='metas'}
    if(type==='diario'){path=`/api/pacientes/${pacienteId}/diario`;body={dataHoraUtc:new Date(val(f,'dataHoraUtc')).toISOString(),tipo:val(f,'tipo'),descricao:val(f,'descricao'),valorNumerico:dec(f,'valorNumerico'),unidade:val(f,'unidade'),escala:integer(f,'escala'),imagemUrl:null};state.patientTab='diario'}
    await api(path,{method:'POST',body:JSON.stringify(body)});closeClinicalAction();toast('Registro salvo no prontuário.');await loadPatient();
  }catch(x){toast(x.message,true)}finally{b.disabled=false;b.textContent='Salvar registro'}
}

async function openEvolutionForm(p,id=null){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">EVOLUÇÃO CLÍNICA</span><h2>${id?'Editar evolução':'Nova evolução SOAP'}</h2><p>${esc(p.nome)} • carregando dados...</p></div>`;
  try{
    const [consultas,current]=await Promise.all([api(`/api/pacientes/${p.id}/consultas`),id?api(`/api/evolucoes/${id}`):Promise.resolve(null)]);
    const now=current?.dataHoraUtc?new Date(current.dataHoraUtc):new Date();
    const consultationOptions=`<option value="">Sem vínculo</option>`+(consultas||[]).map(c=>`<option value="${c.id}" ${current?.consultaId===c.id?'selected':''}>${fmtDateTime(c.dataHoraUtc)} • ${esc(c.motivo||'Consulta')}</option>`).join('');
    box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">EVOLUÇÃO CLÍNICA</span><h2>${id?'Editar evolução':'Nova evolução SOAP'}</h2><p>${esc(p.nome)}</p></div><form id="evolutionForm" class="clinical-form"><div class="form-grid">${field('Data e hora','dataHora','datetime-local',`value="${localDateTimeValue(now)}" required`)}<label>Vincular consulta<select name="consultaId">${consultationOptions}</select></label>${area('S • Subjetivo','subjetivo','placeholder="Relato do paciente, sintomas, percepção, adesão..."')}${area('O • Objetivo','objetivo','placeholder="Achados objetivos, sinais, medidas e dados observáveis..."')}${area('A • Avaliação','avaliacao','placeholder="Síntese e avaliação profissional..."')}${area('P • Plano','plano','placeholder="Conduta, próximos passos, orientações e acompanhamento..."')}${area('Observações complementares','observacoes')}</div><div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">${id?'Salvar alterações':'Registrar evolução'}</button></div></form>`;
    $('.clinical-back').onclick=()=>openClinicalActionMenu(p);
    $('[data-close-clinical-form]').onclick=closeClinicalAction;
    const f=$('#evolutionForm');
    if(current){
      f.querySelector('[name=subjetivo]').value=current.subjetivo||'';
      f.querySelector('[name=objetivo]').value=current.objetivo||'';
      f.querySelector('[name=avaliacao]').value=current.avaliacao||'';
      f.querySelector('[name=plano]').value=current.plano||'';
      f.querySelector('[name=observacoes]').value=current.observacoes||'';
    }
    f.onsubmit=async e=>{
      e.preventDefault();
      const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
      try{
        const body={consultaId:val(e.target,'consultaId')||null,dataHoraUtc:new Date(val(e.target,'dataHora')).toISOString(),subjetivo:val(e.target,'subjetivo'),objetivo:val(e.target,'objetivo'),avaliacao:val(e.target,'avaliacao'),plano:val(e.target,'plano'),observacoes:val(e.target,'observacoes')};
        const path=id?`/api/evolucoes/${id}`:`/api/pacientes/${p.id}/evolucoes`;
        await api(path,{method:id?'PUT':'POST',body:JSON.stringify(body)});
        state.patientTab='evolucoes';closeClinicalAction();toast(id?'Evolução atualizada.':'Evolução registrada.');await loadPatient();
      }catch(err){toast(err.message,true)}finally{btn.disabled=false}
    };
  }catch(err){toast(err.message,true)}
}

async function openSaveMealTemplate(meal){
  if(!meal)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">BIBLIOTECA DE REFEIÇÕES</span><h2>Salvar refeição</h2><p>${esc(meal.nome)}</p></div>
  <form id="saveMealLibraryForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome do modelo','nome','text',`value="${esc(meal.nome)}" required`)}
      ${field('Categoria','categoria','text','placeholder="Café da manhã, pré-treino, almoço..."')}
      ${area('Descrição','descricao','placeholder="Quando usar, perfil do paciente, observações..."')}
    </div>
    <div class="template-summary"><strong>${(meal.itens||[]).length} item(ns)</strong><span>Alimentos e substituições serão salvos neste bloco.</span></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Salvar na biblioteca</button></div>
  </form>`;

  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#saveMealLibraryForm');

  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/refeicoes-plano/${meal.id}/salvar-como-modelo`,{
        method:'POST',
        body:JSON.stringify({
          nome:val(f,'nome'),
          categoria:val(f,'categoria')||null,
          descricao:val(f,'descricao')||null
        })
      });
      closeClinicalAction();toast('Refeição salva na biblioteca.');
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openMealLibrary(planos){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">BIBLIOTECA DE REFEIÇÕES</span><h2>Inserção rápida</h2><p>Reutilize blocos sem duplicar um plano inteiro.</p></div><div class="empty">Carregando biblioteca...</div>`;

  try{
    const modelos=await api('/api/modelos-refeicoes');
    const ativos=(planos||[]).filter(p=>p.status!=='Concluido');

    box.innerHTML=`<div class="modal-heading"><span class="eyebrow">BIBLIOTECA DE REFEIÇÕES</span><h2>Inserção rápida</h2><p>${modelos.length} refeição(ões) salvas</p></div>
      <div class="meal-library-toolbar">
        <input id="mealLibrarySearch" class="search-input" placeholder="Buscar refeição, categoria ou descrição">
        <select id="mealLibraryPlan">${ativos.map(p=>`<option value="${p.id}">${esc(p.nome)} • V${p.versao||1}</option>`).join('')}</select>
      </div>
      <div id="mealLibraryList" class="meal-library-grid"></div>
      <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Fechar</button></div>`;

    $('[data-close-clinical-form]').onclick=closeClinicalAction;

    if(!ativos.length){
      $('#mealLibraryList').innerHTML=`<div class="empty">Crie ou ative um plano alimentar antes de inserir uma refeição.</div>`;
      return;
    }

    const render=q=>{
      const term=String(q||'').trim().toLowerCase();
      const filtered=modelos.filter(m=>!term||
        String(m.nome||'').toLowerCase().includes(term)||
        String(m.categoria||'').toLowerCase().includes(term)||
        String(m.descricao||'').toLowerCase().includes(term));

      $('#mealLibraryList').innerHTML=filtered.length?filtered.map(m=>`<article class="meal-library-card" data-meal-template-id="${m.id}">
        <div><span class="eyebrow">${esc(m.categoria||'SEM CATEGORIA')} • ${m.itens} item(ns)${m.substituicoes?` • ${m.substituicoes} subst.`:''}</span><h4>${esc(m.nome)}</h4><p>${esc(m.descricao||'Sem descrição')}</p><small>${m.horario?String(m.horario).slice(0,5):'Horário flexível'}</small></div>
        <button class="primary insert-meal-template">Inserir no plano</button>
      </article>`).join(''):`<div class="empty">Nenhuma refeição encontrada.</div>`;

      $$('.insert-meal-template').forEach(b=>b.onclick=()=>{
        const card=b.closest('[data-meal-template-id]');
        const modelo=modelos.find(x=>x.id===card.dataset.mealTemplateId);
        const plano=ativos.find(x=>x.id===$('#mealLibraryPlan').value);
        openMealLibraryInsertForm(plano,modelo,planos);
      });
    };

    render('');
    $('#mealLibrarySearch').oninput=e=>render(e.target.value);
  }catch(err){
    box.innerHTML=`<div class="card empty">${esc(err.message)}</div>`;
  }
}

function openMealLibraryInsertForm(plan,modelo,planos){
  if(!plan||!modelo)return;
  const box=$('#clinicalActionContent');
  const time=modelo.horario?String(modelo.horario).slice(0,5):'';

  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link" id="backToMealLibrary">← Biblioteca</button><span class="eyebrow">INSERÇÃO RÁPIDA</span><h2>${esc(modelo.nome)}</h2><p>${esc(plan.nome)} • V${plan.versao||1}</p></div>
  <form id="mealLibraryInsertForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome da refeição','nome','text',`value="${esc(modelo.nome)}"`)}
      ${field('Horário','horario','time',`value="${time}"`)}
      ${area('Observações','observacoes','placeholder="Opcional. Se vazio, mantém a observação do modelo."')}
    </div>
    <div class="template-summary"><strong>${modelo.itens} item(ns)</strong><span>Serão adicionados ao final do plano atual.</span></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Inserir refeição</button></div>
  </form>`;

  $('#backToMealLibrary').onclick=()=>openMealLibrary(planos);
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#mealLibraryInsertForm');

  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/planos-alimentares/${plan.id}/inserir-modelo-refeicao/${modelo.id}`,{
        method:'POST',
        body:JSON.stringify({
          nome:val(f,'nome')||null,
          horario:val(f,'horario')||null,
          observacoes:val(f,'observacoes')||null
        })
      });
      state.patientTab='alimentacao';closeClinicalAction();toast('Refeição inserida no plano.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openMealNutritionTargets(plan,meal){
  if(!plan||!meal)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  const v=x=>x==null?'':String(x);
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">META POR REFEIÇÃO</span><h2>${esc(meal.nome)}</h2><p>${esc(plan.nome)} • compare o planejado com o prescrito.</p></div>
  <form id="mealTargetsForm" class="clinical-form">
    <div class="nutrition-targets-current"><span>Prescrito</span><b>${num(meal.totais?.calorias||0,0)} kcal</b><span>P ${num(meal.totais?.proteinasG||0)}g • C ${num(meal.totais?.carboidratosG||0)}g • G ${num(meal.totais?.gordurasG||0)}g • Fibra ${num(meal.totais?.fibrasG||0)}g</span></div>
    <div class="form-grid">
      ${field('Meta calórica','metaCalorias','number',`step="1" min="1" value="${v(meal.metas?.calorias)}"`)}
      ${field('Proteína (g)','metaProteinasG','number',`step="0.1" min="0" value="${v(meal.metas?.proteinasG)}"`)}
      ${field('Carboidrato (g)','metaCarboidratosG','number',`step="0.1" min="0" value="${v(meal.metas?.carboidratosG)}"`)}
      ${field('Gordura (g)','metaGordurasG','number',`step="0.1" min="0" value="${v(meal.metas?.gordurasG)}"`)}
      ${field('Fibra (g)','metaFibrasG','number',`step="0.1" min="0" value="${v(meal.metas?.fibrasG)}"`)}
    </div>
    <p class="form-hint">Campos vazios ficam sem meta específica.</p>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar meta da refeição</button></div>
  </form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#mealTargetsForm');
  f.onsubmit=async e=>{
    e.preventDefault();const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/refeicoes-plano/${meal.id}/metas-nutricionais`,{method:'PUT',body:JSON.stringify({
        metaCalorias:dec(f,'metaCalorias'),metaProteinasG:dec(f,'metaProteinasG'),
        metaCarboidratosG:dec(f,'metaCarboidratosG'),metaGordurasG:dec(f,'metaGordurasG'),
        metaFibrasG:dec(f,'metaFibrasG')
      })});
      closeClinicalAction();toast('Meta da refeição atualizada.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

function openMealTargetDistribution(plan){
  if(!plan)return;
  const meals=plan.refeicoes||[];
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  const defaultPct=meals.length?100/meals.length:0;
  const currentPct=m=>{
    const target=Number(m.metas?.calorias);
    const planTarget=Number(plan.metaCalorias);
    return Number.isFinite(target)&&Number.isFinite(planTarget)&&planTarget>0?target/planTarget*100:defaultPct;
  };
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">DISTRIBUIÇÃO PLANEJADA</span><h2>Distribuir metas por refeição</h2><p>${esc(plan.nome)} • a mesma proporção será aplicada às metas diárias de kcal, P/C/G e fibras que estiverem definidas.</p></div>
  <form id="mealTargetDistributionForm" class="clinical-form">
    <div class="meal-distribution-editor">${meals.map(m=>`<label data-distribution-meal="${m.id}"><span><b>${esc(m.nome)}</b><small>${m.horario?String(m.horario).slice(0,5):'horário livre'}</small></span><input name="percentual" type="number" min="0" max="100" step="0.1" value="${num(currentPct(m),1)}"><em>%</em></label>`).join('')}</div>
    <div id="mealDistributionTotal" class="meal-distribution-total"></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Aplicar distribuição</button></div>
  </form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#mealTargetDistributionForm'),total=$('#mealDistributionTotal');
  const refresh=()=>{
    const sum=[...f.querySelectorAll('[name=percentual]')].reduce((a,x)=>a+Number(x.value||0),0);
    total.innerHTML=`<strong>${num(sum,1)}%</strong><span>${Math.abs(sum-100)<=.1?'Distribuição pronta para aplicar':'A soma precisa fechar em 100%'}</span>`;
    total.classList.toggle('invalid',Math.abs(sum-100)>.1);
  };
  f.querySelectorAll('[name=percentual]').forEach(x=>x.oninput=refresh);refresh();
  f.onsubmit=async e=>{
    e.preventDefault();const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      const refeicoes=[...f.querySelectorAll('[data-distribution-meal]')].map(r=>({refeicaoId:r.dataset.distributionMeal,percentual:Number(r.querySelector('[name=percentual]').value||0)}));
      const sum=refeicoes.reduce((a,x)=>a+x.percentual,0);if(Math.abs(sum-100)>.1)throw new Error('A distribuição precisa somar 100%.');
      await api(`/api/planos-alimentares/${plan.id}/distribuir-metas-refeicoes`,{method:'POST',body:JSON.stringify({refeicoes})});
      closeClinicalAction();toast('Metas distribuídas entre as refeições.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function loadNutritionPhases(patient,plans){
  const host=$('#patientTabContent');
  if(!host||!patient?.id||host.querySelector('[data-nutrition-phases]'))return;
  const phases=await api(`/api/pacientes/${patient.id}/fases-nutricionais`);
  if(!host.isConnected)return;

  const section=document.createElement('section');
  section.className='card full-card nutrition-phases-section';
  section.dataset.nutritionPhases='1';
  section.innerHTML=`<div class="card-head"><div><h3>Fases nutricionais</h3><small>${phases.length} fase(s) • organize o ciclo além das versões V1/V2/V3</small></div><button class="primary" id="newNutritionPhase">+ Nova fase</button></div>
  <div class="nutrition-phase-list">${phases.length?phases.map((f,i)=>nutritionPhaseCard(f,i,phases.length)).join(''):`<div class="empty">Nenhuma fase planejada. Crie etapas como adaptação, cutting, manutenção ou ganho.</div>`}</div>`;

  host.appendChild(section);
  $('#newNutritionPhase').onclick=()=>openNutritionPhaseForm(patient,plans,null);

  $$('.nutrition-phase-edit').forEach(b=>b.onclick=()=>openNutritionPhaseForm(patient,plans,phases.find(x=>x.id===b.dataset.phaseId)));
  $$('.nutrition-phase-delete').forEach(b=>b.onclick=()=>deleteNutritionPhase(patient,plans,phases.find(x=>x.id===b.dataset.phaseId)));
  $$('.nutrition-phase-up').forEach(b=>b.onclick=()=>moveNutritionPhase(patient,plans,phases,b.dataset.phaseId,-1));
  $$('.nutrition-phase-down').forEach(b=>b.onclick=()=>moveNutritionPhase(patient,plans,phases,b.dataset.phaseId,1));
}

function phaseGoalChips(f){
  const goals=[];
  if(f.metaPesoKg!=null)goals.push(`Peso ${num(f.metaPesoKg,1)} kg`);
  if(f.metaAdesaoPercentual!=null)goals.push(`Adesão ≥ ${num(f.metaAdesaoPercentual,0)}%`);
  if(f.duracaoMinimaDias!=null)goals.push(`Mín. ${num(f.duracaoMinimaDias,0)} dias`);
  if(f.criterioTransicao)goals.push('Critério profissional');
  return goals.length?`<div class="phase-goal-chips">${goals.map(x=>`<span>${esc(x)}</span>`).join('')}</div>`:'';
}

function nutritionPhaseCard(f,index,total){
  const statusLabel={Planejada:'Planejada',EmAndamento:'Em andamento',Concluida:'Concluída',Cancelada:'Cancelada'}[f.status]||f.status;
  const period=`${fmtDate(f.dataInicio)}${f.dataFim?' → '+fmtDate(f.dataFim):' → aberta'}`;
  return `<article class="nutrition-phase-card ${String(f.status||'').toLowerCase()}">
    <div class="nutrition-phase-order"><b>${index+1}</b><div><button class="ghost nutrition-phase-up" data-phase-id="${f.id}" ${index===0?'disabled':''}>↑</button><button class="ghost nutrition-phase-down" data-phase-id="${f.id}" ${index===total-1?'disabled':''}>↓</button></div></div>
    <div class="nutrition-phase-body">
      <div class="nutrition-phase-title"><div><span class="eyebrow">${esc(f.tipo)} • ${period}</span><h4>${esc(f.nome)}</h4></div><span class="pill ${f.status==='EmAndamento'?'Ativa':''}">${esc(statusLabel)}</span></div>
      ${f.objetivo?`<p>${esc(f.objetivo)}</p>`:''}
      ${phaseGoalChips(f)}
      <div class="nutrition-phase-meta">${f.planoNome?`<span><b>Plano:</b> ${esc(f.planoNome)} • V${f.planoVersao||1}</span>`:'<span>Sem plano vinculado</span>'}${f.profissionalNome?`<span><b>Profissional:</b> ${esc(f.profissionalNome)}</span>`:''}</div>
      ${f.observacoes?`<small>${esc(f.observacoes)}</small>`:''}
    </div>
    <div class="nutrition-phase-actions"><button class="secondary nutrition-phase-edit" data-phase-id="${f.id}">Editar</button><button class="ghost nutrition-phase-delete" data-phase-id="${f.id}">Excluir</button></div>
  </article>`;
}

function openNutritionPhaseForm(patient,plans,phase){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  const editing=!!phase;
  const v=x=>x==null?'':String(x);
  const statusOptions=editing?`<label>Status<select name="status"><option value="Planejada">Planejada</option><option value="EmAndamento">Em andamento</option><option value="Concluida">Concluída</option><option value="Cancelada">Cancelada</option></select></label>`:'';

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">PLANEJAMENTO NUTRICIONAL</span><h2>${editing?'Editar fase':'Nova fase nutricional'}</h2><p>${esc(patient.nome)}</p></div>
  <form id="nutritionPhaseForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome da fase','nome','text',`value="${esc(v(phase?.nome))}" placeholder="Ex.: Cutting inicial" required`)}
      <label>Tipo<select name="tipo"><option>Adaptação</option><option>Cutting</option><option>Manutenção</option><option>Refeed</option><option>Ganho</option><option>Performance</option><option>Personalizada</option></select></label>
      ${field('Data de início','dataInicio','date',`value="${v(phase?.dataInicio)||todayISO()}" required`)}
      ${field('Data final','dataFim','date',`value="${v(phase?.dataFim)}"`)}
      <label>Plano alimentar<select name="planoAlimentarId"><option value="">Sem vínculo</option>${(plans||[]).map(p=>`<option value="${p.id}">${esc(p.nome)} • V${p.versao||1}</option>`).join('')}</select></label>
      ${statusOptions}
      ${field('Meta de peso (kg)','metaPesoKg','number',`step="0.1" min="20" max="400" value="${v(phase?.metaPesoKg)}"`)}
      ${field('Adesão mínima (%)','metaAdesaoPercentual','number',`min="0" max="100" value="${v(phase?.metaAdesaoPercentual)}"`)}
      ${field('Duração mínima (dias)','duracaoMinimaDias','number',`min="1" max="3650" value="${v(phase?.duracaoMinimaDias)}"`)}
      ${area('Critério profissional de transição','criterioTransicao','placeholder="Ex.: manter exames estáveis e boa tolerância por duas semanas."')}
      ${area('Objetivo da fase','objetivo',`placeholder="Ex.: reduzir 4 kg preservando força e adesão."`)}
      ${area('Observações','observacoes','placeholder="Estratégia, checkpoints e observações gerais..."')}
    </div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">${editing?'Salvar alterações':'Criar fase'}</button></div>
  </form>`;

  const f=$('#nutritionPhaseForm');
  f.tipo.value=phase?.tipo||'Personalizada';
  f.planoAlimentarId.value=phase?.planoAlimentarId||'';
  if(editing)f.status.value=phase.status||'Planejada';
  f.objetivo.value=phase?.objetivo||'';
  f.criterioTransicao.value=phase?.criterioTransicao||'';
  f.observacoes.value=phase?.observacoes||'';

  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');
    btn.disabled=true;
    try{
      const base={
        nome:val(f,'nome'),
        tipo:val(f,'tipo'),
        objetivo:val(f,'objetivo')||null,
        dataInicio:val(f,'dataInicio'),
        dataFim:val(f,'dataFim')||null,
        planoAlimentarId:val(f,'planoAlimentarId')||null,
        metaPesoKg:dec(f,'metaPesoKg'),
        metaAdesaoPercentual:val(f,'metaAdesaoPercentual')===''?null:Number(val(f,'metaAdesaoPercentual')),
        duracaoMinimaDias:val(f,'duracaoMinimaDias')===''?null:Number(val(f,'duracaoMinimaDias')),
        criterioTransicao:val(f,'criterioTransicao')||null,
        observacoes:val(f,'observacoes')||null
      };

      if(editing){
        await api(`/api/fases-nutricionais/${phase.id}`,{method:'PUT',body:JSON.stringify({...base,status:val(f,'status')})});
        toast('Fase nutricional atualizada.');
      }else{
        await api(`/api/pacientes/${patient.id}/fases-nutricionais`,{method:'POST',body:JSON.stringify(base)});
        toast('Fase nutricional criada.');
      }

      closeClinicalAction();
      await loadPatient();
    }catch(err){
      toast(err.message,true);
    }finally{
      btn.disabled=false;
    }
  };
}

async function deleteNutritionPhase(patient,plans,phase){
  if(!phase)return;
  if(!confirm(`Excluir a fase "${phase.nome}"?`))return;
  try{
    await api(`/api/fases-nutricionais/${phase.id}`,{method:'DELETE'});
    toast('Fase excluída.');
    await loadPatient();
  }catch(err){toast(err.message,true)}
}

async function moveNutritionPhase(patient,plans,phases,id,delta){
  const list=phases.slice();
  const idx=list.findIndex(x=>x.id===id);
  const target=idx+delta;
  if(idx<0||target<0||target>=list.length)return;

  [list[idx],list[target]]=[list[target],list[idx]];
  const payload={fases:list.map((x,i)=>({faseId:x.id,ordem:i+1}))};

  try{
    await api(`/api/pacientes/${patient.id}/fases-nutricionais/reordenar`,{method:'POST',body:JSON.stringify(payload)});
    toast('Ordem das fases atualizada.');
    await loadPatient();
  }catch(err){toast(err.message,true)}
}

async function openNutritionTargets(plan){
  if(!plan)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  const v=x=>x==null?'':String(x);
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">METAS NUTRICIONAIS</span><h2>Meta × prescrito</h2><p>${esc(plan.nome)}</p></div>
  <form id="nutritionTargetsForm" class="clinical-form">
    <div class="nutrition-targets-current"><span>Prescrito agora</span><b>${num(plan.totaisDiarios?.calorias||0,0)} kcal</b><span>P ${num(plan.totaisDiarios?.proteinasG||0)}g • C ${num(plan.totaisDiarios?.carboidratosG||0)}g • G ${num(plan.totaisDiarios?.gordurasG||0)}g • Fibra ${num(plan.totaisDiarios?.fibrasG||0)}g</span></div>
    <div class="form-grid">
      ${field('Meta calórica','metaCalorias','number',`step="1" min="1" value="${v(plan.metaCalorias)}"`)}
      ${field('Proteína (g)','metaProteinasG','number',`step="0.1" min="0" value="${v(plan.metaProteinasG)}"`)}
      ${field('Carboidrato (g)','metaCarboidratosG','number',`step="0.1" min="0" value="${v(plan.metaCarboidratosG)}"`)}
      ${field('Gordura (g)','metaGordurasG','number',`step="0.1" min="0" value="${v(plan.metaGordurasG)}"`)}
      ${field('Fibra (g)','metaFibrasG','number',`step="0.1" min="0" value="${v(plan.metaFibrasG)}"`)}
    </div>
    <p class="form-hint">Deixe um campo vazio para não definir meta naquele indicador.</p>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Salvar metas</button></div>
  </form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#nutritionTargetsForm');
  f.onsubmit=async e=>{
    e.preventDefault();const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/planos-alimentares/${plan.id}/metas-nutricionais`,{method:'PUT',body:JSON.stringify({
        metaCalorias:dec(f,'metaCalorias'),
        metaProteinasG:dec(f,'metaProteinasG'),
        metaCarboidratosG:dec(f,'metaCarboidratosG'),
        metaGordurasG:dec(f,'metaGordurasG'),
        metaFibrasG:dec(f,'metaFibrasG')
      })});
      closeClinicalAction();toast('Metas nutricionais atualizadas.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openSaveMealTemplate(plan){
  if(!plan)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELO ALIMENTAR</span><h2>Salvar plano como modelo</h2><p>${esc(plan.nome)}</p></div>
  <form id="saveMealTemplateForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome do modelo','nome','text',`value="${esc(plan.nome)}" required`)}
      ${area('Descrição','descricao','placeholder="Ex.: Cutting 2200 kcal, rotina 5 refeições..."')}
    </div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Salvar modelo</button></div>
  </form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#saveMealTemplateForm');
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/planos-alimentares/${plan.id}/salvar-como-modelo`,{
        method:'POST',
        body:JSON.stringify({nome:val(f,'nome'),descricao:val(f,'descricao')||null})
      });
      closeClinicalAction();toast('Modelo alimentar salvo para reutilização.');
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openMealTemplatePicker(p){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELOS ALIMENTARES</span><h2>Criar plano a partir de modelo</h2><p>${esc(p.nome)}</p></div><div class="empty">Carregando modelos...</div>`;

  try{
    const modelos=await api('/api/modelos-planos-alimentares');
    if(!modelos.length){
      box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELOS ALIMENTARES</span><h2>Criar plano a partir de modelo</h2><p>${esc(p.nome)}</p></div><div class="empty">Nenhum modelo ativo. Salve um plano existente como modelo primeiro.</div><div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Fechar</button></div>`;
      $('[data-close-clinical-form]').onclick=closeClinicalAction;
      return;
    }

    box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELOS ALIMENTARES</span><h2>Criar plano a partir de modelo</h2><p>${esc(p.nome)} • ${modelos.length} modelo(s)</p></div>
      <div class="template-picker-toolbar"><input id="mealTemplateSearch" class="search-input" placeholder="Buscar modelo"></div>
      <div id="mealTemplateList" class="meal-template-grid"></div>
      <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Fechar</button></div>`;
    $('[data-close-clinical-form]').onclick=closeClinicalAction;

    const render=q=>{
      const term=String(q||'').trim().toLowerCase();
      const filtered=modelos.filter(m=>!term||m.nome.toLowerCase().includes(term)||String(m.descricao||'').toLowerCase().includes(term));
      $('#mealTemplateList').innerHTML=filtered.length?filtered.map(m=>`<article class="meal-template-card" data-template-id="${m.id}">
        <div><span class="eyebrow">${m.refeicoes} refeição(ões) • ${m.itens} item(ns)${m.metaCalorias!=null?` • ${num(m.metaCalorias,0)} kcal alvo`:''}</span><h4>${esc(m.nome)}</h4><p>${esc(m.descricao||'Sem descrição')}</p><small>${esc(m.profissionalNome||'')}</small></div>
        <button class="primary use-meal-template">Usar este modelo</button>
      </article>`).join(''):`<div class="empty">Nenhum modelo encontrado.</div>`;
      $$('.use-meal-template').forEach(b=>b.onclick=()=>{
        const card=b.closest('[data-template-id]');
        const m=modelos.find(x=>x.id===card.dataset.templateId);
        openMealTemplateCreateForm(p,m);
      });
    };
    render('');
    $('#mealTemplateSearch').oninput=e=>render(e.target.value);
  }catch(err){
    box.innerHTML=`<div class="card empty">${esc(err.message)}</div>`;
  }
}

function openMealTemplateCreateForm(p,modelo){
  const box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link" id="backToMealTemplates">← Modelos</button><span class="eyebrow">CRIAR A PARTIR DE MODELO</span><h2>${esc(modelo.nome)}</h2><p>${esc(p.nome)}</p></div>
  <form id="mealTemplateCreateForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome do plano','nome','text',`value="${esc(modelo.nome)}" required`)}
      ${field('Data de início','dataInicio','date',`value="${todayISO()}" required`)}
      ${field('Data final','dataFim','date')}
      ${area('Orientações adicionais','observacoes','placeholder="Opcional. Se vazio, usa as orientações originais do modelo."')}
    </div>
    <div class="template-summary"><strong>${modelo.refeicoes} refeição(ões)</strong><span>${modelo.itens} item(ns) serão copiados para o novo plano.</span></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Criar plano</button></div>
  </form>`;
  $('#backToMealTemplates').onclick=()=>openMealTemplatePicker(p);
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#mealTemplateCreateForm');
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/pacientes/${p.id}/planos-alimentares/criar-de-modelo/${modelo.id}`,{
        method:'POST',
        body:JSON.stringify({
          nome:val(f,'nome'),
          dataInicio:val(f,'dataInicio'),
          dataFim:val(f,'dataFim')||null,
          observacoes:val(f,'observacoes')||null
        })
      });
      state.patientTab='alimentacao';closeClinicalAction();toast('Plano criado a partir do modelo.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openNutritionProgression(p,plan){
  if(!plan)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  const suggested=`${plan.nome} • V${(plan.versao||1)+1}`;
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PROGRESSÃO NUTRICIONAL</span><h2>Nova versão do plano</h2><p>${esc(p.nome)} • baseado em ${esc(plan.nome)}</p></div>
  <form id="nutritionProgressForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome da nova versão','nome','text',`value="${esc(suggested)}" required`)}
      ${field('Data de início','dataInicio','date',`value="${new Date().toISOString().slice(0,10)}" required`)}
      ${field('Data final','dataFim','date')}
      <label>Modo de ajuste<select name="modo"><option value="percentual">Percentual das porções</option><option value="calorias">Calorias alvo</option></select></label>
      ${field('Ajuste (%)','percentual','number','step="0.1" min="-50" max="100" value="0"')}
      ${field('Calorias alvo','caloriasAlvo','number','step="1" min="1" disabled')}
      <label class="span-2 nutrition-check"><input name="concluirAnterior" type="checkbox" checked> Concluir plano anterior ao criar a nova versão</label>
    </div>
    <div id="nutritionProjection" class="nutrition-projection"><div class="empty compact">Ajuste os valores para visualizar a projeção.</div></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Criar nova versão</button></div>
  </form>`;
  $('.clinical-back').onclick=()=>openClinicalActionMenu(p);
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#nutritionProgressForm');
  const mode=f.querySelector('[name=modo]');
  const pct=f.querySelector('[name=percentual]');
  const kcal=f.querySelector('[name=caloriasAlvo]');
  let timer;
  const refresh=()=>{
    clearTimeout(timer);
    timer=setTimeout(async()=>{
      try{
        const qs=mode.value==='calorias'
          ?`caloriasAlvo=${encodeURIComponent(kcal.value||0)}`
          :`percentual=${encodeURIComponent(pct.value||0)}`;
        const s=await api(`/api/planos-alimentares/${plan.id}/simular-ajuste?${qs}`);
        $('#nutritionProjection').innerHTML=`<div class="nutrition-projection-grid">
          <div><small>Atual</small><strong>${num(s.totaisAtuais.calorias,0)} kcal</strong><span>P ${num(s.totaisAtuais.proteinasG)} • C ${num(s.totaisAtuais.carboidratosG)} • G ${num(s.totaisAtuais.gordurasG)}</span></div>
          <div><small>Projetado</small><strong>${num(s.totaisProjetados.calorias,0)} kcal</strong><span>P ${num(s.totaisProjetados.proteinasG)} • C ${num(s.totaisProjetados.carboidratosG)} • G ${num(s.totaisProjetados.gordurasG)}</span></div>
          <div><small>Ajuste</small><strong>${s.ajustePercentual>0?'+':''}${num(s.ajustePercentual,1)}%</strong><span>${s.itensAfetados} alimento(s) escalados</span></div>
        </div>`;
      }catch(err){
        $('#nutritionProjection').innerHTML=`<div class="empty compact">${esc(err.message)}</div>`;
      }
    },180);
  };
  mode.onchange=()=>{
    const byKcal=mode.value==='calorias';
    pct.disabled=byKcal;kcal.disabled=!byKcal;
    if(byKcal&&!kcal.value)kcal.value=Math.round(plan.totaisDiarios?.calorias||0);
    refresh();
  };
  pct.oninput=refresh;kcal.oninput=refresh;
  refresh();
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      const byKcal=mode.value==='calorias';
      await api(`/api/planos-alimentares/${plan.id}/duplicar`,{method:'POST',body:JSON.stringify({
        nome:val(f,'nome'),
        dataInicio:val(f,'dataInicio'),
        dataFim:val(f,'dataFim')||null,
        ajustePercentual:byKcal?null:Number(val(f,'percentual')||0),
        caloriasAlvo:byKcal?Number(val(f,'caloriasAlvo')):null,
        concluirPlanoAnterior:f.querySelector('[name=concluirAnterior]').checked
      })});
      state.patientTab='alimentacao';closeClinicalAction();toast('Nova versão do plano alimentar criada.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

function selectOptions(items,valueKey='id',labelKey='nome',placeholder='Selecione...'){return `<option value="">${placeholder}</option>`+items.map(x=>`<option value="${esc(x[valueKey])}">${esc(x[labelKey])}</option>`).join('')}
function examResultRow(marcadores){return `<div class="builder-row exam-result-row"><label>Marcador<select name="marcadorId" class="exam-marker">${selectOptions(marcadores)}</select></label><label>Valor numérico<input name="valorNumerico" type="number" step="0.001"></label><label>Valor em texto<input name="valorTexto" placeholder="Positivo, negativo..."></label><label>Unidade<input name="unidade"></label><label>Ref. mínima<input name="referenciaMinima" type="number" step="0.001"></label><label>Ref. máxima<input name="referenciaMaxima" type="number" step="0.001"></label><button type="button" class="remove-builder-row" title="Remover">×</button></div>`}
async function openExamForm(p){
  const box=$('#clinicalActionContent');box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">EXAMES LABORATORIAIS</span><h2>Nova coleta</h2><p>${esc(p.nome)} • carregando catálogo...</p></div>`;
  $('.clinical-back').onclick=()=>openClinicalActionMenu(p);
  try{
    const marcadores=await api('/api/exames/marcadores');
    if(!marcadores.length){box.innerHTML+=`<div class="empty">Nenhum marcador ativo no catálogo. Cadastre marcadores pelo Swagger antes de registrar a coleta.</div>`;return}
    box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">EXAMES LABORATORIAIS</span><h2>Nova coleta</h2><p>${esc(p.nome)} • ${marcadores.length} marcador(es) disponíveis</p></div><form id="examForm" class="clinical-form"><div class="form-grid builder-meta">${field('Data da coleta','dataColeta','datetime-local',`value="${localDateTimeValue()}" required`)}${field('Laboratório','laboratorio')}${area('Observações','observacoes')}</div><div class="builder-head"><div><h3>Resultados</h3><p>Adicione um ou mais marcadores desta coleta.</p></div><button type="button" class="secondary" id="addExamResult">+ Resultado</button></div><div id="examResults" class="builder-list"></div><div class="form-actions builder-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar coleta</button></div></form>`;
    $('.clinical-back').onclick=()=>openClinicalActionMenu(p);$('[data-close-clinical-form]').onclick=closeClinicalAction;
    const list=$('#examResults');const add=()=>{list.insertAdjacentHTML('beforeend',examResultRow(marcadores));const row=list.lastElementChild;row.querySelector('.remove-builder-row').onclick=()=>{if(list.children.length>1)row.remove()};row.querySelector('.exam-marker').onchange=e=>{const m=marcadores.find(x=>x.id===e.target.value);if(m&&!row.querySelector('[name=unidade]').value)row.querySelector('[name=unidade]').value=m.unidadePadrao||''}};add();$('#addExamResult').onclick=add;
    $('#examForm').onsubmit=async e=>{e.preventDefault();const f=e.target,b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Salvando...';try{const resultados=[...f.querySelectorAll('.exam-result-row')].map(r=>({marcadorId:r.querySelector('[name=marcadorId]').value,valorNumerico:r.querySelector('[name=valorNumerico]').value===''?null:Number(r.querySelector('[name=valorNumerico]').value),valorTexto:r.querySelector('[name=valorTexto]').value||null,unidade:r.querySelector('[name=unidade]').value||null,referenciaMinima:r.querySelector('[name=referenciaMinima]').value===''?null:Number(r.querySelector('[name=referenciaMinima]').value),referenciaMaxima:r.querySelector('[name=referenciaMaxima]').value===''?null:Number(r.querySelector('[name=referenciaMaxima]').value),referenciaTexto:null,observacao:null})).filter(x=>x.marcadorId);if(!resultados.length)throw new Error('Adicione pelo menos um resultado.');if(resultados.some(x=>x.valorNumerico==null&&!x.valorTexto))throw new Error('Informe valor numérico ou textual em todos os resultados.');await api(`/api/pacientes/${p.id}/exames`,{method:'POST',body:JSON.stringify({dataColetaUtc:new Date(val(f,'dataColeta')).toISOString(),laboratorio:val(f,'laboratorio'),observacoes:val(f,'observacoes'),resultados})});state.patientTab='exames';closeClinicalAction();toast('Coleta laboratorial salva.');await loadPatient()}catch(x){toast(x.message,true)}finally{b.disabled=false;b.textContent='Salvar coleta'}};
  }catch(x){toast(x.message,true)}
}
function foodOptionLabel(a){return `${a.nome} • ${num(a.caloriasPor100g,0)} kcal/100g`}
function foodOptions(alimentos){return `<option value="">Selecione um alimento...</option>`+alimentos.map(a=>`<option value="${a.id}">${esc(foodOptionLabel(a))}</option>`).join('')}
function substitutionRow(alimentos){return `<div class="substitution-row"><select name="subFood">${foodOptions(alimentos)}</select><input name="subQty" type="number" step="0.01" placeholder="Qtd"><input name="subUnit" value="g" placeholder="Unid."><input name="subGrams" type="number" step="0.01" placeholder="Gramas"><button type="button" class="remove-sub">×</button></div>`}
function mealItemRow(alimentos){return `<div class="meal-item-builder"><div class="meal-item-main"><select name="foodId">${foodOptions(alimentos)}</select><input name="qty" type="number" step="0.01" value="100" placeholder="Qtd"><input name="unit" value="g" placeholder="Unid."><input name="grams" type="number" step="0.01" value="100" placeholder="Gramas"><button type="button" class="secondary add-sub">+ Substituição</button><button type="button" class="remove-builder-row">×</button></div><div class="item-macro-preview">Selecione um alimento para calcular os macros.</div><div class="substitution-list"></div></div>`}
function mealBuilder(alimentos,index){return `<section class="meal-builder" data-meal-index="${index}"><div class="meal-builder-head"><div class="meal-meta"><input name="mealName" placeholder="Nome da refeição" value="${index===1?'Café da manhã':'Refeição '+index}"><input name="mealTime" type="time" value="${index===1?'08:00':'12:00'}"></div><button type="button" class="remove-meal secondary">Remover refeição</button></div><div class="meal-target-builder"><small>META DESTA REFEIÇÃO • OPCIONAL</small><div><input name="mealMetaCalorias" type="number" min="1" step="1" placeholder="kcal"><input name="mealMetaProteinasG" type="number" min="0" step="0.1" placeholder="Proteína g"><input name="mealMetaCarboidratosG" type="number" min="0" step="0.1" placeholder="Carbo g"><input name="mealMetaGordurasG" type="number" min="0" step="0.1" placeholder="Gordura g"><input name="mealMetaFibrasG" type="number" min="0" step="0.1" placeholder="Fibra g"></div></div><div class="meal-items"></div><button type="button" class="ghost add-meal-item">+ Adicionar alimento</button></section>`}
function bindMealBuilder(meal,alimentos){
  const items=meal.querySelector('.meal-items');const addItem=()=>{items.insertAdjacentHTML('beforeend',mealItemRow(alimentos));const row=items.lastElementChild;row.querySelector('.remove-builder-row').onclick=()=>{if(items.children.length>1){row.remove();updatePlanPreview(alimentos)}};row.querySelector('.add-sub').onclick=()=>{const list=row.querySelector('.substitution-list');list.insertAdjacentHTML('beforeend',substitutionRow(alimentos));const sub=list.lastElementChild;sub.querySelector('.remove-sub').onclick=()=>sub.remove()};['foodId','grams'].forEach(n=>row.querySelector(`[name=${n}]`).oninput=()=>updatePlanPreview(alimentos));addPlanSelectEvents(row,alimentos)};addItem();meal.querySelector('.add-meal-item').onclick=addItem;meal.querySelector('.remove-meal').onclick=()=>{const all=$$('#mealBuilders .meal-builder');if(all.length>1){meal.remove();updatePlanPreview(alimentos)}}
}
function addPlanSelectEvents(row,alimentos){const select=row.querySelector('[name=foodId]');select.onchange=()=>{const a=alimentos.find(x=>x.id===select.value),grams=Number(row.querySelector('[name=grams]').value||0),preview=row.querySelector('.item-macro-preview');if(!a){preview.textContent='Selecione um alimento para calcular os macros.';return}const f=grams/100;preview.textContent=`${num(a.caloriasPor100g*f,0)} kcal • P ${num(a.proteinasPor100g*f)}g • C ${num(a.carboidratosPor100g*f)}g • G ${num(a.gordurasPor100g*f)}g`;updatePlanPreview(alimentos)}}
function updatePlanPreview(alimentos){let kcal=0,p=0,c=0,g=0,fib=0;$$('#mealBuilders .meal-item-builder').forEach(r=>{const a=alimentos.find(x=>x.id===r.querySelector('[name=foodId]').value),grams=Number(r.querySelector('[name=grams]').value||0);if(!a)return;const m=grams/100;kcal+=Number(a.caloriasPor100g)*m;p+=Number(a.proteinasPor100g)*m;c+=Number(a.carboidratosPor100g)*m;g+=Number(a.gordurasPor100g)*m;fib+=Number(a.fibrasPor100g)*m});const out=$('#planMacroPreview');if(out)out.innerHTML=`<b>${num(kcal,0)} kcal</b><span>P ${num(p)}g</span><span>C ${num(c)}g</span><span>G ${num(g)}g</span><span>Fibra ${num(fib)}g</span>`;const f=$('#mealPlanForm'),target=$('#planTargetPreview');if(f&&target){const line=(label,current,name,unit)=>{const goal=dec(f,name);return `<span><b>${label}</b> ${goal==null?'sem meta':`${num(current,unit==='kcal'?0:1)} / ${num(goal,unit==='kcal'?0:1)} ${unit}`}</span>`};target.innerHTML=line('Kcal',kcal,'metaCalorias','kcal')+line('P',p,'metaProteinasG','g')+line('C',c,'metaCarboidratosG','g')+line('G',g,'metaGordurasG','g')+line('Fibra',fib,'metaFibrasG','g')}}
async function openMealPlanForm(p){
  if(!p?.id&&state.patientId){
    try{p=await api(`/api/pacientes/${state.patientId}`)}catch{}
  }
  if(!p?.id){toast('Paciente não identificado para criar o plano alimentar.',true);return}

  const box=$('#clinicalActionContent');box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PLANO ALIMENTAR</span><h2>Novo plano</h2><p>${esc(p.nome)} • carregando catálogo...</p></div>`;$('.clinical-back').onclick=()=>openClinicalActionMenu(p);
  try{const alimentos=await api('/api/alimentos');if(!alimentos.length){box.innerHTML+=`<div class="empty">Nenhum alimento ativo no catálogo. Cadastre alimentos pelo Swagger antes de montar o plano.</div>`;return}
    box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PLANO ALIMENTAR</span><h2>Novo plano</h2><p>${esc(p.nome)} • ${alimentos.length} alimento(s) disponíveis</p></div><form id="mealPlanForm" class="clinical-form"><div class="form-grid builder-meta">${field('Nome do plano','nome','text','value="Plano alimentar" required')}${field('Data de início','dataInicio','date',`value="${todayISO()}" required`)}${field('Data final','dataFim','date')}${area('Orientações gerais','observacoes')}</div><section class="nutrition-target-builder"><div><strong>Metas nutricionais diárias</strong><small>Opcional — use para comparar meta × prescrito.</small></div><div class="nutrition-target-inputs">${field('Calorias','metaCalorias','number','step="1" min="1" placeholder="2200"')}${field('Proteína (g)','metaProteinasG','number','step="0.1" min="0" placeholder="160"')}${field('Carboidrato (g)','metaCarboidratosG','number','step="0.1" min="0" placeholder="250"')}${field('Gordura (g)','metaGordurasG','number','step="0.1" min="0" placeholder="70"')}${field('Fibra (g)','metaFibrasG','number','step="0.1" min="0" placeholder="30"')}</div><div id="planTargetPreview" class="nutrition-target-preview"></div></section><div class="builder-head"><div><h3>Refeições</h3><p>Monte a rotina alimentar e suas substituições.</p></div><button type="button" class="secondary" id="addMeal">+ Refeição</button></div><div id="mealBuilders" class="builder-list"></div><div class="plan-preview"><small>TOTAL ESTIMADO DO PLANO</small><div id="planMacroPreview" class="macro-summary"><b>0 kcal</b><span>P 0g</span><span>C 0g</span><span>G 0g</span></div></div><div class="form-actions builder-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar plano alimentar</button></div></form>`;
    $('.clinical-back').onclick=()=>openClinicalActionMenu(p);$('[data-close-clinical-form]').onclick=closeClinicalAction;const list=$('#mealBuilders');let mealIndex=0;const addMeal=()=>{mealIndex++;list.insertAdjacentHTML('beforeend',mealBuilder(alimentos,mealIndex));bindMealBuilder(list.lastElementChild,alimentos)};addMeal();$('#addMeal').onclick=addMeal;['metaCalorias','metaProteinasG','metaCarboidratosG','metaGordurasG','metaFibrasG'].forEach(n=>{const e=$(`#mealPlanForm [name=${n}]`);if(e)e.oninput=()=>updatePlanPreview(alimentos)});updatePlanPreview(alimentos);
    $('#mealPlanForm').onsubmit=async e=>{e.preventDefault();const f=e.target,b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Salvando...';try{const refeicoes=[...f.querySelectorAll('.meal-builder')].map((m,idx)=>({nome:m.querySelector('[name=mealName]').value.trim(),horario:m.querySelector('[name=mealTime]').value||null,ordem:idx+1,observacoes:null,metaCalorias:m.querySelector('[name=mealMetaCalorias]').value===''?null:Number(m.querySelector('[name=mealMetaCalorias]').value),metaProteinasG:m.querySelector('[name=mealMetaProteinasG]').value===''?null:Number(m.querySelector('[name=mealMetaProteinasG]').value),metaCarboidratosG:m.querySelector('[name=mealMetaCarboidratosG]').value===''?null:Number(m.querySelector('[name=mealMetaCarboidratosG]').value),metaGordurasG:m.querySelector('[name=mealMetaGordurasG]').value===''?null:Number(m.querySelector('[name=mealMetaGordurasG]').value),metaFibrasG:m.querySelector('[name=mealMetaFibrasG]').value===''?null:Number(m.querySelector('[name=mealMetaFibrasG]').value),itens:[...m.querySelectorAll('.meal-item-builder')].map(r=>({alimentoId:r.querySelector('[name=foodId]').value,quantidade:Number(r.querySelector('[name=qty]').value||0),unidade:r.querySelector('[name=unit]').value.trim(),quantidadeGramas:Number(r.querySelector('[name=grams]').value||0),observacao:null,substituicoes:[...r.querySelectorAll('.substitution-row')].filter(x=>x.querySelector('[name=subFood]').value).map(x=>({alimentoId:x.querySelector('[name=subFood]').value,quantidade:Number(x.querySelector('[name=subQty]').value||0),unidade:x.querySelector('[name=subUnit]').value.trim(),quantidadeGramas:Number(x.querySelector('[name=subGrams]').value||0),observacao:null}))})).filter(x=>x.alimentoId)}));if(refeicoes.some(x=>!x.nome))throw new Error('Informe o nome de todas as refeições.');if(!refeicoes.some(x=>x.itens.length))throw new Error('Adicione pelo menos um alimento ao plano.');const body={nome:val(f,'nome'),dataInicio:val(f,'dataInicio'),dataFim:val(f,'dataFim'),status:'Ativo',observacoes:val(f,'observacoes'),metaCalorias:dec(f,'metaCalorias'),metaProteinasG:dec(f,'metaProteinasG'),metaCarboidratosG:dec(f,'metaCarboidratosG'),metaGordurasG:dec(f,'metaGordurasG'),metaFibrasG:dec(f,'metaFibrasG'),refeicoes};await api(`/api/pacientes/${p.id}/planos-alimentares`,{method:'POST',body:JSON.stringify(body)});state.patientTab='alimentacao';closeClinicalAction();toast('Plano alimentar salvo.');await loadPatient()}catch(x){toast(x.message,true)}finally{b.disabled=false;b.textContent='Salvar plano alimentar'}};
  }catch(x){toast(x.message,true)}
}


async function openReportForm(p){
  const box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">RELATÓRIO CLÍNICO</span><h2>Novo relatório</h2><p>${esc(p.nome)} • gere um snapshot imutável do período.</p></div><form id="reportForm" class="form-grid clinical-form"><label>Data inicial<input name="inicio" type="date"></label><label>Data final<input name="fim" type="date" value="${todayISO()}"></label><label class="span-2">Título<input name="titulo" value="Relatório de evolução clínica"></label><label class="span-2">Conclusão médica<textarea name="conclusaoMedica" placeholder="Síntese clínica, evolução e próximas orientações..."></textarea></label><div class="span-2 report-preview-box" id="reportPreview"><small>PREVIEW</small><p>Use o botão abaixo para conferir os indicadores antes de gerar.</p></div><div class="span-2 form-actions"><button type="button" class="secondary" id="previewReport">Atualizar preview</button><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Gerar relatório</button></div></form>`;
  $('.clinical-back').onclick=()=>openClinicalActionMenu(p);$('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#reportForm');
  const preview=async()=>{const inicio=val(f,'inicio'),fim=val(f,'fim');const qs=new URLSearchParams();if(inicio)qs.set('inicioUtc',new Date(`${inicio}T00:00:00`).toISOString());if(fim)qs.set('fimUtc',new Date(`${fim}T23:59:59`).toISOString());const d=await api(`/api/pacientes/${p.id}/relatorios/preview?${qs}`),i=d.indicadores||{};$('#reportPreview').innerHTML=`<small>PREVIEW DO SNAPSHOT</small><div class="report-preview-metrics"><span><b>${i.consultas??0}</b> consultas</span><span><b>${i.avaliacoes??0}</b> avaliações</span><span><b>${i.exames??0}</b> exames</span><span><b>${num(i.pesoAtualKg)}</b> kg atual</span><span><b>${num(i.variacaoPesoKg)}</b> kg variação</span><span><b>${(d.resultadosForaDaFaixaInformada||[]).length}</b> fora da faixa</span></div>`};
  $('#previewReport').onclick=()=>preview().catch(x=>toast(x.message,true));
  f.onsubmit=async e=>{e.preventDefault();const b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Gerando...';try{const inicio=val(f,'inicio'),fim=val(f,'fim'),body={dataInicioUtc:inicio?new Date(`${inicio}T00:00:00`).toISOString():null,dataFimUtc:fim?new Date(`${fim}T23:59:59`).toISOString():null,titulo:val(f,'titulo'),conclusaoMedica:val(f,'conclusaoMedica')};const r=await api(`/api/pacientes/${p.id}/relatorios`,{method:'POST',body:JSON.stringify(body)});state.patientTab='relatorios';closeClinicalAction();toast('Relatório clínico gerado.');await loadPatient();setTimeout(()=>openReportHtml(r.id),100)}catch(x){toast(x.message,true)}finally{b.disabled=false;b.textContent='Gerar relatório'}};
  preview().catch(()=>{});
}
async function openReportHtml(id){
  const w=window.open('about:blank','_blank');
  if(w){w.document.write('<p style="font-family:sans-serif;padding:24px">Carregando relatório...</p>');w.document.close()}
  try{const html=await api(`/api/relatorios/${id}/html`);if(!w)throw new Error('O navegador bloqueou a nova janela. Permita pop-ups para visualizar o relatório.');w.document.open();w.document.write(html);w.document.close()}catch(x){if(w)w.close();toast(x.message,true)}
}
function openEditPatientForm(p){
  const box=$('#clinicalActionContent');$('#clinicalActionModal').classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">CADASTRO DO PACIENTE</span><h2>Editar dados</h2><p>Atualize os dados cadastrais sem alterar o histórico clínico.</p></div><form id="editPatientForm" class="form-grid clinical-form"><label class="span-2">Nome completo<input name="nome" value="${esc(p.nome)}" required></label><label>CPF<input name="cpf" value="${esc(p.cpf||'')}"></label><label>Data de nascimento<input name="dataNascimento" type="date" value="${p.dataNascimento?String(p.dataNascimento).slice(0,10):''}"></label><label>Sexo<select name="sexo"><option value="">Não informado</option>${['Masculino','Feminino','Outro'].map(x=>`<option ${p.sexo===x?'selected':''}>${x}</option>`).join('')}</select></label><label>Telefone<input name="telefone" value="${esc(p.telefone||'')}"></label><label>E-mail<input name="email" type="email" value="${esc(p.email||'')}"></label><label>Profissão<input name="profissao" value="${esc(p.profissao||'')}"></label><div class="span-2 form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar alterações</button></div></form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;$('#editPatientForm').onsubmit=async e=>{e.preventDefault();const f=e.target,b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Salvando...';try{const body={nome:val(f,'nome'),cpf:val(f,'cpf'),dataNascimento:val(f,'dataNascimento'),sexo:val(f,'sexo'),telefone:val(f,'telefone'),email:val(f,'email'),profissao:val(f,'profissao')};await api(`/api/pacientes/${p.id}`,{method:'PUT',body:JSON.stringify(body)});closeClinicalAction();toast('Dados do paciente atualizados.');await loadPatient()}catch(x){toast(x.message,true)}finally{b.disabled=false;b.textContent='Salvar alterações'}}
}

function clinical(label,value){return `<div class="clinical-field"><small>${label}</small><p>${esc(value||'—')}</p></div>`}function diaryIcon(t){return ({sono:'☾',hidratacao:'💧',alimentacao:'◉',treino:'↗',sintoma:'!',humor:'☺'}[String(t||'').toLowerCase()]||'•')}
async function loadAgenda(){const iso=todayISO(state.selectedDate),d=await api(`/api/agenda?data=${iso}&offsetMinutos=${state.offset}`),label=new Intl.DateTimeFormat('pt-BR',{weekday:'long',day:'2-digit',month:'long'}).format(state.selectedDate);content.innerHTML=`<div class="agenda-header"><div><h3>Agenda</h3><p>${d.total} atendimento(s) neste dia.</p></div><div class="date-nav"><button class="secondary" id="prevDay">←</button><div class="date-title">${label}</div><button class="secondary" id="nextDay">→</button></div></div><div class="stats-grid agenda-stats">${stat('Total',d.total,'consultas')}${stat('Agendadas',d.agendadas,'aguardando')}${stat('Confirmadas',d.confirmadas,'confirmadas')}${stat('Realizadas',d.realizadas,'concluídas')}${stat('Faltas',d.faltas,'não compareceu')}</div><div class="agenda-list">${d.consultas.length?d.consultas.map(c=>`<div class="agenda-item"><div class="agenda-time">${fmtTime(c.dataHoraLocal)}</div><div class="agenda-bar"></div><div class="agenda-person clickable" data-patient="${c.pacienteId}"><strong>${esc(c.pacienteNome)}</strong><small>${esc(c.motivo||'Consulta')} • ${esc(c.telefone||c.email||'sem contato')}</small></div><div class="agenda-actions"><span class="pill ${esc(c.status)}">${esc(c.status)}</span>${c.status==='Agendada'?`<button class="secondary confirm" data-id="${c.id}">Confirmar</button>`:''}</div></div>`).join(''):'<div class="card empty">Nenhuma consulta neste dia.</div>'}</div>`;$('#prevDay').onclick=()=>{state.selectedDate.setDate(state.selectedDate.getDate()-1);loadAgenda()};$('#nextDay').onclick=()=>{state.selectedDate.setDate(state.selectedDate.getDate()+1);loadAgenda()};$$('[data-patient]').forEach(x=>x.onclick=()=>openPatient(x.dataset.patient));$$('.confirm').forEach(x=>x.onclick=async()=>{try{await api(`/api/agenda/consultas/${x.dataset.id}/status?offsetMinutos=${state.offset}`,{method:'PATCH',body:JSON.stringify({status:'Confirmada'})});toast('Consulta confirmada.');loadAgenda()}catch(e){toast(e.message,true)}})}
if(state.token)showApp();

/* v0.3.27 - edição clínica + agenda operacional */
const __renderPatientTab_v024 = renderPatientTab;
renderPatientTab = function(d){
  __renderPatientTab_v024(d);
  enhanceClinicalRecordEditing(d);
};
function enhanceClinicalRecordEditing(d){
  const box=$('#patientTabContent'); if(!box) return;
  const map={consultas:['Editar consulta',d.consultas,openEditConsulta],anamnese:['Editar anamnese',d.anamneses,openEditAnamnese],avaliacoes:['Editar avaliação',d.avaliacoes,openEditAvaliacao]};
  const cfg=map[state.patientTab]; if(!cfg||!cfg[1]?.length) return;
  const head=box.querySelector('.card-head'); if(!head) return;
  const btn=document.createElement('button'); btn.className='secondary clinical-edit-trigger'; btn.textContent=cfg[0];
  btn.onclick=()=>openRecordChooser(cfg[0],cfg[1],cfg[2]); head.appendChild(btn);
}
function openRecordChooser(title,items,handler){
  const box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">EDIÇÃO CLÍNICA</span><h2>${esc(title)}</h2><p>Escolha o registro que deseja alterar.</p></div><div class="record-choice-list">${items.map(x=>`<button class="record-choice" data-edit-id="${x.id}"><b>${fmtDateTime(x.dataHoraUtc||x.dataUtc)}</b><span>${esc(x.motivo||x.objetivoAcompanhamento||('Avaliação • '+(x.pesoKg!=null?num(x.pesoKg)+' kg':'sem peso')))}</span></button>`).join('')}</div><div class="form-actions"><button class="secondary" type="button" data-close-clinical-form>Cancelar</button></div>`;
  $('#clinicalActionModal').classList.remove('hidden');
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  $$('.record-choice').forEach(b=>b.onclick=()=>handler(b.dataset.editId));
}
function statusOptions(current){return ['Agendada','Confirmada','Realizada','Cancelada','Faltou'].map(x=>`<option ${x===current?'selected':''}>${x}</option>`).join('')}
async function openEditConsulta(id){
  const x=await api(`/api/consultas/${id}`),box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">EDIÇÃO CLÍNICA</span><h2>Editar consulta</h2><p>${fmtDateTime(x.dataHoraUtc)}</p></div><form id="editConsultaForm" class="form-grid clinical-form">${field('Data e hora','dataHora','datetime-local',`value="${localDateTimeValue(new Date(x.dataHoraUtc))}" required`)}<label>Status<select name="status">${statusOptions(x.status)}</select></label>${field('Motivo','motivo','text',`value="${esc(x.motivo||'')}"`)}${area('Queixa principal','queixaPrincipal')}${area('Evolução','evolucao')}${area('Conduta','conduta')}${area('Orientações','orientacoes')}<div class="span-2 form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar alterações</button></div></form>`;
  const f=$('#editConsultaForm'); f.elements.queixaPrincipal.value=x.queixaPrincipal||'';f.elements.evolucao.value=x.evolucao||'';f.elements.conduta.value=x.conduta||'';f.elements.orientacoes.value=x.orientacoes||'';
  $('.clinical-back').onclick=()=>{closeClinicalAction();state.patientTab='consultas';loadPatient()};$('[data-close-clinical-form]').onclick=closeClinicalAction;
  f.onsubmit=async e=>{e.preventDefault();try{await api(`/api/consultas/${id}`,{method:'PUT',body:JSON.stringify({dataHoraUtc:new Date(val(f,'dataHora')).toISOString(),motivo:val(f,'motivo'),queixaPrincipal:val(f,'queixaPrincipal'),evolucao:val(f,'evolucao'),conduta:val(f,'conduta'),orientacoes:val(f,'orientacoes'),status:val(f,'status')})});closeClinicalAction();toast('Consulta atualizada.');state.patientTab='consultas';await loadPatient()}catch(err){toast(err.message,true)}};
}
async function openEditAnamnese(id){
  const x=await api(`/api/anamneses/${id}`),box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">EDIÇÃO CLÍNICA</span><h2>Editar anamnese</h2><p>${fmtDate(x.dataUtc)}</p></div><form id="editAnamneseForm" class="form-grid clinical-form">${field('Data','dataUtc','datetime-local',`value="${localDateTimeValue(new Date(x.dataUtc))}"`)}${field('Objetivo','objetivoAcompanhamento','text',`value="${esc(x.objetivoAcompanhamento||'')}"`)}${area('Histórico de doenças','historicoDoencas')}${area('Histórico familiar','historicoFamiliar')}${area('Cirurgias','cirurgias')}${area('Alergias','alergias')}${area('Medicamentos','medicamentos')}${area('Suplementos','suplementos')}${field('Horas médias de sono','sonoHorasMedia','number',`step="0.1" value="${x.sonoHorasMedia??''}"`)}${field('Qualidade do sono','sonoQualidade','text',`value="${esc(x.sonoQualidade||'')}"`)}${field('Estresse 0-10','estresseNivel','number',`min="0" max="10" value="${x.estresseNivel??''}"`)}${field('Atividade física','atividadeFisica','text',`value="${esc(x.atividadeFisica||'')}"`)}${field('Dias de atividade/semana','atividadeFisicaDiasSemana','number',`min="0" max="7" value="${x.atividadeFisicaDiasSemana??''}"`)}${field('Água L/dia','aguaLitrosDia','number',`step="0.1" value="${x.aguaLitrosDia??''}"`)}${area('Hábito intestinal','habitoIntestinal')}${area('Observações','observacoes')}<div class="span-2 form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar alterações</button></div></form>`;
  const f=$('#editAnamneseForm');['historicoDoencas','historicoFamiliar','cirurgias','alergias','medicamentos','suplementos','habitoIntestinal','observacoes'].forEach(k=>f.elements[k].value=x[k]||'');
  $('.clinical-back').onclick=()=>{closeClinicalAction();state.patientTab='anamnese';loadPatient()};$('[data-close-clinical-form]').onclick=closeClinicalAction;
  f.onsubmit=async e=>{e.preventDefault();try{const body={consultaId:x.consultaId||null,dataUtc:new Date(val(f,'dataUtc')).toISOString(),objetivoAcompanhamento:val(f,'objetivoAcompanhamento'),historicoDoencas:val(f,'historicoDoencas'),historicoFamiliar:val(f,'historicoFamiliar'),cirurgias:val(f,'cirurgias'),alergias:val(f,'alergias'),medicamentos:val(f,'medicamentos'),suplementos:val(f,'suplementos'),tabagismo:x.tabagismo||null,etilismo:x.etilismo||null,sonoHorasMedia:dec(f,'sonoHorasMedia'),sonoQualidade:val(f,'sonoQualidade'),despertaDuranteNoite:x.despertaDuranteNoite,estresseNivel:integer(f,'estresseNivel'),atividadeFisica:val(f,'atividadeFisica'),atividadeFisicaDiasSemana:integer(f,'atividadeFisicaDiasSemana'),habitoIntestinal:val(f,'habitoIntestinal'),aguaLitrosDia:dec(f,'aguaLitrosDia'),observacoes:val(f,'observacoes'),respostasPersonalizadas:(x.respostasPersonalizadas||[]).map(r=>({perguntaId:r.perguntaId,resposta:r.resposta||null}))};await api(`/api/anamneses/${id}`,{method:'PUT',body:JSON.stringify(body)});closeClinicalAction();toast('Anamnese atualizada.');state.patientTab='anamnese';await loadPatient()}catch(err){toast(err.message,true)}};
}
async function openEditAvaliacao(id){
  const x=await api(`/api/avaliacoes/${id}`),box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">EDIÇÃO CLÍNICA</span><h2>Editar avaliação</h2><p>${fmtDate(x.dataUtc)}</p></div><form id="editAvaliacaoForm" class="form-grid clinical-form">${field('Data','dataUtc','datetime-local',`value="${localDateTimeValue(new Date(x.dataUtc))}"`)}${field('Peso (kg)','pesoKg','number',`step="0.01" value="${x.pesoKg??''}"`)}${field('Altura (m)','alturaM','number',`step="0.01" value="${x.alturaM??''}"`)}${field('Gordura (%)','percentualGordura','number',`step="0.01" value="${x.percentualGordura??''}"`)}${field('Massa magra (kg)','massaMagraKg','number',`step="0.01" value="${x.massaMagraKg??''}"`)}${field('Massa gorda (kg)','massaGordaKg','number',`step="0.01" value="${x.massaGordaKg??''}"`)}${field('Cintura (cm)','cinturaCm','number',`step="0.01" value="${x.cinturaCm??''}"`)}${field('Abdômen (cm)','abdomenCm','number',`step="0.01" value="${x.abdomenCm??''}"`)}${field('Quadril (cm)','quadrilCm','number',`step="0.01" value="${x.quadrilCm??''}"`)}${field('Pressão sistólica','pressaoSistolica','number',`value="${x.pressaoSistolica??''}"`)}${field('Pressão diastólica','pressaoDiastolica','number',`value="${x.pressaoDiastolica??''}"`)}${field('Frequência cardíaca','frequenciaCardiaca','number',`value="${x.frequenciaCardiaca??''}"`)}<div class="span-2 form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar alterações</button></div></form>`;
  const f=$('#editAvaliacaoForm');$('.clinical-back').onclick=()=>{closeClinicalAction();state.patientTab='avaliacoes';loadPatient()};$('[data-close-clinical-form]').onclick=closeClinicalAction;
  f.onsubmit=async e=>{e.preventDefault();try{const body={consultaId:x.consultaId||null,dataUtc:new Date(val(f,'dataUtc')).toISOString(),pesoKg:dec(f,'pesoKg'),alturaM:dec(f,'alturaM'),percentualGordura:dec(f,'percentualGordura'),massaMagraKg:dec(f,'massaMagraKg'),massaGordaKg:dec(f,'massaGordaKg'),cinturaCm:dec(f,'cinturaCm'),abdomenCm:dec(f,'abdomenCm'),quadrilCm:dec(f,'quadrilCm'),pressaoSistolica:integer(f,'pressaoSistolica'),pressaoDiastolica:integer(f,'pressaoDiastolica'),frequenciaCardiaca:integer(f,'frequenciaCardiaca')};await api(`/api/avaliacoes/${id}`,{method:'PUT',body:JSON.stringify(body)});closeClinicalAction();toast('Avaliação atualizada.');state.patientTab='avaliacoes';await loadPatient()}catch(err){toast(err.message,true)}};
}
function agendaStatusActions(c){return `<div class="agenda-quick-actions"><button class="secondary agenda-status" data-id="${c.id}" data-status="Confirmada">Confirmar</button><button class="secondary agenda-status" data-id="${c.id}" data-status="Realizada">Realizada</button><button class="secondary agenda-status" data-id="${c.id}" data-status="Faltou">Falta</button><button class="secondary agenda-status danger-soft" data-id="${c.id}" data-status="Cancelada">Cancelar</button><button class="secondary agenda-reschedule" data-id="${c.id}" data-current="${esc(c.dataHoraLocal)}">Reagendar</button></div>`}
loadAgenda = async function(){
  const iso=todayISO(state.selectedDate),d=await api(`/api/agenda?data=${iso}&offsetMinutos=${state.offset}`),label=new Intl.DateTimeFormat('pt-BR',{weekday:'long',day:'2-digit',month:'long'}).format(state.selectedDate);
  content.innerHTML=`<div class="agenda-header"><div><h3>Agenda operacional</h3><p>${d.total} atendimento(s) neste dia.</p></div><div class="date-nav"><button class="secondary" id="prevDay">←</button><div class="date-title">${label}</div><button class="secondary" id="nextDay">→</button></div></div><div class="stats-grid agenda-stats">${stat('Total',d.total,'consultas')}${stat('Agendadas',d.agendadas,'aguardando')}${stat('Confirmadas',d.confirmadas,'confirmadas')}${stat('Realizadas',d.realizadas,'concluídas')}${stat('Faltas',d.faltas,'não compareceu')}</div><div class="agenda-list">${d.consultas.length?d.consultas.map(c=>`<div class="agenda-item agenda-item-operational"><div class="agenda-time">${fmtTime(c.dataHoraLocal)}</div><div class="agenda-bar"></div><div class="agenda-person clickable" data-patient="${c.pacienteId}"><strong>${esc(c.pacienteNome)}</strong><small>${esc(c.motivo||'Consulta')} • ${esc(c.telefone||c.email||'sem contato')}</small></div><div class="agenda-actions"><span class="pill ${esc(c.status)}">${esc(c.status)}</span>${agendaStatusActions(c)}</div></div>`).join(''):'<div class="card empty">Nenhuma consulta neste dia.</div>'}</div>`;
  $('#prevDay').onclick=()=>{state.selectedDate.setDate(state.selectedDate.getDate()-1);loadAgenda()};$('#nextDay').onclick=()=>{state.selectedDate.setDate(state.selectedDate.getDate()+1);loadAgenda()};$$('[data-patient]').forEach(x=>x.onclick=()=>openPatient(x.dataset.patient));
  $$('.agenda-status').forEach(b=>b.onclick=async()=>{try{await api(`/api/agenda/consultas/${b.dataset.id}/status?offsetMinutos=${state.offset}`,{method:'PATCH',body:JSON.stringify({status:b.dataset.status})});toast(`Consulta marcada como ${b.dataset.status}.`);await loadAgenda()}catch(e){toast(e.message,true)}});
  $$('.agenda-reschedule').forEach(b=>b.onclick=()=>openRescheduleForm(b.dataset.id,b.dataset.current));
};
function openRescheduleForm(id,current){
  const box=$('#clinicalActionContent'),currentDate=current?new Date(current):new Date();
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">AGENDA</span><h2>Reagendar consulta</h2><p>Escolha a nova data e horário no seu fuso local.</p></div><form id="rescheduleForm" class="form-grid clinical-form">${field('Nova data e hora','dataHora','datetime-local',`value="${localDateTimeValue(currentDate)}" required`)}<div class="span-2 form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Reagendar</button></div></form>`;
  $('#clinicalActionModal').classList.remove('hidden');$('[data-close-clinical-form]').onclick=closeClinicalAction;
  $('#rescheduleForm').onsubmit=async e=>{e.preventDefault();const f=e.target;try{const local=val(f,'dataHora');await api(`/api/agenda/consultas/${id}/reagendar`,{method:'PATCH',body:JSON.stringify({dataHoraLocal:local,offsetMinutos:state.offset})});closeClinicalAction();toast('Consulta reagendada.');await loadAgenda()}catch(err){toast(err.message,true)}};
}


// ===== v0.3.27 — Catálogos + Configurações =====
(function () {
  const hp = window.HealthPlatform || (window.HealthPlatform = {});

  function token() {
    return localStorage.getItem("hp_token") || localStorage.getItem("token") || "";
  }

  async function api(path, options = {}) {
    const headers = Object.assign(
      { "Content-Type": "application/json" },
      options.headers || {}
    );
    const t = token();
    if (t) headers.Authorization = `Bearer ${t}`;
    const res = await fetch(path, Object.assign({}, options, { headers }));
    if (!res.ok) {
      let detail = "";
      try { detail = await res.text(); } catch {}
      throw new Error(detail || `HTTP ${res.status}`);
    }
    if (res.status === 204) return null;
    const ct = res.headers.get("content-type") || "";
    return ct.includes("application/json") ? res.json() : res.text();
  }

  function esc(v) {
    return String(v ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;");
  }

  function mount() {
    return document.querySelector("#content, #app-content, main") || document.body;
  }

  async function renderConfiguracoes() {
    const host = mount();
    host.innerHTML = `
      <section class="page">
        <div class="page-header">
          <div>
            <div class="eyebrow">Administração</div>
            <h1>Configurações</h1>
            <p class="muted">Gerencie os catálogos usados no atendimento e veja os dados do consultório.</p>
          </div>
        </div>

        <div class="settings-grid">
          <article class="card">
            <div class="card-head"><h3>Consultório e usuário</h3></div>
            <div id="cfg-resumo" class="stack-sm"><div class="skeleton-line"></div></div>
          </article>

          <article class="card span-2">
            <div class="card-head">
              <div><h3>Catálogo de alimentos</h3><p class="muted">Base nutricional usada nos planos alimentares.</p></div>
              <button class="btn btn-primary" id="cfg-add-alimento">+ Alimento</button>
            </div>
            <div id="cfg-alimentos"></div>
          </article>

          <article class="card span-2">
            <div class="card-head">
              <div><h3>Marcadores laboratoriais</h3><p class="muted">Ex.: Glicemia, LDL, HDL, TSH.</p></div>
              <button class="btn btn-primary" id="cfg-add-marcador">+ Marcador</button>
            </div>
            <div id="cfg-marcadores"></div>
          </article>

          <article class="card span-2">
            <div class="card-head">
              <div><h3>Perguntas personalizadas</h3><p class="muted">Complementos reutilizáveis da anamnese.</p></div>
              <button class="btn btn-primary" id="cfg-add-pergunta">+ Pergunta</button>
            </div>
            <div id="cfg-perguntas"></div>
          </article>
        </div>
      </section>

      <div id="cfg-modal-root"></div>
    `;

    await Promise.allSettled([
      loadResumo(),
      loadAlimentos(),
      loadMarcadores(),
      loadPerguntas()
    ]);

    document.querySelector("#cfg-add-alimento")?.addEventListener("click", () => modalAlimento());
    document.querySelector("#cfg-add-marcador")?.addEventListener("click", () => modalMarcador());
    document.querySelector("#cfg-add-pergunta")?.addEventListener("click", () => modalPergunta());
  }

  async function loadResumo() {
    const el = document.querySelector("#cfg-resumo");
    try {
      const d = await api("/api/configuracoes/resumo");
      el.innerHTML = `
        <div><strong>Organização</strong><div>${esc(d.organizacao?.nome || "—")}</div></div>
        <div><strong>Usuário</strong><div>${esc(d.usuario?.nome || "—")} · ${esc(d.usuario?.email || "")}</div></div>
        <div><strong>Perfil</strong><div>${esc(d.usuario?.tipoUsuario ?? "—")}</div></div>
        <div><strong>Registro</strong><div>${esc(d.profissional?.registroProfissional || "Não configurado")}</div></div>
        <div><strong>Especialidade</strong><div>${esc(d.profissional?.especialidade || "Não configurada")}</div></div>
      `;
    } catch (e) {
      el.innerHTML = `<div class="empty-state">Não foi possível carregar as configurações.</div>`;
    }
  }

  async function loadAlimentos() {
    const el = document.querySelector("#cfg-alimentos");
    try {
      const d = await api("/api/alimentos?incluirInativos=true");
      const itens = Array.isArray(d) ? d : (d.items || d.data || []);
      el.innerHTML = itens.length ? `
        <div class="catalog-table">
          ${itens.map(x => `
            <div class="catalog-row">
              <div><strong>${esc(x.nome)}</strong><small>${esc(x.categoria || "")}</small></div>
              <div>${Number(x.caloriasPor100g ?? 0).toFixed(1)} kcal</div>
              <div>P ${Number(x.proteinasPor100g ?? 0).toFixed(1)} g</div>
              <div>C ${Number(x.carboidratosPor100g ?? 0).toFixed(1)} g</div>
              <div>G ${Number(x.gordurasPor100g ?? 0).toFixed(1)} g</div>
            </div>
          `).join("")}
        </div>` : `<div class="empty-state">Nenhum alimento cadastrado.</div>`;
    } catch {
      el.innerHTML = `<div class="empty-state">Falha ao carregar alimentos.</div>`;
    }
  }

  async function loadMarcadores() {
    const el = document.querySelector("#cfg-marcadores");
    try {
      const d = await api("/api/exames/marcadores?incluirInativos=true");
      const itens = Array.isArray(d) ? d : (d.items || d.data || []);
      el.innerHTML = itens.length ? `
        <div class="catalog-table">
          ${itens.map(x => `
            <div class="catalog-row">
              <div><strong>${esc(x.nome)}</strong><small>${esc(x.categoria || "")}</small></div>
              <div>${esc(x.unidadePadrao || x.unidade || "—")}</div>
              <div class="badge ${x.ativo === false ? "badge-muted" : "badge-ok"}">${x.ativo === false ? "Inativo" : "Ativo"}</div>
            </div>
          `).join("")}
        </div>` : `<div class="empty-state">Nenhum marcador cadastrado.</div>`;
    } catch {
      el.innerHTML = `<div class="empty-state">Falha ao carregar marcadores.</div>`;
    }
  }

  async function loadPerguntas() {
    const el = document.querySelector("#cfg-perguntas");
    try {
      const d = await api("/api/anamnese/perguntas");
      const itens = Array.isArray(d) ? d : (d.items || d.data || []);
      el.innerHTML = itens.length ? `
        <div class="catalog-table">
          ${itens.map(x => `
            <div class="catalog-row">
              <div><strong>${esc(x.texto || x.pergunta || x.titulo)}</strong><small>${esc(x.tipoResposta || "")}</small></div>
              <div>Personalizada</div>
              <div class="badge ${x.ativa === false ? "badge-muted" : "badge-ok"}">${x.ativa === false ? "Inativa" : "Ativa"}</div>
            </div>
          `).join("")}
        </div>` : `<div class="empty-state">Nenhuma pergunta personalizada.</div>`;
    } catch {
      el.innerHTML = `<div class="empty-state">Falha ao carregar perguntas.</div>`;
    }
  }

  function showModal(title, body, onSave) {
    const root = document.querySelector("#cfg-modal-root");
    root.innerHTML = `
      <div class="modal-backdrop">
        <div class="modal-card modal-lg">
          <div class="modal-header"><h3>${esc(title)}</h3><button class="icon-btn" data-close>×</button></div>
          <form id="cfg-form">
            <div class="modal-body">${body}</div>
            <div class="modal-footer">
              <button type="button" class="btn" data-close>Cancelar</button>
              <button class="btn btn-primary" type="submit">Salvar</button>
            </div>
          </form>
        </div>
      </div>`;
    root.querySelectorAll("[data-close]").forEach(b => b.addEventListener("click", () => root.innerHTML = ""));
    root.querySelector("#cfg-form").addEventListener("submit", async (ev) => {
      ev.preventDefault();
      const btn = ev.submitter;
      if (btn) btn.disabled = true;
      try {
        await onSave(new FormData(ev.currentTarget));
        root.innerHTML = "";
      } catch (e) {
        alert(e.message || "Não foi possível salvar.");
      } finally {
        if (btn) btn.disabled = false;
      }
    });
  }

  function modalAlimento() {
    showModal("Novo alimento", `
      <div class="form-grid two">
        <label>Nome<input name="nome" required></label>
        <label>Categoria<input name="categoria"></label>
        <label>Calorias / 100 g<input name="calorias" type="number" step="0.01" required></label>
        <label>Proteína / 100 g<input name="proteina" type="number" step="0.01"></label>
        <label>Carboidrato / 100 g<input name="carboidrato" type="number" step="0.01"></label>
        <label>Gordura / 100 g<input name="gordura" type="number" step="0.01"></label>
        <label>Fibra / 100 g<input name="fibra" type="number" step="0.01"></label>
      </div>
    `, async f => {
      await api("/api/alimentos", { method: "POST", body: JSON.stringify({
        nome: f.get("nome"),
        categoria: f.get("categoria") || null,
        caloriasPor100g: Number(f.get("calorias") || 0),
        proteinasPor100g: Number(f.get("proteina") || 0),
        carboidratosPor100g: Number(f.get("carboidrato") || 0),
        gordurasPor100g: Number(f.get("gordura") || 0),
        fibrasPor100g: Number(f.get("fibra") || 0)
      })});
      await loadAlimentos();
    });
  }

  function modalMarcador() {
    showModal("Novo marcador laboratorial", `
      <div class="form-grid two">
        <label>Nome<input name="nome" required></label>
        <label>Categoria<input name="categoria"></label>
        <label>Unidade padrão<input name="unidade"></label>
      </div>
    `, async f => {
      await api("/api/exames/marcadores", { method: "POST", body: JSON.stringify({
        nome: f.get("nome"),
        categoria: f.get("categoria") || null,
        unidadePadrao: f.get("unidade") || null
      })});
      await loadMarcadores();
    });
  }

  function modalPergunta() {
    showModal("Nova pergunta de anamnese", `
      <div class="form-grid">
        <label>Pergunta<textarea name="texto" rows="3" required></textarea></label>
        <div class="form-grid two">
          <label>Tipo
            <select name="tipo">
              <option value="Texto">Texto</option>
              <option value="Numero">Número</option>
              <option value="SimNao">Sim / Não</option>
              <option value="Escala">Escala</option>
              <option value="Opcao">Opções</option>
            </select>
          </label>
          </div>
        <label>Opções (uma por linha)<textarea name="opcoes" rows="4"></textarea></label>
      </div>
    `, async f => {
      await api("/api/anamnese/perguntas", { method: "POST", body: JSON.stringify({
        texto: f.get("texto"),
        tipoResposta: f.get("tipo"),
        opcoes: String(f.get("opcoes") || "").split(/\r?\n/).map(x => x.trim()).filter(Boolean)
      })});
      await loadPerguntas();
    });
  }

  // Hook existing router/navigation without breaking prior app.
  document.addEventListener("click", (ev) => {
    const btn = ev.target.closest('[data-route="configuracoes"]');
    if (!btn) return;
    ev.preventDefault();
    document.querySelectorAll(".nav-item").forEach(x => x.classList.remove("active"));
    btn.classList.add("active");
    renderConfiguracoes();
  });

  hp.renderConfiguracoes = renderConfiguracoes;
})();


// ===== v0.3.27 — Configurações editáveis + catálogos completos =====
(function () {
  function tkn() { return localStorage.getItem("hp_token") || localStorage.getItem("token") || ""; }
  async function req(path, options = {}) {
    const headers = Object.assign({ "Content-Type": "application/json" }, options.headers || {});
    const t = tkn(); if (t) headers.Authorization = `Bearer ${t}`;
    const res = await fetch(path, Object.assign({}, options, { headers }));
    if (!res.ok) {
      let body = ""; try { body = await res.text(); } catch {}
      throw new Error(body || `HTTP ${res.status}`);
    }
    if (res.status === 204) return null;
    const ct = res.headers.get("content-type") || "";
    return ct.includes("application/json") ? res.json() : res.text();
  }
  const esc2 = v => String(v ?? "").replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;");

  function editModal(title, html, save) {
    let root = document.querySelector("#cfg-modal-root");
    if (!root) { root = document.createElement("div"); root.id = "cfg-modal-root"; document.body.appendChild(root); }
    root.innerHTML = `<div class="modal-backdrop"><div class="modal-card modal-lg">
      <div class="modal-header"><h3>${esc2(title)}</h3><button class="icon-btn" data-x>×</button></div>
      <form id="v027-form"><div class="modal-body">${html}</div><div class="modal-footer">
        <button type="button" class="btn" data-x>Cancelar</button><button class="btn btn-primary">Salvar</button>
      </div></form></div></div>`;
    root.querySelectorAll("[data-x]").forEach(x => x.onclick = () => root.innerHTML = "");
    root.querySelector("#v027-form").onsubmit = async ev => {
      ev.preventDefault();
      try { await save(new FormData(ev.currentTarget)); root.innerHTML = ""; window.HealthPlatform?.renderConfiguracoes?.(); }
      catch(e) { alert(e.message || "Falha ao salvar"); }
    };
  }

  async function addEditButtons() {
    if (!location.hash.includes("configuracoes") && !document.querySelector("#cfg-resumo")) return;

    let data;
    try { data = await req("/api/configuracoes/resumo"); } catch { return; }

    const resumo = document.querySelector("#cfg-resumo");
    if (resumo && !document.querySelector("#v027-edit-config")) {
      const wrap = document.createElement("div");
      wrap.className = "cfg-actions";
      wrap.innerHTML = `<button class="btn" id="v027-edit-org">Editar consultório</button>
                        <button class="btn" id="v027-edit-prof">Editar profissional</button>`;
      resumo.appendChild(wrap);

      document.querySelector("#v027-edit-org").onclick = () => editModal("Editar consultório", `
        <div class="form-grid two">
          <label>Nome<input name="nome" required value="${esc2(data.organizacao?.nome)}"></label>
          <label>CNPJ<input name="cnpj" value="${esc2(data.organizacao?.cnpj)}"></label>
        </div>`, async f => {
          await req("/api/configuracoes/organizacao", { method:"PUT", body:JSON.stringify({ nome:f.get("nome"), cnpj:f.get("cnpj") || null }) });
        });

      document.querySelector("#v027-edit-prof").onclick = () => editModal("Editar profissional", `
        <div class="form-grid two">
          <label>Nome<input name="nome" required value="${esc2(data.usuario?.nome)}"></label>
          <label>Registro profissional<input name="registro" value="${esc2(data.profissional?.registroProfissional)}"></label>
          <label>Especialidade<input name="especialidade" value="${esc2(data.profissional?.especialidade)}"></label>
        </div>`, async f => {
          await req("/api/profissionais/me", { method:"PUT", body:JSON.stringify({ nome:f.get("nome"), registroProfissional:f.get("registro"), especialidade:f.get("especialidade") || null }) });
        });
    }
  }

  // Delegate catalog edit/deactivate actions.
  document.addEventListener("click", async ev => {
    const row = ev.target.closest(".catalog-row[data-kind][data-id]");
    if (!row) return;
    const kind = row.dataset.kind, id = row.dataset.id;

    if (ev.target.closest("[data-toggle]")) {
      const ativo = ev.target.closest("[data-toggle]").dataset.toggle === "true";
      const baseRoute = kind === "alimentos"
        ? `/api/alimentos/${id}`
        : kind === "marcadores"
          ? `/api/exames/marcadores/${id}`
          : `/api/anamnese/perguntas/${id}`;
      if (ativo) await req(`${baseRoute}/reativar`, { method:"POST" });
      else await req(baseRoute, { method:"DELETE" });
      window.HealthPlatform?.renderConfiguracoes?.(); return;
    }
    if (!ev.target.closest("[data-edit]")) return;

    if (kind === "alimentos") {
      const x = JSON.parse(row.dataset.json);
      editModal("Editar alimento", `
        <div class="form-grid two">
          <label>Nome<input name="nome" required value="${esc2(x.nome)}"></label>
          <label>Categoria<input name="categoria" value="${esc2(x.categoria)}"></label>
          <label>Calorias / 100 g<input name="calorias" type="number" step="0.01" value="${x.caloriasPor100g ?? 0}"></label>
          <label>Proteína / 100 g<input name="proteina" type="number" step="0.01" value="${x.proteinasPor100g ?? 0}"></label>
          <label>Carboidrato / 100 g<input name="carboidrato" type="number" step="0.01" value="${x.carboidratosPor100g ?? 0}"></label>
          <label>Gordura / 100 g<input name="gordura" type="number" step="0.01" value="${x.gordurasPor100g ?? 0}"></label>
          <label>Fibra / 100 g<input name="fibra" type="number" step="0.01" value="${x.fibrasPor100g ?? 0}"></label>
          <label class="check-line"><input name="ativo" type="checkbox" ${x.ativo !== false ? "checked":""}> Ativo</label>
        </div>`, async f => req(`/api/alimentos/${id}`, { method:"PUT", body:JSON.stringify({
          nome:f.get("nome"), categoria:f.get("categoria")||null, caloriasPor100g:+f.get("calorias")||0,
          proteinasPor100g:+f.get("proteina")||0, carboidratosPor100g:+f.get("carboidrato")||0,
          gordurasPor100g:+f.get("gordura")||0, fibrasPor100g:+f.get("fibra")||0
        })}));
    } else if (kind === "marcadores") {
      const x = JSON.parse(row.dataset.json);
      editModal("Editar marcador", `
        <div class="form-grid two">
          <label>Nome<input name="nome" required value="${esc2(x.nome)}"></label>
          <label>Categoria<input name="categoria" value="${esc2(x.categoria)}"></label>
          <label>Unidade padrão<input name="unidade" value="${esc2(x.unidadePadrao)}"></label>
          <label class="check-line"><input name="ativo" type="checkbox" ${x.ativo !== false ? "checked":""}> Ativo</label>
        </div>`, async f => req(`/api/exames/marcadores/${id}`, { method:"PUT", body:JSON.stringify({
          nome:f.get("nome"), categoria:f.get("categoria")||null, unidadePadrao:f.get("unidade")||null
        })}));
    } else if (kind === "perguntas") {
      const x = JSON.parse(row.dataset.json);
      editModal("Editar pergunta", `
        <div class="form-grid">
          <label>Pergunta<textarea name="texto" rows="3" required>${esc2(x.texto)}</textarea></label>
          <div class="form-grid two">
            <label>Tipo<input name="tipo" value="${esc2(x.tipoResposta || "Texto")}"></label>
            </div>
          </div>`, async f => req(`/api/anamnese/perguntas/${id}`, { method:"PUT", body:JSON.stringify({
          texto:f.get("texto"), tipoResposta:f.get("tipo")||"Texto", opcoes:[], ordem:null
        })}));
    }
  });

  // Enrich catalog rows after render.
  async function enrichCatalogs() {
    const configsOpen = document.querySelector("#cfg-resumo");
    if (!configsOpen) return;
    try {
      const a = await req("/api/alimentos?incluirInativos=true");
      const m = await req("/api/exames/marcadores?incluirInativos=true");
      const p = await req("/api/anamnese/perguntas");
      const sets = [
        ["#cfg-alimentos", "alimentos", Array.isArray(a)?a:(a.items||a.data||[])],
        ["#cfg-marcadores", "marcadores", Array.isArray(m)?m:(m.items||m.data||[])],
        ["#cfg-perguntas", "perguntas", Array.isArray(p)?p:(p.items||p.data||[])]
      ];
      for (const [sel, kind, items] of sets) {
        const host = document.querySelector(sel);
        if (!host) continue;
        const rows = [...host.querySelectorAll(".catalog-row")];
        rows.forEach((row, i) => {
          const x = items[i]; if (!x) return;
          row.dataset.kind = kind; row.dataset.id = x.id; row.dataset.json = JSON.stringify(x);
          const actions = document.createElement("div");
          actions.className = "catalog-actions";
          const active = (x.ativo ?? x.ativa) !== false;
          actions.innerHTML = `<button class="btn btn-sm" data-edit>Editar</button>
            <button class="btn btn-sm" data-toggle="${!active}">${active ? "Inativar" : "Ativar"}</button>`;
          row.appendChild(actions);
        });
      }
    } catch {}
  }

  const orig = window.HealthPlatform?.renderConfiguracoes;
  if (orig) {
    window.HealthPlatform.renderConfiguracoes = async function () {
      await orig();
      await addEditButtons();
      await enrichCatalogs();
    };
  }

  document.addEventListener("click", ev => {
    if (ev.target.closest('[data-route="configuracoes"]')) setTimeout(async()=>{await addEditButtons(); await enrichCatalogs();}, 150);
  });
})();



// ===== v0.3.27 — acesso e portal real do paciente =====
async function openPatientAccess(p){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">PORTAL DO PACIENTE</span><h2>${esc(p.nome)}</h2><p>Carregando situação do acesso...</p></div>`;
  try{
    const st=await api(`/api/pacientes/${p.id}/acesso`);
    box.innerHTML=`<div class="modal-heading"><span class="eyebrow">PORTAL DO PACIENTE</span><h2>Acesso de ${esc(p.nome)}</h2><p>${st.possuiAcesso?(st.ativado?'Acesso ativo.':'Convite criado, aguardando ativação.'):'Este paciente ainda não possui usuário de portal.'}</p></div>
      <div class="access-status-card">
        <div>${info('E-mail',st.email||p.email)}</div>
        <span class="pill ${st.ativado?'Ativa':'Agendada'}">${st.ativado?'Ativo':st.possuiAcesso?'Pendente':'Sem acesso'}</span>
      </div>
      <form id="patientAccessForm" class="form-grid clinical-form">
        <label class="span-2">E-mail de acesso<input name="email" type="email" value="${esc(st.email||p.email||'')}" required></label>
        <div class="span-2 form-actions">
          <button type="button" class="secondary" data-close-clinical-form>Fechar</button>
          ${st.possuiAcesso?'<button type="button" class="secondary danger-soft" id="revokePatientAccess">Revogar</button>':''}
          <button class="primary" type="submit">${st.possuiAcesso?'Gerar novo convite':'Liberar acesso'}</button>
        </div>
      </form>
      <div id="patientInviteResult"></div>`;
    $('[data-close-clinical-form]').onclick=closeClinicalAction;
    $('#patientAccessForm').onsubmit=async e=>{
      e.preventDefault();
      try{
        const email=e.target.elements.email.value.trim();
        const invite=await api(`/api/pacientes/${p.id}/acesso`,{method:'POST',body:JSON.stringify({email})});
        const link=`${location.origin}/?ativarPaciente=1&email=${encodeURIComponent(invite.email)}&token=${encodeURIComponent(invite.activationToken)}`;
        $('#patientInviteResult').innerHTML=`<div class="invite-result"><strong>Convite gerado</strong><p>Envie este link ao paciente. Neste MVP local ele pode ser copiado manualmente.</p><textarea id="patientInviteLink" readonly>${esc(link)}</textarea><button class="secondary" id="copyPatientInvite">Copiar link</button></div>`;
        $('#copyPatientInvite').onclick=async()=>{await navigator.clipboard.writeText(link);toast('Link de ativação copiado.')};
      }catch(err){toast(err.message,true)}
    };
    if($('#revokePatientAccess'))$('#revokePatientAccess').onclick=async()=>{
      if(!confirm('Revogar o acesso deste paciente?'))return;
      try{await api(`/api/pacientes/${p.id}/acesso`,{method:'DELETE'});toast('Acesso revogado.');openPatientAccess(p)}catch(err){toast(err.message,true)}
    };
  }catch(err){box.innerHTML=`<div class="card empty">${esc(err.message)}</div>`}
}

async function loadMyPatientPortal(){
  const host=$('#patientPortalContent');
  host.innerHTML='<div class="card"><div class="skeleton" style="height:180px"></div></div>';
  const d=await api(`/api/portal/me/home?data=${todayISO()}`);
  const e=d.evolucaoCorporal||{},plano=d.planoAlimentarAtual,prox=d.proximaConsulta;
  host.innerHTML=`<div class="patient-mobile-home">
    <section class="patient-welcome">
      <div><span class="eyebrow">MEU ACOMPANHAMENTO</span><h1>Olá, ${esc((d.paciente?.nome||state.user?.nome||'Paciente').split(' ')[0])} 👋</h1><p>Aqui está o resumo do seu dia.</p></div>
      <div class="patient-date">${new Intl.DateTimeFormat('pt-BR',{weekday:'long',day:'2-digit',month:'long'}).format(new Date())}</div>
    </section>

    ${prox?`<section class="patient-next-card"><span>PRÓXIMA CONSULTA</span><strong>${fmtDateTime(prox.dataHoraUtc)}</strong><p>${esc(prox.profissionalNome)}${prox.motivo?' • '+esc(prox.motivo):''}</p></section>`:''}

    <div class="patient-portal-grid">
      <section class="card"><div class="card-head"><h3>Minha evolução</h3><small>última avaliação</small></div>
        <div class="portal-metrics wide">${metric(num(e.pesoKg),' kg','Peso')}${metric(num(e.imc,2),'','IMC')}${metric(num(e.percentualGordura),'%','Gordura')}${metric(num(e.cinturaCm),' cm','Cintura')}</div>
        ${e.variacaoPesoKg!=null?`<div class="trend-note">Variação: <strong>${e.variacaoPesoKg>0?'+':''}${num(e.variacaoPesoKg)} kg</strong></div>`:''}
      </section>

      <section class="card"><div class="card-head"><h3>Metas de hoje</h3><small>${d.metasConcluidas}/${d.metasAtivas}</small></div>
        <div id="patientGoals">${d.metasHoje?.length?d.metasHoje.map(m=>`<div class="goal-row patient-goal">
          <div><strong>${esc(m.nome)}</strong><small>${num(m.valorHoje)} ${esc(m.unidade||'')} de ${num(m.valorObjetivo)} ${esc(m.unidade||'')}</small></div>
          <div class="goal-progress"><span style="width:${Math.min(100,Number(m.progressoPercentual||0))}%"></span></div>
          <button class="secondary patient-goal-update" data-meta="${m.id}" data-name="${esc(m.nome)}" data-unit="${esc(m.unidade||'')}" data-current="${m.valorHoje??''}">Atualizar</button>
        </div>`).join(''):sectionEmpty('Nenhuma meta ativa hoje.')}</div>
      </section>

      <section class="card span-2"><div class="card-head"><h3>Plano alimentar</h3><small>${plano?`${plano.refeicoes} refeições`:'sem plano ativo'}</small></div>
        ${plano?`<div class="feature-title">${esc(plano.nome)}</div><div class="meal-strip">${(plano.rotinaHoje||[]).map(r=>`<div><strong>${r.horario?String(r.horario).slice(0,5):'--:--'}</strong><span>${esc(r.nome)}</span><small>${r.itens} item(ns)</small></div>`).join('')}</div>`:sectionEmpty('Nenhum plano alimentar ativo.')}
      </section>

      <section class="card"><div class="card-head"><h3>Meu diário</h3><button class="ghost" id="patientAddDiary">+ Registrar</button></div>
        ${d.registrosHoje?.length?`<div class="diary-list compact">${d.registrosHoje.slice(0,5).map(r=>`<article><div class="diary-icon">${diaryIcon(r.tipo)}</div><div><strong>${esc(r.tipo)}</strong><small>${fmtDateTime(r.dataHoraUtc)}</small><p>${esc(r.descricao||'')}</p></div><div class="diary-value">${r.valorNumerico!=null?`${num(r.valorNumerico)} ${esc(r.unidade||'')}`:''}</div></article>`).join('')}</div>`:sectionEmpty('Você ainda não registrou nada hoje.')}
      </section>

      <section class="card"><div class="card-head"><h3>Exames recentes</h3><small>${d.examesRecentes?.length||0}</small></div>
        ${d.examesRecentes?.length?d.examesRecentes.slice(0,6).map(x=>`<div class="lab-row"><div><strong>${esc(x.marcador)}</strong><small>${fmtDate(x.dataColetaUtc)}</small></div><div><b>${x.valorNumerico!=null?num(x.valorNumerico,2):esc(x.valorTexto||'—')} ${esc(x.unidade||'')}</b><span class="pill ${x.classificacao==='DentroDaReferencia'?'Ativa':x.classificacao}">${esc(x.classificacao)}</span></div></div>`).join(''):sectionEmpty('Nenhum exame recente.')}
      </section>
    </div>
  </div>`;

  $('#patientAddDiary').onclick=openMyDiaryForm;
  $$('.patient-goal-update').forEach(b=>b.onclick=()=>openMyGoalForm(b.dataset));
}

function patientPortalModal(title,body,onSubmit){
  const modal=$('#clinicalActionModal'),box=$('#clinicalActionContent');
  modal.classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MEU ACOMPANHAMENTO</span><h2>${esc(title)}</h2></div><form id="patientPortalForm" class="form-grid clinical-form">${body}<div class="span-2 form-actions"><button class="secondary" type="button" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar</button></div></form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  $('#patientPortalForm').onsubmit=async e=>{e.preventDefault();try{await onSubmit(e.target);closeClinicalAction();toast('Registro atualizado.');await loadMyPatientPortal()}catch(err){toast(err.message,true)}};
}
function openMyDiaryForm(){
  patientPortalModal('Novo registro no diário',`${field('Tipo','tipo','text','value="Observacao" required')}${field('Valor','valorNumerico','number','step="0.01"')}${field('Unidade','unidade')}${field('Escala (0-10)','escala','number','min="0" max="10"')}${area('Descrição','descricao')}`,async f=>{
    await api('/api/portal/me/diario',{method:'POST',body:JSON.stringify({dataHoraUtc:new Date().toISOString(),tipo:val(f,'tipo'),descricao:val(f,'descricao'),valorNumerico:dec(f,'valorNumerico'),unidade:val(f,'unidade'),escala:integer(f,'escala'),imagemUrl:null})});
  });
}
function openMyGoalForm(ds){
  patientPortalModal(`Atualizar ${ds.name}`,`${field(`Valor de hoje${ds.unit?' ('+ds.unit+')':''}`,'valor','number',`step="0.01" value="${esc(ds.current||'')}"`)}${area('Observação','observacao')}`,async f=>{
    await api(`/api/portal/me/metas/${ds.meta}/registro`,{method:'POST',body:JSON.stringify({data:todayISO(),valor:dec(f,'valor'),concluida:null,observacao:val(f,'observacao')})});
  });
}

// ativacao via link de convite
const activationParams=new URLSearchParams(location.search);
if(activationParams.get('ativarPaciente')==='1'){
  $('#loginView').classList.add('hidden');
  $('#appView').classList.add('hidden');
  $('#patientAppView')?.classList.add('hidden');
  $('#activationView').classList.remove('hidden');
  const email=activationParams.get('email')||'';
  $('#activationEmailText').textContent=email;
  $('#activationForm').onsubmit=async e=>{
    e.preventDefault();
    const senha=$('#activationPassword').value,senha2=$('#activationPassword2').value;
    if(senha!==senha2){toast('As senhas não conferem.',true);return}
    const b=$('#activationButton');b.disabled=true;
    try{
      const r=await fetch('/api/auth/paciente/ativar',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,token:activationParams.get('token')||'',senha})});
      const t=await r.text();let d={};try{d=t?JSON.parse(t):{}}catch{}
      if(!r.ok)throw new Error(d.message||`Erro HTTP ${r.status}`);
      history.replaceState({},'',location.pathname);
      $('#activationView').classList.add('hidden');
      $('#loginView').classList.remove('hidden');
      $('#email').value=email;$('#senha').value='';
      toast('Acesso ativado. Entre com sua nova senha.');
    }catch(err){toast(err.message,true)}finally{b.disabled=false}
  };
  $('#backToLogin').onclick=()=>{history.replaceState({},'',location.pathname);$('#activationView').classList.add('hidden');$('#loginView').classList.remove('hidden')};
}

$('#patientLogoutButton')?.addEventListener('click',logout);


// ===== v0.3.27 — Portal do paciente completo =====
async function loadPatientSection(view='inicio'){
  const allowed=['inicio','plano','treino','metas','diario','evolucao','exames'];
  if(!allowed.includes(view))view='inicio';
  $$('#patientPortalNav [data-patient-view]').forEach(b=>b.classList.toggle('active',b.dataset.patientView===view));
  const host=$('#patientPortalContent');
  host.innerHTML='<div class="card"><div class="skeleton" style="height:220px"></div></div>';
  if(view==='inicio')return loadMyPatientPortal();
  if(view==='plano')return loadPatientPlan();
  if(view==='treino')return loadPatientWorkout();
  if(view==='metas')return loadPatientGoals();
  if(view==='diario')return loadPatientDiary();
  if(view==='evolucao')return loadPatientEvolution();
  if(view==='exames')return loadPatientLabs();
}

$$('#patientPortalNav [data-patient-view]').forEach(b=>b.addEventListener('click',()=>loadPatientSection(b.dataset.patientView).catch(e=>toast(e.message,true))));

function patientPageHeader(eyebrow,title,subtitle,action=''){
  return `<section class="patient-page-header"><div><span class="eyebrow">${eyebrow}</span><h1>${title}</h1><p>${subtitle}</p></div>${action}</section>`;
}

async function loadPatientPlan(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/plano'),p=d.plano;
  if(!p){
    host.innerHTML=patientPageHeader('ALIMENTAÇÃO','Meu plano alimentar','Seu plano atual preparado pelo profissional.')+sectionEmpty('Você ainda não possui um plano alimentar ativo.');
    return;
  }
  const totals=p.totais||{};
  host.innerHTML=patientPageHeader('ALIMENTAÇÃO',esc(p.nome),`Plano de ${fmtDate(p.dataInicio)}${p.dataFim?' até '+fmtDate(p.dataFim):''} • ${esc(p.profissional)}`)+`
    <div class="patient-plan-totals">
      ${metric(num(totals.calorias,1),' kcal','Energia')}
      ${metric(num(totals.proteinas,1),' g','Proteínas')}
      ${metric(num(totals.carboidratos,1),' g','Carboidratos')}
      ${metric(num(totals.gorduras,1),' g','Gorduras')}
      ${metric(num(totals.fibras,1),' g','Fibras')}
    </div>
    <div class="patient-meal-list">${(p.refeicoes||[]).map(r=>`
      <article class="card patient-meal-card">
        <div class="card-head"><div><span class="eyebrow">${r.horario?String(r.horario).slice(0,5):'SEM HORÁRIO'}</span><h3>${esc(r.nome)}</h3></div></div>
        ${r.observacoes?`<p class="muted">${esc(r.observacoes)}</p>`:''}
        <div class="patient-food-list">${(r.itens||[]).map(i=>`
          <div class="patient-food-item">
            <div><strong>${esc(i.alimento)}</strong><span>${num(i.quantidade)} ${esc(i.unidade)}${i.observacao?' • '+esc(i.observacao):''}</span></div>
            <small>${num(i.nutrientes?.calorias,1)} kcal • P ${num(i.nutrientes?.proteinas,1)} g • C ${num(i.nutrientes?.carboidratos,1)} g • G ${num(i.nutrientes?.gorduras,1)} g</small>
            ${(i.substituicoes||[]).length?`<div class="food-substitutions"><b>Substituições</b>${i.substituicoes.map(s=>`<span>${esc(s.alimento)} — ${num(s.quantidade)} ${esc(s.unidade)}</span>`).join('')}</div>`:''}
          </div>`).join('')}</div>
      </article>`).join('')}</div>`;
}

async function loadPatientGoals(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/metas?dias=30');
  host.innerHTML=patientPageHeader('HÁBITOS','Minhas metas','Acompanhe seu progresso dos últimos 30 dias.')+`
    <div class="patient-goals-history">${(d.metas||[]).length?(d.metas||[]).map(m=>`
      <article class="card patient-goal-card">
        <div class="card-head"><div><h3>${esc(m.nome)}</h3><small>${esc(m.tipo)} • ${esc(m.frequencia)} • ${esc(m.status)}</small></div><strong>${num(m.resumo?.percentualConclusao,1)}%</strong></div>
        <div class="goal-progress large"><span style="width:${Math.min(100,Number(m.resumo?.percentualConclusao||0))}%"></span></div>
        <p class="muted">Objetivo: ${m.valorObjetivo!=null?`${num(m.valorObjetivo)} ${esc(m.unidade||'')}`:'conclusão manual'} • ${m.resumo?.concluidos||0}/${m.resumo?.registros||0} registros concluídos</p>
        <div class="goal-history">${(m.registros||[]).slice(0,8).map(r=>`<div><span>${fmtDate(r.data)}</span><strong>${r.valor!=null?`${num(r.valor)} ${esc(m.unidade||'')}`:(r.concluida?'Concluída':'—')}</strong><span class="pill ${r.concluida?'Ativa':'Agendada'}">${r.concluida?'OK':'Pendente'}</span></div>`).join('')||'<small>Sem registros no período.</small>'}</div>
        ${m.status==='Ativa'?`<button class="secondary patient-goal-update" data-meta="${m.id}" data-name="${esc(m.nome)}" data-unit="${esc(m.unidade||'')}" data-current="">Atualizar hoje</button>`:''}
      </article>`).join(''):sectionEmpty('Nenhuma meta cadastrada.')}</div>`;
  $$('.patient-goal-update').forEach(b=>b.onclick=()=>openMyGoalForm(b.dataset));
}

async function loadPatientDiary(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/diario');
  host.innerHTML=patientPageHeader('ROTINA','Meu diário','Seus registros dos últimos 30 dias.','<button class="primary" id="patientDiaryNew">+ Novo registro</button>')+`
    <div class="card"><div class="card-head"><h3>${d.total||0} registro(s)</h3><small>${fmtDate(d.de)} — ${fmtDate(d.ate)}</small></div>
      ${(d.itens||[]).length?`<div class="diary-list">${d.itens.map(x=>`<article><div class="diary-icon">${diaryIcon(x.tipo)}</div><div><strong>${esc(x.tipo)}</strong><small>${fmtDateTime(x.dataHoraUtc)}</small><p>${esc(x.descricao||'')}</p></div><div class="diary-value">${x.valorNumerico!=null?`${num(x.valorNumerico)} ${esc(x.unidade||'')}`:x.escala!=null?`${x.escala}/10`:''}</div></article>`).join('')}</div>`:sectionEmpty('Nenhum registro neste período.')}
    </div>`;
  $('#patientDiaryNew').onclick=openMyDiaryForm;
}

async function loadPatientEvolution(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/evolucao?limite=24'),items=d.itens||[];
  const last=items[items.length-1]||{};
  host.innerHTML=patientPageHeader('EVOLUÇÃO','Minha evolução corporal','Histórico das avaliações registradas pelo profissional.')+`
    <div class="patient-plan-totals">
      ${metric(num(last.pesoKg),' kg','Peso atual')}
      ${metric(num(last.imc,2),'','IMC')}
      ${metric(num(last.percentualGordura),'%','Gordura')}
      ${metric(num(last.cinturaCm),' cm','Cintura')}
    </div>
    <article class="card">
      <div class="card-head"><h3>Histórico</h3><small>${items.length} avaliação(ões)</small></div>
      ${items.length?`<div class="evolution-table">
        <div class="evolution-row head"><span>Data</span><span>Peso</span><span>IMC</span><span>Gordura</span><span>Cintura</span><span>PA</span></div>
        ${items.slice().reverse().map(x=>`<div class="evolution-row"><strong>${fmtDate(x.dataUtc)}</strong><span>${num(x.pesoKg)} kg</span><span>${num(x.imc,2)}</span><span>${num(x.percentualGordura)}%</span><span>${num(x.cinturaCm)} cm</span><span>${x.pressaoSistolica&&x.pressaoDiastolica?`${x.pressaoSistolica}/${x.pressaoDiastolica}`:'—'}</span></div>`).join('')}
      </div>`:sectionEmpty('Nenhuma avaliação registrada.')}
    </article>`;
}

async function loadPatientLabs(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/exames?limite=20');
  host.innerHTML=patientPageHeader('LABORATÓRIO','Meus exames','Consulte suas coletas e resultados laboratoriais.')+`
    <div class="patient-lab-history">${(d.exames||[]).length?(d.exames||[]).map(e=>`
      <article class="card">
        <div class="card-head"><div><h3>${fmtDate(e.dataColetaUtc)}</h3><small>${esc(e.laboratorio||'Laboratório não informado')} • ${esc(e.profissional)}</small></div><span class="pill Ativa">${e.resultados?.length||0} resultado(s)</span></div>
        ${e.observacoes?`<p class="muted">${esc(e.observacoes)}</p>`:''}
        <div class="lab-result-grid">${(e.resultados||[]).map(r=>`
          <div class="lab-result-card"><div><strong>${esc(r.marcador)}</strong><span class="pill ${r.classificacao==='DentroDaReferencia'?'Ativa':r.classificacao}">${esc(r.classificacao)}</span></div><b>${r.valorNumerico!=null?num(r.valorNumerico,2):esc(r.valorTexto||'—')} ${esc(r.unidade||'')}</b><small>Referência: ${r.referenciaMinima!=null||r.referenciaMaxima!=null?`${r.referenciaMinima??'—'} — ${r.referenciaMaxima??'—'}`:esc(r.referenciaTexto||'não informada')}</small></div>`).join('')}</div>
      </article>`).join(''):sectionEmpty('Nenhum exame registrado.')}</div>`;
}

// Quando o paciente salva diario/meta, mantenha-o na secao atual se possivel.
const _oldOpenMyDiaryForm=openMyDiaryForm;
const _oldOpenMyGoalForm=openMyGoalForm;


// ===== v0.3.27 — Treinos + biblioteca de exercícios =====
const __renderPatientTab_v030 = renderPatientTab;
renderPatientTab = function(d){
  if(state.patientTab!=='treinos') return __renderPatientTab_v030(d);
  const box=$('#patientTabContent'),treinos=d.treinos||[];
  box.innerHTML=`<section class="card full-card">
    <div class="card-head"><div><h3>Planos de treino</h3><small>${treinos.length} plano(s) • modelos aceleram novas prescrições</small></div><div class="workout-top-actions"><button class="ghost" id="sessionLibraryButton">Biblioteca de sessões</button><button class="secondary" id="workoutFromTemplate">Usar modelo</button><button class="primary" id="newWorkoutFromTab">+ Novo treino</button></div></div>
    ${treinos.length?`<div class="workout-plan-grid">${treinos.map(t=>`
      <article class="workout-plan-card">
        <div class="record-top"><div><span class="eyebrow">V${t.versao||1} • ${fmtDate(t.dataInicio)}${t.dataFim?' — '+fmtDate(t.dataFim):''}</span><h4>${esc(t.nome)}</h4><small>${esc(t.profissionalNome||'')}</small></div><span class="pill ${t.status==='Ativo'?'Ativa':'Agendada'}">${esc(t.status)}</span></div>
        ${t.objetivo?`<p>${esc(t.objetivo)}</p>`:''}
        <div class="workout-progression-badges">${t.ajusteCargaPercentual?`<span>Carga ${t.ajusteCargaPercentual>0?'+':''}${num(t.ajusteCargaPercentual,1)}%</span>`:''}${t.ajusteSeries?`<span>Séries ${t.ajusteSeries>0?'+':''}${t.ajusteSeries}</span>`:''}${t.ajusteRepeticoes?`<span>Reps ${t.ajusteRepeticoes>0?'+':''}${t.ajusteRepeticoes}</span>`:''}${t.ajusteDescansoSegundos?`<span>Descanso ${t.ajusteDescansoSegundos>0?'+':''}${t.ajusteDescansoSegundos}s</span>`:''}</div>
        <div class="workout-plan-actions"><button class="ghost workout-save-template" data-workout-id="${t.id}">Salvar como modelo</button><button class="secondary workout-progress" data-workout-id="${t.id}">Criar progressão</button></div>
        <div class="workout-session-mini">${(t.sessoes||[]).map(s=>`<div class="workout-session-mini-row"><div><strong>${esc(s.nome)}</strong><span>${esc(s.diasSemana||'Dias livres')} • ${(s.itens||[]).length} exercício(s)</span></div><button class="ghost session-save-template" data-session-id="${s.id}" data-workout-id="${t.id}">Salvar sessão</button></div>`).join('')}</div>
      </article>`).join('')}</div>`:sectionEmpty('Nenhum plano de treino cadastrado.')}</section>`;
  const patientForWorkout=d.p||{id:state.patientId,nome:'Paciente'};
  $('#newWorkoutFromTab').onclick=()=>openWorkoutForm(patientForWorkout);
  const workoutTemplateButton=$('#workoutFromTemplate');
  if(workoutTemplateButton)workoutTemplateButton.onclick=()=>openWorkoutTemplatePicker(patientForWorkout);
  const sessionLibraryButton=$('#sessionLibraryButton');
  if(sessionLibraryButton)sessionLibraryButton.onclick=()=>openWorkoutSessionLibrary(treinos);
  $$('.session-save-template').forEach(b=>b.onclick=()=>{const plan=treinos.find(x=>x.id===b.dataset.workoutId);const session=plan?.sessoes?.find(x=>x.id===b.dataset.sessionId);openSaveWorkoutSessionTemplate(session)});
  $$('.workout-save-template').forEach(b=>b.onclick=()=>openSaveWorkoutTemplate(treinos.find(x=>x.id===b.dataset.workoutId)));
  $$('.workout-progress').forEach(b=>b.onclick=()=>openWorkoutProgression(patientForWorkout,treinos.find(x=>x.id===b.dataset.workoutId)));
  loadWorkoutPhases(patientForWorkout,treinos).catch(x=>console.warn('Fases de treino:',x));
};

const __openClinicalForm_v030 = openClinicalForm;
openClinicalForm = function(type,p){
  if(type==='treino'){openWorkoutForm(p);return}
  return __openClinicalForm_v030(type,p);
};

async function openWorkoutForm(p){
  const box=$('#clinicalActionContent');
  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PLANO DE TREINO</span><h2>Novo plano</h2><p>${esc(p.nome)} • carregando exercícios...</p></div>`;
  try{
    let exercicios=await api('/api/exercicios');
    const options=()=>`<option value="">Selecione...</option>${exercicios.map(x=>`<option value="${x.id}">${esc(x.nome)}${x.grupoMuscular?' • '+esc(x.grupoMuscular):''}</option>`).join('')}`;

    box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PLANO DE TREINO</span><h2>Novo plano</h2><p>${esc(p.nome)} • ${exercicios.length} exercício(s) no catálogo</p></div>
      <form id="workoutForm" class="clinical-form">
        <div class="form-grid builder-meta">
          ${field('Nome do plano','nome','text','value="Plano de treino" required')}
          ${field('Objetivo','objetivo')}
          ${field('Data de início','dataInicio','date',`value="${todayISO()}" required`)}
          ${field('Data final','dataFim','date')}
          ${area('Orientações gerais','observacoes')}
        </div>
        <div class="builder-head"><div><h3>Treinos / dias</h3><p>Monte a ficha e a ordem dos exercícios.</p></div><div class="builder-head-actions"><button type="button" class="secondary" id="newExerciseCatalog">+ Exercício no catálogo</button><button type="button" class="secondary" id="addWorkoutSession">+ Treino</button></div></div>
        <div id="workoutSessions" class="builder-list"></div>
        <div class="form-actions builder-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar plano de treino</button></div>
      </form>`;

    const host=$('#workoutSessions');
    function addSession(name='Treino A'){
      const el=document.createElement('div');el.className='meal-builder workout-builder';
      el.innerHTML=`<div class="meal-builder-head"><div class="form-grid three workout-session-fields"><label>Nome<input name="sessionName" value="${esc(name)}" required></label><label>Dias da semana<input name="days" placeholder="Segunda, quinta"></label><label>Observações<input name="sessionObs"></label></div><button type="button" class="icon-btn remove-workout-session">×</button></div>
        <div class="workout-items"></div><button type="button" class="ghost add-workout-item">+ Exercício</button>`;
      host.appendChild(el);
      $('.remove-workout-session',el).onclick=()=>el.remove();
      $('.add-workout-item',el).onclick=()=>addItem(el);
      addItem(el);
    }
    function addItem(session){
      const list=$('.workout-items',session),row=document.createElement('div');row.className='workout-item-builder';
      row.innerHTML=`<label>Exercício<select name="exerciseId" required>${options()}</select></label>
        <label>Séries<input name="series" type="number" min="1" value="3" required></label>
        <label>Repetições<input name="reps" value="10-12" required></label>
        <label>Carga<input name="load" type="number" min="0" step="0.01"></label>
        <label>Unidade<input name="loadUnit" value="kg"></label>
        <label>Descanso (s)<input name="rest" type="number" min="0" value="60"></label>
        <label>Tempo (s)<input name="time" type="number" min="0"></label>
        <label>Observação<input name="itemObs"></label>
        <button type="button" class="icon-btn remove-workout-item">×</button>`;
      list.appendChild(row);
      $('.remove-workout-item',row).onclick=()=>row.remove();
    }

    $('.clinical-back').onclick=()=>openClinicalActionMenu(p);
    $('[data-close-clinical-form]').onclick=closeClinicalAction;
    $('#addWorkoutSession').onclick=()=>addSession(`Treino ${String.fromCharCode(65+host.children.length)}`);
    $('#newExerciseCatalog').onclick=async()=>{
      const nome=prompt('Nome do exercício:');if(!nome)return;
      const grupo=prompt('Grupo muscular (opcional):')||null;
      const videoUrl=prompt('Link do vídeo (opcional):')||null;
      try{
        const novo=await api('/api/exercicios',{method:'POST',body:JSON.stringify({nome,grupoMuscular:grupo,equipamento:null,descricao:null,videoUrl})});
        exercicios=await api('/api/exercicios');
        toast(`Exercício "${novo.nome||nome}" adicionado. Reabrindo o construtor...`);
        await openWorkoutForm(p);
      }catch(err){toast(err.message,true)}
    };
    addSession();

    $('#workoutForm').onsubmit=async e=>{
      e.preventDefault();const f=e.target,b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Salvando...';
      try{
        const sessoes=[...f.querySelectorAll('.workout-builder')].map((s,si)=>({
          nome:s.querySelector('[name=sessionName]').value.trim(),
          diasSemana:s.querySelector('[name=days]').value.trim()||null,
          ordem:si+1,
          observacoes:s.querySelector('[name=sessionObs]').value.trim()||null,
          itens:[...s.querySelectorAll('.workout-item-builder')].map((r,ri)=>({
            exercicioId:r.querySelector('[name=exerciseId]').value,
            ordem:ri+1,
            series:Number(r.querySelector('[name=series]').value||0),
            repeticoes:r.querySelector('[name=reps]').value.trim(),
            carga:r.querySelector('[name=load]').value?Number(r.querySelector('[name=load]').value):null,
            unidadeCarga:r.querySelector('[name=loadUnit]').value.trim()||null,
            descansoSegundos:r.querySelector('[name=rest]').value?Number(r.querySelector('[name=rest]').value):null,
            tempoSegundos:r.querySelector('[name=time]').value?Number(r.querySelector('[name=time]').value):null,
            observacoes:r.querySelector('[name=itemObs]').value.trim()||null
          })).filter(x=>x.exercicioId)
        }));
        if(!sessoes.length||!sessoes.some(x=>x.itens.length))throw new Error('Adicione pelo menos um exercício.');
        await api(`/api/pacientes/${p.id}/treinos`,{method:'POST',body:JSON.stringify({
          nome:val(f,'nome'),objetivo:val(f,'objetivo'),dataInicio:val(f,'dataInicio'),
          dataFim:val(f,'dataFim'),status:'Ativo',observacoes:val(f,'observacoes'),sessoes
        })});
        state.patientTab='treinos';closeClinicalAction();toast('Plano de treino salvo.');await loadPatient();
      }catch(err){toast(err.message,true)}
      finally{b.disabled=false;b.textContent='Salvar plano de treino'}
    };
  }catch(err){box.innerHTML=`<div class="card empty">${esc(err.message)}</div>`}
}

async function loadWorkoutPhases(patient,plans){
  const host=$('#patientTabContent');
  if(!host||!patient?.id||host.querySelector('[data-workout-phases]'))return;
  const phases=await api(`/api/pacientes/${patient.id}/fases-treino`);
  if(!host.isConnected)return;

  const section=document.createElement('section');
  section.className='card full-card workout-phases-section';
  section.dataset.workoutPhases='1';
  section.innerHTML=`<div class="card-head"><div><h3>Ciclo de treino</h3><small>${phases.length} fase(s) • periodização além das versões V1/V2/V3</small></div><button class="primary" id="newWorkoutPhase">+ Nova fase</button></div>
  <div class="workout-phase-list">${phases.length?phases.map((f,i)=>workoutPhaseCard(f,i,phases.length)).join(''):`<div class="empty">Nenhuma fase planejada. Crie etapas como adaptação, hipertrofia, força, deload ou performance.</div>`}</div>`;

  host.appendChild(section);
  $('#newWorkoutPhase').onclick=()=>openWorkoutPhaseForm(patient,plans,null);
  $$('.workout-phase-edit').forEach(b=>b.onclick=()=>openWorkoutPhaseForm(patient,plans,phases.find(x=>x.id===b.dataset.phaseId)));
  $$('.workout-phase-delete').forEach(b=>b.onclick=()=>deleteWorkoutPhase(phases.find(x=>x.id===b.dataset.phaseId)));
  $$('.workout-phase-up').forEach(b=>b.onclick=()=>moveWorkoutPhase(patient,phases,b.dataset.phaseId,-1));
  $$('.workout-phase-down').forEach(b=>b.onclick=()=>moveWorkoutPhase(patient,phases,b.dataset.phaseId,1));
}

function workoutPhaseCard(f,index,total){
  const statusLabel={Planejada:'Planejada',EmAndamento:'Em andamento',Concluida:'Concluída',Cancelada:'Cancelada'}[f.status]||f.status;
  const period=`${fmtDate(f.dataInicio)}${f.dataFim?' → '+fmtDate(f.dataFim):' → aberta'}`;
  return `<article class="workout-phase-card ${String(f.status||'').toLowerCase()}">
    <div class="workout-phase-order"><b>${index+1}</b><div><button class="ghost workout-phase-up" data-phase-id="${f.id}" ${index===0?'disabled':''}>↑</button><button class="ghost workout-phase-down" data-phase-id="${f.id}" ${index===total-1?'disabled':''}>↓</button></div></div>
    <div class="workout-phase-body">
      <div class="workout-phase-title"><div><span class="eyebrow">${esc(f.tipo)} • ${period}</span><h4>${esc(f.nome)}</h4></div><span class="pill ${f.status==='EmAndamento'?'Ativa':''}">${esc(statusLabel)}</span></div>
      ${f.objetivo?`<p>${esc(f.objetivo)}</p>`:''}
      ${phaseGoalChips(f)}
      <div class="workout-phase-meta">${f.planoNome?`<span><b>Ficha:</b> ${esc(f.planoNome)} • V${f.planoVersao||1}</span>`:'<span>Sem ficha vinculada</span>'}${f.profissionalNome?`<span><b>Profissional:</b> ${esc(f.profissionalNome)}</span>`:''}</div>
      ${f.observacoes?`<small>${esc(f.observacoes)}</small>`:''}
    </div>
    <div class="workout-phase-actions"><button class="secondary workout-phase-edit" data-phase-id="${f.id}">Editar</button><button class="ghost workout-phase-delete" data-phase-id="${f.id}">Excluir</button></div>
  </article>`;
}

function openWorkoutPhaseForm(patient,plans,phase){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('workout-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  const editing=!!phase;
  const v=x=>x==null?'':String(x);
  const statusOptions=editing?`<label>Status<select name="status"><option value="Planejada">Planejada</option><option value="EmAndamento">Em andamento</option><option value="Concluida">Concluída</option><option value="Cancelada">Cancelada</option></select></label>`:'';

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">PERIODIZAÇÃO</span><h2>${editing?'Editar fase':'Nova fase de treino'}</h2><p>${esc(patient.nome)}</p></div>
  <form id="workoutPhaseForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome da fase','nome','text',`value="${esc(v(phase?.nome))}" placeholder="Ex.: Bloco de força" required`)}
      <label>Tipo<select name="tipo"><option>Adaptação</option><option>Hipertrofia</option><option>Força</option><option>Deload</option><option>Performance</option><option>Condicionamento</option><option>Personalizada</option></select></label>
      ${field('Data de início','dataInicio','date',`value="${v(phase?.dataInicio)||todayISO()}" required`)}
      ${field('Data final','dataFim','date',`value="${v(phase?.dataFim)}"`)}
      <label>Plano de treino<select name="planoTreinoId"><option value="">Sem vínculo</option>${(plans||[]).map(p=>`<option value="${p.id}">${esc(p.nome)} • V${p.versao||1}</option>`).join('')}</select></label>
      ${statusOptions}
      ${field('Meta de peso (kg)','metaPesoKg','number',`step="0.1" min="20" max="400" value="${v(phase?.metaPesoKg)}"`)}
      ${field('Adesão mínima (%)','metaAdesaoPercentual','number',`min="0" max="100" value="${v(phase?.metaAdesaoPercentual)}"`)}
      ${field('Duração mínima (dias)','duracaoMinimaDias','number',`min="1" max="3650" value="${v(phase?.duracaoMinimaDias)}"`)}
      ${area('Critério profissional de transição','criterioTransicao','placeholder="Ex.: completar a semana-alvo sem queda de técnica antes do próximo bloco."')}
      ${area('Objetivo da fase','objetivo','placeholder="Ex.: priorizar força nos básicos mantendo volume moderado."')}
      ${area('Observações','observacoes','placeholder="Estratégia, progressão e observações gerais..."')}
    </div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">${editing?'Salvar alterações':'Criar fase'}</button></div>
  </form>`;

  const f=$('#workoutPhaseForm');
  f.tipo.value=phase?.tipo||'Personalizada';
  f.planoTreinoId.value=phase?.planoTreinoId||'';
  if(editing)f.status.value=phase.status||'Planejada';
  f.objetivo.value=phase?.objetivo||'';
  f.criterioTransicao.value=phase?.criterioTransicao||'';
  f.observacoes.value=phase?.observacoes||'';
  $('[data-close-clinical-form]').onclick=closeClinicalAction;

  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      const base={
        nome:val(f,'nome'),tipo:val(f,'tipo'),objetivo:val(f,'objetivo')||null,
        dataInicio:val(f,'dataInicio'),dataFim:val(f,'dataFim')||null,
        planoTreinoId:val(f,'planoTreinoId')||null,
        metaPesoKg:dec(f,'metaPesoKg'),
        metaAdesaoPercentual:val(f,'metaAdesaoPercentual')===''?null:Number(val(f,'metaAdesaoPercentual')),
        duracaoMinimaDias:val(f,'duracaoMinimaDias')===''?null:Number(val(f,'duracaoMinimaDias')),
        criterioTransicao:val(f,'criterioTransicao')||null,
        observacoes:val(f,'observacoes')||null
      };
      if(editing){
        await api(`/api/fases-treino/${phase.id}`,{method:'PUT',body:JSON.stringify({...base,status:val(f,'status')})});
        toast('Fase de treino atualizada.');
      }else{
        await api(`/api/pacientes/${patient.id}/fases-treino`,{method:'POST',body:JSON.stringify(base)});
        toast('Fase de treino criada.');
      }
      closeClinicalAction();await loadPatient();
    }catch(err){toast(err.message,true)}
    finally{btn.disabled=false}
  };
}

async function deleteWorkoutPhase(phase){
  if(!phase)return;
  if(!confirm(`Excluir a fase "${phase.nome}"?`))return;
  try{
    await api(`/api/fases-treino/${phase.id}`,{method:'DELETE'});
    toast('Fase de treino excluída.');await loadPatient();
  }catch(err){toast(err.message,true)}
}

async function moveWorkoutPhase(patient,phases,id,delta){
  const list=phases.slice();
  const idx=list.findIndex(x=>x.id===id),target=idx+delta;
  if(idx<0||target<0||target>=list.length)return;
  [list[idx],list[target]]=[list[target],list[idx]];
  try{
    await api(`/api/pacientes/${patient.id}/fases-treino/reordenar`,{
      method:'POST',
      body:JSON.stringify({fases:list.map((x,i)=>({faseId:x.id,ordem:i+1}))})
    });
    toast('Ordem do ciclo atualizada.');await loadPatient();
  }catch(err){toast(err.message,true)}
}

async function openSaveWorkoutSessionTemplate(session){
  if(!session)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('workout-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">BIBLIOTECA DE SESSÕES</span><h2>Salvar sessão de treino</h2><p>${esc(session.nome)}</p></div>
  <form id="saveWorkoutSessionTemplateForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome do modelo','nome','text',`value="${esc(session.nome)}" required`)}
      ${field('Categoria','categoria','text','placeholder="Push, Pull, Pernas, Full Body..."')}
      ${area('Descrição','descricao','placeholder="Objetivo, nível, equipamentos, observações..."')}
    </div>
    <div class="template-summary"><strong>${(session.itens||[]).length} exercício(s)</strong><span>A prescrição completa será salva neste bloco.</span></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Salvar na biblioteca</button></div>
  </form>`;

  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#saveWorkoutSessionTemplateForm');

  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/sessoes-treino/${session.id}/salvar-como-modelo`,{
        method:'POST',
        body:JSON.stringify({
          nome:val(f,'nome'),
          categoria:val(f,'categoria')||null,
          descricao:val(f,'descricao')||null
        })
      });
      closeClinicalAction();toast('Sessão salva na biblioteca.');
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openWorkoutSessionLibrary(treinos){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('workout-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">BIBLIOTECA DE SESSÕES</span><h2>Inserção rápida de treino</h2><p>Reutilize um dia de treino sem duplicar a ficha inteira.</p></div><div class="empty">Carregando biblioteca...</div>`;

  try{
    const modelos=await api('/api/modelos-sessoes-treino');
    const ativos=(treinos||[]).filter(t=>t.status!=='Concluido');

    box.innerHTML=`<div class="modal-heading"><span class="eyebrow">BIBLIOTECA DE SESSÕES</span><h2>Inserção rápida de treino</h2><p>${modelos.length} sessão(ões) salvas</p></div>
      <div class="session-library-toolbar">
        <input id="sessionLibrarySearch" class="search-input" placeholder="Buscar sessão, categoria ou descrição">
        <select id="sessionLibraryPlan">${ativos.map(t=>`<option value="${t.id}">${esc(t.nome)} • V${t.versao||1}</option>`).join('')}</select>
      </div>
      <div id="sessionLibraryList" class="session-library-grid"></div>
      <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Fechar</button></div>`;

    $('[data-close-clinical-form]').onclick=closeClinicalAction;

    if(!ativos.length){
      $('#sessionLibraryList').innerHTML=`<div class="empty">Crie ou ative um plano de treino antes de inserir uma sessão.</div>`;
      return;
    }

    const render=q=>{
      const term=String(q||'').trim().toLowerCase();
      const filtered=modelos.filter(m=>!term||
        String(m.nome||'').toLowerCase().includes(term)||
        String(m.categoria||'').toLowerCase().includes(term)||
        String(m.descricao||'').toLowerCase().includes(term));

      $('#sessionLibraryList').innerHTML=filtered.length?filtered.map(m=>`<article class="session-library-card" data-session-template-id="${m.id}">
        <div><span class="eyebrow">${esc(m.categoria||'SEM CATEGORIA')} • ${m.exercicios} exercício(s)${m.comCarga?` • ${m.comCarga} com carga`:''}</span><h4>${esc(m.nome)}</h4><p>${esc(m.descricao||'Sem descrição')}</p><small>${esc(m.diasSemana||'Dias flexíveis')}</small></div>
        <button class="primary insert-session-template">Inserir no plano</button>
      </article>`).join(''):`<div class="empty">Nenhuma sessão encontrada.</div>`;

      $$('.insert-session-template').forEach(b=>b.onclick=()=>{
        const card=b.closest('[data-session-template-id]');
        const modelo=modelos.find(x=>x.id===card.dataset.sessionTemplateId);
        const plano=ativos.find(x=>x.id===$('#sessionLibraryPlan').value);
        openWorkoutSessionInsertForm(plano,modelo,treinos);
      });
    };

    render('');
    $('#sessionLibrarySearch').oninput=e=>render(e.target.value);
  }catch(err){
    box.innerHTML=`<div class="card empty">${esc(err.message)}</div>`;
  }
}

function openWorkoutSessionInsertForm(plan,modelo,treinos){
  if(!plan||!modelo)return;
  const box=$('#clinicalActionContent');

  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link" id="backToSessionLibrary">← Biblioteca</button><span class="eyebrow">INSERÇÃO RÁPIDA</span><h2>${esc(modelo.nome)}</h2><p>${esc(plan.nome)} • V${plan.versao||1}</p></div>
  <form id="sessionLibraryInsertForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome da sessão','nome','text',`value="${esc(modelo.nome)}"`)}
      ${field('Dias da semana','diasSemana','text',`value="${esc(modelo.diasSemana||'')}" placeholder="Segunda, quinta"`) }
      ${area('Observações','observacoes','placeholder="Opcional. Se vazio, mantém as observações do modelo."')}
    </div>
    <div class="template-summary"><strong>${modelo.exercicios} exercício(s)</strong><span>Serão adicionados como uma nova sessão no fim do plano.</span></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Inserir sessão</button></div>
  </form>`;

  $('#backToSessionLibrary').onclick=()=>openWorkoutSessionLibrary(treinos);
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#sessionLibraryInsertForm');

  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/treinos/${plan.id}/inserir-modelo-sessao/${modelo.id}`,{
        method:'POST',
        body:JSON.stringify({
          nome:val(f,'nome')||null,
          diasSemana:val(f,'diasSemana')||null,
          observacoes:val(f,'observacoes')||null
        })
      });
      state.patientTab='treinos';closeClinicalAction();toast('Sessão inserida no plano.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openSaveWorkoutTemplate(plan){
  if(!plan)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('workout-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELO DE TREINO</span><h2>Salvar ficha como modelo</h2><p>${esc(plan.nome)}</p></div>
  <form id="saveWorkoutTemplateForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome do modelo','nome','text',`value="${esc(plan.nome)}" required`)}
      ${area('Descrição','descricao','placeholder="Ex.: Hipertrofia ABC 5x/semana, iniciante..."')}
    </div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Salvar modelo</button></div>
  </form>`;

  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#saveWorkoutTemplateForm');
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/treinos/${plan.id}/salvar-como-modelo`,{
        method:'POST',
        body:JSON.stringify({nome:val(f,'nome'),descricao:val(f,'descricao')||null})
      });
      closeClinicalAction();toast('Modelo de treino salvo para reutilização.');
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openWorkoutTemplatePicker(p){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('workout-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELOS DE TREINO</span><h2>Criar treino a partir de modelo</h2><p>${esc(p.nome)}</p></div><div class="empty">Carregando modelos...</div>`;

  try{
    const modelos=await api('/api/modelos-planos-treino');

    if(!modelos.length){
      box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELOS DE TREINO</span><h2>Criar treino a partir de modelo</h2><p>${esc(p.nome)}</p></div><div class="empty">Nenhum modelo ativo. Salve uma ficha existente como modelo primeiro.</div><div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Fechar</button></div>`;
      $('[data-close-clinical-form]').onclick=closeClinicalAction;
      return;
    }

    box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MODELOS DE TREINO</span><h2>Criar treino a partir de modelo</h2><p>${esc(p.nome)} • ${modelos.length} modelo(s)</p></div>
      <div class="template-picker-toolbar"><input id="workoutTemplateSearch" class="search-input" placeholder="Buscar modelo de treino"></div>
      <div id="workoutTemplateList" class="workout-template-grid"></div>
      <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Fechar</button></div>`;

    $('[data-close-clinical-form]').onclick=closeClinicalAction;

    const render=q=>{
      const term=String(q||'').trim().toLowerCase();
      const filtered=modelos.filter(m=>!term||m.nome.toLowerCase().includes(term)||String(m.descricao||'').toLowerCase().includes(term)||String(m.objetivo||'').toLowerCase().includes(term));

      $('#workoutTemplateList').innerHTML=filtered.length?filtered.map(m=>`<article class="workout-template-card" data-template-id="${m.id}">
        <div><span class="eyebrow">${m.sessoes} sessão(ões) • ${m.exercicios} exercício(s)</span><h4>${esc(m.nome)}</h4><p>${esc(m.descricao||m.objetivo||'Sem descrição')}</p><small>${esc(m.profissionalNome||'')}</small></div>
        <button class="primary use-workout-template">Usar este modelo</button>
      </article>`).join(''):`<div class="empty">Nenhum modelo encontrado.</div>`;

      $$('.use-workout-template').forEach(b=>b.onclick=()=>{
        const card=b.closest('[data-template-id]');
        const m=modelos.find(x=>x.id===card.dataset.templateId);
        openWorkoutTemplateCreateForm(p,m);
      });
    };

    render('');
    $('#workoutTemplateSearch').oninput=e=>render(e.target.value);
  }catch(err){
    box.innerHTML=`<div class="card empty">${esc(err.message)}</div>`;
  }
}

function openWorkoutTemplateCreateForm(p,modelo){
  const box=$('#clinicalActionContent');

  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link" id="backToWorkoutTemplates">← Modelos</button><span class="eyebrow">CRIAR A PARTIR DE MODELO</span><h2>${esc(modelo.nome)}</h2><p>${esc(p.nome)}</p></div>
  <form id="workoutTemplateCreateForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome do plano','nome','text',`value="${esc(modelo.nome)}" required`)}
      ${field('Objetivo','objetivo','text',`value="${esc(modelo.objetivo||'')}"`)}
      ${field('Data de início','dataInicio','date',`value="${todayISO()}" required`)}
      ${field('Data final','dataFim','date')}
      ${area('Orientações adicionais','observacoes','placeholder="Opcional. Se vazio, usa as orientações originais do modelo."')}
    </div>
    <div class="template-summary"><strong>${modelo.sessoes} sessão(ões)</strong><span>${modelo.exercicios} exercício(s) serão copiados para a nova ficha.</span></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Criar treino</button></div>
  </form>`;

  $('#backToWorkoutTemplates').onclick=()=>openWorkoutTemplatePicker(p);
  $('[data-close-clinical-form]').onclick=closeClinicalAction;

  const f=$('#workoutTemplateCreateForm');
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/pacientes/${p.id}/treinos/criar-de-modelo/${modelo.id}`,{
        method:'POST',
        body:JSON.stringify({
          nome:val(f,'nome'),
          objetivo:val(f,'objetivo')||null,
          dataInicio:val(f,'dataInicio'),
          dataFim:val(f,'dataFim')||null,
          observacoes:val(f,'observacoes')||null
        })
      });

      state.patientTab='treinos';closeClinicalAction();toast('Treino criado a partir do modelo.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function openWorkoutProgression(p,plan){
  if(!plan)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('workout-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><button type="button" class="back-link clinical-back">← Voltar</button><span class="eyebrow">PROGRESSÃO DE TREINO</span><h2>Nova versão do ciclo</h2><p>${esc(p.nome)} • baseado em ${esc(plan.nome)}</p></div>
  <form id="workoutProgressForm" class="clinical-form">
    <div class="form-grid">
      ${field('Nome da nova versão','nome','text',`value="${esc(plan.nome+' • V'+((plan.versao||1)+1))}" required`)}
      ${field('Data de início','dataInicio','date',`value="${todayISO()}" required`)}
      ${field('Data final','dataFim','date')}
      ${field('Carga (%)','cargaPercentual','number','step="0.5" min="-50" max="100" value="0"')}
      ${field('Séries (+/-)','seriesDelta','number','min="-5" max="10" value="0"')}
      ${field('Repetições (+/-)','repeticoesDelta','number','min="-20" max="30" value="0"')}
      ${field('Descanso (+/- segundos)','descansoDelta','number','min="-300" max="600" value="0"')}
      <label class="span-2 workout-check"><input name="concluirAnterior" type="checkbox" checked> Concluir plano anterior ao criar a nova versão</label>
    </div>
    <div id="workoutProjection" class="workout-projection"><div class="empty compact">Ajuste os parâmetros para visualizar a projeção.</div></div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Criar nova versão</button></div>
  </form>`;

  $('.clinical-back').onclick=()=>openClinicalActionMenu(p);
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#workoutProgressForm');
  let timer;
  const refresh=()=>{
    clearTimeout(timer);
    timer=setTimeout(async()=>{
      try{
        const qs=new URLSearchParams({
          cargaPercentual:val(f,'cargaPercentual')||'0',
          seriesDelta:val(f,'seriesDelta')||'0',
          repeticoesDelta:val(f,'repeticoesDelta')||'0',
          descansoDeltaSegundos:val(f,'descansoDelta')||'0'
        });
        const s=await api(`/api/treinos/${plan.id}/simular-progressao?${qs.toString()}`);
        $('#workoutProjection').innerHTML=`<div class="workout-projection-grid">
          <div><small>Exercícios</small><strong>${s.exercicios}</strong><span>${s.exerciciosComCarga} com carga prescrita</span></div>
          <div><small>Soma das cargas</small><strong>${num(s.somaCargasProjetada,1)}</strong><span>atual ${num(s.somaCargasAtual,1)}</span></div>
          <div><small>Repetições</small><strong>${s.prescricoesRepeticoesAjustadas}</strong><span>ajustadas • ${s.prescricoesRepeticoesPreservadas} preservadas</span></div>
        </div>`;
      }catch(err){$('#workoutProjection').innerHTML=`<div class="empty compact">${esc(err.message)}</div>`}
    },180);
  };
  ['cargaPercentual','seriesDelta','repeticoesDelta','descansoDelta'].forEach(n=>f.querySelector(`[name=${n}]`).oninput=refresh);
  refresh();

  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api(`/api/treinos/${plan.id}/duplicar`,{method:'POST',body:JSON.stringify({
        nome:val(f,'nome'),
        dataInicio:val(f,'dataInicio'),
        dataFim:val(f,'dataFim')||null,
        ajusteCargaPercentual:Number(val(f,'cargaPercentual')||0),
        ajusteSeries:Number(val(f,'seriesDelta')||0),
        ajusteRepeticoes:Number(val(f,'repeticoesDelta')||0),
        ajusteDescansoSegundos:Number(val(f,'descansoDelta')||0),
        concluirPlanoAnterior:f.querySelector('[name=concluirAnterior]').checked
      })});
      state.patientTab='treinos';closeClinicalAction();toast('Nova versão do treino criada.');await loadPatient();
    }catch(err){toast(err.message,true)}finally{btn.disabled=false}
  };
}

async function loadPatientWorkout(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/treino'),p=d.plano;
  if(!p){
    host.innerHTML=patientPageHeader('TREINO','Meu treino','Sua ficha atual preparada pelo profissional.')+sectionEmpty('Você ainda não possui um plano de treino ativo.');
    return;
  }
  host.innerHTML=patientPageHeader('TREINO',esc(p.nome),`${esc(p.objetivo||'Plano de exercícios')} • ${esc(p.profissional)}`)+`
    <div class="patient-plan-totals workout-totals">
      ${metric(p.totalSessoes,'','Treinos')}
      ${metric(p.totalExercicios,'','Exercícios')}
      ${metric(fmtDate(p.dataInicio),'','Início')}
      ${metric(p.dataFim?fmtDate(p.dataFim):'—','','Fim')}
    </div>
    ${p.observacoes?`<article class="card workout-guidance"><strong>Orientações</strong><p>${esc(p.observacoes)}</p></article>`:''}
    <div class="patient-workout-list">${(p.sessoes||[]).map(s=>`
      <article class="card patient-workout-session">
        <div class="card-head"><div><span class="eyebrow">${esc(s.diasSemana||'DIAS LIVRES')}</span><h3>${esc(s.nome)}</h3></div><span class="pill Ativa">${(s.itens||[]).length} exercício(s)</span></div>
        ${s.observacoes?`<p class="muted">${esc(s.observacoes)}</p>`:''}
        <div class="patient-exercise-list">${(s.itens||[]).map((i,idx)=>`
          <div class="patient-exercise-card">
            <div class="exercise-order">${idx+1}</div>
            <div class="exercise-main"><div class="exercise-title"><strong>${esc(i.exercicio)}</strong>${i.grupoMuscular?`<small>${esc(i.grupoMuscular)}${i.equipamento?' • '+esc(i.equipamento):''}</small>`:''}</div>
              <div class="exercise-prescription"><b>${i.series} × ${esc(i.repeticoes)}</b>${i.carga!=null?`<span>${num(i.carga)} ${esc(i.unidadeCarga||'kg')}</span>`:''}${i.descansoSegundos!=null?`<span>${i.descansoSegundos}s descanso</span>`:''}${i.tempoSegundos!=null?`<span>${i.tempoSegundos}s execução</span>`:''}</div>
              ${i.observacoes?`<p>${esc(i.observacoes)}</p>`:''}
            </div>
            ${i.videoUrl?`<a class="exercise-video" href="${esc(i.videoUrl)}" target="_blank" rel="noopener noreferrer">▶ Ver vídeo</a>`:''}
          </div>`).join('')}</div>
      </article>`).join('')}</div>`;
}


// ===== v0.3.27 — Execução de treinos + progressão de carga =====
const __renderPatientTab_v031 = renderPatientTab;
renderPatientTab = function(d){
  if(state.patientTab!=='treinos') return __renderPatientTab_v031(d);

  const box=$('#patientTabContent'),treinos=d.treinos||[],h=d.treinosHistorico||{};
  box.innerHTML=`<section class="card full-card">
    <div class="card-head"><div><h3>Planos de treino</h3><small>${treinos.length} plano(s)</small></div><button class="primary" id="newWorkoutFromTab">+ Novo treino</button></div>
    ${treinos.length?`<div class="workout-plan-grid">${treinos.map(t=>`
      <article class="workout-plan-card">
        <div class="record-top"><div><span class="eyebrow">${fmtDate(t.dataInicio)}${t.dataFim?' — '+fmtDate(t.dataFim):''}</span><h4>${esc(t.nome)}</h4><small>${esc(t.profissionalNome||'')}</small></div><span class="pill ${t.status==='Ativo'?'Ativa':'Agendada'}">${esc(t.status)}</span></div>
        ${t.objetivo?`<p>${esc(t.objetivo)}</p>`:''}
        <div class="workout-session-mini">${(t.sessoes||[]).map(s=>`<div><strong>${esc(s.nome)}</strong><span>${esc(s.diasSemana||'Dias livres')} • ${(s.itens||[]).length} exercício(s)</span></div>`).join('')}</div>
      </article>`).join('')}</div>`:sectionEmpty('Nenhum plano de treino cadastrado.')}</section>
    <section class="card full-card">
      <div class="card-head"><div><h3>Adesão e progressão</h3><small>Últimos 90 dias</small></div></div>
      <div class="workout-history-metrics">
        ${metric(h.totalTreinos||0,'','Treinos realizados')}
        ${metric(h.minutosTotais||0,' min','Tempo total')}
        ${metric(h.esforcoMedio!=null?num(h.esforcoMedio,1):'—','/10','Esforço médio')}
        ${metric((h.evolucaoCarga||[]).length,'','Exercícios com carga')}
      </div>
      ${(h.evolucaoCarga||[]).length?`<div class="load-progress-grid">${h.evolucaoCarga.map(x=>`
        <div class="load-progress-card"><strong>${esc(x.exercicio)}</strong><span>Última: ${x.ultimaCarga!=null?num(x.ultimaCarga)+' kg':'—'}</span><span>Maior: ${x.maiorCarga!=null?num(x.maiorCarga)+' kg':'—'}</span><small>${(x.registros||[]).length} registro(s)</small></div>`).join('')}</div>`:sectionEmpty('Ainda não há histórico de cargas registradas.')}
      ${(h.execucoes||[]).length?`<div class="workout-execution-list">${h.execucoes.slice(0,10).map(x=>`
        <article><div><strong>${esc(x.sessao)}</strong><small>${fmtDateTime(x.dataHoraInicioUtc)} • ${esc(x.plano)}</small></div><span>${x.duracaoMinutos||0} min</span><span>${x.esforcoPercebido!=null?`RPE ${x.esforcoPercebido}/10`:'—'}</span></article>`).join('')}</div>`:''}
    </section>`;
  $('#newWorkoutFromTab').onclick=()=>openWorkoutForm(d.p);
};

const __loadPatientWorkout_v031 = loadPatientWorkout;
loadPatientWorkout = async function(){
  const host=$('#patientPortalContent');
  const [d,h]=await Promise.all([
    api('/api/portal/me/treino'),
    api('/api/portal/me/treinos/historico?dias=90')
  ]);
  const p=d.plano;
  if(!p){
    host.innerHTML=patientPageHeader('TREINO','Meu treino','Sua ficha atual preparada pelo profissional.')+sectionEmpty('Você ainda não possui um plano de treino ativo.');
    return;
  }

  host.innerHTML=patientPageHeader('TREINO',esc(p.nome),`${esc(p.objetivo||'Plano de exercícios')} • ${esc(p.profissional)}`)+`
    <div class="patient-plan-totals workout-totals">
      ${metric(p.totalSessoes,'','Treinos')}
      ${metric(p.totalExercicios,'','Exercícios')}
      ${metric(h.total||0,'','Concluídos 90d')}
      ${metric(fmtDate(p.dataInicio),'','Início')}
    </div>
    ${p.observacoes?`<article class="card workout-guidance"><strong>Orientações</strong><p>${esc(p.observacoes)}</p></article>`:''}
    <div class="patient-workout-list">${(p.sessoes||[]).map(s=>`
      <article class="card patient-workout-session">
        <div class="card-head"><div><span class="eyebrow">${esc(s.diasSemana||'DIAS LIVRES')}</span><h3>${esc(s.nome)}</h3></div><button class="primary start-workout" data-session="${s.id}">Registrar treino</button></div>
        ${s.observacoes?`<p class="muted">${esc(s.observacoes)}</p>`:''}
        <div class="patient-exercise-list">${(s.itens||[]).map((i,idx)=>`
          <div class="patient-exercise-card">
            <div class="exercise-order">${idx+1}</div>
            <div class="exercise-main"><div class="exercise-title"><strong>${esc(i.exercicio)}</strong>${i.grupoMuscular?`<small>${esc(i.grupoMuscular)}${i.equipamento?' • '+esc(i.equipamento):''}</small>`:''}</div>
              <div class="exercise-prescription"><b>${i.series} × ${esc(i.repeticoes)}</b>${i.carga!=null?`<span>${num(i.carga)} ${esc(i.unidadeCarga||'kg')}</span>`:''}${i.descansoSegundos!=null?`<span>${i.descansoSegundos}s descanso</span>`:''}</div>
            </div>
            ${i.videoUrl?`<a class="exercise-video" href="${esc(i.videoUrl)}" target="_blank" rel="noopener noreferrer">▶ Vídeo</a>`:''}
          </div>`).join('')}</div>
      </article>`).join('')}</div>
    <section class="card workout-history-patient">
      <div class="card-head"><h3>Histórico recente</h3><small>${h.total||0} treino(s)</small></div>
      ${(h.execucoes||[]).length?`<div class="workout-execution-list">${h.execucoes.slice(0,8).map(x=>`
        <article><div><strong>${esc(x.sessao)}</strong><small>${fmtDateTime(x.dataHoraInicioUtc)}</small></div><span>${x.duracaoMinutos||0} min</span><span>${x.esforcoPercebido!=null?`RPE ${x.esforcoPercebido}/10`:'—'}</span></article>`).join('')}</div>`:sectionEmpty('Nenhum treino registrado ainda.')}
    </section>`;

  $$('.start-workout').forEach(b=>b.onclick=()=>{
    const sessao=(p.sessoes||[]).find(x=>x.id===b.dataset.session);
    if(sessao)openWorkoutExecutionForm(sessao);
  });
};

function openWorkoutExecutionForm(sessao){
  const modal=$('#clinicalActionModal'),box=$('#clinicalActionContent');
  modal.classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">EXECUÇÃO</span><h2>${esc(sessao.nome)}</h2><p>Registre o que você realmente executou hoje.</p></div>
    <form id="workoutExecutionForm" class="clinical-form">
      <div class="form-grid three">
        ${field('Duração (min)','duracao','number','min="0"')}
        ${field('Esforço geral (0-10)','rpe','number','min="0" max="10"')}
        ${field('Horário de início','inicio','datetime-local',`value="${new Date(Date.now()-new Date().getTimezoneOffset()*60000).toISOString().slice(0,16)}"`)}
      </div>
      <div class="execution-items">${(sessao.itens||[]).map(i=>`
        <div class="execution-item" data-item="${i.id}">
          <div><strong>${esc(i.exercicio)}</strong><small>Prescrito: ${i.series} × ${esc(i.repeticoes)}${i.carga!=null?' • '+num(i.carga)+' '+esc(i.unidadeCarga||'kg'):''}</small></div>
          <label>Séries<input name="series" type="number" min="0" value="${i.series}"></label>
          <label>Repetições<input name="reps" value="${esc(i.repeticoes)}"></label>
          <label>Carga<input name="load" type="number" min="0" step="0.01" value="${i.carga??''}"></label>
          <label>RPE<input name="itemRpe" type="number" min="0" max="10"></label>
          <label class="check-line"><input name="done" type="checkbox" checked> Feito</label>
        </div>`).join('')}</div>
      ${area('Observação geral','observacoes')}
      <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Concluir treino</button></div>
    </form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  $('#workoutExecutionForm').onsubmit=async e=>{
    e.preventDefault();const f=e.target,b=f.querySelector('button[type=submit]');b.disabled=true;b.textContent='Salvando...';
    try{
      const inicioLocal=val(f,'inicio');
      const itens=[...f.querySelectorAll('.execution-item')].map(r=>({
        itemTreinoId:r.dataset.item,
        seriesRealizadas:Number(r.querySelector('[name=series]').value||0),
        repeticoesRealizadas:r.querySelector('[name=reps]').value.trim()||null,
        cargaRealizada:r.querySelector('[name=load]').value?Number(r.querySelector('[name=load]').value):null,
        unidadeCarga:'kg',
        esforcoPercebido:r.querySelector('[name=itemRpe]').value?Number(r.querySelector('[name=itemRpe]').value):null,
        concluido:r.querySelector('[name=done]').checked,
        observacoes:null
      }));
      await api('/api/portal/me/treinos/execucoes',{method:'POST',body:JSON.stringify({
        sessaoTreinoId:sessao.id,
        dataHoraInicioUtc:inicioLocal?new Date(inicioLocal).toISOString():new Date().toISOString(),
        dataHoraFimUtc:new Date().toISOString(),
        duracaoMinutos:integer(f,'duracao'),
        esforcoPercebido:integer(f,'rpe'),
        observacoes:val(f,'observacoes'),
        itens
      })});
      closeClinicalAction();toast('Treino registrado. Boa!');await loadPatientWorkout();
    }catch(err){toast(err.message,true)}
    finally{b.disabled=false;b.textContent='Concluir treino'}
  };
}


// ===== v0.3.27 — Gráficos de evolução + painel analítico =====
function hpFinite(v){
  const n=Number(v);
  return v!==null&&v!==undefined&&v!==''&&Number.isFinite(n)?n:null;
}
function hpChartDate(v){
  if(!v)return '';
  try{return new Intl.DateTimeFormat('pt-BR',{day:'2-digit',month:'2-digit'}).format(new Date(v))}
  catch{return String(v)}
}
function hpChartSeries(points){
  return (points||[])
    .map((p,i)=>({x:i,date:p.date,value:hpFinite(p.value),label:p.label||''}))
    .filter(p=>p.value!==null);
}
function hpLineChart(title,points,suffix='',empty='São necessários pelo menos dois registros para gerar este gráfico.'){
  const series=hpChartSeries(points);
  if(series.length<2)return `<article class="analytics-chart analytics-chart-empty"><div class="analytics-chart-head"><div><h4>${esc(title)}</h4></div></div><div class="chart-empty">${esc(empty)}</div></article>`;

  const W=720,H=250,padL=54,padR=20,padT=24,padB=42;
  let min=Math.min(...series.map(x=>x.value)),max=Math.max(...series.map(x=>x.value));
  if(min===max){const delta=Math.abs(min||1)*.08||1;min-=delta;max+=delta}
  const range=max-min;
  min-=range*.08;max+=range*.08;
  const innerW=W-padL-padR,innerH=H-padT-padB;
  const xAt=i=>padL+(series.length===1?innerW/2:(i/(series.length-1))*innerW);
  const yAt=v=>padT+((max-v)/(max-min))*innerH;
  const poly=series.map((p,i)=>`${xAt(i).toFixed(1)},${yAt(p.value).toFixed(1)}`).join(' ');
  const ticks=[0,.25,.5,.75,1].map(r=>{
    const v=max-(max-min)*r,y=padT+innerH*r;
    return `<g><line x1="${padL}" y1="${y}" x2="${W-padR}" y2="${y}" class="chart-grid-line"/><text x="${padL-9}" y="${y+4}" text-anchor="end" class="chart-axis-label">${esc(num(v,2))}</text></g>`;
  }).join('');
  const every=Math.max(1,Math.ceil(series.length/6));
  const labels=series.map((p,i)=>{
    if(i%every!==0 && i!==series.length-1)return '';
    return `<text x="${xAt(i)}" y="${H-13}" text-anchor="middle" class="chart-axis-label">${esc(hpChartDate(p.date))}</text>`;
  }).join('');
  const dots=series.map((p,i)=>`<g class="chart-point-group"><circle cx="${xAt(i)}" cy="${yAt(p.value)}" r="4.5" class="chart-point"/><title>${esc(p.label||hpChartDate(p.date))}: ${esc(num(p.value,2))}${esc(suffix)}</title></g>`).join('');
  const first=series[0].value,last=series[series.length-1].value,delta=last-first;
  const deltaText=`${delta>0?'+':''}${num(delta,2)}${suffix}`;
  return `<article class="analytics-chart">
    <div class="analytics-chart-head"><div><h4>${esc(title)}</h4><small>${series.length} ponto(s)</small></div><span class="chart-delta ${delta>0?'up':delta<0?'down':'flat'}">${esc(deltaText)}</span></div>
    <svg class="native-line-chart" viewBox="0 0 ${W} ${H}" role="img" aria-label="${esc(title)}">
      ${ticks}
      <polyline points="${poly}" class="chart-line"/>
      ${dots}
      ${labels}
    </svg>
    <div class="chart-foot"><span>Inicial: <b>${num(first,2)}${esc(suffix)}</b></span><span>Atual: <b>${num(last,2)}${esc(suffix)}</b></span></div>
  </article>`;
}
function hpMetricChartGrid(charts){
  return `<div class="analytics-grid">${charts.join('')}</div>`;
}
function hpEvalCharts(avaliacoes){
  const ordered=(avaliacoes||[]).slice().sort((a,b)=>new Date(a.dataUtc)-new Date(b.dataUtc));
  return hpMetricChartGrid([
    hpLineChart('Peso',ordered.map(x=>({date:x.dataUtc,value:x.pesoKg})),' kg'),
    hpLineChart('IMC',ordered.map(x=>({date:x.dataUtc,value:x.imc})),''),
    hpLineChart('Gordura corporal',ordered.map(x=>({date:x.dataUtc,value:x.percentualGordura})),'%'),
    hpLineChart('Cintura',ordered.map(x=>({date:x.dataUtc,value:x.cinturaCm})),' cm')
  ]);
}
function hpLabSeriesFromProfessional(exames){
  const map=new Map();
  (exames||[]).forEach(e=>(e.resultados||[]).forEach(r=>{
    const v=hpFinite(r.valorNumerico);if(v===null)return;
    const key=r.marcadorNome||r.marcador||'Marcador';
    if(!map.has(key))map.set(key,{unit:r.unidade||'',points:[]});
    map.get(key).points.push({date:e.dataColetaUtc,value:v,label:`${key} • ${hpChartDate(e.dataColetaUtc)}`});
  }));
  for(const v of map.values())v.points.sort((a,b)=>new Date(a.date)-new Date(b.date));
  return map;
}
function hpLabSeriesFromPatient(exames){
  const map=new Map();
  (exames||[]).forEach(e=>(e.resultados||[]).forEach(r=>{
    const v=hpFinite(r.valorNumerico);if(v===null)return;
    const key=r.marcador||r.marcadorNome||'Marcador';
    if(!map.has(key))map.set(key,{unit:r.unidade||'',points:[]});
    map.get(key).points.push({date:e.dataColetaUtc,value:v,label:`${key} • ${hpChartDate(e.dataColetaUtc)}`});
  }));
  for(const v of map.values())v.points.sort((a,b)=>new Date(a.date)-new Date(b.date));
  return map;
}
function hpLabCharts(map,limit=6){
  const series=[...map.entries()].filter(([,v])=>v.points.length>=2).slice(0,limit);
  if(!series.length)return `<div class="analytics-empty">Ainda não existem marcadores numéricos com duas ou mais coletas para desenhar uma tendência.</div>`;
  return hpMetricChartGrid(series.map(([name,v])=>hpLineChart(name,v.points,v.unit?` ${v.unit}`:'')));
}
function hpLoadCharts(evolucaoCarga,limit=6){
  const rows=(evolucaoCarga||[]).filter(x=>(x.registros||[]).filter(r=>hpFinite(r.cargaRealizada)!==null).length>=2).slice(0,limit);
  if(!rows.length)return `<div class="analytics-empty">Registre carga em pelo menos dois treinos do mesmo exercício para visualizar a progressão.</div>`;
  return hpMetricChartGrid(rows.map(x=>hpLineChart(
    x.exercicio,
    (x.registros||[]).map(r=>({date:r.dataHoraInicioUtc,value:r.cargaRealizada})),
    ' kg'
  )));
}
function hpPatientLoadEvolution(execHistory){
  const map=new Map();
  (execHistory||[]).slice().reverse().forEach(exec=>(exec.itens||[]).forEach(i=>{
    const v=hpFinite(i.cargaRealizada);if(v===null)return;
    const key=i.exercicio||'Exercício';
    if(!map.has(key))map.set(key,[]);
    map.get(key).push({date:exec.dataHoraInicioUtc,value:v});
  }));
  return [...map.entries()].filter(([,pts])=>pts.length>=2).map(([exercicio,registros])=>({exercicio,registros}));
}
function hpInjectAnalyticsSection(host,title,subtitle,body,id){
  if(!host||host.querySelector(`[data-analytics-id="${id}"]`))return;
  const section=document.createElement('section');
  section.className='card full-card analytics-section';
  section.dataset.analyticsId=id;
  section.innerHTML=`<div class="card-head"><div><h3>${esc(title)}</h3><small>${esc(subtitle)}</small></div><span class="analytics-badge">SVG nativo</span></div>${body}`;
  host.appendChild(section);
}

// Prontuário profissional: adiciona gráficos às abas existentes sem substituir o conteúdo clínico.
const __renderPatientTab_v032=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v032(d);
  const host=$('#patientTabContent');
  if(!host)return;

  if(state.patientTab==='resumo'){
    hpInjectAnalyticsSection(
      host,
      'Painel analítico',
      'Tendências corporais registradas',
      hpEvalCharts(d.avaliacoes||[]),
      'professional-summary'
    );
  }
  if(state.patientTab==='avaliacoes'){
    hpInjectAnalyticsSection(
      host,
      'Evolução em gráficos',
      'Peso, IMC, gordura corporal e cintura',
      hpEvalCharts(d.avaliacoes||[]),
      'professional-evaluations'
    );
  }
  if(state.patientTab==='exames'){
    hpInjectAnalyticsSection(
      host,
      'Tendência laboratorial',
      'Marcadores numéricos com histórico suficiente',
      hpLabCharts(hpLabSeriesFromProfessional(d.exames||[])),
      'professional-labs'
    );
  }
  if(state.patientTab==='treinos'){
    hpInjectAnalyticsSection(
      host,
      'Progressão de carga',
      'Carga registrada nas execuções dos últimos 90 dias',
      hpLoadCharts(d.treinosHistorico?.evolucaoCarga||[]),
      'professional-workout-load'
    );
  }
};

// Portal do paciente: evolução corporal com gráficos antes da tabela.
const __loadPatientEvolution_v032=loadPatientEvolution;
loadPatientEvolution=async function(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/evolucao?limite=24'),items=d.itens||[];
  const last=items[items.length-1]||{};
  host.innerHTML=patientPageHeader('EVOLUÇÃO','Minha evolução corporal','Veja as mudanças das suas avaliações ao longo do tempo.')+`
    <div class="patient-plan-totals">
      ${metric(num(last.pesoKg),' kg','Peso atual')}
      ${metric(num(last.imc,2),'','IMC')}
      ${metric(num(last.percentualGordura),'%','Gordura')}
      ${metric(num(last.cinturaCm),' cm','Cintura')}
    </div>
    <section class="card analytics-section">
      <div class="card-head"><div><h3>Gráficos de evolução</h3><small>${items.length} avaliação(ões)</small></div><span class="analytics-badge">Minha evolução</span></div>
      ${hpEvalCharts(items)}
    </section>
    <article class="card">
      <div class="card-head"><h3>Histórico detalhado</h3><small>${items.length} avaliação(ões)</small></div>
      ${items.length?`<div class="evolution-table">
        <div class="evolution-row head"><span>Data</span><span>Peso</span><span>IMC</span><span>Gordura</span><span>Cintura</span><span>PA</span></div>
        ${items.slice().reverse().map(x=>`<div class="evolution-row"><strong>${fmtDate(x.dataUtc)}</strong><span>${num(x.pesoKg)} kg</span><span>${num(x.imc,2)}</span><span>${num(x.percentualGordura)}%</span><span>${num(x.cinturaCm)} cm</span><span>${x.pressaoSistolica&&x.pressaoDiastolica?`${x.pressaoSistolica}/${x.pressaoDiastolica}`:'—'}</span></div>`).join('')}
      </div>`:sectionEmpty('Nenhuma avaliação registrada.')}
    </article>`;
};

// Portal do paciente: exames com tendências numéricas.
const __loadPatientLabs_v032=loadPatientLabs;
loadPatientLabs=async function(){
  const host=$('#patientPortalContent'),d=await api('/api/portal/me/exames?limite=20'),exames=d.exames||[];
  host.innerHTML=patientPageHeader('LABORATÓRIO','Meus exames','Resultados e tendências dos seus marcadores laboratoriais.')+`
    <section class="card analytics-section">
      <div class="card-head"><div><h3>Tendências dos exames</h3><small>Marcadores numéricos ao longo do tempo</small></div><span class="analytics-badge">Histórico</span></div>
      ${hpLabCharts(hpLabSeriesFromPatient(exames))}
    </section>
    <div class="patient-lab-history">${exames.length?exames.map(e=>`
      <article class="card">
        <div class="card-head"><div><h3>${fmtDate(e.dataColetaUtc)}</h3><small>${esc(e.laboratorio||'Laboratório não informado')} • ${esc(e.profissional)}</small></div><span class="pill Ativa">${e.resultados?.length||0} resultado(s)</span></div>
        ${e.observacoes?`<p class="muted">${esc(e.observacoes)}</p>`:''}
        <div class="lab-result-grid">${(e.resultados||[]).map(r=>`
          <div class="lab-result-card"><div><strong>${esc(r.marcador)}</strong><span class="pill ${r.classificacao==='DentroDaReferencia'?'Ativa':r.classificacao}">${esc(r.classificacao)}</span></div><b>${r.valorNumerico!=null?num(r.valorNumerico,2):esc(r.valorTexto||'—')} ${esc(r.unidade||'')}</b><small>Referência: ${r.referenciaMinima!=null||r.referenciaMaxima!=null?`${r.referenciaMinima??'—'} — ${r.referenciaMaxima??'—'}`:esc(r.referenciaTexto||'não informada')}</small></div>`).join('')}</div>
      </article>`).join(''):sectionEmpty('Nenhum exame registrado.')}</div>`;
};

// Portal do paciente: acrescenta gráfico de carga ao treino sem remover ficha/histórico.
const __loadPatientWorkout_v032=loadPatientWorkout;
loadPatientWorkout=async function(){
  await __loadPatientWorkout_v032();
  const host=$('#patientPortalContent');
  try{
    const h=await api('/api/portal/me/treinos/historico?dias=90');
    const series=hpPatientLoadEvolution(h.execucoes||[]);
    hpInjectAnalyticsSection(
      host,
      'Minha progressão de carga',
      'Evolução das cargas realmente utilizadas nos últimos 90 dias',
      hpLoadCharts(series),
      'patient-workout-load'
    );
  }catch(err){
    console.warn('Não foi possível montar gráficos de carga:',err);
  }
};


// ===== v0.3.27 — Alertas clínicos + insights automáticos =====
function hpInsightIcon(categoria){
  return ({Exames:'🧪',Evolução:'📈',Agenda:'📅',Metas:'🎯',Treinos:'🏋️'})[categoria]||'●';
}
function hpInsightClass(sev){
  return sev==='Alta'?'insight-high':sev==='Media'?'insight-medium':'insight-low';
}
function hpInsightCard(x,compact=false){
  return `<article class="insight-card ${hpInsightClass(x.severidade)} ${compact?'compact':''}">
    <div class="insight-icon">${hpInsightIcon(x.categoria)}</div>
    <div class="insight-body">
      <div class="insight-top"><span>${esc(x.categoria)}</span><b>${esc(x.severidade)}</b></div>
      <strong>${esc(x.titulo)}</strong>
      ${compact?'':`<p>${esc(x.descricao)}</p>${x.valor?`<div class="insight-value">${esc(x.valor)}</div>`:''}${x.acaoSugerida?`<small><b>Ação sugerida:</b> ${esc(x.acaoSugerida)}</small>`:''}`}
    </div>
  </article>`;
}
function hpInsightsDisclaimer(){
  return `<div class="insight-disclaimer">Os insights são sinais automáticos baseados nos registros do sistema. Eles não representam diagnóstico, prescrição ou avaliação de urgência e devem ser interpretados pelo profissional no contexto clínico.</div>`;
}
function hpInsightsSummary(d){
  return `<div class="insight-summary">
    <div><strong>${d.total||d.totalInsights||0}</strong><span>Sinais</span></div>
    <div class="high"><strong>${d.alta||0}</strong><span>Alta</span></div>
    <div class="medium"><strong>${d.media||0}</strong><span>Média</span></div>
    <div class="low"><strong>${d.baixa||0}</strong><span>Baixa</span></div>
  </div>`;
}

// Dashboard profissional: adiciona uma central de atenção sem alterar o dashboard original.
const __loadDashboard_v033=loadDashboard;
loadDashboard=async function(){
  await __loadDashboard_v033();
  try{
    const d=await api('/api/insights/dashboard?limite=12');
    const section=document.createElement('section');
    section.className='card insight-dashboard-section';
    section.innerHTML=`<div class="card-head"><div><h3>Central de atenção</h3><small>${d.pacientesComInsights||0} paciente(s) com sinais automáticos</small></div><span class="analytics-badge">Insights</span></div>
      ${hpInsightsSummary(d)}
      ${d.pacientes?.length?`<div class="insight-patient-list">${d.pacientes.map(p=>`
        <article class="insight-patient-row clickable" data-insight-patient="${p.pacienteId}">
          <div class="mini-avatar">${initials(p.pacienteNome)}</div>
          <div class="insight-patient-main"><strong>${esc(p.pacienteNome)}</strong><small>${p.total} sinal(is) • maior prioridade: ${esc(p.severidadeMaxima)}</small><div>${(p.insights||[]).slice(0,2).map(x=>`<span class="mini-insight ${hpInsightClass(x.severidade)}">${esc(x.titulo)}</span>`).join('')}</div></div>
          <span class="pill ${hpInsightClass(p.severidadeMaxima)}">${esc(p.severidadeMaxima)}</span>
        </article>`).join('')}</div>`:`<div class="empty compact">Nenhum sinal automático ativo neste momento.</div>`}
      ${hpInsightsDisclaimer()}`;
    content.appendChild(section);
    $$('[data-insight-patient]').forEach(x=>x.onclick=()=>openPatient(x.dataset.insightPatient));
  }catch(err){
    console.warn('Insights do dashboard indisponíveis:',err);
  }
};

// Prontuário: injeta sinais na aba Resumo depois das camadas anteriores terminarem.
const __renderPatientTab_v033=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v033(d);
  if(state.patientTab!=='resumo'||!state.patientId)return;
  const host=$('#patientTabContent');
  api(`/api/pacientes/${state.patientId}/insights`).then(ins=>{
    if(!host||state.patientTab!=='resumo'||host.querySelector('[data-patient-insights]'))return;
    const section=document.createElement('section');
    section.className='card full-card patient-insights-section';
    section.dataset.patientInsights='1';
    section.innerHTML=`<div class="card-head"><div><h3>Insights de acompanhamento</h3><small>Leitura automática dos registros atuais</small></div><span class="analytics-badge">${ins.total||0} sinal(is)</span></div>
      ${hpInsightsSummary(ins)}
      ${ins.insights?.length?`<div class="patient-insight-grid">${ins.insights.map(x=>hpInsightCard(x)).join('')}</div>`:`<div class="empty compact">Nenhum sinal automático ativo para este paciente.</div>`}
      ${hpInsightsDisclaimer()}`;
    host.prepend(section);
  }).catch(err=>console.warn('Insights do paciente indisponíveis:',err));
};


// ===== v0.3.27 — Pendências + tratamento dos insights =====
const __navigate_v034=navigate;
navigate=function(view){
  if(view!=='pendencias')return __navigate_v034(view);
  state.view='pendencias';
  $$('.nav-item[data-view]').forEach(x=>x.classList.toggle('active',x.dataset.view===view));
  $('.sidebar').classList.remove('open');
  $('#pageEyebrow').textContent='ACOMPANHAMENTO';
  $('#pageTitle').textContent='Pendências';
  setLoading();
  loadPendencias().catch(e=>{content.innerHTML=`<div class="card empty">${esc(e.message)}</div>`;toast(e.message,true)});
};

const __hpInsightCard_v034=hpInsightCard;
hpInsightCard=function(x,compact=false){
  if(compact)return __hpInsightCard_v034(x,compact);
  const encoded=encodeURIComponent(JSON.stringify({
    origemCodigo:x.codigo||null,
    categoria:x.categoria||'Acompanhamento',
    severidade:x.severidade||'Media',
    titulo:x.titulo||'Insight',
    descricao:x.descricao||null,
    valorReferencia:x.valor||null,
    acaoSugerida:x.acaoSugerida||null
  }));
  return `<article class="insight-card ${hpInsightClass(x.severidade)}">
    <div class="insight-icon">${hpInsightIcon(x.categoria)}</div>
    <div class="insight-body">
      <div class="insight-top"><span>${esc(x.categoria)}</span><b>${esc(x.severidade)}</b></div>
      <strong>${esc(x.titulo)}</strong>
      <p>${esc(x.descricao)}</p>
      ${x.valor?`<div class="insight-value">${esc(x.valor)}</div>`:''}
      ${x.acaoSugerida?`<small><b>Ação sugerida:</b> ${esc(x.acaoSugerida)}</small>`:''}
      <div class="insight-actions"><button class="secondary insight-to-pending" data-insight-payload="${encoded}">+ Criar pendência</button></div>
    </div>
  </article>`;
};

document.addEventListener('click',async ev=>{
  const btn=ev.target.closest('.insight-to-pending');
  if(!btn)return;
  ev.preventDefault();ev.stopPropagation();
  if(!state.patientId){toast('Abra o prontuário do paciente para criar a pendência.',true);return}
  try{
    const payload=JSON.parse(decodeURIComponent(btn.dataset.insightPayload));
    const d=new Date();d.setDate(d.getDate()+7);
    payload.vencimentoUtc=d.toISOString();
    await api(`/api/pacientes/${state.patientId}/pendencias`,{method:'POST',body:JSON.stringify(payload)});
    toast('Pendência criada a partir do insight.');
    btn.textContent='Pendência criada';
    btn.disabled=true;
  }catch(err){toast(err.message,true)}
});

function pendingSeverityClass(s){
  return s==='Alta'?'insight-high':s==='Media'?'insight-medium':'insight-low';
}
function pendingStatusLabel(p){
  if(p.status==='Adiada'&&p.adiadaAteUtc)return `Adiada até ${fmtDateTime(p.adiadaAteUtc)}`;
  if(p.status==='Resolvida')return 'Resolvida';
  if(p.status==='Vista')return 'Vista';
  return 'Nova';
}
function pendingCard(p){
  return `<article class="pending-card ${pendingSeverityClass(p.severidade)}" data-pending="${p.id}">
    <div class="pending-check">${p.status==='Resolvida'?'✓':'!'}</div>
    <div class="pending-main">
      <div class="pending-meta"><span>${esc(p.categoria)}</span><b>${esc(p.severidade)}</b><span>${esc(pendingStatusLabel(p))}</span></div>
      <h4>${esc(p.titulo)}</h4>
      <p>${esc(p.descricao||'')}</p>
      <div class="pending-context">
        <span>Paciente: <b>${esc(p.pacienteNome||'')}</b></span>
        ${p.valorReferencia?`<span>Valor: <b>${esc(p.valorReferencia)}</b></span>`:''}
        ${p.vencimentoUtc?`<span>Prazo: <b>${fmtDateTime(p.vencimentoUtc)}</b></span>`:''}
        ${p.consultaRetornoId?`<span>Retorno agendado</span>`:''}
      </div>
      ${p.acaoSugerida?`<small class="pending-suggestion"><b>Ação sugerida:</b> ${esc(p.acaoSugerida)}</small>`:''}
      ${p.resolucao?`<small class="pending-resolution"><b>Registro:</b> ${esc(p.resolucao)}</small>`:''}
    </div>
    <div class="pending-actions">
      ${p.status==='Nova'?`<button class="secondary pending-view" data-id="${p.id}">Marcar vista</button>`:''}
      ${p.status!=='Resolvida'?`<button class="secondary pending-snooze" data-id="${p.id}">Adiar</button><button class="secondary pending-return" data-id="${p.id}" data-patient="${p.pacienteId}">Agendar retorno</button><button class="primary pending-resolve" data-id="${p.id}">Resolver</button>`:''}
      <button class="ghost pending-open-patient" data-patient="${p.pacienteId}">Abrir prontuário</button>
    </div>
  </article>`;
}

async function loadPendencias(status='abertas'){
  const d=await api(`/api/pendencias?status=${encodeURIComponent(status)}&limite=200`);
  content.innerHTML=`<div class="section-head"><div><h3>Pendências clínicas</h3><p>Organize os sinais que precisam de acompanhamento.</p></div><div class="toolbar pending-filter">
      <button class="${status==='abertas'?'primary':'secondary'}" data-pending-filter="abertas">Abertas</button>
      <button class="${status==='Adiada'?'primary':'secondary'}" data-pending-filter="Adiada">Adiadas</button>
      <button class="${status==='Resolvida'?'primary':'secondary'}" data-pending-filter="Resolvida">Resolvidas</button>
      <button class="${status==='todas'?'primary':'secondary'}" data-pending-filter="todas">Todas</button>
    </div></div>
    <div class="stats-grid pending-stats">
      ${stat('Exibidas',d.total,'no filtro atual')}
      ${stat('Novas',d.novas,'ainda não vistas')}
      ${stat('Vistas',d.vistas,'em acompanhamento')}
      ${stat('Resolvidas',d.resolvidas,'no filtro atual')}
    </div>
    <section class="card"><div class="card-head"><h3>Fila de acompanhamento</h3><small>${d.total} item(ns)</small></div>
      <div class="pending-list">${d.itens?.length?d.itens.map(pendingCard).join(''):sectionEmpty('Nenhuma pendência neste filtro.')}</div>
    </section>`;

  $$('[data-pending-filter]').forEach(b=>b.onclick=()=>loadPendencias(b.dataset.pendingFilter));
  $$('.pending-open-patient').forEach(b=>b.onclick=()=>openPatient(b.dataset.patient));
  $$('.pending-view').forEach(b=>b.onclick=async()=>{try{await api(`/api/pendencias/${b.dataset.id}/vista`,{method:'PUT'});toast('Pendência marcada como vista.');await loadPendencias(status)}catch(e){toast(e.message,true)}});
  $$('.pending-resolve').forEach(b=>b.onclick=()=>openResolvePending(b.dataset.id,status));
  $$('.pending-snooze').forEach(b=>b.onclick=()=>openSnoozePending(b.dataset.id,status));
  $$('.pending-return').forEach(b=>b.onclick=()=>openReturnPending(b.dataset.id,b.dataset.patient,status));
}

function pendingModal(title,body,onSave){
  const modal=$('#clinicalActionModal'),box=$('#clinicalActionContent');
  modal.classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">PENDÊNCIA</span><h2>${esc(title)}</h2></div>
    <form id="pendingActionForm" class="clinical-form">${body}<div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">Salvar</button></div></form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  $('#pendingActionForm').onsubmit=async e=>{e.preventDefault();const b=e.target.querySelector('button[type=submit]');b.disabled=true;try{await onSave(e.target);closeClinicalAction()}catch(err){toast(err.message,true)}finally{b.disabled=false}};
}
function openResolvePending(id,status){
  pendingModal('Resolver pendência',area('Como foi resolvida?','resolucao'),async f=>{
    await api(`/api/pendencias/${id}/resolver`,{method:'PUT',body:JSON.stringify({resolucao:val(f,'resolucao')})});
    toast('Pendência resolvida.');await loadPendencias(status);
  });
}
function openSnoozePending(id,status){
  const dt=new Date();dt.setDate(dt.getDate()+7);
  const local=new Date(dt.getTime()-dt.getTimezoneOffset()*60000).toISOString().slice(0,16);
  pendingModal('Adiar pendência',`${field('Relembrar em','quando','datetime-local',`value="${local}" required`)}${area('Motivo/observação','observacao')}`,async f=>{
    await api(`/api/pendencias/${id}/adiar`,{method:'PUT',body:JSON.stringify({adiadaAteUtc:new Date(val(f,'quando')).toISOString(),observacao:val(f,'observacao')})});
    toast('Pendência adiada.');await loadPendencias(status);
  });
}
function openReturnPending(id,pacienteId,status){
  const dt=new Date();dt.setDate(dt.getDate()+7);dt.setHours(14,0,0,0);
  const local=new Date(dt.getTime()-dt.getTimezoneOffset()*60000).toISOString().slice(0,16);
  pendingModal('Agendar retorno',`${field('Data e hora','dataHora','datetime-local',`value="${local}" required`)}${field('Motivo','motivo','text','value="Retorno de acompanhamento"')}`,async f=>{
    await api(`/api/pendencias/${id}/retorno`,{method:'POST',body:JSON.stringify({dataHoraUtc:new Date(val(f,'dataHora')).toISOString(),motivo:val(f,'motivo')})});
    toast('Retorno criado na agenda.');await loadPendencias(status);
  });
}

// Dashboard: mostra um resumo das pendências abertas.
const __loadDashboard_v034=loadDashboard;
loadDashboard=async function(){
  await __loadDashboard_v034();
  try{
    const p=await api('/api/pendencias?status=abertas&limite=6');
    const section=document.createElement('section');
    section.className='card dashboard-pending-section';
    section.innerHTML=`<div class="card-head"><div><h3>Pendências abertas</h3><small>${p.total} item(ns) prioritário(s)</small></div><button class="ghost" id="goPendencias">Gerenciar →</button></div>
      ${p.itens?.length?`<div class="dashboard-pending-list">${p.itens.map(x=>`<div class="dashboard-pending-row" data-open-pending-patient="${x.pacienteId}"><span class="pending-dot ${pendingSeverityClass(x.severidade)}"></span><div><strong>${esc(x.titulo)}</strong><small>${esc(x.pacienteNome)} • ${esc(x.categoria)}</small></div><span class="pill ${pendingSeverityClass(x.severidade)}">${esc(x.severidade)}</span></div>`).join('')}</div>`:sectionEmpty('Nenhuma pendência aberta.')}`;
    content.appendChild(section);
    $('#goPendencias').onclick=()=>navigate('pendencias');
    $$('[data-open-pending-patient]').forEach(x=>x.onclick=()=>openPatient(x.dataset.openPendingPatient));
  }catch(err){console.warn('Pendências do dashboard indisponíveis:',err)}
};


// ===== v0.3.27 — Notificações internas + lembretes =====
let hpNotificationTimer=null;
function notificationPriorityClass(p){
  return p==='Alta'?'notification-high':p==='Media'?'notification-medium':'notification-normal';
}
function notificationIcon(t){
  return t==='Agenda'||t==='Consulta'?'📅':t==='Pendencia'?'⚠️':'🔔';
}
function notificationBadgeNodes(){
  return [$('#notificationBadge'),$('#patientNotificationBadge')].filter(Boolean);
}
function setNotificationBadge(n){
  notificationBadgeNodes().forEach(x=>{
    x.textContent=n>99?'99+':String(n||0);
    x.classList.toggle('hidden',!n);
  });
}
async function refreshNotifications(silent=true){
  if(!state.token)return null;
  try{
    const d=await api('/api/notificacoes?sincronizar=true&limite=50');
    setNotificationBadge(d.naoLidas||0);
    if(!$('#notificationDrawer').classList.contains('hidden'))renderNotifications(d);
    return d;
  }catch(err){
    if(!silent)toast(err.message,true);
    return null;
  }
}
function renderNotifications(d){
  const host=$('#notificationList');
  if(!host)return;
  host.innerHTML=(d.itens||[]).length?(d.itens||[]).map(n=>`
    <article class="notification-item ${n.lida?'read':'unread'} ${notificationPriorityClass(n.prioridade)}" data-notification="${n.id}" data-notification-link="${esc(n.link||'')}">
      <div class="notification-icon">${notificationIcon(n.tipo)}</div>
      <div class="notification-main">
        <div class="notification-meta"><span>${esc(n.tipo)}</span><b>${esc(n.prioridade)}</b>${n.dataEventoUtc?`<span>${fmtDateTime(n.dataEventoUtc)}</span>`:''}</div>
        <strong>${esc(n.titulo)}</strong>
        <p>${esc(n.mensagem)}</p>
      </div>
      ${n.lida?'':'<span class="notification-unread-dot"></span>'}
    </article>`).join(''):`<div class="empty">Nenhuma notificação ativa.</div>`;
  $$('.notification-item').forEach(x=>x.onclick=()=>openNotification(x));
}
async function openNotification(el){
  const id=el.dataset.notification,link=el.dataset.notificationLink;
  try{await api(`/api/notificacoes/${id}/lida`,{method:'PUT'})}catch{}
  closeNotifications();
  await refreshNotifications();
  if((state.user?.tipoUsuario==='Paciente'||state.user?.tipo==='Paciente'||state.user?.tipoUsuario===6||state.user?.tipo===6)){
    if(['inicio','plano','treino','metas','diario','evolucao','exames'].includes(link)){
      await hpOpenPatientNotificationLink(link);
    }
    return;
  }
  if(link==='pendencias'){navigate('pendencias');return}
  if(link==='agenda'){navigate('agenda');return}
  if(link==='dashboard'){navigate('dashboard');return}
}
async function openNotifications(){
  $('#notificationDrawer').classList.remove('hidden');
  $('#notificationList').innerHTML='<div class="empty">Carregando...</div>';
  const d=await refreshNotifications(false);
  if(d)renderNotifications(d);
}
function closeNotifications(){
  $('#notificationDrawer')?.classList.add('hidden');
}
$('#notificationButton')?.addEventListener('click',openNotifications);
$('#patientNotificationButton')?.addEventListener('click',openNotifications);
$$('[data-close-notifications]').forEach(x=>x.addEventListener('click',closeNotifications));
$('#readAllNotifications')?.addEventListener('click',async()=>{
  try{
    await api('/api/notificacoes/ler-todas',{method:'PUT'});
    toast('Notificações marcadas como lidas.');
    await refreshNotifications(false);
  }catch(err){toast(err.message,true)}
});
document.addEventListener('keydown',e=>{if(e.key==='Escape')closeNotifications()});

function startNotificationPolling(){
  if(hpNotificationTimer)clearInterval(hpNotificationTimer);
  refreshNotifications();
  hpNotificationTimer=setInterval(()=>refreshNotifications(),60000);
}
function stopNotificationPolling(){
  if(hpNotificationTimer){clearInterval(hpNotificationTimer);hpNotificationTimer=null}
  setNotificationBadge(0);
}

// Encaixa o motor no ciclo de login/logout sem mexer no backend de autenticação.
const __showApp_v035=showApp;
showApp=function(){
  __showApp_v035();
  startNotificationPolling();
};
const __logout_v035=logout;
logout=function(){
  stopNotificationPolling();
  closeNotifications();
  __logout_v035();
};
if(state.token)setTimeout(startNotificationPolling,300);


// ===== v0.3.27 — Carteira de pacientes + priorização =====
const __navigate_v037=navigate;
navigate=function(view){
  if(view!=='carteira')return __navigate_v037(view);
  state.view='carteira';
  $$('.nav-item[data-view]').forEach(x=>x.classList.toggle('active',x.dataset.view===view));
  $('.sidebar').classList.remove('open');
  $('#pageEyebrow').textContent='ACOMPANHAMENTO';
  $('#pageTitle').textContent='Carteira de pacientes';
  setLoading();
  loadCarteira().catch(e=>{content.innerHTML=`<div class="card empty">${esc(e.message)}</div>`;toast(e.message,true)});
};

function portfolioPriorityClass(p){
  return p==='Alta'?'portfolio-high':p==='Media'?'portfolio-medium':p==='Baixa'?'portfolio-low':'portfolio-stable';
}
function portfolioPatientCard(p){
  return `<article class="portfolio-patient-card ${portfolioPriorityClass(p.prioridade)}" data-portfolio-patient="${p.pacienteId}">
    <div class="portfolio-card-top">
      <div class="mini-avatar">${initials(p.nome)}</div>
      <div class="portfolio-identity"><strong>${esc(p.nome)}</strong><small>${esc(p.email||'Sem e-mail')}</small></div>
      <span class="portfolio-priority">${esc(p.prioridade==='SemSinais'?'Estável':p.prioridade)}</span>
    </div>
    <div class="portfolio-reason">${esc(p.motivoPrioridade)}</div>
    <div class="portfolio-metrics">
      <div><strong>${p.insights||0}</strong><span>Insights</span></div>
      <div><strong>${p.pendenciasAbertas||0}</strong><span>Pendências</span></div>
      <div><strong>${p.treinosUltimos30Dias||0}</strong><span>Treinos 30d</span></div>
      <div><strong>${p.contatosUltimos30Dias||0}</strong><span>Contatos 30d</span></div>
    </div>
    <div class="portfolio-dates">
      <span><b>Última consulta</b>${p.ultimaConsultaUtc?fmtDateTime(p.ultimaConsultaUtc):'—'}</span>
      <span><b>Próxima consulta</b>${p.proximaConsultaUtc?fmtDateTime(p.proximaConsultaUtc):'Sem retorno'}</span>
      <span><b>Última avaliação</b>${p.ultimaAvaliacaoUtc?fmtDate(p.ultimaAvaliacaoUtc):'—'}</span>
      <span><b>Último exame</b>${p.ultimoExameUtc?fmtDate(p.ultimoExameUtc):'—'}</span>
      <span><b>Último contato</b>${p.ultimoContatoUtc?fmtDateTime(p.ultimoContatoUtc):'—'}</span>
      <span><b>Próximo contato</b>${p.proximoContatoUtc?fmtDateTime(p.proximoContatoUtc):'—'}</span>
    </div>
    <div class="portfolio-card-actions">
      <button class="secondary portfolio-contact" data-id="${p.pacienteId}" data-name="${esc(p.nome)}">Registrar contato</button>
      <button class="secondary portfolio-return" data-id="${p.pacienteId}" data-name="${esc(p.nome)}">Agendar retorno</button>
      <button class="ghost portfolio-new-pending" data-id="${p.pacienteId}" data-name="${esc(p.nome)}">+ Pendência</button>
      <button class="ghost portfolio-open" data-id="${p.pacienteId}">Prontuário</button>
    </div>
  </article>`;
}

async function loadCarteira(filters={}){
  const busca=filters.busca??$('#portfolioSearch')?.value??'';
  const prioridade=filters.prioridade??$('#portfolioPriority')?.value??'Todas';
  const ordenar=filters.ordenar??$('#portfolioSort')?.value??'score';
  const q=new URLSearchParams({busca,prioridade,ordenar});
  const d=await api(`/api/carteira?${q.toString()}`);

  content.innerHTML=`<div class="section-head portfolio-head">
    <div><h3>Carteira de pacientes</h3><p>Priorize o acompanhamento pela situação clínica e operacional registrada.</p></div>
  </div>
  <div class="stats-grid portfolio-stats">
    ${stat('Pacientes',d.totalPacientes,'ativos na organização')}
    ${stat('Alta prioridade',d.alta,'revisar primeiro')}
    ${stat('Com pendências',d.comPendencias,'acompanhamento aberto')}
    ${stat('Sem retorno',d.semRetornoFuturo,'sem consulta futura')}
  </div>
  <section class="card portfolio-toolbar-card">
    <div class="portfolio-toolbar">
      <label>Buscar<input id="portfolioSearch" value="${esc(busca)}" placeholder="Nome ou e-mail"></label>
      <label>Prioridade<select id="portfolioPriority">
        ${['Todas','Alta','Media','Baixa','SemSinais'].map(x=>`<option value="${x}" ${x===prioridade?'selected':''}>${x==='Media'?'Média':x==='SemSinais'?'Estável':x}</option>`).join('')}
      </select></label>
      <label>Ordenar<select id="portfolioSort">
        <option value="score" ${ordenar==='score'?'selected':''}>Prioridade</option>
        <option value="nome" ${ordenar==='nome'?'selected':''}>Nome</option>
        <option value="retorno" ${ordenar==='retorno'?'selected':''}>Próximo retorno</option>
        <option value="consulta" ${ordenar==='consulta'?'selected':''}>Última consulta</option>
      </select></label>
      <button class="secondary" id="portfolioApply">Aplicar</button>
    </div>
  </section>
  <section class="portfolio-grid">
    ${d.pacientes?.length?d.pacientes.map(portfolioPatientCard).join(''):`<div class="card empty">Nenhum paciente corresponde aos filtros.</div>`}
  </section>`;

  $('#portfolioApply').onclick=()=>loadCarteira();
  $('#portfolioSearch').addEventListener('keydown',e=>{if(e.key==='Enter')loadCarteira()});
  $('#portfolioPriority').onchange=()=>loadCarteira();
  $('#portfolioSort').onchange=()=>loadCarteira();

  $$('.portfolio-open').forEach(b=>b.onclick=e=>{e.stopPropagation();openPatient(b.dataset.id)});
  $$('.portfolio-contact').forEach(b=>b.onclick=e=>{e.stopPropagation();openPortfolioContact(b.dataset.id,b.dataset.name)});
  $$('.portfolio-return').forEach(b=>b.onclick=e=>{e.stopPropagation();openPortfolioReturn(b.dataset.id,b.dataset.name)});
  $$('.portfolio-new-pending').forEach(b=>b.onclick=e=>{e.stopPropagation();openPortfolioPending(b.dataset.id,b.dataset.name)});
  $$('[data-portfolio-patient]').forEach(card=>card.onclick=e=>{
    if(e.target.closest('button'))return;
    openPatient(card.dataset.portfolioPatient);
  });
  $$('.portfolio-pending').forEach(b=>b.onclick=e=>{
    e.stopPropagation();
    navigate('pendencias');
  });
}

const __loadDashboard_v037=loadDashboard;
loadDashboard=async function(){
  await __loadDashboard_v037();
  try{
    const d=await api('/api/carteira?ordenar=score');
    const top=(d.pacientes||[]).slice(0,4);
    const section=document.createElement('section');
    section.className='card dashboard-portfolio-section';
    section.innerHTML=`<div class="card-head"><div><h3>Pacientes para acompanhar</h3><small>Priorização automática da carteira</small></div><button class="ghost" id="openPortfolio">Ver carteira →</button></div>
      <div class="dashboard-portfolio-list">${top.length?top.map(p=>`
        <article data-dashboard-portfolio="${p.pacienteId}" class="dashboard-portfolio-row">
          <div class="mini-avatar">${initials(p.nome)}</div>
          <div><strong>${esc(p.nome)}</strong><small>${esc(p.motivoPrioridade)}</small></div>
          <span class="portfolio-priority ${portfolioPriorityClass(p.prioridade)}">${esc(p.prioridade==='SemSinais'?'Estável':p.prioridade)}</span>
        </article>`).join(''):sectionEmpty('Nenhum paciente na carteira.')}</div>`;
    content.appendChild(section);
    $('#openPortfolio').onclick=()=>navigate('carteira');
    $$('[data-dashboard-portfolio]').forEach(x=>x.onclick=()=>openPatient(x.dataset.dashboardPortfolio));
  }catch(err){console.warn('Carteira do dashboard indisponível:',err)}
};


// ===== v0.3.27 — Follow-up + ações rápidas da carteira =====
function portfolioActionModal(eyebrow,title,subtitle,body,onSubmit,submitText='Salvar'){
  const modal=$('#clinicalActionModal'),box=$('#clinicalActionContent');
  modal.classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">${esc(eyebrow)}</span><h2>${esc(title)}</h2><p>${esc(subtitle||'')}</p></div>
    <form id="portfolioActionForm" class="clinical-form">${body}<div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button class="primary" type="submit">${esc(submitText)}</button></div></form>`;
  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  $('#portfolioActionForm').onsubmit=async e=>{
    e.preventDefault();
    const b=e.target.querySelector('button[type=submit]');b.disabled=true;
    try{await onSubmit(e.target);closeClinicalAction()}catch(err){toast(err.message,true)}
    finally{b.disabled=false}
  };
}
function hpLocalInputDate(date){
  return new Date(date.getTime()-date.getTimezoneOffset()*60000).toISOString().slice(0,16);
}
function openPortfolioContact(patientId,name){
  const now=hpLocalInputDate(new Date());
  portfolioActionModal('FOLLOW-UP','Registrar contato',name,`
    <div class="form-grid three">
      <label>Canal<select name="canal"><option>WhatsApp</option><option>Telefone</option><option>Email</option><option>Presencial</option><option>Outro</option></select></label>
      ${field('Data e hora','dataHora','datetime-local',`value="${now}" required`)}
      ${field('Próximo contato','proximoContato','datetime-local')}
    </div>
    ${field('Resultado','resultado','text','placeholder="Ex.: Paciente respondeu e confirmou retorno" required')}
    ${area('Observações','observacoes')}
  `,async f=>{
    const dataHora=val(f,'dataHora');
    const proximo=val(f,'proximoContato');
    await api(`/api/pacientes/${patientId}/followups`,{method:'POST',body:JSON.stringify({
      dataHoraUtc:dataHora?new Date(dataHora).toISOString():new Date().toISOString(),
      canal:f.querySelector('[name=canal]').value,
      resultado:val(f,'resultado'),
      observacoes:val(f,'observacoes'),
      proximoContatoUtc:proximo?new Date(proximo).toISOString():null
    })});
    toast('Contato registrado no acompanhamento.');
    if(state.view==='carteira')await loadCarteira();
  },'Registrar contato');
}
function openPortfolioReturn(patientId,name){
  const dt=new Date();dt.setDate(dt.getDate()+7);dt.setHours(14,0,0,0);
  portfolioActionModal('AGENDA','Agendar retorno',name,`
    ${field('Data e hora','dataHora','datetime-local',`value="${hpLocalInputDate(dt)}" required`)}
    ${field('Motivo','motivo','text','value="Retorno de acompanhamento" required')}
    ${area('Orientações/observações','orientacoes')}
  `,async f=>{
    const data=val(f,'dataHora');
    await api(`/api/pacientes/${patientId}/consultas`,{method:'POST',body:JSON.stringify({
      dataHoraUtc:new Date(data).toISOString(),
      motivo:val(f,'motivo'),
      queixaPrincipal:null,
      evolucao:null,
      conduta:null,
      orientacoes:val(f,'orientacoes'),
      status:'Agendada'
    })});
    toast('Retorno agendado.');
    if(state.view==='carteira')await loadCarteira();
  },'Agendar');
}
function openPortfolioPending(patientId,name){
  const dt=new Date();dt.setDate(dt.getDate()+7);
  portfolioActionModal('PENDÊNCIA','Criar pendência',name,`
    <div class="form-grid two">
      ${field('Título','titulo','text','placeholder="Ex.: Confirmar retorno" required')}
      <label>Prioridade<select name="severidade"><option>Media</option><option>Alta</option><option>Baixa</option></select></label>
    </div>
    ${field('Prazo','vencimento','datetime-local',`value="${hpLocalInputDate(dt)}"`)}
    ${area('Descrição','descricao')}
  `,async f=>{
    const vencimento=val(f,'vencimento');
    await api(`/api/pacientes/${patientId}/pendencias`,{method:'POST',body:JSON.stringify({
      origemCodigo:null,
      categoria:'Follow-up',
      severidade:f.querySelector('[name=severidade]').value,
      titulo:val(f,'titulo'),
      descricao:val(f,'descricao'),
      valorReferencia:null,
      acaoSugerida:null,
      vencimentoUtc:vencimento?new Date(vencimento).toISOString():null
    })});
    toast('Pendência criada.');
    if(state.view==='carteira')await loadCarteira();
  },'Criar pendência');
}

// Histórico de follow-up dentro do Resumo do prontuário.
const __renderPatientTab_v038=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v038(d);
  if(state.patientTab!=='resumo'||!state.patientId)return;
  const host=$('#patientTabContent');
  api(`/api/pacientes/${state.patientId}/followups?limite=12`).then(f=>{
    if(!host||state.patientTab!=='resumo'||host.querySelector('[data-followup-history]'))return;
    const section=document.createElement('section');
    section.className='card full-card followup-history-section';
    section.dataset.followupHistory='1';
    section.innerHTML=`<div class="card-head"><div><h3>Follow-up</h3><small>${f.total||0} contato(s) registrado(s)</small></div><button class="secondary" id="patientQuickContact">+ Registrar contato</button></div>
      ${(f.itens||[]).length?`<div class="followup-history-list">${f.itens.map(x=>`
        <article>
          <div class="followup-channel">${x.canal==='WhatsApp'?'💬':x.canal==='Telefone'?'☎':x.canal==='Email'?'✉':x.canal==='Presencial'?'👤':'●'}</div>
          <div><strong>${esc(x.resultado)}</strong><small>${fmtDateTime(x.dataHoraUtc)} • ${esc(x.canal)} • ${esc(x.profissionalNome)}</small>${x.observacoes?`<p>${esc(x.observacoes)}</p>`:''}</div>
          ${x.proximoContatoUtc?`<span>Próximo: ${fmtDateTime(x.proximoContatoUtc)}</span>`:''}
        </article>`).join('')}</div>`:sectionEmpty('Nenhum contato de follow-up registrado.')}
      `;
    host.appendChild(section);
    $('#patientQuickContact').onclick=()=>openPortfolioContact(state.patientId,d.p.nome);
  }).catch(err=>console.warn('Follow-up indisponível:',err));
};


// ===== v0.3.27 — Fila de follow-up + lembretes =====
const __navigate_v039=navigate;
navigate=function(view){
  if(view!=='followups')return __navigate_v039(view);
  state.view='followups';
  $$('.nav-item[data-view]').forEach(x=>x.classList.toggle('active',x.dataset.view===view));
  $('.sidebar').classList.remove('open');
  $('#pageEyebrow').textContent='RELACIONAMENTO';
  $('#pageTitle').textContent='Fila de follow-up';
  setLoading();
  loadFollowUpQueue().catch(e=>{content.innerHTML=`<div class="card empty">${esc(e.message)}</div>`;toast(e.message,true)});
};

function followQueueClass(x){
  return x.faixa==='Vencido'?'follow-queue-overdue':x.faixa==='Hoje'?'follow-queue-today':x.faixa==='Proximos7Dias'?'follow-queue-soon':'follow-queue-future';
}
function followQueueLabel(x){
  if(x.faixa==='Vencido')return x.diasAtraso===1?'1 dia atrasado':`${x.diasAtraso} dias atrasado`;
  if(x.faixa==='Hoje')return 'Contato previsto hoje';
  if(x.faixa==='Proximos7Dias')return 'Próximos 7 dias';
  return 'Futuro';
}
function followQueueCard(x){
  return `<article class="follow-queue-card ${followQueueClass(x)}">
    <div class="follow-queue-status"><span></span><b>${esc(followQueueLabel(x))}</b></div>
    <div class="follow-queue-main">
      <div class="follow-queue-name"><div class="mini-avatar">${initials(x.pacienteNome)}</div><div><strong>${esc(x.pacienteNome)}</strong><small>${esc(x.telefone||x.email||'Sem contato cadastrado')}</small></div></div>
      <div class="follow-queue-details">
        <span><b>Próximo contato</b>${fmtDateTime(x.proximoContatoUtc)}</span>
        <span><b>Último contato</b>${fmtDateTime(x.ultimoContatoUtc)} • ${esc(x.ultimoCanal)}</span>
        <span><b>Último resultado</b>${esc(x.ultimoResultado)}</span>
        <span><b>Contatos 30d</b>${x.contatosUltimos30Dias}</span>
      </div>
    </div>
    <div class="follow-queue-actions">
      <button class="primary follow-queue-contact" data-id="${x.pacienteId}" data-name="${esc(x.pacienteNome)}">Registrar contato</button>
      <button class="secondary follow-queue-patient" data-id="${x.pacienteId}">Prontuário</button>
    </div>
  </article>`;
}
async function loadFollowUpQueue(filters={}){
  const faixa=filters.faixa??$('#followQueueFilter')?.value??'abertos';
  const busca=filters.busca??$('#followQueueSearch')?.value??'';
  const q=new URLSearchParams({faixa,busca});
  const d=await api(`/api/followups/fila?${q.toString()}`);

  content.innerHTML=`<div class="section-head"><div><h3>Fila de follow-up</h3><p>Contatos previstos, vencidos e próximos em uma única visão.</p></div></div>
    <div class="stats-grid follow-queue-stats">
      ${stat('Com follow-up',d.total,'pacientes com próximo contato')}
      ${stat('Vencidos',d.vencidos,'precisam de atenção')}
      ${stat('Hoje',d.hoje,'contatos previstos')}
      ${stat('Próximos 7 dias',d.proximos7Dias,'programados')}
    </div>
    <section class="card follow-queue-toolbar-card">
      <div class="follow-queue-toolbar">
        <label>Buscar<input id="followQueueSearch" value="${esc(busca)}" placeholder="Paciente, telefone ou e-mail"></label>
        <label>Faixa<select id="followQueueFilter">
          <option value="abertos" ${faixa==='abertos'?'selected':''}>Vencidos + hoje + 7 dias</option>
          <option value="vencidos" ${faixa==='vencidos'?'selected':''}>Vencidos</option>
          <option value="hoje" ${faixa==='hoje'?'selected':''}>Hoje</option>
          <option value="7dias" ${faixa==='7dias'?'selected':''}>Próximos 7 dias</option>
          <option value="futuro" ${faixa==='futuro'?'selected':''}>Futuros</option>
          <option value="todos" ${faixa==='todos'?'selected':''}>Todos</option>
        </select></label>
        <button class="secondary" id="followQueueApply">Aplicar</button>
      </div>
    </section>
    <section class="follow-queue-list">${d.itens?.length?d.itens.map(followQueueCard).join(''):`<div class="card empty">Nenhum follow-up nesta faixa.</div>`}</section>`;

  $('#followQueueApply').onclick=()=>loadFollowUpQueue();
  $('#followQueueFilter').onchange=()=>loadFollowUpQueue();
  $('#followQueueSearch').addEventListener('keydown',e=>{if(e.key==='Enter')loadFollowUpQueue()});

  $$('.follow-queue-contact').forEach(b=>b.onclick=()=>{
    openPortfolioContact(b.dataset.id,b.dataset.name);
  });
  $$('.follow-queue-patient').forEach(b=>b.onclick=()=>openPatient(b.dataset.id));
}

// Depois de registrar contato pela fila, a lista se atualiza automaticamente.
const __openPortfolioContact_v039=openPortfolioContact;
openPortfolioContact=function(patientId,name){
  __openPortfolioContact_v039(patientId,name);
  const form=$('#portfolioActionForm');
  if(!form)return;
  const original=form.onsubmit;
  form.onsubmit=async e=>{
    await original(e);
    if(state.view==='followups' && $('#clinicalActionModal')?.classList.contains('hidden')){
      await loadFollowUpQueue();
    }
  };
};

// Dashboard: contatos que merecem atenção.
const __loadDashboard_v039=loadDashboard;
loadDashboard=async function(){
  await __loadDashboard_v039();
  try{
    const d=await api('/api/followups/fila?faixa=abertos');
    const top=(d.itens||[]).slice(0,4);
    const section=document.createElement('section');
    section.className='card dashboard-followup-section';
    section.innerHTML=`<div class="card-head"><div><h3>Follow-ups</h3><small>${d.vencidos||0} vencido(s) • ${d.hoje||0} para hoje</small></div><button class="ghost" id="openFollowUpQueue">Ver fila →</button></div>
      ${top.length?`<div class="dashboard-followup-list">${top.map(x=>`
        <article data-dashboard-followup="${x.pacienteId}">
          <span class="followup-mini-dot ${followQueueClass(x)}"></span>
          <div><strong>${esc(x.pacienteNome)}</strong><small>${esc(followQueueLabel(x))} • ${fmtDateTime(x.proximoContatoUtc)}</small></div>
          <span>${x.contatosUltimos30Dias} contato(s)</span>
        </article>`).join('')}</div>`:sectionEmpty('Nenhum follow-up próximo.')}`;
    content.appendChild(section);
    $('#openFollowUpQueue').onclick=()=>navigate('followups');
    $$('[data-dashboard-followup]').forEach(x=>x.onclick=()=>openPatient(x.dataset.dashboardFollowup));
  }catch(err){console.warn('Fila de follow-up do dashboard indisponível:',err)}
};

// Notificação de follow-up leva para a nova fila.
const __openNotification_v039=openNotification;
openNotification=async function(el){
  const link=el.dataset.notificationLink;
  if(link!=='followups')return __openNotification_v039(el);
  const id=el.dataset.notification;
  try{await api(`/api/notificacoes/${id}/lida`,{method:'PUT'})}catch{}
  closeNotifications();
  await refreshNotifications();
  navigate('followups');
};


// ===== v0.3.27 — Gestão + indicadores operacionais =====
const __navigate_v0310=navigate;
navigate=function(view){
  if(view!=='gestao')return __navigate_v0310(view);
  state.view='gestao';
  $$('.nav-item[data-view]').forEach(x=>x.classList.toggle('active',x.dataset.view===view));
  $('.sidebar').classList.remove('open');
  $('#pageEyebrow').textContent='GESTÃO';
  $('#pageTitle').textContent='Indicadores operacionais';
  setLoading();
  loadManagement().catch(e=>{content.innerHTML=`<div class="card empty">${esc(e.message)}</div>`;toast(e.message,true)});
};

function managementBarChart(items){
  const max=Math.max(1,...(items||[]).map(x=>Number(x.valor)||0));
  return `<div class="management-bars">${(items||[]).map(x=>`
    <div class="management-bar-row">
      <span>${esc(x.rotulo)}</span>
      <div><i style="width:${Math.max(4,((Number(x.valor)||0)/max)*100)}%"></i></div>
      <b>${x.valor||0}</b>
    </div>`).join('')}</div>`;
}
function managementMiniSeries(items){
  const values=(items||[]).map(x=>Number(x.valor)||0);
  const max=Math.max(1,...values);
  return `<div class="management-week-series">${(items||[]).map(x=>`
    <div title="${esc(x.rotulo)}: ${x.valor}">
      <i style="height:${Math.max(8,((Number(x.valor)||0)/max)*100)}%"></i>
      <span>${esc(x.rotulo)}</span>
    </div>`).join('')}</div>`;
}
function managementAttentionRow(x){
  return `<article class="management-attention-row" data-management-patient="${x.pacienteId}">
    <div class="mini-avatar">${initials(x.nome)}</div>
    <div>
      <strong>${esc(x.nome)}</strong>
      <small>${x.pendenciasAbertas} pendência(s) • ${x.insightsEstimados} sinal(is)${x.semRetornoFuturo?' • sem retorno futuro':''}</small>
    </div>
    <button class="ghost management-open-patient" data-id="${x.pacienteId}">Abrir</button>
  </article>`;
}
async function loadManagement(diasOverride){
  const dias=diasOverride??Number($('#managementPeriod')?.value||30);
  const d=await api(`/api/gestao/resumo?dias=${dias}`);

  content.innerHTML=`<div class="section-head management-head">
    <div><h3>Gestão operacional</h3><p>Acompanhe volume, comparecimento e rotina de acompanhamento.</p></div>
    <div class="management-head-actions">
      <button class="secondary" id="managementExportCsv">Exportar CSV</button>
      <button class="secondary" id="managementPrintReport">Relatório imprimível</button>
      <label class="management-period">Período<select id="managementPeriod">
      ${[7,30,60,90,180,365].map(x=>`<option value="${x}" ${x===d.dias?'selected':''}>${x} dias</option>`).join('')}
    </select></label>
    </div>
  </div>

  <div class="stats-grid management-primary-stats">
    ${stat('Pacientes ativos',d.pacientesAtivos,`${d.pacientesNovos} novo(s) no período`)}
    ${stat('Consultas realizadas',d.consultasRealizadas,`${d.taxaComparecimentoPct}% de comparecimento`)}
    ${stat('Follow-ups',d.followUpsRealizados,`${d.followUpsVencidos} vencido(s)`)}
    ${stat('Pendências abertas',d.pendenciasAbertas,`${d.pendenciasResolvidasPeriodo} resolvida(s) no período`)}
  </div>

  <div class="management-grid">
    <section class="card">
      <div class="card-head"><div><h3>Consultas por status</h3><small>${d.consultasTotal} registro(s) no período</small></div></div>
      ${managementBarChart(d.consultasPorStatus)}
    </section>
    <section class="card">
      <div class="card-head"><div><h3>Atividade ao longo do período</h3><small>Consultas + follow-ups por semana</small></div></div>
      ${managementMiniSeries(d.atividadePorSemana)}
    </section>
  </div>

  <div class="management-secondary-grid">
    <section class="card">
      <div class="card-head"><h3>Engajamento registrado</h3><small>${d.dias} dias</small></div>
      <div class="management-engagement">
        <div><strong>${d.treinosRegistrados}</strong><span>Treinos registrados</span></div>
        <div><strong>${d.registrosDiario}</strong><span>Registros de diário</span></div>
        <div><strong>${d.registrosMetas}</strong><span>Registros de metas</span></div>
        <div><strong>${d.consultasAgendadas}</strong><span>Consultas agendadas</span></div>
      </div>
    </section>
    <section class="card">
      <div class="card-head"><h3>Ausências e cancelamentos</h3><small>Indicadores operacionais</small></div>
      <div class="management-engagement">
        <div><strong>${d.faltas}</strong><span>Faltas</span></div>
        <div><strong>${d.consultasCanceladas}</strong><span>Cancelamentos</span></div>
        <div><strong>${d.followUpsVencidos}</strong><span>Follow-ups vencidos</span></div>
        <div><strong>${d.pendenciasAbertas}</strong><span>Pendências abertas</span></div>
      </div>
    </section>
  </div>

  <section class="card management-attention-section">
    <div class="card-head"><div><h3>Pacientes que merecem revisão</h3><small>Ordenados por pendências, sinais e ausência de retorno</small></div><button class="ghost" id="managementOpenPortfolio">Abrir carteira →</button></div>
    <div class="management-attention-list">${d.pacientesAtencao?.length?d.pacientesAtencao.map(managementAttentionRow).join(''):sectionEmpty('Nenhum paciente com sinal operacional relevante.')}</div>
  </section>`;

  $('#managementPeriod').onchange=()=>loadManagement(Number($('#managementPeriod').value));
  $('#managementExportCsv').onclick=()=>downloadManagementCsv(Number($('#managementPeriod').value));
  $('#managementPrintReport').onclick=()=>openManagementPrintable(Number($('#managementPeriod').value));
  $('#managementOpenPortfolio').onclick=()=>navigate('carteira');
  $$('.management-open-patient').forEach(b=>b.onclick=e=>{e.stopPropagation();openPatient(b.dataset.id)});
  $$('[data-management-patient]').forEach(x=>x.onclick=e=>{if(!e.target.closest('button'))openPatient(x.dataset.managementPatient)});
}

// Dashboard ganha atalho leve para gestão.
const __loadDashboard_v0310=loadDashboard;
loadDashboard=async function(){
  await __loadDashboard_v0310();
  try{
    const d=await api('/api/gestao/resumo?dias=30');
    const section=document.createElement('section');
    section.className='card dashboard-management-section';
    section.innerHTML=`<div class="card-head"><div><h3>Resumo de gestão</h3><small>Últimos 30 dias</small></div><button class="ghost" id="openManagement">Ver indicadores →</button></div>
      <div class="dashboard-management-metrics">
        <div><strong>${d.taxaComparecimentoPct}%</strong><span>Comparecimento</span></div>
        <div><strong>${d.followUpsRealizados}</strong><span>Follow-ups</span></div>
        <div><strong>${d.pendenciasAbertas}</strong><span>Pendências</span></div>
        <div><strong>${d.pacientesNovos}</strong><span>Novos pacientes</span></div>
      </div>`;
    content.appendChild(section);
    $('#openManagement').onclick=()=>navigate('gestao');
  }catch(err){console.warn('Resumo de gestão indisponível:',err)}
};


// ===== v0.3.27 — Relatórios gerenciais + exportação =====
async function hpAuthenticatedBlob(url){
  const response=await fetch(url,{headers:{Authorization:`Bearer ${state.token}`}});
  if(!response.ok){
    let message=`Falha ao gerar arquivo (${response.status})`;
    try{const j=await response.json();message=j.message||message}catch{}
    throw new Error(message);
  }
  return {blob:await response.blob(),disposition:response.headers.get('content-disposition')||''};
}
function hpDispositionFilename(disposition,fallback){
  const utf=disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if(utf)return decodeURIComponent(utf[1].replace(/["']/g,''));
  const normal=disposition.match(/filename="?([^";]+)"?/i);
  return normal?normal[1]:fallback;
}
async function downloadManagementCsv(dias){
  try{
    const {blob,disposition}=await hpAuthenticatedBlob(`/api/gestao/export/csv?dias=${dias}`);
    const url=URL.createObjectURL(blob);
    const a=document.createElement('a');
    a.href=url;
    a.download=hpDispositionFilename(disposition,`healthplatform-gestao-${dias}d.csv`);
    document.body.appendChild(a);a.click();a.remove();
    setTimeout(()=>URL.revokeObjectURL(url),1500);
    toast('CSV gerado.');
  }catch(err){toast(err.message,true)}
}
async function openManagementPrintable(dias){
  try{
    const response=await fetch(`/api/gestao/export/html?dias=${dias}`,{headers:{Authorization:`Bearer ${state.token}`}});
    if(!response.ok)throw new Error(`Falha ao gerar relatório (${response.status})`);
    const html=await response.text();
    const blob=new Blob([html],{type:'text/html;charset=utf-8'});
    const url=URL.createObjectURL(blob);
    const win=window.open(url,'_blank');
    if(!win){
      URL.revokeObjectURL(url);
      throw new Error('O navegador bloqueou a nova janela do relatório.');
    }
    setTimeout(()=>URL.revokeObjectURL(url),60000);
  }catch(err){toast(err.message,true)}
}


// ===== v0.3.27 — Estabilização de notificações =====
function hpResolvePatientNotificationLink(link){
  const allowed=new Set(['inicio','plano','metas','diario','evolucao','exames','treino']);
  return allowed.has(link)?link:'inicio';
}
function hpOpenPatientNotificationLink(link){
  const target=hpResolvePatientNotificationLink(link);
  if(typeof loadPatientPortalView==='function')return loadPatientPortalView(target);
  if(typeof loadPatientSection==='function')return loadPatientSection(target);
  if(typeof navigatePatientPortal==='function')return navigatePatientPortal(target);
}
try{
  const logoutBtn=$('#logoutButton');
  if(logoutBtn)logoutBtn.onclick=()=>logout();
  const patientLogoutBtn=$('#patientLogoutButton');
  if(patientLogoutBtn)patientLogoutBtn.onclick=()=>logout();
}catch(err){
  console.warn('Falha ao religar logout ao cleanup de notificações:',err);
}


// ===== v0.3.27 — Busca global + central de ações =====
let hpGlobalSearchTimer=null;

function hpEnsureGlobalSearchModal(){
  let modal=$('#globalSearchModal');
  if(modal)return modal;
  modal=document.createElement('div');
  modal.id='globalSearchModal';
  modal.className='global-search-overlay hidden';
  modal.innerHTML=`<div class="global-search-dialog">
    <div class="global-search-input-wrap">
      <span>⌕</span>
      <input id="globalSearchInput" autocomplete="off" placeholder="Paciente, consulta, pendência, follow-up...">
      <kbd>Esc</kbd>
    </div>
    <div id="globalSearchMeta" class="global-search-meta">Digite pelo menos 2 caracteres.</div>
    <div id="globalSearchResults" class="global-search-results"></div>
  </div>`;
  document.body.appendChild(modal);

  modal.addEventListener('click',e=>{if(e.target===modal)hpCloseGlobalSearch()});
  $('#globalSearchInput').addEventListener('input',()=>{
    clearTimeout(hpGlobalSearchTimer);
    hpGlobalSearchTimer=setTimeout(()=>hpRunGlobalSearch(),220);
  });
  $('#globalSearchInput').addEventListener('keydown',e=>{
    if(e.key==='Enter'){
      const first=$('.global-search-result');
      if(first)first.click();
    }
  });
  return modal;
}

function hpOpenGlobalSearch(){
  const modal=hpEnsureGlobalSearchModal();
  modal.classList.remove('hidden');
  const input=$('#globalSearchInput');
  input.value='';
  $('#globalSearchResults').innerHTML='';
  $('#globalSearchMeta').textContent='Digite pelo menos 2 caracteres.';
  setTimeout(()=>input.focus(),0);
}

function hpCloseGlobalSearch(){
  const modal=$('#globalSearchModal');
  if(modal)modal.classList.add('hidden');
}

function hpSearchIcon(tipo){
  return tipo==='Paciente'?'👤':tipo==='Pendência'?'✓':tipo==='Follow-up'?'↻':'◷';
}

function hpSearchDate(value){
  return value?fmtDateTime(value):'';
}

function hpSearchResultHtml(x){
  return `<button class="global-search-result" data-search-type="${esc(x.tipo)}" data-search-id="${x.id}" data-search-patient="${x.pacienteId||''}" data-search-destination="${esc(x.destino||'')}">
    <span class="global-search-result-icon">${hpSearchIcon(x.tipo)}</span>
    <span class="global-search-result-main">
      <span><b>${esc(x.titulo)}</b><em>${esc(x.tipo)}</em>${x.severidade?`<em class="search-severity">${esc(x.severidade)}</em>`:''}</span>
      <small>${esc(x.subtitulo||'')}</small>
    </span>
    <span class="global-search-result-date">${esc(hpSearchDate(x.dataUtc))}</span>
  </button>`;
}

async function hpRunGlobalSearch(){
  const termo=$('#globalSearchInput')?.value.trim()||'';
  const results=$('#globalSearchResults');
  const meta=$('#globalSearchMeta');
  if(termo.length<2){
    meta.textContent='Digite pelo menos 2 caracteres.';
    results.innerHTML='';
    return;
  }

  meta.textContent='Buscando...';
  try{
    const d=await api(`/api/busca?termo=${encodeURIComponent(termo)}&limite=30`);
    meta.textContent=`${d.total||0} resultado(s) para “${termo}”`;
    results.innerHTML=d.itens?.length
      ? d.itens.map(hpSearchResultHtml).join('')
      : `<div class="global-search-empty">Nenhum resultado encontrado.</div>`;

    $$('.global-search-result').forEach(el=>el.onclick=()=>hpExecuteGlobalSearchResult(el));
  }catch(err){
    meta.textContent='Falha na busca.';
    results.innerHTML=`<div class="global-search-empty">${esc(err.message)}</div>`;
  }
}

function hpExecuteGlobalSearchResult(el){
  const type=el.dataset.searchType;
  const patientId=el.dataset.searchPatient;
  const destination=el.dataset.searchDestination;
  hpCloseGlobalSearch();

  if(type==='Paciente' && patientId){
    return openPatient(patientId);
  }
  if(destination==='pendencias'){
    navigate('pendencias');
    return;
  }
  if(destination==='followups'){
    navigate('followups');
    return;
  }
  if(destination==='agenda'){
    navigate('agenda');
    return;
  }
  if(patientId)return openPatient(patientId);
  navigate('carteira');
}

document.addEventListener('keydown',e=>{
  if((e.ctrlKey||e.metaKey) && e.key.toLowerCase()==='k'){
    e.preventDefault();
    hpOpenGlobalSearch();
  }else if(e.key==='Escape' && !$('#globalSearchModal')?.classList.contains('hidden')){
    hpCloseGlobalSearch();
  }
});

try{
  const globalSearchButton=$('#globalSearchButton');
  if(globalSearchButton)globalSearchButton.onclick=hpOpenGlobalSearch;
}catch(err){
  console.warn('Busca global indisponível:',err);
}


// ===== v0.3.27 — Central do Dia =====
const __navigate_v0314=navigate;
navigate=function(view){
  if(view!=='central-dia')return __navigate_v0314(view);
  state.view='central-dia';
  $$('.nav-item[data-view]').forEach(x=>x.classList.toggle('active',x.dataset.view===view));
  $('.sidebar').classList.remove('open');
  $('#pageEyebrow').textContent='ROTINA';
  $('#pageTitle').textContent='Central do dia';
  setLoading();
  loadCentralDia().catch(e=>{
    content.innerHTML=`<div class="card empty">${esc(e.message)}</div>`;
    toast(e.message,true);
  });
};

function hpBrowserOffsetMinutes(){ return -new Date().getTimezoneOffset(); }
function hpCentralEmpty(text){ return `<div class="central-day-empty">${esc(text)}</div>`; }

function hpCentralConsultation(x){
  return `<article class="central-day-row" data-central-patient="${x.pacienteId}">
    <div class="central-day-time">${esc(fmtDateTime(x.dataHoraUtc))}</div>
    <div><strong>${esc(x.pacienteNome)}</strong><small>${esc(x.status)}${x.motivo?` • ${esc(x.motivo)}`:''}</small></div>
    <button class="ghost central-open-patient" data-id="${x.pacienteId}">Prontuário</button>
  </article>`;
}
function hpCentralFollowup(x){
  const label=x.faixa==='Vencido'?(x.diasAtraso===1?'1 dia atrasado':`${x.diasAtraso} dias atrasado`):'Hoje';
  return `<article class="central-day-row central-followup ${x.faixa==='Vencido'?'is-overdue':''}">
    <div class="central-day-badge">${esc(label)}</div>
    <div><strong>${esc(x.pacienteNome)}</strong><small>${esc(x.ultimoCanal)} • ${esc(x.ultimoResultado)}</small></div>
    <button class="secondary central-register-contact" data-id="${x.pacienteId}" data-name="${esc(x.pacienteNome)}">Contato</button>
  </article>`;
}
function hpCentralPending(x){
  return `<article class="central-day-row ${x.severidade==='Alta'?'is-high':''}">
    <div class="central-day-badge">${esc(x.severidade)}</div>
    <div><strong>${esc(x.titulo)}</strong><small>${esc(x.pacienteNome)}${x.vencimentoUtc?` • ${esc(fmtDateTime(x.vencimentoUtc))}`:''}</small></div>
    <button class="ghost central-open-pending">Fila</button>
  </article>`;
}
function hpCentralPatient(x){
  return `<article class="central-day-row" data-central-patient="${x.pacienteId}">
    <div class="mini-avatar">${initials(x.pacienteNome)}</div>
    <div><strong>${esc(x.pacienteNome)}</strong><small>${x.pendenciasAbertas} pendência(s)${x.semRetornoFuturo?' • sem retorno futuro':''}</small></div>
    <button class="ghost central-open-patient" data-id="${x.pacienteId}">Abrir</button>
  </article>`;
}

async function loadCentralDia(){
  const d=await api(`/api/central-dia?offsetMinutos=${hpBrowserOffsetMinutes()}`);
  content.innerHTML=`<div class="section-head">
    <div><h3>Central do dia</h3><p>Consultas, contatos e pendências que merecem ação hoje.</p></div>
    <button class="secondary" id="centralRefresh">Atualizar</button>
  </div>
  <div class="stats-grid central-day-stats">
    ${stat('Consultas hoje',d.consultasHoje,'agenda do dia')}
    ${stat('Follow-ups vencidos',d.followUpsVencidos,'pedem contato')}
    ${stat('Follow-ups hoje',d.followUpsHoje,'previstos para hoje')}
    ${stat('Pendências prioritárias',d.pendenciasPrioritarias,'alta ou vencendo')}
  </div>
  <div class="central-day-grid">
    <section class="card">
      <div class="card-head"><div><h3>Agenda de hoje</h3><small>${d.consultasHoje} consulta(s)</small></div><button class="ghost" id="centralOpenAgenda">Abrir agenda →</button></div>
      <div class="central-day-list">${d.consultas?.length?d.consultas.map(hpCentralConsultation).join(''):hpCentralEmpty('Nenhuma consulta para hoje.')}</div>
    </section>
    <section class="card">
      <div class="card-head"><div><h3>Follow-ups</h3><small>${d.followUpsVencidos} vencido(s) • ${d.followUpsHoje} hoje</small></div><button class="ghost" id="centralOpenFollowups">Ver fila →</button></div>
      <div class="central-day-list">${d.followUps?.length?d.followUps.map(hpCentralFollowup).join(''):hpCentralEmpty('Nenhum contato pendente para hoje.')}</div>
    </section>
    <section class="card">
      <div class="card-head"><div><h3>Pendências prioritárias</h3><small>${d.pendenciasPrioritarias} item(ns)</small></div><button class="ghost" id="centralOpenPendencias">Ver fila →</button></div>
      <div class="central-day-list">${d.pendencias?.length?d.pendencias.map(hpCentralPending).join(''):hpCentralEmpty('Nenhuma pendência prioritária.')}</div>
    </section>
    <section class="card">
      <div class="card-head"><div><h3>Pacientes para revisão</h3><small>${d.pacientesRevisao} em destaque</small></div><button class="ghost" id="centralOpenPortfolio">Abrir carteira →</button></div>
      <div class="central-day-list">${d.pacientes?.length?d.pacientes.map(hpCentralPatient).join(''):hpCentralEmpty('Nenhuma revisão operacional sugerida.')}</div>
    </section>
  </div>`;

  $('#centralRefresh').onclick=()=>loadCentralDia();
  $('#centralOpenAgenda').onclick=()=>navigate('agenda');
  $('#centralOpenFollowups').onclick=()=>navigate('followups');
  $('#centralOpenPendencias').onclick=()=>navigate('pendencias');
  $('#centralOpenPortfolio').onclick=()=>navigate('carteira');
  $$('.central-open-pending').forEach(b=>b.onclick=()=>navigate('pendencias'));
  $$('.central-open-patient').forEach(b=>b.onclick=e=>{e.stopPropagation();openPatient(b.dataset.id)});
  $$('.central-register-contact').forEach(b=>b.onclick=()=>openPortfolioContact(b.dataset.id,b.dataset.name));
  $$('[data-central-patient]').forEach(x=>x.onclick=e=>{if(!e.target.closest('button'))openPatient(x.dataset.centralPatient)});
}

const __loadDashboard_v0314=loadDashboard;
loadDashboard=async function(){
  await __loadDashboard_v0314();
  try{
    const d=await api(`/api/central-dia?offsetMinutos=${hpBrowserOffsetMinutes()}`);
    const section=document.createElement('section');
    section.className='card dashboard-central-day';
    section.innerHTML=`<div class="card-head">
      <div><h3>Hoje</h3><small>${d.consultasHoje} consulta(s) • ${d.followUpsVencidos+d.followUpsHoje} follow-up(s) • ${d.pendenciasPrioritarias} pendência(s)</small></div>
      <button class="ghost" id="openCentralDay">Abrir central →</button>
    </div>
    <div class="dashboard-central-day-metrics">
      <div><strong>${d.consultasHoje}</strong><span>Consultas</span></div>
      <div><strong>${d.followUpsVencidos}</strong><span>Follow-ups vencidos</span></div>
      <div><strong>${d.followUpsHoje}</strong><span>Follow-ups hoje</span></div>
      <div><strong>${d.pendenciasPrioritarias}</strong><span>Pendências</span></div>
    </div>`;
    content.appendChild(section);
    $('#openCentralDay').onclick=()=>navigate('central-dia');
  }catch(err){
    console.warn('Central do dia indisponível no dashboard:',err);
  }
};



function hpClinicalSummaryText(r){
  if(!r)return '';
  const lines=[];
  lines.push(`RESUMO CLÍNICO — ${r.pacienteNome||'Paciente'}`);
  lines.push(`Gerado em: ${fmtDateTime(r.geradoEmUtc)}`);
  lines.push('');
  lines.push('AGENDA');
  lines.push(r.ultimaConsulta?`Última consulta: ${fmtDateTime(r.ultimaConsulta.dataHoraUtc)} — ${r.ultimaConsulta.status}${r.ultimaConsulta.motivo?` — ${r.ultimaConsulta.motivo}`:''}`:'Última consulta: não registrada');
  lines.push(r.proximaConsulta?`Próxima consulta: ${fmtDateTime(r.proximaConsulta.dataHoraUtc)}${r.proximaConsulta.motivo?` — ${r.proximaConsulta.motivo}`:''}`:'Próxima consulta: não agendada');
  lines.push('');
  lines.push('EVOLUÇÃO SOAP');
  if(r.ultimaEvolucao){
    lines.push(`Data: ${fmtDateTime(r.ultimaEvolucao.dataHoraUtc)} — ${r.ultimaEvolucao.profissionalNome}`);
    if(r.ultimaEvolucao.subjetivo)lines.push(`S: ${r.ultimaEvolucao.subjetivo}`);
    if(r.ultimaEvolucao.objetivo)lines.push(`O: ${r.ultimaEvolucao.objetivo}`);
    if(r.ultimaEvolucao.avaliacao)lines.push(`A: ${r.ultimaEvolucao.avaliacao}`);
    if(r.ultimaEvolucao.plano)lines.push(`P: ${r.ultimaEvolucao.plano}`);
  }else lines.push('Nenhuma evolução SOAP registrada.');
  lines.push('');
  lines.push('AVALIAÇÃO CORPORAL');
  if(r.ultimaAvaliacao){
    const a=r.ultimaAvaliacao;
    const dados=[a.pesoKg!=null?`${num(a.pesoKg)} kg`:null,a.imc!=null?`IMC ${num(a.imc,2)}`:null,a.percentualGordura!=null?`Gordura ${num(a.percentualGordura)}%`:null,a.cinturaCm!=null?`Cintura ${num(a.cinturaCm)} cm`:null].filter(Boolean);
    lines.push(`${fmtDate(a.dataUtc)}${dados.length?` — ${dados.join(' — ')}`:''}`);
  }else lines.push('Nenhuma avaliação corporal registrada.');
  lines.push('');
  lines.push('ANAMNESE');
  if(r.ultimaAnamnese){
    const a=r.ultimaAnamnese;
    lines.push(`Data: ${fmtDate(a.dataUtc)}`);
    if(a.objetivoAcompanhamento)lines.push(`Objetivo: ${a.objetivoAcompanhamento}`);
    lines.push(`Alergias: ${a.alergias||'não registradas'}`);
    lines.push(`Medicamentos: ${a.medicamentos||'não registrados'}`);
    if(a.suplementos)lines.push(`Suplementos: ${a.suplementos}`);
  }else lines.push('Nenhuma anamnese registrada.');
  lines.push('');
  lines.push('EXAMES FORA DA REFERÊNCIA');
  if(r.examesAlterados?.length)r.examesAlterados.forEach(x=>lines.push(`- ${x.marcador}: ${num(x.valorNumerico,2)} ${x.unidade||''} (${x.classificacao}) — ${fmtDate(x.dataColetaUtc)}`));
  else lines.push('Nenhum resultado numérico recente fora da referência registrada.');
  lines.push('');
  lines.push('ACOMPANHAMENTO');
  lines.push(`Metas ativas: ${r.metasAtivas}`);
  lines.push(`Treinos nos últimos 30 dias: ${r.treinosUltimos30Dias}`);
  lines.push(`Pendências abertas: ${r.pendenciasAbertas}`);
  lines.push(`Pendências de alta prioridade: ${r.pendenciasAltaPrioridade}`);
  lines.push('');
  lines.push('Resumo informativo. Consulte o prontuário completo para decisão clínica.');
  return lines.join('\n');
}

function hpClinicalSummaryPrintHtml(r){
  const escPrint=v=>esc(v??'');
  const row=(label,value)=>`<div class="print-row"><b>${escPrint(label)}</b><span>${escPrint(value||'—')}</span></div>`;
  const soap=r.ultimaEvolucao,body=r.ultimaAvaliacao,anam=r.ultimaAnamnese;
  return `<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><title>Resumo clínico — ${escPrint(r.pacienteNome)}</title><style>
  *{box-sizing:border-box}body{font-family:Arial,sans-serif;color:#1f2937;margin:28px;line-height:1.45}h1{font-size:22px;margin:0 0 4px}h2{font-size:15px;margin:22px 0 8px;padding-bottom:5px;border-bottom:1px solid #dfe4ea}.muted{color:#6b7280;font-size:12px}.metrics{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin:16px 0}.metric{border:1px solid #dfe4ea;border-radius:8px;padding:9px}.metric b{display:block;font-size:18px}.metric span{font-size:11px;color:#6b7280}.print-row{display:grid;grid-template-columns:150px 1fr;gap:10px;padding:5px 0}.soap{display:grid;grid-template-columns:1fr 1fr;gap:8px}.soap>div{border:1px solid #dfe4ea;border-radius:8px;padding:9px;white-space:pre-wrap}.labs{width:100%;border-collapse:collapse}.labs th,.labs td{border-bottom:1px solid #e5e7eb;text-align:left;padding:7px 5px;font-size:12px}.footer{margin-top:26px;font-size:10px;color:#6b7280}@media print{body{margin:12mm}}
  </style></head><body>
  <h1>Resumo clínico</h1><div class="muted">${escPrint(r.pacienteNome)} • gerado em ${escPrint(fmtDateTime(r.geradoEmUtc))}</div>
  <div class="metrics"><div class="metric"><b>${r.pendenciasAbertas}</b><span>Pendências abertas</span></div><div class="metric"><b>${r.pendenciasAltaPrioridade}</b><span>Alta prioridade</span></div><div class="metric"><b>${r.metasAtivas}</b><span>Metas ativas</span></div><div class="metric"><b>${r.treinosUltimos30Dias}</b><span>Treinos / 30 dias</span></div></div>
  <h2>Agenda</h2>${row('Última consulta',r.ultimaConsulta?`${fmtDateTime(r.ultimaConsulta.dataHoraUtc)} — ${r.ultimaConsulta.status}${r.ultimaConsulta.motivo?` — ${r.ultimaConsulta.motivo}`:''}`:'Não registrada')}${row('Próxima consulta',r.proximaConsulta?`${fmtDateTime(r.proximaConsulta.dataHoraUtc)}${r.proximaConsulta.motivo?` — ${r.proximaConsulta.motivo}`:''}`:'Não agendada')}
  <h2>Evolução SOAP</h2>${soap?`<div class="muted">${escPrint(fmtDateTime(soap.dataHoraUtc))} • ${escPrint(soap.profissionalNome)}</div><div class="soap"><div><b>S — Subjetivo</b><p>${escPrint(soap.subjetivo||'—')}</p></div><div><b>O — Objetivo</b><p>${escPrint(soap.objetivo||'—')}</p></div><div><b>A — Avaliação</b><p>${escPrint(soap.avaliacao||'—')}</p></div><div><b>P — Plano</b><p>${escPrint(soap.plano||'—')}</p></div></div>`:'<p>Nenhuma evolução SOAP registrada.</p>'}
  <h2>Avaliação corporal</h2>${body?`${row('Data',fmtDate(body.dataUtc))}${row('Peso',body.pesoKg!=null?`${num(body.pesoKg)} kg`:'—')}${row('IMC',body.imc!=null?num(body.imc,2):'—')}${row('Gordura',body.percentualGordura!=null?`${num(body.percentualGordura)}%`:'—')}${row('Cintura',body.cinturaCm!=null?`${num(body.cinturaCm)} cm`:'—')}`:'<p>Nenhuma avaliação corporal registrada.</p>'}
  <h2>Anamnese</h2>${anam?`${row('Data',fmtDate(anam.dataUtc))}${row('Objetivo',anam.objetivoAcompanhamento)}${row('Alergias',anam.alergias||'Não registradas')}${row('Medicamentos',anam.medicamentos||'Não registrados')}${row('Suplementos',anam.suplementos||'Não registrados')}`:'<p>Nenhuma anamnese registrada.</p>'}
  <h2>Exames fora da referência</h2>${r.examesAlterados?.length?`<table class="labs"><thead><tr><th>Marcador</th><th>Valor</th><th>Situação</th><th>Data</th></tr></thead><tbody>${r.examesAlterados.map(x=>`<tr><td>${escPrint(x.marcador)}</td><td>${escPrint(`${num(x.valorNumerico,2)} ${x.unidade||''}`)}</td><td>${escPrint(x.classificacao)}</td><td>${escPrint(fmtDate(x.dataColetaUtc))}</td></tr>`).join('')}</tbody></table>`:'<p>Nenhum resultado numérico recente fora da referência registrada.</p>'}
  <div class="footer">Resumo informativo gerado pelo HealthPlatform. Consulte o prontuário completo para decisão clínica.</div><script>window.addEventListener('load',()=>setTimeout(()=>window.print(),150));<\/script></body></html>`;
}

async function hpCopyClinicalSummary(){
  if(!state.patientId)return;
  try{
    const r=await api(`/api/pacientes/${state.patientId}/resumo-clinico`);
    const text=hpClinicalSummaryText(r);
    if(navigator.clipboard?.writeText)await navigator.clipboard.writeText(text);
    else{
      const ta=document.createElement('textarea');ta.value=text;ta.style.position='fixed';ta.style.opacity='0';
      document.body.appendChild(ta);ta.select();document.execCommand('copy');ta.remove();
    }
    toast('Handoff clínico copiado.');
  }catch(err){toast(err.message,true)}
}

async function hpPrintClinicalSummary(){
  if(!state.patientId)return;
  try{
    const r=await api(`/api/pacientes/${state.patientId}/resumo-clinico`);
    const win=window.open('','_blank','noopener,noreferrer');
    if(!win){toast('O navegador bloqueou a janela de impressão.',true);return}
    win.document.open();win.document.write(hpClinicalSummaryPrintHtml(r));win.document.close();
  }catch(err){toast(err.message,true)}
}

// ===== v0.3.27 — Resumo clínico consolidado =====
document.addEventListener('click',async e=>{
  if(e.target?.id==='clinicalSummaryCopy'){await hpCopyClinicalSummary();return}
  if(e.target?.id==='clinicalSummaryPrint'){await hpPrintClinicalSummary();return}
  if(e.target?.id!=='clinicalSummaryRefresh')return;
  const btn=e.target;
  btn.disabled=true;
  try{
    await loadPatient();
    toast('Resumo clínico atualizado.');
  }catch(err){
    toast(err.message,true);
  }
});


// ===== v0.3.27 — Equipe e gestão de profissionais =====
(function(){
  const tiposEquipe=['Admin','Medico','Nutricionista','Personal','Secretaria'];
  const tipoLabel=t=>({Admin:'Administrador',Medico:'Médico',Nutricionista:'Nutricionista',Personal:'Personal',Secretaria:'Secretaria'}[t]||t);
  const tipoProfissional=t=>['Medico','Nutricionista','Personal'].includes(t);

  function equipeModal(title,html,onSave){
    let root=$('#teamModalRoot');
    if(!root){
      root=document.createElement('div');
      root.id='teamModalRoot';
      document.body.appendChild(root);
    }
    root.innerHTML=`<div class="modal-backdrop"><div class="modal-card modal-lg">
      <div class="modal-header"><h3>${esc(title)}</h3><button class="icon-btn" data-team-close>×</button></div>
      <form id="teamForm"><div class="modal-body">${html}</div><div class="modal-footer">
        <button type="button" class="btn" data-team-close>Cancelar</button>
        <button type="submit" class="btn btn-primary">Salvar</button>
      </div></form></div></div>`;
    root.querySelectorAll('[data-team-close]').forEach(x=>x.onclick=()=>root.innerHTML='');
    $('#teamForm').onsubmit=async e=>{
      e.preventDefault();
      const btn=e.currentTarget.querySelector('button[type=submit]');
      btn.disabled=true;
      try{
        await onSave(new FormData(e.currentTarget));
        root.innerHTML='';
        toast('Equipe atualizada.');
        await renderEquipe();
      }catch(err){
        toast(err.message,true);
      }finally{
        btn.disabled=false;
      }
    };
  }

  function teamTypeOptions(value){
    return tiposEquipe.map(x=>`<option value="${x}" ${x===value?'selected':''}>${tipoLabel(x)}</option>`).join('');
  }

  function updateProfessionalFields(form){
    const type=form.querySelector('[name=tipoUsuario]')?.value;
    const wrap=form.querySelector('[data-professional-fields]');
    if(wrap)wrap.classList.toggle('hidden',!tipoProfissional(type));
    const reg=form.querySelector('[name=registroProfissional]');
    if(reg)reg.required=tipoProfissional(type);
  }

  function openCreateTeamMember(){
    equipeModal('Adicionar membro da equipe',`
      <div class="form-grid two">
        <label>Nome<input name="nome" required></label>
        <label>E-mail<input name="email" type="email" required></label>
        <label>Tipo de acesso<select name="tipoUsuario">${teamTypeOptions('Medico')}</select></label>
        <label>Senha temporária<input name="senhaTemporaria" type="password" minlength="10" required autocomplete="new-password"></label>
      </div>
      <div class="form-grid two" data-professional-fields>
        <label>Registro profissional<input name="registroProfissional" required placeholder="CRM, CRN, CREF..."></label>
        <label>Especialidade<input name="especialidade"></label>
      </div>
      <p class="form-hint">A senha temporária deve atender à política atual do sistema. O membro poderá entrar assim que o acesso for criado.</p>
    `,async f=>{
      await api('/api/equipe',{method:'POST',body:JSON.stringify({
        nome:f.get('nome'),
        email:f.get('email'),
        tipoUsuario:f.get('tipoUsuario'),
        senhaTemporaria:f.get('senhaTemporaria'),
        registroProfissional:f.get('registroProfissional')||null,
        especialidade:f.get('especialidade')||null
      })});
    });
    const form=$('#teamForm');
    form.querySelector('[name=tipoUsuario]').onchange=()=>updateProfessionalFields(form);
    updateProfessionalFields(form);
  }

  function openEditTeamMember(m){
    const disabledSelf=m.ehUsuarioAtual?'disabled':'';
    equipeModal('Editar membro da equipe',`
      <div class="form-grid two">
        <label>Nome<input name="nome" required value="${esc(m.nome)}"></label>
        <label>E-mail<input value="${esc(m.email)}" disabled></label>
        <label>Tipo de acesso<select name="tipoUsuario" ${disabledSelf}>${teamTypeOptions(m.tipoUsuario)}</select></label>
        <label class="team-active-check"><input name="ativo" type="checkbox" ${m.ativo?'checked':''} ${disabledSelf}> Acesso ativo</label>
      </div>
      <div class="form-grid two ${tipoProfissional(m.tipoUsuario)?'':'hidden'}" data-professional-fields>
        <label>Registro profissional<input name="registroProfissional" value="${esc(m.registroProfissional||'')}"></label>
        <label>Especialidade<input name="especialidade" value="${esc(m.especialidade||'')}"></label>
      </div>
      ${m.ehUsuarioAtual?'<p class="form-hint">Seu próprio acesso administrativo não pode ser removido por esta tela.</p>':''}
    `,async f=>{
      await api(`/api/equipe/${m.usuarioId}`,{method:'PUT',body:JSON.stringify({
        nome:f.get('nome'),
        tipoUsuario:m.ehUsuarioAtual?m.tipoUsuario:f.get('tipoUsuario'),
        ativo:m.ehUsuarioAtual?true:f.get('ativo')==='on',
        registroProfissional:f.get('registroProfissional')||null,
        especialidade:f.get('especialidade')||null
      })});
    });
    const form=$('#teamForm');
    const select=form.querySelector('[name=tipoUsuario]');
    if(select&&!m.ehUsuarioAtual)select.onchange=()=>updateProfessionalFields(form);
    updateProfessionalFields(form);
  }

  function openResetTeamPassword(m){
    if(m.ehUsuarioAtual||!m.ativo)return;
    equipeModal('Redefinir senha temporária',`
      <p>Defina uma nova senha temporária para <strong>${esc(m.nome)}</strong>.</p>
      <div class="form-grid">
        <label>Nova senha temporária<input name="novaSenhaTemporaria" type="password" minlength="10" required autocomplete="new-password"></label>
      </div>
      <p class="form-hint">A senha não é armazenada no AuditLog. A auditoria registra apenas que houve redefinição.</p>
    `,async f=>{
      await api(`/api/equipe/${m.usuarioId}/redefinir-senha`,{
        method:'POST',
        body:JSON.stringify({novaSenhaTemporaria:f.get('novaSenhaTemporaria')})
      });
    });
  }

  async function renderEquipe(){
    if(state.user?.tipoUsuario!=='Admin'){
      content.innerHTML='<div class="card empty">A gestão da equipe é exclusiva para administradores.</div>';
      return;
    }
    $('#pageEyebrow').textContent='ADMINISTRAÇÃO';
    $('#pageTitle').textContent='Equipe';
    setLoading();

    const params=new URLSearchParams({incluirInativos:'true'});
    if(state.teamSearch)params.set('busca',state.teamSearch);
    if(state.teamType)params.set('tipo',state.teamType);
    if(state.teamStatus)params.set('status',state.teamStatus);
    const membros=await api(`/api/equipe?${params.toString()}`);
    const ativos=membros.filter(x=>x.ativo).length;
    const profissionais=membros.filter(x=>tipoProfissional(x.tipoUsuario)).length;

    content.innerHTML=`<div class="section-head team-head">
      <div><h3>Equipe do consultório</h3><p>Gerencie acessos e perfis profissionais da organização.</p></div>
      <button class="primary" id="teamAdd">+ Adicionar membro</button>
    </div>
    <div class="stats-grid team-stats">
      ${stat('Membros',membros.length,'acessos cadastrados')}
      ${stat('Ativos',ativos,'podem autenticar')}
      ${stat('Profissionais',profissionais,'médico, nutri ou personal')}
      ${stat('Inativos',membros.length-ativos,'acessos bloqueados')}
    </div>
    <div class="team-filterbar">
      <input id="teamSearch" class="search-input" placeholder="Buscar nome ou e-mail">
      <select id="teamTypeFilter"><option value="">Todos os perfis</option>${tiposEquipe.map(x=>`<option value="${x}">${tipoLabel(x)}</option>`).join('')}</select>
      <select id="teamStatusFilter"><option value="">Todos os status</option><option value="ativo">Ativos</option><option value="inativo">Inativos</option></select>
    </div>
    <section class="card full-card">
      <div class="team-list">${membros.length?membros.map(m=>`<article class="team-row ${m.ativo?'':'is-inactive'}" data-team-id="${m.usuarioId}">
        <div class="mini-avatar">${initials(m.nome)}</div>
        <div class="team-main"><strong>${esc(m.nome)}${m.ehUsuarioAtual?' <em>Você</em>':''}</strong><small>${esc(m.email)}</small></div>
        <div><span class="pill ${m.ativo?'Ativa':'Cancelada'}">${m.ativo?'Ativo':'Inativo'}</span></div>
        <div class="team-role"><b>${esc(tipoLabel(m.tipoUsuario))}</b><small>${esc(m.registroProfissional||m.especialidade||'Sem registro profissional')}</small></div>
        <div class="team-row-actions"><button class="secondary team-edit">Editar</button><button class="ghost team-reset-password" ${m.ehUsuarioAtual||!m.ativo?'disabled':''}>Senha</button></div>
      </article>`).join(''):sectionEmpty('Nenhum membro cadastrado.')}</div>
    </section>`;

    $('#teamAdd').onclick=openCreateTeamMember;

    $('#teamSearch').value=state.teamSearch||'';
    $('#teamTypeFilter').value=state.teamType||'';
    $('#teamStatusFilter').value=state.teamStatus||'';

    let teamSearchTimer;
    $('#teamSearch').oninput=e=>{
      clearTimeout(teamSearchTimer);
      teamSearchTimer=setTimeout(()=>{
        state.teamSearch=e.target.value.trim();
        renderEquipe().catch(err=>toast(err.message,true));
      },250);
    };
    $('#teamTypeFilter').onchange=e=>{
      state.teamType=e.target.value;
      renderEquipe().catch(err=>toast(err.message,true));
    };
    $('#teamStatusFilter').onchange=e=>{
      state.teamStatus=e.target.value;
      renderEquipe().catch(err=>toast(err.message,true));
    };

    $$('.team-row').forEach(row=>{
      const m=membros.find(x=>x.usuarioId===row.dataset.teamId);
      row.querySelector('.team-edit').onclick=()=>openEditTeamMember(m);
      const reset=row.querySelector('.team-reset-password');
      if(reset)reset.onclick=()=>openResetTeamPassword(m);
    });
  }

  document.addEventListener('click',e=>{
    const btn=e.target.closest('[data-route="equipe"]');
    if(!btn)return;
    e.preventDefault();
    $$('.nav-item').forEach(x=>x.classList.remove('active'));
    btn.classList.add('active');
    $('.sidebar').classList.remove('open');
    renderEquipe().catch(err=>{
      content.innerHTML=`<div class="card empty">${esc(err.message)}</div>`;
      toast(err.message,true);
    });
  });

  window.HealthPlatform=window.HealthPlatform||{};
  window.HealthPlatform.renderEquipe=renderEquipe;
})();


// ===== v0.3.27 — Minha Conta + troca de senha =====
(function(){
  function accountModal(title,html,onSave){
    let root=document.querySelector('#accountModalRoot');
    if(!root){
      root=document.createElement('div');
      root.id='accountModalRoot';
      document.body.appendChild(root);
    }
    root.innerHTML=`<div class="modal-backdrop"><div class="modal-card modal-lg">
      <div class="modal-header"><h3>${esc(title)}</h3><button class="icon-btn" data-account-close>×</button></div>
      <form id="accountForm"><div class="modal-body">${html}</div><div class="modal-footer">
        <button type="button" class="btn" data-account-close>Cancelar</button>
        <button type="submit" class="btn btn-primary">Salvar</button>
      </div></form></div></div>`;
    root.querySelectorAll('[data-account-close]').forEach(x=>x.onclick=()=>root.innerHTML='');
    root.querySelector('#accountForm').onsubmit=async e=>{
      e.preventDefault();
      const btn=e.currentTarget.querySelector('button[type=submit]');
      btn.disabled=true;
      try{
        await onSave(new FormData(e.currentTarget));
        root.innerHTML='';
        toast('Conta atualizada.');
        window.HealthPlatform?.renderConfiguracoes?.();
      }catch(err){
        toast(err.message,true);
      }finally{
        btn.disabled=false;
      }
    };
  }

  async function ensureAccountPanel(){
    const cfg=document.querySelector('#cfg-resumo');
    if(!cfg||document.querySelector('#hp-account-panel'))return;

    let account;
    try{account=await api('/api/configuracoes/minha-conta')}catch{return}

    const panel=document.createElement('section');
    panel.id='hp-account-panel';
    panel.className='account-panel';
    panel.innerHTML=`<div class="account-panel-head">
      <div><span class="eyebrow">SEGURANÇA</span><h3>Minha conta</h3><small>Dados pessoais e credenciais de acesso.</small></div>
      <div class="account-panel-actions"><button class="secondary" id="accountEditName">Editar nome</button><button class="primary" id="accountChangePassword">Alterar senha</button></div>
    </div>
    <div class="account-info-grid">
      <div><small>Nome</small><strong>${esc(account.nome||'—')}</strong></div>
      <div><small>E-mail</small><strong>${esc(account.email||'—')}</strong></div>
      <div><small>Perfil</small><strong>${esc(account.tipoUsuario||'—')}</strong></div>
      <div><small>Conta criada</small><strong>${account.createdAtUtc?fmtDate(account.createdAtUtc):'—'}</strong></div>
    </div>`;

    cfg.parentElement?.insertBefore(panel,cfg.nextSibling);

    panel.querySelector('#accountEditName').onclick=()=>accountModal('Editar minha conta',`
      <div class="form-grid">
        <label>Nome<input name="nome" required value="${esc(account.nome||'')}"></label>
        <label>E-mail<input value="${esc(account.email||'')}" disabled></label>
      </div>
      <p class="form-hint">O e-mail de login não é alterado nesta tela.</p>
    `,async f=>{
      const updated=await api('/api/configuracoes/minha-conta',{
        method:'PUT',
        body:JSON.stringify({nome:f.get('nome')})
      });
      state.user.nome=updated.nome;
      localStorage.setItem('hp_user',JSON.stringify(state.user));
      $('#userName').textContent=updated.nome;
      $('#avatar').textContent=initials(updated.nome);
    });

    panel.querySelector('#accountChangePassword').onclick=()=>accountModal('Alterar minha senha',`
      <div class="form-grid">
        <label>Senha atual<input name="senhaAtual" type="password" required autocomplete="current-password"></label>
        <label>Nova senha<input name="novaSenha" type="password" minlength="10" required autocomplete="new-password"></label>
        <label>Confirmar nova senha<input name="confirmacaoNovaSenha" type="password" minlength="10" required autocomplete="new-password"></label>
      </div>
      <p class="form-hint">A senha deve respeitar a política do sistema. Nenhuma senha é gravada no AuditLog.</p>
    `,async f=>{
      await api('/api/configuracoes/minha-conta/alterar-senha',{
        method:'POST',
        body:JSON.stringify({
          senhaAtual:f.get('senhaAtual'),
          novaSenha:f.get('novaSenha'),
          confirmacaoNovaSenha:f.get('confirmacaoNovaSenha')
        })
      });
      toast('Senha alterada com sucesso.');
    });
  }

  const observer=new MutationObserver(()=>{
    if(document.querySelector('#cfg-resumo'))ensureAccountPanel();
  });
  observer.observe(document.body,{childList:true,subtree:true});

  document.addEventListener('click',e=>{
    if(e.target.closest('[data-route="configuracoes"]')){
      setTimeout(()=>ensureAccountPanel(),80);
    }
  });

  window.HealthPlatform=window.HealthPlatform||{};
  window.HealthPlatform.ensureAccountPanel=ensureAccountPanel;
})();


// ===== v0.3.28 — Evolução de hábitos + gráficos de anamnese =====
function hpHabitDelta(value,suffix=''){
  if(value===null||value===undefined)return 'Sem comparação';
  const n=Number(value);
  if(!Number.isFinite(n))return 'Sem comparação';
  if(n===0)return 'Sem mudança';
  return `${n>0?'+':''}${num(n,2)}${suffix} vs anterior`;
}
function hpHabitCurrentCard(label,value,suffix,delta,detail=''){
  const has=value!==null&&value!==undefined&&value!=='';
  return `<article class="habit-current-card">
    <span>${esc(label)}</span>
    <strong>${has?`${num(value,2)}${esc(suffix)}`:'—'}</strong>
    <small>${esc(hpHabitDelta(delta,suffix))}</small>
    ${detail?`<em>${esc(detail)}</em>`:''}
  </article>`;
}
function hpHabitCharts(d){
  const items=(d?.itens||[]).slice().sort((a,b)=>new Date(a.dataUtc)-new Date(b.dataUtc));
  return hpMetricChartGrid([
    hpLineChart('Sono médio',items.map(x=>({date:x.dataUtc,value:x.sonoHorasMedia,label:`Sono • ${hpChartDate(x.dataUtc)}`})),' h'),
    hpLineChart('Estresse',items.map(x=>({date:x.dataUtc,value:x.estresseNivel,label:`Estresse • ${hpChartDate(x.dataUtc)}`})),'/10'),
    hpLineChart('Atividade física',items.map(x=>({date:x.dataUtc,value:x.atividadeFisicaDiasSemana,label:`Atividade • ${hpChartDate(x.dataUtc)}`})),' d/sem'),
    hpLineChart('Consumo de água',items.map(x=>({date:x.dataUtc,value:x.aguaLitrosDia,label:`Água • ${hpChartDate(x.dataUtc)}`})),' L')
  ]);
}
function hpHabitEvolutionBody(d){
  const a=d?.atual||{},delta=d?.variacaoDesdeAnterior||{};
  const current=`<div class="habit-current-grid">
    ${hpHabitCurrentCard('Sono',a.sonoHorasMedia,' h',delta.sonoHorasMedia,a.sonoQualidade?`Qualidade: ${a.sonoQualidade}`:'')}
    ${hpHabitCurrentCard('Estresse',a.estresseNivel,'/10',delta.estresseNivel,'Escala informada na anamnese')}
    ${hpHabitCurrentCard('Atividade',a.atividadeFisicaDiasSemana,' d/sem',delta.atividadeFisicaDiasSemana,a.atividadeFisica||'')}
    ${hpHabitCurrentCard('Água',a.aguaLitrosDia,' L',delta.aguaLitrosDia,a.despertaDuranteNoite===true?'Sono com despertares':a.despertaDuranteNoite===false?'Sem despertares informados':'')}
  </div>`;
  return `${current}<div class="habit-chart-head"><strong>Tendência longitudinal</strong><small>${d?.total||0} anamnese(s) no período carregado</small></div>${hpHabitCharts(d)}`;
}
async function hpInjectHabitEvolution(host,patientId,id='professional-habits'){
  if(!host||!patientId||host.querySelector(`[data-habit-evolution="${id}"]`))return;
  try{
    const d=await api(`/api/pacientes/${patientId}/evolucao-habitos?limite=24`);
    if(!host.isConnected||host.querySelector(`[data-habit-evolution="${id}"]`))return;
    const section=document.createElement('section');
    section.className='card full-card analytics-section habit-evolution-section';
    section.dataset.habitEvolution=id;
    section.innerHTML=`<div class="card-head"><div><h3>Evolução de hábitos</h3><small>Sono, estresse, atividade física e hidratação ao longo das anamneses</small></div><span class="analytics-badge">Anamnese</span></div>${hpHabitEvolutionBody(d)}`;
    host.appendChild(section);
  }catch(err){
    console.warn('Evolução de hábitos indisponível:',err);
  }
}

const __renderPatientTab_v0328=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v0328(d);
  const host=$('#patientTabContent');
  if(!host||!state.patientId)return;
  const tab=state.patientTab;
  if(tab==='anamnese'){
    hpInjectHabitEvolution(host,state.patientId,'professional-anamnesis');
  }
  if(tab==='resumo'){
    hpInjectHabitEvolution(host,state.patientId,'professional-summary-habits');
  }
};


// ===== v0.3.32 — Check-ins de evolução + adesão por fase =====
function hpCheckInMetric(label,value,suffix=''){
  return `<article class="checkin-current-card"><span>${esc(label)}</span><strong>${value==null?'—':`${num(value,1)}${esc(suffix)}`}</strong></article>`;
}

function hpCheckInCharts(items){
  const rows=(items||[]).slice().sort((a,b)=>new Date(a.dataUtc)-new Date(b.dataUtc));
  return hpMetricChartGrid([
    hpLineChart('Peso',rows.map(x=>({date:x.dataUtc,value:x.pesoKg,label:`Peso • ${hpChartDate(x.dataUtc)}`})),' kg'),
    hpLineChart('Adesão alimentar',rows.map(x=>({date:x.dataUtc,value:x.adesaoAlimentacaoPercentual,label:`Dieta • ${hpChartDate(x.dataUtc)}`})),'%'),
    hpLineChart('Adesão ao treino',rows.map(x=>({date:x.dataUtc,value:x.adesaoTreinoPercentual,label:`Treino • ${hpChartDate(x.dataUtc)}`})),'%'),
    hpLineChart('Energia',rows.map(x=>({date:x.dataUtc,value:x.energiaNivel,label:`Energia • ${hpChartDate(x.dataUtc)}`})),'/10')
  ]);
}

function hpCheckInHistoryRows(items,professional=false){
  if(!(items||[]).length)return `<div class="empty">Nenhum check-in registrado ainda.</div>`;
  return `<div class="checkin-history-list">${items.slice().reverse().map(x=>`<article class="checkin-history-row">
    <div><strong>${fmtDateTime(x.dataUtc)}</strong><small>${esc(x.origem||'')}</small></div>
    <span>${x.pesoKg!=null?`${num(x.pesoKg,1)} kg`:'Peso —'}</span>
    <span>Dieta ${x.adesaoAlimentacaoPercentual!=null?x.adesaoAlimentacaoPercentual+'%':'—'}</span>
    <span>Treino ${x.adesaoTreinoPercentual!=null?x.adesaoTreinoPercentual+'%':'—'}</span>
    <span>Energia ${x.energiaNivel!=null?x.energiaNivel+'/10':'—'}</span>
    <div class="checkin-phase-tags">${x.faseNutricionalNome?`<em>${esc(x.faseNutricionalNome)}</em>`:''}${x.faseTreinoNome?`<em>${esc(x.faseTreinoNome)}</em>`:''}</div>
    ${professional?`<button class="ghost professional-checkin-edit" data-checkin-id="${x.id}">Editar</button>`:''}
  </article>`).join('')}</div>`;
}

async function hpInjectProfessionalCheckIns(host,patientId,id='checkins'){
  if(!host||!patientId||host.querySelector(`[data-checkins="${id}"]`))return;
  try{
    const d=await api(`/api/pacientes/${patientId}/check-ins?limite=30`);
    if(!host.isConnected||host.querySelector(`[data-checkins="${id}"]`))return;

    const current=d.atual||{};
    const section=document.createElement('section');
    section.className='card full-card checkin-section';
    section.dataset.checkins=id;
    section.innerHTML=`<div class="card-head"><div><h3>Check-ins de evolução</h3><small>${d.total||0} registro(s) • adesão e resposta ao ciclo</small></div><button class="primary professional-checkin-new">+ Registrar check-in</button></div>
      <div class="checkin-current-grid">
        ${hpCheckInMetric('Peso',current.pesoKg,' kg')}
        ${hpCheckInMetric('Adesão dieta',current.adesaoAlimentacaoPercentual,'%')}
        ${hpCheckInMetric('Adesão treino',current.adesaoTreinoPercentual,'%')}
        ${hpCheckInMetric('Energia',current.energiaNivel,'/10')}
      </div>
      ${hpCheckInCharts(d.itens||[])}
      ${hpCheckInHistoryRows(d.itens||[],true)}`;

    host.appendChild(section);
    section.querySelector('.professional-checkin-new').onclick=()=>openProfessionalCheckInForm(patientId,null);
    section.querySelectorAll('.professional-checkin-edit').forEach(b=>{
      b.onclick=()=>openProfessionalCheckInForm(patientId,(d.itens||[]).find(x=>x.id===b.dataset.checkinId));
    });
  }catch(err){
    console.warn('Check-ins indisponíveis:',err);
  }
}

async function openProfessionalCheckInForm(patientId,item){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  const [nutritionPhases,workoutPhases]=await Promise.all([
    api(`/api/pacientes/${patientId}/fases-nutricionais`).catch(()=>[]),
    api(`/api/pacientes/${patientId}/fases-treino`).catch(()=>[])
  ]);

  const editing=!!item,v=x=>x==null?'':String(x);
  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">CHECK-IN</span><h2>${editing?'Editar acompanhamento':'Registrar acompanhamento'}</h2><p>Resposta do paciente às fases atuais.</p></div>
  <form id="professionalCheckInForm" class="clinical-form">
    <div class="form-grid">
      ${field('Data e hora','dataUtc','datetime-local',`value="${item?.dataUtc?String(item.dataUtc).slice(0,16):new Date().toISOString().slice(0,16)}" required`)}
      ${field('Peso (kg)','pesoKg','number',`step="0.1" min="20" max="400" value="${v(item?.pesoKg)}"`)}
      ${field('Adesão à dieta (%)','adesaoAlimentacaoPercentual','number',`min="0" max="100" value="${v(item?.adesaoAlimentacaoPercentual)}"`)}
      ${field('Adesão ao treino (%)','adesaoTreinoPercentual','number',`min="0" max="100" value="${v(item?.adesaoTreinoPercentual)}"`)}
      ${field('Fome (0–10)','fomeNivel','number',`min="0" max="10" value="${v(item?.fomeNivel)}"`)}
      ${field('Energia (0–10)','energiaNivel','number',`min="0" max="10" value="${v(item?.energiaNivel)}"`)}
      ${field('Sono (0–10)','sonoNivel','number',`min="0" max="10" value="${v(item?.sonoNivel)}"`)}
      ${field('Percepção de evolução (0–10)','percepcaoEvolucaoNivel','number',`min="0" max="10" value="${v(item?.percepcaoEvolucaoNivel)}"`)}
      <label>Fase nutricional<select name="faseNutricionalId"><option value="">Sem vínculo</option>${nutritionPhases.map(x=>`<option value="${x.id}">${esc(x.nome)}</option>`).join('')}</select></label>
      <label>Fase de treino<select name="faseTreinoId"><option value="">Sem vínculo</option>${workoutPhases.map(x=>`<option value="${x.id}">${esc(x.nome)}</option>`).join('')}</select></label>
      ${area('Observações','observacoes','placeholder="Fome, disposição, dificuldades, resposta ao plano..."')}
    </div>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">${editing?'Salvar alterações':'Registrar check-in'}</button></div>
  </form>`;

  const f=$('#professionalCheckInForm');
  f.faseNutricionalId.value=item?.faseNutricionalId||'';
  f.faseTreinoId.value=item?.faseTreinoId||'';
  f.observacoes.value=item?.observacoes||'';
  $('[data-close-clinical-form]').onclick=closeClinicalAction;

  f.onsubmit=async e=>{
    e.preventDefault();const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      const body={
        dataUtc:new Date(val(f,'dataUtc')).toISOString(),
        pesoKg:dec(f,'pesoKg'),
        adesaoAlimentacaoPercentual:val(f,'adesaoAlimentacaoPercentual')===''?null:Number(val(f,'adesaoAlimentacaoPercentual')),
        adesaoTreinoPercentual:val(f,'adesaoTreinoPercentual')===''?null:Number(val(f,'adesaoTreinoPercentual')),
        fomeNivel:val(f,'fomeNivel')===''?null:Number(val(f,'fomeNivel')),
        energiaNivel:val(f,'energiaNivel')===''?null:Number(val(f,'energiaNivel')),
        sonoNivel:val(f,'sonoNivel')===''?null:Number(val(f,'sonoNivel')),
        percepcaoEvolucaoNivel:val(f,'percepcaoEvolucaoNivel')===''?null:Number(val(f,'percepcaoEvolucaoNivel')),
        faseNutricionalId:val(f,'faseNutricionalId')||null,
        faseTreinoId:val(f,'faseTreinoId')||null,
        observacoes:val(f,'observacoes')||null
      };

      await api(editing?`/api/check-ins/${item.id}`:`/api/pacientes/${patientId}/check-ins`,{
        method:editing?'PUT':'POST',body:JSON.stringify(body)
      });
      closeClinicalAction();toast(editing?'Check-in atualizado.':'Check-in registrado.');await loadPatient();
    }catch(err){toast(err.message,true)}
    finally{btn.disabled=false}
  };
}

async function loadMyCheckInsIntoEvolution(){
  const host=$('#patientPortalContent');
  if(!host||host.querySelector('[data-my-checkins]'))return;
  try{
    const d=await api('/api/portal/me/check-ins?limite=30');
    if(!host.isConnected)return;

    const section=document.createElement('section');
    section.className='card checkin-section';
    section.dataset.myCheckins='1';
    section.innerHTML=`<div class="card-head"><div><h3>Meus check-ins</h3><small>${d.total||0} registro(s) • acompanhe como você está respondendo</small></div><button class="primary" id="patientCheckInNew">+ Novo check-in</button></div>
      ${hpCheckInCharts(d.itens||[])}
      ${hpCheckInHistoryRows(d.itens||[],false)}`;
    host.appendChild(section);
    $('#patientCheckInNew').onclick=openMyCheckInForm;
  }catch(err){console.warn('Meus check-ins indisponíveis:',err)}
}

function openMyCheckInForm(){
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">MEU CHECK-IN</span><h2>Como você está respondendo?</h2><p>Essas informações ajudam seu profissional a acompanhar a fase atual.</p></div>
  <form id="patientCheckInForm" class="clinical-form">
    <div class="form-grid">
      ${field('Peso atual (kg)','pesoKg','number','step="0.1" min="20" max="400"')}
      ${field('Adesão à dieta (%)','adesaoAlimentacaoPercentual','number','min="0" max="100"')}
      ${field('Adesão ao treino (%)','adesaoTreinoPercentual','number','min="0" max="100"')}
      ${field('Fome (0–10)','fomeNivel','number','min="0" max="10"')}
      ${field('Energia (0–10)','energiaNivel','number','min="0" max="10"')}
      ${field('Sono (0–10)','sonoNivel','number','min="0" max="10"')}
      ${field('Percepção de evolução (0–10)','percepcaoEvolucaoNivel','number','min="0" max="10"')}
      ${area('Observações','observacoes','placeholder="Conte como foi sua semana, dificuldades, fome, disposição..."')}
    </div>
    <p class="form-hint">O sistema vincula automaticamente este check-in às fases atuais, quando houver.</p>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Enviar check-in</button></div>
  </form>`;

  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#patientCheckInForm');
  f.onsubmit=async e=>{
    e.preventDefault();const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      await api('/api/portal/me/check-ins',{method:'POST',body:JSON.stringify({
        pesoKg:dec(f,'pesoKg'),
        adesaoAlimentacaoPercentual:val(f,'adesaoAlimentacaoPercentual')===''?null:Number(val(f,'adesaoAlimentacaoPercentual')),
        adesaoTreinoPercentual:val(f,'adesaoTreinoPercentual')===''?null:Number(val(f,'adesaoTreinoPercentual')),
        fomeNivel:val(f,'fomeNivel')===''?null:Number(val(f,'fomeNivel')),
        energiaNivel:val(f,'energiaNivel')===''?null:Number(val(f,'energiaNivel')),
        sonoNivel:val(f,'sonoNivel')===''?null:Number(val(f,'sonoNivel')),
        percepcaoEvolucaoNivel:val(f,'percepcaoEvolucaoNivel')===''?null:Number(val(f,'percepcaoEvolucaoNivel')),
        observacoes:val(f,'observacoes')||null
      })});
      closeClinicalAction();toast('Check-in enviado.');await loadPatientSection('evolucao');
    }catch(err){toast(err.message,true)}
    finally{btn.disabled=false}
  };
}

// Prontuário: check-ins no resumo, alimentação e treinos.
const __renderPatientTab_v032_checkins=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v032_checkins(d);
  const host=$('#patientTabContent');
  if(!host||!state.patientId)return;
  if(['resumo','alimentacao','treinos'].includes(state.patientTab)){
    hpInjectProfessionalCheckIns(host,state.patientId,`professional-${state.patientTab}-checkins`);
  }
};

// Portal: acrescenta check-ins à tela de evolução sem remover os gráficos corporais.
const __loadPatientEvolution_v032_checkins=loadPatientEvolution;
loadPatientEvolution=async function(){
  await __loadPatientEvolution_v032_checkins();
  await loadMyCheckInsIntoEvolution();
};


// ===== v0.3.33 — Análise comparativa de fases =====
function hpPhaseMetric(label,value,suffix=''){
  return `<div class="phase-analysis-metric"><span>${esc(label)}</span><strong>${value==null?'—':`${num(value,1)}${esc(suffix)}`}</strong></div>`;
}

function hpPhaseAnalysisCard(f){
  const weight=f.variacaoPesoKg;
  const weightText=weight==null?'—':`${weight>0?'+':''}${num(weight,1)} kg`;
  return `<article class="phase-analysis-card">
    <div class="phase-analysis-head">
      <div><span class="eyebrow">${esc(f.dominio)} • ${esc(f.tipo)}</span><h4>${esc(f.nome)}</h4></div>
      <span>${f.checkIns} check-in(s)</span>
    </div>
    <div class="phase-analysis-grid">
      ${hpPhaseMetric('Peso Δ',weight,' kg')}
      ${hpPhaseMetric('Adesão dieta',f.mediaAdesaoAlimentacao,'%')}
      ${hpPhaseMetric('Adesão treino',f.mediaAdesaoTreino,'%')}
      ${hpPhaseMetric('Energia',f.mediaEnergia,'/10')}
      ${hpPhaseMetric('Fome',f.mediaFome,'/10')}
      ${hpPhaseMetric('Sono',f.mediaSono,'/10')}
    </div>
    <small>${f.pesoInicialKg!=null&&f.pesoFinalKg!=null?`Peso ${num(f.pesoInicialKg,1)} → ${num(f.pesoFinalKg,1)} kg`:'Sem série de peso suficiente'}${f.mediaPercepcaoEvolucao!=null?` • percepção ${num(f.mediaPercepcaoEvolucao,1)}/10`:''}</small>
  </article>`;
}

function hpPhaseHighlightCard(title,item,suffix=''){
  if(!item)return `<article class="phase-highlight-card muted"><span>${esc(title)}</span><strong>Sem dados</strong><small>Registre check-ins vinculados às fases.</small></article>`;
  const value=item.valor==null?'—':`${num(item.valor,1)}${esc(suffix)}`;
  return `<article class="phase-highlight-card"><span>${esc(title)}</span><strong>${esc(item.nome)}</strong><small>${esc(item.dominio)} • ${value} • ${item.checkIns} check-in(s)</small></article>`;
}

async function hpInjectPhaseAnalysis(host,patientId,id='phase-analysis',mode='all'){
  if(!host||!patientId||host.querySelector(`[data-phase-analysis="${id}"]`))return;
  try{
    const d=await api(`/api/pacientes/${patientId}/analise-fases`);
    if(!host.isConnected||host.querySelector(`[data-phase-analysis="${id}"]`))return;

    let phases=[];
    if(mode==='nutrition') phases=d.nutricao||[];
    else if(mode==='workout') phases=d.treino||[];
    else phases=[...(d.nutricao||[]),...(d.treino||[])];

    const section=document.createElement('section');
    section.className='card full-card phase-analysis-section';
    section.dataset.phaseAnalysis=id;

    const highlights=mode==='workout'
      ? [
          hpPhaseHighlightCard('Melhor adesão ao treino',d.destaques?.melhorAdesaoTreino,'%'),
          hpPhaseHighlightCard('Maior energia média',d.destaques?.maiorEnergiaMedia,'/10')
        ]
      : mode==='nutrition'
      ? [
          hpPhaseHighlightCard('Melhor adesão alimentar',d.destaques?.melhorAdesaoAlimentar,'%'),
          hpPhaseHighlightCard('Maior redução de peso',d.destaques?.maiorReducaoPeso,' kg')
        ]
      : [
          hpPhaseHighlightCard('Melhor adesão alimentar',d.destaques?.melhorAdesaoAlimentar,'%'),
          hpPhaseHighlightCard('Melhor adesão ao treino',d.destaques?.melhorAdesaoTreino,'%'),
          hpPhaseHighlightCard('Maior redução de peso',d.destaques?.maiorReducaoPeso,' kg'),
          hpPhaseHighlightCard('Maior energia média',d.destaques?.maiorEnergiaMedia,'/10')
        ];

    section.innerHTML=`<div class="card-head"><div><h3>Análise de resposta por fase</h3><small>${d.totalCheckIns||0} check-in(s) considerados • comparação baseada nos registros vinculados</small></div><span class="analytics-badge">Ciclos</span></div>
      <div class="phase-highlight-grid">${highlights.join('')}</div>
      <div class="phase-analysis-list">${phases.length?phases.map(hpPhaseAnalysisCard).join(''):`<div class="empty">Nenhuma fase disponível para comparação.</div>`}</div>`;

    host.appendChild(section);
  }catch(err){
    console.warn('Análise de fases indisponível:',err);
  }
}

const __renderPatientTab_v033_phaseanalysis=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v033_phaseanalysis(d);
  const host=$('#patientTabContent');
  if(!host||!state.patientId)return;

  if(state.patientTab==='alimentacao'){
    hpInjectPhaseAnalysis(host,state.patientId,'nutrition-phase-analysis','nutrition');
  }else if(state.patientTab==='treinos'){
    hpInjectPhaseAnalysis(host,state.patientId,'workout-phase-analysis','workout');
  }else if(state.patientTab==='resumo'){
    hpInjectPhaseAnalysis(host,state.patientId,'summary-phase-analysis','all');
  }
};


// ===== v0.3.34 — Metas de fase + critérios de transição =====
function hpTransitionCriterion(c){if(!c?.configurado)return '';const state=c.atendido===true?'ok':'pending';const label=c.atendido===true?'Atendido':'Pendente';return `<div class="transition-criterion ${state}"><div><strong>${esc(c.rotulo)}</strong><small>${esc(c.detalhe||'')}</small></div><span>${label}</span></div>`;}
function hpTransitionStatusCard(f){const objective=f.criteriosObjetivosConfigurados||0,done=f.criteriosObjetivosAtendidos||0;const badge=f.objetivosProntosParaRevisao?'Pronta para revisão':objective?'Em progresso':'Sem critérios objetivos';return `<article class="transition-status-card"><div class="transition-status-head"><div><span class="eyebrow">${esc(f.tipo)} • ${f.diasDecorridos} dia(s)</span><h4>${esc(f.nome)}</h4></div><span class="pill ${f.objetivosProntosParaRevisao?'Ativa':''}">${esc(badge)}</span></div><div class="transition-progress"><strong>${done}/${objective}</strong><span>critérios objetivos atendidos</span></div><div class="transition-criteria">${(f.criterios||[]).map(hpTransitionCriterion).join('')||'<small>Nenhuma meta objetiva configurada.</small>'}</div>${f.criterioTransicao?`<div class="transition-manual"><b>Critério profissional</b><span>${esc(f.criterioTransicao)}</span></div>`:''}</article>`;}
async function hpInjectTransitionStatus(host,patientId,id='transition-status',mode='all'){if(!host||!patientId||host.querySelector(`[data-transition-status="${id}"]`))return;try{const d=await api(`/api/pacientes/${patientId}/status-transicao-fases`);if(!host.isConnected||host.querySelector(`[data-transition-status="${id}"]`))return;const phases=mode==='nutrition'?(d.nutricao||[]):mode==='workout'?(d.treino||[]):[...(d.nutricao||[]),...(d.treino||[])];const relevant=phases.filter(x=>x.status==='EmAndamento'||x.criteriosObjetivosConfigurados>0||x.requerAvaliacaoProfissional);const section=document.createElement('section');section.className='card full-card transition-status-section';section.dataset.transitionStatus=id;section.innerHTML=`<div class="card-head"><div><h3>Prontidão para transição</h3><small>Metas objetivas ajudam na revisão; a decisão final continua sendo do profissional.</small></div><span class="analytics-badge">Critérios</span></div><div class="transition-status-list">${relevant.length?relevant.map(hpTransitionStatusCard).join(''):`<div class="empty">Nenhuma fase possui critérios de transição configurados.</div>`}</div>`;host.appendChild(section);}catch(err){console.warn('Status de transição indisponível:',err)}}
const __renderPatientTab_v034_transition=renderPatientTab;renderPatientTab=function(d){__renderPatientTab_v034_transition(d);const host=$('#patientTabContent');if(!host||!state.patientId)return;if(state.patientTab==='alimentacao')hpInjectTransitionStatus(host,state.patientId,'nutrition-transition-status','nutrition');else if(state.patientTab==='treinos')hpInjectTransitionStatus(host,state.patientId,'workout-transition-status','workout');else if(state.patientTab==='resumo')hpInjectTransitionStatus(host,state.patientId,'summary-transition-status','all');};


// ===== v0.3.35 — Revisão de fase + transição assistida =====
function hpPhaseReviewHistory(items){
  if(!(items||[]).length)return `<div class="empty">Nenhuma revisão de fase registrada ainda.</div>`;
  return `<div class="phase-review-history">${items.slice(0,6).map(x=>`<article class="phase-review-history-card">
    <div><span class="eyebrow">${esc(x.dominio)} • ${fmtDateTime(x.dataUtc)}</span><strong>${esc(x.faseNome)}</strong></div>
    <span class="phase-review-decision">${esc(x.decisao)}</span>
    <p>${esc(x.justificativa)}</p>
    <small>${x.faseDestinoNome?`Próxima fase: ${esc(x.faseDestinoNome)} • `:''}${x.criteriosConfigurados?`${x.criteriosAtendidos}/${x.criteriosConfigurados} critérios objetivos`: 'Sem critérios objetivos'}${x.overrideCriterios?' • decisão profissional com critérios pendentes':''}${x.revisadoPorNome?` • ${esc(x.revisadoPorNome)}`:''}</small>
  </article>`).join('')}</div>`;
}

function hpTransitionStatusCardReview(f,domain,patientId){
  const objective=f.criteriosObjetivosConfigurados||0,done=f.criteriosObjetivosAtendidos||0;
  const badge=f.objetivosProntosParaRevisao?'Pronta para revisão':objective?'Em progresso':'Sem critérios objetivos';
  const canReview=f.status==='EmAndamento';
  return `<article class="transition-status-card">
    <div class="transition-status-head"><div><span class="eyebrow">${esc(f.tipo)} • ${f.diasDecorridos} dia(s)</span><h4>${esc(f.nome)}</h4></div><span class="pill ${f.objetivosProntosParaRevisao?'Ativa':''}">${esc(badge)}</span></div>
    <div class="transition-progress"><strong>${done}/${objective}</strong><span>critérios objetivos atendidos</span></div>
    <div class="transition-criteria">${(f.criterios||[]).map(hpTransitionCriterion).join('')||'<small>Nenhuma meta objetiva configurada.</small>'}</div>
    ${f.criterioTransicao?`<div class="transition-manual"><b>Critério profissional</b><span>${esc(f.criterioTransicao)}</span></div>`:''}
    ${canReview?`<button class="primary phase-review-action" data-domain="${domain}" data-phase-id="${f.faseId}">Revisar fase</button>`:''}
  </article>`;
}

async function openPhaseReview(patientId,domain,phase){
  if(!phase)return;
  const box=$('#clinicalActionContent');
  $('#clinicalActionModal').classList.add('nutrition-modal-open');
  $('#clinicalActionModal').classList.remove('hidden');

  const configured=phase.criteriosObjetivosConfigurados||0;
  const attended=phase.criteriosObjetivosAtendidos||0;
  const hasPending=configured>0&&!phase.objetivosProntosParaRevisao;

  box.innerHTML=`<div class="modal-heading"><span class="eyebrow">REVISÃO DE FASE • ${esc(domain==='nutrition'?'NUTRIÇÃO':'TREINO')}</span><h2>${esc(phase.nome)}</h2><p>Registre a decisão profissional e mantenha o histórico do ciclo.</p></div>
  <div class="phase-review-summary">
    <strong>${attended}/${configured}</strong><span>critérios objetivos atendidos</span>
    <div>${(phase.criterios||[]).map(hpTransitionCriterion).join('')||'<small>Nenhum critério objetivo configurado.</small>'}</div>
    ${phase.criterioTransicao?`<p><b>Critério profissional:</b> ${esc(phase.criterioTransicao)}</p>`:''}
  </div>
  <form id="phaseReviewForm" class="clinical-form">
    <label>Decisão
      <select name="decisao">
        <option value="Manter">Manter fase em andamento</option>
        <option value="Concluir">Concluir esta fase</option>
        <option value="Avancar">Concluir e avançar para a próxima</option>
      </select>
    </label>
    ${area('Justificativa da decisão','justificativa','placeholder="Registre a leitura profissional dos resultados, adesão e contexto do paciente." required')}
    ${hasPending?`<label class="phase-review-override"><input type="checkbox" name="confirmarMesmoSemCriterios"><span>Confirmo a decisão profissional mesmo com critérios objetivos pendentes.</span></label>`:''}
    <p class="form-hint">O sistema nunca avança sozinho. A decisão, justificativa e eventual override ficam registrados.</p>
    <div class="form-actions"><button type="button" class="secondary" data-close-clinical-form>Cancelar</button><button type="submit" class="primary">Registrar revisão</button></div>
  </form>`;

  $('[data-close-clinical-form]').onclick=closeClinicalAction;
  const f=$('#phaseReviewForm');
  f.onsubmit=async e=>{
    e.preventDefault();
    const btn=e.target.querySelector('button[type=submit]');btn.disabled=true;
    try{
      const path=domain==='nutrition'?`/api/fases-nutricionais/${phase.faseId}/revisar`:`/api/fases-treino/${phase.faseId}/revisar`;
      const response=await api(path,{method:'POST',body:JSON.stringify({
        decisao:val(f,'decisao'),
        justificativa:val(f,'justificativa'),
        confirmarMesmoSemCriterios:!!f.querySelector('[name=confirmarMesmoSemCriterios]')?.checked
      })});
      closeClinicalAction();toast(response.message||'Revisão registrada.');await loadPatient();
    }catch(err){toast(err.message,true)}
    finally{btn.disabled=false}
  };
}

hpInjectTransitionStatus=async function(host,patientId,id='transition-status',mode='all'){
  if(!host||!patientId||host.querySelector(`[data-transition-status="${id}"]`))return;
  try{
    const dominio=mode==='nutrition'?'Nutricao':mode==='workout'?'Treino':null;
    const historyUrl=`/api/pacientes/${patientId}/revisoes-fases?limite=6${dominio?`&dominio=${dominio}`:''}`;
    const [d,history]=await Promise.all([
      api(`/api/pacientes/${patientId}/status-transicao-fases`),
      api(historyUrl)
    ]);
    if(!host.isConnected||host.querySelector(`[data-transition-status="${id}"]`))return;

    const nutrition=(d.nutricao||[]).map(x=>({...x,__domain:'nutrition'}));
    const workout=(d.treino||[]).map(x=>({...x,__domain:'workout'}));
    const phases=mode==='nutrition'?nutrition:mode==='workout'?workout:[...nutrition,...workout];
    const relevant=phases.filter(x=>x.status==='EmAndamento'||x.criteriosObjetivosConfigurados>0||x.requerAvaliacaoProfissional);

    const section=document.createElement('section');
    section.className='card full-card transition-status-section';
    section.dataset.transitionStatus=id;
    section.innerHTML=`<div class="card-head"><div><h3>Prontidão e revisão de fase</h3><small>Critérios apoiam a decisão; nenhuma transição acontece sem ação do profissional.</small></div><span class="analytics-badge">Revisão</span></div>
      <div class="transition-status-list">${relevant.length?relevant.map(x=>hpTransitionStatusCardReview(x,x.__domain,patientId)).join(''):`<div class="empty">Nenhuma fase possui critérios ou está em andamento.</div>`}</div>
      <div class="phase-review-history-head"><strong>Histórico de decisões</strong><small>Registros imutáveis das revisões mais recentes</small></div>
      ${hpPhaseReviewHistory(history.itens||[])}`;

    host.appendChild(section);
    section.querySelectorAll('.phase-review-action').forEach(b=>{
      const phase=phases.find(x=>x.faseId===b.dataset.phaseId&&x.__domain===b.dataset.domain);
      b.onclick=()=>openPhaseReview(patientId,b.dataset.domain,phase);
    });
  }catch(err){console.warn('Revisão de fases indisponível:',err)}
};


// ===== v0.3.36 — Volume de treino + distribuição muscular =====
function hpWorkoutVolumeBar(row,max){
  const width=max>0?Math.max(3,Math.round((row.seriesSemanaisEstimadas||0)*100/max)):0;
  return `<article class="workout-volume-row">
    <div class="workout-volume-label">
      <strong>${esc(row.grupoMuscular)}</strong>
      <small>${row.exerciciosDistintos||0} exercício(s) • ${num(row.percentualDoVolumeSemanal||0,1)}% do volume planejado</small>
    </div>
    <div class="workout-volume-track"><span style="width:${width}%"></span></div>
    <div class="workout-volume-values">
      <b>${num(row.seriesSemanaisEstimadas||0,0)}</b><small>séries/sem. estimadas</small>
      <b>${num(row.mediaSeriesRealizadasSemana||0,1)}</b><small>séries/sem. realizadas</small>
    </div>
  </article>`;
}

function hpWorkoutSessionVolume(session){
  const freq=session.frequenciaSemanal||1;
  return `<article class="workout-session-volume-card">
    <div><span class="eyebrow">${esc(session.diasSemana||'Frequência não informada')}</span><strong>${esc(session.nome)}</strong></div>
    <div class="workout-session-volume-numbers">
      <span><b>${session.seriesPorSessao||0}</b> séries/sessão</span>
      <span><b>${session.seriesSemanaisEstimadas||0}</b> séries/sem.</span>
      <span><b>${freq}x</b> frequência</span>
    </div>
    ${!session.frequenciaInferida?'<small>Frequência não reconhecida; estimativa considera 1x/semana.</small>':''}
  </article>`;
}

async function hpInjectWorkoutVolume(host,patientId,id='workout-volume',days=30){
  if(!host||!patientId||host.querySelector(`[data-workout-volume="${id}"]`))return;
  try{
    const d=await api(`/api/pacientes/${patientId}/treinos/analise-volume?dias=${days}`);
    if(!host.isConnected||host.querySelector(`[data-workout-volume="${id}"]`))return;

    const rows=d.porGrupo||[];
    const max=Math.max(0,...rows.map(x=>x.seriesSemanaisEstimadas||0));
    const r=d.resumo||{};

    const section=document.createElement('section');
    section.className='card full-card workout-volume-section';
    section.dataset.workoutVolume=id;
    section.innerHTML=`<div class="card-head">
      <div><h3>Volume e distribuição muscular</h3><small>${esc(d.plano?.nome||'Plano')} • V${d.plano?.versao||1} • execuções dos últimos ${d.dias} dias</small></div>
      <span class="analytics-badge">Volume</span>
    </div>
    <div class="workout-volume-summary">
      <article><span>Séries planejadas</span><strong>${num(r.seriesSemanaisEstimadas||0,0)}</strong><small>por semana estimada</small></article>
      <article><span>Séries realizadas</span><strong>${num(r.seriesRealizadasPeriodo||0,0)}</strong><small>no período</small></article>
      <article><span>Média realizada</span><strong>${num(r.mediaSeriesRealizadasSemana||0,1)}</strong><small>séries por semana</small></article>
      <article><span>Maior concentração</span><strong>${esc(r.maiorConcentracaoGrupo||'—')}</strong><small>${r.maiorConcentracaoPercentual!=null?num(r.maiorConcentracaoPercentual,1)+'%':'—'}</small></article>
    </div>
    <div class="workout-volume-list">${rows.length?rows.map(x=>hpWorkoutVolumeBar(x,max)).join(''):`<div class="empty">A ficha não possui grupos musculares disponíveis para análise.</div>`}</div>
    <div class="workout-session-volume-list">${(d.porSessao||[]).map(hpWorkoutSessionVolume).join('')}</div>
    <p class="form-hint">${esc(d.observacao||'')}</p>`;

    host.appendChild(section);
  }catch(err){
    console.warn('Análise de volume indisponível:',err);
  }
}

const __renderPatientTab_v036_volume=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v036_volume(d);
  const host=$('#patientTabContent');
  if(!host||!state.patientId)return;
  if(state.patientTab==='treinos'){
    hpInjectWorkoutVolume(host,state.patientId,'workout-volume-main',30);
  }else if(state.patientTab==='resumo'){
    hpInjectWorkoutVolume(host,state.patientId,'workout-volume-summary',30);
  }
};


// ===== v0.3.37 — Progressão por exercício + recordes de carga =====
function hpExerciseTrendLabel(value){
  return ({AcimaDaBase:'Acima da base',Estavel:'Estável',AbaixoDaBase:'Abaixo da base',SemBase:'Sem base'})[value]||value||'Sem base';
}

function hpExerciseProgressCard(x){
  const delta=x.deltaCarga==null?'—':`${x.deltaCarga>0?'+':''}${num(x.deltaCarga,2)} ${esc(x.unidade)}`;
  const percent=x.variacaoPercentual==null?'—':`${x.variacaoPercentual>0?'+':''}${num(x.variacaoPercentual,1)}%`;
  const latest=x.ultimaExecucao||{};
  return `<article class="exercise-progress-card">
    <div class="exercise-progress-head">
      <div><span class="eyebrow">${esc(x.grupoMuscular)} • ${esc(x.unidade)}</span><h4>${esc(x.exercicio)}</h4></div>
      <span class="exercise-trend ${String(x.tendenciaCarga||'').toLowerCase()}">${esc(hpExerciseTrendLabel(x.tendenciaCarga))}</span>
    </div>
    <div class="exercise-progress-metrics">
      <div><span>Inicial</span><strong>${num(x.primeiraCarga,2)} ${esc(x.unidade)}</strong></div>
      <div><span>Atual</span><strong>${num(x.ultimaCarga,2)} ${esc(x.unidade)}</strong></div>
      <div><span>Melhor marca</span><strong>${num(x.maiorCarga,2)} ${esc(x.unidade)}</strong></div>
      <div><span>Evolução</span><strong>${esc(delta)}</strong><small>${esc(percent)}</small></div>
    </div>
    <div class="exercise-progress-foot">
      <span><b>${x.novosRecordes||0}</b> novo(s) recorde(s)</span>
      <span><b>${x.registros||0}</b> registro(s)</span>
      ${latest.seriesRealizadas!=null?`<span><b>${latest.seriesRealizadas}</b> séries no último</span>`:''}
      ${latest.repeticoesRealizadas?`<span>Reps: <b>${esc(latest.repeticoesRealizadas)}</b></span>`:''}
      ${latest.esforcoPercebido!=null?`<span>RPE: <b>${latest.esforcoPercebido}/10</b></span>`:''}
    </div>
  </article>`;
}

function hpExerciseProgressCharts(rows,limit=6){
  const selected=(rows||[]).filter(x=>(x.pontos||[]).length>=2).slice(0,limit);
  if(!selected.length)return `<div class="analytics-empty">Registre carga em pelo menos duas execuções do mesmo exercício e unidade para comparar a evolução.</div>`;
  return hpMetricChartGrid(selected.map(x=>hpLineChart(
    `${x.exercicio} • ${x.unidade}`,
    (x.pontos||[]).map(p=>({date:p.dataHoraInicioUtc,value:p.cargaRealizada,label:`${x.exercicio} • ${hpChartDate(p.dataHoraInicioUtc)}`})),
    ` ${x.unidade}`
  )));
}

function hpExerciseProgressSection(d,title='Progressão por exercício'){
  const rows=d.exercicios||[],r=d.resumo||{},hi=d.destaques?.maiorEvolucao,pr=d.destaques?.maisRecordes;
  return `<div class="card-head"><div><h3>${esc(title)}</h3><small>${d.dias} dias • cargas comparadas por exercício e unidade</small></div><span class="analytics-badge">Carga</span></div>
    <div class="exercise-progress-summary">
      <article><span>Exercícios com carga</span><strong>${r.exerciciosComCarga||0}</strong><small>no período</small></article>
      <article><span>Com base comparativa</span><strong>${r.exerciciosComBaseComparativa||0}</strong><small>2+ registros</small></article>
      <article><span>Novos recordes</span><strong>${r.novosRecordesPeriodo||0}</strong><small>melhores cargas sucessivas</small></article>
      <article><span>Maior evolução</span><strong>${hi?esc(hi.exercicio):'—'}</strong><small>${hi?.variacaoPercentual!=null?`${hi.variacaoPercentual>0?'+':''}${num(hi.variacaoPercentual,1)}% • ${esc(hi.unidade)}`:'sem base suficiente'}</small></article>
    </div>
    ${pr?`<div class="exercise-pr-highlight"><span>🏆 Mais recordes no período</span><strong>${esc(pr.exercicio)}</strong><small>${pr.novosRecordes} novo(s) recorde(s) • melhor ${num(pr.maiorCarga,2)} ${esc(pr.unidade)}</small></div>`:''}
    ${hpExerciseProgressCharts(rows)}
    <div class="exercise-progress-list">${rows.length?rows.map(hpExerciseProgressCard).join(''):`<div class="empty">Ainda não existem cargas registradas nas execuções desse período.</div>`}</div>
    <p class="form-hint">${esc(d.observacao||'')}</p>`;
}

async function hpInjectExerciseProgression(host,patientId,id='exercise-progression',days=180){
  if(!host||!patientId||host.querySelector(`[data-exercise-progression="${id}"]`))return;
  try{
    const d=await api(`/api/pacientes/${patientId}/treinos/progressao-exercicios?dias=${days}`);
    if(!host.isConnected||host.querySelector(`[data-exercise-progression="${id}"]`))return;
    const section=document.createElement('section');
    section.className='card full-card exercise-progress-section';
    section.dataset.exerciseProgression=id;
    section.innerHTML=hpExerciseProgressSection(d);
    host.appendChild(section);
  }catch(err){console.warn('Progressão por exercício indisponível:',err)}
}

async function hpInjectMyExerciseProgression(host,id='my-exercise-progression',days=180){
  if(!host||host.querySelector(`[data-exercise-progression="${id}"]`))return;
  try{
    const d=await api(`/api/portal/me/treinos/progressao-exercicios?dias=${days}`);
    if(!host.isConnected||host.querySelector(`[data-exercise-progression="${id}"]`))return;
    const section=document.createElement('section');
    section.className='card exercise-progress-section';
    section.dataset.exerciseProgression=id;
    section.innerHTML=hpExerciseProgressSection(d,'Minha progressão por exercício');
    host.appendChild(section);
  }catch(err){console.warn('Minha progressão por exercício indisponível:',err)}
}

const __renderPatientTab_v037_exerciseprogress=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v037_exerciseprogress(d);
  const host=$('#patientTabContent');
  if(!host||!state.patientId)return;
  if(state.patientTab==='treinos'){
    hpInjectExerciseProgression(host,state.patientId,'exercise-progression-main',180);
  }else if(state.patientTab==='resumo'){
    hpInjectExerciseProgression(host,state.patientId,'exercise-progression-summary',180);
  }
};

const __loadPatientWorkout_v037_exerciseprogress=loadPatientWorkout;
loadPatientWorkout=async function(){
  await __loadPatientWorkout_v037_exerciseprogress();
  const host=$('#patientPortalContent');
  await hpInjectMyExerciseProgression(host,'my-exercise-progression',180);
};


// ===== v0.3.38 — Estagnação + fadiga + sinais de progressão =====
function hpTrainingSignalLabel(status){
  return ({
    Progredindo:'Progredindo',
    Estagnacao:'Estagnação',
    PossivelFadiga:'Revisão por carga/RPE',
    Estavel:'Estável',
    SemBase:'Sem base'
  })[status]||status||'Sem base';
}

function hpTrainingSignalCard(x){
  const variation=x.variacaoRecentePercentual==null?'—':`${x.variacaoRecentePercentual>0?'+':''}${num(x.variacaoRecentePercentual,1)}%`;
  return `<article class="training-signal-card ${String(x.status||'').toLowerCase()}">
    <div class="training-signal-head">
      <div><span class="eyebrow">${esc(x.grupoMuscular)} • ${esc(x.unidade)}</span><h4>${esc(x.exercicio)}</h4></div>
      <span class="training-signal-badge">${esc(hpTrainingSignalLabel(x.status))}</span>
    </div>
    <div class="training-signal-metrics">
      <div><span>Carga recente</span><strong>${num(x.mediaCargaRecente,2)} ${esc(x.unidade)}</strong></div>
      <div><span>Variação</span><strong>${esc(variation)}</strong></div>
      <div><span>RPE recente</span><strong>${x.mediaRpeRecente==null?'—':num(x.mediaRpeRecente,1)+'/10'}</strong></div>
      <div><span>Dias sem PR</span><strong>${x.diasSemRecorde??'—'}</strong></div>
    </div>
    <div class="training-signal-notes">${(x.sinais||[]).map(s=>`<small>${esc(s)}</small>`).join('')}</div>
    ${x.revisaoSugerida?'<div class="training-review-note">Revisão sugerida pelo histórico recente — sem ajuste automático da prescrição.</div>':''}
  </article>`;
}

function hpTrainingSignalCharts(rows){
  const selected=(rows||[]).filter(x=>x.revisaoSugerida&&(x.pontos||[]).length>=3).slice(0,4);
  if(!selected.length)return '';
  return hpMetricChartGrid(selected.map(x=>hpLineChart(
    `${x.exercicio} • ${hpTrainingSignalLabel(x.status)}`,
    (x.pontos||[]).map(p=>({date:p.dataUtc,value:p.carga,label:`${x.exercicio} • ${hpChartDate(p.dataUtc)}`})),
    ` ${x.unidade}`
  )));
}

function hpTrainingSignalsSection(d,title='Sinais de progressão'){
  const r=d.resumo||{},rows=d.exercicios||[];
  return `<div class="card-head"><div><h3>${esc(title)}</h3><small>${d.dias} dias • leitura de carga + RPE das execuções</small></div><span class="analytics-badge">Sinais</span></div>
    <div class="training-signal-summary">
      <article><span>Progredindo</span><strong>${r.progredindo||0}</strong><small>séries de exercício</small></article>
      <article><span>Estagnação</span><strong>${r.estagnacao||0}</strong><small>revisão possível</small></article>
      <article><span>Carga/RPE</span><strong>${r.possivelFadiga||0}</strong><small>revisão sugerida</small></article>
      <article><span>Sem base</span><strong>${r.semBase||0}</strong><small>menos de 3 registros</small></article>
    </div>
    ${hpTrainingSignalCharts(rows)}
    <div class="training-signal-list">${rows.length?rows.map(hpTrainingSignalCard).join(''):`<div class="empty">Ainda não existem registros suficientes para analisar progressão.</div>`}</div>
    <p class="form-hint">${esc(d.observacao||'')}</p>`;
}

async function hpInjectTrainingSignals(host,patientId,id='training-signals',days=120){
  if(!host||!patientId||host.querySelector(`[data-training-signals="${id}"]`))return;
  try{
    const d=await api(`/api/pacientes/${patientId}/treinos/analise-progresso?dias=${days}`);
    if(!host.isConnected||host.querySelector(`[data-training-signals="${id}"]`))return;
    const section=document.createElement('section');
    section.className='card full-card training-signal-section';
    section.dataset.trainingSignals=id;
    section.innerHTML=hpTrainingSignalsSection(d);
    host.appendChild(section);
  }catch(err){console.warn('Sinais de progressão indisponíveis:',err)}
}

async function hpInjectMyTrainingSignals(host,id='my-training-signals',days=120){
  if(!host||host.querySelector(`[data-training-signals="${id}"]`))return;
  try{
    const d=await api(`/api/portal/me/treinos/analise-progresso?dias=${days}`);
    if(!host.isConnected||host.querySelector(`[data-training-signals="${id}"]`))return;
    const section=document.createElement('section');
    section.className='card training-signal-section';
    section.dataset.trainingSignals=id;
    section.innerHTML=hpTrainingSignalsSection(d,'Meus sinais de progressão');
    host.appendChild(section);
  }catch(err){console.warn('Meus sinais de progressão indisponíveis:',err)}
}

const __renderPatientTab_v038_trainingsignals=renderPatientTab;
renderPatientTab=function(d){
  __renderPatientTab_v038_trainingsignals(d);
  const host=$('#patientTabContent');
  if(!host||!state.patientId)return;
  if(state.patientTab==='treinos'){
    hpInjectTrainingSignals(host,state.patientId,'training-signals-main',120);
  }else if(state.patientTab==='resumo'){
    hpInjectTrainingSignals(host,state.patientId,'training-signals-summary',120);
  }
};

const __loadPatientWorkout_v038_trainingsignals=loadPatientWorkout;
loadPatientWorkout=async function(){
  await __loadPatientWorkout_v038_trainingsignals();
  const host=$('#patientPortalContent');
  await hpInjectMyTrainingSignals(host,'my-training-signals',120);
};


// ===== v0.3.39 — MVP Preview / polimento de demonstração =====
const HP_MVP_VERSION='0.3.41';

function hpMvpChecklistItem(icon,title,text){
  return `<article class="mvp-guide-item"><span>${icon}</span><div><strong>${esc(title)}</strong><small>${esc(text)}</small></div></article>`;
}

function openMvpGuide(){
  const modal=$('#clinicalActionModal'),box=$('#clinicalActionContent');
  if(!modal||!box)return;
  modal.classList.remove('hidden');
  box.innerHTML=`<div class="modal-heading">
      <span class="eyebrow">MVP PREVIEW • v${HP_MVP_VERSION}</span>
      <h2>Roteiro rápido para testar o sistema</h2>
      <p>Não precisa testar tudo de uma vez. Use o sistema como usaria no dia a dia e anote principalmente onde você hesitar, procurar demais ou sentir falta de alguma coisa.</p>
    </div>
    <div class="mvp-guide-grid">
      ${hpMvpChecklistItem('01','Cadastre ou escolha um paciente','Veja se encontrar, abrir e entender o prontuário parece natural.')}
      ${hpMvpChecklistItem('02','Simule uma consulta','Registre consulta, avaliação, anamnese ou evolução e observe se falta algum campo importante.')}
      ${hpMvpChecklistItem('03','Monte alimentação e treino','Teste criação, edição, fases, metas, sessões e progressão como faria com um paciente real fictício.')}
      ${hpMvpChecklistItem('04','Use os acompanhamentos','Confira check-ins, gráficos, alertas, pendências, follow-up e sinais de evolução.')}
      ${hpMvpChecklistItem('05','Entre como paciente','Avalie se o portal mostra o que um paciente realmente precisa enxergar sem informação demais.')}
      ${hpMvpChecklistItem('06','Procure atritos','Anote botões difíceis de achar, telas cheias, nomes confusos, passos repetitivos e qualquer comportamento estranho.')}
    </div>
    <div class="mvp-feedback-card">
      <strong>O feedback mais valioso agora</strong>
      <p><b>Faltou:</b> algo que você procurou e não encontrou.<br><b>Confundiu:</b> algo que existe, mas você não entendeu de primeira.<br><b>Demorou:</b> algo que exige cliques demais.<br><b>Quebrou:</b> qualquer erro, comportamento estranho ou dado incoerente.</p>
    </div>
    <div class="form-actions"><button class="secondary" type="button" id="copyMvpFeedbackTemplate">Copiar modelo de feedback</button><button class="primary" type="button" data-close-mvp-guide>Começar a testar</button></div>`;

  const close=()=>closeClinicalAction();
  $('[data-close-mvp-guide]').onclick=close;
  $('#copyMvpFeedbackTemplate').onclick=async()=>{
    const template=`HealthPlatform MVP v${HP_MVP_VERSION}

TELA/FLUXO:
O QUE EU ESTAVA TENTANDO FAZER:

FALTOU:
CONFUNDIU:
DEMOROU:
QUEBROU/BUG:
SUGESTÃO:

PRIORIDADE: baixa / média / alta`;
    try{
      await navigator.clipboard.writeText(template);
      toast('Modelo de feedback copiado.');
    }catch{
      toast('Não foi possível copiar automaticamente.',true);
    }
  };
}

function hpInstallMvpPreviewUi(){
  const guide=$('#mvpGuideButton');
  if(guide)guide.onclick=openMvpGuide;

  document.body.dataset.mvp='preview';
  document.body.dataset.mvpVersion=HP_MVP_VERSION;

  // Escape fecha a camada mais provável sem alterar dados.
  document.addEventListener('keydown',e=>{
    if(e.key!=='Escape')return;
    const clinical=$('#clinicalActionModal');
    const create=$('#createPatientModal');
    const patient=$('#patientModal');
    const notifications=$('#notificationDrawer');

    if(clinical&&!clinical.classList.contains('hidden')){closeClinicalAction();return}
    if(create&&!create.classList.contains('hidden')){create.classList.add('hidden');return}
    if(patient&&!patient.classList.contains('hidden')){patient.classList.add('hidden');return}
    if(notifications&&!notifications.classList.contains('hidden')){
      notifications.classList.add('hidden');
      return;
    }
    $('.sidebar')?.classList.remove('open');
  });

  // Melhora a mensagem quando o navegador estiver offline durante a demo.
  window.addEventListener('offline',()=>toast('Sem conexão. Aguarde a internet voltar para continuar.',true));
  window.addEventListener('online',()=>toast('Conexão restabelecida.'));
}

hpInstallMvpPreviewUi();


// ===== v0.3.41 — RS visual identity / mobile + tablet UX =====
function hpInstallRsResponsiveUi(){
  const app=$('#appView'),sidebar=$('.sidebar'),menu=$('#menuButton');
  if(!app||!sidebar||!menu||app.querySelector('.rs-sidebar-screen'))return;

  const screen=document.createElement('button');
  screen.type='button';
  screen.className='rs-sidebar-screen';
  screen.setAttribute('aria-label','Fechar menu');
  app.appendChild(screen);

  const sync=()=>{
    const open=sidebar.classList.contains('open');
    screen.classList.toggle('visible',open);
    document.body.classList.toggle('rs-menu-open',open&&window.innerWidth<=820);
    menu.setAttribute('aria-expanded',String(open));
  };

  menu.setAttribute('aria-controls','professionalSidebar');
  sidebar.id='professionalSidebar';
  menu.addEventListener('click',()=>requestAnimationFrame(sync));
  screen.onclick=()=>{
    sidebar.classList.remove('open');
    sync();
  };

  $$('.sidebar .nav-item').forEach(item=>item.addEventListener('click',()=>{
    sidebar.classList.remove('open');
    sync();
  }));

  window.addEventListener('resize',()=>{
    if(window.innerWidth>820)sidebar.classList.remove('open');
    sync();
  });

  // Gives iOS/iPadOS a stable viewport class without UA sniffing.
  document.documentElement.classList.toggle('rs-touch-ui',matchMedia('(pointer:coarse)').matches);
  matchMedia('(pointer:coarse)').addEventListener?.('change',e=>{
    document.documentElement.classList.toggle('rs-touch-ui',e.matches);
  });

  sync();
}
hpInstallRsResponsiveUi();
