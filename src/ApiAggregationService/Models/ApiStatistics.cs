using System;

namespace ApiAggregationService.Models
{
    public class ApiStatistics
    {
        public string ApiName { get; set; }
        public int TotalRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public PerformanceBuckets PerformanceBuckets { get; set; }
    }

    public class PerformanceBuckets
    {
        public int Fast { get; set; }      // < 100ms
        public int Average { get; set; }   // 100-200ms
        public int Slow { get; set; }      // > 200ms
    }

    public class RequestLog
    {
        public string ApiName { get; set; }
        public double ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
    }
}