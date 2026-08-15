-- HealthPlatform v0.1.5 - relatorios clinicos imutaveis/snapshots.
CREATE TABLE IF NOT EXISTS "RelatoriosClinicos" (
    "Id" uuid NOT NULL, "PacienteId" uuid NOT NULL, "ProfissionalId" uuid NOT NULL,
    "DataInicioUtc" timestamp with time zone NULL, "DataFimUtc" timestamp with time zone NULL,
    "DataGeracaoUtc" timestamp with time zone NOT NULL, "Titulo" character varying(220) NOT NULL,
    "ConclusaoMedica" text NULL, "VersaoTemplate" character varying(30) NOT NULL, "ConteudoJson" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_RelatoriosClinicos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RelatoriosClinicos_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RelatoriosClinicos_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_RelatoriosClinicos_PacienteId_DataGeracaoUtc" ON "RelatoriosClinicos" ("PacienteId", "DataGeracaoUtc");
CREATE INDEX IF NOT EXISTS "IX_RelatoriosClinicos_ProfissionalId" ON "RelatoriosClinicos" ("ProfissionalId");
