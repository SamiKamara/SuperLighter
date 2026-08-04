$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "SuperLighter.App\SuperLighter.App.csproj"
$output = Join-Path $PSScriptRoot "artifacts\publish\win-x64"

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    -o $output

Write-Host "Published SuperLighter.exe to $output"
