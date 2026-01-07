namespace ComplaintAnalysis.Application.DTOs;

public class ClassifyComplaintRequest
{
    public string Complaint { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public List<string> KeyPhrases { get; set; } = new();
    public List<EntityDto>? Entities { get; set; }
}

