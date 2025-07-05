# Procurement API Integration Tests

This project contains comprehensive integration tests for the Procurement API, following Microsoft's best practices for ASP.NET Core integration testing.

## Overview

The test project uses:
- **xUnit** as the testing framework
- **WebApplicationFactory** for creating test web hosts
- **Entity Framework In-Memory Database** for testing data persistence
- **Microsoft.AspNetCore.Mvc.Testing** for HTTP client testing

## Project Structure

```
ProcurementAPI.Tests/
├── CustomWebApplicationFactory.cs    # Custom factory for test configuration
├── Utilities.cs                      # Test data seeding utilities
├── TestHelpers.cs                    # Common test helper methods
├── BasicTests.cs                     # Basic endpoint accessibility tests
├── SupplierControllerTests.cs        # Comprehensive supplier endpoint tests
├── RfqControllerTests.cs             # RFQ endpoint tests with filtering
├── ItemControllerTests.cs            # Items endpoint tests with categories
├── ErrorHandlingTests.cs             # Error scenarios and edge cases
├── PerformanceTests.cs               # Concurrent operations and performance
└── README.md                         # This file
```

## Test Categories

### 1. Basic Tests (`BasicTests.cs`)
- Endpoint accessibility verification
- Content type validation
- Health check endpoint testing
- Swagger documentation accessibility

### 2. Supplier Controller Tests (`SupplierControllerTests.cs`)
- **GET /api/suppliers** - List suppliers with pagination and filtering
- **GET /api/suppliers/{id}** - Get specific supplier
- **PUT /api/suppliers/{id}** - Update supplier (full DTO updates)
- **GET /api/suppliers/countries** - Get unique countries

### 3. RFQ Controller Tests (`RfqControllerTests.cs`)
- **GET /api/rfqs** - List RFQs with pagination and filtering
- **GET /api/rfqs/{id}** - Get specific RFQ details
- **GET /api/rfqs/statuses** - Get available RFQ statuses
- **GET /api/rfqs/summary** - Get RFQ summary statistics

### 4. Item Controller Tests (`ItemControllerTests.cs`)
- **GET /api/items** - List items with pagination and filtering
- **GET /api/items/{id}** - Get specific item
- **GET /api/items/categories** - Get available item categories

### 5. Error Handling Tests (`ErrorHandlingTests.cs`)
- Invalid endpoints and HTTP methods
- Invalid pagination parameters
- Special characters in search queries
- Concurrent update scenarios
- Database error handling

### 6. Performance Tests (`PerformanceTests.cs`)
- Multiple concurrent requests
- Large dataset queries
- Memory usage monitoring
- Database connection pooling
- Mixed read/write operations

## Key Features Tested

### PUT Endpoint Testing
The PUT endpoint tests cover:
- ✅ **Full field updates** - All fields updated from complete DTO
- ✅ **Entity Framework change tracking** - EF Core handles precise updates
- ✅ **Null value handling** - Fields can be set to null
- ✅ **Boolean field updates** - IsActive field updates
- ✅ **Decimal field updates** - CreditLimit field updates
- ✅ **Invalid ID handling** - Returns 404 for non-existent suppliers
- ✅ **Database persistence** - Changes are verified in the database

### Filtering and Pagination
- ✅ **Search filtering** - Company name and supplier code search
- ✅ **Country filtering** - Filter by specific country
- ✅ **Rating filtering** - Filter by minimum rating
- ✅ **Status filtering** - Filter by active/inactive status
- ✅ **Pagination** - Page size and page number handling

## Running the Tests

### Prerequisites
- .NET 9.0 SDK
- The main ProcurementAPI project must be built

### Command Line
```bash
# Navigate to the test project directory
cd tests/ProcurementAPI.Tests

# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~SupplierControllerTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio
1. Open the solution in Visual Studio
2. Build the solution
3. Open Test Explorer (Test > Test Explorer)
4. Run all tests or specific test methods

### Visual Studio Code
1. Install the .NET Core Test Explorer extension
2. Open the test project folder
3. Use the Test Explorer panel to run tests

## Test Data

The tests use seeded data from `Utilities.cs`:
- **5 sample suppliers** with diverse data
- **Multiple countries** (USA, Germany, China)
- **Various ratings** (3-5 stars)
- **Mixed active/inactive status**
- **Different payment terms and credit limits**

## Test Configuration

### CustomWebApplicationFactory
- Configures in-memory database for testing
- Seeds test data automatically
- Provides isolated test environment
- Handles dependency injection for tests

### Database Isolation
- Each test class gets a fresh database instance
- Test data is seeded before each test run
- No interference between test runs
- Fast execution with in-memory database

## Best Practices Implemented

### Following Microsoft Guidelines
- ✅ **IClassFixture** for shared test context
- ✅ **WebApplicationFactory** for test host creation
- ✅ **In-memory database** for fast, isolated testing
- ✅ **Proper test naming** conventions
- ✅ **Arrange-Act-Assert** pattern
- ✅ **Comprehensive assertions** for both API responses and database state

### Test Organization
- ✅ **Separate test classes** for different concerns
- ✅ **Helper methods** for common operations
- ✅ **Reusable test data** utilities
- ✅ **Clear test names** describing the scenario
- ✅ **Proper cleanup** and isolation

## Test Scenarios Covered

### Supplier Update (PUT) Endpoint
1. **Full DTO Updates**
   - Client sends complete DTO with all fields
   - EF Core change tracker handles precise updates
   - All fields are updated as specified

2. **Edge Cases**
   - Update with invalid supplier ID (404)
   - Update with null values (set fields to null)
   - Concurrent update handling

### Supplier Retrieval (GET) Endpoints
1. **List Suppliers**
   - Pagination (page, pageSize)
   - Filtering (search, country, rating, status)
   - Response structure validation

2. **Get by ID**
   - Valid ID returns supplier
   - Invalid ID returns 404
   - Response data validation

3. **Get Countries**
   - Returns unique country list
   - Contains expected countries

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- **Fast execution** with in-memory database
- **No external dependencies** required
- **Consistent results** across environments
- **Comprehensive coverage** of critical paths

## Troubleshooting

### Common Issues
1. **Build errors**: Ensure the main API project builds successfully
2. **Test failures**: Check that the API endpoints are working correctly
3. **Database issues**: Verify the DbContext configuration in the main project

### Debugging
- Use `dotnet test --logger "console;verbosity=detailed"` for detailed output
- Set breakpoints in test methods for debugging
- Check the test output for specific error messages

## Contributing

When adding new tests:
1. Follow the existing naming conventions
2. Use the helper methods in `TestHelpers.cs`
3. Add appropriate assertions for both API responses and database state
4. Include edge cases and error scenarios
5. Update this README if adding new test categories 