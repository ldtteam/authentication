using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LDTTeam.Authentication.Server.Data.Models
{
    public class EndpointMetric
    {
        public Guid Id { get; set; }
        public string Provider { get; set; }
        public string RewardId { get; set; }
        public bool Result { get; set; }
        public long Count { get; set; }
        
        [JsonIgnore]
        public List<HistoricalEndpointMetric> HistoricalMetrics { get; set; }
    }
}