using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft_Teams_Graph_RESTAPIs_Connect.Models
{
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class LOBData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("task")]
        public MessageCard Task { get; set; }

        [JsonProperty("assignedTo")]
        public string AssignedTo { get; set; }

        [JsonProperty("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }


}