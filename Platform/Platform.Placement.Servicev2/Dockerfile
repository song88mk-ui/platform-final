FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

COPY Platform/Platform.Shared Platform.Shared
COPY Platform/Platform.Placement.Servicev2 Platform.Placement.Servicev2

WORKDIR /source/Platform.Placement.Servicev2

RUN dotnet restore
RUN dotnet build --configuration Release \
  --framework netcoreapp3.1 \
  --runtime linux-x64

RUN dotnet publish -o publish \
  && cd publish \
  && rm -f App.config \
  && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app

RUN apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install -y xmlstarlet \
  && apt-get clean

COPY --from=build /source/Platform.Placement.Servicev2/publish .
COPY --from=build /source/Platform.Placement.Servicev2/entrypoint.sh /app
CMD ["/app/entrypoint.sh"]
