using Azure;
using Azure.AI.TextAnalytics;
using ComplaintAnalysis.Domain.Entities;
using ComplaintAnalysis.Domain.Models;
using ComplaintAnalysis.Domain.Services;
using ComplaintAnalysis.Domain.ValueObjects;
using ComplaintAnalysis.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;

namespace ComplaintAnalysis.Infrastructure.AzureAI;

public class AzureLanguageService : IComplaintAnalyzer
{
    private readonly TextAnalyticsClient _client;
    private readonly ILoggerAdapter _logger;
    private readonly IConfiguration _configuration;

    public AzureLanguageService(
        TextAnalyticsClient client,
        ILoggerAdapter logger,
        IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AnalysisResult> AnalyzeAsync(Complaint complaint)
    {
        try
        {
            _logger.LogInformation(
                "Calling Azure Language API - ComplaintId: {ComplaintId}",
                complaint.Id
            );

            // Prepare documents for Azure API
            var documents = new List<TextDocumentInput>
            {
                new TextDocumentInput(complaint.Id.ToString(), complaint.Text)
                {
                    Language = "en"
                }
            };

            // Analyze sentiment with opinion mining enabled
            var sentimentResponse = await _client.AnalyzeSentimentBatchAsync(
                documents,
                options: new AnalyzeSentimentOptions
                {
                    IncludeStatistics = false,
                    IncludeOpinionMining = true
                }
            );

            // Extract key phrases
            var keyPhrasesResponse = await _client.ExtractKeyPhrasesBatchAsync(
                documents,
                options: new TextAnalyticsRequestOptions
                {
                    IncludeStatistics = false
                }
            );

            // Recognize entities
            var entitiesResponse = await _client.RecognizeEntitiesBatchAsync(
                documents,
                options: new TextAnalyticsRequestOptions
                {
                    IncludeStatistics = false
                }
            );

            // Process sentiment result
            var sentimentDocument = sentimentResponse.Value.FirstOrDefault();
            if (sentimentDocument == null)
            {
                throw new InvalidOperationException("Sentiment analysis failed: No document returned");
            }
            
            if (sentimentDocument.HasError)
            {
                throw new InvalidOperationException(
                    $"Sentiment analysis failed: {sentimentDocument.Error.Message}"
                );
            }

            // Get primary sentiment (highest confidence)
            var primarySentiment = sentimentDocument.DocumentSentiment.Sentiment.ToString();
            var sentiment = Sentiment.Create(primarySentiment);

            // Process key phrases
            var keyPhraseDocument = keyPhrasesResponse.Value.FirstOrDefault();
            if (keyPhraseDocument == null)
            {
                throw new InvalidOperationException("Key phrase extraction failed: No document returned");
            }
            
            if (keyPhraseDocument.HasError)
            {
                throw new InvalidOperationException(
                    $"Key phrase extraction failed: {keyPhraseDocument.Error.Message}"
                );
            }

            // Map key phrases to domain objects
            // Strategy to get more key phrases:
            // 1. Use key phrases from extraction service
            // 2. Add entity texts as additional key phrases (if not already present)
            // This combines multiple sources to maximize key phrase extraction.
            var keyPhrasesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keyPhrases = new List<KeyPhrase>();

            // Add key phrases from extraction service
            foreach (var kp in keyPhraseDocument.KeyPhrases)
            {
                if (!string.IsNullOrWhiteSpace(kp) && keyPhrasesSet.Add(kp))
                {
                    keyPhrases.Add(KeyPhrase.Create(kp, 1.0));
                }
            }

            // Process entities
            var entityDocument = entitiesResponse.Value.FirstOrDefault();
            if (entityDocument == null)
            {
                throw new InvalidOperationException("Entity recognition failed: No document returned");
            }
            
            if (entityDocument.HasError)
            {
                throw new InvalidOperationException(
                    $"Entity recognition failed: {entityDocument.Error.Message}"
                );
            }

            // Map entities to domain objects
            var entities = entityDocument.Entities
                .Select(e => Entity.Create(e.Text, e.Category.ToString(), e.ConfidenceScore))
                .ToList();

            // Add entity texts as additional key phrases (if not already present)
            // This helps get more key phrases by leveraging entity recognition
            foreach (var entity in entityDocument.Entities)
            {
                if (!string.IsNullOrWhiteSpace(entity.Text) && keyPhrasesSet.Add(entity.Text))
                {
                    // Use entity confidence score for key phrase
                    keyPhrases.Add(KeyPhrase.Create(entity.Text, entity.ConfidenceScore));
                }
            }

            _logger.LogInformation(
                "Azure Language API call completed - ComplaintId: {ComplaintId}, Sentiment: {Sentiment}, KeyPhrases: {KeyPhraseCount} (from extraction + entities), Entities: {EntityCount}",
                complaint.Id,
                sentiment.Label,
                keyPhrases.Count,
                entities.Count
            );

            return AnalysisResult.Create(sentiment, keyPhrases, entities);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Azure Language API request failed - ComplaintId: {ComplaintId}, ErrorCode: {ErrorCode}",
                complaint.Id,
                ex.ErrorCode ?? "Unknown"
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during analysis - ComplaintId: {ComplaintId}",
                complaint.Id
            );
            throw;
        }
    }
}
