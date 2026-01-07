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
  "sentiment": "Positive" | "Negative" | "Neutral" | "Mixed",
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
   ```powershell
   dotnet restore
   ```

2. Build the solution:
   ```powershell
   dotnet build
   ```

3. Run the Azure Function:
   ```powershell
   cd ComplaintAnalysis.Functions
   func start
   ```

## Deployment

### GitHub Actions CI/CD

The project includes a GitHub Actions workflow for automated deployment to Azure Functions.

**Workflow File**: `.github/workflows/dev_compaint-analysis(staging).yml`

**Trigger**: 
- Automatically on push to `Dev` branch
- Manual trigger via GitHub Actions UI

**Deployment Target**: 
- Azure Function App: `complaint-analysis`
- Slot: `staging`

### Required GitHub Secrets

For the workflow to deploy successfully, you need the following secrets configured in your GitHub repository:

- `AZUREAPPSERVICE_PUBLISHPROFILE_1D809FB594894F1895B8786273E77F6E` - Publish profile from the **staging slot**

### Getting the Staging Publish Profile

To get the publish profile for the staging slot, run:

```powershell
az functionapp deployment list-publishing-profiles `
  --name complaint-analysis `
  --resource-group YOUR_RESOURCE_GROUP `
  --slot staging `
  --xml
```

Copy the entire XML output and update the GitHub secret.

### Manual Deployment

You can also deploy manually using Azure Functions Core Tools:

```powershell
# Navigate to Functions project
cd ComplaintAnalysis.Functions

# Login to Azure
az login

# Deploy to staging slot
func azure functionapp publish complaint-analysis --slot staging
```

Or using ZIP deployment:

```powershell
# Build and publish
dotnet publish ComplaintAnalysis.Functions/ComplaintAnalysis.Functions.csproj -c Release -o ./publish

# Create ZIP
Compress-Archive -Path ./publish/* -DestinationPath functionapp.zip -Force

# Deploy ZIP to staging slot
az functionapp deployment source config-zip `
  --resource-group YOUR_RESOURCE_GROUP `
  --name complaint-analysis `
  --slot staging `
  --src functionapp.zip
```

### Azure Setup

For initial Azure setup and configuration, see `AZURE_SETUP.md` for detailed PowerShell commands to create all required Azure resources.

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

- ✅ Sentiment analysis (Positive, Negative, Neutral, Mixed)
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

