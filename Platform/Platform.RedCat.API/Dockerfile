# song88/plt-rc-api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY Platform.RedCat.API Platform.RedCat.API
COPY Platform.Shared Platform.Shared

WORKDIR /source/Platform.RedCat.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -c Release -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.RedCat.API/publish .
CMD ["dotnet", "Platform.RedCat.API.dll"]
