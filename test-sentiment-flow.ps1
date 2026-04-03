#!/usr/bin/env pwsh

<#
.SYNOPSIS
Test sentiment flow from entry creation through backend
#>

$baseUrl = "http://localhost:5000/api/v1"
$testEmail = "sentiment-test-$(Get-Random)@example.com"
$testPassword = "Test123!@#"

Write-Host "Test Sentiment Flow" -ForegroundColor Cyan
Write-Host "=" * 50

# Step 1: Register
Write-Host "`nStep 1: Registering user..."
try {
    $payload = @{
        email = $testEmail
        password = $testPassword
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -ContentType "application/json" -Body $payload -ErrorAction Stop
    $token = $response.accessToken.Trim()
    Write-Host "OK: Registered $testEmail"
}
catch {
    Write-Host "Info: User exists, trying login instead..."
    $payload = @{
        email = $testEmail
        password = $testPassword
    } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body $payload -ErrorAction Stop
    $token = $response.accessToken.Trim()
    Write-Host "OK: Logged in"
}

Write-Host "Token received: $($token.Substring(0, 30))..."

# Step 2: Create entry with negative sentiment
Write-Host "`nStep 2: Creating entry with negative sentiment..."

$entryPayload = @{
    title = "Test Entry"
    bodyText = "I do not like this. Really bad."
    confidentiality = "public"
    sentimentScore = -0.4
    sentimentLabel = "Negative"
    sentimentModel = "simple-keyword-analysis"
    tags = @("test")
} | ConvertTo-Json

Write-Host "Sending payload with sentimentScore: -0.4"

$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

$createResponse = Invoke-RestMethod -Uri "$baseUrl/entries" -Method Post -Headers $headers -Body $entryPayload

Write-Host "OK: Entry created"
Write-Host "Response sentimentScore: $($createResponse.sentimentScore)"
Write-Host "Response sentimentLabel: $($createResponse.sentimentLabel)"

$entryId = $createResponse.id

# Step 3: Retrieve to verify persistence
Write-Host "`nStep 3: Retrieving entry..."
$getResponse = Invoke-RestMethod -Uri "$baseUrl/entries/$entryId" -Method Get -Headers $headers
Write-Host "Database has sentimentScore: $($getResponse.sentimentScore)"
Write-Host "Database has sentimentLabel: $($getResponse.sentimentLabel)"

if ($getResponse.sentimentScore -eq -0.4) {
    Write-Host "`nSUCCESS: Sentiment preserved!"
} else {
    Write-Host "`nISSUE: Sentiment lost - expected -0.4, got $($getResponse.sentimentScore)"
}

