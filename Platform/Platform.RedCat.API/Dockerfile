# song88/plt-rc-api
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform.RedCat.API Platform.RedCat.API
ADD Platform.Shared Platform.Shared

WORKDIR /source/Platform.RedCat.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
RUN apt-get update -qq && \
    apt-get install curl -y && \
    apt-get clean && \
    rm -fr /var/lib/apt/lists/*

COPY --from=build /source/Platform.RedCat.API/publish .
CMD ["/usr/bin/dotnet", "Platform.RedCat.API.dll"]
