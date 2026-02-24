# 🧪 Testes de Integração - Tarifa API

## 📋 Visão Geral

Este documento descreve os testes de integração criados para a **Tarifa API**. Os testes de integração complementam os testes unitários existentes, validando a integração entre componentes e o comportamento real da aplicação com banco de dados.

## ✨ O que foi Criado

### 1. **Infraestrutura de Testes** (`TarifaWebApplicationFactory.cs`)
Classe base que configura o ambiente de teste usando `WebApplicationFactory<Program>`:
- ✅ Configuração de banco de dados de teste (SQLite)
- ✅ Remoção do Kafka Consumer Service (para testes isolados)
- ✅ Configurações de teste personalizadas
- ✅ Métodos utilitários para inicialização e limpeza do banco

### 2. **Testes de Repositório** (`TarifacaoRepositoryIntegrationTests.cs`)
Testa operações do repositório com banco de dados real:
- ✅ Adicionar tarifação no banco de dados
- ✅ Verificar existência por identificação
- ✅ Múltiplas inserções
- ✅ Persistência correta de todos os campos

### 3. **Testes de Handler** (`ProcessarTarifaHandlerIntegrationTests.cs`)
Testa o processamento completo de tarifas via MediatR:
- ✅ Processamento de tarifa com comando válido
- ✅ Garantia de idempotência (processar múltiplas vezes a mesma tarifa)
- ✅ Processamento de múltiplas tarifas diferentes
- ✅ Processamento para diferentes contas

### 4. **Testes de Persistência** (`DbInitializerIntegrationTests.cs`)
Testa a inicialização do banco de dados:
- ✅ Criação da tabela Tarifacao
- ✅ Criação de índices
- ✅ Idempotência da inicialização
- ✅ Estrutura correta das colunas

### 5. **Testes de Configuração** (`TarifaConfigurationIntegrationTests.cs`)
Testa a configuração de tarifa:
- ✅ Registro como Singleton
- ✅ Carregamento do valor padrão
- ✅ Resolução via Dependency Injection
- ✅ Consistência entre múltiplas resoluções

### 6. **Testes End-to-End** (`TarifaEndToEndIntegrationTests.cs`)
Testa fluxos completos da aplicação:
- ✅ Fluxo completo de processamento de tarifa
- ✅ Processamento de múltiplas tarifas (10 tarifas)
- ✅ Idempotência em cenários reais (5 processamentos da mesma tarifa)
- ✅ Processamento simultâneo (20 tarifas em paralelo)
- ✅ Diferentes valores de transferência
- ✅ Múltiplas tarifas para a mesma conta

## 📊 Cobertura de Testes

### Componentes Testados
- ✅ `TarifacaoRepository`
- ✅ `ProcessarTarifaHandler`
- ✅ `DbInitializer`
- ✅ `TarifaConfiguration`
- ✅ Integração MediatR
- ✅ Integração com banco de dados SQLite

### Cenários Cobertos
- ✅ Happy path (casos de sucesso)
- ✅ Idempotência
- ✅ Concorrência/Paralelismo
- ✅ Múltiplas operações
- ✅ Diferentes valores e parâmetros
- ✅ Persistência de dados
- ✅ Configuração e DI

## 🚀 Como Executar

### Executar todos os testes de integração:
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration"
```

### Executar testes por categoria:

**Repositórios:**
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Repositories"
```

**Handlers:**
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Handlers"
```

**Persistência:**
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Persistence"
```

**Configuração:**
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Configuration"
```

**End-to-End:**
```bash
dotnet test Tarifa.Tests --filter "TarifaEndToEndIntegrationTests"
```

### Executar com relatório de cobertura:
```bash
dotnet test Tarifa.Tests --collect:"XPlat Code Coverage"
```

### Executar testes em modo verbose:
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration" --logger "console;verbosity=detailed"
```

## 📦 Pacotes Adicionados

Foi adicionado ao `Tarifa.Tests.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
```

Este pacote fornece o `WebApplicationFactory` necessário para testes de integração.

## 🏗️ Estrutura de Diretórios

```
Tarifa.Tests/
├── Integration/
│   ├── Configuration/
│   │   └── TarifaConfigurationIntegrationTests.cs
│   ├── Handlers/
│   │   └── ProcessarTarifaHandlerIntegrationTests.cs
│   ├── Persistence/
│   │   └── DbInitializerIntegrationTests.cs
│   ├── Repositories/
│   │   └── TarifacaoRepositoryIntegrationTests.cs
│   ├── TarifaEndToEndIntegrationTests.cs
│   ├── TarifaWebApplicationFactory.cs
│   └── README.md
├── Configuration/
├── Handlers/
├── Services/
└── Tarifa.Tests.csproj
```

## 🎯 Características dos Testes

### Boas Práticas Implementadas

1. **Isolamento** ✅
   - Cada teste é independente
   - Banco de dados limpo após cada teste
   - Uso de `IAsyncLifetime` para setup/teardown

2. **AAA Pattern** ✅
   - Arrange: Preparação dos dados
   - Act: Execução da ação
   - Assert: Verificação dos resultados

3. **Nomenclatura Clara** ✅
   - Padrão: `Method_Should_When`
   - Exemplo: `Handle_DeveProcessarTarifa_QuandoComandoValido`

4. **Assertions Fluentes** ✅
   - Uso de FluentAssertions
   - Mensagens descritivas
   - Melhor legibilidade

5. **Fixtures** ✅
   - `IClassFixture<TarifaWebApplicationFactory>`
   - Compartilhamento eficiente de setup

6. **Async/Await** ✅
   - Todos os testes são assíncronos
   - Simula comportamento real

## ⚠️ Observações Importantes

### Kafka Consumer
O `TarifaConsumerService` é desabilitado nos testes de integração para:
- Evitar dependências externas (Kafka)
- Manter testes isolados e rápidos
- Focar no comportamento da aplicação

### Banco de Dados
- Usa SQLite para testes
- Banco separado: `tarifa_test.db`
- Limpo automaticamente após cada teste

### Performance
Os testes de integração são mais lentos que os unitários porque:
- Interagem com banco de dados real
- Inicializam a aplicação completa
- Testam fluxos end-to-end

Recomendação: Execute separadamente dos testes unitários.

## 📈 Resultados Esperados

Ao executar os testes, você deve ver:
- ✅ **32+ testes** passando
- ✅ Cobertura de todos os componentes principais
- ✅ Validação de cenários de sucesso e edge cases
- ✅ Garantia de idempotência
- ✅ Validação de concorrência

## 🔧 Troubleshooting

### Erro: "Database file locked"
**Solução:** Certifique-se de que nenhuma outra instância está usando o arquivo de teste.

### Erro: "Table already exists"
**Solução:** O `DbInitializer` deve ser idempotente. Verifique a limpeza entre testes.

### Testes lentos
**Solução:** Normal para testes de integração. Execute separadamente dos unitários.

## 📚 Próximos Passos

Para expandir os testes de integração, considere adicionar:
- [ ] Testes com Kafka (usando TestContainers)
- [ ] Testes de stress/load
- [ ] Testes de falhas e recuperação
- [ ] Testes de migrações de banco de dados
- [ ] Testes de métricas e logging

## 🤝 Contribuindo

Ao adicionar novos recursos:
1. Adicione testes de integração correspondentes
2. Mantenha a nomenclatura consistente
3. Documente cenários específicos
4. Verifique a cobertura de código

---

**Autor:** GitHub Copilot  
**Data:** 2024  
**Versão:** 1.0
