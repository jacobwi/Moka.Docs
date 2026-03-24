# MokaDocs — Clear build cache (bin, obj, node_modules, _site)
# Works from any directory — auto-detects repo root via .git or .slnx
# Usage: ./ClearProjectsCache.ps1 [-Force] [-DryRun]

param(
    [switch]$Force,
    [switch]$DryRun
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

if (-not ((Test-Path "$repoRoot/.git") -or (Test-Path "$repoRoot/*.slnx"))) {
    Write-Host "Could not find repository root from $repoRoot" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Repository: $repoRoot" -ForegroundColor Cyan
Write-Host ""

$targets = @("bin", "obj", "node_modules", "_site", "_sample-site")
$foldersFound = Get-ChildItem -Path $repoRoot -Directory -Recurse -Include $targets -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notlike "*\.git*" }

if ($foldersFound.Count -eq 0) {
    Write-Host "Already clean — no cache folders found." -ForegroundColor Green
    exit 0
}

$totalSize = 0
foreach ($folder in $foldersFound) {
    $size = (Get-ChildItem -Path $folder.FullName -Recurse -File -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum).Sum
    $totalSize += $size
}

$sizeStr = if ($totalSize -gt 1GB) { "{0:N2} GB" -f ($totalSize / 1GB) }
           elseif ($totalSize -gt 1MB) { "{0:N1} MB" -f ($totalSize / 1MB) }
           else { "{0:N0} KB" -f ($totalSize / 1KB) }

Write-Host "Folders to delete:" -ForegroundColor Yellow
foreach ($folder in $foldersFound) {
    $relPath = $folder.FullName.Substring($repoRoot.Length + 1)
    Write-Host "  $relPath" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Total: $($foldersFound.Count) folders ($sizeStr)" -ForegroundColor Yellow
Write-Host ""

if ($DryRun) {
    Write-Host "Dry run — no folders deleted." -ForegroundColor Cyan
    exit 0
}

if (-not $Force) {
    $confirm = Read-Host "Delete all? (Y/N)"
    if ($confirm.ToUpper() -ne "Y") {
        Write-Host "Cancelled." -ForegroundColor Red
        exit 0
    }
    Write-Host ""
}

$pool = [RunspaceFactory]::CreateRunspacePool(1, 8)
$pool.Open()
$runspaces = @()

foreach ($folder in $foldersFound) {
    $ps = [PowerShell]::Create()
    $ps.RunspacePool = $pool
    [void]$ps.AddScript({
        param($path)
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        $path
    }).AddArgument($folder.FullName)

    $runspaces += [PSCustomObject]@{
        Pipe   = $ps
        Handle = $ps.BeginInvoke()
        Folder = $folder
    }
}

foreach ($rs in $runspaces) {
    $rs.Pipe.EndInvoke($rs.Handle) | Out-Null
    $relPath = $rs.Folder.FullName.Substring($repoRoot.Length + 1)
    Write-Host "  Deleted: $relPath" -ForegroundColor Green
    $rs.Pipe.Dispose()
}

$pool.Close()
$pool.Dispose()

Write-Host ""
Write-Host "Done — freed $sizeStr." -ForegroundColor Green
