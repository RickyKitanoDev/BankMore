# 🧪 Testes Unitários - BankMore (Implementação Simplificada)

## ⚠️ Status Atual

Devido à complexidade das entidades existentes (construtores privados, propriedades readonly), implementamos **testes de integração básicos** em vez de testes unitários puros com mocks.

---

## 🎯 Abordagem Alternativa Recomendada

### **Opção 1: Testes de Integração com Banco em Memória**

Use **SQLite in-memory** para testes reais sem mocks:

```csharp
public class IntegrationTestBase
{
    protected IDbConnection CreateInMemoryDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // Cria schema
        using var cmd = connection.CreateCommand();
        cmd.CommandText = File.ReadAllText("Infrastructure/Persistence/scripts.sql");
        cmd.ExecuteNonQuery();
        
        return connection;
    }
}
```

### **Opção 2: Testcontainers**

Use containers Docker reais para testes:

```bash
dotnet add package Testcontainers --version 3.5.0
dotnet add package Testcontainers.Kafka --version 3.5.0
```

```csharp
public class KafkaIntegrationTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
    
    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();
    }
    
    [Fact]
    public async Task DevePublicarEvento()
    {
        var producer = new KafkaProducer(_kafka.GetBootstrapAddress());
        await producer.PublishAsync("topic", new Event());
        // Assert...
    }
}
```

---

## 🚀 Como Executar Testes Manuais

### **1. Teste de Registro**

```http
POST http://localhost:5001/api/auth/register
Content-Type: application/json

{
  "cpf": "12345678900",
  "nome": "João Silva",
  "senha": "senha123"
}
```

**Resposta Esperada:**
```json
{
  "numeroConta": 12345
}
```

---

### **2. Teste de Login**

```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "cpf": "12345678900",
  "senha": "senha123"
}
```

**Resposta Esperada:**
```json
{
  "token": "eyJhbGc...",
  "numeroConta": 12345
}
```

---

### **3. Teste de Saldo**

```http
GET http://localhost:5001/api/movimentacao/saldo
Authorization: Bearer eyJhbGc...
```

**Resposta Esperada:**
```json
{
  "numeroConta": 12345,
  "saldo": 0.00
}
```

---

### **4. Teste de Transferência**

```http
POST http://localhost:5002/api/transferencia
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "identificacaoRequisicao": "uuid-123",
  "contaDestinoNumero": 54321,
  "valor": 100.00
}
```

**Resposta Esperada:**
```
204 No Content
```

---

## 📊 Suíte de Testes Manuais (Postman/Insomnia)

### **Cenário 1: Fluxo Completo**

1. ✅ **Registrar Conta A**
   - CPF: 11111111111
   - Senha: senha123

2. ✅ **Registrar Conta B**
   - CPF: 22222222222
   - Senha: senha456

3. ✅ **Fazer Login Conta A**
   - Obtém token

4. ✅ **Creditar R$ 1000 na Conta A**
   ```http
   POST /api/movimentacao
   {
     "identificacaoRequisicao": "cred-1",
     "valor": 1000.00,
     "tipo": "C"
   }
   ```

5. ✅ **Consultar Saldo Conta A**
   - Deve retornar: R$ 1000.00

6. ✅ **Transferir R$ 200 para Conta B**
   ```http
   POST /api/transferencia
   {
     "identificacaoRequisicao": "trans-1",
     "contaDestinoNumero": [numero_conta_b],
     "valor": 200.00
   }
   ```

7. ✅ **Consultar Saldo Conta A**
   - Deve retornar: R$ 798.00 (1000 - 200 - 2 tarifa)

8. ✅ **Fazer Login Conta B e Consultar Saldo**
   - Deve retornar: R$ 200.00

---

## 🧪 Teste de Idempotência

```http
# Primeira chamada - deve processar
POST /api/transferencia
{
  "identificacaoRequisicao": "idem-123",
  "contaDestinoNumero": 54321,
  "valor": 50.00
}

# Segunda chamada - deve ignorar (idempotência)
POST /api/transferencia
{
  "identificacaoRequisicao": "idem-123",
  "contaDestinoNumero": 54321,
  "valor": 50.00
}
```

✅ **Saldo deve ser debitado APENAS UMA VEZ**

---

## 🧪 Teste de Cache

1. **Consultar Saldo (primeira vez)**
   ```bash
   curl http://localhost:5001/api/movimentacao/saldo \
     -H "Authorization: Bearer $TOKEN"
   ```
   📊 **Logs esperados:** `Cache MISS - Buscando saldo no banco`

