FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.Job.Resettlement Platform.Job.Resettlement

WORKDIR /source/Platform.Job.Resettlement

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.Job.Resettlement/publish .
CMD ["/usr/bin/dotnet", "Platform.Job.Resettlement.dll"]
