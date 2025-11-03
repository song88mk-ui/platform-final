FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform.Shared Platform.Shared
ADD Platform.Betting.API Platform.Betting.API

WORKDIR /source/Platform.Betting.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.Betting.API/publish .
CMD ["/usr/bin/dotnet", "Platform.Betting.API.dll"]
