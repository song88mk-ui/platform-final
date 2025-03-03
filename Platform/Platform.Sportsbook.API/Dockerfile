FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform.Sportsbook.API Platform.Sportsbook.API
ADD Platform.Shared Platform.Shared

WORKDIR /source/Platform.Sportsbook.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.Sportsbook.API/publish .
CMD ["/usr/bin/dotnet", "Platform.Sportsbook.API.dll"]
