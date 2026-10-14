#!/bin/sh
set -eu

project_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$project_dir"

dotnet tool restore
dotnet test SupermarketAPI.Tests.csproj \
  --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults

dotnet tool run reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:CoverageReport \
  -reporttypes:Html