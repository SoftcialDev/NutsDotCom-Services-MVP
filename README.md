# Complaint Analysis Service

A backend service built with Azure Functions that processes complaints using Azure AI Language services to extract sentiment and key phrases. The solution follows Domain-Driven Design (DDD) principles.

## Architecture

The solution is organized into four layers following DDD:

- **Domain Layer** (`ComplaintAnalysis.Domain`): Core business entities, value objects, and domain service interfaces
- **Application Layer** (`ComplaintAnalysis.Application`): Use cases and DTOs
- **Infrastructure Layer** (`ComplaintAnalysis.Infrastructure`): Azure AI integration and logging adapters
- **Presentation Layer** (`ComplaintAnalysis.Functions`): Azure Function HTTP trigger

## Prerequisites

- .NET 8.0 SDK
- Azure subscription with Language Service resource
- Azure Functions Core Tools (for local development)

## Configuration

Update `local.settings.json` with your Azure Language Service credentials:

```json
{
  "Values": {
    "AZURE_LANGUAGE_ENDPOINT": "https://your-resource.cognitiveservices.azure.com/",
    "AZURE_LANGUAGE_KEY": "your-key",
    "KeyPhraseConfidenceThreshold": "0.5"
  }
}
```

## API Endpoint

**POST** `/api/complaints`

### Request Body
```json
{
  "text": "Your complaint text here"
}
```

### Response
```json
{
  "requestId": "guid",
  "timestamp": "2024-01-15T10:30:00Z",
  "sentiment": "Positive" | "Negative" | "Neutral",
  "keyPhrases": [
    {
      "text": "key phrase",
      "confidenceScore": 1.0
    }
  ]
}
```

## Running Locally

1. Restore packages:
   ```bash
   dotnet restore
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the Azure Function:
   ```bash
   cd ComplaintAnalysis.Functions
   func start
   ```

## Project Structure

```
ComplaintAnalysisService/
├── ComplaintAnalysis.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Models/
│   └── Services/
├── ComplaintAnalysis.Application/
│   ├── DTOs/
│   └── UseCases/
├── ComplaintAnalysis.Infrastructure/
│   ├── AzureAI/
│   └── Logging/
└── ComplaintAnalysis.Functions/
    ├── Functions/
    ├── host.json
    └── local.settings.json
```

## Features

- ✅ Sentiment analysis (Positive, Negative, Neutral)
- ✅ Key phrase extraction
- ✅ Confidence-based filtering for key phrases
- ✅ Comprehensive logging
- ✅ Error handling
- ✅ Request/Response tracking with RequestId and Timestamp

## Notes

- The service is stateless (no persistence)
- Key phrases are filtered by confidence threshold (default: 0.5)
- All key phrases above the threshold are returned (no limit)
- Sentiment returns only the primary label (capitalized)

