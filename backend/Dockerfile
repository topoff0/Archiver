FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.config ./
COPY src/Archiver.Domain/Archiver.Domain.csproj src/Archiver.Domain/
COPY src/Archiver.Application/Archiver.Application.csproj src/Archiver.Application/
COPY src/Archiver.Api/Archiver.Api.csproj src/Archiver.Api/
COPY tests/Archiver.Tests/Archiver.Tests.csproj tests/Archiver.Tests/

RUN dotnet restore src/Archiver.Application/Archiver.Application.csproj --configfile NuGet.config
RUN dotnet restore tests/Archiver.Tests/Archiver.Tests.csproj --configfile NuGet.config
RUN dotnet restore src/Archiver.Api/Archiver.Api.csproj --configfile NuGet.config

COPY src ./src
COPY tests ./tests
RUN dotnet build src/Archiver.Application/Archiver.Application.csproj -c Release --no-restore
RUN dotnet build tests/Archiver.Tests/Archiver.Tests.csproj -c Release --no-restore
RUN dotnet run --project tests/Archiver.Tests/Archiver.Tests.csproj -c Release --no-build --no-restore
RUN dotnet publish src/Archiver.Api/Archiver.Api.csproj -c Release -o /app/publish --no-restore

FROM build AS test
ENTRYPOINT ["dotnet", "run", "--project", "tests/Archiver.Tests/Archiver.Tests.csproj", "-c", "Release", "--no-build", "--no-restore"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Archiver.Api.dll"]
