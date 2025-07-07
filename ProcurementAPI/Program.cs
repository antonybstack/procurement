using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Internal;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProcurementAPI.Data;
using ProcurementAPI.HealthChecks;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;

// Enable Npgsql OpenTelemetry instrumentation
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for health checks
builder.Services.AddHttpClient();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<ProcurementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging()
           .EnableDetailedErrors());

// Register services
builder.Services.AddScoped<ISupplierDataService, SupplierDataService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<SwaggerHealthCheck>("swagger", tags: new[] { "ready" })
    .AddCheck<ApiEndpointsHealthCheck>("api_endpoints", tags: new[] { "ready" });

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Determine OTLP endpoint based on environment
var otlpEndpoint = builder.Environment.EnvironmentName == "Docker"
    ? "http://otel-collector:4317"
    : "http://localhost:4319"; // Use host port for local development

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.AddOtlpExporter(opts =>
    {
        opts.Endpoint = new Uri(otlpEndpoint);
        opts.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
        {
            ScheduledDelayMilliseconds = 1000 // Send every 1 second
        };
    });
    logging.AddConsoleExporter();
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
});
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "procurement-api", serviceVersion: "1.0.0")
        .AddAttributes(new KeyValuePair<string, object>[]
        {
            new("deployment.environment", builder.Environment.EnvironmentName),
            new("service.instance.id", Environment.MachineName)
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(otlpEndpoint);
            opts.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
            {
                ScheduledDelayMilliseconds = 1000 // Send every 1 second
            };
        })
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(otlpEndpoint);
            opts.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
            {
                ScheduledDelayMilliseconds = 1000 // Send every 1 second
            };
        }));

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Logger.LogInformation("Application Name: {ApplicationName} ZZZZZ", builder.Environment.ApplicationName);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program { }