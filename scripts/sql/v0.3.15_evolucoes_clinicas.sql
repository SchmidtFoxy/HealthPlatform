BEGIN;

CREATE TABLE IF NOT EXISTS "EvolucoesClinicas" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "ConsultaId" uuid NULL,
    "DataHoraUtc" timestamp with time zone NOT NULL,
    "Subjetivo" character varying(6000) NULL,
    "Objetivo" character varying(6000) NULL,
    "Avaliacao" character varying(6000) NULL,
    "Plano" character varying(6000) NULL,
    "Observacoes" character varying(3000) NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_EvolucoesClinicas" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EvolucoesClinicas_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EvolucoesClinicas_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EvolucoesClinicas_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EvolucoesClinicas_Consultas_ConsultaId"
        FOREIGN KEY ("ConsultaId") REFERENCES "Consultas" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_EvolucoesClinicas_PacienteId_DataHoraUtc"
    ON "EvolucoesClinicas" ("PacienteId", "DataHoraUtc");
CREATE INDEX IF NOT EXISTS "IX_EvolucoesClinicas_ProfissionalId_DataHoraUtc"
    ON "EvolucoesClinicas" ("ProfissionalId", "DataHoraUtc");
CREATE INDEX IF NOT EXISTS "IX_EvolucoesClinicas_ConsultaId"
    ON "EvolucoesClinicas" ("ConsultaId");

COMMIT;
