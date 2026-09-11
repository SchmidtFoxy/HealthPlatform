CREATE TABLE IF NOT EXISTS "EventosXp" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "Data" date NOT NULL,
    "Fonte" character varying(40) NOT NULL,
    "FonteId" uuid NOT NULL,
    "Pontos" integer NOT NULL,
    "Motivo" character varying(500) NOT NULL,
    "Adequacao" character varying(30) NULL,
    CONSTRAINT "PK_EventosXp" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventosXp_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventosXp_PacienteId_Fonte_FonteId" ON "EventosXp" ("PacienteId", "Fonte", "FonteId");
CREATE INDEX IF NOT EXISTS "IX_EventosXp_OrganizacaoId_Data" ON "EventosXp" ("OrganizacaoId", "Data");
