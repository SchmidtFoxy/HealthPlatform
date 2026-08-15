BEGIN;

ALTER TABLE "PlanosTreino" ADD COLUMN IF NOT EXISTS "PlanoOrigemId" uuid NULL;
ALTER TABLE "PlanosTreino" ADD COLUMN IF NOT EXISTS "Versao" integer NOT NULL DEFAULT 1;
ALTER TABLE "PlanosTreino" ADD COLUMN IF NOT EXISTS "AjusteCargaPercentual" numeric NOT NULL DEFAULT 0;
ALTER TABLE "PlanosTreino" ADD COLUMN IF NOT EXISTS "AjusteSeries" integer NOT NULL DEFAULT 0;
ALTER TABLE "PlanosTreino" ADD COLUMN IF NOT EXISTS "AjusteRepeticoes" integer NOT NULL DEFAULT 0;
ALTER TABLE "PlanosTreino" ADD COLUMN IF NOT EXISTS "AjusteDescansoSegundos" integer NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS "IX_PlanosTreino_PlanoOrigemId"
    ON "PlanosTreino" ("PlanoOrigemId");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_PlanosTreino_PlanosTreino_PlanoOrigemId'
    ) THEN
        ALTER TABLE "PlanosTreino"
            ADD CONSTRAINT "FK_PlanosTreino_PlanosTreino_PlanoOrigemId"
            FOREIGN KEY ("PlanoOrigemId")
            REFERENCES "PlanosTreino" ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

COMMIT;
