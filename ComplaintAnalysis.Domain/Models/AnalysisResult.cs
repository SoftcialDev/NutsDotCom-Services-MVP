using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Domain.Models;

public class AnalysisResult
{
    public Sentiment Sentiment { get; private set; }
    public List<KeyPhrase> KeyPhrases { get; private set; }

    private AnalysisResult(Sentiment sentiment, List<KeyPhrase> keyPhrases)
    {
        Sentiment = sentiment;
        KeyPhrases = keyPhrases;
    }

    public static AnalysisResult Create(Sentiment sentiment, List<KeyPhrase> keyPhrases)
    {
        if (sentiment == null)
        {
            throw new ArgumentNullException(nameof(sentiment));
        }

        if (keyPhrases == null)
        {
            throw new ArgumentNullException(nameof(keyPhrases));
        }

        return new AnalysisResult(sentiment, keyPhrases);
    }
}

