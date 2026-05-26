Set-Location (Join-Path $PSScriptRoot "..\BizAnalytics.Api\bin\Debug\net9.0")
$env:ASPNETCORE_URLS = "http://127.0.0.1:5000"
& "C:\Program Files\dotnet\dotnet.exe" "BizAnalytics.Api.dll"
