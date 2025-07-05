using System.Net.Http.Json;
using ProcurementAPI.DTOs;

namespace ProcurementAPI.Tests;

public static class TestHelpers
{
    public static async Task<PaginatedResult<SupplierDto>> GetSuppliersAsync(HttpClient client, string? query = null)
    {
        var url = "/api/suppliers";
        if (!string.IsNullOrEmpty(query))
        {
            url += $"?{query}";
        }

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaginatedResult<SupplierDto>>()
            ?? throw new InvalidOperationException("Failed to deserialize suppliers response");
    }

    public static async Task<SupplierDto> GetSupplierByIdAsync(HttpClient client, int id)
    {
        var response = await client.GetAsync($"/api/suppliers/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierDto>()
            ?? throw new InvalidOperationException($"Failed to deserialize supplier {id} response");
    }

    public static async Task<SupplierDto> UpdateSupplierAsync(HttpClient client, int id, SupplierUpdateDto updateData)
    {
        var response = await client.PutAsJsonAsync($"/api/suppliers/{id}", updateData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SupplierDto>()
            ?? throw new InvalidOperationException("Failed to deserialize supplier response");
    }

    public static async Task<List<string>> GetCountriesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/suppliers/countries");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<string>>()
            ?? throw new InvalidOperationException("Failed to deserialize countries response");
    }

    public static SupplierUpdateDto CreateSampleUpdateData()
    {
        return new SupplierUpdateDto
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
    }
}