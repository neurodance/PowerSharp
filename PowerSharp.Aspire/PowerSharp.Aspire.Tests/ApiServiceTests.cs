using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace PowerSharp.Aspire.Tests;

public class ApiServiceTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task GetWeatherForecast_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/weatherforecast", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsValidJson()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var forecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast", cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.NotEmpty(forecasts);
        Assert.All(forecasts, forecast =>
        {
            Assert.True(forecast.Date >= DateOnly.FromDateTime(DateTime.Now));
            Assert.InRange(forecast.TemperatureC, -20, 55);
            Assert.NotNull(forecast.Summary);
        });
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsFiveForecasts()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var forecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast", cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/health", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AliveEndpoint_ReturnsHealthy()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        
        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/alive", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

// Model for deserialization
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
