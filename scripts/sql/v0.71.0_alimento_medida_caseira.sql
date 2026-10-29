-- AESYN 0.71.0: medida caseira e densidade no alimento (prescrever em unidade ou ml).
-- So adiciona tres colunas opcionais em "Alimentos" (nao altera nem apaga nada). Pode rodar mais de uma vez.
BEGIN;

ALTER TABLE "Alimentos" ADD COLUMN IF NOT EXISTS "UnidadeNome" character varying(40) NULL;
ALTER TABLE "Alimentos" ADD COLUMN IF NOT EXISTS "GramasPorUnidade" numeric NULL;
ALTER TABLE "Alimentos" ADD COLUMN IF NOT EXISTS "GramasPorMl" numeric NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261029200000_V0710AlimentoMedidaCaseira', '10.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
