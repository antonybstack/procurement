using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using Xunit;

namespace ProcurementAPI.Tests;

public class PerformanceTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PerformanceTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MultipleConcurrentRequests_HandlesGracefully()
    {
        // Arrange
        const int numberOfRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send multiple concurrent requests
        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(_client.GetAsync("/api/suppliers"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(numberOfRequests, responses.Length);
        Assert.All(responses, response => response.EnsureSuccessStatusCode());
    }

    [Fact]
    public async Task MultipleConcurrentUpdates_HandlesGracefully()
    {
        // Arrange
        const int numberOfUpdates = 5;
        var tasks = new List<Task<HttpResponseMessage>>();
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = "SUP001",
            CompanyName = "Performance Test Company",
            ContactName = "Performance Contact",
            Email = "perf@example.com",
            Phone = "+1-555-0000",
            Address = "123 Performance Street",
            City = "Performance City",
            State = "PC",
            Country = "USA",
            PostalCode = "12345",
            TaxId = "TAX000000",
            PaymentTerms = "Net 30",
            CreditLimit = 10000.00m,
            Rating = 5,
            IsActive = true
        };

        // Act - Send multiple concurrent updates to different suppliers
        for (int i = 1; i <= numberOfUpdates; i++)
        {
            tasks.Add(_client.PutAsJsonAsync($"/api/suppliers/{i}", updateData));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(numberOfUpdates, responses.Length);
        // Most should succeed, some might fail due to concurrency
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        Assert.True(successCount >= numberOfUpdates * 0.6); // At least 60% should succeed
    }

    [Fact]
    public async Task LargeDatasetQuery_ReturnsInReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Query with large page size
        var response = await _client.GetAsync("/api/suppliers?pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SupplierDto>>();
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task ComplexFiltering_ReturnsInReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Query with multiple filters
        var response = await _client.GetAsync("/api/suppliers?search=tech&country=USA&minRating=4&isActive=true");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SupplierDto>>();
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 3000); // Should complete within 3 seconds
    }

    [Fact]
    public async Task MemoryUsage_StaysReasonable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var tasks = new List<Task>();

        // Act - Perform multiple operations
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(_client.GetAsync("/api/suppliers"));
            tasks.Add(_client.GetAsync("/api/rfqs"));
            tasks.Add(_client.GetAsync("/api/items"));
        }

        await Task.WhenAll(tasks);
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        Assert.True(memoryIncrease < 50 * 1024 * 1024); // Should not increase by more than 50MB
    }

    [Fact]
    public async Task DatabaseConnectionPool_HandlesMultipleRequests()
    {
        // Arrange
        const int numberOfRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send requests that require database connections
        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(_client.GetAsync($"/api/suppliers/{i + 1}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(numberOfRequests, responses.Length);
        // All should complete successfully (some may be 404, but should not be 500)
        Assert.All(responses, response =>
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task ConcurrentReadWriteOperations_HandlesGracefully()
    {
        // Arrange
        const int numberOfOperations = 10;
        var readTasks = new List<Task<HttpResponseMessage>>();
        var writeTasks = new List<Task<HttpResponseMessage>>();
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = "SUP001",
            CompanyName = "Concurrent Test",
            ContactName = "Concurrent Contact",
            Email = "concurrent@example.com",
            Phone = "+1-555-0000",
            Address = "123 Concurrent Street",
            City = "Concurrent City",
            State = "CC",
            Country = "USA",
            PostalCode = "12345",
            TaxId = "TAX000000",
            PaymentTerms = "Net 30",
            CreditLimit = 10000.00m,
            Rating = 4,
            IsActive = true
        };

        // Act - Mix read and write operations
        for (int i = 0; i < numberOfOperations; i++)
        {
            readTasks.Add(_client.GetAsync("/api/suppliers"));
            writeTasks.Add(_client.PutAsJsonAsync($"/api/suppliers/{i + 1}", updateData));
        }

        var allTasks = readTasks.Concat(writeTasks).ToArray();
        var responses = await Task.WhenAll(allTasks);

        // Assert
        Assert.Equal(numberOfOperations * 2, responses.Length);
        // Reads should all succeed
        Assert.All(readTasks, task => task.Result.EnsureSuccessStatusCode());
        // Writes should mostly succeed (some may fail due to concurrency)
        var writeSuccessCount = writeTasks.Count(task => task.Result.IsSuccessStatusCode);
        Assert.True(writeSuccessCount >= numberOfOperations * 0.5); // At least 50% should succeed
    }
}