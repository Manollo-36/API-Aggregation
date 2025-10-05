using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;

namespace ApiAggregationService.Services
{
    public class ApiStatisticsService : IApiStatisticsService
    {
        private readonly ConcurrentBag<RequestLog> _requestLogs = new();
        private readonly object _lockObject = new();

        public void LogApiRequest(string apiName, double responseTimeMs)
        {
            _requestLogs.Add(new RequestLog
            {
                ApiName = apiName,
                ResponseTimeMs = responseTimeMs,
                Timestamp = System.DateTime.UtcNow
            });
        }

        public IEnumerable<ApiStatistics> GetAllStats()
        {
            lock (_lockObject)
            {
                return _requestLogs
                    .GroupBy(r => r.ApiName)
                    .Select(g => CreateApiStatistics(g.Key, g.ToList()))
                    .ToList();
            }
        }

        public ApiStatistics GetStatsByApiName(string apiName)
        {
            lock (_lockObject)
            {
                var logs = _requestLogs.Where(r => r.ApiName.Equals(apiName, StringComparison.OrdinalIgnoreCase)).ToList();
                return logs.Any() ? CreateApiStatistics(apiName, logs) : null;
            }
        }

        public void ClearStats()
        {
            lock (_lockObject)
            {
                _requestLogs.Clear();
            }
        }

        private ApiStatistics CreateApiStatistics(string apiName, List<RequestLog> logs)
        {
            if (!logs.Any()) return null;

            var responseTimes = logs.Select(l => l.ResponseTimeMs).ToList();
            
            return new ApiStatistics
            {
                ApiName = apiName,
                TotalRequests = logs.Count,
                AverageResponseTimeMs = Math.Round(responseTimes.Average(), 2),
                PerformanceBuckets = new PerformanceBuckets
                {
                    Fast = responseTimes.Count(rt => rt < 100),
                    Average = responseTimes.Count(rt => rt >= 100 && rt <= 200),
                    Slow = responseTimes.Count(rt => rt > 200)
                }
            };
        }
    }
}