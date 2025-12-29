param(
    [string]$Version = "1.0.0"
)
$normalizedVersion = $Version -replace '^[vV]', ''
dotnet pack PowerThreadPool/PowerThreadPool.csproj -c Release /p:Version=$normalizedVersion