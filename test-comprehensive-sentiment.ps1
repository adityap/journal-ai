#!/usr/bin/env pwsh

$baseUrl = "http://localhost:5000/api/v1"
$testEmail = "test-$(Get-Random)@example.com"
$testPassword = "Test123!@#"

Write-Host "Sentiment Flow Test"
Write-Host "==================="

# Register
$payload = @{ email = $testEmail; password = $testPassword } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -ContentType "application/json" -Body $payload
$token = $response.accessToken.Trim()
Write-Host "Registered: $testEmail"

$passCount = 0
$failCount = 0

# Test 1: Positive sentiment
$entryPayload = @{
    title = "Positive Test"
    bodyText = "I love this amazing wonderful thing!"
    confidentiality = "public"
} | ConvertTo-Json

$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }
$result1 = Invoke-RestMethod -Uri "$baseUrl/entries" -Method Post -Headers $headers -Body $entryPayload
$id1 = $result1.id
Write-Host "Created positive entry: Score=$($result1.sentimentScore)"

# Test 2: Negative sentiment
$entryPayload = @{
    title = "Negative Test"
    bodyText = "This is terrible awful horrible"
    confidentiality = "public"
} | ConvertTo-Json
$result2 = Invoke-RestMethod -Uri "$baseUrl/entries" -Method Post -Headers $headers -Body $entryPayload
$id2 = $result2.id
Write-Host "Created negative entry: Score=$($result2.sentimentScore)"

# Test 3: Neutral sentiment
$entryPayload = @{
    title = "Neutral Test"
    bodyText = "This is a normal regular entry"
    confidentiality = "public"
} | ConvertTo-Json
$result3 = Invoke-RestMethod -Uri "$baseUrl/entries" -Method Post -Headers $headers -Body $entryPayload
$id3 = $result3.id
Write-Host "Created neutral entry: Score=$($result3.sentimentScore)"

# Verify persistence
Write-Host "`nVerifying persistence..."
$get1 = Invoke-RestMethod -Uri "$baseUrl/entries/$id1" -Method Get -Headers @{ Authorization = "Bearer $token" }
$get2 = Invoke-RestMethod -Uri "$baseUrl/entries/$id2" -Method Get -Headers @{ Authorization = "Bearer $token" }
$get3 = Invoke-RestMethod -Uri "$baseUrl/entries/$id3" -Method Get -Headers @{ Authorization = "Bearer $token" }

if ($get1.sentimentScore -gt 0) { Write-Host "PASS: Positive persisted"; $passCount++ } else { Write-Host "FAIL: Positive failed"; $failCount++ }
if ($get2.sentimentScore -lt 0) { Write-Host "PASS: Negative persisted"; $passCount++ } else { Write-Host "FAIL: Negative failed"; $failCount++ }
if ($get3.sentimentScore -eq 0) { Write-Host "PASS: Neutral persisted"; $passCount++ } else { Write-Host "FAIL: Neutral failed"; $failCount++ }

Write-Host ""
Write-Host "Results: $passCount passed, $failCount failed"
if ($failCount -eq 0) { Write-Host "SUCCESS!" } else { Write-Host "ISSUES FOUND" }

