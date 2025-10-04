using System;

namespace ApiAggregationService.Models
{
    public class AggregatedWeatherData
    {
        // public double Temperature { get; set; }
        // public double Humidity { get; set; }
        // public double WindSpeed { get; set; }
        // public string Condition { get; set; }
        // public DateTime Timestamp { get; set; }
        public Main main { get; set; }
        public Current_weather current_weather { get; set; }
        public Current current { get; set; }

        public class Main
        {
            public double temp { get; set; }
            public double humidity { get; set; }
        }
        public class Current_weather
        {
            public double temperature { get; set; }
            public double windspeed { get; set; }
            public double humidity { get; set; }
        }
        public class Current
        {
            public double temperature { get; set; }
            public double humidity { get; set; }
        }
    }
}