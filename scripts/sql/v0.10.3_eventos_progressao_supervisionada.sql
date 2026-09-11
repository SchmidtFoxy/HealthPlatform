CREATE TABLE IF NOT EXISTS "EventosProgressaoSupervisionada" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "CicloEsportivoPacienteId" uuid NULL,
    "Eixo" character varying(40) NOT NULL,
    "Descricao" character varying(1000) NOT NULL,
    "DataAplicacaoUtc" timestamp with time zone NOT NULL,
    "Status" character varying(30) NOT NULL DEFAULT 'EmObservacao',
    "Observacoes" character varying(1600) NULL,
    "EncerradoEmUtc" timestamp with time zone NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_EventosProgressaoSupervisionada" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventosProgressaoSupervisionada_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EventosProgressaoSupervisionada_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EventosProgressaoSupervisionada_Ciclos_CicloId" FOREIGN KEY ("CicloEsportivoPacienteId") REFERENCES "CiclosEsportivosPaciente" ("Id") ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS "IX_EventosProgressaoSupervisionada_PacienteId_DataAplicacaoUtc" ON "EventosProgressaoSupervisionada" ("PacienteId", "DataAplicacaoUtc");
CREATE INDEX IF NOT EXISTS "IX_EventosProgressaoSupervisionada_OrganizacaoId_Status" ON "EventosProgressaoSupervisionada" ("OrganizacaoId", "Status");
