-- HealthPlatform v0.1.3 - upgrade idempotente para bancos criados pelas versoes 0.1.0-0.1.2.
-- Em instalacoes novas, o EF baseline ja cria essas tabelas; por isso tudo usa IF NOT EXISTS.

CREATE TABLE IF NOT EXISTS "Anamneses" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "ConsultaId" uuid NULL,
    "DataUtc" timestamp with time zone NOT NULL,
    "ObjetivoAcompanhamento" character varying(2000) NULL,
    "HistoricoDoencas" text NULL,
    "HistoricoFamiliar" text NULL,
    "Cirurgias" text NULL,
    "Alergias" text NULL,
    "Medicamentos" text NULL,
    "Suplementos" text NULL,
    "Tabagismo" character varying(80) NULL,
    "Etilismo" character varying(80) NULL,
    "SonoHorasMedia" numeric NULL,
    "SonoQualidade" character varying(80) NULL,
    "DespertaDuranteNoite" boolean NULL,
    "EstresseNivel" integer NULL,
    "AtividadeFisica" text NULL,
    "AtividadeFisicaDiasSemana" integer NULL,
    "HabitoIntestinal" text NULL,
    "AguaLitrosDia" numeric NULL,
    "Observacoes" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_Anamneses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Anamneses_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Anamneses_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Anamneses_Consultas_ConsultaId" FOREIGN KEY ("ConsultaId") REFERENCES "Consultas" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Anamneses_PacienteId_DataUtc" ON "Anamneses" ("PacienteId", "DataUtc");
CREATE INDEX IF NOT EXISTS "IX_Anamneses_ProfissionalId" ON "Anamneses" ("ProfissionalId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Anamneses_ConsultaId" ON "Anamneses" ("ConsultaId") WHERE "ConsultaId" IS NOT NULL;

CREATE TABLE IF NOT EXISTS "PerguntasAnamnese" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Texto" character varying(500) NOT NULL,
    "TipoResposta" character varying(30) NOT NULL,
    "OpcoesJson" text NULL,
    "Ordem" integer NOT NULL,
    "Ativa" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_PerguntasAnamnese" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PerguntasAnamnese_Organizacoes_OrganizacaoId" FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PerguntasAnamnese_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_PerguntasAnamnese_OrganizacaoId_ProfissionalId_Ordem" ON "PerguntasAnamnese" ("OrganizacaoId", "ProfissionalId", "Ordem");

CREATE TABLE IF NOT EXISTS "RespostasAnamnesePersonalizadas" (
    "Id" uuid NOT NULL,
    "AnamneseId" uuid NOT NULL,
    "PerguntaAnamneseId" uuid NOT NULL,
    "Resposta" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_RespostasAnamnesePersonalizadas" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RespostasAnamnesePersonalizadas_Anamneses_AnamneseId" FOREIGN KEY ("AnamneseId") REFERENCES "Anamneses" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RespostasAnamnesePersonalizadas_PerguntasAnamnese_PerguntaAnamneseId" FOREIGN KEY ("PerguntaAnamneseId") REFERENCES "PerguntasAnamnese" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_RespostasAnamnesePersonalizadas_PerguntaAnamneseId" ON "RespostasAnamnesePersonalizadas" ("PerguntaAnamneseId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RespostasAnamnesePersonalizadas_AnamneseId_PerguntaAnamneseId" ON "RespostasAnamnesePersonalizadas" ("AnamneseId", "PerguntaAnamneseId");
