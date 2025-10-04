using System;
using System.Collections.Generic;

namespace ApiAggregationService.Models
{
    public class ApiPerformanceStats
    { public string ApiName { get; set; }
        public int TotalRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public Dictionary<string, int> PerformanceBuckets { get; set; } // e.g., { "fast": 5, "average": 2, "slow": 1 }
    }
}