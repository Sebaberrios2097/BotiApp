# Runtime (usa los artefactos ya compilados por CI)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "BotiApp.dll"]
