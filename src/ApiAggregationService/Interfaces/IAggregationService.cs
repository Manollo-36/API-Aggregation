using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiAggregationService.Models;

namespace ApiAggregationService.Interfaces
{
    public interface IAggregationService
    {
        Task<IEnumerable<AggregatedWeatherData>> GetAggregatedWeatherDataAsync(
            IEnumerable<string> apiUrls,
            Func<AggregatedWeatherData, bool> filter = null,
            Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>> orderBy = null);
    }
}