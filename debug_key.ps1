$key = "AIzaSyAL-pLNOKk53QBm7Dw8jV3qq4iA9Fwd1F8"
$url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=$key"
$body = @{
    contents = @(
        @{
            parts = @(
                @{ text = "Hello" }
            )
        }
    )
} | ConvertTo-Json -Depth 4

try {
    Write-Host "Checking Key: $key"
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    Write-Host "✅ Key is VALID (Response received)"
    $response | ConvertTo-Json
}
catch {
    Write-Host "❌ Key is INVALID"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Error Details:"
    $_.ErrorDetails.Message
    $stream = $_.Exception.Response.GetResponseStream()
    if ($stream) {
        $reader = New-Object System.IO.StreamReader($stream)
        $reader.ReadToEnd()
    }
}
