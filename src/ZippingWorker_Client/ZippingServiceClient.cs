using System.Xml.Serialization;

namespace ZippingWorker_Client
{
    using Model;

    /// <summary>
    /// Client for interacting with the ZippingWorkerService API
    /// </summary>
    public class ZippingServiceClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ZippingServiceClient
        /// </summary>
        /// <param name="baseUrl">Base URL of the ZippingWorkerService (e.g., "http://localhost:5000")</param>
        /// <param name="httpClient">Optional HttpClient instance. If not provided, a new one will be created.</param>
        public ZippingServiceClient(string baseUrl, HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Submits a zip request to the service
        /// </summary>
        /// <param name="zipInfo">The zip information to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTTP response message</returns>
        public async Task<HttpResponseMessage> SubmitZipRequestAsync(
            ZipInfoType zipInfo, 
            CancellationToken cancellationToken = default)
        {
            if (zipInfo == null)
                throw new ArgumentNullException(nameof(zipInfo));

            var bytes = SerializeToXmlBytes(zipInfo);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            return await _httpClient.PostAsync(
                $"{_baseUrl}/api/zipinfo/binary", 
                content, 
                cancellationToken);
        }

        /// <summary>
        /// Submits a zip request and returns the response content as a string
        /// </summary>
        /// <param name="zipInfo">The zip information to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response content</returns>
        public async Task<string> SubmitZipRequestStringAsync(
            ZipInfoType zipInfo, 
            CancellationToken cancellationToken = default)
        {
            var response = await SubmitZipRequestAsync(zipInfo, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        /// <summary>
        /// Serializes ZipInfoType to XML byte array
        /// </summary>
        private static byte[] SerializeToXmlBytes(ZipInfoType zipInfo)
        {
            // Resolve environment variables before serialization
            zipInfo.PrepareForSerialization();

            var serializer = new XmlSerializer(typeof(ZipInfoType));
            using var ms = new MemoryStream();
            serializer.Serialize(ms, zipInfo);
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes ZipInfoType from XML byte array
        /// </summary>
        public static ZipInfoType DeserializeFromXmlBytes(byte[] data)
        {
            var serializer = new XmlSerializer(typeof(ZipInfoType));
            using var ms = new MemoryStream(data);
            return (ZipInfoType)serializer.Deserialize(ms)!;
        }

        /// <summary>
        /// Serializes ZipInfoType to XML string
        /// </summary>
        public static string SerializeToXmlString(ZipInfoType zipInfo)
        {
            // Resolve environment variables before serialization
            zipInfo.PrepareForSerialization();

            var serializer = new XmlSerializer(typeof(ZipInfoType));
            using var sw = new StringWriter();
            serializer.Serialize(sw, zipInfo);
            return sw.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
