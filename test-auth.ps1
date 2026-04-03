#!/usr/bin/env pwsh

Write-Host "=== Testing Login ===" -ForegroundColor Cyan

# Test 1: Login
try {
    $loginResult = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" `
      -Method Post `
      -ContentType "application/json" `
      -Body '{"email":"dev@example.com","password":"devpassword"}'
    
    $token = $loginResult.accessToken
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0,30))..."
    Write-Host ""
    
    # Test 2: Entries with token
    Write-Host "=== Testing Entries Endpoint ===" -ForegroundColor Cyan
    try {
        $entriesResult = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/entries?page=1" `
          -Method Get `
          -Headers @{"Authorization"="Bearer $token"}
        Write-Host "✅ GET /entries succeeded!" -ForegroundColor Green
        Write-Host "Entries count: $($entriesResult.data.items.Count)"
        Write-Host ""
        Write-Host "🎉 AUTHENTICATION FLOW COMPLETE AND WORKING!" -ForegroundColor Green
    } catch {
        $status = $_.Exception.Response.StatusCode.Value__
        Write-Host "❌ GET /entries failed with status $status" -ForegroundColor Red
        Write-Host "Error response: $($_.Exception.Response | Out-String)"
    }
} catch {
    Write-Host "❌ Login failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)"
}
