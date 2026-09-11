# HealthPlatform — Changelog

## v0.5.0 — Connected Care Stable Milestone
**Tipo:** marco de versão / promoção estável

- promove a v0.4.6, validada com 508/508 testes, como nova baseline;
- consolida a experiência Patient Today introduzida no ciclo v0.4.x;
- mantém identidade visual RS e UX responsiva para desktop, tablet e mobile;
- atualiza runtime, healthcheck, Swagger, interface e scripts para 0.5.0;
- não adiciona schema, migration, regra clínica ou mutação de dados.

## Ciclo v0.4.x — resumo real

### v0.4.0 — Connected Care / Patient Today
**Tipo:** feature

- nova experiência diária do paciente;
- registros rápidos de peso, água, sono, dor, energia e sintomas;
- integração com metas, próxima consulta, plano alimentar, exames e evolução;
- reutilização do `RegistroDiarioPaciente`, sem criar estrutura paralela.

### v0.4.0-r1
**Tipo:** hotfix de teste

- atualiza o smoke test do `POPULAR-REMOTO-RICO.ps1` para a implementação V2 atual;
- sem alteração funcional ou de banco.

### v0.4.1
**Tipo:** promoção técnica

- promoção da baseline 0.4.0-r1;
- sem funcionalidade clínica nova.

### v0.4.2
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.3
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.4
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.4-r1
**Tipo:** hotfix de teste

- corrige a validação 60/508 para não depender de texto acentuado no Windows PowerShell;
- passa a validar rota e estrutura reais do histórico de treino;
- sem alteração funcional ou de banco.

### v0.4.5
**Tipo:** promoção técnica

- promoção da baseline 0.4.4-r1 validada com 508/508;
- sem funcionalidade clínica nova.

### v0.4.6
**Tipo:** promoção técnica

- promoção da baseline 0.4.5 validada com 508/508;
- sem funcionalidade clínica nova.

## Próximo ciclo funcional

O ciclo v0.5.x será usado para evoluções reais do Connected Care, priorizando a ponte profissional ↔ paciente. As próximas entregas devem privilegiar módulos como solicitações clínicas, tarefas/acompanhamento, protocolos configuráveis e comunicação contextual, e cada release deverá declarar explicitamente o que entrou.
