namespace ComplaintAnalysis.Application.DTOs;

public class ClassifyComplaintResponse
{
    public Guid RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public string NormalizedText { get; set; } = string.Empty;
    public List<string> Tokens { get; set; } = new();
    public List<int> NegationSpans { get; set; } = new();
    public List<KeyPhraseDto> KeyPhrases { get; set; } = new();
    public List<EntityDto> Entities { get; set; } = new();
    public string Sentiment { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<LabelDto> Labels { get; set; } = new();
}

public class LabelDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Score { get; set; }
    public double Confidence { get; set; }
    public int Priority { get; set; }
    public List<MatchEvidenceDto> Evidence { get; set; } = new();
}

public class MatchEvidenceDto
{
    public string Kind { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string Excerpt { get; set; } = string.Empty;
}

