CREATE TABLE IF NOT EXISTS Tarifacao (
    Id TEXT PRIMARY KEY,
    ContaId TEXT NOT NULL,
    ValorTarifado REAL NOT NULL,
    DataHoraTarifacao TEXT NOT NULL,
    IdentificacaoTransferencia TEXT NOT NULL UNIQUE
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Tarifacao_Identificacao
ON Tarifacao (IdentificacaoTransferencia);
