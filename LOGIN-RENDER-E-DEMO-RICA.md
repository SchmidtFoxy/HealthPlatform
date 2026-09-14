# LOGIN E DEMO RICA — Render

## Se clicar em Entrar e não funcionar

O e-mail profissional padrão é:

`admin@healthplatform.local`

A senha **não é** `ChangeMe_123!` no Render, a menos que você tenha escolhido exatamente essa senha.

Use o valor configurado no Render em:

`Seed__AdminPassword`

Se a variável não existir:

1. abra o Web Service no Render;
2. vá em **Environment**;
3. crie `Seed__AdminPassword`;
4. escolha uma senha forte;
5. confirme também `Seed__AdminEmail=admin@healthplatform.local`;
6. salve;
7. faça um novo deploy/restart.

Como `DemoBootstrap__Enabled=true`, o seed do admin roda na inicialização e cria o usuário caso ele ainda não exista.

## Popular a demo

Depois que o login profissional funcionar:

```powershell
.\POPULAR-REMOTO-RICO.ps1 `
  -BaseUrl "https://SEU-SERVICO.onrender.com" `
  -Senha "SUA_SENHA_DO_RENDER"
```

O script primeiro garante a população base e depois adiciona histórico clínico, SOAP, anamneses, exames, alimentação, fases, check-ins e execuções de treino.
