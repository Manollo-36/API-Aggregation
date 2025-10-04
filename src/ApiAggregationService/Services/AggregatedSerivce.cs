using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using ApiAggregationService.Interfaces;
using ApiAggregationService.Models;

namespace ApiAggregationService.Services
{
    public class AggregationService : IAggregationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "AggregatedWeatherData";

        public AggregationService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        // Change return type to IEnumerable<AggregatedWeatherData>
        public async Task<IEnumerable<AggregatedWeatherData>> GetAggregatedWeatherDataAsync(
            IEnumerable<string> apiUrls,
            Func<AggregatedWeatherData, bool> filter = null,
            Func<IEnumerable<AggregatedWeatherData>, IOrderedEnumerable<AggregatedWeatherData>> orderBy = null)
        {
            if (_cache.TryGetValue(CacheKey, out IEnumerable<AggregatedWeatherData> cachedData))
            {
                return cachedData;
            }

            var tasks = apiUrls.Select(url => FetchWeatherDataAsync(url));
            var results = await Task.WhenAll(tasks);

            var aggregatedData = results.SelectMany(data => data).ToList();

            if (filter != null)
            {
                aggregatedData = aggregatedData.Where(filter).ToList();
            }

            if (orderBy != null)
            {
                aggregatedData = orderBy(aggregatedData).ToList();
            }

            _cache.Set(CacheKey, aggregatedData, TimeSpan.FromMinutes(5)); // Cache for 5 minutes

            return aggregatedData;
        }

        private async Task<IEnumerable<AggregatedWeatherData>> FetchWeatherDataAsync(string apiUrl)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(apiUrl);
                
                // Deserialize as a single object, then wrap in a collection
                var singleResult = JsonConvert.DeserializeObject<AggregatedWeatherData>(response);
                
                // Return as a single-item collection
                return new List<AggregatedWeatherData> { singleResult };
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request exceptions
                throw new Exception($"Error fetching data from {apiUrl}: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // Handle JSON deserialization exceptions
                throw new Exception($"Error deserializing data from {apiUrl}: {ex.Message}", ex);
            }
        }
    }
}