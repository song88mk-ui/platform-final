FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.CashOutStake.Servicev2 Platform.CashOutStake.Servicev2

WORKDIR /source/Platform.CashOutStake.Servicev2

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

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

# We need the cert to connect to the DocumentDB cluster
RUN curl -o /usr/local/share/ca-certificates/aws-global-bundle.crt https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem && \
    update-ca-certificates

COPY --from=build /source/Platform.CashOutStake.Servicev2/publish .
CMD ["/usr/bin/dotnet", "Platform.CashOutStake.Servicev2.dll"]
