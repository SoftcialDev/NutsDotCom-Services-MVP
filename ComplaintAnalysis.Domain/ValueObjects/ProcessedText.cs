namespace ComplaintAnalysis.Domain.ValueObjects;

public class ProcessedText
{
    public string NormalizedText { get; private set; }
    public List<string> Tokens { get; private set; }
    public List<int> NegationSpans { get; private set; }

    private ProcessedText(string normalizedText, List<string> tokens, List<int> negationSpans)
    {
        NormalizedText = normalizedText;
        Tokens = tokens;
        NegationSpans = negationSpans;
    }

    public static ProcessedText Create(string normalizedText, List<string> tokens, List<int> negationSpans)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            throw new ArgumentException("Normalized text cannot be null or empty.", nameof(normalizedText));
        }

        if (tokens == null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (negationSpans == null)
        {
            throw new ArgumentNullException(nameof(negationSpans));
        }

        return new ProcessedText(normalizedText, tokens, negationSpans);
    }
}

