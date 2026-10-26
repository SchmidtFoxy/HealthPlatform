-- AESYN 0.65.9: exercicios extras no treino do paciente.
-- So cria uma tabela nova (nao altera nem apaga nada). Pode rodar mais de uma vez.
BEGIN;

CREATE TABLE IF NOT EXISTS "ExecucoesExerciciosExtras" (
    "Id" uuid NOT NULL,
    "ExecucaoTreinoId" uuid NOT NULL,
    "ExercicioId" uuid NOT NULL,
    "Ordem" integer NOT NULL,
    "SeriesRealizadas" integer NULL,
    "RepeticoesRealizadas" character varying(80) NULL,
    "CargaRealizada" numeric NULL,
    "UnidadeCarga" character varying(20) NULL,
    "Observacoes" character varying(500) NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_ExecucoesExerciciosExtras" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExecucoesExerciciosExtras_ExecucoesTreino_ExecucaoTreinoId" FOREIGN KEY ("ExecucaoTreinoId") REFERENCES "ExecucoesTreino" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ExecucoesExerciciosExtras_Exercicios_ExercicioId" FOREIGN KEY ("ExercicioId") REFERENCES "Exercicios" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_ExecucoesExerciciosExtras_ExecucaoTreinoId" ON "ExecucoesExerciciosExtras" ("ExecucaoTreinoId");
CREATE INDEX IF NOT EXISTS "IX_ExecucoesExerciciosExtras_ExercicioId" ON "ExecucoesExerciciosExtras" ("ExercicioId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261026220000_V0659TreinoExerciciosExtras', '10.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
