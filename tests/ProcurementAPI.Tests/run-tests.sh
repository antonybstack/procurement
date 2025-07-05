#!/bin/bash

echo "=== Procurement API Integration Tests ==="
echo "Building test project..."

# Build the test project
dotnet build --verbosity normal

if [ $? -eq 0 ]; then
    echo "Build successful!"
    echo "Running tests..."
    
    # Run the tests
    dotnet test --verbosity normal
    
    if [ $? -eq 0 ]; then
        echo "All tests passed!"
    else
        echo "Some tests failed!"
        exit 1
    fi
else
    echo "Build failed!"
    exit 1
fi 