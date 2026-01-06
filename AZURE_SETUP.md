# Azure Setup Guide - PowerShell Commands

This guide provides PowerShell commands to configure all required Azure services for the Complaint Analysis Service.

## Prerequisites

1. **Azure CLI** installed and logged in:
   ```powershell
   az login
   ```

2. **Azure PowerShell Module** (optional, alternative to Azure CLI):
   ```powershell
   Install-Module -Name Az -AllowClobber -Scope CurrentUser
   Connect-AzAccount
   ```

## Step 1: Set Variables

```powershell
# Set your variables
$resourceGroupName = "rg-complaint-analysis"
$location = "eastus"  # Change to your preferred region
$languageServiceName = "complaint-analysis-lang"  # Must be globally unique
$functionAppName = "func-complaint-analysis"  # Must be globally unique
$storageAccountName = "stcomplaintanalysis"  # Must be globally unique, lowercase, alphanumeric only
$appServicePlanName = "asp-complaint-analysis"
```

## Step 2: Create Resource Group

```powershell
# Create resource group
az group create `
    --name $resourceGroupName `
    --location $location
```

## Step 3: Create Language Service (Cognitive Services)

```powershell
# Create Language Service (Text Analytics)
az cognitiveservices account create `
    --name $languageServiceName `
    --resource-group $resourceGroupName `
    --kind TextAnalytics `
    --sku S `
    --location $location

# Get the endpoint
$languageEndpoint = az cognitiveservices account show `
    --name $languageServiceName `
    --resource-group $resourceGroupName `
    --query properties.endpoint `
    --output tsv

# Get the key
$languageKey = az cognitiveservices account keys list `
    --name $languageServiceName `
    --resource-group $resourceGroupName `
    --query key1 `
    --output tsv

# Display the values (save these!)
Write-Host "Language Service Endpoint: $languageEndpoint"
Write-Host "Language Service Key: $languageKey"
```

## Step 4: Create Storage Account (Required for Azure Functions)

```powershell
# Create storage account
az storage account create `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_LRS

# Get storage account connection string
$storageConnectionString = az storage account show-connection-string `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --query connectionString `
    --output tsv
```

## Step 5: Create App Service Plan (Consumption Plan for Cost Efficiency)

```powershell
# Create Consumption Plan (serverless, pay-per-use)
az functionapp plan create `
    --name $appServicePlanName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Y1  # Y1 = Consumption Plan (cheapest option)
```

**Alternative: If the above command doesn't work, use:**

```powershell
# Create App Service Plan manually
az appservice plan create `
    --name $appServicePlanName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Y1 `
    --is-linux false
```

## Step 6: Create Function App

```powershell
# Create Function App
az functionapp create `
    --name $functionAppName `
    --storage-account $storageAccountName `
    --resource-group $resourceGroupName `
    --plan $appServicePlanName `
    --runtime dotnet-isolated `
    --functions-version 4 `
    --os-type Windows
```

## Step 7: Configure Function App Settings

```powershell
# Set application settings
az functionapp config appsettings set `
    --name $functionAppName `
    --resource-group $resourceGroupName `
    --settings `
        "AZURE_LANGUAGE_ENDPOINT=$languageEndpoint" `
        "AZURE_LANGUAGE_KEY=$languageKey" `
        "KeyPhraseConfidenceThreshold=0.5" `
        "AzureWebJobsStorage=$storageConnectionString" `
        "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated"
```

## Step 8: Verify Configuration

```powershell
# List all app settings
az functionapp config appsettings list `
    --name $functionAppName `
    --resource-group $resourceGroupName `
    --output table
```

## Step 9: Get Function App URL

```powershell
# Get the function app URL
$functionAppUrl = az functionapp show `
    --name $functionAppName `
    --resource-group $resourceGroupName `
    --query defaultHostName `
    --output tsv

Write-Host "Function App URL: https://$functionAppUrl"
Write-Host "API Endpoint: https://$functionAppUrl/api/complaints"
```

## Step 10: Update Local Settings (for local development)

After getting the values from Step 3, update your `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_LANGUAGE_ENDPOINT": "YOUR_ENDPOINT_FROM_STEP_3",
    "AZURE_LANGUAGE_KEY": "YOUR_KEY_FROM_STEP_3",
    "KeyPhraseConfidenceThreshold": "0.5"
  }
}
```

## Deployment Commands

### Option 1: Deploy using Azure Functions Core Tools

```powershell
# Install Azure Functions Core Tools (if not installed)
# winget install Microsoft.AzureFunctionsCoreTools

