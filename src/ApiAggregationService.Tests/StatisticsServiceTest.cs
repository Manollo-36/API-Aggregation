using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ApiAggregationService.Services;

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
            svc.LogApiRequest("Crypto", 200);  // slow
            svc.LogApiRequest("Crypto", 300);  // slow

            var stat = svc.GetAllStats().FirstOrDefault(s => s.ApiName == "Crypto");
            Assert.NotNull(stat);
            Assert.Equal(6, stat.TotalRequests);

            Assert.Equal(2, stat.PerformanceBuckets["fast (<100ms)"]);
            Assert.Equal(2, stat.PerformanceBuckets["average (100-200ms)"]);
            Assert.Equal(2, stat.PerformanceBuckets["slow (>=200ms)"]);
        }

        [Fact]
        public void ReturnsEmptyStatsWhenNoRequests()
        {
            var svc = new ApiStatisticsService();
            var stat = svc.GetAllStats().FirstOrDefault(s => s.ApiName == "MissingApi");
            Assert.Null(stat);
        }
    }
}