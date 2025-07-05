#!/bin/bash

echo "=== Procurement API Integration Tests - Detailed Run ==="
echo "Current directory: $(pwd)"
echo ""

echo "1. Checking .NET version..."
dotnet --version
echo ""

echo "2. Checking if test project exists..."
if [ -f "ProcurementAPI.Tests.csproj" ]; then
    echo "✓ Test project file found"
else
    echo "✗ Test project file not found"
    exit 1
fi
echo ""

echo "3. Restoring packages..."
dotnet restore --verbosity normal
if [ $? -ne 0 ]; then
    echo "✗ Package restore failed"
    exit 1
fi
echo "✓ Packages restored successfully"
echo ""

echo "4. Building test project..."
dotnet build --verbosity normal --no-restore
if [ $? -ne 0 ]; then
    echo "✗ Build failed"
    exit 1
fi
echo "✓ Build successful"
echo ""

echo "5. Discovering tests..."
dotnet test --list-tests --verbosity normal --no-build
if [ $? -ne 0 ]; then
    echo "✗ Test discovery failed"
    exit 1
fi
echo "✓ Test discovery successful"
echo ""

echo "6. Running tests..."
dotnet test --verbosity normal --no-build --logger "console;verbosity=normal"
if [ $? -eq 0 ]; then
    echo "✓ All tests passed!"
else
    echo "✗ Some tests failed!"
    exit 1
fi 