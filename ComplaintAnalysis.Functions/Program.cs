using Azure;
using Azure.AI.TextAnalytics;
using ComplaintAnalysis.Application.UseCases;
using ComplaintAnalysis.Domain.Services;
using ComplaintAnalysis.Infrastructure.AzureAI;
using ComplaintAnalysis.Infrastructure.Configuration;
using ComplaintAnalysis.Infrastructure.Data;
using ComplaintAnalysis.Infrastructure.Logging;
using ComplaintAnalysis.Infrastructure.TextProcessing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Register Azure Text Analytics Client
        var endpoint = configuration["AZURE_LANGUAGE_ENDPOINT"];
        var key = configuration["AZURE_LANGUAGE_KEY"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "AZURE_LANGUAGE_ENDPOINT and AZURE_LANGUAGE_KEY must be configured."
            );
        }

        services.AddSingleton(new TextAnalyticsClient(
            new Uri(endpoint),
            new AzureKeyCredential(key)
        ));

        // Register logging adapter
        services.AddScoped<ILoggerAdapter, LoggerAdapter>();

        // Register domain service
        services.AddScoped<IComplaintAnalyzer, AzureLanguageService>();

        // Register application use case
        services.AddScoped<ProcessComplaintUseCase>();

        // Register Entity Framework with SQLite
        var dbPath = configuration["LabelConfigurationDbPath"] ?? "labelconfig.db";
        
        // Ensure directory exists for the database file
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            try
            {
                Directory.CreateDirectory(dbDirectory);
            }
            catch
            {
                // If directory creation fails, use just filename (current directory)
                dbPath = Path.GetFileName(dbPath);
            }
        }
        
        // Use absolute path or relative path
        var fullDbPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(AppContext.BaseDirectory, dbPath);
        var connectionString = $"Data Source={fullDbPath}";
        
        services.AddDbContext<LabelConfigurationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register text classification services
        services.AddSingleton<TextPreprocessor>();
        services.AddScoped<LabelConfigurationLoader>();
        services.AddScoped<ITextClassifier, TextClassifier>();
        services.AddScoped<ClassifyComplaintUseCase>();

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);
    })
    .Build();

// Seed database on startup (if needed)
SeedDatabase(host.Services, host.Services.GetRequiredService<IConfiguration>());

host.Run();

static void SeedDatabase(IServiceProvider serviceProvider, IConfiguration configuration)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LabelConfigurationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<LabelConfigurationSeeder>>();
    
    try
    {
        // Ensure database is created
        context.Database.EnsureCreated();
        
        // Check if database needs seeding
        if (!context.LabelConfigurations.Any())
        {
            var seeder = new LabelConfigurationSeeder(context, logger);
            var jsonPath = configuration["LabelConfigurationPath"] ?? "labels-config.json";
            
            // Try to find JSON file
            var searchPaths = new List<string>
            {
                Path.Combine(AppContext.BaseDirectory, jsonPath),
                Path.Combine(Directory.GetCurrentDirectory(), jsonPath),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", jsonPath),
                jsonPath
            };
            
            string? fullPath = null;
            foreach (var path in searchPaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    fullPath = normalizedPath;
                    break;
                }
            }
            
            if (fullPath != null)
            {
                seeder.SeedFromJsonAsync(fullPath).Wait();
                logger.LogInformation("Database seeded successfully from {Path}", fullPath);
            }
            else
            {
                logger.LogWarning("JSON file not found for seeding. Database will be empty.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding database");
    }
}
