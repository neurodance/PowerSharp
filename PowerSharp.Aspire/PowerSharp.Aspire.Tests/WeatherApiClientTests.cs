using System.Net;
using System.Text.Json;
using PowerSharp.Aspire.Web;

namespace PowerSharp.Aspire.Tests;

public class WeatherApiClientTests
{
    [Fact]
    public async Task GetWeatherAsync_ReturnsForecasts()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var weatherClient = new WeatherApiClient(httpClient);

        // Act
        var forecasts = await weatherClient.GetWeatherAsync(cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.NotEmpty(forecasts);
    }

    [Fact]
    public async Task GetWeatherAsync_RespectsMaxItems()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var weatherClient = new WeatherApiClient(httpClient);

        // Act
        var forecasts = await weatherClient.GetWeatherAsync(maxItems: 3, cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.True(forecasts.Length <= 3, $"Expected at most 3 forecasts, but got {forecasts.Length}");
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsValidTemperatureConversion()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var weatherClient = new WeatherApiClient(httpClient);

        // Act
        var forecasts = await weatherClient.GetWeatherAsync(maxItems: 1, cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.NotEmpty(forecasts);
        
        var forecast = forecasts[0];
        // Verify Fahrenheit conversion formula: F = C / 0.5556 + 32
        var expectedF = 32 + (int)(forecast.TemperatureC / 0.5556);
        Assert.Equal(expectedF, forecast.TemperatureF);
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsValidDates()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var weatherClient = new WeatherApiClient(httpClient);

        // Act
        var forecasts = await weatherClient.GetWeatherAsync(cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.All(forecasts, forecast =>
        {
            Assert.True(forecast.Date >= DateOnly.FromDateTime(DateTime.Today), 
                $"Forecast date {forecast.Date} should be today or in the future");
        });
    }

    [Fact]
    public async Task GetWeatherAsync_HandlesEmptyResult()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PowerSharp_Aspire_AppHost>(cancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var weatherClient = new WeatherApiClient(httpClient);

        // Act
        var forecasts = await weatherClient.GetWeatherAsync(maxItems: 0, cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(forecasts);
        Assert.Empty(forecasts);
    }
}
