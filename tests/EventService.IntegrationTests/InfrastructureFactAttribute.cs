namespace EventService.IntegrationTests;

public sealed class InfrastructureFactAttribute : FactAttribute
{
    public InfrastructureFactAttribute()
    {
        if (!EventApiFixture.IsEnabled)
        {
            Skip = "Set RUN_INFRASTRUCTURE_TESTS=true to run the Testcontainers suite.";
        }
    }
}
