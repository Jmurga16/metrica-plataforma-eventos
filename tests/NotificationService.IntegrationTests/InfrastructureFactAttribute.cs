using System.Runtime.CompilerServices;

namespace NotificationService.IntegrationTests;

public sealed class InfrastructureFactAttribute : FactAttribute
{
    public InfrastructureFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!NotificationApiFixture.IsEnabled)
        {
            Skip = "Set RUN_INFRASTRUCTURE_TESTS=true to run the Testcontainers suite.";
        }
    }
}
