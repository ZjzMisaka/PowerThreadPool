param(
    [string]$Version = "1.0.0"
)
dotnet pack PowerThreadPool/PowerThreadPool.csproj -c Release /p:Version=$Version