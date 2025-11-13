FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

COPY --from=shared ./Platform.Shared ./Platform.Shared

WORKDIR /source/Platform.Websocket.API2
COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release --no-restore -o /app


FROM mcr.microsoft.com/dotnet/aspnet:7.0
EXPOSE 80
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Platform.Websocket.API2.dll"]
