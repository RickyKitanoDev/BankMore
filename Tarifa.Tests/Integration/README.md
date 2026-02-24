# Testes de Integração - Tarifa API

Este diretório contém os testes de integração para a aplicação Tarifa API.

## Estrutura dos Testes

### 📁 Integration/
Contém todos os testes de integração organizados por domínio:

#### Configuration/
- **TarifaConfigurationIntegrationTests.cs**: Testa a configuração e injeção de dependência da TarifaConfiguration como Singleton

#### Handlers/
- **ProcessarTarifaHandlerIntegrationTests.cs**: Testa o processamento completo de tarifas incluindo:
  - Processamento de tarifas válidas
  - Garantia de idempotência
  - Processamento de múltiplas tarifas
  - Integração com repositório

#### Persistence/
- **DbInitializerIntegrationTests.cs**: Testa a inicialização do banco de dados incluindo:
  - Criação de tabelas
  - Criação de índices
  - Idempotência da inicialização
  - Estrutura de colunas

#### Repositories/
- **TarifacaoRepositoryIntegrationTests.cs**: Testa operações do repositório com banco de dados real:
  - Inserção de tarifações
  - Verificação de existência por identificação
  - Múltiplas inserções
  - Persistência de dados

#### End-to-End/
- **TarifaEndToEndIntegrationTests.cs**: Testa fluxos completos da aplicação:
  - Fluxo completo de processamento de tarifa
  - Processamento de múltiplas tarifas
  - Processamento simultâneo
  - Idempotência em cenários reais
  - Diferentes valores e contas

## Infraestrutura de Testes

### TarifaWebApplicationFactory
Classe base que configura o ambiente de teste usando `WebApplicationFactory`:
- Configura banco de dados de teste (SQLite in-memory)
- Remove o Kafka Consumer Service para testes isolados
- Configura variáveis de ambiente de teste
- Fornece métodos utilitários para inicialização e limpeza

## Como Executar

### Executar todos os testes de integração:
```bash
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration"
```

### Executar testes específicos por categoria:
```bash
# Repositórios
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Repositories"

# Handlers
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Handlers"

# Persistence
dotnet test Tarifa.Tests --filter "FullyQualifiedName~Integration.Persistence"

# End-to-End
dotnet test Tarifa.Tests --filter "TarifaEndToEndIntegrationTests"
```

### Executar com relatório de cobertura:
```bash
dotnet test Tarifa.Tests --collect:"XPlat Code Coverage"
```

## Características dos Testes

### ✅ Boas Práticas Implementadas

1. **Isolamento**: Cada teste é independente e limpa o banco após execução
2. **AAA Pattern**: Arrange, Act, Assert bem definidos
3. **Nomenclatura Clara**: Nomes descritivos seguindo padrão `Method_Should_When`
4. **Assertions Fluentes**: Uso de FluentAssertions para melhor legibilidade
5. **Fixtures**: Uso de `IClassFixture` para compartilhar setup entre testes
6. **Async/Await**: Testes assíncronos para simular comportamento real
7. **Limpeza**: Implementação de `IAsyncLifetime` para setup e teardown

### 🎯 Cobertura de Cenários

- ✅ Casos de sucesso (happy path)
- ✅ Idempotência
- ✅ Concorrência
- ✅ Múltiplas operações
- ✅ Diferentes valores e parâmetros
- ✅ Integração entre componentes
- ✅ Persistência de dados

## Dependências

Os testes de integração utilizam:
- **xUnit**: Framework de testes
- **FluentAssertions**: Assertions fluentes
- **Microsoft.AspNetCore.Mvc.Testing**: WebApplicationFactory para testes de integração
- **SQLite**: Banco de dados em memória para testes
- **Dapper**: Acesso a dados

## Notas Importantes

⚠️ **Kafka**: O Kafka Consumer Service é desabilitado nos testes de integração para evitar dependências externas.

⚠️ **Banco de Dados**: Cada execução de teste usa um banco SQLite isolado que é limpo após cada teste.

⚠️ **Performance**: Testes de integração são mais lentos que unitários. Execute-os separadamente quando necessário.

## Manutenção

Ao adicionar novos recursos à API, considere:
1. Adicionar testes de integração correspondentes
2. Testar a integração entre componentes
3. Verificar cenários de erro e edge cases
4. Manter a nomenclatura e estrutura consistentes
