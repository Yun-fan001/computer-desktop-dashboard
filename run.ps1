# Build and run in dev
$ErrorActionPreference = 'Stop'

if (-not $env:DOTNET_ROOT) { $env:DOTNET_ROOT = 'C:\Users\19899\.dotnet' }
$env:PATH = "$env:DOTNET_ROOT;" + $env:PATH

dotnet build "src\KanbanBoard\KanbanBoard.csproj" -c Debug
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& "src\KanbanBoard\bin\Debug\net8.0-windows\KanbanBoard.exe"

