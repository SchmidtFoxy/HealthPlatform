BEGIN;

CREATE TABLE IF NOT EXISTS "MetasPaciente" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "Tipo" character varying(50) NOT NULL,
    "ValorObjetivo" numeric NULL,
    "Unidade" character varying(40) NULL,
    "Frequencia" character varying(30) NOT NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date NULL,
    "Status" character varying(30) NOT NULL,
    "Observacoes" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_MetasPaciente" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MetasPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_MetasPaciente_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_MetasPaciente_PacienteId_Status_DataInicio" ON "MetasPaciente" ("PacienteId", "Status", "DataInicio");
CREATE INDEX IF NOT EXISTS "IX_MetasPaciente_ProfissionalId" ON "MetasPaciente" ("ProfissionalId");

CREATE TABLE IF NOT EXISTS "RegistrosMetas" (
    "Id" uuid NOT NULL,
    "MetaPacienteId" uuid NOT NULL,
    "Data" date NOT NULL,
    "Valor" numeric NULL,
    "Concluida" boolean NULL,
    "Observacao" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_RegistrosMetas" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RegistrosMetas_MetasPaciente_MetaPacienteId" FOREIGN KEY ("MetaPacienteId") REFERENCES "MetasPaciente" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RegistrosMetas_MetaPacienteId_Data" ON "RegistrosMetas" ("MetaPacienteId", "Data");

CREATE TABLE IF NOT EXISTS "RegistrosDiarioPaciente" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "DataHoraUtc" timestamp with time zone NOT NULL,
    "Tipo" character varying(50) NOT NULL,
    "Descricao" text NULL,
    "ValorNumerico" numeric NULL,
    "Unidade" character varying(40) NULL,
    "Escala" integer NULL,
    "ImagemUrl" character varying(1000) NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_RegistrosDiarioPaciente" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RegistrosDiarioPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_RegistrosDiarioPaciente_PacienteId_DataHoraUtc" ON "RegistrosDiarioPaciente" ("PacienteId", "DataHoraUtc");

COMMIT;
