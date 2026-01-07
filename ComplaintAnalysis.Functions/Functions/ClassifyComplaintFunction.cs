using System.Net;
using ComplaintAnalysis.Application.DTOs;
using ComplaintAnalysis.Application.UseCases;
using ComplaintAnalysis.Infrastructure.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ComplaintAnalysis.Functions.Functions;

public class ClassifyComplaintFunction
{
    private readonly ClassifyComplaintUseCase _useCase;
    private readonly ILoggerAdapter _logger;
    private readonly IConfiguration _configuration;

    public ClassifyComplaintFunction(
        ClassifyComplaintUseCase useCase,
        ILoggerAdapter logger,
        IConfiguration configuration)
    {
        _useCase = useCase;
        _logger = logger;
        _configuration = configuration;
    }

    [Function("ClassifyComplaint")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "complaints/labels")] HttpRequestData req,
        FunctionContext executionContext)
    {
        // Handle CORS preflight request
        if (req.Method == "OPTIONS")
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response, req);
            return response;
        }

        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        try
        {
            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Request received with empty body - RequestId: {RequestId}", requestId);
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required", requestId);
            }

            // Deserialize request
            ClassifyComplaintRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<ClassifyComplaintRequest>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Invalid JSON in request body - RequestId: {RequestId}, Error: {Error}", requestId, ex.Message);
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format", requestId);
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Complaint))
            {
                _logger.LogWarning("Request received with missing or empty complaint - RequestId: {RequestId}", requestId);
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Complaint is required", requestId);
            }

            if (string.IsNullOrWhiteSpace(request.Sentiment))
            {
                _logger.LogWarning("Request received with missing sentiment - RequestId: {RequestId}", requestId);
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Sentiment is required", requestId);
            }

            // Log incoming request
            _logger.LogInformation(
                "Request received - RequestId: {RequestId}, Complaint length: {ComplaintLength}, Sentiment: {Sentiment}, KeyPhrases: {KeyPhraseCount}, Timestamp: {Timestamp}",
                requestId,
                request.Complaint.Length,
                request.Sentiment,
                request.KeyPhrases?.Count ?? 0,
                startTime
            );

            // Execute use case
            var response = await _useCase.ExecuteAsync(request);

            // Log response
            _logger.LogInformation(
                "Response sent - RequestId: {RequestId}, Severity: {Severity}, Labels: {LabelCount}, Status: 200",
                response.RequestId,
                response.Severity,
                response.Labels.Count
            );

            // Return success response
            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            httpResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            AddCorsHeaders(httpResponse, req);
            
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await httpResponse.WriteStringAsync(jsonResponse);
            return httpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred - RequestId: {RequestId}, Exception: {ExceptionMessage}",
                requestId,
                ex.Message
            );

            return CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.",
                requestId
            );
        }
    }

    private HttpResponseData CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string errorMessage,
        Guid requestId)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        AddCorsHeaders(response, req);

        var errorResponse = new
        {
            error = errorMessage,
            requestId = requestId
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        response.WriteStringAsync(jsonResponse).Wait();
        return response;
    }

    private void AddCorsHeaders(HttpResponseData response, HttpRequestData request)
    {
        // Get origin from request
        string? origin = null;
        if (request.Headers.TryGetValues("Origin", out var originValues))
        {
            origin = originValues.FirstOrDefault();
        }

        // Get allowed origins from configuration or use defaults for development
        var configuredOrigins = _configuration["CORS_ALLOWED_ORIGINS"];
        var allowedOrigins = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredOrigins))
        {
            // Parse comma-separated origins from configuration
            allowedOrigins = configuredOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        else
        {
            // Default development origins
            allowedOrigins = new List<string>
            {
                "http://localhost:5175",
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:5174",
                "http://localhost:5176"
            };
        }

        // Determine allowed origin
        string allowedOrigin;
        if (!string.IsNullOrWhiteSpace(origin) && allowedOrigins.Contains(origin))
        {
            allowedOrigin = origin;
        }
        else if (string.IsNullOrWhiteSpace(configuredOrigins))
        {
            // For development, allow all origins if not configured
            allowedOrigin = origin ?? "*";
        }
        else
        {
            // For production with configured origins, use first allowed origin or deny
            allowedOrigin = allowedOrigins.FirstOrDefault() ?? "*";
        }

        response.Headers.Add("Access-Control-Allow-Origin", allowedOrigin);
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.Headers.Add("Access-Control-Max-Age", "3600");
    }
}

