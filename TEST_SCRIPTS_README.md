# Test Scripts for Procurement API

This directory contains shell scripts to run tests for the Procurement API project with proper output handling and error reporting.

## Available Scripts

### 1. `run-tests.sh` - Full-Featured Test Runner

A comprehensive test runner with colored output, error handling, and additional features.

**Features:**
- ✅ Colored output for better readability
- ✅ Prerequisites checking (dotnet CLI, project structure)
- ✅ Clean build option
- ✅ Code coverage support
- ✅ Detailed error reporting
- ✅ Test summary and project structure display

**Usage:**
```bash
# Run tests normally
./run-tests.sh

# Clean previous builds and run tests
./run-tests.sh --clean

# Run tests with code coverage
./run-tests.sh --coverage

# Clean builds and run tests with coverage
./run-tests.sh --clean --coverage

# Show help
./run-tests.sh --help
```

**Options:**
- `--clean`: Clean previous builds before running tests
- `--coverage`: Run tests with code coverage (requires coverlet)
- `--help`, `-h`: Show help message

### 2. `test.sh` - Simple Test Runner

A minimal test runner for basic testing needs.

**Features:**
- ✅ Simple and straightforward
- ✅ Basic error checking
- ✅ No dependencies on external tools

**Usage:**
```bash
./test.sh
```

## Prerequisites

- .NET 9.0 SDK installed
- Run scripts from the project root directory
- For coverage: `dotnet tool install --global coverlet.console`

## Project Structure

The scripts expect the following project structure:
```
procurement/
├── ProcurementAPI/
│   └── ProcurementAPI.csproj
├── tests/
│   └── ProcurementAPI.Tests/
│       └── ProcurementAPI.Tests.csproj
├── run-tests.sh
├── test.sh
└── TEST_SCRIPTS_README.md
```

## Troubleshooting

### Permission Issues
If you encounter permission issues, make sure the scripts are executable:
```bash
chmod +x run-tests.sh
chmod +x test.sh
```

### .NET CLI Not Found
Ensure .NET 9.0 SDK is installed and in your PATH:
```bash
dotnet --version
```

### Project Structure Issues
Make sure you're running the scripts from the project root directory where `ProcurementAPI/ProcurementAPI.csproj` exists.

### Test Failures
If tests fail, the scripts will:
1. Show detailed error output
2. Exit with non-zero status code
3. Provide information about which tests failed

## Integration with CI/CD

These scripts can be easily integrated into CI/CD pipelines:

```yaml
# Example GitHub Actions step
- name: Run Tests
  run: |
    chmod +x run-tests.sh
    ./run-tests.sh --clean
```

```yaml
# Example Azure DevOps step
- script: |
    chmod +x run-tests.sh
    ./run-tests.sh --clean
  displayName: 'Run Tests'
```

## Output Examples

### Successful Test Run
```
==========================================
[INFO] Starting Procurement API Test Runner
==========================================
[INFO] Using .NET version: 9.0.100
[INFO] Project structure verified
[INFO] Building main project...
[SUCCESS] Main project built successfully
[INFO] Building test project...
[SUCCESS] Test project built successfully
[INFO] Test Summary:
==========================================
[INFO] Test files found: 8
[INFO] Test project structure:
tests/ProcurementAPI.Tests/BasicTests.cs
tests/ProcurementAPI.Tests/CustomWebApplicationFactory.cs
...
==========================================
[INFO] Running tests...
==========================================
Test Run Successful.
Total tests: 108
     Passed: 108
==========================================
[SUCCESS] All tests passed!
==========================================
[SUCCESS] Test runner completed successfully!
==========================================
```

### Failed Test Run
```
[ERROR] Some tests failed!
==========================================
Test Run Failed.
Total tests: 108
     Passed: 106
     Failed: 2
==========================================
```

## Contributing

When adding new test scripts:
1. Follow the existing naming convention
2. Include proper error handling
3. Add documentation to this README
4. Test the script thoroughly
5. Make sure it's executable (`chmod +x`)

## Notes

- The scripts use `set -e` to exit on any error
- All scripts check for proper project structure
- The full-featured script includes comprehensive logging
- Both scripts are designed to work in CI/CD environments 