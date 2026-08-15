BEGIN;

CREATE TABLE IF NOT EXISTS "Exercicios" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "GrupoMuscular" character varying(100) NULL,
    "Equipamento" character varying(120) NULL,
    "Descricao" text NULL,
    "VideoUrl" character varying(1000) NULL,
    "Ativo" boolean NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_Exercicios" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Exercicios_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_Exercicios_OrganizacaoId_Nome"
    ON "Exercicios" ("OrganizacaoId", "Nome");

CREATE TABLE IF NOT EXISTS "PlanosTreino" (
    "Id" uuid NOT NULL,
    "PacienteId" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Nome" character varying(180) NOT NULL,
    "Objetivo" character varying(500) NULL,
    "DataInicio" date NOT NULL,
    "DataFim" date NULL,
    "Status" character varying(30) NOT NULL,
    "Observacoes" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_PlanosTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PlanosTreino_Pacientes_PacienteId"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PlanosTreino_Profissionais_ProfissionalId"
        FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_PlanosTreino_PacienteId_Status_DataInicio"
    ON "PlanosTreino" ("PacienteId", "Status", "DataInicio");
CREATE INDEX IF NOT EXISTS "IX_PlanosTreino_ProfissionalId"
    ON "PlanosTreino" ("ProfissionalId");

CREATE TABLE IF NOT EXISTS "SessoesTreino" (
    "Id" uuid NOT NULL,
    "PlanoTreinoId" uuid NOT NULL,
    "Nome" character varying(120) NOT NULL,
    "DiasSemana" character varying(120) NULL,
    "Ordem" integer NOT NULL,
    "Observacoes" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_SessoesTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessoesTreino_PlanosTreino_PlanoTreinoId"
        FOREIGN KEY ("PlanoTreinoId") REFERENCES "PlanosTreino" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_SessoesTreino_PlanoTreinoId_Ordem"
    ON "SessoesTreino" ("PlanoTreinoId", "Ordem");

CREATE TABLE IF NOT EXISTS "ItensTreino" (
    "Id" uuid NOT NULL,
    "SessaoTreinoId" uuid NOT NULL,
    "ExercicioId" uuid NOT NULL,
    "Ordem" integer NOT NULL,
    "Series" integer NOT NULL,
    "Repeticoes" character varying(50) NOT NULL,
    "Carga" numeric NULL,
    "UnidadeCarga" character varying(20) NULL,
    "DescansoSegundos" integer NULL,
    "TempoSegundos" integer NULL,
    "Observacoes" text NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_ItensTreino" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ItensTreino_SessoesTreino_SessaoTreinoId"
        FOREIGN KEY ("SessaoTreinoId") REFERENCES "SessoesTreino" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ItensTreino_Exercicios_ExercicioId"
        FOREIGN KEY ("ExercicioId") REFERENCES "Exercicios" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_ItensTreino_SessaoTreinoId_Ordem"
    ON "ItensTreino" ("SessaoTreinoId", "Ordem");
CREATE INDEX IF NOT EXISTS "IX_ItensTreino_ExercicioId"
    ON "ItensTreino" ("ExercicioId");

-- Catálogo inicial idempotente para cada organização já existente.
INSERT INTO "Exercicios"
    ("Id","OrganizacaoId","Nome","GrupoMuscular","Equipamento","Descricao","VideoUrl","Ativo","CreatedAtUtc","UpdatedAtUtc")
SELECT gen_random_uuid(), o."Id", seed.nome, seed.grupo, seed.equipamento, seed.descricao, NULL, TRUE, NOW(), NULL
FROM "Organizacoes" o
CROSS JOIN (
    VALUES
      ('Agachamento livre','Pernas','Barra','Agachamento com barra, respeitando amplitude e técnica orientadas pelo profissional.'),
      ('Supino reto','Peito','Barra ou halteres','Pressão horizontal para peitoral, tríceps e deltoide anterior.'),
      ('Remada curvada','Costas','Barra','Remada para dorsais e musculatura das costas com tronco inclinado.'),
      ('Desenvolvimento de ombros','Ombros','Halteres','Pressão vertical para deltoides e tríceps.'),
      ('Levantamento terra','Posterior / Costas','Barra','Movimento multiarticular com ênfase em cadeia posterior.'),
      ('Prancha abdominal','Core','Peso corporal','Isometria de tronco para estabilidade do core.')
) AS seed(nome, grupo, equipamento, descricao)
WHERE NOT EXISTS (
    SELECT 1 FROM "Exercicios" e
    WHERE e."OrganizacaoId" = o."Id" AND LOWER(e."Nome") = LOWER(seed.nome)
);

COMMIT;
