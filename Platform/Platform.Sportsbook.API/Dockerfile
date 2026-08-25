FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.Sportsbook.API Platform.Sportsbook.API

WORKDIR /source/Platform.Sportsbook.API

ARG NUGET_PASSWORD

RUN dotnet nuget add source \
  --username FlyingCockatoo \
  --password ${NUGET_PASSWORD} \
  --store-password-in-clear-text \
  --name github "https://nuget.pkg.github.com/FlyingCockatoo/index.json"

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -c Release -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app

# We need the cert to connect to the DocumentDB cluster (ADD instead of curl: curl is not in aspnet:10.0)
ADD https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem /usr/local/share/ca-certificates/aws-global-bundle.crt
RUN update-ca-certificates

COPY --from=build /source/Platform.Sportsbook.API/publish .
CMD ["dotnet", "Platform.Sportsbook.API.dll"]
