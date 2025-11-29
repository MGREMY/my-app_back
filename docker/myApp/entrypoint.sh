#!/bin/sh

# Set the port (e.g., 8080)
export ASPNETCORE_HTTP_PORTS=8080

# Start dotnet
exec dotnet Host.Api.dll