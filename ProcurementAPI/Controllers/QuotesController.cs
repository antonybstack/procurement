using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly ProcurementDbContext _context;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(ProcurementDbContext context, ILogger<QuotesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all quotes with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<QuoteSummaryDto>>> GetQuotes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int? rfqId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

            var query = _context.Quotes
                .Include(q => q.Supplier)
                .Include(q => q.RequestForQuote)
                .Include(q => q.RfqLineItem)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q =>
                    q.QuoteNumber.Contains(search) ||
                    q.Supplier.CompanyName.Contains(search) ||
                    q.RequestForQuote.RfqNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<QuoteStatus>(status, true, out var statusEnum))
            {
                query = query.Where(q => q.Status == statusEnum);
            }

            if (rfqId.HasValue)
            {
                query = query.Where(q => q.RfqId == rfqId.Value);
            }

            if (supplierId.HasValue)
            {
                query = query.Where(q => q.SupplierId == supplierId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(q => q.SubmittedDate >= fromDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            if (toDate.HasValue)
            {
                query = query.Where(q => q.SubmittedDate <= toDate.Value.ToDateTime(TimeOnly.MaxValue));
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var quotes = await query
                .OrderByDescending(q => q.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs manually to avoid SQL generation issues
            var quoteDtos = quotes.Select(q => new QuoteSummaryDto
            {
                QuoteId = q.QuoteId,
                QuoteNumber = q.QuoteNumber,
                Status = q.Status.ToString(),
                UnitPrice = q.UnitPrice,
                TotalPrice = q.TotalPrice,
                QuantityOffered = q.QuantityOffered,
                DeliveryDate = q.DeliveryDate,
                SubmittedDate = q.SubmittedDate,
                SupplierName = q.Supplier.CompanyName,
                RfqNumber = q.RequestForQuote.RfqNumber,
                ItemDescription = q.RfqLineItem.Description ?? "No description"
            }).ToList();

            var result = new PaginatedResult<QuoteSummaryDto>
            {
                Data = quoteDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quotes");
            return StatusCode(500, "An error occurred while retrieving quotes");
        }
    }

    /// <summary>
    /// Get a specific quote by ID with full details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QuoteDto>> GetQuote(int id)
    {
        try
        {
            var quote = await _context.Quotes
                .Where(q => q.QuoteId == id)
                .Include(q => q.Supplier)
                .Include(q => q.RequestForQuote)
                .Include(q => q.RfqLineItem)
                    .ThenInclude(rli => rli.Item)
                .FirstOrDefaultAsync();

            if (quote == null)
            {
                return NotFound($"Quote with ID {id} not found");
            }

            var quoteDto = new QuoteDto
            {
                QuoteId = quote.QuoteId,
                RfqId = quote.RfqId,
                SupplierId = quote.SupplierId,
                LineItemId = quote.LineItemId,
                QuoteNumber = quote.QuoteNumber,
                Status = quote.Status.ToString(),
                UnitPrice = quote.UnitPrice,
                TotalPrice = quote.TotalPrice,
                QuantityOffered = quote.QuantityOffered,
                DeliveryDate = quote.DeliveryDate,
                PaymentTerms = quote.PaymentTerms,
                WarrantyPeriodMonths = quote.WarrantyPeriodMonths,
                TechnicalComplianceNotes = quote.TechnicalComplianceNotes,
                SubmittedDate = quote.SubmittedDate,
                ValidUntilDate = quote.ValidUntilDate,
                CreatedAt = quote.CreatedAt,
                Supplier = new SupplierDto
                {
                    SupplierId = quote.Supplier.SupplierId,
                    SupplierCode = quote.Supplier.SupplierCode,
                    CompanyName = quote.Supplier.CompanyName,
                    ContactName = quote.Supplier.ContactName,
                    Email = quote.Supplier.Email,
                    Phone = quote.Supplier.Phone,
                    Address = quote.Supplier.Address,
                    City = quote.Supplier.City,
                    State = quote.Supplier.State,
                    Country = quote.Supplier.Country,
                    PostalCode = quote.Supplier.PostalCode,
                    TaxId = quote.Supplier.TaxId,
                    PaymentTerms = quote.Supplier.PaymentTerms,
                    CreditLimit = quote.Supplier.CreditLimit,
                    Rating = quote.Supplier.Rating,
                    IsActive = quote.Supplier.IsActive,
                    CreatedAt = quote.Supplier.CreatedAt
                },
                RfqLineItem = new RfqLineItemDto
                {
                    LineItemId = quote.RfqLineItem.LineItemId,
                    LineNumber = quote.RfqLineItem.LineNumber,
                    QuantityRequired = quote.RfqLineItem.QuantityRequired,
                    UnitOfMeasure = quote.RfqLineItem.UnitOfMeasure,
                    Description = quote.RfqLineItem.Description,
                    DeliveryDate = quote.RfqLineItem.DeliveryDate,
                    EstimatedUnitCost = quote.RfqLineItem.EstimatedUnitCost,
                    Item = quote.RfqLineItem.Item != null ? new ItemDto
                    {
                        ItemId = quote.RfqLineItem.Item.ItemId,
                        ItemCode = quote.RfqLineItem.Item.ItemCode,
                        Description = quote.RfqLineItem.Item.Description,
                        Category = quote.RfqLineItem.Item.Category.ToString(),
                        UnitOfMeasure = quote.RfqLineItem.Item.UnitOfMeasure,
                        StandardCost = quote.RfqLineItem.Item.StandardCost,
                        MinOrderQuantity = quote.RfqLineItem.Item.MinOrderQuantity,
                        LeadTimeDays = quote.RfqLineItem.Item.LeadTimeDays,
                        IsActive = quote.RfqLineItem.Item.IsActive,
                        CreatedAt = quote.RfqLineItem.Item.CreatedAt
                    } : null
                }
            };

            return Ok(quoteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quote with ID {QuoteId}", id);
            return StatusCode(500, "An error occurred while retrieving the quote");
        }
    }

    /// <summary>
    /// Create a new quote
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuoteDto>> CreateQuote([FromBody] QuoteCreateDto createDto)
    {
        try
        {
            // Validate that RFQ, supplier, and line item exist
            var rfq = await _context.RequestForQuotes.FindAsync(createDto.RfqId);
            if (rfq == null)
            {
                return BadRequest($"RFQ with ID {createDto.RfqId} not found");
            }

            var supplier = await _context.Suppliers.FindAsync(createDto.SupplierId);
            if (supplier == null)
            {
                return BadRequest($"Supplier with ID {createDto.SupplierId} not found");
            }

            var lineItem = await _context.RfqLineItems.FindAsync(createDto.LineItemId);
            if (lineItem == null)
            {
                return BadRequest($"RFQ Line Item with ID {createDto.LineItemId} not found");
            }

            // Parse status
            if (!Enum.TryParse<QuoteStatus>(createDto.Status, true, out var status))
            {
                return BadRequest($"Invalid status: {createDto.Status}");
            }

            var quote = new Quote
            {
                RfqId = createDto.RfqId,
                SupplierId = createDto.SupplierId,
                LineItemId = createDto.LineItemId,
                QuoteNumber = createDto.QuoteNumber,
                Status = status,
                UnitPrice = createDto.UnitPrice,
                TotalPrice = createDto.TotalPrice,
                QuantityOffered = createDto.QuantityOffered,
                DeliveryDate = createDto.DeliveryDate,
                PaymentTerms = createDto.PaymentTerms,
                WarrantyPeriodMonths = createDto.WarrantyPeriodMonths,
                TechnicalComplianceNotes = createDto.TechnicalComplianceNotes,
                ValidUntilDate = createDto.ValidUntilDate,
                SubmittedDate = DateTime.UtcNow
            };

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            // Return the created quote
            return await GetQuote(quote.QuoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote");
            return StatusCode(500, "An error occurred while creating the quote");
        }
    }

    /// <summary>
    /// Update an existing quote
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<QuoteDto>> UpdateQuote(int id, [FromBody] QuoteUpdateDto updateDto)
    {
        try
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null)
            {
                return NotFound($"Quote with ID {id} not found");
            }

            // Parse status
            if (!Enum.TryParse<QuoteStatus>(updateDto.Status, true, out var status))
            {
                return BadRequest($"Invalid status: {updateDto.Status}");
            }

            // Update fields
            quote.QuoteNumber = updateDto.QuoteNumber;
            quote.Status = status;
            quote.UnitPrice = updateDto.UnitPrice;
            quote.TotalPrice = updateDto.TotalPrice;
            quote.QuantityOffered = updateDto.QuantityOffered;
            quote.DeliveryDate = updateDto.DeliveryDate;
            quote.PaymentTerms = updateDto.PaymentTerms;
            quote.WarrantyPeriodMonths = updateDto.WarrantyPeriodMonths;
            quote.TechnicalComplianceNotes = updateDto.TechnicalComplianceNotes;
            quote.ValidUntilDate = updateDto.ValidUntilDate;
            quote.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Quote {QuoteId} updated successfully", id);
            return await GetQuote(id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating quote {QuoteId}", id);
            return Conflict("The quote was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quote {QuoteId}", id);
            return StatusCode(500, "An error occurred while updating the quote");
        }
    }

    /// <summary>
    /// Delete a quote
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuote(int id)
    {
        try
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null)
            {
                return NotFound($"Quote with ID {id} not found");
            }

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Quote {QuoteId} deleted successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quote {QuoteId}", id);
            return StatusCode(500, "An error occurred while deleting the quote");
        }
    }

    /// <summary>
    /// Get quote statuses for filtering
    /// </summary>
    [HttpGet("statuses")]
    public ActionResult<List<string>> GetStatuses()
    {
        return Ok(Enum.GetNames<QuoteStatus>().ToList());
    }

    /// <summary>
    /// Get quotes by RFQ ID
    /// </summary>
    [HttpGet("rfq/{rfqId}")]
    public async Task<ActionResult<List<QuoteSummaryDto>>> GetQuotesByRfq(int rfqId)
    {
        try
        {
            var quotes = await _context.Quotes
                .Where(q => q.RfqId == rfqId)
                .Include(q => q.Supplier)
                .Include(q => q.RequestForQuote)
                .Include(q => q.RfqLineItem)
                .OrderByDescending(q => q.SubmittedDate)
                .ToListAsync();

            // Map to DTOs manually to avoid SQL generation issues
            var quoteDtos = quotes.Select(q => new QuoteSummaryDto
            {
                QuoteId = q.QuoteId,
                QuoteNumber = q.QuoteNumber,
                Status = q.Status.ToString(),
                UnitPrice = q.UnitPrice,
                TotalPrice = q.TotalPrice,
                QuantityOffered = q.QuantityOffered,
                DeliveryDate = q.DeliveryDate,
                SubmittedDate = q.SubmittedDate,
                SupplierName = q.Supplier.CompanyName,
                RfqNumber = q.RequestForQuote.RfqNumber,
                ItemDescription = q.RfqLineItem.Description ?? "No description"
            }).ToList();

            return Ok(quoteDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quotes for RFQ {RfqId}", rfqId);
            return StatusCode(500, "An error occurred while retrieving quotes for the RFQ");
        }
    }

    /// <summary>
    /// Get quotes by supplier ID
    /// </summary>
    [HttpGet("supplier/{supplierId}")]
    public async Task<ActionResult<PaginatedResult<QuoteSummaryDto>>> GetQuotesBySupplier(
        int supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

            var query = _context.Quotes
                .Where(q => q.SupplierId == supplierId)
                .Include(q => q.Supplier)
                .Include(q => q.RequestForQuote)
                .Include(q => q.RfqLineItem);

            var totalCount = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs manually to avoid SQL generation issues
            var quoteDtos = quotes.Select(q => new QuoteSummaryDto
            {
                QuoteId = q.QuoteId,
                QuoteNumber = q.QuoteNumber,
                Status = q.Status.ToString(),
                UnitPrice = q.UnitPrice,
                TotalPrice = q.TotalPrice,
                QuantityOffered = q.QuantityOffered,
                DeliveryDate = q.DeliveryDate,
                SubmittedDate = q.SubmittedDate,
                SupplierName = q.Supplier.CompanyName,
                RfqNumber = q.RequestForQuote.RfqNumber,
                ItemDescription = q.RfqLineItem.Description ?? "No description"
            }).ToList();

            var result = new PaginatedResult<QuoteSummaryDto>
            {
                Data = quoteDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quotes for supplier {SupplierId}", supplierId);
            return StatusCode(500, "An error occurred while retrieving quotes for the supplier");
        }
    }

    /// <summary>
    /// Get quote summary statistics
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetQuoteSummary()
    {
        try
        {
            var summary = await _context.Quotes
                .GroupBy(q => q.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    TotalValue = g.Sum(q => q.TotalPrice)
                })
                .ToListAsync();

            var totalQuotes = await _context.Quotes.CountAsync();
            var totalValue = await _context.Quotes.SumAsync(q => q.TotalPrice);
            var avgQuoteValue = totalQuotes > 0 ? totalValue / totalQuotes : 0;

            return Ok(new
            {
                Summary = summary,
                TotalQuotes = totalQuotes,
                TotalValue = totalValue,
                AverageQuoteValue = avgQuoteValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quote summary");
            return StatusCode(500, "An error occurred while retrieving quote summary");
        }
    }
}