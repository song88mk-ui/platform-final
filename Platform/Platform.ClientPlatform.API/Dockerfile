FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.ClientPlatform.API Platform.ClientPlatform.API

WORKDIR /source/Platform.ClientPlatform.API

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
  -o publish && \
  cd publish && \
  rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.ClientPlatform.API/publish .
CMD ["dotnet", "Platform.ClientPlatform.API.dll"]
