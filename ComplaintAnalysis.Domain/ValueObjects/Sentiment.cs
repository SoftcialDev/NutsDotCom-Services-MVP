namespace ComplaintAnalysis.Domain.ValueObjects;

public class Sentiment
{
    public string Label { get; private set; }

    private static readonly HashSet<string> ValidLabels = new()
    {
        "Positive",
        "Negative",
        "Neutral"
    };

    private Sentiment(string label)
    {
        Label = label;
    }

    public static Sentiment Create(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Sentiment label cannot be null or empty.", nameof(label));
        }

        var capitalizedLabel = CapitalizeFirstLetter(label);

        if (!ValidLabels.Contains(capitalizedLabel))
        {
            throw new ArgumentException(
                $"Invalid sentiment label: {label}. Valid labels are: Positive, Negative, Neutral.",
                nameof(label)
            );
        }

        return new Sentiment(capitalizedLabel);
    }

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return char.ToUpper(input[0]) + (input.Length > 1 ? input.Substring(1).ToLower() : string.Empty);
    }
}

