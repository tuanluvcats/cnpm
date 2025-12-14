#!/bin/bash

echo "Cleaning SanBong project..."
cd "$(dirname "$0")"

# Remove old PharmaWeb build artifacts
rm -f obj/PharmaWeb.csproj.*

# Clean project
dotnet clean

# Remove bin and obj directories
rm -rf bin/
rm -rf obj/

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build project
echo "Building project..."
dotnet build

echo "Done!"
