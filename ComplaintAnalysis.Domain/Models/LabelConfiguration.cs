namespace ComplaintAnalysis.Domain.Models;

public class LabelConfiguration
{
    public string Version { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public GlobalConfiguration Global { get; set; } = new();
    public List<LabelRule> Labels { get; set; } = new();
}

public class GlobalConfiguration
{
    public double MinConfidenceToEmit { get; set; }
    public int MaxLabelsPerComplaint { get; set; }
    public int NegationWindowTokens { get; set; }
    public SeverityConfiguration Severity { get; set; } = new();
}

public class SeverityConfiguration
{
    public SeverityLevel High { get; set; } = new();
    public SeverityLevel Medium { get; set; } = new();
}

public class SeverityLevel
{
    public List<string> Sentiment { get; set; } = new();
    public List<string> CriticalTerms { get; set; } = new();
    public List<string> CriticalLabels { get; set; } = new();
}

public class LabelRule
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int Threshold { get; set; }
    public List<KeywordRule> Keywords { get; set; } = new();
    public List<PhraseRule> Phrases { get; set; } = new();
    public List<RegexRule> Regex { get; set; } = new();
    public List<string> Exclusions { get; set; } = new();
    public List<string> Dominates { get; set; } = new();
    public List<HardRule> HardRules { get; set; } = new();
}

public class KeywordRule
{
    public string T { get; set; } = string.Empty; // text
    public int W { get; set; } // weight
}

public class PhraseRule
{
    public string T { get; set; } = string.Empty; // text
    public int W { get; set; } // weight
}

public class RegexRule
{
    public string T { get; set; } = string.Empty; // pattern
    public int W { get; set; } // weight
}

public class HardRule
{
    public string Type { get; set; } = string.Empty;
    public List<string> Phrases { get; set; } = new();
    public int EmitScore { get; set; }
}

