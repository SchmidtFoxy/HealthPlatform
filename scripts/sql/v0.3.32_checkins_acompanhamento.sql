BEGIN;

CREATE TABLE IF NOT EXISTS "CheckInsAcompanhamento" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "FaseNutricionalId" uuid NULL,
    "FaseTreinoId" uuid NULL,
    "RegistradoPorUsuarioId" uuid NOT NULL,
    "DataUtc" timestamp with time zone NOT NULL,
    "PesoKg" numeric(8,2) NULL,
    "AdesaoAlimentacaoPercentual" integer NULL,
    "AdesaoTreinoPercentual" integer NULL,
    "FomeNivel" integer NULL,
    "EnergiaNivel" integer NULL,
    "SonoNivel" integer NULL,
    "PercepcaoEvolucaoNivel" integer NULL,
    "Observacoes" character varying(2000) NULL,
    "Origem" character varying(30) NOT NULL DEFAULT 'Profissional',
    CONSTRAINT "PK_CheckInsAcompanhamento" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CheckInsAcompanhamento_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CheckInsAcompanhamento_FasesNutricionais_FaseNutricionalId"
        FOREIGN KEY ("FaseNutricionalId") REFERENCES "FasesNutricionais" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CheckInsAcompanhamento_FasesTreino_FaseTreinoId"
        FOREIGN KEY ("FaseTreinoId") REFERENCES "FasesTreino" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_CheckInsAcompanhamento_PacienteId_DataUtc"
    ON "CheckInsAcompanhamento" ("PacienteId", "DataUtc");

CREATE INDEX IF NOT EXISTS "IX_CheckInsAcompanhamento_OrganizacaoId_DataUtc"
    ON "CheckInsAcompanhamento" ("OrganizacaoId", "DataUtc");

CREATE INDEX IF NOT EXISTS "IX_CheckInsAcompanhamento_FaseNutricionalId"
    ON "CheckInsAcompanhamento" ("FaseNutricionalId");

CREATE INDEX IF NOT EXISTS "IX_CheckInsAcompanhamento_FaseTreinoId"
    ON "CheckInsAcompanhamento" ("FaseTreinoId");

COMMIT;
