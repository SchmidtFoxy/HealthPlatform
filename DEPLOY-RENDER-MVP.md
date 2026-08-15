# HealthPlatform v0.3.40 — Deploy do MVP no Render

Esta configuração é propositalmente simples e serve para **demonstração/teste de usabilidade**, não como arquitetura final de produção.

## O que o Blueprint cria

O `render.yaml` cria:

- 1 Web Service Docker gratuito: `healthplatform-mvp`
- 1 PostgreSQL gratuito: `healthplatform-mvp-db`
- health check HTTP em `/api/health`
- JWT secret gerado pelo Render
- conexão com o Postgres via rede interna
- bootstrap de schema somente para o banco demo novo

## 1. Suba esta pasta para um repositório Git

O `render.yaml` e o `Dockerfile` precisam ficar na raiz do repositório.

## 2. Crie o Blueprint

No Render:

1. **New**
2. **Blueprint**
3. conecte o repositório
4. confirme os recursos detectados
5. quando o Render pedir `Seed__AdminPassword`, informe uma senha forte e guarde-a

O login profissional será:

- e-mail: `admin@healthplatform.local`
- senha: a que você informou no Blueprint

## 3. Aguarde o deploy

Quando o deploy finalizar, abra a URL `https://...onrender.com`.

O endpoint abaixo deve responder `status: ok`:

`/api/health`

## 4. Popule o ambiente remoto

No seu Windows, dentro desta mesma pasta:

```powershell
.\POPULAR-REMOTO.ps1 `
  -BaseUrl "https://SEU-SERVICO.onrender.com" `
  -Senha "SENHA_DO_ADMIN"
```

O script é idempotente e cria os cenários fictícios usados na demonstração.

Ele também prepara um acesso de paciente:

- e-mail: `ana.ribeiro.demo@healthplatform.local`
- senha: `PacienteDemo_123!`

## 5. Faça o smoke test remoto

```powershell
.\TESTAR-RENDER.ps1 `
  -BaseUrl "https://SEU-SERVICO.onrender.com" `
  -Senha "SENHA_DO_ADMIN"
```

Meta: `12/12`.

## Como o bootstrap funciona

Localmente nada mudou: `PREPARAR.ps1` continua usando a migration baseline estável e os upgrades SQL históricos.

No Render, quando `DemoBootstrap__Enabled=true`, a aplicação usa `EnsureCreatedAsync()` para criar **o schema atual inteiro em um Postgres novo e vazio**, e então executa o seed do admin.

Isso é adequado para esta demonstração congelada. Não é a estratégia de migrations que deverá ser usada no host definitivo.

## Observação sobre o plano gratuito

O banco gratuito do Render é temporário. Não coloque dados reais ou algo que precise ser preservado a longo prazo.

Use somente pacientes e informações fictícias.
