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

public class QuoteControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public QuoteControllerTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task GetQuotes_ReturnsSuccessStatusCode()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetQuotes_ReturnsPaginatedResult()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count >= 0);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetQuoteById_WithValidId_ReturnsQuote()
    {
        // First create a quote to test with
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 2,
            SupplierId = 2,
            LineItemId = 3,
            QuoteNumber = "Q-TEST-001",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            PaymentTerms = "Net 30",
            WarrantyPeriodMonths = 12
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Now get the quote by ID
        var response = await client.GetAsync($"/api/quotes/{createdQuote.QuoteId}");
        var quote = await DeserializeOrFail<QuoteDto>(response);

        Assert.NotNull(quote);
        Assert.Equal(createdQuote.QuoteId, quote.QuoteId);
        Assert.Equal("Q-TEST-001", quote.QuoteNumber);
        Assert.Equal("Pending", quote.Status);
        Assert.Equal(100.00m, quote.UnitPrice);
        Assert.Equal(1000.00m, quote.TotalPrice);
        Assert.Equal(10, quote.QuantityOffered);
        Assert.NotNull(quote.Supplier);
        Assert.NotNull(quote.RfqLineItem);
    }

    [Fact]
    public async Task GetQuoteById_WithInvalidId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var invalidQuoteId = 999;
        var response = await client.GetAsync($"/api/quotes/{invalidQuoteId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_WithValidData_ReturnsCreatedQuote()
    {
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 3,
            SupplierId = 1,
            LineItemId = 4,
            QuoteNumber = "Q-CREATE-001",
            Status = "Submitted",
            UnitPrice = 150.00m,
            TotalPrice = 1500.00m,
            QuantityOffered = 10,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
            PaymentTerms = "Net 45",
            WarrantyPeriodMonths = 24,
            TechnicalComplianceNotes = "Meets all technical specifications",
            ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90))
        };

        var response = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(response);

        Assert.NotNull(createdQuote);
        Assert.True(createdQuote.QuoteId > 0);
        Assert.Equal("Q-CREATE-001", createdQuote.QuoteNumber);
        Assert.Equal("Submitted", createdQuote.Status);
        Assert.Equal(150.00m, createdQuote.UnitPrice);
        Assert.Equal(1500.00m, createdQuote.TotalPrice);
        Assert.Equal(10, createdQuote.QuantityOffered);
        Assert.Equal("Net 45", createdQuote.PaymentTerms);
        Assert.Equal(24, createdQuote.WarrantyPeriodMonths);
        Assert.Equal("Meets all technical specifications", createdQuote.TechnicalComplianceNotes);
        Assert.NotNull(createdQuote.Supplier);
        Assert.NotNull(createdQuote.RfqLineItem);
        Assert.True(createdQuote.SubmittedDate > DateTime.UtcNow.AddMinutes(-5));

        // Verify the quote was persisted in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbQuote = await dbContext.Quotes.FindAsync(createdQuote.QuoteId);
        Assert.NotNull(dbQuote);
        Assert.Equal("Q-CREATE-001", dbQuote.QuoteNumber);
        Assert.Equal(QuoteStatus.Submitted, dbQuote.Status);
        Assert.Equal(150.00m, dbQuote.UnitPrice);
    }

    [Fact]
    public async Task CreateQuote_WithInvalidRfqId_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 999, // Invalid RFQ ID
            SupplierId = 1,
            LineItemId = 1,
            QuoteNumber = "Q-INVALID-001",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response = await client.PostAsJsonAsync("/api/quotes", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_WithInvalidSupplierId_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 1,
            SupplierId = 999, // Invalid supplier ID
            LineItemId = 1,
            QuoteNumber = "Q-INVALID-002",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response = await client.PostAsJsonAsync("/api/quotes", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_WithInvalidLineItemId_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 1,
            SupplierId = 1,
            LineItemId = 999, // Invalid line item ID
            QuoteNumber = "Q-INVALID-003",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response = await client.PostAsJsonAsync("/api/quotes", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_WithInvalidStatus_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 2,
            SupplierId = 4,
            LineItemId = 3,
            QuoteNumber = "Q-INVALID-STATUS-001",
            Status = "InvalidStatus", // Invalid status
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response = await client.PostAsJsonAsync("/api/quotes", createDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuote_WithValidData_ReturnsUpdatedQuote()
    {
        // First create a quote to update
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 2,
            SupplierId = 4,
            LineItemId = 3,
            QuoteNumber = "Q-UPDATE-001",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Now update the quote
        var updateDto = new QuoteUpdateDto
        {
            QuoteNumber = "Q-UPDATE-001-REVISED",
            Status = "Awarded",
            UnitPrice = 95.00m,
            TotalPrice = 950.00m,
            QuantityOffered = 10,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            PaymentTerms = "Net 60",
            WarrantyPeriodMonths = 36,
            TechnicalComplianceNotes = "Updated technical compliance notes",
            ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(120))
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/quotes/{createdQuote.QuoteId}", updateDto);
        var updatedQuote = await DeserializeOrFail<QuoteDto>(updateResponse);

        Assert.NotNull(updatedQuote);
        Assert.Equal(createdQuote.QuoteId, updatedQuote.QuoteId);
        Assert.Equal("Q-UPDATE-001-REVISED", updatedQuote.QuoteNumber);
        Assert.Equal("Awarded", updatedQuote.Status);
        Assert.Equal(95.00m, updatedQuote.UnitPrice);
        Assert.Equal(950.00m, updatedQuote.TotalPrice);
        Assert.Equal("Net 60", updatedQuote.PaymentTerms);
        Assert.Equal(36, updatedQuote.WarrantyPeriodMonths);
        Assert.Equal("Updated technical compliance notes", updatedQuote.TechnicalComplianceNotes);

        // Verify the update was persisted in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbQuote = await dbContext.Quotes.FindAsync(createdQuote.QuoteId);
        Assert.NotNull(dbQuote);
        Assert.Equal("Q-UPDATE-001-REVISED", dbQuote.QuoteNumber);
        Assert.Equal(QuoteStatus.Awarded, dbQuote.Status);
        Assert.Equal(95.00m, dbQuote.UnitPrice);
    }

    [Fact]
    public async Task UpdateQuote_WithInvalidId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var invalidQuoteId = 999;
        var updateDto = new QuoteUpdateDto
        {
            QuoteNumber = "Q-INVALID-UPDATE",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response = await client.PutAsJsonAsync($"/api/quotes/{invalidQuoteId}", updateDto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuote_WithInvalidStatus_ReturnsBadRequest()
    {
        // First create a quote to update
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 2,
            SupplierId = 5,
            LineItemId = 3,
            QuoteNumber = "Q-INVALID-STATUS-UPDATE",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Try to update with invalid status
        var updateDto = new QuoteUpdateDto
        {
            QuoteNumber = "Q-INVALID-STATUS-UPDATE",
            Status = "InvalidStatus", // Invalid status
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response = await client.PutAsJsonAsync($"/api/quotes/{createdQuote.QuoteId}", updateDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_WithValidId_ReturnsNoContent()
    {
        // First create a quote to delete
        using var client = _factory.CreateClient();
        var createDto = new QuoteCreateDto
        {
            RfqId = 2,
            SupplierId = 5,
            LineItemId = 3,
            QuoteNumber = "Q-DELETE-001",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Now delete the quote
        var deleteResponse = await client.DeleteAsync($"/api/quotes/{createdQuote.QuoteId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify the quote was deleted from the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbQuote = await dbContext.Quotes.FindAsync(createdQuote.QuoteId);
        Assert.Null(dbQuote);
    }

    [Fact]
    public async Task DeleteQuote_WithInvalidId_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var invalidQuoteId = 999;
        var response = await client.DeleteAsync($"/api/quotes/{invalidQuoteId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQuoteStatuses_ReturnsAllStatuses()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes/statuses");
        var statuses = await DeserializeOrFail<List<string>>(response);

        Assert.NotNull(statuses);
        Assert.Contains("Pending", statuses);
        Assert.Contains("Submitted", statuses);
        Assert.Contains("Awarded", statuses);
        Assert.Contains("Rejected", statuses);
        Assert.Contains("Expired", statuses);
    }

    [Fact]
    public async Task GetQuotesByRfq_ReturnsQuotesForRfq()
    {
        // First create quotes for a specific RFQ
        using var client = _factory.CreateClient();
        var rfqId = 1;

        // Create first quote
        var createDto1 = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = 1,
            LineItemId = 1,
            QuoteNumber = "Q-RFQ-001",
            Status = "Submitted",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };
        await client.PostAsJsonAsync("/api/quotes", createDto1);

        // Create second quote
        var createDto2 = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = 2,
            LineItemId = 1,
            QuoteNumber = "Q-RFQ-002",
            Status = "Submitted",
            UnitPrice = 95.00m,
            TotalPrice = 950.00m,
            QuantityOffered = 10
        };
        await client.PostAsJsonAsync("/api/quotes", createDto2);

        // Get quotes by RFQ
        var response = await client.GetAsync($"/api/quotes/rfq/{rfqId}");
        var quotes = await DeserializeOrFail<List<QuoteSummaryDto>>(response);

        Assert.NotNull(quotes);
        Assert.True(quotes.Count >= 2);
        Assert.All(quotes, quote => Assert.True(quote.QuoteId > 0));
    }

    [Fact]
    public async Task GetQuotesBySupplier_ReturnsPaginatedQuotesForSupplier()
    {
        // First create quotes for a specific supplier
        using var client = _factory.CreateClient();
        var supplierId = 1;

        // Create first quote
        var createDto1 = new QuoteCreateDto
        {
            RfqId = 1,
            SupplierId = supplierId,
            LineItemId = 1,
            QuoteNumber = "Q-SUPPLIER-001",
            Status = "Submitted",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };
        await client.PostAsJsonAsync("/api/quotes", createDto1);

        // Create second quote
        var createDto2 = new QuoteCreateDto
        {
            RfqId = 2,
            SupplierId = supplierId,
            LineItemId = 1,
            QuoteNumber = "Q-SUPPLIER-002",
            Status = "Awarded",
            UnitPrice = 95.00m,
            TotalPrice = 950.00m,
            QuantityOffered = 10
        };
        await client.PostAsJsonAsync("/api/quotes", createDto2);

        // Get quotes by supplier
        var response = await client.GetAsync($"/api/quotes/supplier/{supplierId}?page=1&pageSize=10");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        Assert.True(result.Data.Count >= 2);
        Assert.All(result.Data, quote => Assert.True(quote.QuoteId > 0));
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetQuoteSummary_ReturnsSummaryStatistics()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes/summary");
        var summary = await DeserializeOrFail<object>(response);

        Assert.NotNull(summary);
        // The summary should contain summary array with status counts
        var summaryJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("summary", summaryJson);
        Assert.Contains("status", summaryJson);
        Assert.Contains("count", summaryJson);
    }

    [Fact]
    public async Task GetQuotes_WithStatusFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes?status=Submitted");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        Assert.All(result.Data, quote => Assert.Equal("Submitted", quote.Status));
    }

    [Fact]
    public async Task GetQuotes_WithSearchFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes?search=Q-TEST");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        // Results should contain quotes with "Q-TEST" in the quote number
        Assert.All(result.Data, quote =>
            Assert.True(quote.QuoteNumber.Contains("Q-TEST") ||
                       quote.SupplierName.Contains("Q-TEST") ||
                       quote.RfqNumber.Contains("Q-TEST")));
    }

    [Fact]
    public async Task GetQuotes_WithDateRangeFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var response = await client.GetAsync($"/api/quotes?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        // All quotes should be within the date range
        Assert.All(result.Data, quote =>
        {
            var submittedDate = DateOnly.FromDateTime(quote.SubmittedDate);
            Assert.True(submittedDate >= fromDate && submittedDate <= toDate);
        });
    }

    [Fact]
    public async Task GetQuotes_WithPagination_ReturnsCorrectPage()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes?page=2&pageSize=5");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.Data.Count <= 5);
    }
}

public class QuoteControllerReadOnlyTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public QuoteControllerReadOnlyTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task GetQuotes_ReturnsSuccessStatusCode()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetQuotes_ReturnsPaginatedResult()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Data.Count >= 0);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task GetQuoteStatuses_ReturnsAllStatuses()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes/statuses");
        var statuses = await DeserializeOrFail<List<string>>(response);

        Assert.NotNull(statuses);
        Assert.Contains("Pending", statuses);
        Assert.Contains("Submitted", statuses);
        Assert.Contains("Awarded", statuses);
        Assert.Contains("Rejected", statuses);
        Assert.Contains("Expired", statuses);
    }

    [Fact]
    public async Task GetQuoteSummary_ReturnsSummaryStatistics()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes/summary");
        var summary = await DeserializeOrFail<object>(response);

        Assert.NotNull(summary);
    }

    [Fact]
    public async Task GetQuotes_WithStatusFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes?status=Submitted");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        Assert.All(result.Data, quote => Assert.Equal("Submitted", quote.Status));
    }

    [Fact]
    public async Task GetQuotes_WithSearchFilter_ReturnsFilteredResults()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes?search=electronics");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetQuotes_WithPagination_ReturnsCorrectPage()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quotes?page=1&pageSize=10");
        var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.Data.Count <= 10);
    }
}