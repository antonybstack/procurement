using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcurementAPI.Data;
using ProcurementAPI.HealthChecks;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;

namespace ProcurementAPI.Tests;

public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    private static int _databaseCounter = 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Create a new service provider.
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Add a database context (ApplicationDbContext) using an in-memory 
            // database for testing with a unique name for each test class.
            var databaseName = $"InMemoryDbForTesting_{Interlocked.Increment(ref _databaseCounter)}";
            services.AddDbContext<ProcurementDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.UseInternalServiceProvider(serviceProvider);
            });

            // Register services for testing
            services.AddScoped<ISupplierDataService, SupplierDataService>();
            services.AddScoped<ISupplierService, SupplierService>();

            // Build the service provider.
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database contexts
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ProcurementDbContext>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                // Ensure the database is created.
                db.Database.EnsureCreated();

                try
                {
                    // Seed the database with test data.
                    Utilities.InitializeDbForTests(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the " +
                        "database with test messages. Error: {Message}", ex.Message);
                }
            }
        });
    }
}