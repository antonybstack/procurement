using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RfqsController : ControllerBase
{
    private readonly ProcurementDbContext _context;
    private readonly ILogger<RfqsController> _logger;

    public RfqsController(ProcurementDbContext context, ILogger<RfqsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all RFQs with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<RfqDto>>> GetRfqs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

            var query = _context.RequestForQuotes.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(rfq =>
                    rfq.Title.Contains(search) ||
                    rfq.RfqNumber.Contains(search) ||
                    rfq.Description!.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RfqStatus>(status, true, out var statusEnum))
            {
                query = query.Where(rfq => rfq.Status == statusEnum);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(rfq => rfq.IssueDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(rfq => rfq.IssueDate <= toDate.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination and include related data
            var rfqs = await query
                .OrderByDescending(rfq => rfq.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(rfq => new RfqDto
                {
                    RfqId = rfq.RfqId,
                    RfqNumber = rfq.RfqNumber,
                    Title = rfq.Title,
                    Description = rfq.Description,
                    Status = rfq.Status.ToString(),
                    IssueDate = rfq.IssueDate,
                    DueDate = rfq.DueDate,
                    AwardDate = rfq.AwardDate,
                    TotalEstimatedValue = rfq.TotalEstimatedValue,
                    Currency = rfq.Currency,
                    CreatedBy = rfq.CreatedBy,
                    CreatedAt = rfq.CreatedAt,
                    LineItemsCount = rfq.RfqLineItems.Count,
                    SuppliersInvited = rfq.RfqSuppliers.Count,
                    QuotesReceived = rfq.Quotes.Count
                })
                .ToListAsync();

            var result = new PaginatedResult<RfqDto>
            {
                Data = rfqs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving RFQs");
            return StatusCode(500, "An error occurred while retrieving RFQs");
        }
    }

    /// <summary>
    /// Get a specific RFQ by ID with full details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RfqDetailDto>> GetRfq(int id)
    {
        try
        {
            var rfq = await _context.RequestForQuotes
                .Where(rfq => rfq.RfqId == id)
                .Select(rfq => new RfqDetailDto
                {
                    RfqId = rfq.RfqId,
                    RfqNumber = rfq.RfqNumber,
                    Title = rfq.Title,
                    Description = rfq.Description,
                    Status = rfq.Status.ToString(),
                    IssueDate = rfq.IssueDate,
                    DueDate = rfq.DueDate,
                    AwardDate = rfq.AwardDate,
                    TotalEstimatedValue = rfq.TotalEstimatedValue,
                    Currency = rfq.Currency,
                    CreatedBy = rfq.CreatedBy,
                    CreatedAt = rfq.CreatedAt,
                    LineItemsCount = rfq.RfqLineItems.Count,
                    SuppliersInvited = rfq.RfqSuppliers.Count,
                    QuotesReceived = rfq.Quotes.Count,
                    LineItems = rfq.RfqLineItems.Select(rli => new RfqLineItemDto
                    {
                        LineItemId = rli.LineItemId,
                        LineNumber = rli.LineNumber,
                        QuantityRequired = rli.QuantityRequired,
                        UnitOfMeasure = rli.UnitOfMeasure,
                        Description = rli.Description,
                        DeliveryDate = rli.DeliveryDate,
                        EstimatedUnitCost = rli.EstimatedUnitCost,
                        Item = rli.Item != null ? new ItemDto
                        {
                            ItemId = rli.Item.ItemId,
                            ItemCode = rli.Item.ItemCode,
                            Description = rli.Item.Description,
                            Category = rli.Item.Category.ToString(),
                            UnitOfMeasure = rli.Item.UnitOfMeasure,
                            StandardCost = rli.Item.StandardCost,
                            MinOrderQuantity = rli.Item.MinOrderQuantity,
                            LeadTimeDays = rli.Item.LeadTimeDays,
                            IsActive = rli.Item.IsActive,
                            CreatedAt = rli.Item.CreatedAt
                        } : null
                    }).ToList(),
                    InvitedSuppliers = rfq.RfqSuppliers.Select(rs => new SupplierDto
                    {
                        SupplierId = rs.Supplier.SupplierId,
                        SupplierCode = rs.Supplier.SupplierCode,
                        CompanyName = rs.Supplier.CompanyName,
                        ContactName = rs.Supplier.ContactName,
                        Email = rs.Supplier.Email,
                        Phone = rs.Supplier.Phone,
                        City = rs.Supplier.City,
                        State = rs.Supplier.State,
                        Country = rs.Supplier.Country,
                        Rating = rs.Supplier.Rating,
                        IsActive = rs.Supplier.IsActive,
                        CreatedAt = rs.Supplier.CreatedAt
                    }).ToList(),
                    Quotes = rfq.Quotes.Select(q => new QuoteDto
                    {
                        QuoteId = q.QuoteId,
                        QuoteNumber = q.QuoteNumber,
                        Status = q.Status.ToString(),
                        UnitPrice = q.UnitPrice,
                        TotalPrice = q.TotalPrice,
                        QuantityOffered = q.QuantityOffered,
                        DeliveryDate = q.DeliveryDate,
                        PaymentTerms = q.PaymentTerms,
                        WarrantyPeriodMonths = q.WarrantyPeriodMonths,
                        SubmittedDate = q.SubmittedDate,
                        ValidUntilDate = q.ValidUntilDate,
                        Supplier = new SupplierDto
                        {
                            SupplierId = q.Supplier.SupplierId,
                            SupplierCode = q.Supplier.SupplierCode,
                            CompanyName = q.Supplier.CompanyName,
                            ContactName = q.Supplier.ContactName,
                            Email = q.Supplier.Email,
                            Phone = q.Supplier.Phone,
                            City = q.Supplier.City,
                            State = q.Supplier.State,
                            Country = q.Supplier.Country,
                            Rating = q.Supplier.Rating,
                            IsActive = q.Supplier.IsActive,
                            CreatedAt = q.Supplier.CreatedAt
                        },
                        RfqLineItem = new RfqLineItemDto
                        {
                            LineItemId = q.RfqLineItem.LineItemId,
                            LineNumber = q.RfqLineItem.LineNumber,
                            QuantityRequired = q.RfqLineItem.QuantityRequired,
                            UnitOfMeasure = q.RfqLineItem.UnitOfMeasure,
                            Description = q.RfqLineItem.Description,
                            DeliveryDate = q.RfqLineItem.DeliveryDate,
                            EstimatedUnitCost = q.RfqLineItem.EstimatedUnitCost
                        }
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (rfq == null)
            {
                return NotFound($"RFQ with ID {id} not found");
            }

            return Ok(rfq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving RFQ with ID {RfqId}", id);
            return StatusCode(500, "An error occurred while retrieving the RFQ");
        }
    }

    /// <summary>
    /// Get RFQ statuses for filtering
    /// </summary>
    [HttpGet("statuses")]
    public ActionResult<List<string>> GetStatuses()
    {
        return Ok(Enum.GetNames<RfqStatus>().ToList());
    }

    /// <summary>
    /// Get RFQ summary statistics
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetRfqSummary()
    {
        try
        {
            var summary = await _context.RequestForQuotes
                .GroupBy(rfq => rfq.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    TotalValue = g.Sum(rfq => rfq.TotalEstimatedValue ?? 0)
                })
                .ToListAsync();

            var totalRfqs = await _context.RequestForQuotes.CountAsync();
            var totalQuotes = await _context.Quotes.CountAsync();
            var totalSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive);

            return Ok(new
            {
                Summary = summary,
                TotalRfqs = totalRfqs,
                TotalQuotes = totalQuotes,
                TotalSuppliers = totalSuppliers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving RFQ summary");
            return StatusCode(500, "An error occurred while retrieving RFQ summary");
        }
    }
}