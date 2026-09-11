CREATE TABLE IF NOT EXISTS "CiclosEsportivosPaciente" (
    "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "ProfissionalId" uuid NOT NULL,
    "FaseTreinoId" uuid NULL, "FaseNutricionalId" uuid NULL, "Nome" character varying(160) NOT NULL,
    "PerfilEsportivo" character varying(50) NOT NULL, "Objetivo" character varying(800) NULL,
    "DataInicio" date NOT NULL, "DataFim" date NOT NULL, "Status" character varying(30) NOT NULL,
    "MetaTreinosSemanais" integer NULL, "MetaConsistenciaPercentual" integer NULL, "MetaPesoKg" numeric(8,2) NULL,
    "Observacoes" character varying(1200) NULL, CONSTRAINT "PK_CiclosEsportivosPaciente" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CiclosEsportivosPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CiclosEsportivosPaciente_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CiclosEsportivosPaciente_FasesTreino_FaseTreinoId" FOREIGN KEY ("FaseTreinoId") REFERENCES "FasesTreino" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CiclosEsportivosPaciente_FasesNutricionais_FaseNutricionalId" FOREIGN KEY ("FaseNutricionalId") REFERENCES "FasesNutricionais" ("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_PacienteId_Periodo" ON "CiclosEsportivosPaciente" ("PacienteId", "DataInicio", "DataFim");
CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_OrganizacaoId_Status" ON "CiclosEsportivosPaciente" ("OrganizacaoId", "Status");
CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_ProfissionalId" ON "CiclosEsportivosPaciente" ("ProfissionalId");
CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_FaseTreinoId" ON "CiclosEsportivosPaciente" ("FaseTreinoId");
CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_FaseNutricionalId" ON "CiclosEsportivosPaciente" ("FaseNutricionalId");
