# Setup script for Docker environment
Write-Host "Setting up BankMore Docker environment..." -ForegroundColor Green

# Create data directory if it doesn't exist
$dataPath = Join-Path $PSScriptRoot "data"
if (-not (Test-Path $dataPath)) {
    New-Item -ItemType Directory -Path $dataPath -Force | Out-Null
    Write-Host "Created data directory: $dataPath" -ForegroundColor Yellow
} else {
    Write-Host "Data directory already exists: $dataPath" -ForegroundColor Cyan
}

# Set permissions (on Linux/WSL)
if ($IsLinux -or $IsMacOS) {
    chmod -R 777 $dataPath
    Write-Host "Set permissions on data directory" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Setup complete! You can now run:" -ForegroundColor Green
Write-Host "  docker-compose up --build" -ForegroundColor White
Write-Host ""
