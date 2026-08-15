BEGIN;

ALTER TABLE "FasesNutricionais"
    ADD COLUMN IF NOT EXISTS "MetaPesoKg" numeric(8,2) NULL,
    ADD COLUMN IF NOT EXISTS "MetaAdesaoPercentual" integer NULL,
    ADD COLUMN IF NOT EXISTS "DuracaoMinimaDias" integer NULL,
    ADD COLUMN IF NOT EXISTS "CriterioTransicao" character varying(1000) NULL;

ALTER TABLE "FasesTreino"
    ADD COLUMN IF NOT EXISTS "MetaPesoKg" numeric(8,2) NULL,
    ADD COLUMN IF NOT EXISTS "MetaAdesaoPercentual" integer NULL,
    ADD COLUMN IF NOT EXISTS "DuracaoMinimaDias" integer NULL,
    ADD COLUMN IF NOT EXISTS "CriterioTransicao" character varying(1000) NULL;

COMMIT;
