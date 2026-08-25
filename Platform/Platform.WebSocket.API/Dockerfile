FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.WebSocket.API Platform.WebSocket.API

WORKDIR /source/Platform.WebSocket.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -c Release -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.WebSocket.API/publish .
CMD ["dotnet", "Platform.WebSocket.API.dll"]
