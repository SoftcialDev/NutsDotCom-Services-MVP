using System.Text.Json;
using ComplaintAnalysis.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplaintAnalysis.Infrastructure.Data;

public class LabelConfigurationSeeder
{
    private readonly LabelConfigurationDbContext _context;
    private readonly ILogger<LabelConfigurationSeeder> _logger;

    public LabelConfigurationSeeder(
        LabelConfigurationDbContext context,
        ILogger<LabelConfigurationSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedFromJsonAsync(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
        {
            _logger.LogWarning("JSON file not found at {Path}, skipping seed", jsonFilePath);
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var jsonConfig = JsonSerializer.Deserialize<JsonConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (jsonConfig == null)
        {
            _logger.LogError("Failed to deserialize JSON configuration");
            return;
        }

        // Check if configuration already exists
        var existing = await _context.LabelConfigurations
            .FirstOrDefaultAsync(c => c.Version == jsonConfig.Version);

        if (existing != null)
        {
            _logger.LogInformation("Configuration version {Version} already exists, skipping seed", jsonConfig.Version);
            return;
        }

        // Create configuration entity
        var configEntity = new LabelConfigurationEntity
        {
            Version = jsonConfig.Version,
            Language = jsonConfig.Language,
            MinConfidenceToEmit = jsonConfig.Global.MinConfidenceToEmit,
            MaxLabelsPerComplaint = jsonConfig.Global.MaxLabelsPerComplaint,
            NegationWindowTokens = jsonConfig.Global.NegationWindowTokens,
            HighSentiment = JsonSerializer.Serialize(jsonConfig.Global.Severity.High.Sentiment),
            HighCriticalTerms = JsonSerializer.Serialize(jsonConfig.Global.Severity.High.CriticalTerms),
            HighCriticalLabels = JsonSerializer.Serialize(jsonConfig.Global.Severity.High.CriticalLabels),
            MediumSentiment = JsonSerializer.Serialize(jsonConfig.Global.Severity.Medium.Sentiment),
            MediumCriticalTerms = JsonSerializer.Serialize(jsonConfig.Global.Severity.Medium.CriticalTerms),
            MediumCriticalLabels = JsonSerializer.Serialize(jsonConfig.Global.Severity.Medium.CriticalLabels)
        };

        _context.LabelConfigurations.Add(configEntity);
        await _context.SaveChangesAsync();

        // Add label rules
        foreach (var label in jsonConfig.Labels)
        {
            var labelEntity = new LabelRuleEntity
            {
                ConfigurationId = configEntity.Id,
                LabelId = label.Id,
                DisplayName = label.DisplayName,
                Priority = label.Priority,
                Threshold = label.Threshold,
                Keywords = JsonSerializer.Serialize(label.Keywords),
                Phrases = JsonSerializer.Serialize(label.Phrases),
                Regex = JsonSerializer.Serialize(label.Regex),
                Exclusions = JsonSerializer.Serialize(label.Exclusions),
                Dominates = JsonSerializer.Serialize(label.Dominates),
                HardRules = JsonSerializer.Serialize(label.HardRules)
            };

            _context.LabelRules.Add(labelEntity);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully seeded configuration version {Version} with {Count} labels", 
            jsonConfig.Version, jsonConfig.Labels.Count);
    }

    // Helper classes for JSON deserialization
    private class JsonConfig
    {
        public string Version { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public JsonGlobal Global { get; set; } = new();
        public List<JsonLabel> Labels { get; set; } = new();
    }

    private class JsonGlobal
    {
        public double MinConfidenceToEmit { get; set; }
        public int MaxLabelsPerComplaint { get; set; }
        public int NegationWindowTokens { get; set; }
        public JsonSeverity Severity { get; set; } = new();
    }

    private class JsonSeverity
    {
        public JsonSeverityLevel High { get; set; } = new();
        public JsonSeverityLevel Medium { get; set; } = new();
    }

    private class JsonSeverityLevel
    {
        public List<string> Sentiment { get; set; } = new();
        public List<string> CriticalTerms { get; set; } = new();
        public List<string> CriticalLabels { get; set; } = new();
    }

    private class JsonLabel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int Threshold { get; set; }
        public List<JsonKeywordRule> Keywords { get; set; } = new();
        public List<JsonPhraseRule> Phrases { get; set; } = new();
        public List<JsonRegexRule> Regex { get; set; } = new();
        public List<string> Exclusions { get; set; } = new();
        public List<string> Dominates { get; set; } = new();
        public List<JsonHardRule> HardRules { get; set; } = new();
    }

    private class JsonKeywordRule
    {
        public string T { get; set; } = string.Empty;
        public int W { get; set; }
    }

    private class JsonPhraseRule
    {
        public string T { get; set; } = string.Empty;
        public int W { get; set; }
    }

    private class JsonRegexRule
    {
        public string T { get; set; } = string.Empty;
        public int W { get; set; }
    }

    private class JsonHardRule
    {
        public string Type { get; set; } = string.Empty;
        public List<string> Phrases { get; set; } = new();
        public int EmitScore { get; set; }
    }
}

