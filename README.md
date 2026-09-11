# v0.5.7 — Monitoramento Guiado / Connected Care

A v0.5.7 transforma o diário genérico do paciente em uma experiência mais orientada para sinais e sintomas, sem criar schema novo.

## O que entrou

- registros rápidos de pressão arterial, glicemia, frequência cardíaca, saturação e temperatura;
- pressão registrada de forma estruturada em sistólica + diastólica;
- mantém peso, água, sono, dor, energia e sintomas no fluxo diário;
- novo endpoint profissional `GET /api/pacientes/{id}/monitoramento?dias=7`;
- novo endpoint PatientOnly `GET /api/portal/me/monitoramento?dias=7`;
- resumo de último valor, média, mínimo e máximo no período;
- novo card de **Monitoramento remoto** no resumo do prontuário;
- aviso explícito de que os dados informados pelo paciente não substituem avaliação clínica;
- reutiliza `RegistrosDiarioPaciente`: **sem schema novo e sem migration nova**.

## Baseline

A v0.5.6 foi validada pelo usuário com **561/561 testes** e é a baseline desta release.

## Validação esperada

- `PREPARAR.ps1`: 31/31.
- `TESTAR.ps1`: 570/570.
