# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY backend/backend.csproj backend/
RUN dotnet restore backend/backend.csproj

COPY backend/ backend/
RUN dotnet publish backend/backend.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "backend.dll"]
