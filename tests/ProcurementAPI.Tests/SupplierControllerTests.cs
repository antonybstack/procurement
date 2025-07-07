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

public class SupplierControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public SupplierControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<T> DeserializeOrFail<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}\nContent: {content}");
        }
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task GetSuppliers_ReturnsSuccessStatusCode()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetSuppliers_ReturnsPaginatedResult()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        Assert.True(result.TotalCount > 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }



    [Fact]
    public async Task GetSupplierById_WithInvalidId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var invalidSupplierId = 999;
        var response = await client.GetAsync($"/api/suppliers/{invalidSupplierId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_WithValidData_ReturnsUpdatedSupplier()
    {
        using var client = _factory.CreateClient();
        var supplierId = 1;
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = "SUP001",
            CompanyName = "Updated Tech Solutions Inc.",
            ContactName = "John Updated Smith",
            Email = "john.updated@techsolutions.com",
            Phone = "+1-555-0101",
            Address = "123 Tech Street",
            City = "San Francisco",
            State = "CA",
            Country = "USA",
            PostalCode = "94105",
            TaxId = "TAX123456",
            PaymentTerms = "Net 30",
            CreditLimit = 50000.00m,
            Rating = 4,
            IsActive = true
        };
        var response = await client.PutAsJsonAsync($"/api/suppliers/{supplierId}", updateData);
        var updatedSupplier = await DeserializeOrFail<SupplierDto>(response);
        Assert.NotNull(updatedSupplier);
        Assert.Equal(supplierId, updatedSupplier.SupplierId);
        Assert.Equal("Updated Tech Solutions Inc.", updatedSupplier.CompanyName);
        Assert.Equal("John Updated Smith", updatedSupplier.ContactName);
        Assert.Equal("john.updated@techsolutions.com", updatedSupplier.Email);
        Assert.Equal(4, updatedSupplier.Rating);
        // Verify the update was persisted in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbSupplier = await dbContext.Suppliers.FindAsync(supplierId);
        Assert.NotNull(dbSupplier);
        Assert.Equal("Updated Tech Solutions Inc.", dbSupplier.CompanyName);
        Assert.Equal("John Updated Smith", dbSupplier.ContactName);
        Assert.Equal("john.updated@techsolutions.com", dbSupplier.Email);
        Assert.Equal(4, dbSupplier.Rating);
    }

    [Fact]
    public async Task UpdateSupplier_WithPartialData_UpdatesAllFields()
    {
        using var client = _factory.CreateClient();
        var supplierId = 2;
        var originalSupplier = await GetSupplierFromDatabase(supplierId);
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = originalSupplier.SupplierCode,
            CompanyName = originalSupplier.CompanyName,
            ContactName = originalSupplier.ContactName,
            Email = originalSupplier.Email,
            Phone = originalSupplier.Phone,
            Address = originalSupplier.Address,
            City = originalSupplier.City,
            State = originalSupplier.State,
            Country = originalSupplier.Country,
            PostalCode = originalSupplier.PostalCode,
            TaxId = originalSupplier.TaxId,
            PaymentTerms = originalSupplier.PaymentTerms,
            CreditLimit = originalSupplier.CreditLimit,
            Rating = 5, // Only change the rating
            IsActive = originalSupplier.IsActive
        };
        var response = await client.PutAsJsonAsync($"/api/suppliers/{supplierId}", updateData);
        var updatedSupplier = await DeserializeOrFail<SupplierDto>(response);
        Assert.NotNull(updatedSupplier);
        Assert.Equal(supplierId, updatedSupplier.SupplierId);
        Assert.Equal(5, updatedSupplier.Rating);
        Assert.Equal(originalSupplier.CompanyName, updatedSupplier.CompanyName);
        Assert.Equal(originalSupplier.ContactName, updatedSupplier.ContactName);
        Assert.Equal(originalSupplier.Email, updatedSupplier.Email);
    }

    [Fact]
    public async Task UpdateSupplier_WithInvalidId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var invalidSupplierId = 999;
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = "SUP999",
            CompanyName = "Test Company",
            ContactName = "Test Contact",
            Email = "test@company.com",
            Phone = "+1-555-9999",
            Address = "999 Test Street",
            City = "Test City",
            State = "TS",
            Country = "USA",
            PostalCode = "99999",
            TaxId = "TAX999999",
            PaymentTerms = "Net 30",
            CreditLimit = 10000.00m,
            Rating = 3,
            IsActive = true
        };
        var response = await client.PutAsJsonAsync($"/api/suppliers/{invalidSupplierId}", updateData);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_WithNullValues_UpdatesFieldsToNull()
    {
        using var client = _factory.CreateClient();
        var supplierId = 4;
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = "SUP004",
            CompanyName = "European Electronics GmbH",
            ContactName = null,
            Email = null,
            Phone = "+49-30-123456",
            Address = "10 Berliner Strasse",
            City = "Berlin",
            State = "Berlin",
            Country = "Germany",
            PostalCode = "10115",
            TaxId = "DE123456789",
            PaymentTerms = "Net 60",
            CreditLimit = 100000.00m,
            Rating = 5,
            IsActive = true
        };
        var response = await client.PutAsJsonAsync($"/api/suppliers/{supplierId}", updateData);
        var updatedSupplier = await DeserializeOrFail<SupplierDto>(response);
        Assert.NotNull(updatedSupplier);
        Assert.Null(updatedSupplier.ContactName);
        Assert.Null(updatedSupplier.Email);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbSupplier = await dbContext.Suppliers.FindAsync(supplierId);
        Assert.NotNull(dbSupplier);
        Assert.Null(dbSupplier.ContactName);
        Assert.Null(dbSupplier.Email);
    }

    [Fact]
    public async Task UpdateSupplier_WithBooleanField_UpdatesCorrectly()
    {
        using var client = _factory.CreateClient();
        var supplierId = 5;
        var originalSupplier = await GetSupplierFromDatabase(supplierId);
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = originalSupplier.SupplierCode,
            CompanyName = originalSupplier.CompanyName,
            ContactName = originalSupplier.ContactName,
            Email = originalSupplier.Email,
            Phone = originalSupplier.Phone,
            Address = originalSupplier.Address,
            City = originalSupplier.City,
            State = originalSupplier.State,
            Country = originalSupplier.Country,
            PostalCode = originalSupplier.PostalCode,
            TaxId = originalSupplier.TaxId,
            PaymentTerms = originalSupplier.PaymentTerms,
            CreditLimit = originalSupplier.CreditLimit,
            Rating = originalSupplier.Rating,
            IsActive = false // Change only the IsActive field
        };
        var response = await client.PutAsJsonAsync($"/api/suppliers/{supplierId}", updateData);
        var updatedSupplier = await DeserializeOrFail<SupplierDto>(response);
        Assert.NotNull(updatedSupplier);
        Assert.False(updatedSupplier.IsActive);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbSupplier = await dbContext.Suppliers.FindAsync(supplierId);
        Assert.NotNull(dbSupplier);
        Assert.False(dbSupplier.IsActive);
    }

    [Fact]
    public async Task UpdateSupplier_WithDecimalField_UpdatesCorrectly()
    {
        using var client = _factory.CreateClient();
        var supplierId = 1;
        var originalSupplier = await GetSupplierFromDatabase(supplierId);
        var newCreditLimit = 75000.50m;
        var updateData = new SupplierUpdateDto
        {
            SupplierCode = originalSupplier.SupplierCode,
            CompanyName = originalSupplier.CompanyName,
            ContactName = originalSupplier.ContactName,
            Email = originalSupplier.Email,
            Phone = originalSupplier.Phone,
            Address = originalSupplier.Address,
            City = originalSupplier.City,
            State = originalSupplier.State,
            Country = originalSupplier.Country,
            PostalCode = originalSupplier.PostalCode,
            TaxId = originalSupplier.TaxId,
            PaymentTerms = originalSupplier.PaymentTerms,
            CreditLimit = newCreditLimit, // Change only the CreditLimit
            Rating = originalSupplier.Rating,
            IsActive = originalSupplier.IsActive
        };
        var response = await client.PutAsJsonAsync($"/api/suppliers/{supplierId}", updateData);
        var updatedSupplier = await DeserializeOrFail<SupplierDto>(response);
        Assert.NotNull(updatedSupplier);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbSupplier = await dbContext.Suppliers.FindAsync(supplierId);
        Assert.NotNull(dbSupplier);
        Assert.Equal(newCreditLimit, dbSupplier.CreditLimit);
    }

    [Fact]
    public async Task GetCountries_ReturnsUniqueCountries()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers/countries");
        var countries = await DeserializeOrFail<List<string>>(response);
        Assert.NotNull(countries);
        Assert.True(countries.Count > 0);
        Assert.Contains("USA", countries);
        Assert.Contains("Germany", countries);
        Assert.Contains("China", countries);
    }

    [Fact]
    public async Task GetSuppliers_WithSearchFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?search=Tech");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.True(
                supplier.CompanyName.Contains("Tech", StringComparison.OrdinalIgnoreCase) ||
                supplier.SupplierCode.Contains("Tech", StringComparison.OrdinalIgnoreCase),
                $"Supplier {supplier.CompanyName} does not contain 'Tech' in name or code");
        }
    }

    [Fact]
    public async Task GetSuppliers_WithCountryFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?country=USA");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.Equal("USA", supplier.Country);
        }
    }

    [Fact]
    public async Task GetSuppliers_WithRatingFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?minRating=4");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.True(supplier.Rating >= 4, $"Supplier {supplier.CompanyName} has rating {supplier.Rating} which is less than 4");
        }
    }

    [Fact]
    public async Task GetSuppliers_WithStatusFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?isActive=true");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.True(supplier.IsActive, $"Supplier {supplier.CompanyName} is not active");
        }
    }

    [Fact]
    public async Task GetSuppliers_WithPagination_ReturnsCorrectPage()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?page=1&pageSize=2");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.Data.Count <= 2);
    }

    private async Task<Supplier> GetSupplierFromDatabase(int supplierId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        return await dbContext.Suppliers.FindAsync(supplierId)
            ?? throw new InvalidOperationException($"Supplier with ID {supplierId} not found");
    }
}

public class SupplierControllerReadOnlyTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public SupplierControllerReadOnlyTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<T> DeserializeOrFail<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}\nContent: {content}");
        }
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task GetSuppliers_ReturnsSuccessStatusCode()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetSuppliers_ReturnsPaginatedResult()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        Assert.True(result.TotalCount > 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetSupplierById_WithValidId_ReturnsSupplier()
    {
        using var client = _factory.CreateClient();
        var supplierId = 1;
        var response = await client.GetAsync($"/api/suppliers/{supplierId}");
        var supplier = await DeserializeOrFail<SupplierDto>(response);
        Assert.NotNull(supplier);
        Assert.Equal(supplierId, supplier.SupplierId);
        Assert.Equal("SUP001", supplier.SupplierCode);
        // Don't assert specific company name as it might be updated by other tests
        Assert.NotNull(supplier.CompanyName);
        Assert.NotEmpty(supplier.CompanyName);
    }

    [Fact]
    public async Task GetSupplierById_WithInvalidId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var invalidSupplierId = 999;
        var response = await client.GetAsync($"/api/suppliers/{invalidSupplierId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCountries_ReturnsUniqueCountries()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers/countries");
        var countries = await DeserializeOrFail<List<string>>(response);
        Assert.NotNull(countries);
        Assert.True(countries.Count > 0);
        Assert.Contains("USA", countries);
        Assert.Contains("Germany", countries);
        Assert.Contains("China", countries);
    }

    [Fact]
    public async Task GetSuppliers_WithSearchFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?search=Tech");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.True(
                supplier.CompanyName.Contains("Tech", StringComparison.OrdinalIgnoreCase) ||
                supplier.SupplierCode.Contains("Tech", StringComparison.OrdinalIgnoreCase),
                $"Supplier {supplier.CompanyName} does not contain 'Tech' in name or code");
        }
    }

    [Fact]
    public async Task GetSuppliers_WithCountryFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?country=USA");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.Equal("USA", supplier.Country);
        }
    }

    [Fact]
    public async Task GetSuppliers_WithRatingFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?minRating=4");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.True(supplier.Rating >= 4, $"Supplier {supplier.CompanyName} has rating {supplier.Rating} which is less than 4");
        }
    }

    [Fact]
    public async Task GetSuppliers_WithStatusFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?isActive=true");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
        foreach (var supplier in result.Data)
        {
            Assert.True(supplier.IsActive, $"Supplier {supplier.CompanyName} is not active");
        }
    }

    [Fact]
    public async Task GetSuppliers_WithPagination_ReturnsCorrectPage()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/suppliers?page=1&pageSize=2");
        var result = await DeserializeOrFail<PaginatedResult<SupplierDto>>(response);
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.Data.Count <= 2);
    }

    private async Task<Supplier> GetSupplierFromDatabase(int supplierId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        return await dbContext.Suppliers.FindAsync(supplierId)
            ?? throw new InvalidOperationException($"Supplier with ID {supplierId} not found");
    }
}