-- HealthPlatform v0.1.4 - upgrade idempotente de exames laboratoriais.
-- Bancos novos recebem estas estruturas pelo baseline EF atual; bancos existentes recebem via SQL.

CREATE TABLE IF NOT EXISTS "MarcadoresLaboratoriais" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "Nome" character varying(160) NOT NULL,
    "NomeNormalizado" character varying(160) NOT NULL,
    "Categoria" character varying(100) NULL,
    "UnidadePadrao" character varying(50) NULL,
    "Ativo" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_MarcadoresLaboratoriais" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MarcadoresLaboratoriais_Organizacoes_OrganizacaoId" FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_MarcadoresLaboratoriais_OrganizacaoId_NomeNormalizado"
    ON "MarcadoresLaboratoriais" ("OrganizacaoId", "NomeNormalizado");

CREATE TABLE IF NOT EXISTS "ExamesLaboratoriais" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "DataColetaUtc" timestamp with time zone NOT NULL,
    "Laboratorio" character varying(160) NULL,
    "Observacoes" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_ExamesLaboratoriais" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExamesLaboratoriais_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ExamesLaboratoriais_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_ExamesLaboratoriais_PacienteId_DataColetaUtc"
    ON "ExamesLaboratoriais" ("PacienteId", "DataColetaUtc");
CREATE INDEX IF NOT EXISTS "IX_ExamesLaboratoriais_ProfissionalId"
    ON "ExamesLaboratoriais" ("ProfissionalId");

CREATE TABLE IF NOT EXISTS "ResultadosExamesLaboratoriais" (
    "Id" uuid NOT NULL,
    "ExameLaboratorialId" uuid NOT NULL,
    "MarcadorLaboratorialId" uuid NOT NULL,
    "ValorNumerico" numeric NULL,
    "ValorTexto" character varying(300) NULL,
    "Unidade" character varying(50) NULL,
    "ReferenciaMinima" numeric NULL,
    "ReferenciaMaxima" numeric NULL,
    "ReferenciaTexto" character varying(300) NULL,
    "Observacao" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_ResultadosExamesLaboratoriais" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ResultadosExamesLaboratoriais_ExamesLaboratoriais_ExameLaboratorialId" FOREIGN KEY ("ExameLaboratorialId") REFERENCES "ExamesLaboratoriais" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ResultadosExamesLaboratoriais_MarcadoresLaboratoriais_MarcadorLaboratorialId" FOREIGN KEY ("MarcadorLaboratorialId") REFERENCES "MarcadoresLaboratoriais" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ResultadosExamesLaboratoriais_ExameLaboratorialId_MarcadorLaboratorialId"
    ON "ResultadosExamesLaboratoriais" ("ExameLaboratorialId", "MarcadorLaboratorialId");
CREATE INDEX IF NOT EXISTS "IX_ResultadosExamesLaboratoriais_MarcadorLaboratorialId"
    ON "ResultadosExamesLaboratoriais" ("MarcadorLaboratorialId");
