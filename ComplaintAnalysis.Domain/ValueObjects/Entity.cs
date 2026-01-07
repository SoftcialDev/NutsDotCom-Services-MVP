namespace ComplaintAnalysis.Domain.ValueObjects;

public class Entity
{
    public string Text { get; private set; }
    public string Category { get; private set; }
    public double ConfidenceScore { get; private set; }

    private Entity(string text, string category, double confidenceScore)
    {
        Text = text;
        Category = category;
        ConfidenceScore = confidenceScore;
    }

    public static Entity Create(string text, string category, double confidenceScore)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Entity text cannot be null or empty.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Entity category cannot be null or empty.", nameof(category));
        }

        if (confidenceScore < 0.0 || confidenceScore > 1.0)
        {
            throw new ArgumentException(
                "Confidence score must be between 0.0 and 1.0.",
                nameof(confidenceScore)
            );
        }

        return new Entity(text, category, confidenceScore);
    }
}


