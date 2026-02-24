#!/bin/bash
# Setup script for Docker environment

echo "Setting up BankMore Docker environment..."

# Create data directory if it doesn't exist
DATA_DIR="./data"
if [ ! -d "$DATA_DIR" ]; then
    mkdir -p "$DATA_DIR"
    echo "Created data directory: $DATA_DIR"
else
    echo "Data directory already exists: $DATA_DIR"
fi

# Set permissions
chmod -R 777 "$DATA_DIR"
echo "Set permissions on data directory"

echo ""
echo "Setup complete! You can now run:"
echo "  docker-compose up --build"
echo ""
