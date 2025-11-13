FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . ./
RUN dotnet restore && dotnet publish -o out src/host/MyApp.Host.Api/MyApp.Host.Api.csproj

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080

COPY --chmod=755 docker/myApp/entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]