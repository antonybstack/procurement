using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly ProcurementDbContext _context;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(ProcurementDbContext context, ILogger<ItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all items with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ItemDto>>> GetItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

            var query = _context.Items.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Description.Contains(search) ||
                    i.ItemCode.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ItemCategory>(category, true, out var categoryEnum))
            {
                query = query.Where(i => i.Category == categoryEnum);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(i => i.Description)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ItemDto
                {
                    ItemId = i.ItemId,
                    ItemCode = i.ItemCode,
                    Description = i.Description,
                    Category = i.Category.ToString(),
                    UnitOfMeasure = i.UnitOfMeasure,
                    StandardCost = i.StandardCost,
                    MinOrderQuantity = i.MinOrderQuantity,
                    LeadTimeDays = i.LeadTimeDays,
                    IsActive = i.IsActive,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            var result = new PaginatedResult<ItemDto>
            {
                Data = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items");
            return StatusCode(500, "An error occurred while retrieving items");
        }
    }

    /// <summary>
    /// Get a specific item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetItem(int id)
    {
        try
        {
            var item = await _context.Items
                .Where(i => i.ItemId == id)
                .Select(i => new ItemDto
                {
                    ItemId = i.ItemId,
                    ItemCode = i.ItemCode,
                    Description = i.Description,
                    Category = i.Category.ToString(),
                    UnitOfMeasure = i.UnitOfMeasure,
                    StandardCost = i.StandardCost,
                    MinOrderQuantity = i.MinOrderQuantity,
                    LeadTimeDays = i.LeadTimeDays,
                    IsActive = i.IsActive,
                    CreatedAt = i.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound($"Item with ID {id} not found");
            }

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item with ID {ItemId}", id);
            return StatusCode(500, "An error occurred while retrieving the item");
        }
    }

    /// <summary>
    /// Get categories for filtering
    /// </summary>
    [HttpGet("categories")]
    public ActionResult<List<string>> GetCategories()
    {
        try
        {
            var categories = Enum.GetValues<ItemCategory>()
                .Select(c => c.ToString())
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }
}