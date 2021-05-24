using Newtonsoft.Json;

namespace YellaAPI.Models.Nebula
{
    public class NebulaConversionGetStatusRequest
    {
        [JsonProperty]
        public string Cmd { get; } = "getstatus";

        public string Pluginname { get; set; }
    }
}
