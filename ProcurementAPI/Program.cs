using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.HealthChecks;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;
using Sparkify.Observability;


var builder = WebApplication.CreateBuilder(args);

// Register OpenTelemetry and Serilog
builder.RegisterOpenTelemetry()
       .RegisterSerilog();

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

// Add HttpClient for AI service
// builder.Services.AddHttpClient<IAiRecommendationService, AiRecommendationService>(client =>
// {
//     client.Timeout = TimeSpan.FromSeconds(30);
// });

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<SwaggerHealthCheck>("swagger", tags: new[] { "ready" })
    .AddCheck<ApiEndpointsHealthCheck>("api_endpoints", tags: new[] { "ready" });

// Determine OTLP endpoint based on environment
var otlpEndpoint = builder.Environment.EnvironmentName == "Docker"
    ? "http://otel-collector:4317"
    : "http://localhost:4319"; // Use host port for local development

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

// Log startup information
app.LogStartupInfo(builder);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAngular");

// Use Serilog request logging
app.RegisterSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program { }