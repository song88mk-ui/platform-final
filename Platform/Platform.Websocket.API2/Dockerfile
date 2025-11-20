FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.Websocket.API2 Platform.WebSocket.API

WORKDIR /source/Platform.WebSocket.API

RUN dotnet restore
RUN dotnet build \
  --no-restore \
  --configuration Release
RUN dotnet publish \
  --no-restore \
  --output publish && \
  cd publish && \
  rm -r appsettings.*.json


FROM mcr.microsoft.com/dotnet/aspnet:7.0
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.WebSocket.API/publish .
ENTRYPOINT ["dotnet", "Platform.Websocket.API2.dll"]
