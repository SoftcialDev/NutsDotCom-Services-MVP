namespace ComplaintAnalysis.Application.DTOs;

public class EntityDto
{
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}


