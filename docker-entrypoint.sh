#!/bin/sh
set -eu

PORT_VALUE="${PORT:-10000}"
export ASPNETCORE_URLS="http://0.0.0.0:${PORT_VALUE}"

echo "HealthPlatform MVP iniciando em 0.0.0.0:${PORT_VALUE}"
exec dotnet HealthPlatform.Api.dll --hostBuilder:reloadConfigOnChange=false
