CREATE TABLE IF NOT EXISTS ContaCorrente (
    Id TEXT PRIMARY KEY,
    NumeroConta INTEGER NOT NULL,
    Cpf TEXT NOT NULL UNIQUE,
    Nome TEXT NOT NULL UNIQUE,
    SenhaHash TEXT NOT NULL,
    Ativo INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Movimento (
    Id TEXT PRIMARY KEY,
    ContaCorrenteId TEXT NOT NULL,
    IdentificacaoRequisicao TEXT NOT NULL,
    Valor REAL NOT NULL,
    Tipo TEXT NOT NULL,
    DataMovimento TEXT NOT NULL,
    FOREIGN KEY (ContaCorrenteId) REFERENCES ContaCorrente(Id)
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Movimento_Identificacao
ON Movimento (IdentificacaoRequisicao);

CREATE TABLE IF NOT EXISTS PasswordReset (
    Id TEXT PRIMARY KEY,
    ContaId TEXT NOT NULL,
    TokenHash TEXT NOT NULL,
    Expires TEXT NOT NULL,
    Used INTEGER NOT NULL
);
