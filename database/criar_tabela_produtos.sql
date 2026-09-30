-- =====================================================================
--  Cadastro de Produtos - script de criacao da tabela (SQLite)
--
--  Este e o mesmo script que a aplicacao executa ao iniciar
--  (DatabaseInitializer). Ele e idempotente: pode ser rodado varias
--  vezes sem apagar dados.
--
--  Para rodar manualmente:
--      sqlite3 produtos.db < database/criar_tabela_produtos.sql
-- =====================================================================

CREATE TABLE IF NOT EXISTS Produtos (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome          TEXT    NOT NULL CHECK (length(trim(Nome)) BETWEEN 1 AND 100),
    Preco         REAL    NOT NULL CHECK (Preco >= 0),
    Estoque       INTEGER NOT NULL DEFAULT 0 CHECK (Estoque >= 0),
    Categoria     TEXT    NOT NULL CHECK (length(trim(Categoria)) BETWEEN 1 AND 50),

    -- Soft delete: "Excluir" marca Ativo = 0 em vez de apagar a linha.
    Ativo         INTEGER NOT NULL DEFAULT 1 CHECK (Ativo IN (0, 1)),
    DataCadastro  TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    DataExclusao  TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_Produtos_Ativo ON Produtos (Ativo);
