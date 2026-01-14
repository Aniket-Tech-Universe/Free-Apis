$key = "AIzaSyDD1R1zA4l7BWMfKXcJLIM2wObhCBu1MvA"
$url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=$key"
$body = @{ contents = @(@{ parts = @(@{ text = "Hi" }) }) } | ConvertTo-Json -Depth 4

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    Write-Host "RESPONSE: 200 OK" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 5 | Write-Host
}
catch {
    Write-Host "STATUS CODE: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "BODY:"
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $reader.ReadToEnd() | Write-Host
}
