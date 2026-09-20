BEGIN;

CREATE TABLE IF NOT EXISTS "ProgramasTreinoModelo" (
    "Id" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "OrganizacaoId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "Objetivo" character varying(160) NULL,
    "Descricao" character varying(1200) NULL,
    "ConteudoJson" text NOT NULL,
    "Ativo" boolean NOT NULL DEFAULT TRUE,
    CONSTRAINT "PK_ProgramasTreinoModelo" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProgramasTreinoModelo_Organizacoes_OrganizacaoId" FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ProgramasTreinoModelo_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_ProgramasTreinoModelo_OrganizacaoId_Ativo_Nome" ON "ProgramasTreinoModelo" ("OrganizacaoId", "Ativo", "Nome");
CREATE INDEX IF NOT EXISTS "IX_ProgramasTreinoModelo_OrganizacaoId_Objetivo" ON "ProgramasTreinoModelo" ("OrganizacaoId", "Objetivo");
CREATE INDEX IF NOT EXISTS "IX_ProgramasTreinoModelo_ProfissionalId" ON "ProgramasTreinoModelo" ("ProfissionalId");
COMMIT;
