using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace LDTTeam.Authentication.Modules.Api.Logging
{
    public class Embed
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("timestamp")]
        // ReSharper disable once UnusedMember.Global
        public string Timestamp => DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

        [JsonPropertyName("footer")]
        public Footer? EmbedFooter { get; set; }

        public class Footer
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
            
            [JsonPropertyName("icon_url")]
            public string IconUrl { get; set; }
        }
        
        [JsonPropertyName("fields")]
        public List<Field>? Fields { get; set; }

        public class Field
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            
            [JsonPropertyName("value")]
            public string Value { get; set; }
            
            [JsonPropertyName("inline")]
            public bool Inline { get; set; }
        }
    }
}