BEGIN;

CREATE TABLE IF NOT EXISTS "RevisoesFases" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "RevisadoPorUsuarioId" uuid NOT NULL,
    "Dominio" character varying(20) NOT NULL,
    "FaseId" uuid NOT NULL,
    "FaseNome" character varying(160) NOT NULL,
    "FaseDestinoId" uuid NULL,
    "FaseDestinoNome" character varying(160) NULL,
    "Decisao" character varying(30) NOT NULL,
    "Justificativa" character varying(2000) NOT NULL,
    "DataUtc" timestamp with time zone NOT NULL,
    "StatusAntes" character varying(30) NOT NULL,
    "StatusDepois" character varying(30) NOT NULL,
    "CriteriosConfigurados" integer NOT NULL DEFAULT 0,
    "CriteriosAtendidos" integer NOT NULL DEFAULT 0,
    "ObjetivosProntosParaRevisao" boolean NOT NULL DEFAULT FALSE,
    "OverrideCriterios" boolean NOT NULL DEFAULT FALSE,
    "CriterioProfissional" character varying(1000) NULL,
    "SnapshotIndicadoresJson" text NULL,
    CONSTRAINT "PK_RevisoesFases" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RevisoesFases_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_RevisoesFases_PacienteId_DataUtc"
    ON "RevisoesFases" ("PacienteId", "DataUtc");

CREATE INDEX IF NOT EXISTS "IX_RevisoesFases_OrganizacaoId_Dominio_DataUtc"
    ON "RevisoesFases" ("OrganizacaoId", "Dominio", "DataUtc");

CREATE INDEX IF NOT EXISTS "IX_RevisoesFases_FaseId"
    ON "RevisoesFases" ("FaseId");

COMMIT;
