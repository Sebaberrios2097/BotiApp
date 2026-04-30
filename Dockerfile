FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "publish/BotiApp.dll"]
