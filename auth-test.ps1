#!/usr/bin/env pwsh

Write-Host "=== Journal AI Authentication Flow Test ===" -ForegroundColor Cyan

# Test 1: Backend connectivity
Write-Host "`n[TEST 1] Backend Connectivity" -ForegroundColor Yellow
try {
  $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 3 -ErrorAction SilentlyContinue
  Write-Host "✓ Backend is responding" -ForegroundColor Green
} catch {
  Write-Host "✗ Backend not responding - trying login endpoint instead" -ForegroundColor Yellow
}

# Test 2: Login and get token
Write-Host "`n[TEST 2] Login Request" -ForegroundColor Yellow
$loginBody = @{
  email = "dev@example.com"
  password = "devpassword"
} | ConvertTo-Json

try {
  $loginResponse = Invoke-WebRequest `
    -Uri "http://localhost:5000/api/v1/auth/login" `
    -Method Post `
    -Headers @{"Content-Type" = "application/json"} `
    -Body $loginBody `
    -UseBasicParsing `
    -TimeoutSec 5

  $loginData = $loginResponse.Content | ConvertFrom-Json
  
  Write-Host "✓ Login successful" -ForegroundColor Green
  Write-Host "  Status: $($loginResponse.StatusCode)" -ForegroundColor Green
  Write-Host "  Response structure:" -ForegroundColor Cyan
  $loginData | Get-Member -MemberType NoteProperty | ForEach-Object {
    $prop = $_.Name
    $value = $loginData.$prop
    if ($prop -eq "accessToken") {
      Write-Host "    $prop: $(($value).Substring(0, 20))..." -ForegroundColor Green
    } else {
      Write-Host "    $prop: $value" -ForegroundColor Cyan
    }
  }
  
  $token = $loginData.accessToken
  $user = $loginData.user
  
} catch {
  Write-Host "✗ Login failed: $_" -ForegroundColor Red
  exit 1
}

# Test 3: Verify token structure
Write-Host "`n[TEST 3] Token Structure Analysis" -ForegroundColor Yellow
if ($token) {
  $parts = $token -split '\.'
  if ($parts.Count -eq 3) {
    Write-Host "✓ Token has valid JWT structure (3 parts)" -ForegroundColor Green
    
    # Decode header
    $header = [System.Convert]::FromBase64String(($parts[0] + "===").PadRight(4 * ([Math]::Ceiling($parts[0].Length / 4)), '='))
    $headerJson = [System.Text.Encoding]::UTF8.GetString($header)
    Write-Host "  Header: $headerJson" -ForegroundColor Cyan
    
    # Decode payload
    $payload = [System.Convert]::FromBase64String(($parts[1] + "===").PadRight(4 * ([Math]::Ceiling($parts[1].Length / 4)), '='))
    $payloadJson = [System.Text.Encoding]::UTF8.GetString($payload)
    Write-Host "  Payload: $payloadJson" -ForegroundColor Cyan
  } else {
    Write-Host "✗ Token does not have valid JWT structure" -ForegroundColor Red
  }
} else {
  Write-Host "✗ No token in response" -ForegroundColor Red
}

# Test 4: Protected endpoint without token
Write-Host "`n[TEST 4] Protected Endpoint (No Token)" -ForegroundColor Yellow
try {
  $response = Invoke-WebRequest `
    -Uri "http://localhost:5000/api/v1/entries" `
    -UseBasicParsing `
    -TimeoutSec 5 `
    -ErrorAction Stop
  Write-Host "✗ Endpoint allowed request without token (should have been 401)" -ForegroundColor Red
} catch {
  $statusCode = $_.Exception.Response.StatusCode
  if ($statusCode -eq 401) {
    Write-Host "✓ Endpoint correctly rejected unauthorized request (401)" -ForegroundColor Green
  } else {
    Write-Host "? Endpoint returned status: $statusCode" -ForegroundColor Yellow
  }
}

# Test 5: Protected endpoint WITH token
Write-Host "`n[TEST 5] Protected Endpoint (With Token)" -ForegroundColor Yellow
try {
  $headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
  }
  
  $response = Invoke-WebRequest `
    -Uri "http://localhost:5000/api/v1/entries" `
    -Headers $headers `
    -UseBasicParsing `
    -TimeoutSec 5
  
  Write-Host "✓ Protected endpoint accessible with token" -ForegroundColor Green
  Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
  
  $data = $response.Content | ConvertFrom-Json
  Write-Host "  Response has 'items' property: $($null -ne $data.items)" -ForegroundColor Cyan
  Write-Host "  Item count: $($data.items.Count)" -ForegroundColor Cyan
  
} catch {
  Write-Host "✗ Protected endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
  Write-Host "  Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

# Test 6: Frontend integration test
Write-Host "`n[TEST 6] Frontend Integration" -ForegroundColor Yellow
Write-Host "Login credentials:" -ForegroundColor Cyan
Write-Host "  Email: dev@example.com" -ForegroundColor Cyan
Write-Host "  Password: devpassword" -ForegroundColor Cyan
Write-Host "`nSteps to verify:" -ForegroundColor Cyan
Write-Host "  1. Open http://localhost:5173 in browser" -ForegroundColor Cyan
Write-Host "  2. Enter credentials and click 'Sign in'" -ForegroundColor Cyan
Write-Host "  3. Check browser DevTools > Storage > localStorage:" -ForegroundColor Cyan
Write-Host "     - Should have 'auth_token' key with JWT value" -ForegroundColor Cyan
Write-Host "     - Should have 'auth_user' key with user object" -ForegroundColor Cyan
Write-Host "  4. After login, you should see the dashboard/Timeline" -ForegroundColor Cyan
Write-Host "  5. Refresh the page (F5) - you should stay logged in" -ForegroundColor Cyan
Write-Host "  6. Check Network tab - Authorization header should be sent with requests" -ForegroundColor Cyan

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
