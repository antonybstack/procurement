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

    /// <summary>
    /// Create a new item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem([FromBody] ItemCreateDto createDto)
    {
        try
        {
            // Validate category
            if (!Enum.TryParse<ItemCategory>(createDto.Category, true, out var category))
            {
                return BadRequest($"Invalid category: {createDto.Category}");
            }

            // Check if item code already exists
            var existingItem = await _context.Items
                .FirstOrDefaultAsync(i => i.ItemCode == createDto.ItemCode);
            if (existingItem != null)
            {
                return BadRequest($"Item with code {createDto.ItemCode} already exists");
            }

            var item = new Item
            {
                ItemCode = createDto.ItemCode,
                Description = createDto.Description,
                Category = category,
                UnitOfMeasure = createDto.UnitOfMeasure,
                StandardCost = createDto.StandardCost,
                MinOrderQuantity = createDto.MinOrderQuantity,
                LeadTimeDays = createDto.LeadTimeDays,
                IsActive = createDto.IsActive
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Return the created item
            return await GetItem(item.ItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            return StatusCode(500, "An error occurred while creating the item");
        }
    }

    /// <summary>
    /// Update an existing item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int id, [FromBody] ItemUpdateDto updateDto)
    {
        try
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound($"Item with ID {id} not found");
            }

            // Validate category
            if (!Enum.TryParse<ItemCategory>(updateDto.Category, true, out var category))
            {
                return BadRequest($"Invalid category: {updateDto.Category}");
            }

            // Check if item code already exists (excluding current item)
            var existingItem = await _context.Items
                .FirstOrDefaultAsync(i => i.ItemCode == updateDto.ItemCode && i.ItemId != id);
            if (existingItem != null)
            {
                return BadRequest($"Item with code {updateDto.ItemCode} already exists");
            }

            // Update fields
            item.ItemCode = updateDto.ItemCode;
            item.Description = updateDto.Description;
            item.Category = category;
            item.UnitOfMeasure = updateDto.UnitOfMeasure;
            item.StandardCost = updateDto.StandardCost;
            item.MinOrderQuantity = updateDto.MinOrderQuantity;
            item.LeadTimeDays = updateDto.LeadTimeDays;
            item.IsActive = updateDto.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} updated successfully", id);
            return await GetItem(id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating item {ItemId}", id);
            return Conflict("The item was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId}", id);
            return StatusCode(500, "An error occurred while updating the item");
        }
    }

    /// <summary>
    /// Delete an item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteItem(int id)
    {
        try
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound($"Item with ID {id} not found");
            }

            // Check if item is referenced by any RFQ line items
            var hasReferences = await _context.RfqLineItems
                .AnyAsync(rli => rli.ItemId == id);
            if (hasReferences)
            {
                return BadRequest("Cannot delete item that is referenced by RFQ line items");
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} deleted successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {ItemId}", id);
            return StatusCode(500, "An error occurred while deleting the item");
        }
    }

    /// <summary>
    /// Get item summary statistics
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetItemSummary()
    {
        try
        {
            var summary = await _context.Items
                .GroupBy(i => i.Category)
                .Select(g => new
                {
                    Category = g.Key.ToString(),
                    Count = g.Count(),
                    ActiveCount = g.Count(i => i.IsActive),
                    AvgStandardCost = g.Average(i => i.StandardCost ?? 0)
                })
                .ToListAsync();

            var totalItems = await _context.Items.CountAsync();
            var activeItems = await _context.Items.CountAsync(i => i.IsActive);
            var avgLeadTime = await _context.Items.AverageAsync(i => i.LeadTimeDays);

            return Ok(new
            {
                Summary = summary,
                TotalItems = totalItems,
                ActiveItems = activeItems,
                AverageLeadTime = avgLeadTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item summary");
            return StatusCode(500, "An error occurred while retrieving item summary");
        }
    }
}