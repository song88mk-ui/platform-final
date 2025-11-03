FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.Operator.Cashout.Service Platform.Operator.Cashout.Service

WORKDIR /source/Platform.Operator.Cashout.Service

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.Operator.Cashout.Service/publish .
CMD ["/usr/bin/dotnet", "Platform.Operator.Cashout.Service.dll"]
