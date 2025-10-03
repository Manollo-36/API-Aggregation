using System;

namespace ApiAggregationService.Models
{
    public class AggregatedWeatherData
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Condition { get; set; }
        public DateTime Timestamp { get; set; }
    }
}