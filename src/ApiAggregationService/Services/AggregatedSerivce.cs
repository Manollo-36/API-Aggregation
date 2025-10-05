using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;
using System.Diagnostics;

namespace ApiAggregationService.Services
{
    public class AggregationService : IAggregationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IApiStatisticsService _statisticsService;
        private const string CacheKey = "AggregatedWeatherData";

        public AggregationService(HttpClient httpClient, IMemoryCache cache, IApiStatisticsService statisticsService)
        {
            _httpClient = httpClient;
            _cache = cache;
            _statisticsService = statisticsService;
        }

        public async Task<IEnumerable<AggregatedWeatherData>> GetAggregatedWeatherDataAsync(
            IEnumerable<string> apiUrls,
            Func<AggregatedWeatherData, bool> filter = null,
            Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>> orderBy = null)
        {
            var cacheKey = $"{CacheKey}_{string.Join("_", apiUrls)}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<AggregatedWeatherData> cachedData))
            {
                return ApplyFilterAndSort(cachedData, filter, orderBy);
            }

            var tasks = apiUrls.Select(url => FetchWeatherDataAsync(url)).ToArray();
            var results = await Task.WhenAll(tasks);
            
            var aggregatedData = results.SelectMany(r => r).ToList();
            
            _cache.Set(cacheKey, aggregatedData, TimeSpan.FromMinutes(1));
            
            return ApplyFilterAndSort(aggregatedData, filter, orderBy);
        }

        private async Task<IEnumerable<AggregatedWeatherData>> FetchWeatherDataAsync(string apiUrl)
        {
            var stopwatch = Stopwatch.StartNew();
            string apiName = GetApiNameFromUrl(apiUrl);
            
            try
            {
                var response = await _httpClient.GetStringAsync(apiUrl);
                stopwatch.Stop();
                
                // Log the request statistics
                _statisticsService.LogApiRequest(apiName, stopwatch.ElapsedMilliseconds);
                
                var singleResult = JsonConvert.DeserializeObject<AggregatedWeatherData>(response);
                return new List<AggregatedWeatherData> { singleResult };
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _statisticsService.LogApiRequest(apiName, stopwatch.ElapsedMilliseconds);
                throw new Exception($"Error fetching data from {apiUrl}: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _statisticsService.LogApiRequest(apiName, stopwatch.ElapsedMilliseconds);
                throw new Exception($"Error deserializing data from {apiUrl}: {ex.Message}", ex);
            }
        }

        private string GetApiNameFromUrl(string url)
        {
            if (url.Contains("openweathermap.org"))
                return "OpenWeatherMap";
            else if (url.Contains("open-meteo.com"))
                return "OpenMeteo";
            else if (url.Contains("weatherstack.com"))
                return "WeatherStack";
            else
                return "Unknown API";
        }

        private IEnumerable<AggregatedWeatherData> ApplyFilterAndSort(
            IEnumerable<AggregatedWeatherData> data,
            Func<AggregatedWeatherData, bool> filter,
            Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>> orderBy)
        {
            IEnumerable<AggregatedWeatherData> result = data;
            
            if (filter != null)
            {
                result = result.Where(filter);
            }
            
            if (orderBy != null)
            {
                result = orderBy(result);
            }
            
            return result;
        }
    }
}