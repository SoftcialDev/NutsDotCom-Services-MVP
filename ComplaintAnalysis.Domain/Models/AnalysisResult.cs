using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Domain.Models;

public class AnalysisResult
{
    public Sentiment Sentiment { get; private set; }
    public List<KeyPhrase> KeyPhrases { get; private set; }
    public List<Entity> Entities { get; private set; }

    private AnalysisResult(Sentiment sentiment, List<KeyPhrase> keyPhrases, List<Entity> entities)
    {
        Sentiment = sentiment;
        KeyPhrases = keyPhrases;
        Entities = entities;
    }

    public static AnalysisResult Create(Sentiment sentiment, List<KeyPhrase> keyPhrases, List<Entity> entities)
    {
        if (sentiment == null)
        {
            throw new ArgumentNullException(nameof(sentiment));
        }

        if (keyPhrases == null)
        {
            throw new ArgumentNullException(nameof(keyPhrases));
        }

        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        return new AnalysisResult(sentiment, keyPhrases, entities);
    }
}

