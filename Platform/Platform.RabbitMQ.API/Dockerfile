FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.RabbitMQ.API Platform.RabbitMQ.API

WORKDIR /source/Platform.RabbitMQ.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.RabbitMQ.API/publish .
CMD ["/usr/bin/dotnet", "Platform.RabbitMQ.API.dll"]
