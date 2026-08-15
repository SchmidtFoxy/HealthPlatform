BEGIN;

CREATE TABLE IF NOT EXISTS "ModelosRefeicoes" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Nome" character varying(160) NOT NULL,
    "Categoria" character varying(80) NULL,
    "Descricao" character varying(600) NULL,
    "ConteudoJson" text NOT NULL,
    "Ativo" boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_ModelosRefeicoes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ModelosRefeicoes_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ModelosRefeicoes_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_ModelosRefeicoes_OrganizacaoId_Ativo_Nome"
    ON "ModelosRefeicoes" ("OrganizacaoId", "Ativo", "Nome");

CREATE INDEX IF NOT EXISTS "IX_ModelosRefeicoes_OrganizacaoId_Categoria"
    ON "ModelosRefeicoes" ("OrganizacaoId", "Categoria");

CREATE INDEX IF NOT EXISTS "IX_ModelosRefeicoes_ProfissionalId"
    ON "ModelosRefeicoes" ("ProfissionalId");

COMMIT;
