FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY Platform/Platform.Shared Platform.Shared
COPY Platform/Platform.Websocket.API2 Platform.WebSocket.API

WORKDIR /source/Platform.WebSocket.API

ARG NUGET_PASSWORD

RUN dotnet nuget add source \
  --username FlyingCockatoo \
  --password ${NUGET_PASSWORD} \
  --store-password-in-clear-text \
  --name github "https://nuget.pkg.github.com/FlyingCockatoo/index.json"

RUN dotnet restore
RUN dotnet build \
  --no-restore \
  --configuration Release
RUN dotnet publish \
  -c Release \
  --no-restore \
  --output publish && \
  cd publish && \
  rm -r appsettings.*.json


FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.WebSocket.API/publish .
USER app
ENTRYPOINT ["dotnet", "Platform.Websocket.API2.dll"]
