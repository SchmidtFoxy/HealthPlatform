BEGIN;

CREATE TABLE IF NOT EXISTS "ExecucoesTreino" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "PlanoTreinoId" uuid NOT NULL,
    "SessaoTreinoId" uuid NOT NULL,
    "DataHoraInicioUtc" timestamp with time zone NOT NULL,
    "DataHoraFimUtc" timestamp with time zone NULL,
    "DuracaoMinutos" integer NULL,
    "EsforcoPercebido" integer NULL,
    "Observacoes" character varying(2000) NULL,
    "Status" character varying(30) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_ExecucoesTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExecucoesTreino_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ExecucoesTreino_PlanosTreino_PlanoTreinoId"
        FOREIGN KEY ("PlanoTreinoId") REFERENCES "PlanosTreino" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ExecucoesTreino_SessoesTreino_SessaoTreinoId"
        FOREIGN KEY ("SessaoTreinoId") REFERENCES "SessoesTreino" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_ExecucoesTreino_PacienteId_DataHoraInicioUtc"
    ON "ExecucoesTreino" ("PacienteId", "DataHoraInicioUtc");
CREATE INDEX IF NOT EXISTS "IX_ExecucoesTreino_SessaoTreinoId"
    ON "ExecucoesTreino" ("SessaoTreinoId");
CREATE INDEX IF NOT EXISTS "IX_ExecucoesTreino_PlanoTreinoId"
    ON "ExecucoesTreino" ("PlanoTreinoId");

CREATE TABLE IF NOT EXISTS "ExecucoesItensTreino" (
    "Id" uuid NOT NULL,
    "ExecucaoTreinoId" uuid NOT NULL,
    "ItemTreinoId" uuid NOT NULL,
    "SeriesRealizadas" integer NULL,
    "RepeticoesRealizadas" character varying(80) NULL,
    "CargaRealizada" numeric NULL,
    "UnidadeCarga" character varying(20) NULL,
    "EsforcoPercebido" integer NULL,
    "Concluido" boolean NOT NULL DEFAULT TRUE,
    "Observacoes" character varying(1000) NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_ExecucoesItensTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExecucoesItensTreino_ExecucoesTreino_ExecucaoTreinoId"
        FOREIGN KEY ("ExecucaoTreinoId") REFERENCES "ExecucoesTreino" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ExecucoesItensTreino_ItensTreino_ItemTreinoId"
        FOREIGN KEY ("ItemTreinoId") REFERENCES "ItensTreino" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_ExecucoesItensTreino_ExecucaoTreinoId"
    ON "ExecucoesItensTreino" ("ExecucaoTreinoId");
CREATE INDEX IF NOT EXISTS "IX_ExecucoesItensTreino_ItemTreinoId"
    ON "ExecucoesItensTreino" ("ItemTreinoId");

COMMIT;
