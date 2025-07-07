#!/bin/bash

# Procurement API Test Runner Script
# This script runs dotnet tests with proper output handling and error reporting

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if dotnet is available
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        print_error "dotnet CLI is not installed or not in PATH"
        exit 1
    fi
    
    DOTNET_VERSION=$(dotnet --version)
    print_status "Using .NET version: $DOTNET_VERSION"
}

# Function to check if we're in the right directory
check_directory() {
    if [ ! -f "ProcurementAPI/ProcurementAPI.csproj" ]; then
        print_error "ProcurementAPI.csproj not found. Please run this script from the project root directory."
        exit 1
    fi
    
    if [ ! -f "tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj" ]; then
        print_error "Test project not found. Please ensure tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj exists."
        exit 1
    fi
    
    print_status "Project structure verified"
}

# Function to clean previous builds
clean_builds() {
    print_status "Cleaning previous builds..."
    
    # Clean main project
    if [ -d "ProcurementAPI/bin" ]; then
        rm -rf ProcurementAPI/bin
        print_status "Cleaned ProcurementAPI/bin"
    fi
    
    if [ -d "ProcurementAPI/obj" ]; then
        rm -rf ProcurementAPI/obj
        print_status "Cleaned ProcurementAPI/obj"
    fi
    
    # Clean test project
    if [ -d "tests/ProcurementAPI.Tests/bin" ]; then
        rm -rf tests/ProcurementAPI.Tests/bin
        print_status "Cleaned tests/ProcurementAPI.Tests/bin"
    fi
    
    if [ -d "tests/ProcurementAPI.Tests/obj" ]; then
        rm -rf tests/ProcurementAPI.Tests/obj
        print_status "Cleaned tests/ProcurementAPI.Tests/obj"
    fi
}

# Function to build the main project
build_main_project() {
    print_status "Building main project..."
    
    if dotnet build ProcurementAPI/ProcurementAPI.csproj --verbosity normal; then
        print_success "Main project built successfully"
    else
        print_error "Failed to build main project"
        exit 1
    fi
}

# Function to build the test project
build_test_project() {
    print_status "Building test project..."
    
    if dotnet build tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj --verbosity normal; then
        print_success "Test project built successfully"
    else
        print_error "Failed to build test project"
        exit 1
    fi
}

# Function to run tests
run_tests() {
    print_status "Running tests..."
    echo "=========================================="
    
    # Run tests with detailed output
    if dotnet test tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj \
        --verbosity normal \
        --logger "console;verbosity=detailed" \
        --no-build; then
        
        print_success "All tests passed!"
        echo "=========================================="
    else
        print_error "Some tests failed!"
        echo "=========================================="
        exit 1
    fi
}

# Function to run tests with coverage (optional)
run_tests_with_coverage() {
    print_status "Running tests with coverage..."
    echo "=========================================="
    
    # Check if coverlet is available
    if ! dotnet tool list --global | grep -q coverlet; then
        print_warning "Coverlet not installed globally. Installing..."
        dotnet tool install --global coverlet.console
    fi
    
    # Run tests with coverage
    if dotnet test tests/ProcurementAPI.Tests/ProcurementAPI.Tests.csproj \
        --verbosity normal \
        --logger "console;verbosity=detailed" \
        --collect:"XPlat Code Coverage" \
        --results-directory ./coverage \
        --no-build; then
        
        print_success "Tests with coverage completed!"
        print_status "Coverage report available in ./coverage directory"
        echo "=========================================="
    else
        print_error "Tests with coverage failed!"
        echo "=========================================="
        exit 1
    fi
}

# Function to show test summary
show_test_summary() {
    print_status "Test Summary:"
    echo "=========================================="
    
    # Count test files
    TEST_FILES=$(find tests/ProcurementAPI.Tests -name "*.cs" -type f | wc -l)
    print_status "Test files found: $TEST_FILES"
    
    # Show test project structure
    print_status "Test project structure:"
    find tests/ProcurementAPI.Tests -name "*.cs" -type f | sort
    
    echo "=========================================="
}

# Main execution
main() {
    echo "=========================================="
    print_status "Starting Procurement API Test Runner"
    echo "=========================================="
    
    # Check prerequisites
    check_dotnet
    check_directory
    
    # Parse command line arguments
    CLEAN_BUILD=false
    WITH_COVERAGE=false
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --clean)
                CLEAN_BUILD=true
                shift
                ;;
            --coverage)
                WITH_COVERAGE=true
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [OPTIONS]"
                echo ""
                echo "Options:"
                echo "  --clean     Clean previous builds before running tests"
                echo "  --coverage  Run tests with code coverage"
                echo "  --help, -h  Show this help message"
                echo ""
                echo "Examples:"
                echo "  $0                    # Run tests normally"
                echo "  $0 --clean           # Clean and run tests"
                echo "  $0 --coverage        # Run tests with coverage"
                echo "  $0 --clean --coverage # Clean, run tests with coverage"
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done
    
    # Clean builds if requested
    if [ "$CLEAN_BUILD" = true ]; then
        clean_builds
    fi
    
    # Build projects
    build_main_project
    build_test_project
    
    # Show test summary
    show_test_summary
    
    # Run tests
    if [ "$WITH_COVERAGE" = true ]; then
        run_tests_with_coverage
    else
        run_tests
    fi
    
    print_success "Test runner completed successfully!"
    echo "=========================================="
}

# Run main function with all arguments
main "$@" 