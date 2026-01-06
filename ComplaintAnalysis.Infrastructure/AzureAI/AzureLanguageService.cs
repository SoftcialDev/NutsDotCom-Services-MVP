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

            // Analyze sentiment
            var sentimentResponse = await _client.AnalyzeSentimentBatchAsync(
                documents,
                options: new AnalyzeSentimentOptions
                {
                    IncludeStatistics = false,
                    IncludeOpinionMining = false
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
            // Note: Azure doesn't provide confidence scores for key phrases directly
            // We'll use a default confidence of 1.0 or calculate based on document sentiment
            var keyPhrases = keyPhraseDocument.KeyPhrases
                .Select(kp => KeyPhrase.Create(kp, 1.0))
                .ToList();

            _logger.LogInformation(
                "Azure Language API call completed - ComplaintId: {ComplaintId}, Sentiment: {Sentiment}, KeyPhrases: {Count}",
                complaint.Id,
                sentiment.Label,
                keyPhrases.Count
            );

            return AnalysisResult.Create(sentiment, keyPhrases);
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

