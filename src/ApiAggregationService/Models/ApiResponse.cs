using System;

namespace ApiAggregationService.Models
{
    public class ApiResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}