# Checklist de privacidade / LGPD — v0.19.43

Este checklist é técnico e de produto; **não certifica conformidade jurídica**.

- [x] Aceite versionado de Termos de Uso.
- [x] Aceite versionado de Política de Privacidade.
- [x] Data/hora dos aceites registradas no usuário e no AuditLog.
- [x] Consulta do status de privacidade pelo titular autenticado.
- [x] Correção de dados básicos já disponível em Minha Conta.
- [x] Solicitação de exclusão registrada para análise, sem apagar prontuário/histórico silenciosamente.
- [x] Desativação de conta exige senha + confirmação explícita.
- [x] Desativação revoga Web Push e SecurityStamp.
- [x] Auditoria visível ao próprio usuário sem expor payloads sensíveis.
- [x] Treino, dieta, arquivos, alterações de paciente, impersonação e arquivamentos possuem gates de auditoria no smoke test.
- [x] Tokens, senhas e conteúdo de arquivos não devem entrar no AuditLog.
- [ ] Revisar controlador/operador, DPO/canal de contato e bases legais com jurídico antes da produção comercial.
- [ ] Definir política formal de retenção por categoria de dado e obrigação profissional.
- [ ] Definir procedimento operacional para pedidos do titular e prazos aplicáveis.
- [ ] Revisar contratos com operadores/suboperadores de infraestrutura, e-mail e push.
