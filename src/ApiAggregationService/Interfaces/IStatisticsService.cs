using System.Collections.Generic;
using ApiAggregationService.Models;

namespace ApiAggregationService.Services
{
    public interface IApiStatisticsService
    {
        void LogApiRequest(string apiName, long responseTimeMs);
        IEnumerable<ApiPerformanceStats> GetAllStats();
    }
}