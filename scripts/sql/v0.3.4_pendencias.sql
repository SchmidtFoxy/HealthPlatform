BEGIN;

CREATE TABLE IF NOT EXISTS "PendenciasClinicas" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "OrigemCodigo" character varying(100) NULL,
    "Categoria" character varying(80) NOT NULL,
    "Severidade" character varying(20) NOT NULL,
    "Titulo" character varying(300) NOT NULL,
    "Descricao" character varying(3000) NULL,
    "ValorReferencia" character varying(300) NULL,
    "AcaoSugerida" character varying(2000) NULL,
    "Status" character varying(30) NOT NULL,
    "VencimentoUtc" timestamp with time zone NULL,
    "VistaEmUtc" timestamp with time zone NULL,
    "AdiadaAteUtc" timestamp with time zone NULL,
    "ResolvidaEmUtc" timestamp with time zone NULL,
    "Resolucao" character varying(2000) NULL,
    "ConsultaRetornoId" uuid NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_PendenciasClinicas" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PendenciasClinicas_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PendenciasClinicas_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PendenciasClinicas_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PendenciasClinicas_Consultas_ConsultaRetornoId"
        FOREIGN KEY ("ConsultaRetornoId") REFERENCES "Consultas" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_PendenciasClinicas_OrganizacaoId_Status_VencimentoUtc"
    ON "PendenciasClinicas" ("OrganizacaoId", "Status", "VencimentoUtc");
CREATE INDEX IF NOT EXISTS "IX_PendenciasClinicas_PacienteId_Status"
    ON "PendenciasClinicas" ("PacienteId", "Status");
CREATE INDEX IF NOT EXISTS "IX_PendenciasClinicas_PacienteId_OrigemCodigo"
    ON "PendenciasClinicas" ("PacienteId", "OrigemCodigo");
CREATE INDEX IF NOT EXISTS "IX_PendenciasClinicas_ProfissionalId"
    ON "PendenciasClinicas" ("ProfissionalId");
CREATE INDEX IF NOT EXISTS "IX_PendenciasClinicas_ConsultaRetornoId"
    ON "PendenciasClinicas" ("ConsultaRetornoId");

COMMIT;
