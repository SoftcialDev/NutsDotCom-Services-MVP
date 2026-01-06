# Azure Setup Script for Complaint Analysis Service
# This script creates all required Azure resources

param(
    [string]$ResourceGroupName = "rg-complaint-analysis",
    [string]$Location = "eastus",
    [string]$LanguageServiceName = "complaint-analysis-lang-$(Get-Random -Maximum 9999)",
    [string]$FunctionAppName = "func-complaint-analysis-$(Get-Random -Maximum 9999)",
    [string]$StorageAccountName = "stcomplaint$(Get-Random -Maximum 9999)",
    [string]$AppServicePlanName = "asp-complaint-analysis"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Azure Setup for Complaint Analysis Service" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Azure CLI is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Install from: https://aka.ms/installazurecliwindows" -ForegroundColor Yellow
    exit 1
}

# Check if logged in
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

Write-Host "`nStep 1: Creating Resource Group..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create resource group" -ForegroundColor Red
    exit 1
}

Write-Host "Step 2: Creating Language Service (Cognitive Services)..." -ForegroundColor Yellow
az cognitiveservices account create `
    --name $LanguageServiceName `
    --resource-group $ResourceGroupName `
    --kind TextAnalytics `
    --sku S `
    --location $Location

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create Language Service" -ForegroundColor Red
    exit 1
}

$languageEndpoint = az cognitiveservices account show `
    --name $LanguageServiceName `
    --resource-group $ResourceGroupName `
    --query properties.endpoint `
    --output tsv

$languageKey = az cognitiveservices account keys list `
    --name $LanguageServiceName `
    --resource-group $ResourceGroupName `
    --query key1 `
    --output tsv

Write-Host "Step 3: Creating Storage Account..." -ForegroundColor Yellow
az storage account create `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Standard_LRS

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create storage account" -ForegroundColor Red
    exit 1
}

$storageConnectionString = az storage account show-connection-string `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    --query connectionString `
    --output tsv

Write-Host "Step 4: Creating App Service Plan (Consumption Plan)..." -ForegroundColor Yellow
az appservice plan create `
    --name $AppServicePlanName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Y1 `
    --is-linux false

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create App Service Plan" -ForegroundColor Red
    exit 1
}

Write-Host "Step 5: Creating Function App..." -ForegroundColor Yellow
az functionapp create `
    --name $FunctionAppName `
    --storage-account $StorageAccountName `
    --resource-group $ResourceGroupName `
    --plan $AppServicePlanName `
    --runtime dotnet-isolated `
    --functions-version 4 `
    --os-type Windows

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create Function App" -ForegroundColor Red
    exit 1
}

Write-Host "Step 6: Configuring Function App Settings..." -ForegroundColor Yellow
az functionapp config appsettings set `
    --name $FunctionAppName `
    --resource-group $ResourceGroupName `
    --settings `
        "AZURE_LANGUAGE_ENDPOINT=$languageEndpoint" `
        "AZURE_LANGUAGE_KEY=$languageKey" `
        "KeyPhraseConfidenceThreshold=0.5" `
        "AzureWebJobsStorage=$storageConnectionString" `
        "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to configure app settings" -ForegroundColor Red
    exit 1
}

$functionAppUrl = az functionapp show `
    --name $FunctionAppName `
    --resource-group $ResourceGroupName `
    --query defaultHostName `
    --output tsv

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green

Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "Language Service: $LanguageServiceName" -ForegroundColor White
Write-Host "Function App: $FunctionAppName" -ForegroundColor White
Write-Host "Function App URL: https://$functionAppUrl" -ForegroundColor White
Write-Host "API Endpoint: https://$functionAppUrl/api/complaints" -ForegroundColor White

Write-Host "`n=== IMPORTANT: Save These Values ===" -ForegroundColor Yellow
Write-Host "Language Service Endpoint: $languageEndpoint" -ForegroundColor Cyan
Write-Host "Language Service Key: $languageKey" -ForegroundColor Cyan
Write-Host "`nUpdate your local.settings.json with these values!" -ForegroundColor Yellow

Write-Host "`nNext Steps:" -ForegroundColor Green
Write-Host "1. Update ComplaintAnalysis.Functions/local.settings.json with the endpoint and key above"
Write-Host "2. Deploy your function: cd ComplaintAnalysis.Functions && func azure functionapp publish $FunctionAppName"
Write-Host "3. Test the API: https://$functionAppUrl/api/complaints"

