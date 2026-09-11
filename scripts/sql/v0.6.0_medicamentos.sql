CREATE TABLE IF NOT EXISTS "MedicamentosPaciente" (
    "Id" uuid NOT NULL, "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "ProfissionalId" uuid NOT NULL,
    "Nome" varchar(220) NOT NULL, "Dose" numeric(12,3) NULL, "Unidade" varchar(40) NULL, "Via" varchar(80) NULL,
    "Frequencia" varchar(40) NOT NULL DEFAULT 'Diario', "HorariosLocais" varchar(200) NULL,
    "DataInicio" date NOT NULL, "DataFim" date NULL, "Orientacao" varchar(1600) NULL, "Ativo" boolean NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_MedicamentosPaciente" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MedicamentosPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_MedicamentosPaciente_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_MedicamentosPaciente_OrganizacaoId_PacienteId_Ativo" ON "MedicamentosPaciente" ("OrganizacaoId", "PacienteId", "Ativo");

CREATE TABLE IF NOT EXISTS "RegistrosMedicamentos" (
    "Id" uuid NOT NULL, "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "MedicamentoId" uuid NOT NULL,
    "DataHoraPrevistaUtc" timestamp with time zone NOT NULL, "DataHoraTomadaUtc" timestamp with time zone NULL,
    "Status" varchar(30) NOT NULL, "Observacao" varchar(1000) NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_RegistrosMedicamentos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RegistrosMedicamentos_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_RegistrosMedicamentos_MedicamentosPaciente_MedicamentoId" FOREIGN KEY ("MedicamentoId") REFERENCES "MedicamentosPaciente" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_RegistrosMedicamentos_OrganizacaoId_PacienteId_DataHoraPrevistaUtc" ON "RegistrosMedicamentos" ("OrganizacaoId", "PacienteId", "DataHoraPrevistaUtc");
CREATE INDEX IF NOT EXISTS "IX_RegistrosMedicamentos_MedicamentoId_DataHoraPrevistaUtc" ON "RegistrosMedicamentos" ("MedicamentoId", "DataHoraPrevistaUtc");
