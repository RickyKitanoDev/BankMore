# Script para executar testes de integração por categoria
# Executa em grupos menores para feedback mais rápido

Write-Host "🧪 Executando Testes de Integração - Tarifa API" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Testes de Configuração (mais rápidos)
Write-Host "📋 1/5 - Testando Configuração..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Integration.Configuration" --logger "console;verbosity=minimal" --no-build
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Falhou!" -ForegroundColor Red; exit 1 }
Write-Host "✅ Configuração OK" -ForegroundColor Green
Write-Host ""

# 2. Testes de Persistência
Write-Host "💾 2/5 - Testando Persistência..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Integration.Persistence" --logger "console;verbosity=minimal" --no-build
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Falhou!" -ForegroundColor Red; exit 1 }
Write-Host "✅ Persistência OK" -ForegroundColor Green
Write-Host ""

# 3. Testes de Repositório
Write-Host "🗄️ 3/5 - Testando Repositório..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Integration.Repositories" --logger "console;verbosity=minimal" --no-build
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Falhou!" -ForegroundColor Red; exit 1 }
Write-Host "✅ Repositório OK" -ForegroundColor Green
Write-Host ""

# 4. Testes de Handler
Write-Host "⚙️ 4/5 - Testando Handlers..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Integration.Handlers" --logger "console;verbosity=minimal" --no-build
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Falhou!" -ForegroundColor Red; exit 1 }
Write-Host "✅ Handlers OK" -ForegroundColor Green
Write-Host ""

# 5. Testes End-to-End
Write-Host "🎯 5/5 - Testando End-to-End..." -ForegroundColor Yellow
dotnet test --filter "TarifaEndToEndIntegrationTests" --logger "console;verbosity=minimal" --no-build
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Falhou!" -ForegroundColor Red; exit 1 }
Write-Host "✅ End-to-End OK" -ForegroundColor Green
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✅ TODOS OS TESTES DE INTEGRAÇÃO PASSARAM!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
