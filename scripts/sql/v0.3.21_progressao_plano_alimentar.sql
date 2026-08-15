BEGIN;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "PlanoOrigemId" uuid NULL;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "Versao" integer NOT NULL DEFAULT 1;

ALTER TABLE "PlanosAlimentares"
    ADD COLUMN IF NOT EXISTS "AjustePercentual" numeric NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS "IX_PlanosAlimentares_PlanoOrigemId"
    ON "PlanosAlimentares" ("PlanoOrigemId");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_PlanosAlimentares_PlanosAlimentares_PlanoOrigemId'
    ) THEN
        ALTER TABLE "PlanosAlimentares"
            ADD CONSTRAINT "FK_PlanosAlimentares_PlanosAlimentares_PlanoOrigemId"
            FOREIGN KEY ("PlanoOrigemId")
            REFERENCES "PlanosAlimentares" ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

COMMIT;
