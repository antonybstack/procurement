using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using ProcurementAPI.Data;
using ProcurementAPI.HealthChecks;
using ProcurementAPI.Models;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;
using ProcurementAPI.Services.Ingestion;
using Scalar.AspNetCore;
using Sparkify.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(true, true)
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddCommandLine(args);

/*
Alternatives:
- https://openrouter.ai/meta-llama/llama-3.3-70b-instruct:free
- https://openrouter.ai/deepseek/deepseek-chat-v3-0324:free
- https://openrouter.ai/openai/gpt-4o-mini
- https://openrouter.ai/google/gemini-2.0-flash-exp:free
*/
var chatModel = "openai/gpt-5-nano" ?? builder.Configuration.GetRequiredSection("ModelName").Value!;
chatModel = "gpt-5-nano";
//string openRouterKey = builder.Configuration.GetRequiredSection("OpenRouterKey").Value!;
var openAIKey = builder.Configuration.GetRequiredSection("OpenAIKey").Value!;
var embeddingModelName = builder.Configuration.GetRequiredSection("EmbeddingModelName").Value!;

// Register OpenTelemetry and Serilog
builder.RegisterOpenTelemetry()
    .RegisterSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(options =>
// {
//     options.SwaggerDoc("v1", new OpenApiInfo
//     {
//         Title = "Procurement API",
//         Version = "v1",
//         Description = "AI-powered Procurement API"
//     });
// });
// builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
// Add HttpClient for health checks
builder.Services.AddHttpClient();

// Add Entity Framework with PostgreSQL and pgvector support

builder.Services.AddDbContext<ProcurementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseVector())
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors());

// Configure PostgreSQL vector store with connection string
// Note: This will be updated later with embedding generator configuration
// builder.Services.AddPostgresVectorStore();

// Register services
builder.Services.AddScoped<ISupplierDataService, SupplierDataService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISupplierVectorService, SupplierVectorService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    // .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    // .AddCheck<SwaggerHealthCheck>("swagger", tags: new[] { "ready" })
    .AddCheck<ApiEndpointsHealthCheck>("api_endpoints", tags: new[] { "ready" });

// Determine OTLP endpoint based on environment
var otlpEndpoint = builder.Environment.EnvironmentName == "Docker"
    ? "http://otel-collector:4317"
    : "http://localhost:4319"; // Use host port for local development

// CORS for frontend clients - important for HTTP streaming
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Type"); // Required for Server-Sent Events
    });
});

// Configure JSON options for streaming
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false; // Minimize payload for streaming
});

/*** AI STUFF BEGIN ***/

// Create OpenAI client configured for OpenRouter API compatibility
// OpenAIClient openRouterClient = new OpenAIClient(
//     new ApiKeyCredential(openRouterKey),
//     new OpenAIClientOptions()
//     {
//         Endpoint = new Uri("https://openrouter.ai/api/v1"),
//         NetworkTimeout = TimeSpan.FromMinutes(2)
//     });

var openAiChatClient = new OpenAIClient(
    new ApiKeyCredential(openAIKey),
    new OpenAIClientOptions
    {
        NetworkTimeout = TimeSpan.FromMinutes(2)
    });


// Create OpenAI client for embeddings and other models for OpenAI API compatibility
var openAiEmbeddingClient = new OpenAIClient(
    new ApiKeyCredential(openAIKey),
    new OpenAIClientOptions
    {
        NetworkTimeout = TimeSpan.FromMinutes(2)
    });


// Use containerized Ollama for embeddings (Docker container on port 11435)
// Console.WriteLine("Using Docker Ollama for embeddings on port 11435");
// IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(new Uri("http://localhost:11435"), "nomic-embed-text");
// SampleEmbeddingGenerator embeddingGenerator = new(
//     new Uri("https://openrouter.ai/api/v1"),
//     embeddingModelName);

// var chatClient = openAIClient.AsChatClient("gpt-4o-mini");
// var embeddingGenerator = openAIClient.AsEmbeddingGenerator("text-embedding-3-small");

var chatClient = openAiChatClient
    .GetChatClient(chatModel)
    .AsIChatClient();
// IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = openRouterClient.GetEmbeddingClient(embeddingModelName).AsIEmbeddingGenerator();

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = openAiEmbeddingClient
    .GetEmbeddingClient(embeddingModelName)
    .AsIEmbeddingGenerator();

// builder.Services.AddPostgresVectorStore(builder.Configuration.GetConnectionString("DefaultConnection")!, new PostgresVectorStoreOptions() { EmbeddingGenerator = embeddingGenerator });

// builder.Services.AddSingleton<ElasticsearchClient>(sp =>
//     new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("http://localhost:9200"))));
// #pragma warning disable SKEXP0020
// builder.Services.AddElasticsearchVectorStore();
// #pragma warning restore SKEXP0020

var elasticsearchUrl = builder.Configuration.GetValue<string>("ElasticsearchUrl") ?? "http://localhost:9200"; // builder.Configuration.GetValue<string>("ElasticsearchUrl") ??
builder.Services.AddSingleton<ElasticsearchClient>(sp =>
    new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(elasticsearchUrl))));
builder.Services.AddElasticsearchVectorStore();

