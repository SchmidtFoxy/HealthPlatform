BEGIN;

CREATE TABLE IF NOT EXISTS "NotificacoesInternas" (
    "Id" uuid NOT NULL,
    "OrganizacaoId" uuid NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Tipo" character varying(60) NOT NULL,
    "Prioridade" character varying(20) NOT NULL,
    "Titulo" character varying(300) NOT NULL,
    "Mensagem" character varying(2000) NOT NULL,
    "OrigemTipo" character varying(80) NULL,
    "OrigemId" uuid NULL,
    "OrigemChave" character varying(220) NOT NULL,
    "DataEventoUtc" timestamp with time zone NULL,
    "Link" character varying(500) NULL,
    "LidaEmUtc" timestamp with time zone NULL,
    "Ativa" boolean NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_NotificacoesInternas" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_NotificacoesInternas_Organizacoes_OrganizacaoId"
        FOREIGN KEY ("OrganizacaoId") REFERENCES "Organizacoes" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_NotificacoesInternas_OrganizacaoId_UsuarioId_OrigemChave"
    ON "NotificacoesInternas" ("OrganizacaoId", "UsuarioId", "OrigemChave");
CREATE INDEX IF NOT EXISTS "IX_NotificacoesInternas_UsuarioId_Ativa_LidaEmUtc"
    ON "NotificacoesInternas" ("UsuarioId", "Ativa", "LidaEmUtc");
CREATE INDEX IF NOT EXISTS "IX_NotificacoesInternas_UsuarioId_DataEventoUtc"
    ON "NotificacoesInternas" ("UsuarioId", "DataEventoUtc");

COMMIT;
