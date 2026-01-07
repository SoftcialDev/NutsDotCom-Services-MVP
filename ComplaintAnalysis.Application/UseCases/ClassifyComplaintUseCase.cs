using ComplaintAnalysis.Application.DTOs;
using ComplaintAnalysis.Domain.Models;
using ComplaintAnalysis.Domain.Services;
using ComplaintAnalysis.Domain.ValueObjects;
using ComplaintAnalysis.Infrastructure.Configuration;
using ComplaintAnalysis.Infrastructure.TextProcessing;

namespace ComplaintAnalysis.Application.UseCases;

public class ClassifyComplaintUseCase
{
    private readonly ITextClassifier _textClassifier;
    private readonly TextPreprocessor _textPreprocessor;
    private readonly LabelConfigurationLoader _configLoader;

    public ClassifyComplaintUseCase(
        ITextClassifier textClassifier,
        TextPreprocessor textPreprocessor,
        LabelConfigurationLoader configLoader)
    {
        _textClassifier = textClassifier;
        _textPreprocessor = textPreprocessor;
        _configLoader = configLoader;
    }

    public Task<ClassifyComplaintResponse> ExecuteAsync(ClassifyComplaintRequest request)
    {
        var requestId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Pre-process the complaint text
        var processedText = _textPreprocessor.Process(request.Complaint);

        // Load label configuration
        var configuration = _configLoader.LoadConfiguration();

        // Extract key phrases and entities from request
        var keyPhrases = request.KeyPhrases ?? new List<string>();
        var entities = request.Entities?.Select(e => e.Text).ToList() ?? new List<string>();

        // Classify using domain service
        var classificationResult = _textClassifier.Classify(
            processedText,
            request.Sentiment,
            keyPhrases,
            entities,
            configuration
        );

        // Map to response DTO
        var response = new ClassifyComplaintResponse
        {
            RequestId = requestId,
            Timestamp = timestamp,
            NormalizedText = processedText.NormalizedText,
            Tokens = processedText.Tokens,
            NegationSpans = processedText.NegationSpans,
            KeyPhrases = request.KeyPhrases.Select(kp => new KeyPhraseDto
            {
                Text = kp,
                ConfidenceScore = 1.0 // Default confidence for provided key phrases
            }).ToList(),
            Entities = request.Entities ?? new List<EntityDto>(),
            Sentiment = classificationResult.Sentiment,
            Severity = classificationResult.Severity,
            Labels = classificationResult.Labels.Select(l => new LabelDto
            {
                Id = l.Id,
                DisplayName = l.DisplayName,
                Score = l.Score,
                Confidence = l.Confidence,
                Priority = l.Priority,
                Evidence = l.Evidence.Select(e => new MatchEvidenceDto
                {
                    Kind = e.Kind,
                    Pattern = e.Pattern,
                    Weight = e.Weight,
                    Excerpt = e.Excerpt
                }).ToList()
            }).ToList()
        };

        return Task.FromResult(response);
    }
}

