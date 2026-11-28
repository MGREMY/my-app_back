FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /app
COPY . ./
RUN dotnet restore && dotnet publish -o out Src/Host/MyApp.Host.Gateway/MyApp.Host.Gateway.csproj

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
COPY --from=build --chown=app:app /app/out .
COPY --chown=app:app --chmod=755 docker/gateway/entrypoint.sh /entrypoint.sh

EXPOSE 8080

USER app
ENTRYPOINT ["/entrypoint.sh"]