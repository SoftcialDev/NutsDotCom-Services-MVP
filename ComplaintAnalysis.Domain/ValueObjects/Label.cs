using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Domain.ValueObjects;

public class Label
{
    public string Id { get; private set; }
    public string DisplayName { get; private set; }
    public int Score { get; private set; }
    public double Confidence { get; private set; }
    public int Priority { get; private set; }
    public List<MatchEvidence> Evidence { get; private set; }

    private Label(string id, string displayName, int score, double confidence, int priority, List<MatchEvidence> evidence)
    {
        Id = id;
        DisplayName = displayName;
        Score = score;
        Confidence = confidence;
        Priority = priority;
        Evidence = evidence;
    }

    public static Label Create(string id, string displayName, int score, double confidence, int priority, List<MatchEvidence> evidence)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be null or empty.", nameof(displayName));
        }

        if (confidence < 0.0 || confidence > 1.0)
        {
            throw new ArgumentException(
                "Confidence must be between 0.0 and 1.0.",
                nameof(confidence)
            );
        }

        if (evidence == null)
        {
            throw new ArgumentNullException(nameof(evidence));
        }

        return new Label(id, displayName, score, confidence, priority, evidence);
    }
}

