#!/bin/bash

# Simple test runner for Procurement API
echo "Running Procurement API tests..."

# Check if we're in the right directory
if [ ! -f "ProcurementAPI/ProcurementAPI.csproj" ]; then
    echo "Error: Please run this script from the project root directory"
    exit 1
fi

# Build the main project
echo "Building main project..."
dotnet build ProcurementAPI/ProcurementAPI.csproj

# Build the test project
echo "Building test project..."
dotnet build tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj

# Run tests
echo "Running tests..."
dotnet test tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj --verbosity normal

echo "Tests completed!" 