# Navigate to Functions project
cd ComplaintAnalysis.Functions

# Login to Azure
az login

# Deploy
func azure functionapp publish $functionAppName
```

### Option 2: Deploy using ZIP deployment

```powershell
# Build the solution
dotnet publish ComplaintAnalysis.Functions/ComplaintAnalysis.Functions.csproj -c Release -o ./publish

# Create ZIP
Compress-Archive -Path ./publish/* -DestinationPath functionapp.zip -Force

# Deploy ZIP
az functionapp deployment source config-zip `
    --resource-group $resourceGroupName `
    --name $functionAppName `
    --src functionapp.zip
```

## Cost Optimization Tips

1. **Consumption Plan (Y1)**: Pay only for execution time and memory used
2. **Language Service S0 (Standard)**: Pay per transaction
3. **Storage Account (Standard_LRS)**: Minimal cost for function app requirements

## Cleanup (if needed)

```powershell
# Delete entire resource group (removes all resources)
az group delete --name $resourceGroupName --yes --no-wait
```

## Troubleshooting

### Check Function App Logs

```powershell
# Stream logs
az functionapp log tail `
    --name $functionAppName `
    --resource-group $resourceGroupName
```

### Test the Function

```powershell
# Get function key (for testing)
$functionKey = az functionapp function keys list `
    --name $functionAppName `
    --resource-group $resourceGroupName `
    --function-name ProcessComplaint `
    --query default `
    --output tsv

# Test with curl (PowerShell)
$body = @{
    text = "I am very unhappy with the service quality"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://$functionAppUrl/api/complaints" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

## Quick Setup Script (All-in-One)

Save this as `setup-azure.ps1`:

```powershell
# Set variables
$resourceGroupName = "rg-complaint-analysis"
$location = "eastus"
$languageServiceName = "complaint-analysis-lang-$(Get-Random -Maximum 9999)"
$functionAppName = "func-complaint-analysis-$(Get-Random -Maximum 9999)"
$storageAccountName = "stcomplaint$(Get-Random -Maximum 9999)"
$appServicePlanName = "asp-complaint-analysis"

Write-Host "Creating Resource Group..."
az group create --name $resourceGroupName --location $location

Write-Host "Creating Language Service..."
az cognitiveservices account create `
    --name $languageServiceName `
    --resource-group $resourceGroupName `
    --kind TextAnalytics `
    --sku S `
    --location $location

$languageEndpoint = az cognitiveservices account show `
    --name $languageServiceName `
    --resource-group $resourceGroupName `
    --query properties.endpoint --output tsv

$languageKey = az cognitiveservices account keys list `
    --name $languageServiceName `
    --resource-group $resourceGroupName `
    --query key1 --output tsv

Write-Host "Creating Storage Account..."
az storage account create `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_LRS

$storageConnectionString = az storage account show-connection-string `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --query connectionString --output tsv

Write-Host "Creating App Service Plan..."
az appservice plan create `
    --name $appServicePlanName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Y1 `
    --is-linux false

Write-Host "Creating Function App..."
az functionapp create `
    --name $functionAppName `
    --storage-account $storageAccountName `
    --resource-group $resourceGroupName `
    --plan $appServicePlanName `
    --runtime dotnet-isolated `
    --functions-version 4 `
    --os-type Windows

Write-Host "Configuring App Settings..."
az functionapp config appsettings set `
    --name $functionAppName `
    --resource-group $resourceGroupName `
    --settings `
        "AZURE_LANGUAGE_ENDPOINT=$languageEndpoint" `
        "AZURE_LANGUAGE_KEY=$languageKey" `
        "KeyPhraseConfidenceThreshold=0.5" `
        "AzureWebJobsStorage=$storageConnectionString" `
        "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated"

Write-Host "`n=== Configuration Complete ===" -ForegroundColor Green
Write-Host "Language Service Endpoint: $languageEndpoint"
Write-Host "Language Service Key: $languageKey"
Write-Host "Function App Name: $functionAppName"
Write-Host "`nUpdate your local.settings.json with the endpoint and key above." -ForegroundColor Yellow
```

Run it with:
```powershell
.\setup-azure.ps1
```

