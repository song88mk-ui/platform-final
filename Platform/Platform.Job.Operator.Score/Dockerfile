FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /source

ADD Platform/Platform.Shared Platform.Shared
ADD Platform/Platform.Job.Operator.Score Platform.Job.Operator.Score

WORKDIR /source/Platform.Job.Operator.Score

RUN dotnet restore
RUN dotnet build --configuration Release
RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Platform.Job.Operator.Score/publish .
CMD ["/usr/bin/dotnet", "Platform.Job.Operator.Score.dll"]
