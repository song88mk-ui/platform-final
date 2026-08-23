# song88/plt-rc-api
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform.RedCat.API Platform.RedCat.API
ADD Platform.Shared Platform.Shared

WORKDIR /source/Platform.RedCat.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -c Release -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.RedCat.API/publish .
CMD ["/usr/bin/dotnet", "Platform.RedCat.API.dll"]
