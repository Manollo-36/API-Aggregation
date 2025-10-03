using System;

namespace ApiAggregationService.Models
{
    public class Statistics
    {
        public int RequestCount { get; set; }
        public double AverageRequestTime { get; set; }
        public double TotalRequestTime { get; set; }
    }
}