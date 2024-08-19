FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN apt-get update
RUN apt-get upgrade
RUN apt-get install -y gdal-bin libgdal-dev

USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GisHelper/GisHelper.csproj", "GisHelper/"]
RUN dotnet restore "./GisHelper/GisHelper.csproj"
COPY . .
WORKDIR "/src/GisHelper"
RUN dotnet build "./GisHelper.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GisHelper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GisHelper.dll"]