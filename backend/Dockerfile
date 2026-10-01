FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SignalCut.sln", "./"]
COPY ["src/SignalCut.Domain/SignalCut.Domain.csproj", "src/SignalCut.Domain/"]
COPY ["src/SignalCut.Application/SignalCut.Application.csproj", "src/SignalCut.Application/"]
COPY ["src/SignalCut.Infrastructure/SignalCut.Infrastructure.csproj", "src/SignalCut.Infrastructure/"]
COPY ["src/SignalCut.API/SignalCut.API.csproj", "src/SignalCut.API/"]
COPY ["tests/SignalCut.Tests/SignalCut.Tests.csproj", "tests/SignalCut.Tests/"]

RUN dotnet restore "SignalCut.sln"

COPY . .
WORKDIR "/src/src/SignalCut.API"
RUN dotnet build "SignalCut.API.csproj" -c Release -o /app/build
RUN dotnet publish "SignalCut.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install FFmpeg in container for fallback rendering
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "SignalCut.API.dll"]
