FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RoadmapPlatform.slnx ./
COPY RoadmapPlatform.Api/RoadmapPlatform.Api.csproj RoadmapPlatform.Api/
COPY RoadmapPlatform.Application/RoadmapPlatform.Application.csproj RoadmapPlatform.Application/
COPY RoadmapPlatform.Infrastructure/RoadmapPlatform.Infrastructure.csproj RoadmapPlatform.Infrastructure/
COPY RoadmapPlatform.MarketPulseJob/RoadmapPlatform.MarketPulseJob.csproj RoadmapPlatform.MarketPulseJob/

RUN dotnet restore RoadmapPlatform.slnx

COPY . .
RUN dotnet publish RoadmapPlatform.Api/RoadmapPlatform.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "RoadmapPlatform.Api.dll"]