BEGIN;

CREATE TABLE IF NOT EXISTS "FasesNutricionais" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "PlanoAlimentarId" uuid NULL,
    "Nome" character varying(160) NOT NULL,
    "Tipo" character varying(50) NOT NULL,
    "Objetivo" character varying(600) NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date NULL,
    "Ordem" integer NOT NULL DEFAULT 1,
    "Status" character varying(30) NOT NULL DEFAULT 'Planejada',
    "Observacoes" character varying(1200) NULL,
    CONSTRAINT "PK_FasesNutricionais" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FasesNutricionais_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FasesNutricionais_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FasesNutricionais_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FasesNutricionais_PlanosAlimentares_PlanoAlimentarId"
        FOREIGN KEY ("PlanoAlimentarId") REFERENCES "PlanosAlimentares" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_FasesNutricionais_PacienteId_Ordem"
    ON "FasesNutricionais" ("PacienteId", "Ordem");

CREATE INDEX IF NOT EXISTS "IX_FasesNutricionais_OrganizacaoId_Status"
    ON "FasesNutricionais" ("OrganizacaoId", "Status");

CREATE INDEX IF NOT EXISTS "IX_FasesNutricionais_PlanoAlimentarId"
    ON "FasesNutricionais" ("PlanoAlimentarId");

CREATE INDEX IF NOT EXISTS "IX_FasesNutricionais_ProfissionalId"
    ON "FasesNutricionais" ("ProfissionalId");

COMMIT;
