BEGIN;

CREATE TABLE IF NOT EXISTS "ProtocolosAcompanhamento" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Tipo" character varying(60) NOT NULL,
    "Titulo" character varying(180) NOT NULL,
    "Unidade" character varying(40) NULL,
    "Frequencia" character varying(30) NOT NULL DEFAULT 'Diario',
    "HorarioLocal" character varying(5) NULL,
    "DiasSemana" character varying(80) NULL,
    "Instrucoes" character varying(1200) NULL,
    "Ativo" boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_ProtocolosAcompanhamento" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProtocolosAcompanhamento_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ProtocolosAcompanhamento_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_ProtocolosAcompanhamento_OrganizacaoId_PacienteId_Ativo" ON "ProtocolosAcompanhamento" ("OrganizacaoId", "PacienteId", "Ativo");
CREATE INDEX IF NOT EXISTS "IX_ProtocolosAcompanhamento_PacienteId_Tipo_Ativo" ON "ProtocolosAcompanhamento" ("PacienteId", "Tipo", "Ativo");

COMMIT;
