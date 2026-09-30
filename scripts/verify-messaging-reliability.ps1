$ErrorActionPreference = 'Stop'

$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
docker info *> $null
$dockerExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorPreference
if ($dockerExitCode -ne 0) {
    throw 'Docker must be running to execute the messaging reliability suite.'
}

$previousInfrastructureTests = $env:RUN_INFRASTRUCTURE_TESTS
try {
    $env:RUN_INFRASTRUCTURE_TESTS = 'true'
    dotnet test tests/NotificationService.IntegrationTests/NotificationService.IntegrationTests.csproj `
        -c Release `
        --filter 'FullyQualifiedName~MessagingReliabilityTests'
    if ($LASTEXITCODE -ne 0) {
        throw 'The messaging reliability suite failed.'
    }
}
finally {
    $env:RUN_INFRASTRUCTURE_TESTS = $previousInfrastructureTests
}
