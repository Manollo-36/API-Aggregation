using System.Collections.Generic;
using ApiAggregationService.Models;

namespace ApiAggregationService.Interfaces
{
    public interface IApiStatisticsService
    {
        void LogApiRequest(string apiName, double responseTimeMs);
        IEnumerable<ApiStatistics> GetAllStats();
        ApiStatistics GetStatsByApiName(string apiName);
        void ClearStats();
    }
}