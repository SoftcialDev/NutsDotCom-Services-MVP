using Azure;
using Azure.AI.TextAnalytics;
using ComplaintAnalysis.Application.UseCases;
using ComplaintAnalysis.Domain.Services;
using ComplaintAnalysis.Infrastructure.AzureAI;
using ComplaintAnalysis.Infrastructure.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);
    })
    .Build();

host.Run();
