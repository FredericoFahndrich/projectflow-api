FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY Directory.Build.props ProjectFlow.sln ./
COPY src/ProjectFlow.Api/ProjectFlow.Api.csproj src/ProjectFlow.Api/
RUN dotnet restore src/ProjectFlow.Api/ProjectFlow.Api.csproj
COPY src/ProjectFlow.Api/ src/ProjectFlow.Api/
RUN dotnet publish src/ProjectFlow.Api/ProjectFlow.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
RUN addgroup -S app && adduser -S app -G app && mkdir -p /app/uploads && chown -R app:app /app
COPY --from=build --chown=app:app /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectFlow.Api.dll"]

