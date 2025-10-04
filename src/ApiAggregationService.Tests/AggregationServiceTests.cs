using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Xunit;
using ApiAggregationService.Models;
using ApiAggregationService.Services;
using Microsoft.Extensions.Caching.Memory;

namespace ApiAggregationService.Tests
{
    public class AggregationServiceTests
    {
        private readonly Mock<HttpClient> _httpClientMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly AggregationService _aggregationService;

        public AggregationServiceTests()
        {
            _httpClientMock = new Mock<HttpClient>();
            _cacheMock = new Mock<IMemoryCache>();
            _aggregationService = new AggregationService(_httpClientMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task GetAggregatedWeatherDataAsync_ReturnsAggregatedData()
        {
            // Arrange
            var apiUrls = new List<string>
            {
                "http://api1.com/weather",
                "http://api2.com/weather",
                "http://api3.com/weather"
            };

            var mockResponse = new List<AggregatedWeatherData.Main >
            {
                new AggregatedWeatherData.Main { temp = 20, humidity = 50 },
                new AggregatedWeatherData.Main { temp = 25, humidity = 60 }
            };

            _httpClientMock.Setup(client => client.GetStringAsync(It.IsAny<string>()))
                .ReturnsAsync(JsonConvert.SerializeObject(mockResponse));

            // Act
            var result = await _aggregationService.GetAggregatedWeatherDataAsync(apiUrls);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(22.5, result.First().main.temp);
            Assert.Equal(55, result.First().main.humidity);
        }

        [Fact]
        public async Task GetAggregatedWeatherDataAsync_WithFilter_ReturnsFilteredData()
        {
            // Arrange
            var apiUrls = new List<string>
            {
                "http://api1.com/weather",
                "http://api2.com/weather"
            };

            var mockResponse = new List<AggregatedWeatherData.Main>
            {
                new AggregatedWeatherData.Main { temp = 20, humidity = 50 },
                new AggregatedWeatherData.Main { temp = 30, humidity = 70 }
            };

            _httpClientMock.Setup(client => client.GetStringAsync(It.IsAny<string>()))
                .ReturnsAsync(JsonConvert.SerializeObject(mockResponse));

            Func<AggregatedWeatherData, bool> filter = data => data.main.temp > 25;

            // Act
            var result = await _aggregationService.GetAggregatedWeatherDataAsync(apiUrls, filter);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(30, result.First().main.temp);
        }

        [Fact]
        public async Task GetAggregatedWeatherDataAsync_WithError_ReturnsNull()
        {
            // Arrange
            var apiUrls = new List<string>
            {
                "http://api1.com/weather"
            };

            _httpClientMock.Setup(client => client.GetStringAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await _aggregationService.GetAggregatedWeatherDataAsync(apiUrls);

            // Assert
            Assert.Null(result);
        }
    }
}