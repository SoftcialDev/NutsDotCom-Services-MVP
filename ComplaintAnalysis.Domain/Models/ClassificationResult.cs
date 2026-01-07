using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Domain.Models;

public class ClassificationResult
{
    public string Sentiment { get; private set; }
    public string Severity { get; private set; }
    public List<Label> Labels { get; private set; }

    private ClassificationResult(string sentiment, string severity, List<Label> labels)
    {
        Sentiment = sentiment;
        Severity = severity;
        Labels = labels;
    }

    public static ClassificationResult Create(string sentiment, string severity, List<Label> labels)
    {
        if (string.IsNullOrWhiteSpace(sentiment))
        {
            throw new ArgumentException("Sentiment cannot be null or empty.", nameof(sentiment));
        }

        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Severity cannot be null or empty.", nameof(severity));
        }

        if (labels == null)
        {
            throw new ArgumentNullException(nameof(labels));
        }

        return new ClassificationResult(sentiment, severity, labels);
    }
}

