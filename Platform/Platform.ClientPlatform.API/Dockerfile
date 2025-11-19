FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.ClientPlatform.API Platform.ClientPlatform.API

WORKDIR /source/Platform.ClientPlatform.API

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

COPY --from=build /source/Platform.ClientPlatform.API/publish .
CMD ["/usr/bin/dotnet", "Platform.ClientPlatform.API.dll"]
