BEGIN;

CREATE TABLE IF NOT EXISTS "Alimentos" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "NomeNormalizado" character varying(180) NOT NULL,
    "Categoria" character varying(100),
    "CaloriasPor100g" numeric NOT NULL,
    "ProteinasPor100g" numeric NOT NULL,
    "CarboidratosPor100g" numeric NOT NULL,
    "GordurasPor100g" numeric NOT NULL,
    "FibrasPor100g" numeric NOT NULL,
    "Ativo" boolean NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_Alimentos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Alimentos_Organizacoes_OrganizacaoId" FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Alimentos_OrganizacaoId_NomeNormalizado" ON "Alimentos" ("OrganizacaoId", "NomeNormalizado");

CREATE TABLE IF NOT EXISTS "PlanosAlimentares" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date,
    "Status" character varying(30) NOT NULL,
    "Observacoes" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_PlanosAlimentares" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PlanosAlimentares_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PlanosAlimentares_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_PlanosAlimentares_PacienteId_DataInicio" ON "PlanosAlimentares" ("PacienteId", "DataInicio");
CREATE INDEX IF NOT EXISTS "IX_PlanosAlimentares_ProfissionalId" ON "PlanosAlimentares" ("ProfissionalId");

CREATE TABLE IF NOT EXISTS "RefeicoesPlanoAlimentar" (
    "Id" uuid NOT NULL,
    "PlanoAlimentarId" uuid NOT NULL,
    "Nome" character varying(120) NOT NULL,
    "Horario" time without time zone,
    "Ordem" integer NOT NULL,
    "Observacoes" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_RefeicoesPlanoAlimentar" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefeicoesPlanoAlimentar_PlanosAlimentares_PlanoAlimentarId" FOREIGN KEY ("PlanoAlimentarId") REFERENCES "PlanosAlimentares" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_RefeicoesPlanoAlimentar_PlanoAlimentarId_Ordem" ON "RefeicoesPlanoAlimentar" ("PlanoAlimentarId", "Ordem");

CREATE TABLE IF NOT EXISTS "ItensRefeicaoPlano" (
    "Id" uuid NOT NULL,
    "RefeicaoPlanoAlimentarId" uuid NOT NULL,
    "AlimentoId" uuid NOT NULL,
    "Quantidade" numeric NOT NULL,
    "Unidade" character varying(40) NOT NULL,
    "QuantidadeGramas" numeric NOT NULL,
    "Observacao" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_ItensRefeicaoPlano" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ItensRefeicaoPlano_RefeicoesPlanoAlimentar_RefeicaoPlanoAlimentarId" FOREIGN KEY ("RefeicaoPlanoAlimentarId") REFERENCES "RefeicoesPlanoAlimentar" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ItensRefeicaoPlano_Alimentos_AlimentoId" FOREIGN KEY ("AlimentoId") REFERENCES "Alimentos" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_ItensRefeicaoPlano_RefeicaoPlanoAlimentarId" ON "ItensRefeicaoPlano" ("RefeicaoPlanoAlimentarId");
CREATE INDEX IF NOT EXISTS "IX_ItensRefeicaoPlano_AlimentoId" ON "ItensRefeicaoPlano" ("AlimentoId");

CREATE TABLE IF NOT EXISTS "SubstituicoesItensRefeicao" (
    "Id" uuid NOT NULL,
    "ItemRefeicaoPlanoId" uuid NOT NULL,
    "AlimentoId" uuid NOT NULL,
    "Quantidade" numeric NOT NULL,
    "Unidade" character varying(40) NOT NULL,
    "QuantidadeGramas" numeric NOT NULL,
    "Observacao" text,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_SubstituicoesItensRefeicao" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SubstituicoesItensRefeicao_ItensRefeicaoPlano_ItemRefeicaoPlanoId" FOREIGN KEY ("ItemRefeicaoPlanoId") REFERENCES "ItensRefeicaoPlano" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SubstituicoesItensRefeicao_Alimentos_AlimentoId" FOREIGN KEY ("AlimentoId") REFERENCES "Alimentos" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_SubstituicoesItensRefeicao_ItemRefeicaoPlanoId" ON "SubstituicoesItensRefeicao" ("ItemRefeicaoPlanoId");
CREATE INDEX IF NOT EXISTS "IX_SubstituicoesItensRefeicao_AlimentoId" ON "SubstituicoesItensRefeicao" ("AlimentoId");

COMMIT;
