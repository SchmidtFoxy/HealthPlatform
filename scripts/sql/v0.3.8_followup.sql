BEGIN;

CREATE TABLE IF NOT EXISTS "InteracoesAcompanhamento" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "DataHoraUtc" timestamp with time zone NOT NULL,
    "Canal" character varying(40) NOT NULL,
    "Resultado" character varying(240) NOT NULL,
    "Observacoes" character varying(3000) NULL,
    "ProximoContatoUtc" timestamp with time zone NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_InteracoesAcompanhamento" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InteracoesAcompanhamento_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InteracoesAcompanhamento_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InteracoesAcompanhamento_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_InteracoesAcompanhamento_PacienteId_DataHoraUtc"
    ON "InteracoesAcompanhamento" ("PacienteId", "DataHoraUtc");
CREATE INDEX IF NOT EXISTS "IX_InteracoesAcompanhamento_ProfissionalId_DataHoraUtc"
    ON "InteracoesAcompanhamento" ("ProfissionalId", "DataHoraUtc");
CREATE INDEX IF NOT EXISTS "IX_InteracoesAcompanhamento_OrganizacaoId_ProximoContatoUtc"
    ON "InteracoesAcompanhamento" ("OrganizacaoId", "ProximoContatoUtc");

COMMIT;
