using System;
using System.Diagnostics;
using ApiAggregationService.Models;

namespace ApiAggregationService.Services
{
    public class StatisticsService
    {
        private int _requestCount = 0;
        private long _totalResponseTime = 0;

        public void TrackRequest(long responseTimeMs = 0)
        {
            _requestCount++;
            _totalResponseTime += responseTimeMs;
        }

        public int GetRequestCount()
        {
            return _requestCount;
        }

        public double GetAverageResponseTime()
        {
            return _requestCount == 0 ? 0 : (double)_totalResponseTime / _requestCount;
        }

        public (int RequestCount, double AverageResponseTime) GetStatistics()
        {
            return (_requestCount, GetAverageResponseTime());
        }
    }

}