// builder.Services.AddElasticsearchVectorStore(new ElasticsearchClientSettings(new Uri(elasticsearchUrl)));

// var nodePool = new SingleNodePool(new Uri(elasticsearchUrl));
// var settings = new ElasticsearchClientSettings(
//     nodePool,
//     (defaultSerializer, settings) =>
//         new DefaultSourceSerializer(settings, options =>
//             options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper));
// var client = new ElasticsearchClient(settings);
//
// builder.Services.AddSingleton(client);
//
// builder.Services.AddElasticsearchVectorStore();

// test chat client and embedding generator (optional)
var testOpenAI = builder.Configuration.GetSection("TestOpenAIOnStartup").Get<bool?>() ?? false;
if (testOpenAI)
    try
    {
        var testChat = Task.Run(async () => await chatClient.GetResponseAsync(
            new[]
            {
                new ChatMessage(ChatRole.System, "You are a helpful assistant."),
                new ChatMessage(ChatRole.User, "Hello world")
            })).GetAwaiter().GetResult();

        GeneratedEmbeddings<Embedding<float>> testEmbed = Task.Run(async () => await embeddingGenerator.GenerateAsync(
            new[] { "Hello world" })).GetAwaiter().GetResult();

        Console.WriteLine("✅ OpenAI API test successful");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ OpenAI API test failed: {ex.Message}");
    }

var vectorStorePath = string.Empty;
var resetVectorStores = builder.Configuration.GetSection("ResetVectorStores").Get<bool?>() ?? false;
if (resetVectorStores)
{
    // delete old vector-stores that match the pattern
    var oldFiles = Directory.GetFiles(AppContext.BaseDirectory, "vector-store-*.db");
    foreach (var oldFile in oldFiles)
        try
        {
            File.Delete(oldFile);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete old vector store file {oldFile}: {ex.Message}");
        }

    // Configure services
    vectorStorePath = Path.Combine(AppContext.BaseDirectory, $"vector-store-{Guid.NewGuid()}.db");
}
else
{
    var existingVectorStore = Directory.GetFiles(AppContext.BaseDirectory, "vector-store-*.db").FirstOrDefault();
    vectorStorePath = existingVectorStore ?? Path.Combine(AppContext.BaseDirectory, $"vector-store-{Guid.NewGuid()}.db");
    Debug.WriteLine($"Using vector store at: {vectorStorePath}");
}

var vectorStoreConnectionString = $"Data Source={vectorStorePath}";

builder.Services.AddSqliteCollection<string, IngestedChunk>("data-chatapp-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-chatapp-documents", vectorStoreConnectionString);

builder.Services.AddScoped<DataIngestor>();

builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

/*** AI STUFF END ***/

var app = builder.Build();
// app.MapOpenApi();
app.UseOpenApi(options => { options.Path = "/openapi/{documentName}.json"; });
app.MapScalarApiReference("/docs");
// Task.Run(async () =>
// {
//     // get a scope to resolve scoped services
//     using var scope = app.Services.CreateScope();
//     var services = scope.ServiceProvider;
//     
//     // Get service created from AddPostgresVectorStore to test
//     var vectorStore = services.GetRequiredService<PostgresVectorStore>();
//     var supplierVectorService = services.GetRequiredService<ISupplierVectorService>();
//     
//     // Initialize vector store
//     await supplierVectorService.InitializeVectorStoreAsync();
//     
//     // Create and upsert test data
//     var records = await supplierVectorService.CreateTestDataAsync();
//     
//     // Wait for indexing to complete
//     await Task.Delay(5000);
//     
//     // Test text-based search
//     // var searchResult = await supplierVectorService.SearchByTextAsync("SupplierCodeXYZ789", top: 1);
//     
//     // Test vector-based search
//     var embeddingGenerator = app.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
//     var searchString = "What is SupplierCodeXYZ789";
//     // var searchVector = (await embeddingGenerator.GenerateAsync(searchString)).Vector;
//     var resultRecords = await supplierVectorService.SearchByVectorAsync(searchString, top: 1);
//     
//     await foreach (var record in resultRecords)
//     {
//         Console.WriteLine("Search string: " + searchString);
//         Console.WriteLine("SupplierId: " + record.Record.SupplierId);
//         Console.WriteLine("EmbeddingText: " + record.Record.EmbeddingText);
//         Console.WriteLine("BuildSearchableContent: " + record.Record.BuildSearchableContent());
//         // Console.WriteLine("Embedding: " + record.Record.EmbeddingText.ToString());
//         // Console.WriteLine("Embedding: " + record.Record.Embedding.ToArray().ToString());
//         Console.WriteLine();
//     }
// }).GetAwaiter().GetResult();
// Log startup information
app.LogStartupInfo(builder);

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

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

// By default, we ingest PDF files from the /Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
var ingestDocuments = builder.Configuration.GetSection("IngestDocuments").Get<bool?>() ?? false;
if (ingestDocuments)
{
    var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Documents");
    Directory.CreateDirectory(dataPath); // Ensure the directory exists
    await DataIngestor.IngestDataAsync(
        app.Services,
        new PDFDirectorySource(dataPath));
}

app.Run();

public partial class Program
{
}