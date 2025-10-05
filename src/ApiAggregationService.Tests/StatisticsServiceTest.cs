using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ApiAggregationService.Services;
using ApiAggregationService.Interfaces; 
using Moq;
using System.Net.Http;  // Add this if using Moq

namespace ApiAggregationService.Tests
{
    public class ApiStatisticsServiceTests
    {
        [Fact]
        public void LogsSingleRequestCorrectly()
        {
            var svc = new ApiStatisticsService();
            svc.LogApiRequest("Weather", 123);

            var stat = svc.GetAllStats().FirstOrDefault(s => s.ApiName == "Weather");
            Assert.NotNull(stat);
            Assert.Equal(1, stat.TotalRequests);
            Assert.Equal(123, stat.AverageResponseTimeMs);
        }

        [Fact]
        public void CalculatesAverageForMultipleRequests()
        {
            var svc = new ApiStatisticsService();
            svc.LogApiRequest("News", 100);
            svc.LogApiRequest("News", 200);
            svc.LogApiRequest("News", 300);

            var stat = svc.GetAllStats().FirstOrDefault(s => s.ApiName == "News");
            Assert.NotNull(stat);
            Assert.Equal(3, stat.TotalRequests);
            Assert.Equal(200, stat.AverageResponseTimeMs);
        }

        [Fact]
        public void BucketsRequestsCorrectly()
        {
            var svc = new ApiStatisticsService();
            svc.LogApiRequest("Crypto", 50);   // fast
            svc.LogApiRequest("Crypto", 99);   // fast
            svc.LogApiRequest("Crypto", 100);  // average
            svc.LogApiRequest("Crypto", 150);  // average
            svc.LogApiRequest("Crypto", 200);  // average (200 is <= 200)
            svc.LogApiRequest("Crypto", 300);  // slow

            var stat = svc.GetAllStats().FirstOrDefault(s => s.ApiName == "Crypto");
            Assert.NotNull(stat);
            Assert.Equal(6, stat.TotalRequests);

            // Access properties directly, not as dictionary
            Assert.Equal(2, stat.PerformanceBuckets.Fast);     // < 100ms
            Assert.Equal(3, stat.PerformanceBuckets.Average);  // 100-200ms
            Assert.Equal(1, stat.PerformanceBuckets.Slow);     // > 200ms
        }

        [Fact]
        public void ReturnsEmptyStatsWhenNoRequests()
        {
            var svc = new ApiStatisticsService();
            var stats = svc.GetAllStats();
            var stat = stats.FirstOrDefault(s => s.ApiName == "MissingApi");
            Assert.Null(stat);
        }

        [Fact]
        public void ClearStatsWorksCorrectly()
        {
            var svc = new ApiStatisticsService();
            svc.LogApiRequest("TestApi", 100);
            
            // Verify data exists
            var statsBefore = svc.GetAllStats();
            Assert.Single(statsBefore);
            
            // Clear and verify empty
            svc.ClearStats();
            var statsAfter = svc.GetAllStats();
            Assert.Empty(statsAfter);
        }

        [Fact]
        public void GetStatsByApiNameWorksCorrectly()
        {
            var svc = new ApiStatisticsService();
            svc.LogApiRequest("SpecificApi", 150);
            
            var stat = svc.GetStatsByApiName("SpecificApi");
            Assert.NotNull(stat);
            Assert.Equal("SpecificApi", stat.ApiName);
            Assert.Equal(1, stat.TotalRequests);
            Assert.Equal(150, stat.AverageResponseTimeMs);
        }

        // If you have tests that create AggregationService, fix them like this:
        [Fact]
        public void AggregationService_WithStatisticsService_WorksCorrectly()
        {
            // Option 1: Use real statistics service
            var statisticsService = new ApiStatisticsService();
            var httpClient = new HttpClient();
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            
            var aggregationService = new AggregationService(httpClient, memoryCache, statisticsService);
            
            // Your test logic here...
        }

        [Fact]
        public void AggregationService_WithMockedStatisticsService_WorksCorrectly()
        {
            // Option 2: Use mocked statistics service
            var mockStatisticsService = new Mock<IApiStatisticsService>();
            var httpClient = new HttpClient();
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            
            var aggregationService = new AggregationService(httpClient, memoryCache, mockStatisticsService.Object);
            
            // Your test logic here...
            // You can verify that LogApiRequest was called:
            // mockStatisticsService.Verify(s => s.LogApiRequest(It.IsAny<string>(), It.IsAny<double>()), Times.Once);
        }
    }
}