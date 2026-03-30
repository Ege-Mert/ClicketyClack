param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

$env:DOTNET_CLI_HOME = $repoRoot
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot ".dotnet") | Out-Null

$publishRoot = Join-Path $repoRoot "publish"
$outputDir = Join-Path $publishRoot $Runtime
$zipPath = Join-Path $publishRoot ("ClicketyClack-{0}-{1}.zip" -f $Version, $Runtime)

if (Test-Path $outputDir)
{
    Remove-Item -LiteralPath $outputDir -Recurse -Force
}

if (Test-Path $zipPath)
{
    Remove-Item -LiteralPath $zipPath -Force
}

dotnet publish .\ClicketyClack.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $outputDir `
    /p:PublishSingleFile=true `
    /p:EnableCompressionInSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    /p:Version=$Version

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed with exit code $LASTEXITCODE. If this is the first RID-specific publish on this machine, dotnet may need network access to restore runtime packages."
}

Compress-Archive -Path (Join-Path $outputDir "*") -DestinationPath $zipPath -Force

Write-Host "Published to: $outputDir"
Write-Host "Zip ready for GitHub release: $zipPath"
