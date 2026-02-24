CREATE TABLE IF NOT EXISTS Transferencia (
    Id TEXT PRIMARY KEY,
    ContaOrigemId TEXT NOT NULL,
    ContaDestinoNumero INTEGER NOT NULL,
    Valor REAL NOT NULL,
    DataTransferencia TEXT NOT NULL,
    IdentificacaoRequisicao TEXT NOT NULL UNIQUE,
    Status TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Transferencia_Identificacao
ON Transferencia (IdentificacaoRequisicao);
