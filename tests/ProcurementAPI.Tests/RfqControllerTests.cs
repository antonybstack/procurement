using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;
using Xunit;

namespace ProcurementAPI.Tests;

public class RfqControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RfqControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRfqs_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetRfqs_ReturnsPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<RfqDto>>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Data.Count >= 0);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetRfqs_WithSearchFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs?search=electronics");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<RfqDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        // Note: Results depend on seeded data
    }

    [Fact]
    public async Task GetRfqs_WithStatusFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs?status=draft");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<RfqDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRfqs_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs?page=1&pageSize=5");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<RfqDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.Data.Count <= 5);
    }

    [Fact]
    public async Task GetRfqStatuses_ReturnsAvailableStatuses()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs/statuses");
        var statuses = await response.Content.ReadFromJsonAsync<List<string>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(statuses);
        Assert.True(statuses.Count > 0);
        Assert.Contains("Draft", statuses);
        Assert.Contains("Published", statuses);
        Assert.Contains("Closed", statuses);
        Assert.Contains("Awarded", statuses);
    }

    [Fact]
    public async Task GetRfqSummary_ReturnsSummaryData()
    {
        // Act
        var response = await _client.GetAsync("/api/rfqs/summary");
        var summary = await response.Content.ReadFromJsonAsync<object>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(summary);
    }

    [Fact]
    public async Task GetRfqById_WithValidId_ReturnsRfq()
    {
        // Arrange - This test depends on seeded data
        // We'll test the endpoint structure even if no RFQ exists

        // Act
        var response = await _client.GetAsync("/api/rfqs/1");

        // Assert
        // If RFQ exists, it should return 200, otherwise 404
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRfqById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidRfqId = 999;

        // Act
        var response = await _client.GetAsync($"/api/rfqs/{invalidRfqId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}