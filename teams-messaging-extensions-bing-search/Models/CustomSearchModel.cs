using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TeamsMessagingExtensionsSearchAuthConfig.Models
{
    public class CustomSearchModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }

        [JsonProperty(PropertyName = "snippet")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "dateLastCrawled")]
        public DateTime DatePublished { get; set; }
    }
}
