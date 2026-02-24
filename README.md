# BankMore - Banking Microservices Platform

Sistema bancário distribuído com microserviços .NET 8, Kafka e SQLite.

## 🚀 Quick Start com Docker

### Passo 1: Preparar ambiente

**Windows (PowerShell):**
```powershell
.\setup-docker.ps1
```

**Linux/macOS:**
```bash
chmod +x setup-docker.sh
./setup-docker.sh
```

### Passo 2: Subir os containers

```bash
docker-compose down
docker-compose up --build
```

### Passo 3: Acessar as APIs

- **Account.API**: http://localhost:5001/swagger
- **Transfer.API**: http://localhost:5002/swagger
- **Tarifa.API**: http://localhost:5003/swagger

---

## 🐛 Troubleshooting

### Erro: SQLite disk I/O error
**Solução**: Execute o script de setup antes de subir o Docker
```bash
.\setup-docker.ps1  # Windows
./setup-docker.sh   # Linux/macOS
```

### Erro: Kafka broker not available
**Solução**: Aguarde ~40s após docker-compose up. O Kafka precisa inicializar.

---

## 🛠️ Stack Tecnológica

- .NET 8
- Confluent.Kafka
- SQLite
- MediatR
- Dapper
- Docker & Docker Compose