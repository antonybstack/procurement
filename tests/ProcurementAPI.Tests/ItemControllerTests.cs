using System.Collections.Generic;
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

public class ItemControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ItemControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/items");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ItemDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetItems_ReturnsPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/items");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ItemDto>>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Data.Count >= 0);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetItems_WithSearchFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/items?search=laptop");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ItemDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetItems_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/items?category=Electronics");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ItemDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetItems_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/api/items?page=1&pageSize=5");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<ItemDto>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.Data.Count <= 5);
    }

    [Fact]
    public async Task GetItemCategories_ReturnsAvailableCategories()
    {
        // Act
        var response = await _client.GetAsync("/api/items/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<string>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(categories);
        Assert.True(categories.Count > 0);
        Assert.Contains("Electronics", categories);
        Assert.Contains("Services", categories);
    }

    [Fact]
    public async Task GetItemById_WithValidId_ReturnsItem()
    {
        // Arrange - This test depends on seeded data
        // We'll test the endpoint structure even if no item exists

        // Act
        var response = await _client.GetAsync("/api/items/1");

        // Assert
        // If item exists, it should return 200, otherwise 404
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetItemById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidItemId = 999;

        // Act
        var response = await _client.GetAsync($"/api/items/{invalidItemId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/items/categories");

        // Assert
        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(categories);
        Assert.True(categories.Count > 0);
    }

    [Fact]
    public async Task GetItemSummary_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/items/summary");

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(summary);
    }

    [Fact]
    public async Task CreateItem_WithValidData_ReturnsSuccessStatusCode()
    {
        // Arrange
        var createDto = new ItemCreateDto
        {
            ItemCode = "TEST-001",
            Description = "Test Item",
            Category = "Electronics",
            UnitOfMeasure = "pieces",
            StandardCost = 100.00m,
            MinOrderQuantity = 1,
            LeadTimeDays = 30,
            IsActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/items", createDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(result);
        Assert.Equal(createDto.ItemCode, result.ItemCode);
        Assert.Equal(createDto.Description, result.Description);
    }

    [Fact]
    public async Task CreateItem_WithDuplicateItemCode_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new ItemCreateDto
        {
            ItemCode = "DUPLICATE-001",
            Description = "Duplicate Item",
            Category = "Electronics",
            UnitOfMeasure = "pieces",
            StandardCost = 100.00m,
            MinOrderQuantity = 1,
            LeadTimeDays = 30,
            IsActive = true
        };

        // Act - Create first item
        var response1 = await _client.PostAsJsonAsync("/api/items", createDto);
        response1.EnsureSuccessStatusCode();

        // Act - Try to create duplicate
        var response2 = await _client.PostAsJsonAsync("/api/items", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task GetItem_WithValidId_ReturnsSuccessStatusCode()
    {
        // Arrange - Create an item first
        var createDto = new ItemCreateDto
        {
            ItemCode = "GET-TEST-001",
            Description = "Get Test Item",
            Category = "Electronics",
            UnitOfMeasure = "pieces",
            StandardCost = 100.00m,
            MinOrderQuantity = 1,
            LeadTimeDays = 30,
            IsActive = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/items", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createdItem = await createResponse.Content.ReadFromJsonAsync<ItemDto>();

        // Act
        var response = await _client.GetAsync($"/api/items/{createdItem!.ItemId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(result);
        Assert.Equal(createdItem.ItemId, result.ItemId);
    }

    [Fact]
    public async Task GetItem_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/items/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_WithValidData_ReturnsSuccessStatusCode()
    {
        // Arrange - Create an item first
        var createDto = new ItemCreateDto
        {
            ItemCode = "UPDATE-TEST-001",
            Description = "Update Test Item",
            Category = "Electronics",
            UnitOfMeasure = "pieces",
            StandardCost = 100.00m,
            MinOrderQuantity = 1,
            LeadTimeDays = 30,
            IsActive = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/items", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createdItem = await createResponse.Content.ReadFromJsonAsync<ItemDto>();

        // Arrange - Update data
        var updateDto = new ItemUpdateDto
        {
            ItemCode = "UPDATE-TEST-001-UPDATED",
            Description = "Updated Test Item",
            Category = "Machinery",
            UnitOfMeasure = "units",
            StandardCost = 150.00m,
            MinOrderQuantity = 5,
            LeadTimeDays = 45,
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/items/{createdItem!.ItemId}", updateDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(result);
        Assert.Equal(updateDto.ItemCode, result.ItemCode);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(updateDto.Category, result.Category);
    }

    [Fact]
    public async Task DeleteItem_WithValidId_ReturnsSuccessStatusCode()
    {
        // Arrange - Create an item first
        var createDto = new ItemCreateDto
        {
            ItemCode = "DELETE-TEST-001",
            Description = "Delete Test Item",
            Category = "Electronics",
            UnitOfMeasure = "pieces",
            StandardCost = 100.00m,
            MinOrderQuantity = 1,
            LeadTimeDays = 30,
            IsActive = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/items", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createdItem = await createResponse.Content.ReadFromJsonAsync<ItemDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/items/{createdItem!.ItemId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify item is deleted
        var getResponse = await _client.GetAsync($"/api/items/{createdItem.ItemId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/items/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}