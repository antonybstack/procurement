# RAG POC MVP: Database Query Functions

## Goal
Enable AI chat to answer procurement questions using PostgreSQL data with minimal development effort (1-2 days).

## Target Questions
1. "What products are available in our catalog?" → Items table
2. "Find suppliers for electronic components" → Suppliers + SupplierCapabilities  
3. "Show me recent RFQ activity" → RequestForQuotes table
4. "What are the top performing suppliers?" → Supplier performance query
5. "Help me find procurement best practices" → Keep existing document search

## Implementation Plan

### Step 1: Create ProcurementDataService (2 hours)

**Interface:**
```csharp
// Services/IProcurementDataService.cs
public interface IProcurementDataService
{
    Task<string> GetCatalogItemsAsync(string? category = null, int limit = 20);
    Task<string> FindSuppliersAsync(string? requirements = null, string? location = null, int minRating = 1, int limit = 20);
    Task<string> GetRecentRFQsAsync(int days = 30, string? status = null, int limit = 20);
    Task<string> GetTopSuppliersAsync(int limit = 10);
    Task<string> GetSupplierDetailsAsync(int supplierId);
}
```

**Key Implementation Points:**
- Use existing ProcurementDbContext
- Return JSON serialized results
- Include error handling
- Filter by IsActive = true
- Use Include() for related data (SupplierCapabilities)

### Step 2: Update ChatController (2 hours)

**Add Function Tools:**
```csharp
var chatOptions = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(SearchAsync),              // Keep existing
        AIFunctionFactory.Create(GetCatalogItemsAsync),     // New
        AIFunctionFactory.Create(FindSuppliersAsync),       // New  
        AIFunctionFactory.Create(GetRecentRFQsAsync),       // New
        AIFunctionFactory.Create(GetTopSuppliersAsync),     // New
        AIFunctionFactory.Create(GetSupplierDetailsAsync)   // New
    ]
};
```

**Update System Prompt:**
```csharp
private string GetSystemPrompt()
{
    return """
    You are a procurement assistant with access to:
    - Product catalog (items by category)
    - Supplier database with capabilities
    - RFQ activity and status
    - Supplier performance metrics
    - Procurement documents
    
    Categories: electronics, machinery, raw_materials, packaging, services, components
    RFQ statuses: draft, published, closed, awarded, cancelled
    """;
}
```

### Step 3: Register Service (5 minutes)
```csharp
// Program.cs
builder.Services.AddScoped<IProcurementDataService, ProcurementDataService>();
```

### Step 4: Update Streaming Endpoint (1 hour)
Apply same function tools to the streaming completion endpoint.

## Quick Implementation Queries

### GetCatalogItemsAsync
```csharp
var items = await _context.Items
    .Where(i => i.IsActive && (category == null || i.Category.ToString() == category))
    .OrderBy(i => i.ItemCode)
    .Take(limit)
    .Select(i => new { i.ItemId, i.ItemCode, i.Description, Category = i.Category.ToString(), i.StandardCost })
    .ToListAsync();
```

### FindSuppliersAsync  
```csharp
var suppliers = await _context.Suppliers
    .Where(s => s.IsActive && s.Rating >= minRating)
    .Include(s => s.SupplierCapabilities)
    .Where(s => requirements == null || 
        s.CompanyName.Contains(requirements) ||
        s.SupplierCapabilities.Any(sc => sc.CapabilityValue.Contains(requirements)))
    .OrderByDescending(s => s.Rating)
    .Take(limit)
    .ToListAsync();
```

### GetRecentRFQsAsync
```csharp
var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
var rfqs = await _context.RequestForQuotes
    .Where(rfq => rfq.IssueDate >= cutoffDate)
    .Where(rfq => status == null || rfq.Status.ToString() == status)
    .OrderByDescending(rfq => rfq.IssueDate)
    .Take(limit)
    .ToListAsync();
```

### GetTopSuppliersAsync
```csharp
// Use supplier_performance view or calculate on-the-fly
var topSuppliers = await _context.Database
    .SqlQueryRaw<dynamic>(@"
        SELECT s.supplier_id, s.company_name, s.rating, s.country,
               COUNT(q.quote_id) as total_quotes,
               COUNT(CASE WHEN q.status = 'awarded' THEN 1 END) as awarded_quotes
        FROM suppliers s 
        LEFT JOIN quotes q ON s.supplier_id = q.supplier_id
        WHERE s.is_active = true
        GROUP BY s.supplier_id, s.company_name, s.rating, s.country
        ORDER BY s.rating DESC, awarded_quotes DESC
        LIMIT {0}", limit)
    .ToListAsync();
```

## Testing Checklist

### Functional Tests
- [ ] "What products are available?" returns items
- [ ] "Find suppliers for electronics" filters correctly  
- [ ] "Recent RFQ activity" shows time-filtered results
- [ ] "Top suppliers" returns ranked list
- [ ] Document search still works for best practices

### Technical Tests
- [ ] All function tools registered correctly
- [ ] JSON serialization works
- [ ] Error handling prevents crashes
- [ ] Response times < 3 seconds
- [ ] Database connections properly managed

## Deployment Steps

1. **Code Changes** (30 min)
   - Add IProcurementDataService.cs
   - Add ProcurementDataService.cs  
   - Update ChatController.cs
   - Update Program.cs

2. **Testing** (1 hour)
   - Test each function individually
   - Test chat integration
   - Verify sample data exists

3. **Validation** (30 min)
   - Test all 5 target questions
   - Check performance
   - Document any issues

## Success Criteria

**Day 1:**
- ✅ All 5 question types answered
- ✅ No function tool errors
- ✅ Response time < 3 seconds

**Week 1:**  
- 90% query success rate
- Positive user feedback
- Stable performance

## Files to Create/Modify

1. **NEW**: `ProcurementAPI/Services/IProcurementDataService.cs`
2. **NEW**: `ProcurementAPI/Services/DataServices/ProcurementDataService.cs`
3. **MODIFY**: `ProcurementAPI/Controllers/ChatController.cs`
4. **MODIFY**: `ProcurementAPI/Program.cs`

**Total Effort: 6-8 hours over 1-2 days**

This focused MVP delivers immediate value while establishing the foundation for more sophisticated RAG capabilities later.
