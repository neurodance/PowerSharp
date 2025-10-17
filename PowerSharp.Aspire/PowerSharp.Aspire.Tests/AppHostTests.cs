namespace PowerSharp.Aspire.Tests;

public class AppHostTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task AppHost_CanStartSuccessfully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);

        // Act
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Assert - if we get here, the app started successfully
        Assert.NotNull(app);
    }

    [Fact]
    public async Task AppHost_HasExpectedResources()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resources = model.Resources.Select(r => r.Name).ToList();

        // Assert
        Assert.Contains("apiservice", resources);
        Assert.Contains("webfrontend", resources);
    }

    [Fact]
    public async Task AllResources_BecomeHealthy()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act & Assert
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        
        // If we get here without timeout, all resources are healthy
        Assert.True(true);
    }
}
