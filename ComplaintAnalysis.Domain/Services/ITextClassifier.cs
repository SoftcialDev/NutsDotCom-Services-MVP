using ComplaintAnalysis.Domain.Models;
using ComplaintAnalysis.Domain.ValueObjects;

namespace ComplaintAnalysis.Domain.Services;

public interface ITextClassifier
{
    ClassificationResult Classify(
        ProcessedText processedText,
        string sentiment,
        List<string> keyPhrases,
        List<string> entities,
        LabelConfiguration configuration);
}

