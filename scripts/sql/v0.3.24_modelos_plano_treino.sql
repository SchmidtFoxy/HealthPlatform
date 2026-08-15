BEGIN;

CREATE TABLE IF NOT EXISTS "ModelosPlanosTreino" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "Descricao" character varying(600) NULL,
    "ConteudoJson" text NOT NULL,
    "Ativo" boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_ModelosPlanosTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ModelosPlanosTreino_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ModelosPlanosTreino_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_ModelosPlanosTreino_OrganizacaoId_Ativo_Nome"
    ON "ModelosPlanosTreino" ("OrganizacaoId", "Ativo", "Nome");

CREATE INDEX IF NOT EXISTS "IX_ModelosPlanosTreino_ProfissionalId"
    ON "ModelosPlanosTreino" ("ProfissionalId");

COMMIT;
