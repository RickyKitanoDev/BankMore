# 🚀 Quick Start - BankMore

## Para Avaliadores

Este guia permite executar o projeto BankMore em **menos de 5 minutos**.

---

## ⚡ Início Rápido

### Pré-requisito

- ✅ Docker Desktop instalado e rodando

### Executar

```bash
# 1. Clonar repositório
git clone https://github.com/RickyKitanoDev/BankMore.git
cd BankMore

# 2. Subir todos os serviços
docker-compose up -d

# 3. Aguardar ~2 minutos para tudo inicializar
```

### Acessar

- **Account API:** http://localhost:5001/swagger
- **Transfer API:** http://localhost:5002/swagger  
- **Tarifa API:** http://localhost:5003/swagger

---

## 📝 Fluxo de Teste Completo

### 1. Registrar Usuário

**POST** `http://localhost:5001/api/auth/register`

```json
{
  "numeroConta": 12345,
  "cpf": "12345678909",
  "nome": "João Silva",
  "senha": "SenhaForte123!"
}
```

**Resposta:** `201 Created`

---

### 2. Fazer Login

**POST** `http://localhost:5001/api/auth/login`

```json
{
  "numeroOuCpf": "12345",
  "senha": "SenhaForte123!"
}
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

💡 **Copie o token** - será usado nas próximas requisições

---

### 3. Adicionar Saldo (Crédito)

**POST** `http://localhost:5001/api/movimentacao`

**Headers:**
```
Authorization: Bearer {SEU_TOKEN}
Content-Type: application/json
```

**Body:**
```json
{
  "identificacaoRequisicao": "cred-001",
  "valor": 1000.00,
  "tipo": "C"
}
```

**Resposta:** `204 No Content`

---

### 4. Consultar Saldo

**GET** `http://localhost:5001/api/movimentacao/saldo`

**Headers:**
```
Authorization: Bearer {SEU_TOKEN}
```

**Resposta:**
```json
{
  "saldo": 1000.00
}
```

---

### 5. Registrar Segundo Usuário (Destino)

**POST** `http://localhost:5001/api/auth/register`

```json
{
  "numeroConta": 56789,
  "cpf": "98765432100",
  "nome": "Maria Santos",
  "senha": "OutraSenha123!"
}
```

---

### 6. Realizar Transferência

**POST** `http://localhost:5002/api/transferencia`

**Headers:**
```
Authorization: Bearer {TOKEN_DO_JOAO}
Content-Type: application/json
```

**Body:**
```json
{
  "identificacaoRequisicao": "transf-001",
  "contaDestinoNumero": 56789,
  "valor": 100.00
}
```

**Resposta:** `204 No Content`

**O que acontece:**
1. ✅ Débito de R$ 100 na conta do João (12345)
2. ✅ Crédito de R$ 100 na conta da Maria (56789)
3. ✅ Evento publicado no Kafka
4. ✅ Tarifa de R$ 2 calculada automaticamente
5. ✅ Débito da tarifa na conta do João

---

### 7. Verificar Saldo Final

**GET** `http://localhost:5001/api/movimentacao/saldo`

**Headers:**
```
Authorization: Bearer {TOKEN_DO_JOAO}
```

**Resposta:**
```json
{
  "saldo": 898.00
}
```

💡 **Cálculo:** 1000 - 100 (transferência) - 2 (tarifa) = R$ 898

---

## 🔍 Verificar Logs

```bash
# Ver logs de todos os serviços
docker-compose logs -f

# Ver apenas logs de transferências
docker-compose logs -f transfer-api

# Ver apenas logs de tarifas
docker-compose logs -f tarifa-api
```

**Eventos esperados nos logs:**
```
transfer-api  | Transferência concluída com sucesso: transf-001
tarifa-api    | Processando tarifa para transferência: transf-001
tarifa-api    | Tarifa de R$ 2.00 aplicada com sucesso
account-api   | Débito de tarifa realizado: R$ 2.00
```

---

## 🗄️ Verificar Banco de Dados

Os bancos SQLite ficam em `./data/`:

```bash
# Verificar transferências
sqlite3 ./data/transfer.db "SELECT * FROM Transferencia;"

# Verificar tarifas
sqlite3 ./data/tarifacao.db "SELECT * FROM Tarifacao;"

# Verificar movimentações
sqlite3 ./data/bankmore.db "SELECT * FROM Movimento ORDER BY DataMovimento DESC LIMIT 10;"
```

---

## 📊 Arquitetura do Fluxo

```
Cliente
  │
  ├─► POST /api/auth/register  ───► Account.API (cria conta)
  │
  ├─► POST /api/auth/login     ───► Account.API (retorna JWT)
  │
  ├─► POST /api/movimentacao   ───► Account.API (crédito R$ 1000)
  │
  └─► POST /api/transferencia  ───► Transfer.API
                                     │
                                     ├─► Account.API (débito origem)
                                     ├─► Account.API (crédito destino)
                                     └─► Kafka (publica evento)
                                           │
                                           └─► Tarifa.API (consome evento)
                                                 │
                                                 ├─► Calcula tarifa (R$ 2)
                                                 └─► Kafka (publica tarifa)
                                                       │
                                                       └─► Account.API (debita tarifa)
```

---

## 🧪 Executar Testes

```bash
# Apenas testes unitários (rápido - 5s)
dotnet test --filter "FullyQualifiedName!~Integration"

# Todos os testes
dotnet test
```

**Resultado esperado:**
```
Test Run Successful.
Total tests: 63
     Passed: 63
```

---

## ⚠️ Troubleshooting

### Erro: "port is already allocated"

```bash
# Parar outros containers usando as portas
docker-compose down
docker ps -a | grep 5001  # Verificar se há algo na porta
```

### Erro: "Cannot connect to Kafka"

```bash
# Kafka demora ~40s para inicializar
# Espere um pouco mais ou reinicie:
docker-compose restart transfer-api tarifa-api
```

### Erro: "Cannot connect to Redis"

Redis é **opcional**. Se não estiver disponível, usa cache em memória.

### Limpar Tudo e Recomeçar

```bash
# Remove containers, volumes e imagens
docker-compose down -v
docker system prune -a

# Recria tudo
docker-compose up --build -d
```

---

## 🛑 Parar Serviços

```bash
# Parar (mantém volumes/dados)
docker-compose stop

# Parar e remover (apaga dados)
docker-compose down -v
```

---

## 📚 Documentação Completa

Para mais detalhes, consulte:
- **README.md** - Documentação completa
- **CLEANUP_SUMMARY.md** - Log de mudanças recentes
- `/swagger` - Documentação interativa das APIs

---

## ✅ Checklist para Avaliação

- [ ] Clonar repositório
- [ ] Executar `docker-compose up -d`
- [ ] Acessar Swagger: http://localhost:5001/swagger
- [ ] Registrar usuário
- [ ] Fazer login (copiar token)
- [ ] Adicionar saldo
- [ ] Criar segundo usuário
- [ ] Realizar transferência
- [ ] Verificar saldo final (deve ter desconto da tarifa)
- [ ] Verificar logs: `docker-compose logs -f`
- [ ] Executar testes: `dotnet test --filter "FullyQualifiedName!~Integration"`

---

**🎉 Pronto! Sistema BankMore funcionando perfeitamente!**

Tempo estimado: **5 minutos** ⏱️
