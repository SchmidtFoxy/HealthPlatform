-- AESYN 0.72.6: alimentos favoritos por refeicao ("montar pela meta") e alimentos sugeridos pelo medico.
-- Cria uma tabela nova e vazia e acrescenta 9 alimentos em cada clinica que ainda nao os tenha (compara pelo nome).
-- Nao altera nem apaga nada. Pode rodar mais de uma vez.
BEGIN;

CREATE TABLE IF NOT EXISTS "AlimentosFavoritos" (
    "Id" uuid NOT NULL,
    "ProfissionalId" uuid NOT NULL,
    "Refeicao" character varying(80) NOT NULL,
    "AlimentoId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_AlimentosFavoritos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AlimentosFavoritos_Alimentos_AlimentoId" FOREIGN KEY ("AlimentoId") REFERENCES "Alimentos" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AlimentosFavoritos_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_AlimentosFavoritos_AlimentoId" ON "AlimentosFavoritos" ("AlimentoId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AlimentosFavoritos_ProfissionalId_Refeicao_AlimentoId" ON "AlimentosFavoritos" ("ProfissionalId", "Refeicao", "AlimentoId");

-- Valores por 100 g. Tapioca, cafe e maca pela TACO; canela pela USDA; whey pelos rotulos (variam um pouco por sabor).
INSERT INTO "Alimentos" ("Id", "OrganizacaoId", "Nome", "NomeNormalizado", "Categoria", "CaloriasPor100g", "ProteinasPor100g", "CarboidratosPor100g", "GordurasPor100g", "FibrasPor100g", "UnidadeNome", "GramasPorUnidade", "GramasPorMl", "Ativo", "CreatedAtUtc")
SELECT gen_random_uuid(), o."Id", b.nome, b.normalizado, b.categoria, b.kcal, b.prot, b.carb, b.gord, b.fibra, b.medida, b.gramas, b.ml, true, now()
FROM "Organizacoes" o
CROSS JOIN (VALUES
    ('Tapioca (goma hidratada)', 'TAPIOCA (GOMA HIDRATADA)', 'Cereais e derivados', 240, 0, 60, 0, 0, 'colher de sopa', 15, NULL::numeric),
    ('Café preto (sem açúcar)', 'CAFE PRETO (SEM ACUCAR)', 'Bebidas', 9, 0.7, 1.5, 0.1, 0, 'xícara de café', 50, 1),
    ('Maçã Fuji (com casca)', 'MACA FUJI (COM CASCA)', 'Frutas', 56, 0.3, 15.2, 0, 1.3, 'unidade média', 130, NULL),
    ('Canela em pó', 'CANELA EM PO', 'Temperos', 247, 4, 80.6, 1.2, 53.1, 'colher de chá', 2.6, NULL),
    ('Whey protein concentrado (genérico)', 'WHEY PROTEIN CONCENTRADO (GENERICO)', 'Suplementos', 400, 72, 15, 6, 0, 'dose', 30, NULL),
    ('Whey Growth Concentrado 80%', 'WHEY GROWTH CONCENTRADO 80%', 'Suplementos', 403, 76.7, 9.3, 6.7, 0, 'dose', 30, NULL),
    ('Whey New Millen 100% Whey', 'WHEY NEW MILLEN 100% WHEY', 'Suplementos', 407, 70, 20, 5, 0, 'dose', 30, NULL),
    ('Whey Max Titanium Top Whey 3W', 'WHEY MAX TITANIUM TOP WHEY 3W', 'Suplementos', 425, 80, 11, 6.8, 0, 'dose', 40, NULL),
    ('Whey Integralmédica 100% Pure', 'WHEY INTEGRALMEDICA 100% PURE', 'Suplementos', 407, 70, 17, 6.3, 0, 'dose', 30, NULL)
) AS b(nome, normalizado, categoria, kcal, prot, carb, gord, fibra, medida, gramas, ml)
ON CONFLICT ("OrganizacaoId", "NomeNormalizado") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261030010000_V0726AlimentosFavoritos', '10.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
