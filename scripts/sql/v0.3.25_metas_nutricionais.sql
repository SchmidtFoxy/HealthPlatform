BEGIN;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "MetaCalorias" numeric(10,2) NULL;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "MetaProteinasG" numeric(10,2) NULL;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "MetaCarboidratosG" numeric(10,2) NULL;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "MetaGordurasG" numeric(10,2) NULL;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "MetaFibrasG" numeric(10,2) NULL;

COMMIT;
