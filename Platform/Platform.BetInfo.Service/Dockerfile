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
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.BetInfo.Service/publish .
CMD ["/usr/bin/dotnet", "Platform.BetInfo.Service.dll"]
