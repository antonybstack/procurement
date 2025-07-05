Write-Host "=== Running Tests ==="
Write-Host "Current directory: $(Get-Location)"
Write-Host "Building project..."
dotnet build
Write-Host "Running tests..."
dotnet test --verbosity normal
Write-Host "Tests completed." 