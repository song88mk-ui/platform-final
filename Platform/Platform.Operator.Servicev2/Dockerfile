FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY Platform/Platform.Shared Platform.Shared
COPY Platform/Platform.Operator.Servicev2 Platform.Operator.Servicev2

WORKDIR /source/Platform.Operator.Servicev2

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

COPY --from=build /source/Platform.Operator.Servicev2/publish .
USER app
CMD ["dotnet", "Platform.Operator.Servicev2.dll"]
