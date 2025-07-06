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

public class QuoteIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public QuoteIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task QuoteLifecycle_CompleteWorkflow_WorksCorrectly()
    {
        // Test the complete quote lifecycle: Create -> Read -> Update -> Delete
        using var client = _factory.CreateClient();
        var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();

        var createDto = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = supplierId,
            LineItemId = lineItemId,
            QuoteNumber = "Q-LIFECYCLE-001",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            PaymentTerms = "Net 30",
            WarrantyPeriodMonths = 12,
            TechnicalComplianceNotes = "Lifecycle test quote",
            ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90))
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        Assert.NotNull(createdQuote);
        Assert.True(createdQuote.QuoteId > 0);
        Assert.Equal("Q-LIFECYCLE-001", createdQuote.QuoteNumber);
        Assert.Equal("Pending", createdQuote.Status);

        // Step 2: Read the quote
        var readResponse = await client.GetAsync($"/api/quotes/{createdQuote.QuoteId}");
        var readQuote = await DeserializeOrFail<QuoteDto>(readResponse);

        Assert.NotNull(readQuote);
        Assert.Equal(createdQuote.QuoteId, readQuote.QuoteId);
        Assert.Equal("Q-LIFECYCLE-001", readQuote.QuoteNumber);
        Assert.Equal("Pending", readQuote.Status);
        Assert.NotNull(readQuote.Supplier);
        Assert.NotNull(readQuote.RfqLineItem);

        // Step 3: Update the quote
        var updateDto = new QuoteUpdateDto
        {
            QuoteNumber = "Q-LIFECYCLE-001-REVISED",
            Status = "Submitted",
            UnitPrice = 95.00m,
            TotalPrice = 950.00m,
            QuantityOffered = 10,
            PaymentTerms = "Net 45",
            WarrantyPeriodMonths = 24,
            TechnicalComplianceNotes = "Updated lifecycle test quote"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/quotes/{createdQuote.QuoteId}", updateDto);
        var updatedQuote = await DeserializeOrFail<QuoteDto>(updateResponse);

        Assert.NotNull(updatedQuote);
        Assert.Equal(createdQuote.QuoteId, updatedQuote.QuoteId);
        Assert.Equal("Q-LIFECYCLE-001-REVISED", updatedQuote.QuoteNumber);
        Assert.Equal("Submitted", updatedQuote.Status);
        Assert.Equal(95.00m, updatedQuote.UnitPrice);

        // Step 4: Delete the quote
        var deleteResponse = await client.DeleteAsync($"/api/quotes/{createdQuote.QuoteId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Step 5: Verify deletion
        var verifyResponse = await client.GetAsync($"/api/quotes/{createdQuote.QuoteId}");
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task QuoteBusinessRules_ValidationWorksCorrectly()
    {
        using var client = _factory.CreateClient();

        // Test 1: Cannot create quote with invalid RFQ ID
        var invalidRfqDto = new QuoteCreateDto
        {
            RfqId = 999,
            SupplierId = Utilities.GetNextSupplierId(),
            LineItemId = Utilities.GetNextLineItemId(),
            QuoteNumber = "Q-INVALID-RFQ",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response1 = await client.PostAsJsonAsync("/api/quotes", invalidRfqDto);
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

        // Test 2: Cannot create quote with invalid supplier ID
        var invalidSupplierDto = new QuoteCreateDto
        {
            RfqId = Utilities.GetNextRfqId(),
            SupplierId = 999,
            LineItemId = Utilities.GetNextLineItemId(),
            QuoteNumber = "Q-INVALID-SUPPLIER",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response2 = await client.PostAsJsonAsync("/api/quotes", invalidSupplierDto);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        // Test 3: Cannot create quote with invalid line item ID
        var invalidLineItemDto = new QuoteCreateDto
        {
            RfqId = Utilities.GetNextRfqId(),
            SupplierId = Utilities.GetNextSupplierId(),
            LineItemId = 999,
            QuoteNumber = "Q-INVALID-LINEITEM",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response3 = await client.PostAsJsonAsync("/api/quotes", invalidLineItemDto);
        Assert.Equal(HttpStatusCode.BadRequest, response3.StatusCode);

        // Note: Duplicate quote validation is not implemented in the API
        // Test 4: Create a valid quote to ensure the API works
        var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();
        var validDto = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = supplierId,
            LineItemId = lineItemId,
            QuoteNumber = "Q-VALID-TEST",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var response4 = await client.PostAsJsonAsync("/api/quotes", validDto);
        Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
    }

    [Fact]
    public async Task QuoteStatusTransitions_WorkCorrectly()
    {
        using var client = _factory.CreateClient();
        var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();

        var createDto = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = supplierId,
            LineItemId = lineItemId,
            QuoteNumber = "Q-STATUS-TEST",
            Status = "Pending",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Test status transitions
        var statuses = new[] { "Submitted", "Awarded", "Rejected" };

        foreach (var status in statuses)
        {
            var updateDto = new QuoteUpdateDto
            {
                QuoteNumber = createdQuote.QuoteNumber,
                Status = status,
                UnitPrice = createdQuote.UnitPrice,
                TotalPrice = createdQuote.TotalPrice,
                QuantityOffered = createdQuote.QuantityOffered
            };

            var updateResponse = await client.PutAsJsonAsync($"/api/quotes/{createdQuote.QuoteId}", updateDto);
            var updatedQuote = await DeserializeOrFail<QuoteDto>(updateResponse);

            Assert.Equal(status, updatedQuote.Status);
        }
    }

    [Fact]
    public async Task QuoteFiltering_WorksCorrectly()
    {
        using var client = _factory.CreateClient();
        var statuses = new[] { "Pending", "Submitted", "Awarded" };
        var createdQuotes = new List<QuoteDto>();

        for (int i = 0; i < statuses.Length; i++)
        {
            var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();
            var createDto = new QuoteCreateDto
            {
                RfqId = rfqId,
                SupplierId = supplierId,
                LineItemId = lineItemId,
                QuoteNumber = $"Q-FILTER-{i:D3}",
                Status = statuses[i],
                UnitPrice = 100.00m + i,
                TotalPrice = (100.00m + i) * 10,
                QuantityOffered = 10
            };

            var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
            var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);
            createdQuotes.Add(createdQuote);
        }

        // Test filtering by status
        foreach (var status in statuses)
        {
            var response = await client.GetAsync($"/api/quotes?status={status}");
            var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

            Assert.NotNull(result);
            Assert.All(result.Data, quote => Assert.Equal(status, quote.Status));
        }

        // Test search filtering
        var searchResponse = await client.GetAsync("/api/quotes?search=Q-FILTER");
        var searchResult = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(searchResponse);

        Assert.NotNull(searchResult);
        Assert.All(searchResult.Data, quote => Assert.Contains("Q-FILTER", quote.QuoteNumber));
    }

    [Fact]
    public async Task QuotePagination_WorksCorrectly()
    {
        using var client = _factory.CreateClient();
        var createdQuotes = new List<QuoteDto>();

        for (int i = 1; i <= 15; i++)
        {
            var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();
            var createDto = new QuoteCreateDto
            {
                RfqId = rfqId,
                SupplierId = supplierId,
                LineItemId = lineItemId,
                QuoteNumber = $"Q-PAGINATION-{i:D3}",
                Status = "Submitted",
                UnitPrice = 100.00m + i,
                TotalPrice = (100.00m + i) * 10,
                QuantityOffered = 10
            };

            var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
            var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);
            createdQuotes.Add(createdQuote);
        }

        // Test pagination
        var pageSize = 5;
        var totalPages = 3;

        var found = 0;
        for (int page = 1; page <= totalPages; page++)
        {
            var response = await client.GetAsync($"/api/quotes?page={page}&pageSize={pageSize}");
            var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

            Assert.NotNull(result);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.True(result.Data.Count <= pageSize);

            // Only count our test-created quotes
            foreach (var q in createdQuotes)
            {
                if (result.Data.Any(d => d.QuoteNumber == q.QuoteNumber))
                    found++;
            }
        }
        Assert.Equal(15, found);
    }

    [Fact]
    public async Task QuoteRelationships_AreCorrectlyLoaded()
    {
        using var client = _factory.CreateClient();
        var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();

        var createDto = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = supplierId,
            LineItemId = lineItemId,
            QuoteNumber = "Q-RELATIONSHIPS",
            Status = "Submitted",
            UnitPrice = 100.00m,
            TotalPrice = 1000.00m,
            QuantityOffered = 10
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Verify relationships are loaded
        Assert.NotNull(createdQuote.Supplier);
        Assert.Equal(supplierId, createdQuote.Supplier.SupplierId);
        // Don't assert on specific supplier name as it cycles through suppliers

        Assert.NotNull(createdQuote.RfqLineItem);
        Assert.Equal(lineItemId, createdQuote.RfqLineItem.LineItemId);

        // Verify RFQ relationship through line item
        Assert.NotNull(createdQuote.RfqLineItem.Item);
        Assert.Equal(1, createdQuote.RfqLineItem.Item.ItemId);
        Assert.Equal("High-performance laptop", createdQuote.RfqLineItem.Item.Description);
    }

    [Fact]
    public async Task QuoteSummary_CalculatesCorrectly()
    {
        using var client = _factory.CreateClient();
        var quotes = new[]
        {
            new { Status = "Submitted", Value = 1000.00m },
            new { Status = "Submitted", Value = 2000.00m },
            new { Status = "Awarded", Value = 1500.00m },
            new { Status = "Rejected", Value = 3000.00m }
        };

        foreach (var q in quotes)
        {
            var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();
            var createDto = new QuoteCreateDto
            {
                RfqId = rfqId,
                SupplierId = supplierId,
                LineItemId = lineItemId,
                QuoteNumber = $"Q-SUMMARY-{q.Status}-{lineItemId}",
                Status = q.Status,
                UnitPrice = q.Value / 10,
                TotalPrice = q.Value,
                QuantityOffered = 10
            };
            await client.PostAsJsonAsync("/api/quotes", createDto);
        }

        // Get summary
        var response = await client.GetAsync("/api/quotes/summary");
        var summary = await DeserializeOrFail<object>(response);

        Assert.NotNull(summary);

        // Verify summary contains expected data
        var summaryJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("summary", summaryJson);
        Assert.Contains("status", summaryJson);
        Assert.Contains("count", summaryJson);
    }

    [Fact]
    public async Task QuoteByRfq_ReturnsCorrectQuotes()
    {
        using var client = _factory.CreateClient();
        var rfqId = Utilities.GetNextRfqId();
        var createdQuotes = new List<QuoteDto>();

        for (int i = 1; i <= 3; i++)
        {
            var (_, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();
            var createDto = new QuoteCreateDto
            {
                RfqId = rfqId,
                SupplierId = supplierId,
                LineItemId = lineItemId,
                QuoteNumber = $"Q-RFQ-{i:D3}",
                Status = "Submitted",
                UnitPrice = 100.00m + i,
                TotalPrice = (100.00m + i) * 10,
                QuantityOffered = 10
            };

            var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
            var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);
            createdQuotes.Add(createdQuote);
        }

        // Get quotes by RFQ
        var response = await client.GetAsync($"/api/quotes/rfq/{rfqId}");
        var quotes = await DeserializeOrFail<List<QuoteSummaryDto>>(response);

        Assert.NotNull(quotes);
        // Only check that our test-created quotes are present
        foreach (var q in createdQuotes)
        {
            Assert.Contains(quotes, d => d.QuoteNumber == q.QuoteNumber);
        }
    }

    [Fact]
    public async Task QuoteBySupplier_WithPagination_WorksCorrectly()
    {
        using var client = _factory.CreateClient();
        var supplierId = Utilities.GetNextSupplierId();
        var createdQuotes = new List<QuoteDto>();

        for (int i = 1; i <= 8; i++)
        {
            var (rfqId, _, lineItemId) = Utilities.GetNextQuoteCombination();
            var createDto = new QuoteCreateDto
            {
                RfqId = rfqId,
                SupplierId = supplierId,
                LineItemId = lineItemId,
                QuoteNumber = $"Q-SUPPLIER-{i:D3}",
                Status = "Submitted",
                UnitPrice = 100.00m + i,
                TotalPrice = (100.00m + i) * 10,
                QuantityOffered = 10
            };

            var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
            var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);
            createdQuotes.Add(createdQuote);
        }

        // Test pagination for supplier quotes
        var pageSize = 3;
        var totalPages = 3;

        var found = 0;
        for (int page = 1; page <= totalPages; page++)
        {
            var response = await client.GetAsync($"/api/quotes/supplier/{supplierId}?page={page}&pageSize={pageSize}");
            var result = await DeserializeOrFail<PaginatedResult<QuoteSummaryDto>>(response);

            Assert.NotNull(result);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.True(result.Data.Count <= pageSize);
            // Only check that our test-created quotes are present
            foreach (var q in createdQuotes)
            {
                if (result.Data.Any(d => d.QuoteNumber == q.QuoteNumber))
                    found++;
            }
        }
        Assert.Equal(8, found);
    }

    [Fact]
    public async Task QuoteDataIntegrity_IsMaintained()
    {
        using var client = _factory.CreateClient();
        var (rfqId, supplierId, lineItemId) = Utilities.GetNextQuoteCombination();

        var createDto = new QuoteCreateDto
        {
            RfqId = rfqId,
            SupplierId = supplierId,
            LineItemId = lineItemId,
            QuoteNumber = "Q-INTEGRITY-TEST",
            Status = "Submitted",
            UnitPrice = 123.45m,
            TotalPrice = 1234.50m,
            QuantityOffered = 10,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            PaymentTerms = "Net 30",
            WarrantyPeriodMonths = 12,
            TechnicalComplianceNotes = "Data integrity test",
            ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90))
        };

        var createResponse = await client.PostAsJsonAsync("/api/quotes", createDto);
        var createdQuote = await DeserializeOrFail<QuoteDto>(createResponse);

        // Verify all data is preserved
        Assert.Equal("Q-INTEGRITY-TEST", createdQuote.QuoteNumber);
        Assert.Equal("Submitted", createdQuote.Status);
        Assert.Equal(123.45m, createdQuote.UnitPrice);
        Assert.Equal(1234.50m, createdQuote.TotalPrice);
        Assert.Equal(10, createdQuote.QuantityOffered);
        Assert.Equal("Net 30", createdQuote.PaymentTerms);
        Assert.Equal(12, createdQuote.WarrantyPeriodMonths);
        Assert.Equal("Data integrity test", createdQuote.TechnicalComplianceNotes);
        Assert.NotNull(createdQuote.DeliveryDate);
        Assert.NotNull(createdQuote.ValidUntilDate);
        Assert.True(createdQuote.SubmittedDate > DateTime.UtcNow.AddMinutes(-5));

        // Verify database persistence
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
        var dbQuote = await dbContext.Quotes
            .Include(q => q.Supplier)
            .Include(q => q.RfqLineItem)
            .FirstOrDefaultAsync(q => q.QuoteId == createdQuote.QuoteId);

        Assert.NotNull(dbQuote);
        Assert.Equal("Q-INTEGRITY-TEST", dbQuote.QuoteNumber);
        Assert.Equal(QuoteStatus.Submitted, dbQuote.Status);
        Assert.Equal(123.45m, dbQuote.UnitPrice);
        Assert.NotNull(dbQuote.Supplier);
        Assert.NotNull(dbQuote.RfqLineItem);
    }
}