# DAHD — consistent SQLite backup
# Produces a single-file, torn-write-free snapshot of the live dahd.db using
# SQLite's VACUUM INTO (safe even while the API is running).
#
#   .\backup-db.ps1                       -> api\Dahd.Api\backups\dahd-backup-<timestamp>.db
#   .\backup-db.ps1 -Dest D:\safe\dahd.db -> that exact path

param([string]$Dest)

$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "api\Dahd.Api"

if (-not $Dest) {
    $backupDir = Join-Path $apiDir "backups"
    if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir | Out-Null }
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $Dest = Join-Path $backupDir "dahd-backup-$stamp.db"
}

Write-Host "Backing up dahd.db -> $Dest" -ForegroundColor Cyan
Push-Location $apiDir
try {
    dotnet run -c Release -- backup "$Dest"
}
finally {
    Pop-Location
}
Write-Host "Done." -ForegroundColor Green
