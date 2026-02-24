# ✅ Testes Unitários - Transfer.API

## 🎯 RESUMO

**Total de Testes:** 12  
**Status:** ✅ 100% passando  
**Cobertura:** Handler de transferências e cache Redis

---

## 📦 Estrutura dos Testes

```
Transfer.Tests/
├── Handlers/
│   └── RealizarTransferenciaHandlerTests.cs  ✅ 6 testes
└── Infrastructure/
    └── RedisCacheTests.cs                    ✅ 6 testes
```

---

## ✅ RealizarTransferenciaHandlerTests (6 testes)

### **Testes Implementados:**

1. ✅ `Handle_DeveRealizarTransferenciaComSucesso`
   - Valida fluxo completo de transferência
   - Verifica débito na origem
   - Verifica crédito no destino
   - Confirma publicação no Kafka

2. ✅ `Handle_DeveRespeitarIdempotencia`
   - Valida que não processa duplicatas
   - Verifica `IdentificacaoRequisicao` única

3. ✅ `Handle_DeveLancarExcecao_QuandoValorInvalido` (2 cenários)
   - Valor zero: `0`
   - Valor negativo: `-10`

4. ✅ `Handle_DeveLancarExcecao_QuandoTokenNaoEncontrado`
   - Valida presença de token JWT
   - Verifica cabeçalho Authorization

5. ✅ `Handle_DevePublicarEventoKafkaComDadosCorretos`
   - Valida evento `transferencias-realizadas`
   - Verifica estrutura do evento

### **Cobertura:**

- ✅ Fluxo completo de transferência
- ✅ Validações de valor
- ✅ Validação de token
- ✅ Idempotência
- ✅ Publicação no Kafka
- ✅ Registro no banco de dados

### **Exemplo de Teste:**

```csharp
[Fact]
public async Task Handle_DeveRealizarTransferenciaComSucesso()
{
    // Arrange
    var command = new RealizarTransferenciaCommand(
        "transfer-123", 54321, 100.00m
    ) { ContaOrigemId = Guid.NewGuid(), ContaOrigemNumero = 12345 };

    _accountApiMock
        .Setup(x => x.RealizarMovimentacaoAsync(..., 'D'))
        .ReturnsAsync(true);

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    _kafkaProducerMock.Verify(
        x => x.PublishAsync("transferencias-realizadas", It.IsAny<object>()), 
        Times.Once
    );
}
```

---

## ✅ RedisCacheTests (6 testes)

### **Testes Implementados:**

1. ✅ `Cache_DeveArmazenarERecuperarSaldo`
   - Valida operações básicas de cache
   - Testa serialização/deserialização

2. ✅ `Cache_DeveRetornarNull_ParaCacheMiss`
   - Valida comportamento quando chave não existe

3. ✅ `Cache_DeveArmazenarComTTL`
   - Valida TTL de 30 segundos
   - Testa `AbsoluteExpirationRelativeToNow`

4. ✅ `Cache_DeveInvalidarChave`
   - Valida remoção de cache
   - Testa método `RemoveAsync`

5. ✅ `Cache_DeveSerializarValidacao`
   - Valida cache de validação de contas
   - Testa serialização de booleanos

6. ✅ `Cache_DeveSuportarMultiplasChaves`
   - Valida isolamento entre chaves
   - Testa múltiplos caches simultâneos

### **Cobertura:**

- ✅ Redis cache básico (GET/SET/REMOVE)
- ✅ TTL (Time-to-Live)
- ✅ Serialização JSON
- ✅ Múltiplas chaves
- ✅ Cache de saldos
- ✅ Cache de validações

### **Por Que Redis em vez de MemoryCache?**

| Característica | MemoryCache | Redis (Distributed) |
|----------------|-------------|---------------------|
| **Escopo** | Por instância | Compartilhado |
| **Escalabilidade** | ❌ Não escala | ✅ Múltiplas instâncias |
| **Persistência** | ❌ Perdido ao reiniciar | ✅ Persistente |
| **Performance** | ⚡ Muito rápido | 🚀 Rápido (network) |
| **Uso** | Account.API | Transfer.API |

---

## 🛠️ Stack de Testes

| Ferramenta | Versão | Propósito |
|------------|--------|-----------|
| **xUnit** | 2.5.3 | Framework de testes |
| **Moq** | 4.20.70 | Mocking |
| **FluentAssertions** | 6.12.0 | Assertions |
| **StackExchangeRedis** | 8.0.0 | Redis (runtime) |

---

## 🚀 Como Rodar os Testes

### **Via CLI:**

```bash
# Todos os testes
dotnet test Transfer.Tests/

# Com detalhes
dotnet test Transfer.Tests/ --verbosity normal

# Apenas handlers
dotnet test Transfer.Tests/ --filter "FullyQualifiedName~Handlers"

# Apenas infrastructure
dotnet test Transfer.Tests/ --filter "FullyQualifiedName~Infrastructure"
```

### **Via Visual Studio:**

```
Test Explorer → Transfer.Tests → Run All
```

---

## 📊 Resultado

```
✅ Test summary: total: 12; failed: 0; succeeded: 12
✅ Build: SUCCESS
✅ Tempo de execução: ~2,4s
```

---

## 🎯 Cobertura Geral

| Componente | Testes | Status |
|------------|--------|--------|
| **RealizarTransferenciaHandler** | 6 | ✅ Completo |
| **Redis Cache** | 6 | ✅ Completo |

### **Cenários Testados:**

✅ **Handler:**
- Transferência bem-sucedida
- Idempotência
- Validação de valor
- Validação de token
- Publicação Kafka

✅ **Cache:**
- GET/SET/REMOVE
- TTL (30 segundos)
- Cache HIT/MISS
- Serialização JSON
- Múltiplas chaves

---

## 📚 Próximos Passos (Opcional)

### **1. Testes de Integração com Redis Real**

```bash
dotnet add package Testcontainers.Redis --version 3.5.0
```

```csharp
public class RedisIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
    }

    [Fact]
    public async Task DeveArmazenarERecuperarDoRedisReal()
    {
        var cache = new RedisCache(new RedisCacheOptions
        {
            Configuration = _redis.GetConnectionString()
        });

        await cache.SetStringAsync("test-key", "test-value");
        var value = await cache.GetStringAsync("test-key");

        value.Should().Be("test-value");
    }
}
```

### **2. Testes de Kafka**

```bash
dotnet add package Testcontainers.Kafka --version 3.5.0
```

### **3. Testes E2E**

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
```

---

## 💡 Conclusão

✅ **12 testes unitários funcionando**  
✅ **Handler principal testado**  
✅ **Lógica de cache Redis testada**  
✅ **Build e execução rápida (~2,4s)**

**Transfer.API pronto com testes de qualidade!** 🚀
