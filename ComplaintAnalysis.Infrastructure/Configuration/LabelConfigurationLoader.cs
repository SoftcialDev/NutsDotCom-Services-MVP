using System.Text.Json;
using ComplaintAnalysis.Domain.Models;
using ComplaintAnalysis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ComplaintAnalysis.Infrastructure.Configuration;

public class LabelConfigurationLoader
{
    private readonly LabelConfigurationDbContext _context;
    private readonly IConfiguration _configuration;
    private LabelConfiguration? _cachedConfig;

    public LabelConfigurationLoader(
        LabelConfigurationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public LabelConfiguration LoadConfiguration()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        // Try to load from database first
        var configEntity = _context.LabelConfigurations
            .Include(c => c.Labels)
            .OrderByDescending(c => c.Id)
            .FirstOrDefault();

        if (configEntity != null)
        {
            _cachedConfig = MapToDomainModel(configEntity);
            return _cachedConfig;
        }

        // Fallback to JSON file if database is empty
        return LoadFromJsonFallback();
    }

    private LabelConfiguration LoadFromJsonFallback()
    {
        var configPath = _configuration["LabelConfigurationPath"] ?? "labels-config.json";
        
        // Try multiple locations in order of preference
        var searchPaths = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, configPath),
            Path.Combine(Directory.GetCurrentDirectory(), configPath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", configPath),
            configPath
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

        if (fullPath == null || !File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                "Label configuration not found in database and JSON fallback file not found. " +
                "Please ensure the database is seeded or labels-config.json exists."
            );
        }

        var jsonContent = File.ReadAllText(fullPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<LabelConfiguration>(jsonContent, options);
        
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize label configuration file.");
        }

        _cachedConfig = config;
        return _cachedConfig;
    }

    private LabelConfiguration MapToDomainModel(Data.Entities.LabelConfigurationEntity entity)
    {
        var config = new LabelConfiguration
        {
            Version = entity.Version,
            Language = entity.Language,
            Global = new GlobalConfiguration
            {
                MinConfidenceToEmit = entity.MinConfidenceToEmit,
                MaxLabelsPerComplaint = entity.MaxLabelsPerComplaint,
                NegationWindowTokens = entity.NegationWindowTokens,
                Severity = new SeverityConfiguration
                {
                    High = new SeverityLevel
                    {
                        Sentiment = JsonSerializer.Deserialize<List<string>>(entity.HighSentiment) ?? new(),
                        CriticalTerms = JsonSerializer.Deserialize<List<string>>(entity.HighCriticalTerms) ?? new(),
                        CriticalLabels = JsonSerializer.Deserialize<List<string>>(entity.HighCriticalLabels) ?? new()
                    },
                    Medium = new SeverityLevel
                    {
                        Sentiment = JsonSerializer.Deserialize<List<string>>(entity.MediumSentiment) ?? new(),
                        CriticalTerms = JsonSerializer.Deserialize<List<string>>(entity.MediumCriticalTerms) ?? new(),
                        CriticalLabels = JsonSerializer.Deserialize<List<string>>(entity.MediumCriticalLabels) ?? new()
                    }
                }
            },
            Labels = new List<LabelRule>()
        };

        foreach (var labelEntity in entity.Labels)
        {
            var label = new LabelRule
            {
                Id = labelEntity.LabelId,
                DisplayName = labelEntity.DisplayName,
                Priority = labelEntity.Priority,
                Threshold = labelEntity.Threshold,
                Keywords = JsonSerializer.Deserialize<List<KeywordRule>>(labelEntity.Keywords) ?? new(),
                Phrases = JsonSerializer.Deserialize<List<PhraseRule>>(labelEntity.Phrases) ?? new(),
                Regex = JsonSerializer.Deserialize<List<RegexRule>>(labelEntity.Regex) ?? new(),
                Exclusions = JsonSerializer.Deserialize<List<string>>(labelEntity.Exclusions) ?? new(),
                Dominates = JsonSerializer.Deserialize<List<string>>(labelEntity.Dominates) ?? new(),
                HardRules = JsonSerializer.Deserialize<List<HardRule>>(labelEntity.HardRules) ?? new()
            };

            config.Labels.Add(label);
        }

        return config;
    }
}
