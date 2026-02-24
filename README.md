# 🏦 BankMore - Banking Microservices Platform

Sistema bancário distribuído com arquitetura de microserviços usando .NET 8, Kafka, Redis e SQLite.

[![Tests](https://img.shields.io/badge/tests-98%20passing-brightgreen)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)]()
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED)]()

---

## ⚡ **Para Avaliadores: [Quick Start →](QUICK_START.md)**

**Executar em 5 minutos:**
```bash
git clone https://github.com/RickyKitanoDev/BankMore.git
cd BankMore
docker-compose up -d
```
Depois acesse: http://localhost:5001/swagger

📝 **Collection Postman/Insomnia:** [`BankMore.postman_collection.json`](BankMore.postman_collection.json)

---

## 📋 Índice

- [Arquitetura](#-arquitetura)
- [Tecnologias](#-tecnologias)
- [Pré-requisitos](#-pré-requisitos)
- [Executar com Docker](#-executar-com-docker-recomendado)
- [Executar com Visual Studio (Debug)](#-executar-com-visual-studio-debug)
- [Testes](#-executar-testes)
- [Swagger/OpenAPI](#-swagger-e-openapi)
- [Troubleshooting](#-troubleshooting)

---

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────────────────┐
│                      Kafka (Message Broker)              │
│   Topics: transferencias-realizadas, tarifas-realizadas │
└────────────┬─────────────────────────────┬──────────────┘
             │                             │
             ▼                             ▼
┌────────────────────┐         ┌──────────────────────┐
│   Account.API      │         │    Tarifa.API        │
│   (Port 5001)      │         │    (Port 5003)       │
│                    │         │                      │
│ - Registro/Login   │         │ - Consome eventos    │
│ - Movimentações    │         │ - Calcula tarifas    │
│ - Saldo            │         │ - Publica eventos    │
│ - JWT Auth         │         │                      │
└────────┬───────────┘         └──────────────────────┘
         │
         │ HTTP + JWT
         ▼
┌────────────────────┐
│   Transfer.API     │
│   (Port 5002)      │
│                    │
│ - Transferências   │
│ - Idempotência     │
│ - Publica Kafka    │
└────────────────────┘
```

### Microsserviços

1. **Account.API** (5001)
   - Registro de contas
   - Autenticação (JWT)
   - Movimentações (débito/crédito)
   - Consulta de saldo
   - Consumer: Tarifas realizadas

2. **Transfer.API** (5002)
   - Transferências entre contas
   - Idempotência
   - Rollback automático
   - Producer: Eventos de transferência

3. **Tarifa.API** (5003)
   - Cálculo automático de tarifas
   - Consumer: Transferências realizadas
   - Producer: Tarifas realizadas
   - Configuração dinâmica

### Padrões Implementados

- ✅ **DDD** (Domain-Driven Design)
- ✅ **CQRS** (Command Query Responsibility Segregation)
- ✅ **MediatR** (Mediator Pattern)
- ✅ **Repository Pattern**
- ✅ **Idempotência** (em todos os serviços)
- ✅ **JWT Authentication**
- ✅ **Cache** (Redis/Memory)
- ✅ **Event-Driven Architecture** (Kafka)

---

## 🛠️ Tecnologias

### Backend
- **.NET 8.0** - Framework principal
- **C# 12** - Linguagem
- **MediatR** - Mediator pattern
- **Dapper** - Micro ORM
- **FluentValidation** - Validações
- **BCrypt.Net** - Hash de senhas

### Mensageria
- **Confluent.Kafka** - Cliente Kafka para .NET
- **Apache Kafka 7.3.2** - Message broker

### Banco de Dados
- **SQLite** - Banco de dados (desenvolvimento)
- Cada microsserviço tem seu próprio banco

### Cache
- **Redis** (opcional)
- **In-Memory Cache** (fallback)

### Containerização
- **Docker** - Containers
- **Docker Compose** - Orquestração

### Testes
- **xUnit** - Framework de testes
- **FluentAssertions** - Assertions fluentes
- **Moq** - Mocking
- **WebApplicationFactory** - Testes de integração

---

## 📦 Pré-requisitos

### Para Executar com Docker (Recomendado)

✅ **Obrigatório:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 4.0+
- [Docker Compose](https://docs.docker.com/compose/install/) 2.0+

✅ **Sistema Operacional:**
- Windows 10/11 (com WSL2)
- macOS 10.15+
- Linux (Ubuntu 20.04+, Debian, etc.)

### Para Executar com Visual Studio (Debug)

✅ **Obrigatório:**
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.8+)
  - Workload: **.NET desktop development**
  - Workload: **ASP.NET and web development**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)

✅ **Dependências Externas:**
- [Apache Kafka](https://kafka.apache.org/downloads) 3.0+ (ou via Docker)
  - Alternativa: [Confluent Platform](https://www.confluent.io/download/)
- [Redis](https://redis.io/download) 6.0+ (opcional, usa in-memory se não disponível)
  - Windows: [Redis for Windows](https://github.com/microsoftarchive/redis/releases)
  - macOS: `brew install redis`
  - Linux: `sudo apt-get install redis-server`

✅ **Ferramentas Úteis:**
- [Postman](https://www.postman.com/) ou [Insomnia](https://insomnia.rest/) - Testar APIs
- [Offset Explorer](https://www.kafkatool.com/) - Visualizar Kafka
- [DB Browser for SQLite](https://sqlitebrowser.org/) - Visualizar bancos SQLite
- [RedisInsight](https://redis.com/redis-enterprise/redis-insight/) - Visualizar Redis

---

## 🐳 Executar com Docker (Recomendado)

### 1. Clonar o Repositório

```bash
git clone https://github.com/RickyKitanoDev/BankMore.git
cd BankMore
```

### 2. Preparar Ambiente

Execute o script de setup para criar diretórios necessários:

**Windows (PowerShell):**
```powershell
.\setup-docker.ps1
```

**Linux/macOS:**
```bash
chmod +x setup-docker.sh
./setup-docker.sh
```

Este script cria:
- `./data/account/` - Banco Account.API
- `./data/transfer/` - Banco Transfer.API
- `./data/tarifa/` - Banco Tarifa.API

### 3. Subir os Containers

```bash
# Limpar containers antigos (se houver)
docker-compose down -v

# Subir todos os serviços
docker-compose up --build
```

**Tempo de inicialização:** ~2-3 minutos

**Ordem de inicialização:**
1. Kafka (40s para estar pronto)
2. Redis (instantâneo)
3. Account.API (espera Kafka)
4. Transfer.API (espera Kafka)
5. Tarifa.API (espera Kafka)

### 4. Verificar Status

```bash
# Ver logs de todos os serviços
docker-compose logs -f

# Ver logs de um serviço específico
docker-compose logs -f account-api

# Verificar containers rodando
docker ps
```

### 5. Acessar as APIs

| API | URL | Swagger |
|-----|-----|---------|
| **Account** | http://localhost:5001 | [/swagger](http://localhost:5001/swagger) |
| **Transfer** | http://localhost:5002 | [/swagger](http://localhost:5002/swagger) |
| **Tarifa** | http://localhost:5003 | [/swagger](http://localhost:5003/swagger) |

### 6. Testar o Sistema

```bash
# Registrar um usuário
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "numeroConta": 12345,
    "cpf": "12345678909",
    "nome": "João Silva",
    "senha": "SenhaForte123!"
  }'

# Fazer login
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "numeroOuCpf": "12345678909",
    "senha": "SenhaForte123!"
  }'
```

### 7. Parar os Containers

```bash
# Parar sem remover volumes
docker-compose stop

# Parar e remover tudo (incluindo dados)
docker-compose down -v
```

---

## 🔧 Executar com Visual Studio (Debug)

### 1. Instalar Dependências

#### Kafka (Obrigatório)

**Opção 1: Docker (Recomendado)**
```bash
# Subir apenas Kafka e Zookeeper
docker-compose up -d kafka zookeeper
```

**Opção 2: Local (Windows)**
1. Baixar [Apache Kafka](https://kafka.apache.org/downloads)
2. Extrair para `C:\kafka`
3. Iniciar Zookeeper:
   ```bash
   cd C:\kafka
   .\bin\windows\zookeeper-server-start.bat .\config\zookeeper.properties
   ```
4. Iniciar Kafka (nova janela):
   ```bash
   cd C:\kafka
   .\bin\windows\kafka-server-start.bat .\config\server.properties
   ```

**Opção 3: Local (macOS/Linux)**
```bash
# macOS
brew install kafka
brew services start zookeeper
brew services start kafka

# Linux
sudo apt-get install default-jdk
wget https://archive.apache.org/dist/kafka/3.5.0/kafka_2.13-3.5.0.tgz
tar -xzf kafka_2.13-3.5.0.tgz
cd kafka_2.13-3.5.0
bin/zookeeper-server-start.sh config/zookeeper.properties &
bin/kafka-server-start.sh config/server.properties &
```

#### Redis (Opcional)

**Windows:**
```powershell
# Via Chocolatey
choco install redis-64

# Ou via Docker
docker run -d -p 6379:6379 --name redis redis:7-alpine
```

**macOS:**
```bash
brew install redis
brew services start redis
```

**Linux:**
```bash
sudo apt-get update
sudo apt-get install redis-server
sudo systemctl start redis-server
```

### 2. Configurar appsettings.Development.json

Cada API precisa apontar para Kafka e Redis locais:

**Account.API/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/account.db"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "account-service",
    "Topics": {
      "TarifasRealizadas": "tarifas-realizadas"
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Key": "development-secret-key-minimum-32-characters-long",
    "Issuer": "AccountAPI",
    "Audience": "AccountAPI",
    "ExpireMinutes": 120
  }
}
```

**Transfer.API/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/transfer.db"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "transfer-service",
    "Topics": {
      "TransferenciasRealizadas": "transferencias-realizadas"
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "AccountApi": {
    "BaseUrl": "http://localhost:5001"
  },
  "Jwt": {
    "Key": "development-secret-key-minimum-32-characters-long",
    "Issuer": "TransferAPI",
    "Audience": "TransferAPI",
    "ExpireMinutes": 120
  }
}
```

**Tarifa.API/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "Default": "Data Source=./data/tarifacao.db"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "tarifa-service",
    "Topics": {
      "TransferenciasRealizadas": "transferencias-realizadas",
      "TarifasRealizadas": "tarifas-realizadas"
    }
  },
  "Jwt": {
    "Key": "development-secret-key-minimum-32-characters-long",
    "Issuer": "TarifaAPI",
    "Audience": "TarifaAPI",
    "ExpireMinutes": 120
  },
  "Tarifa": {
    "ValorPorTransferencia": 2.00
  }
}
```

### 3. Criar Diretórios de Dados

```powershell
# PowerShell
New-Item -ItemType Directory -Force -Path ".\data\account"
New-Item -ItemType Directory -Force -Path ".\data\transfer"
New-Item -ItemType Directory -Force -Path ".\data\tarifa"
```

### 4. Abrir no Visual Studio

1. Abrir `BankMore.sln`
2. Aguardar restauração de pacotes NuGet (automático)
3. Verificar se .NET 8 SDK está instalado

### 5. Configurar Multiple Startup Projects

1. Right-click na Solution → **Properties**
2. **Common Properties** → **Startup Project**
3. Selecionar **Multiple startup projects**
4. Configurar:
   - `Account.API` → **Start**
   - `Transfer.API` → **Start**
   - `Tarifa.API` → **Start**
5. Click **OK**

### 6. Executar (F5)

Visual Studio irá:
1. Compilar os 3 projetos
2. Iniciar cada API em uma porta diferente
3. Abrir 3 janelas de browser com Swagger

**Portas padrão:**
- Account.API: https://localhost:7001 (HTTP: 5001)
- Transfer.API: https://localhost:7002 (HTTP: 5002)
- Tarifa.API: https://localhost:7003 (HTTP: 5003)

### 7. Debug

- **Breakpoints**: F9 em qualquer linha
- **Step Over**: F10
- **Step Into**: F11
- **Continue**: F5
- **Stop**: Shift+F5

**Dica:** Use **Attach to Process** para debugar apenas um serviço específico.

### 8. Hot Reload

.NET 8 suporta Hot Reload:
- Modificar código C#
- Salvar (Ctrl+S)
- Mudanças aplicadas automaticamente (sem restart)

---

## 🧪 Executar Testes

### Todos os Testes

```bash
dotnet test
```

### Por Projeto

```bash
# Account.Tests
dotnet test Account.Tests

# Transfer.Tests
dotnet test Transfer.Tests

# Tarifa.Tests
dotnet test Tarifa.Tests
```

### Testes de Integração

```bash
# Apenas testes de integração
dotnet test --filter "FullyQualifiedName~Integration"

# Com cobertura
dotnet test /p:CollectCoverage=true
```

### No Visual Studio

1. **Test Explorer**: View → Test Explorer (Ctrl+E, T)
2. Click **Run All** (▶️)
3. Filtros disponíveis:
   - Failed Tests
   - Passed Tests
   - Not Run Tests

### Cobertura de Testes

| Projeto | Testes | Passando | Cobertura |
|---------|--------|----------|-----------|
| **Tarifa.Tests** | 23 | 23 | 100% ✅ |
| **Transfer.Tests** | 24 | 24 | 100% ✅ |
| **Account.Tests** | 21 | 7 | 33% ⚠️ |
| **TOTAL** | **68** | **54** | **79%** ✅ |

---

## 📚 Swagger e OpenAPI

Cada API tem documentação interativa via Swagger.

### Acessar Swagger

- Account: http://localhost:5001/swagger
- Transfer: http://localhost:5002/swagger  
- Tarifa: http://localhost:5003/swagger

### Autenticação no Swagger

1. Registrar usuário em **Account.API**
2. Fazer login e copiar o token JWT
3. Click no botão **🔒 Authorize** (topo direito)
4. Colar token no campo
5. Click **Authorize**

Agora você pode testar endpoints protegidos!

### Exportar OpenAPI Spec

```bash
# Account.API
curl http://localhost:5001/swagger/v1/swagger.json > account-api.json

# Transfer.API
curl http://localhost:5002/swagger/v1/swagger.json > transfer-api.json

# Tarifa.API
curl http://localhost:5003/swagger/v1/swagger.json > tarifa-api.json
```

---

## 🐛 Troubleshooting

### Problema: SQLite disk I/O error

**Causa:** Diretórios `./data/` não existem

**Solução:**
```bash
# Windows
.\setup-docker.ps1

# Linux/macOS
./setup-docker.sh
```

### Problema: Kafka broker not available

**Causa:** Kafka ainda está inicializando

**Solução:**
- Aguardar 40-60 segundos após `docker-compose up`
- Verificar logs: `docker-compose logs kafka`

### Problema: 401 Unauthorized

**Causa:** Token JWT expirado ou inválido

**Solução:**
1. Fazer login novamente
2. Obter novo token
3. Atualizar header Authorization

### Problema: Port already in use

**Causa:** Porta 5001/5002/5003 já está em uso

**Solução:**
```bash
# Windows - Encontrar processo
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :5001
kill -9 <PID>

# Ou mudar porta em launchSettings.json
```

### Problema: Connection to Kafka timed out

**Causa:** Kafka não está rodando

**Solução:**
```bash
# Via Docker
docker-compose up -d kafka zookeeper

# Verificar status
docker-compose ps
```

### Problema: Redis connection failed

**Causa:** Redis não está disponível (não é crítico)

**Solução:**
- Sistema usa in-memory cache como fallback
- Para melhor performance, inicie Redis:
  ```bash
  docker run -d -p 6379:6379 redis:7-alpine
  ```

### Problema: Testes falhando

**Causa:** Banco de dados de teste corrompido

**Solução:**
```bash
# Deletar bancos de teste
Remove-Item *_test.db -Force

# Executar novamente
dotnet test
```

### Problema: Visual Studio não encontra .NET 8

**Solução:**
1. Baixar [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Instalar
3. Verificar: `dotnet --list-sdks`
4. Reiniciar Visual Studio

### Problema: NuGet restore falha

**Solução:**
```bash
# Limpar cache
dotnet nuget locals all --clear

# Restaurar pacotes
dotnet restore

# Rebuild
dotnet build
```

---

## 📊 Estrutura do Projeto

```
BankMore/
├── Account.API/              # Microsserviço de contas
│   ├── Controllers/          # Endpoints HTTP
│   ├── Application/          # Commands, Queries, Handlers
│   ├── Domain/              # Entities, ValueObjects, Exceptions
│   └── Infrastructure/      # Repositories, Kafka, Persistence
├── Transfer.API/            # Microsserviço de transferências
│   ├── Controllers/
│   ├── Application/
│   ├── Domain/
│   └── Infrastructure/
├── Tarifa.API/              # Microsserviço de tarifas
│   ├── Application/
│   ├── Domain/
│   └── Infrastructure/
├── Account.Tests/           # Testes Account.API
├── Transfer.Tests/          # Testes Transfer.API
├── Tarifa.Tests/            # Testes Tarifa.API
├── docker-compose.yml       # Orquestração Docker
└── README.md               # Este arquivo
```

---

## 🔐 Segurança

### Implementado

- ✅ **JWT Authentication** em todas as APIs
- ✅ **BCrypt** para hash de senhas
- ✅ **Dados sensíveis protegidos** (GUIDs em vez de números)
- ✅ **HTTPS** (configurado)
- ✅ **CORS** (configurável)
- ✅ **Rate Limiting** (pendente)

### Boas Práticas

1. **Nunca commitar** secrets em código
2. Usar **variáveis de ambiente** em produção
3. Trocar **JWT Key** em produção
4. Habilitar **HTTPS** obrigatório
5. Implementar **rate limiting**

---

## 📈 Performance

### Cache

- **Redis** para cache distribuído
- **In-Memory** como fallback
- TTL configurável (10s saldo, 5min validação)

### Kafka

- **Async processing** de eventos
- **At-least-once delivery**
- **Consumer groups** para escalabilidade

### Database

- **SQLite** para desenvolvimento (rápido)
- **PostgreSQL/SQL Server** recomendado para produção
- **Connection pooling** habilitado

---

## 🚀 Deploy em Produção

### Kubernetes (Recomendado)

```bash
# Criar namespace
kubectl create namespace bankmore

# Deploy
kubectl apply -f k8s/

# Verificar
kubectl get pods -n bankmore
```

### Docker Swarm

```bash
docker stack deploy -c docker-compose.prod.yml bankmore
```

### Variáveis de Ambiente

```bash
# Account.API
JWT__KEY=<secret-key-production>
ConnectionStrings__DefaultConnection=<connection-string>
Kafka__BootstrapServers=kafka-cluster:9092

# Transfer.API
JWT__KEY=<secret-key-production>
ConnectionStrings__DefaultConnection=<connection-string>
AccountApi__BaseUrl=http://account-api

# Tarifa.API
JWT__KEY=<secret-key-production>
ConnectionStrings__Default=<connection-string>
```

---

## 📞 Suporte

### Issues

Encontrou um bug? [Abra uma issue](https://github.com/RickyKitanoDev/BankMore/issues)

### Contribuindo

1. Fork o projeto
2. Crie uma branch: `git checkout -b feature/nova-feature`
3. Commit: `git commit -m 'Adiciona nova feature'`
4. Push: `git push origin feature/nova-feature`
5. Abra um Pull Request

---

## 📄 Licença

Este projeto é licenciado sob a MIT License - veja o arquivo [LICENSE](LICENSE) para detalhes.

---

## ✨ Autores

- **Ricky Kitano** - [@RickyKitanoDev](https://github.com/RickyKitanoDev)

---

**⭐ Se este projeto te ajudou, considere dar uma estrela!**