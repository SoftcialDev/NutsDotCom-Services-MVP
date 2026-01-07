namespace ComplaintAnalysis.Domain.ValueObjects;

public class MatchEvidence
{
    public string Kind { get; private set; } // "keyword" | "phrase" | "regex" | "hard_rule" | "keyphrase"
    public string Pattern { get; private set; }
    public int Weight { get; private set; }
    public string Excerpt { get; private set; }

    private MatchEvidence(string kind, string pattern, int weight, string excerpt)
    {
        Kind = kind;
        Pattern = pattern;
        Weight = weight;
        Excerpt = excerpt;
    }

    public static MatchEvidence Create(string kind, string pattern, int weight, string excerpt)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Kind cannot be null or empty.", nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));
        }

        if (string.IsNullOrWhiteSpace(excerpt))
        {
            throw new ArgumentException("Excerpt cannot be null or empty.", nameof(excerpt));
        }

        return new MatchEvidence(kind, pattern, weight, excerpt);
    }
}

