BEGIN;

CREATE TABLE IF NOT EXISTS "FasesTreino" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "PlanoTreinoId" uuid NULL,
    "Nome" character varying(160) NOT NULL,
    "Tipo" character varying(50) NOT NULL,
    "Objetivo" character varying(600) NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date NULL,
    "Ordem" integer NOT NULL DEFAULT 1,
    "Status" character varying(30) NOT NULL DEFAULT 'Planejada',
    "Observacoes" character varying(1200) NULL,
    CONSTRAINT "PK_FasesTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FasesTreino_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FasesTreino_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FasesTreino_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FasesTreino_PlanosTreino_PlanoTreinoId"
        FOREIGN KEY ("PlanoTreinoId") REFERENCES "PlanosTreino" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_FasesTreino_PacienteId_Ordem"
    ON "FasesTreino" ("PacienteId", "Ordem");
CREATE INDEX IF NOT EXISTS "IX_FasesTreino_OrganizacaoId_Status"
    ON "FasesTreino" ("OrganizacaoId", "Status");
CREATE INDEX IF NOT EXISTS "IX_FasesTreino_PlanoTreinoId"
    ON "FasesTreino" ("PlanoTreinoId");
CREATE INDEX IF NOT EXISTS "IX_FasesTreino_ProfissionalId"
    ON "FasesTreino" ("ProfissionalId");

COMMIT;
