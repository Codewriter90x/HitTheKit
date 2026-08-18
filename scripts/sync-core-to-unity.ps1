$ErrorActionPreference = "Stop"

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepositoryRoot = (Resolve-Path (Join-Path $ScriptDirectory "..")).Path
$CoreProject = Join-Path $RepositoryRoot "src/HitTheKit.Core/HitTheKit.Core.csproj"
$PluginDirectory = Join-Path $RepositoryRoot "src/HitTheKit.Unity/Assets/Plugins/HitTheKit.Core"
$Configuration = if ($env:CONFIGURATION) { $env:CONFIGURATION } else { "Debug" }

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet is required to build HitTheKit.Core."
}

[xml]$ProjectXml = Get-Content -LiteralPath $CoreProject
$TargetFramework = [string]$ProjectXml.Project.PropertyGroup.TargetFramework
if ($TargetFramework -ne "netstandard2.1") {
    throw "Expected HitTheKit.Core to target netstandard2.1, found '$TargetFramework'."
}

& dotnet build $CoreProject --configuration $Configuration --framework $TargetFramework
if ($LASTEXITCODE -ne 0) {
    throw "HitTheKit.Core build failed with exit code $LASTEXITCODE."
}

$SourceDll = Join-Path $RepositoryRoot "src/HitTheKit.Core/bin/$Configuration/$TargetFramework/HitTheKit.Core.dll"
if (-not (Test-Path -LiteralPath $SourceDll -PathType Leaf)) {
    throw "Core build succeeded but '$SourceDll' was not produced."
}

New-Item -ItemType Directory -Path $PluginDirectory -Force | Out-Null
Copy-Item -LiteralPath $SourceDll -Destination (Join-Path $PluginDirectory "HitTheKit.Core.dll") -Force
Write-Host "Synchronized HitTheKit.Core.dll to '$PluginDirectory'."
