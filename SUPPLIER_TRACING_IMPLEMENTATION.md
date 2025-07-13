# Supplier Update Process - Comprehensive Tracing Implementation

## Overview

This document describes the comprehensive tracing implementation for the supplier update process, covering all layers from Controller to Service to DataService.

## Architecture

The tracing implementation follows OpenTelemetry best practices with:

- **Custom ActivitySource instances** for different operation types
- **Hierarchical activities** that create a complete trace tree
- **Rich metadata** with tags and events at each level
- **Error tracking** with proper exception handling

## Tracing Structure

### 1. Controller Layer (`SuppliersController.UpdateSupplier`)

**Activity**: `supplier.update` (Server)
**Tags**:
- `supplier.id`: The supplier ID being updated
- `supplier.code`: The supplier code
- `supplier.company_name`: The company name
- `supplier.operation`: "update"
- `http.method`: "PUT"
- `http.route`: "api/Suppliers/{id}"

**Events**:
- `supplier.validation.started`: When validation begins
- `supplier.validation.completed`: When validation succeeds
- `supplier.validation.failed`: When validation fails (with error message)

### 2. Service Layer (`SupplierService.UpdateSupplierAsync`)

**Main Activity**: `supplier.update` (Internal)
**Tags**:
- `supplier.id`: The supplier ID
- `supplier.code`: The supplier code
- `supplier.company_name`: The company name
- `supplier.operation`: "service_update"

**Sub-activities**:

#### 2.1 Check Supplier Exists
**Activity**: `supplier.check_exists` (Internal)
**Tags**:
- `supplier.id`: The supplier ID
- `supplier.operation`: "check_exists"
- `supplier.validation.result`: true/false

#### 2.2 Validate Supplier Code
**Activity**: `supplier.validate` (Internal)
**Tags**:
- `supplier.code`: The supplier code
- `supplier.operation`: "validate_code_uniqueness"
- `supplier.validation.result`: true/false

**Events**:
- `supplier.validation.completed`: When validation succeeds
- `supplier.validation.failed`: When validation fails

#### 2.3 Database Update
**Activity**: `supplier.database.update` (Internal)
**Tags**:
- `supplier.id`: The supplier ID
- `supplier.database.operation`: "update"

**Events**:
- `supplier.database.started`: When database operation begins
- `supplier.database.completed`: When database operation succeeds
- `supplier.database.failed`: When database operation fails

### 3. Data Service Layer (`SupplierDataService`)

#### 3.1 Get Supplier Entity
**Activity**: `database.supplier.get_by_id` (Internal)
**Tags**:
- `database.entity.id`: The supplier ID
- `database.entity.type`: "supplier"
- `database.operation`: "get_by_id"
- `database.query.type`: "select"
- `database.result.count`: 0 or 1
- `supplier.code`: The supplier code (if found)
- `supplier.company_name`: The company name (if found)

**Events**:
- `database.query.started`: When query begins
- `database.query.completed`: When query succeeds
- `database.query.failed`: When query fails

#### 3.2 Check Supplier Code Exists
**Activity**: `database.supplier.check_code_exists` (Internal)
**Tags**:
- `database.entity.type`: "supplier"
- `database.operation`: "check_code_exists"
- `database.query.type`: "select"
- `supplier.code`: The supplier code
- `database.entity.id`: The exclude ID (if provided)
- `database.result.count`: 0 or 1
- `supplier.validation.result`: true/false

**Events**:
- `database.query.started`: When query begins
- `database.query.completed`: When query succeeds
- `database.query.failed`: When query fails

#### 3.3 Update Supplier
**Activity**: `database.supplier.update` (Internal)
**Tags**:
- `database.entity.id`: The supplier ID
- `database.entity.type`: "supplier"
- `database.operation`: "update"
- `database.query.type`: "update"
- `supplier.code`: The supplier code
- `supplier.company_name`: The company name

**Events**:
- `database.transaction.started`: When transaction begins
- `database.query.started`: When query begins
- `database.query.completed`: When query succeeds
- `database.transaction.completed`: When transaction succeeds
- `database.transaction.failed`: When transaction fails

## Trace Flow Example

```
PUT /api/suppliers/123
├── supplier.update (Controller - Server)
│   ├── supplier.update (Service - Internal)
│   │   ├── supplier.check_exists (Service - Internal)
│   │   │   └── database.supplier.get_by_id (DataService - Internal)
│   │   ├── supplier.validate (Service - Internal)
│   │   │   └── database.supplier.check_code_exists (DataService - Internal)
│   │   └── supplier.database.update (Service - Internal)
│   │       └── database.supplier.update (DataService - Internal)
│   └── supplier.update (Controller - Server)
```

## Benefits

1. **Complete Visibility**: Every step of the supplier update process is traced
2. **Performance Monitoring**: Each layer's performance can be measured independently
3. **Error Tracking**: Failures are captured with context at each level
4. **Debugging**: Rich metadata helps identify issues quickly
5. **Observability**: Full trace tree shows the complete request flow

## Usage in Grafana Tempo

The traces will appear in Grafana Tempo with:

- **Service Name**: `procurement-api`
- **Operation Names**: Various supplier and database operations
- **Tags**: Rich metadata for filtering and analysis
- **Events**: Timeline of important milestones
- **Error Information**: Detailed error context when failures occur

## Configuration

The tracing is configured in `Program.cs`:

```csharp
.WithTracing(tracing => tracing
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddNpgsql()
    .AddSource(TracingExtensions.SupplierActivitySource.Name)
    .AddSource(TracingExtensions.DatabaseActivitySource.Name)
    .AddOtlpExporter(...))
```

## Testing

To test the tracing:

1. Start the observability stack: `./start-observability-local.sh`
2. Make a PUT request to update a supplier: `PUT /api/suppliers/{id}`
3. Check Grafana Tempo: `http://localhost:3000` → Explore → Tempo
4. Query for traces: `{service.name="procurement-api"}`

The trace will show the complete flow from controller through service to database operations. 