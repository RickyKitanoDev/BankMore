-- Drop old table if exists and recreate with new schema
DROP TABLE IF EXISTS Transferencia;

CREATE TABLE Transferencia (
    Id TEXT PRIMARY KEY,
    ContaOrigemId TEXT NOT NULL,
    ContaDestinoId TEXT NOT NULL,
    Valor REAL NOT NULL,
    DataTransferencia TEXT NOT NULL,
    IdentificacaoRequisicao TEXT NOT NULL UNIQUE,
    Status TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Transferencia_Identificacao
ON Transferencia (IdentificacaoRequisicao);
