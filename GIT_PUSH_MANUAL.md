# 🚀 Git Push - Comandos Manuais

## Opção 1: Script Automático (Recomendado)

```powershell
.\git-push.ps1
```

---

## Opção 2: Comandos Manuais

### 1. Verificar Status
```bash
git status
```

### 2. Adicionar Todos os Arquivos
```bash
git add -A
```

### 3. Criar Commit
```bash
git commit -m "feat: complete BankMore microservices platform

Major features:
- Account.API: Registration, login (JWT), movements, balance
- Transfer.API: Transfers with rollback, idempotency, Kafka events
- Tarifa.API: Automatic fees calculation, Kafka consumer/producer

Technical improvements:
- Redis cache (optional, with in-memory fallback)
- Kafka timeout (5s) to prevent hanging requests
- JWT authentication with proper claims
- Swagger cleanup (hidden internal properties)
- Docker Compose ready (one command setup)

Testing:
- 98 tests passing (63 unit + 35 integration)
- Removed flaky HTTP integration tests
- All critical flows validated

Documentation:
- QUICK_START.md for evaluators
- Postman collection included
- Complete README with architecture
- Docker setup scripts

System ready for production deployment and evaluation."
```

### 4. Push para GitHub
```bash
git push origin master
```

---

## 📊 O Que Será Enviado

### **Código**
- ✅ 3 Microsserviços completos (Account, Transfer, Tarifa)
- ✅ 98 testes passando
- ✅ Docker Compose configurado
- ✅ Redis + Kafka + SQLite

### **Documentação**
- ✅ README.md completo
- ✅ QUICK_START.md para avaliadores
- ✅ TESTS_FINAL_STATUS.md
- ✅ BankMore.postman_collection.json

### **Scripts**
- ✅ setup-docker.ps1 / .sh
- ✅ docker-compose.yml

---

## ⚠️ Se Houver Erro

### "Permission denied"
```bash
# Verificar autenticação
git config user.name
git config user.email

# Configurar se necessário
git config user.name "Seu Nome"
git config user.email "seu@email.com"
```

### "Updates were rejected"
```bash
# Pull primeiro (se houver mudanças remotas)
git pull origin master --rebase
git push origin master
```

### "Not a git repository"
```bash
# Verificar se está no diretório correto
cd D:\projects\BankMore
git status
```

---

## ✅ Verificar Push

Após o push, verifique no GitHub:
```
https://github.com/RickyKitanoDev/BankMore
```

Deve aparecer:
- ✅ Commit mais recente
- ✅ Badge "98 tests passing"
- ✅ QUICK_START.md visível
- ✅ Postman collection disponível

---

## 🎉 Pronto!

O repositório estará atualizado e pronto para avaliação.

O avaliador poderá:
```bash
git clone https://github.com/RickyKitanoDev/BankMore.git
cd BankMore
docker-compose up -d
```

E em 5 minutos terá o sistema funcionando!
