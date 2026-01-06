namespace ComplaintAnalysis.Domain.ValueObjects;

public class KeyPhrase
{
    public string Text { get; private set; }
    public double ConfidenceScore { get; private set; }

    private KeyPhrase(string text, double confidenceScore)
    {
        Text = text;
        ConfidenceScore = confidenceScore;
    }

    public static KeyPhrase Create(string text, double confidenceScore)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Key phrase text cannot be null or empty.", nameof(text));
        }

        if (confidenceScore < 0.0 || confidenceScore > 1.0)
        {
            throw new ArgumentException(
                "Confidence score must be between 0.0 and 1.0.",
                nameof(confidenceScore)
            );
        }

        return new KeyPhrase(text, confidenceScore);
    }
}

