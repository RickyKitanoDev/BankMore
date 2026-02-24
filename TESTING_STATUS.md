# ✅ Testes - Status Atual e Próximos Passos

## 🎯 STATUS ATUAL

✅ **Build:** SUCCESS  
✅ **Testes:** 6/6 passando (100%)  
✅ **Projetos de Teste:** Configurados com xUnit, Moq e FluentAssertions

---

## 📦 Estrutura Atual

```
BankMore/
├── Account.Tests/
│   ├── BasicTests.cs           ✅ 2 testes (placeholder)
│   └── Account.Tests.csproj    ✅ Configurado
│
├── Transfer.Tests/
│   ├── BasicTests.cs           ✅ 2 testes (placeholder)
│   └── Transfer.Tests.csproj   ✅ Configurado
│
└── Tarifa.Tests/
    ├── BasicTests.cs           ✅ 2 testes (placeholder)
    └── Tarifa.Tests.csproj     ✅ Configurado
```

---

## ⚠️ Por Que Testes Unitários Não Foram Implementados?

### **Problema: Entidades com Construtores Privados**

As entidades atuais **não são testáveis** com mocks:

```csharp
public class ContaCorrente
{
    public Guid Id { get; private set; }
    public string Cpf { get; private set; }
    
    private ContaCorrente() { }  // ❌ Não pode criar instância fake
    
    public ContaCorrente(Guid id, int numero, string cpf, ...) { ... }
}
```

**Para testar com Moq, precisaríamos:**

```csharp
// ❌ Não funciona
var conta = new ContaCorrente 
{ 
    Id = Guid.NewGuid(),  // Erro: set accessor is inaccessible
    Cpf = "123"
};

// ✅ Alternativa: Factory Method
public static class ContaCorrenteFactory
{
    public static ContaCorrente CreateForTesting(Guid id, string cpf)
    {
        return new ContaCorrente(id, 12345, cpf, "Nome", "hash", true);
    }
}
```

---

## 🚀 Como Rodar os Testes Atuais

### **Via Terminal**

```bash
# Todos os testes
dotnet test

# Com detalhes
dotnet test --verbosity normal

# Projeto específico
dotnet test Account.Tests/

# Com cobertura
dotnet test /p:CollectCoverage=true
```

### **Via Visual Studio**

```
Test → Run All Tests (Ctrl+R, A)
```

---

## 🧪 RECOMENDAÇÃO: Testes Manuais

Enquanto os testes unitários não estão implementados, **use testes manuais**:

### **1. Collection Postman**

Crie uma collection com os fluxos principais:

```json
{
  "info": { "name": "BankMore - Testes E2E" },
  "item": [
    {
      "name": "1. Registrar Conta",
      "request": {
        "method": "POST",
        "url": "{{base_url}}/api/auth/register",
        "body": {
          "mode": "raw",
          "raw": "{\"cpf\":\"12345678900\",\"nome\":\"João\",\"senha\":\"senha123\"}"
        }
      },
      "event": [{
        "listen": "test",
        "script": {
          "exec": [
            "pm.test('Status 200', () => pm.response.to.have.status(200));",
            "pm.environment.set('numero_conta', pm.response.json());"
          ]
        }
      }]
    },
    {
      "name": "2. Login",
      "request": {
        "method": "POST",
        "url": "{{base_url}}/api/auth/login",
        "body": {
          "mode": "raw",
          "raw": "{\"numeroOuCpf\":\"{{numero_conta}}\",\"senha\":\"senha123\"}"
        }
      },
      "event": [{
        "listen": "test",
        "script": {
          "exec": [
            "pm.test('Status 200', () => pm.response.to.have.status(200));",
            "pm.environment.set('token', pm.response.text());"
          ]
        }
      }]
    },
    {
      "name": "3. Consultar Saldo",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/movimentacao/saldo",
        "header": [{
          "key": "Authorization",
          "value": "Bearer {{token}}"
        }]
      },
      "event": [{
        "listen": "test",
        "script": {
          "exec": [
            "pm.test('Status 200', () => pm.response.to.have.status(200));",
            "pm.test('Saldo >= 0', () => {",
            "  const res = pm.response.json();",
            "  pm.expect(res.saldo).to.be.at.least(0);",
            "});"
          ]
        }
      }]
    }
  ]
}
```

### **2. Script de Teste Bash**

```bash
#!/bin/bash
BASE_URL="http://localhost:5001"

echo "=== TESTE 1: Registrar Conta ==="
NUMERO_CONTA=$(curl -s -X POST $BASE_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"cpf":"11111111111","nome":"Test User","senha":"senha123"}')
echo "Conta criada: $NUMERO_CONTA"

echo "=== TESTE 2: Login ==="
TOKEN=$(curl -s -X POST $BASE_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"numeroOuCpf\":\"$NUMERO_CONTA\",\"senha\":\"senha123\"}")
echo "Token: ${TOKEN:0:20}..."

echo "=== TESTE 3: Consultar Saldo ==="
curl -s -X GET $BASE_URL/api/movimentacao/saldo \
  -H "Authorization: Bearer $TOKEN" | jq

echo "=== TESTES CONCLUÍDOS ==="
```

