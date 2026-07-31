param(
    [Parameter(Mandatory = $true)]
    [string]$RemoteUrl,

    [string]$Branch = "main",

    [string]$Message = "Initial System Guardian app"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location -LiteralPath $root

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git is not installed or is not available in PATH."
}

if (-not (Test-Path -LiteralPath (Join-Path $root ".git"))) {
    git init
}

git add .

$pending = git status --porcelain
if ($pending) {
    git commit -m $Message
}
else {
    Write-Host "No file changes to commit."
}

git branch -M $Branch

$origin = git remote get-url origin 2>$null
if ($LASTEXITCODE -eq 0 -and $origin) {
    git remote set-url origin $RemoteUrl
}
else {
    git remote add origin $RemoteUrl
}

git push -u origin $Branch
