using Newtonsoft.Json;

namespace YellaAPI.Models.Nebula
{
    public class NebulaConversionStartRequest
    {
        [JsonProperty]
        public string Cmd { get; } = "start";

        public string Pluginname { get; set; }

        public string Projectname { get; set; }

        public string Medianame { get; set; }

        public string Mediafolder { get; set; }

        public string Srcfile { get; set; }

        public string Key { get; set; }

        public object Addtext { get; set; }
    }
}
