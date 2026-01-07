using System.Text;
using System.Text.RegularExpressions;
using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Infrastructure.TextProcessing;

public class TextPreprocessor
{
    private static readonly Dictionary<string, string> ContractionMap = new()
    {
        { "didn't", "did not" },
        { "don't", "do not" },
        { "doesn't", "does not" },
        { "won't", "will not" },
        { "can't", "cannot" },
        { "couldn't", "could not" },
        { "shouldn't", "should not" },
        { "wouldn't", "would not" },
        { "isn't", "is not" },
        { "aren't", "are not" },
        { "wasn't", "was not" },
        { "weren't", "were not" },
        { "hasn't", "has not" },
        { "haven't", "have not" },
        { "hadn't", "had not" },
        { "mustn't", "must not" },
        { "needn't", "need not" },
        { "shan't", "shall not" },
        { "ain't", "am not" },
        { "I'm", "I am" },
        { "you're", "you are" },
        { "he's", "he is" },
        { "she's", "she is" },
        { "it's", "it is" },
        { "we're", "we are" },
        { "they're", "they are" },
        { "I've", "I have" },
        { "you've", "you have" },
        { "we've", "we have" },
        { "they've", "they have" },
        { "I'll", "I will" },
        { "you'll", "you will" },
        { "he'll", "he will" },
        { "she'll", "she will" },
        { "we'll", "we will" },
        { "they'll", "they will" },
        { "I'd", "I would" },
        { "you'd", "you would" },
        { "he'd", "he would" },
        { "she'd", "she would" },
        { "we'd", "we would" },
        { "they'd", "they would" }
    };

    private static readonly HashSet<string> NegationTriggers = new()
    {
        "no", "not", "never", "without", "n't"
    };

    public ProcessedText Process(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ProcessedText.Create("", new List<string>(), new List<int>());
        }

        // 1. Normalize quotes (smart quotes to straight quotes)
        var normalized = text.Replace('\u2018', '\'').Replace('\u2019', '\'')
                             .Replace('\u201C', '"').Replace('\u201D', '"');

        // 2. Lowercase
        normalized = normalized.ToLower().Trim();

        // 3. Cleaning: Remove URLs, tracking IDs, order numbers, emojis
        normalized = CleanText(normalized);

        // 4. Normalize variants (contractions)
        normalized = NormalizeVariants(normalized);

        // 5. Tokenization
        var tokens = Tokenize(normalized);

        // 6. Find negation spans
        var negationSpans = FindNegationPositions(tokens);

        return ProcessedText.Create(normalized, tokens, negationSpans);
    }

    private string CleanText(string text)
    {
        // Remove URLs (http:// or https:// followed by non-whitespace)
        text = Regex.Replace(text, @"https?://\S+", " ", RegexOptions.IgnoreCase);

        // Remove tracking IDs: # followed by letters/numbers (e.g., #ABC123)
        // Be very specific - only match # followed by 2+ alphanumeric and then digits
        text = Regex.Replace(text, @"#\w{2,}\d+", " ", RegexOptions.IgnoreCase);

        // Remove order numbers: "order" or "ord" followed by separator and digits (e.g., order-123, order #456)
        text = Regex.Replace(text, @"\b(order|ord)[\s-]+#?\d+\b", " ", RegexOptions.IgnoreCase);

        // Remove basic emojis (simplified - common ranges only)
        text = Regex.Replace(text, @"[\u2600-\u27BF]", " ", RegexOptions.None);

        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    private string NormalizeVariants(string text)
    {
        var result = new StringBuilder(text);
        
        // Sort contractions by length (longest first) to avoid partial matches
        var sortedContractions = ContractionMap.OrderByDescending(kvp => kvp.Key.Length);
        
        foreach (var (contraction, expansion) in sortedContractions)
        {
            // Use word boundaries to avoid partial matches
            var pattern = $@"\b{Regex.Escape(contraction)}\b";
            result = new StringBuilder(Regex.Replace(result.ToString(), pattern, expansion, RegexOptions.IgnoreCase));
        }

        return result.ToString();
    }

    private List<string> Tokenize(string text)
    {
        // Tokenizer: extract words (sequences of letters, may include apostrophes)
        // This is simpler and more permissive - matches any sequence of letters and apostrophes
        var matches = Regex.Matches(text, @"[a-z']+", RegexOptions.IgnoreCase);
        return matches.Cast<Match>()
            .Select(m => m.Value.ToLower())
            .Where(t => t.Any(char.IsLetter)) // Filter out tokens that are only apostrophes
            .ToList();
    }

    private List<int> FindNegationPositions(List<string> tokens)
    {
        var negPos = new List<int>();
        for (int i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            if (NegationTriggers.Contains(tok) || tok.EndsWith("n't"))
            {
                negPos.Add(i);
            }
        }
        return negPos;
    }
}

