# 🧪 Testes de Integração - Account API

## 📊 Status Atual

```
✅ PRONTOS: 20+ testes HTTP de integração
🎯 ABORDAGEM: Testes via HttpClient (API completa)
🚀 PADRÃO: Mesmo padrão de qualidade do Tarifa.API
✅ COMPILANDO: Sem erros
```

## 📁 Estrutura

```
Account.Tests/Integration/
├── Auth/
│   ├── RegisterHttpIntegrationTests.cs       ✅ 5 testes
│   └── LoginHttpIntegrationTests.cs          ✅ 5 testes
├── Movimentacao/
│   └── MovimentacaoHttpIntegrationTests.cs   ✅ 7 testes
├── AccountEndToEndHttpTests.cs               ✅ 5 testes
├── AccountWebApplicationFactory.cs           ✅ Infraestrutura
└── README.md
```

## 🎯 Testes Criados

### 1. RegisterHttpIntegrationTests (5 testes)
- ✅ `Register_ComDadosValidos_DeveRetornar200`
- ✅ `Register_ComCpfDuplicado_DeveRetornar400`
- ✅ `Register_ComNumerContaDuplicado_DeveRetornar400`
- ✅ `Register_ComDadosInvalidos_DeveRetornar400`
- ✅ `Register_MultiplasContas_DeveExecutarComSucesso`

**Cobertura:**
- Registro bem-sucedido via HTTP
- Validação de CPF duplicado
- Validação de número de conta duplicado
- Validação de dados obrigatórios
- Registro simultâneo de múltiplas contas

### 2. LoginHttpIntegrationTests (5 testes)
- ✅ `Login_ComCredenciaisValidas_DeveRetornarToken`
- ✅ `Login_ComSenhaIncorreta_DeveRetornar401`
- ✅ `Login_ComCpfInexistente_DeveRetornar401`
- ✅ `Login_PorNumeroConta_DeveRetornarToken`
- ✅ `Login_MultipleUsuariosSimultaneos_DeveExecutarComSucesso`

**Cobertura:**
- Login bem-sucedido (retorna JWT)
- Rejeição de senha incorreta
- Rejeição de usuário inexistente
- Login por CPF ou número de conta
- Login simultâneo de múltiplos usuários

### 3. MovimentacaoHttpIntegrationTests (7 testes)
- ✅ `RealizarMovimentacao_Credito_DeveRetornar200`
- ✅ `RealizarMovimentacao_Debito_ComSaldoSuficiente_DeveRetornar200`
- ✅ `RealizarMovimentacao_Debito_ComSaldoInsuficiente_DeveRetornar400`
- ✅ `RealizarMovimentacao_SemAutenticacao_DeveRetornar403`
- ✅ `ObterSaldo_DeveRetornarSaldoCorreto`
- ✅ `FluxoCompleto_MultiplasMovimentacoes_DeveMantserSaldoCorreto`

**Cobertura:**
- Crédito em conta
- Débito com saldo suficiente
- Rejeição de débito com saldo insuficiente
- Autenticação JWT obrigatória
- Consulta de saldo
- Múltiplas movimentações com consistência

### 4. AccountEndToEndHttpTests (5 testes)
- ✅ `FluxoCompleto_RegistroLoginMovimentacaoSaldo_DeveExecutarComSucesso`
- ✅ `FluxoCompleto_InativarContaERejetarMovimentacao_DeveExecutarComSucesso`
- ✅ `FluxoCompleto_MultiplasContasIndependentes_DeveExecutarComSucesso`
- ✅ `FluxoCompleto_SequenciaDeOperacoes_DeveMantserConsistencia`
- ✅ `FluxoCompleto_CacheDeveSerInvalidadoAposMovimentacao`

**Cobertura:**
- Fluxo completo: Registro → Login → Movimentação → Saldo
- Inativação de conta
- Múltiplas contas independentes
- Sequência complexa de operações
- Validação de cache (invalidação)

## 🔧 Abordagem: Testes HTTP

### Por que HTTP e não MediatR direto?

✅ **Prós da abordagem HTTP:**
1. Testa a API **completa** (controllers, middleware, autenticação)
2. Não precisa conhecer estrutura interna dos commands
3. Simula uso real da API
4. Testa serialização/desserialização JSON
5. Valida autenticação JWT
6. Testa status codes HTTP corretos

⚠️ **Contras:**
1. Mais lento que testes unitários de MediatR
2. Menos isolado (testa mais componentes)

### Quando usar cada abordagem?

| Tipo | Quando Usar |
|------|-------------|
| **HTTP Tests** | ✅ Testes de integração (API completa) |
| **MediatR Tests** | ✅ Testes unitários (handlers isolados) |
| **Híbrido** | ⭐ Ideal: HTTP para E2E + MediatR para unidade |

## 🚀 Como Executar

### Todos os testes de integração
```bash
dotnet test Account.Tests --filter "FullyQualifiedName~Integration"
```

### Por categoria
```bash
# Auth
dotnet test Account.Tests --filter "FullyQualifiedName~Integration.Auth"

# Movimentação
dotnet test Account.Tests --filter "MovimentacaoHttpIntegrationTests"

# End-to-End
dotnet test Account.Tests --filter "AccountEndToEndHttpTests"
```

### Teste específico
```bash
dotnet test Account.Tests --filter "Register_ComDadosValidos_DeveRetornar200"
```

## 📦 Infraestrutura

### AccountWebApplicationFactory
```csharp
- ✅ Mock de Kafka Consumer (desabilitado)
- ✅ Banco SQLite de teste
- ✅ JWT com chave de teste
- ✅ Memory Cache configurado
- ✅ Limpeza entre testes
```

### Características
1. **Isolamento** - Cada teste limpa o banco
2. **HttpClient** - Testa via requisições HTTP reais
3. **JWT Real** - Autenticação funcionando
4. **FluentAssertions** - Assertions expressivas
5. **AAA Pattern** - Arrange, Act, Assert

## ✅ Cenários Cobertos

### Casos de Sucesso
- ✅ Registro de conta
- ✅ Login (CPF e número de conta)
- ✅ Movimentações (crédito/débito)
- ✅ Consulta de saldo
- ✅ Inativação de conta
- ✅ Fluxos completos

### Casos de Erro
- ✅ CPF/Conta duplicado
- ✅ Senha incorreta
- ✅ Saldo insuficiente
- ✅ Sem autenticação (403)
- ✅ Dados inválidos

### Casos Especiais
- ✅ Múltiplas contas simultâneas
- ✅ Sequência de operações
- ✅ Cache de saldo
- ✅ Invalidação de cache

## 🎓 Diferença para Tarifa.API

| Aspecto | Tarifa.API | Account.API |
|---------|------------|-------------|
| Abordagem | MediatR direto | HTTP Client |
| Autenticação | Não tem | ✅ JWT obrigatório |
| Complexidade | Simples | Mais complexo |
| Testes | 23 testes | 22 testes |
| Status | ✅ Passando | ✅ Prontos |

## 💡 Próximos Passos (Opcional)

- [ ] Adicionar testes de MediatR (handlers isolados)
- [ ] Testes de Repositories
- [ ] Testes de Persistence (DbInitializer)
- [ ] Testes de Performance
- [ ] Testes com TestContainers (Kafka real)

---

**Status:** ✅ **COMPLETO E PRONTO PARA EXECUÇÃO**  
**Versão:** .NET 8.0  
**Abordagem:** HTTP Integration Tests

