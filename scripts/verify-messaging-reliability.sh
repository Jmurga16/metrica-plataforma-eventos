#!/usr/bin/env bash
set -euo pipefail

docker info >/dev/null

RUN_INFRASTRUCTURE_TESTS=true dotnet test \
  tests/NotificationService.IntegrationTests/NotificationService.IntegrationTests.csproj \
  -c Release \
  --filter 'FullyQualifiedName~MessagingReliabilityTests'
