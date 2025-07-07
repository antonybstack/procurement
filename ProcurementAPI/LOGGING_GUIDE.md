# OpenTelemetry Logging Guide for Suppliers API

This guide explains the comprehensive logging implementation for the Suppliers API using OpenTelemetry, optimized for Grafana Loki querying.

## Logging Architecture

The logging implementation follows a structured approach with the following components:

### 1. Logging Extensions (`LoggingExtensions.cs`)
Provides consistent logging patterns across all layers:
- `LogSupplierOperation()` - For supplier-specific operations
- `LogDatabaseOperation()` - For database operations
- `LogValidationError()` - For validation failures
- `LogPerformanceMetric()` - For performance tracking

### 2. Correlation IDs
Every log entry includes a correlation ID that links related operations across layers:
- Controller → Service → Data Service
- Automatically propagated via OpenTelemetry Activity
- Enables tracing complete request flows

### 3. Structured Logging
All logs use structured format with consistent fields:
- `operation` - The specific operation being performed
- `component` - The layer/component (supplier, database, validation, performance)
- `correlation_id` - Links related operations
- `supplier_id` - When applicable
- `supplier_code` - When applicable
- `company_name` - When applicable
- `duration_ms` - For performance metrics

## Log Levels

- **Information** - Normal operations, successful completions
- **Warning** - Validation errors, not found scenarios
- **Error** - Exceptions, failures
- **Debug** - Detailed operation steps, filter applications

## Loki Query Examples

### 1. Find All Supplier Operations
```logql
{component="supplier"}
```

### 2. Find Failed Operations
```logql
{component="supplier"} |= "failed"
```

### 3. Find Operations for Specific Supplier
```logql
{supplier_id="123"}
```

### 4. Find Operations by Supplier Code
```logql
{supplier_code="SUP001"}
```

### 5. Find Validation Errors
```logql
{component="validation"}
```

### 6. Find Performance Issues (operations taking > 1000ms)
```logql
{component="performance"} | json | duration_ms > 1000
```

### 7. Find Database Operations
```logql
{component="database"}
```

### 8. Trace Complete Request Flow
```logql
{correlation_id="your-correlation-id"}
```

### 9. Find Operations by Company Name
```logql
{company_name="Tech Solutions Inc"}
```

### 10. Find Recent Errors
```logql
{component="supplier"} |= "error" | json | timestamp > now() - 1h
```

## Operation Types

### Controller Operations
- `get_suppliers_started/completed/failed`
- `get_supplier_by_id_started/completed/failed/not_found`
- `create_supplier_started/completed/failed/validation_failed/invalid_data`
- `update_supplier_started/completed/failed/validation_failed/invalid_data`
- `delete_supplier_started/completed/failed/not_found`
- `get_countries_started/completed/failed`
- `validate_supplier_code_started/completed/failed`

### Service Operations
- `service_get_suppliers_started/completed/failed`
- `service_get_supplier_by_id_started/completed/failed/not_found`
- `service_create_supplier_started/completed/failed`
- `service_update_supplier_started/completed/failed/not_found`
- `service_delete_supplier_started/completed/failed/not_found`
- `service_get_countries_started/completed/failed`
- `service_validate_supplier_code_started/completed/failed`
- `supplier_validation_passed`

### Database Operations
- `get_suppliers_started/completed/failed`
- `get_supplier_by_id_started/completed/failed/not_found`
- `get_supplier_entity_by_id_started/completed/failed/not_found`
- `create_supplier_started/completed/failed`
- `update_supplier_started/completed/failed`
- `delete_supplier_started/completed/failed/not_found`
- `supplier_exists_check_started/completed/failed`
- `supplier_code_exists_check_started/completed/failed`
- `get_countries_started/completed/failed`
- `get_total_suppliers_count_started/completed/failed`
- `applied_search_filter`
- `applied_country_filter`
- `applied_rating_filter`
- `applied_active_filter`
- `count_query_executed`

### Performance Metrics
- `get_suppliers`
- `get_supplier_by_id`
- `create_supplier`
- `update_supplier`
- `delete_supplier`
- `get_countries`
- `validate_supplier_code`
- `service_get_suppliers`
- `service_get_supplier_by_id`
- `service_create_supplier`
- `service_update_supplier`
- `service_delete_supplier`
- `service_get_countries`
- `service_validate_supplier_code`
- `database_get_suppliers`
- `database_get_supplier_by_id`
- `database_get_supplier_entity_by_id`
- `database_create_supplier`
- `database_update_supplier`
- `database_delete_supplier`
- `database_supplier_exists_check`
- `database_supplier_code_exists_check`
- `database_get_countries`
- `database_get_total_suppliers_count`

## Advanced Loki Queries

### 1. Performance Analysis
```logql
{component="performance"} | json | duration_ms > 500 | line_format "{{.operation}}: {{.duration_ms}}ms"
```

### 2. Error Rate by Operation
```logql
sum by (operation) (rate({component="supplier"} |= "failed" [5m]))
```

### 3. Top Slow Operations
```logql
{component="performance"} | json | sort(duration_ms) | limit 10
```

### 4. Validation Error Summary
```logql
{component="validation"} | json | group_by(field) | count_over_time([5m])
```

### 5. Database Query Performance
```logql
{component="database"} | json | duration_ms > 100 | line_format "{{.operation}}: {{.duration_ms}}ms"
```

### 6. Supplier Activity by Country
```logql
{component="supplier"} | json | group_by(country) | count_over_time([1h])
```

### 7. Correlation Flow Analysis
```logql
{correlation_id="abc123"} | json | sort(timestamp)
```

### 8. Error Patterns
```logql
{component="supplier"} |= "error" | json | group_by(operation) | count_over_time([1h])
```

## Monitoring Dashboards

### Key Metrics to Monitor
1. **Request Volume** - Total operations per minute
2. **Error Rate** - Failed operations percentage
3. **Performance** - 95th percentile response times
4. **Database Performance** - Query execution times
5. **Validation Errors** - Most common validation failures

### Alert Examples
1. Error rate > 5% in 5 minutes
2. Response time > 2000ms for 95th percentile
3. Database operation failures > 10 in 1 minute
4. Validation errors > 50 in 5 minutes

## Best Practices

1. **Always include correlation_id** for request tracing
2. **Use structured logging** with consistent field names
3. **Log at appropriate levels** (Info for normal ops, Warning for business logic issues, Error for exceptions)
4. **Include relevant context** (supplier_id, supplier_code, company_name)
5. **Measure performance** for all operations
6. **Log validation errors** with specific field and reason
7. **Use snake_case** for field names in Loki queries

## Troubleshooting

### Common Issues
1. **Missing correlation_id** - Check OpenTelemetry configuration
2. **High log volume** - Adjust log levels for debug operations
3. **Slow queries** - Use performance metrics to identify bottlenecks
4. **Missing context** - Ensure all relevant fields are included in log data

### Debug Queries
```logql
# Find operations without correlation_id
{component="supplier"} !~ "correlation_id"

# Find operations with missing supplier_id when expected
{component="supplier"} !~ "supplier_id" |= "get_supplier_by_id"

# Find performance outliers
{component="performance"} | json | duration_ms > 5000
``` 