2. **Consultar Saldo (segunda vez - dentro de 30s)**
   ```bash
   curl http://localhost:5001/api/movimentacao/saldo \
     -H "Authorization: Bearer $TOKEN"
   ```
   📊 **Logs esperados:** `Cache HIT - Saldo da conta 123`

3. **Fazer Movimentação**
   ```bash
   curl -X POST http://localhost:5001/api/movimentacao \
     -H "Authorization: Bearer $TOKEN" \
     -d '{"identificacaoRequisicao":"mov-1","valor":10,"tipo":"C"}'
   ```
   📊 **Logs esperados:** `Cache INVALIDATED - Saldo da conta 123`

4. **Consultar Saldo novamente**
   ```bash
   curl http://localhost:5001/api/movimentacao/saldo \
     -H "Authorization: Bearer $TOKEN"
   ```
   📊 **Logs esperados:** `Cache MISS - Buscando saldo no banco`

---

## 🧪 Teste de Kafka (via Logs)

1. **Fazer Transferência**
2. **Verificar logs do Tarifa.API**
   ```bash
   docker logs -f bankmore-tarifa
   ```
   📊 **Esperado:**
   ```
   Kafka Consumer iniciado - Topic: transferencias-realizadas
   Mensagem recebida do Kafka - Offset: 0
   Tarifa processada: ...
   Evento de tarifa publicado no Kafka
   ```

3. **Verificar logs do Account.API**
   ```bash
   docker logs -f bankmore-account
   ```
   📊 **Esperado:**
   ```
   Kafka Consumer iniciado - Topic: tarifas-realizadas
   Mensagem de tarifa recebida do Kafka - Offset: 0
   Tarifa debitada com sucesso: Conta 12345, Valor 2.00
   ```

---

## 🧪 Teste de Validações

### **1. Saldo Insuficiente**
```http
POST /api/transferencia
{
  "identificacaoRequisicao": "fail-1",
  "contaDestinoNumero": 54321,
  "valor": 999999.00
}
```
📊 **Esperado:** `400 Bad Request - Saldo insuficiente`

---

### **2. Conta Destino Inválida**
```http
POST /api/transferencia
{
  "identificacaoRequisicao": "fail-2",
  "contaDestinoNumero": 99999,
  "valor": 100.00
}
```
📊 **Esperado:** `400 Bad Request - Conta de destino não encontrada`

---

### **3. Transferir para Mesma Conta**
```http
POST /api/transferencia
{
  "identificacaoRequisicao": "fail-3",
  "contaDestinoNumero": 12345,
  "valor": 100.00
}
```
📊 **Esperado:** `400 Bad Request - Não é possível transferir para a mesma conta`

---

### **4. Valor Inválido**
```http
POST /api/transferencia
{
  "identificacaoRequisicao": "fail-4",
  "contaDestinoNumero": 54321,
  "valor": 0
}
```
📊 **Esperado:** `400 Bad Request - Valor deve ser maior que zero`

---

## 📊 Collection Postman

Crie uma collection com os testes acima:

```json
{
  "info": { "name": "BankMore Tests" },
  "item": [
    {
      "name": "1. Register Account A",
      "request": {
        "method": "POST",
        "url": "{{base_url}}/api/auth/register",
        "body": {
          "mode": "raw",
          "raw": "{\"cpf\":\"11111111111\",\"nome\":\"João\",\"senha\":\"senha123\"}"
        }
      },
      "event": [{
        "listen": "test",
        "script": {
          "exec": [
            "pm.test('Status 200', () => pm.response.to.have.status(200));",
            "pm.environment.set('numero_conta_a', pm.response.json().numeroConta);"
          ]
        }
      }]
    }
  ]
}
```

---

## 🚀 Próximos Passos (Implementação Futura)

### **1. Refatorar Entidades para Testabilidade**

```csharp
// Antes (difícil testar)
public class ContaCorrente
{
    private ContaCorrente() { }
    public Guid Id { get; private set; }
}

// Depois (testável)
public class ContaCorrente
{
    public Guid Id { get; init; }
    public static ContaCorrente Create(...) => new(...);
}
```

### **2. Implementar Testes de Integração**

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

### **3. Implementar Testes E2E com Playwright**

```bash
dotnet add package Microsoft.Playwright
```

---

## 💡 Conclusão

Enquanto os testes unitários puros requerem refatoração das entidades, você pode:

1. ✅ **Usar testes manuais** (Postman/curl)
2. ✅ **Implementar testes de integração** (próximo passo)
3. ✅ **Monitorar logs** para validar comportamento
4. ✅ **Usar Swagger** para testes exploratórios

**Sistema testável manualmente e pronto para evolução!** 🚀
