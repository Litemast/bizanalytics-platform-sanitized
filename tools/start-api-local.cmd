@echo off
cd /d "C:\Users\corsar\Desktop\проекты\BizAnalyticsPlatform\BizAnalytics.Api\bin\Debug\net9.0"
set ASPNETCORE_URLS=http://127.0.0.1:5000
set LOG_FILE=C:\Users\corsar\Desktop\проекты\BizAnalyticsPlatform\backend-runtime.log
echo [%date% %time%] starting BizAnalytics.Api>> "%LOG_FILE%"
"C:\Program Files\dotnet\dotnet.exe" BizAnalytics.Api.dll >> "%LOG_FILE%" 2>&1
