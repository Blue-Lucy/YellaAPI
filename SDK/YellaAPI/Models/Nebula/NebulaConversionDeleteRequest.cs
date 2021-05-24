using Newtonsoft.Json;
using System;

namespace YellaAPI.Models.Nebula
{
    public class NebulaConversionDeleteRequest
    {
        [JsonProperty]
        public string Cmd { get; } = "deletestatus";

        public Guid Id { get; set; }

        public string Pluginname { get; set; }
    }
}
