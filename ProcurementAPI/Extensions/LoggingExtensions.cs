using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ProcurementAPI.Extensions;

public static class LoggingExtensions
{
    public static void LogSupplierOperation(
        this ILogger logger,
        LogLevel level,
        string operation,
        int? supplierId = null,
        string? supplierCode = null,
        string? companyName = null,
        string? correlationId = null,
        Exception? exception = null,
        object? additionalData = null)
    {
        var logData = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["component"] = "supplier",
            ["correlation_id"] = correlationId ?? Activity.Current?.Id ?? "unknown"
        };

        if (supplierId.HasValue)
            logData["supplier_id"] = supplierId.Value;

        if (!string.IsNullOrEmpty(supplierCode))
            logData["supplier_code"] = supplierCode;

        if (!string.IsNullOrEmpty(companyName))
            logData["company_name"] = companyName;

        if (additionalData != null)
        {
            foreach (var prop in additionalData.GetType().GetProperties())
            {
                var value = prop.GetValue(additionalData);
                if (value != null)
                    logData[prop.Name.ToSnakeCase()] = value;
            }
        }

        if (exception != null)
        {
            logger.Log(level, exception, "Supplier operation failed: {Operation} | {LogData}", operation, logData);
        }
        else
        {
            logger.Log(level, "Supplier operation: {Operation} | {LogData}", operation, logData);
        }
    }

    public static void LogDatabaseOperation(
        this ILogger logger,
        LogLevel level,
        string operation,
        string entity,
        int? entityId = null,
        string? correlationId = null,
        Exception? exception = null,
        object? additionalData = null)
    {
        var logData = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["entity"] = entity,
            ["component"] = "database",
            ["correlation_id"] = correlationId ?? Activity.Current?.Id ?? "unknown"
        };

        if (entityId.HasValue)
            logData["entity_id"] = entityId.Value;

        if (additionalData != null)
        {
            foreach (var prop in additionalData.GetType().GetProperties())
            {
                var value = prop.GetValue(additionalData);
                if (value != null)
                    logData[prop.Name.ToSnakeCase()] = value;
            }
        }

        if (exception != null)
        {
            logger.Log(level, exception, "Database operation failed: {Operation} on {Entity} | {LogData}", operation, entity, logData);
        }
        else
        {
            logger.Log(level, "Database operation: {Operation} on {Entity} | {LogData}", operation, entity, logData);
        }
    }

    public static void LogValidationError(
        this ILogger logger,
        string field,
        string value,
        string reason,
        int? supplierId = null,
        string? correlationId = null)
    {
        var logData = new Dictionary<string, object?>
        {
            ["validation_error"] = true,
            ["field"] = field,
            ["value"] = value,
            ["reason"] = reason,
            ["component"] = "validation",
            ["correlation_id"] = correlationId ?? Activity.Current?.Id ?? "unknown"
        };

        if (supplierId.HasValue)
            logData["supplier_id"] = supplierId.Value;

        logger.LogWarning("Validation error: {Field}={Value} | {LogData}", field, value, logData);
    }

    public static void LogPerformanceMetric(
        this ILogger logger,
        string operation,
        long durationMs,
        string? correlationId = null,
        object? additionalData = null)
    {
        var logData = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["duration_ms"] = durationMs,
            ["component"] = "performance",
            ["correlation_id"] = correlationId ?? Activity.Current?.Id ?? "unknown"
        };

        if (additionalData != null)
        {
            foreach (var prop in additionalData.GetType().GetProperties())
            {
                var value = prop.GetValue(additionalData);
                if (value != null)
                    logData[prop.Name.ToSnakeCase()] = value;
            }
        }

        logger.LogInformation("Performance metric: {Operation} took {DurationMs}ms | {LogData}", operation, durationMs, logData);
    }

    private static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }
}