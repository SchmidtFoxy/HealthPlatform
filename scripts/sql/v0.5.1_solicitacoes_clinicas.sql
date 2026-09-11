BEGIN;

CREATE TABLE IF NOT EXISTS "SolicitacoesClinicas" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Tipo" character varying(60) NOT NULL,
    "Titulo" character varying(300) NOT NULL,
    "Descricao" character varying(3000) NULL,
    "DataLimiteUtc" timestamp with time zone NULL,
    "Status" character varying(30) NOT NULL DEFAULT 'Pendente',
    "RespostaPaciente" character varying(4000) NULL,
    "LinkResposta" character varying(1000) NULL,
    "RespondidaEmUtc" timestamp with time zone NULL,
    "RevisadaEmUtc" timestamp with time zone NULL,
    "ObservacaoRevisao" character varying(3000) NULL,
    CONSTRAINT "PK_SolicitacoesClinicas" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SolicitacoesClinicas_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SolicitacoesClinicas_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_SolicitacoesClinicas_OrganizacaoId_Status_DataLimiteUtc"
    ON "SolicitacoesClinicas" ("OrganizacaoId", "Status", "DataLimiteUtc");
CREATE INDEX IF NOT EXISTS "IX_SolicitacoesClinicas_PacienteId_Status"
    ON "SolicitacoesClinicas" ("PacienteId", "Status");
CREATE INDEX IF NOT EXISTS "IX_SolicitacoesClinicas_ProfissionalId"
    ON "SolicitacoesClinicas" ("ProfissionalId");

COMMIT;
