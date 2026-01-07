namespace ComplaintAnalysis.Application.DTOs;

public class ComplaintAnalysisResponse
{
    public Guid RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Sentiment { get; set; } = string.Empty;
    public List<KeyPhraseDto> KeyPhrases { get; set; } = new();
    public List<EntityDto> Entities { get; set; } = new();
}

