# DAHD — one-command deploy packaging
# Produces a single self-contained bundle in .\dist-server that one Kestrel
# process serves (API + Angular UI on the same port), carrying your current
# data and a fresh production secret.
#
#   .\publish.ps1                 -> .\dist-server  (fresh JWT key, current dahd.db)
#   .\publish.ps1 -FreshDb        -> ship an empty DB (auto-seeds on first run)
#   .\publish.ps1 -Out D:\deploy  -> output elsewhere

param(
    [string]$Out = (Join-Path $PSScriptRoot "dist-server"),
    [switch]$FreshDb
)

$ErrorActionPreference = "Stop"
$root   = $PSScriptRoot
$apiDir = Join-Path $root "api"
$webDir = Join-Path $root "web"

function Section($t) { Write-Host "`n=== $t ===" -ForegroundColor Cyan }

# 1) clean output
if (Test-Path $Out) { Remove-Item $Out -Recurse -Force }
New-Item -ItemType Directory -Path $Out | Out-Null

# 2) publish the API
Section "Publishing API (Release)"
dotnet publish (Join-Path $apiDir "Dahd.Api") -c Release -o $Out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# 3) build the Angular UI (production => apiUrl '/api') and stage into wwwroot
Section "Building frontend (production)"
Push-Location $webDir
try {
    if (-not (Test-Path (Join-Path $webDir "node_modules"))) { npm ci }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "ng build failed" }
}
finally { Pop-Location }
$wwwroot = Join-Path $Out "wwwroot"
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item (Join-Path $webDir "dist\web\browser\*") $wwwroot -Recurse -Force

# 4) database — ship current data (default) or a fresh empty DB
Section "Staging database"
$destDb = Join-Path $Out "dahd.db"
if ($FreshDb) {
    Write-Host "  -FreshDb: no DB shipped; the app will create + seed on first run."
} else {
    $liveDb = Join-Path $apiDir "Dahd.Api\dahd.db"
    if (Test-Path $liveDb) {
        Push-Location (Join-Path $apiDir "Dahd.Api")
        try { dotnet run -c Release --no-build -- backup "$destDb" }
        finally { Pop-Location }
        Write-Host "  Shipped a consistent snapshot of current data -> dahd.db"
    } else {
        Write-Host "  No live dahd.db found; the app will create + seed on first run."
    }
}

# 5) production appsettings with a fresh random JWT key
Section "Writing appsettings.Production.json"
$bytes = New-Object 'System.Byte[]' 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
$settings = [ordered]@{
    Logging           = @{ LogLevel = @{ Default = "Information"; "Microsoft.AspNetCore" = "Warning" } }
    AllowedHosts      = "*"
    Database          = @{ Provider = "Sqlite" }
    ConnectionStrings = @{ Sqlite = "Data Source=dahd.db" }
    Jwt               = @{ Issuer = "DahdApi"; Audience = "DahdClient"; Key = $jwtKey; AccessTokenMinutes = 60; RefreshTokenDays = 14 }
}
($settings | ConvertTo-Json -Depth 6) | Out-File (Join-Path $Out "appsettings.Production.json") -Encoding utf8

# 6) run instructions inside the bundle
@"
DAHD — server bundle
====================
One process serves the API and the UI.

Run:
  set ASPNETCORE_ENVIRONMENT=Production
  set ASPNETCORE_URLS=http://0.0.0.0:8080
  dotnet Dahd.Api.dll
Then open http://<server>:8080  (login admin / admin123 — change it!)

Files:
  Dahd.Api.dll ...............  the app (API + serves wwwroot)
  wwwroot\ ...................  built Angular UI
  dahd.db ....................  SQLite database (your data). Back it up regularly.
  appsettings.Production.json   provider=Sqlite, DB path, a freshly generated JWT key

Notes:
- Put HTTPS in front (Nginx/IIS/Caddy) for production.
- The JWT key here is unique to this bundle. Keep it secret.
- Restore = replace dahd.db with a known-good backup while the app is stopped.
"@ | Out-File (Join-Path $Out "README-DEPLOY.txt") -Encoding utf8

Section "Done"
Write-Host "Bundle ready: $Out" -ForegroundColor Green
Write-Host "Copy that folder to the server and run:  dotnet Dahd.Api.dll" -ForegroundColor Green
