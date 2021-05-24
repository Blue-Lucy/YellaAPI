using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YellaAPI.Models.Nebula;

namespace YellaAPI
{
    public class YellaClient
    {
        private readonly YellaClientConfig _config;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly HttpClient _httpClient;
        private readonly Uri _baseAddress;

        public YellaClient(YellaClientConfig config)
        {
            if (_httpClient == default) { _httpClient = HttpClientFactory.Create(); }

            _config = config;

            var uriBuilder = new UriBuilder($"{config.Host.TrimEnd('/')}/");
            uriBuilder.Port = config.Port ?? uriBuilder.Port;
            _baseAddress = uriBuilder.Uri;

            _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore };
            _serializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };
        }

        public YellaClient(IHttpClientFactory clientFactory, YellaClientConfig config) :
            this(clientFactory.CreateClient(), config) { }

        public YellaClient(HttpClient httpClient, YellaClientConfig config) :
            this(config) { _httpClient = httpClient; }
        
        public void Dispose() => _httpClient.Dispose();

        public Task<NebulaConversionStartResponse> StartConversion(NebulaConversionStartRequest request) =>
            Post<NebulaConversionStartResponse>("api", "pluginapi", request);

        public Task<NebulaConversionGetStatusResponse> GetConversionStatus(NebulaConversionGetStatusRequest request) =>
            Post<NebulaConversionGetStatusResponse>("api", "pluginapi", request);

        public Task<NebulaConversionDeleteResponse> DeleteConversion(NebulaConversionDeleteRequest request) =>
            Post<NebulaConversionDeleteResponse>("api", "pluginapi", request);

        #region Private API Methods
        protected async Task Delete(string path, string dest) => await SendAsync(HttpMethod.Delete, path, dest);

        protected async Task Patch(string path, string dest, object body) => await SendAsync(HttpMethod.Patch, path, dest, body);

        protected async Task Post(string path, string dest, object body) => await SendAsync(HttpMethod.Post, path, dest, body);

        protected async Task Put(string path, string dest, object body) => await SendAsync(HttpMethod.Put, path, dest, body);

        protected async Task SendAsync(HttpMethod method, string path, string dest, object body = null)
        {
            var request = new HttpRequestMessage(method, new Uri(_baseAddress, $"{path}?token={_config.Token}"))
            {
                Content = body == null ? null : GetJsonContent(body)
            };
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        protected async Task<T> Get<T>(string path, string dest) => await SendAsync<T>(HttpMethod.Get, path, dest);

        protected async Task<T> Post<T>(string path, string dest, object body) => await SendAsync<T>(HttpMethod.Post, path, dest, body);

        protected async Task<T> Put<T>(string path, string dest, object body) => await SendAsync<T>(HttpMethod.Put, path, dest, body);

        protected async Task<T> SendAsync<T>(HttpMethod method, string path, string dest, object body = null)
        {
            var request = new HttpRequestMessage(method, new Uri(_baseAddress, $"{path}?token={_config.Token}&dest={dest}"))
            {
                Content = body == null ? null : GetJsonContent(body)
            };
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return ParseResponseBody<T>(responseBody);
        }

        private T ParseResponseBody<T>(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody)) { return default; }
            return JsonConvert.DeserializeObject<T>(responseBody, _serializerSettings);
        }

        private HttpContent GetJsonContent(object body)
        {
            var json = JsonConvert.SerializeObject(body, _serializerSettings);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
        #endregion
    }
}
