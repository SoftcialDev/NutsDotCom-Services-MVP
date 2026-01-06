using ComplaintAnalysis.Application.DTOs;
using ComplaintAnalysis.Domain.Entities;
using ComplaintAnalysis.Domain.Services;
using Microsoft.Extensions.Configuration;

namespace ComplaintAnalysis.Application.UseCases;

public class ProcessComplaintUseCase
{
    private readonly IComplaintAnalyzer _analyzer;
    private readonly IConfiguration _configuration;

    public ProcessComplaintUseCase(
        IComplaintAnalyzer analyzer,
        IConfiguration configuration)
    {
        _analyzer = analyzer;
        _configuration = configuration;
    }

    public async Task<ComplaintAnalysisResponse> ExecuteAsync(ProcessComplaintRequest request)
    {
        var requestId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Create complaint entity
        var complaint = Complaint.Create(request.Text);

        // Analyze complaint
        var analysisResult = await _analyzer.AnalyzeAsync(complaint);

        // Get confidence threshold from configuration (default: 0.5)
        var confidenceThreshold = _configuration.GetValue<double>(
            "KeyPhraseConfidenceThreshold",
            0.5
        );

        // Filter key phrases by confidence threshold
        var filteredKeyPhrases = analysisResult.KeyPhrases
            .Where(kp => kp.ConfidenceScore >= confidenceThreshold)
            .ToList();

        // Map to response DTO
        var response = new ComplaintAnalysisResponse
        {
            RequestId = requestId,
            Timestamp = timestamp,
            Sentiment = analysisResult.Sentiment.Label,
            KeyPhrases = filteredKeyPhrases
                .Select(kp => new KeyPhraseDto
                {
                    Text = kp.Text,
                    ConfidenceScore = kp.ConfidenceScore
                })
                .ToList()
        };

        return response;
    }
}

