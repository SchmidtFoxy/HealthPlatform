CREATE TABLE IF NOT EXISTS "ProntidoesDiarias" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "Data" date NOT NULL,
    "SonoHoras" numeric(4,2) NOT NULL,
    "SonoQualidade" integer NULL,
    "EnergiaNivel" integer NOT NULL,
    "DorNivel" integer NOT NULL,
    "DisposicaoNivel" integer NOT NULL,
    "RecuperacaoNivel" integer NOT NULL,
    "HorasDesdeUltimoTreino" numeric(6,2) NULL,
    "EsforcoUltimoTreino" integer NULL,
    "Score" integer NOT NULL,
    "RecomendacaoTreino" character varying(30) NOT NULL,
    "MotivoRecomendacao" character varying(1200) NULL,
    "Origem" character varying(30) NOT NULL DEFAULT 'Paciente',
    CONSTRAINT "PK_ProntidoesDiarias" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProntidoesDiarias_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProntidoesDiarias_PacienteId_Data" ON "ProntidoesDiarias" ("PacienteId", "Data");
CREATE INDEX IF NOT EXISTS "IX_ProntidoesDiarias_OrganizacaoId_Data" ON "ProntidoesDiarias" ("OrganizacaoId", "Data");
