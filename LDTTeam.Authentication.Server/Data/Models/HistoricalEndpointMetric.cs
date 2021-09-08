using System;
using System.Text.Json.Serialization;

namespace LDTTeam.Authentication.Server.Data.Models
{
    public class HistoricalEndpointMetric
    {
        public Guid Id { get; set; }

        public DateTimeOffset DateTime { get; set; }
        
        public long Count { get; set; }

        [JsonIgnore]
        public EndpointMetric Metric { get; set; }
    }
}