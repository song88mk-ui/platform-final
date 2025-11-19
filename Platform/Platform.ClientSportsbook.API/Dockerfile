FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.ClientSportsbook.API Platform.ClientSportsbook.API

WORKDIR /source/Platform.ClientSportsbook.API

RUN dotnet restore
RUN dotnet build \
  --no-restore \
  --configuration Release
RUN dotnet publish \
  --no-restore \
  -o publish && \
  cd publish && \
  rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.ClientSportsbook.API/publish .
CMD ["/usr/bin/dotnet", "Platform.ClientSportsbook.API.dll"]
