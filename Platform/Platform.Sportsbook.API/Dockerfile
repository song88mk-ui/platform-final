FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.Sportsbook.API Platform.Sportsbook.API

WORKDIR /source/Platform.Sportsbook.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

# We need the cert to connect to the DocumentDB cluster
RUN curl -o /usr/local/share/ca-certificates/aws-global-bundle.crt https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem && \
    update-ca-certificates

COPY --from=build /source/Platform.Sportsbook.API/publish .
CMD ["/usr/bin/dotnet", "Platform.Sportsbook.API.dll"]
