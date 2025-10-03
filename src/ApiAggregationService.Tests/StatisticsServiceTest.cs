using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApiAggregationService.Services;
using Xunit;

namespace ApiAggregationService.Tests
{
    public class StatisticsServiceTests
    {
        private readonly StatisticsService _statisticsService;

        public StatisticsServiceTests()
        {
            _statisticsService = new StatisticsService();
        }

        [Fact]
        public void TrackRequest_ShouldIncrementCount()
        {
            // Arrange
            var initialCount = _statisticsService.GetRequestCount();

            // Act
            _statisticsService.TrackRequest();

            // Assert
            Assert.Equal(initialCount + 1, _statisticsService.GetRequestCount());
        }

        [Fact]
        public void TrackRequest_ShouldRecordElapsedTime()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            _statisticsService.TrackRequest();
            stopwatch.Stop();

            // Assert
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            var recordedTime = _statisticsService.GetAverageResponseTime();

            Assert.True(recordedTime >= 0);
            Assert.True(recordedTime <= elapsedTime);
        }

        [Fact]
        public void GetStatistics_ShouldReturnCorrectMetrics()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                _statisticsService.TrackRequest();
            }

            // Act
            var stats = _statisticsService.GetStatistics();

            // Assert
            Assert.Equal(5, stats.RequestCount);
            Assert.True(stats.AverageResponseTime >= 0);
        }
    }
}