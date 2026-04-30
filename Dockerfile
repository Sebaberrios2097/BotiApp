# ── Etapa 1: compilar y publicar ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish BotiApp/BotiApp.csproj -c Release -o /app/publish

# ── Etapa 2: imagen final (solo runtime) ─────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "publish/BotiApp.dll"]
