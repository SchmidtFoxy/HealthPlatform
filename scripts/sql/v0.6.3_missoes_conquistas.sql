CREATE TABLE IF NOT EXISTS "DesafiosSemanaisPaciente" (
    "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "SemanaInicio" date NOT NULL,
    "Codigo" character varying(60) NOT NULL, "Titulo" character varying(160) NOT NULL, "Descricao" character varying(500) NOT NULL,
    "TipoMetrica" character varying(40) NOT NULL, "Meta" integer NOT NULL, "Progresso" integer NOT NULL, "RecompensaXp" integer NOT NULL,
    "ConcluidoEmUtc" timestamp with time zone NULL, CONSTRAINT "PK_DesafiosSemanaisPaciente" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DesafiosSemanaisPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DesafiosSemanaisPaciente_PacienteId_SemanaInicio_Codigo" ON "DesafiosSemanaisPaciente" ("PacienteId", "SemanaInicio", "Codigo");
CREATE INDEX IF NOT EXISTS "IX_DesafiosSemanaisPaciente_OrganizacaoId_SemanaInicio" ON "DesafiosSemanaisPaciente" ("OrganizacaoId", "SemanaInicio");

CREATE TABLE IF NOT EXISTS "ConquistasPaciente" (
    "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "Codigo" character varying(60) NOT NULL,
    "Titulo" character varying(160) NOT NULL, "Descricao" character varying(500) NOT NULL, "Icone" character varying(20) NOT NULL,
    "DataConquista" date NOT NULL, "RecompensaXp" integer NOT NULL, CONSTRAINT "PK_ConquistasPaciente" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ConquistasPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConquistasPaciente_PacienteId_Codigo" ON "ConquistasPaciente" ("PacienteId", "Codigo");
CREATE INDEX IF NOT EXISTS "IX_ConquistasPaciente_OrganizacaoId_DataConquista" ON "ConquistasPaciente" ("OrganizacaoId", "DataConquista");
