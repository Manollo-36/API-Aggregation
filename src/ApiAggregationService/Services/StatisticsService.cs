using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ApiAggregationService.Models;

namespace ApiAggregationService.Services
{
    public class ApiStatisticsService : IApiStatisticsService
    {
        private readonly ConcurrentDictionary<string, List<long>> _apiTimings = new();

        public void LogApiRequest(string apiName, long responseTimeMs)
        {
            var timings = _apiTimings.GetOrAdd(apiName, _ => new List<long>());
            lock (timings)
            {
                timings.Add(responseTimeMs);
            }
        }

        public IEnumerable<ApiPerformanceStats> GetAllStats()
        {
            foreach (var kvp in _apiTimings)
            {
                var times = kvp.Value.ToArray();
                yield return new ApiPerformanceStats
                {
                    ApiName = kvp.Key,
                    TotalRequests = times.Length,
                    AverageResponseTimeMs = times.Length == 0 ? 0 : times.Average(),
                    PerformanceBuckets = new Dictionary<string, int>
                    {
                        { "fast (<100ms)", times.Count(t => t < 100) },
                        { "average (100-200ms)", times.Count(t => t >= 100 && t < 200) },
                        { "slow (>=200ms)", times.Count(t => t >= 200) }
                    }
                };
            }
        }
    }
}