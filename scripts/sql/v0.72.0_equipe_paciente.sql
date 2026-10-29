-- v0.72.0: equipe do paciente. Cria a tabela ProfissionaisPacientes e ja coloca cada profissional
-- na equipe dos pacientes que ele atende hoje (consulta nao cancelada nem falta, plano de dieta ou
-- treino, evolucao, anamnese, resposta no chat ou responsavel definido; numa clinica com
-- um so profissional ativo, ele fica com todos os pacientes, como ja e hoje). Nada e apagado ou alterado
-- nas tabelas que ja existem. Pode rodar de novo sem problema.
BEGIN;
CREATE TABLE IF NOT EXISTS "ProfissionaisPacientes" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "CriadoPorUsuarioId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_ProfissionaisPacientes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProfissionaisPacientes_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProfissionaisPacientes_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_ProfissionaisPacientes_OrganizacaoId_ProfissionalId" ON "ProfissionaisPacientes" ("OrganizacaoId", "ProfissionalId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProfissionaisPacientes_PacienteId_ProfissionalId" ON "ProfissionaisPacientes" ("PacienteId", "ProfissionalId");
CREATE INDEX IF NOT EXISTS "IX_ProfissionaisPacientes_ProfissionalId" ON "ProfissionaisPacientes" ("ProfissionalId");

INSERT INTO "ProfissionaisPacientes" ("Id", "OrganizacaoId", "PacienteId", "ProfissionalId", "CreatedAtUtc")
SELECT gen_random_uuid(), p."OrganizacaoId", v."PacienteId", v."ProfissionalId", now()
FROM (
    SELECT "PacienteId", "ProfissionalId" FROM "Consultas" WHERE "Status" NOT IN (4, 5)
    UNION SELECT "PacienteId", "ProfissionalId" FROM "PlanosAlimentares"
    UNION SELECT "PacienteId", "ProfissionalId" FROM "PlanosTreino"
    UNION SELECT "PacienteId", "ProfissionalId" FROM "EvolucoesClinicas"
    UNION SELECT "PacienteId", "ProfissionalId" FROM "Anamneses"
    UNION SELECT "PacienteId", "ProfissionalId" FROM "InteracoesAcompanhamento"
        WHERE "Canal" LIKE 'Chat:%' AND "Resultado" = 'Profissional'
    UNION SELECT "Id", "ProfissionalResponsavelId" FROM "Pacientes" WHERE "ProfissionalResponsavelId" IS NOT NULL
    -- Clinica com um so profissional ativo: hoje ele ja recebe tudo, entao continua com todos.
    UNION SELECT pa."Id", pu."Id" FROM "Pacientes" pa
        JOIN "Profissionais" pu ON pu."OrganizacaoId" = pa."OrganizacaoId" AND pu."Ativo"
        WHERE (SELECT count(*) FROM "Profissionais" x WHERE x."OrganizacaoId" = pa."OrganizacaoId" AND x."Ativo") = 1
) v
JOIN "Pacientes" p ON p."Id" = v."PacienteId"
JOIN "Profissionais" pr ON pr."Id" = v."ProfissionalId" AND pr."OrganizacaoId" = p."OrganizacaoId"
ON CONFLICT ("PacienteId", "ProfissionalId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261029180000_V0700EquipePaciente', '10.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;
COMMIT;
-- Conferencia: quantos pacientes ativos ficaram sem ninguem na equipe (so o admin ve).
SELECT count(*) AS pacientes_sem_equipe FROM "Pacientes" p
WHERE p."Ativo" AND NOT EXISTS (SELECT 1 FROM "ProfissionaisPacientes" e WHERE e."PacienteId" = p."Id");
