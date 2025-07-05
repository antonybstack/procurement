#!/bin/bash

echo "=== Test Runner Debug ==="
echo "Current directory: $(pwd)"
echo "Files in directory:"
ls -la

echo ""
echo "Building project..."
dotnet build

echo ""
echo "Running tests..."
dotnet test --verbosity normal

echo ""
echo "Test runner completed." 