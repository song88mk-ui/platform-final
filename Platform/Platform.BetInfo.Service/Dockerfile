FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.BetInfo.Service Platform.BetInfo.Service

WORKDIR /source/Platform.BetInfo.Service

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
RUN apt-get update -qq && \
    apt-get install curl -y && \
    apt-get clean && \
    rm -fr /var/lib/apt/lists/*

COPY --from=build /source/Platform.BetInfo.Service/publish .
CMD ["/usr/bin/dotnet", "Platform.BetInfo.Service.dll"]
