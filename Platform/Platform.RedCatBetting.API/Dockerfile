# song88/plt-rc-betting-api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ADD Platform.RedCatBetting.API Platform.RedCatBetting.API
ADD Platform.Shared Platform.Shared

WORKDIR /source/Platform.RedCatBetting.API

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -c Release -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.RedCatBetting.API/publish .
CMD ["dotnet", "Platform.RedCatBetting.API.dll"]
