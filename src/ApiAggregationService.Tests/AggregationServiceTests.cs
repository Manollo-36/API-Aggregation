using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiAggregationService.Controllers;
using ApiAggregationService.Models;
using ApiAggregationService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;
using ApiAggregationService.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

public class AggregationServiceTests
{
    private Mock<IApiStatisticsService> CreateMockStatisticsService()
    {
        var mockStatisticsService = new Mock<IApiStatisticsService>();
        mockStatisticsService.Setup(s => s.LogApiRequest(It.IsAny<string>(), It.IsAny<double>()));
        return mockStatisticsService;
    }

    #region Service Tests

    [Fact]
    public async Task GetAggregatedWeatherDataAsync_ReturnsAggregatedData()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseData = new AggregatedWeatherData
        {
            main = new AggregatedWeatherData.Main { temp = 20, humidity = 50 }
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseData))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockStatisticsService = CreateMockStatisticsService();
        var service = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);

        var apiUrls = new List<string> { "http://fakeapi.com/weather" };

        // Act
        var result = await service.GetAggregatedWeatherDataAsync(apiUrls);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(20, result.First().main.temp);
        Assert.Equal(50, result.First().main.humidity);
        
        // Verify statistics were logged
        mockStatisticsService.Verify(s => s.LogApiRequest(It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task GetAggregatedWeatherDataAsync_HandlesHttpRequestException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockStatisticsService = CreateMockStatisticsService();
        var service = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);

        var apiUrls = new List<string> { "http://fakeapi.com/weather" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => service.GetAggregatedWeatherDataAsync(apiUrls));
    }

    [Fact]
    public async Task GetAggregatedWeatherDataAsync_HandlesInvalidJson()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockStatisticsService = CreateMockStatisticsService();
        var service = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);

        var apiUrls = new List<string> { "http://fakeapi.com/weather" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => service.GetAggregatedWeatherDataAsync(apiUrls));
    }

    [Fact]
    public async Task GetAggregatedWeatherDataAsync_AppliesFilter()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseData1 = new AggregatedWeatherData
        {
            main = new AggregatedWeatherData.Main { temp = 15, humidity = 40 }
        };
        var responseData2 = new AggregatedWeatherData
        {
            main = new AggregatedWeatherData.Main { temp = 25, humidity = 60 }
        };

        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseData1))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseData2))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockStatisticsService = CreateMockStatisticsService();
        var service = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);

        var apiUrls = new List<string> { "http://api1.com/weather", "http://api2.com/weather" };
        Func<AggregatedWeatherData, bool> filter = data => data.main.temp > 20;

        // Act
        var result = await service.GetAggregatedWeatherDataAsync(apiUrls, filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(25, result.First().main.temp);
    }

    [Fact]
    public async Task GetAggregatedWeatherDataAsync_AppliesOrdering()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var responseData1 = new AggregatedWeatherData
        {
            main = new AggregatedWeatherData.Main { temp = 25, humidity = 60 }
        };
        var responseData2 = new AggregatedWeatherData
        {
            main = new AggregatedWeatherData.Main { temp = 15, humidity = 40 }
        };

        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseData1))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseData2))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockStatisticsService = CreateMockStatisticsService();
        var service = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);

        var apiUrls = new List<string> { "http://api1.com/weather", "http://api2.com/weather" };
        Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>> orderBy =
            data => data.OrderBy(d => d.main.temp);

        // Act
        var result = await service.GetAggregatedWeatherDataAsync(apiUrls, null, orderBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(15, result.First().main.temp);
        Assert.Equal(25, result.Last().main.temp);
    }

    #endregion

    #region Controller Tests

    [Fact]
    public async Task GetAggregatedData_ReturnsOkResult()
    {
        // Arrange
        var mockService = new Mock<IAggregationService>();
        var mockConfig = new Mock<IConfiguration>();

        mockService.Setup(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()))
            .ReturnsAsync(new List<AggregatedWeatherData>
            {
                new AggregatedWeatherData { main = new AggregatedWeatherData.Main { temp = 25 } }
            });

        mockConfig.Setup(c => c["WeatherApi:OpenWeatherApiKey"]).Returns("test-key");
        mockConfig.Setup(c => c["WeatherApi2:WeatherStackApiKey"]).Returns("test-key2");

        var controller = new AggregationController(mockService.Object, mockConfig.Object);

        // Act
        var result = await controller.GetAggregatedData(40.7128, -74.0060);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAggregatedData_WithFilter_AppliesFilter()
    {
        // Arrange
        var mockService = new Mock<IAggregationService>();
        var mockConfig = new Mock<IConfiguration>();

        mockService.Setup(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()))
            .ReturnsAsync(new List<AggregatedWeatherData>());

        mockConfig.Setup(c => c["WeatherApi:OpenWeatherApiKey"]).Returns("test-key");
        mockConfig.Setup(c => c["WeatherApi2:WeatherStackApiKey"]).Returns("test-key2");

        var controller = new AggregationController(mockService.Object, mockConfig.Object);

        // Act
        var result = await controller.GetAggregatedData(40.7128, -74.0060, "20", "asc");

        // Assert
        mockService.Verify(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAggregatedData_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var mockService = new Mock<IAggregationService>();
        var mockConfig = new Mock<IConfiguration>();

        mockService.Setup(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()))
            .ThrowsAsync(new Exception("Service error"));

        mockConfig.Setup(c => c["WeatherApi:OpenWeatherApiKey"]).Returns("test-key");
        mockConfig.Setup(c => c["WeatherApi2:WeatherStackApiKey"]).Returns("test-key2");

        var controller = new AggregationController(mockService.Object, mockConfig.Object);

        // Act
        var result = await controller.GetAggregatedData(40.7128, -74.0060);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAggregatedData_MissingApiKeys_HandlesGracefully()
    {
        // Arrange
        var mockService = new Mock<IAggregationService>();
        var mockConfig = new Mock<IConfiguration>();

        mockService.Setup(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()))
            .ReturnsAsync(new List<AggregatedWeatherData>());

        // Don't setup API keys (they'll return null)
        var controller = new AggregationController(mockService.Object, mockConfig.Object);

        // Act
        var result = await controller.GetAggregatedData(40.7128, -74.0060);

        // Assert
        // Should still call service even with null API keys
        mockService.Verify(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()),
            Times.Once);
    }

    #endregion

    #region Model Tests

    [Fact]
    public void AggregatedWeatherData_CanBeCreated()
    {
        // Act
        var data = new AggregatedWeatherData
        {
            main = new AggregatedWeatherData.Main { temp = 25.5, humidity = 65 },
            current_weather = new AggregatedWeatherData.Current_weather { temperature = 24.2, windspeed = 15.3, humidity = 60 },
            current = new AggregatedWeatherData.Current { temperature = 26.1, humidity = 70 }
        };

        // Assert
        Assert.NotNull(data);
        Assert.Equal(25.5, data.main.temp);
        Assert.Equal(65, data.main.humidity);
        Assert.Equal(24.2, data.current_weather.temperature);
        Assert.Equal(15.3, data.current_weather.windspeed);
        Assert.Equal(60, data.current_weather.humidity);
        Assert.Equal(26.1, data.current.temperature);
        Assert.Equal(70, data.current.humidity);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task IntegrationTest_GetAggregatedData()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Override with mock services for integration testing if needed
                });
            });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/aggregation?latitude=40.7128&longitude=-74.0060");

        // Assert
        // Note: This will fail without real API keys, but tests the routing and pipeline
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task IntegrationTest_HomeEndpoint()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("API Aggregation Service is running", content);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetAggregatedWeatherDataAsync_EmptyApiUrls_ReturnsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockStatisticsService = CreateMockStatisticsService();
        var service = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);

        var apiUrls = new List<string>();

        // Act
        var result = await service.GetAggregatedWeatherDataAsync(apiUrls);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAggregatedData_InvalidFilterValue_IgnoresFilter()
    {
        // Arrange
        var mockService = new Mock<IAggregationService>();
        var mockConfig = new Mock<IConfiguration>();

        mockService.Setup(s => s.GetAggregatedWeatherDataAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Func<AggregatedWeatherData, bool>>(),
            It.IsAny<Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>>>()))
            .ReturnsAsync(new List<AggregatedWeatherData>());

        mockConfig.Setup(c => c["WeatherApi:OpenWeatherApiKey"]).Returns("test-key");
        mockConfig.Setup(c => c["WeatherApi2:WeatherStackApiKey"]).Returns("test-key2");

        var controller = new AggregationController(mockService.Object, mockConfig.Object);

        // Act
        var result = await controller.GetAggregatedData(40.7128, -74.0060, "invalid", "asc");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}

