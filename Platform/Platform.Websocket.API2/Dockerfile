FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

COPY --from=shared ./Platform.Shared ./Platform.Shared

WORKDIR /source/Platform.Websocket.API2
COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
RUN apt-get update -qq && \
    apt-get install curl -y && \
    apt-get clean && \
    rm -fr /var/lib/apt/lists/*

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Platform.Websocket.API2.dll"]