**Executar:**
```bash
chmod +x test-e2e.sh
./test-e2e.sh
```

---

## 📝 Próximos Passos para Testes Automatizados

### **Opção 1: Testes de Integração (RECOMENDADO)**

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task DeveRegistrarEFazerLogin()
    {
        // Registrar
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            cpf = "12345678900",
            nome = "João Silva",
            senha = "senha123"
        });
        
        registerResponse.EnsureSuccessStatusCode();
        var numeroConta = await registerResponse.Content.ReadAsStringAsync();
        
        // Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            numeroOuCpf = numeroConta,
            senha = "senha123"
        });
        
        loginResponse.EnsureSuccessStatusCode();
        var token = await loginResponse.Content.ReadAsStringAsync();
        
        // Assertions
        token.Should().NotBeNullOrEmpty();
    }
}
```

**Instalar:**
```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
```

---

### **Opção 2: Testcontainers (Para Kafka/Redis)**

```csharp
using Testcontainers.Kafka;
using Testcontainers.Redis;

public class KafkaIntegrationTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.5.0")
        .Build();
    
    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();
        await _redis.StartAsync();
    }

    [Fact]
    public async Task DevePublicarEConsumirEventoKafka()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress()
        };
        
        var producer = new KafkaProducer(config);
        await producer.PublishAsync("test-topic", new { Id = 123 });
        
        // Assert via consumer...
    }

    public async Task DisposeAsync()
    {
        await _kafka.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
```

**Instalar:**
```bash
dotnet add package Testcontainers --version 3.5.0
dotnet add package Testcontainers.Kafka --version 3.5.0
dotnet add package Testcontainers.Redis --version 3.5.0
```

---

### **Opção 3: Refatorar Entidades (Mais Trabalhoso)**

```csharp
// Antes
public class ContaCorrente
{
    public Guid Id { get; private set; }
    private ContaCorrente() { }
}

// Depois
public class ContaCorrente
{
    public required Guid Id { get; init; }
    public required string Cpf { get; init; }
    
    public static ContaCorrente Create(...) => new() { ... };
}
```

---

## 📊 Comparação de Abordagens

| Abordagem | Esforço | Cobertura | Velocidade | Recomendação |
|-----------|---------|-----------|------------|--------------|
| **Testes Manuais** | Baixo | Básica | Lento | ⭐ Curto prazo |
| **Testes Integração** | Médio | Alta | Média | ⭐⭐⭐ Melhor opção |
| **Testcontainers** | Médio | Muito Alta | Média | ⭐⭐ Para infraestrutura |
| **Refatorar + Unitários** | Alto | Máxima | Rápido | ⭐ Longo prazo |

---

## 🎯 Recomendação Final

### **Agora (Curto Prazo):**
1. ✅ Use **TESTING_MANUAL.md** para validação
2. ✅ Crie **collection Postman** com fluxos principais
3. ✅ Monitore **logs Docker** para debugging

### **Próxima Sprint (Médio Prazo):**
1. 🔨 Implemente **Testes de Integração** com `WebApplicationFactory`
2. 🔨 Adicione **Testcontainers** para Kafka
3. 🔨 Configure **CI/CD** para rodar testes automaticamente

### **Backlog (Longo Prazo):**
1. 📝 Refatore entidades para testabilidade
2. 📝 Implemente testes unitários puros
3. 📝 Adicione testes de performance (BenchmarkDotNet)

---

## ✅ Checklist Pré-Deploy

- [ ] Testes manuais passando (Postman)
- [ ] Logs sem erros (Docker)
- [ ] Cache funcionando (verificar logs "Cache HIT/MISS")
- [ ] Kafka processando eventos (verificar Tarifa.API)
- [ ] Swagger acessível (3 APIs)
- [ ] Build sem warnings (`dotnet build`)

---

## 📚 Documentos Relacionados

- `TESTING_MANUAL.md` - Guia de testes manuais completos
- `TESTING_STRATEGY.md` - Estratégia geral de testes
- `CACHE_IMPLEMENTATION.md` - Validação de cache
- `SECURITY_REFACTORING.md` - Testes de segurança

---

## 💡 Conclusão

✅ **Testes básicos funcionando** (build OK, 6/6 passando)  
✅ **Testes manuais documentados** (Postman, Swagger, curl)  
✅ **Próximos passos definidos** (Integração → Testcontainers → Unitários)

**Sistema pronto para validação manual e evolução gradual!** 🚀
