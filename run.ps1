# DAHD — one-click local launcher
# Opens the API and the Angular frontend in two separate PowerShell windows
# so they keep running independently. Run from the repo root:  .\run.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "Starting DAHD API (http://localhost:5070) ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$root\api'; dotnet run --project Dahd.Api --launch-profile http"
)

Start-Sleep -Seconds 2

Write-Host "Starting DAHD frontend (http://localhost:4200) ..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$root\web'; npm start"
)

Write-Host ""
Write-Host "Two windows are launching:" -ForegroundColor Green
Write-Host "  API      -> http://localhost:5070   (Swagger: /swagger)"
Write-Host "  Frontend -> http://localhost:4200"
Write-Host ""
Write-Host "First login after the DB has been idle may take a moment while LocalDB wakes up — that's normal."
Write-Host "Close the two windows to stop the servers."
