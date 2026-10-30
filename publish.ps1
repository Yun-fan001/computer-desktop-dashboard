# Publish self-contained portable build (no .NET runtime required)
$ErrorActionPreference = 'Stop'

if (-not $env:DOTNET_ROOT) { $env:DOTNET_ROOT = 'C:\Users\19899\.dotnet' }
$env:PATH = "$env:DOTNET_ROOT;" + $env:PATH

dotnet publish "src\KanbanBoard\KanbanBoard.csproj" `
    -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "publish"

Write-Host ""
Write-Host "Portable build output: publish\KanbanBoard.exe"

