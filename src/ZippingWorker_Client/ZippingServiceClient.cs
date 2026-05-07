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

            var bytes = SerializeToXmlBytes(zipInfo, _baseUrl);
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
        /// Gets the current configuration from the service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTTP response message</returns>
        public async Task<HttpResponseMessage> GetConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetAsync(
                $"{_baseUrl}/api/ZippingWorker_Serviceconfiguration",
                cancellationToken);
        }

        /// <summary>
        /// Gets the current configuration and returns it as a string
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Configuration response content</returns>
        public async Task<string> GetConfigurationStringAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await GetConfigurationAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the service configuration with the specified parameters
        /// </summary>
        /// <param name="servicePort">Service listening port</param>
        /// <param name="sevenZipExePath">Path to 7z.exe</param>
        /// <param name="tempDirSymlink">Temporary directory for symlinks</param>
        /// <param name="tempDirSymlinkCreateIfNotExist">Create symlink temp directory if it doesn't exist</param>
        /// <param name="tempDirZipStaging">Staging directory for zip creation</param>
        /// <param name="tempDirZipStagingCreateIfNotExist">Create zip staging directory if it doesn't exist</param>
        /// <param name="useStaging">Use staging directory for zip creation</param>
        /// <param name="archiver">Archiver type: sevenzip or dotnetzip</param>
        /// <param name="compressionLevel">Compression level: nocompression, fastest, fast, normal, maximum, ultra</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTTP response message</returns>
        public async Task<HttpResponseMessage> UpdateConfigurationAsync(
            int? servicePort = null,
            string? sevenZipExePath = null,
            string? tempDirSymlink = null,
            bool? tempDirSymlinkCreateIfNotExist = null,
            string? tempDirZipStaging = null,
            bool? tempDirZipStagingCreateIfNotExist = null,
            bool? useStaging = null,
            string? archiver = null,
            string? compressionLevel = null,
            CancellationToken cancellationToken = default)
        {
            var queryParams = new List<string>();

            if (servicePort.HasValue)
                queryParams.Add($"serviceport={servicePort.Value}");
            if (!string.IsNullOrWhiteSpace(sevenZipExePath))
                queryParams.Add($"sevenzipexepath={Uri.EscapeDataString(sevenZipExePath)}");
            if (!string.IsNullOrWhiteSpace(tempDirSymlink))
                queryParams.Add($"tempdir_symlink={Uri.EscapeDataString(tempDirSymlink)}");
            if (tempDirSymlinkCreateIfNotExist.HasValue)
                queryParams.Add($"tempdir_symlink_createIfNotExist={tempDirSymlinkCreateIfNotExist.Value}");
            if (!string.IsNullOrWhiteSpace(tempDirZipStaging))
                queryParams.Add($"tempdir_zipstaging={Uri.EscapeDataString(tempDirZipStaging)}");
            if (tempDirZipStagingCreateIfNotExist.HasValue)
                queryParams.Add($"tempdir_zipstaging_createIfNotExist={tempDirZipStagingCreateIfNotExist.Value}");
            if (useStaging.HasValue)
                queryParams.Add($"usestaging={useStaging.Value}");
            if (!string.IsNullOrWhiteSpace(archiver))
                queryParams.Add($"archiver={Uri.EscapeDataString(archiver)}");
            if (!string.IsNullOrWhiteSpace(compressionLevel))
                queryParams.Add($"compressionlevel={Uri.EscapeDataString(compressionLevel)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;

            return await _httpClient.PutAsync(
                $"{_baseUrl}/api/ZippingWorker_Serviceconfiguration{queryString}",
                null,
                cancellationToken);
        }

        /// <summary>
        /// Updates the service configuration and returns the response content as a string
        /// </summary>
        /// <param name="servicePort">Service listening port</param>
        /// <param name="sevenZipExePath">Path to 7z.exe</param>
        /// <param name="tempDirSymlink">Temporary directory for symlinks</param>
        /// <param name="tempDirSymlinkCreateIfNotExist">Create symlink temp directory if it doesn't exist</param>
        /// <param name="tempDirZipStaging">Staging directory for zip creation</param>
        /// <param name="tempDirZipStagingCreateIfNotExist">Create zip staging directory if it doesn't exist</param>
        /// <param name="useStaging">Use staging directory for zip creation</param>
        /// <param name="archiver">Archiver type: sevenzip or dotnetzip</param>
        /// <param name="compressionLevel">Compression level: nocompression, fastest, fast, normal, maximum, ultra</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response content</returns>
        public async Task<string> UpdateConfigurationStringAsync(
            int? servicePort = null,
            string? sevenZipExePath = null,
            string? tempDirSymlink = null,
            bool? tempDirSymlinkCreateIfNotExist = null,
            string? tempDirZipStaging = null,
            bool? tempDirZipStagingCreateIfNotExist = null,
            bool? useStaging = null,
            string? archiver = null,
            string? compressionLevel = null,
            CancellationToken cancellationToken = default)
        {
            var response = await UpdateConfigurationAsync(
                servicePort,
                sevenZipExePath,
                tempDirSymlink,
                tempDirSymlinkCreateIfNotExist,
                tempDirZipStaging,
                tempDirZipStagingCreateIfNotExist,
                useStaging,
                archiver,
                compressionLevel,
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        /// <summary>
        /// Serializes ZipInfoType to XML byte array
        /// </summary>
        private static byte[] SerializeToXmlBytes(ZipInfoType zipInfo, string zippingServiceIP)
        {
            // Resolve environment variables before serialization
            zipInfo.PrepareForSerialization(zippingServiceIP);

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
        public static string SerializeToXmlString(ZipInfoType zipInfo, string zippingServiceIP)
        {
            // Resolve environment variables before serialization
            zipInfo.PrepareForSerialization(zippingServiceIP);

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
