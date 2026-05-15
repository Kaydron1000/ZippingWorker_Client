using ZippingWorker_Client;

namespace ZippingWorker_Client.Examples
{
    using Model;

    /// <summary>
    /// Example usage of the ZippingWorkerService C# client
    /// </summary>
    public class ClientExamples
    {
        /// <summary>
        /// Basic synchronous example with auto-detected drives
        /// </summary>
        public static async Task BasicUsageExample()
        {
            Console.WriteLine("=== Basic Usage Example (Auto-Detected Drives) ===");

            using var client = new ZippingServiceClient("http://localhost:5000");

            // Drive letters are now automatically detected from the system
            var zipRequest = ZipRequestBuilder.Create()
                .WithZipFileName("example-archive.zip")
                .WithZipFileLocation(@"C:\temp\output")
                .WithCompressionLevel(CompressionLevelEnumType.ultra)
                .WithValidation(ValidateEnumType.extract)
                // No need to manually add drive letters - they're auto-detected!
                .AddFile(@"C:\source\file1.txt", "documents/file1.txt")
                .AddFile(@"C:\source\file2.pdf", "documents/file2.pdf")
                .Build();

            var response = await client.SubmitZipRequestAsync(zipRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Success: {result}");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Example with manual drive letter mapping (overrides auto-detection)
        /// </summary>
        public static async Task ManualDriveMappingExample()
        {
            Console.WriteLine("\n=== Manual Drive Mapping Example ===");

            using var client = new ZippingServiceClient("http://localhost:5000");

            // Manually adding drive letters disables auto-detection
            var zipRequest = ZipRequestBuilder.Create()
                .WithZipFileName("custom-mapping-archive.zip")
                .WithZipFileLocation(@"C:\temp\output")
                .AddFile(@"C:\source\file1.txt", "documents/file1.txt")
                .Build();

            var response = await client.SubmitZipRequestAsync(zipRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Success: {result}");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Example with multiple files and auto-detected drives
        /// </summary>
        public static async Task MultipleFilesExample()
        {
            Console.WriteLine("\n=== Multiple Files Example (Auto-Detected Drives) ===");

            var filesToZip = new[]
            {
                (@"C:\data\report.docx", "reports/report.docx", (string?)null),
                (@"C:\data\image.png", "images/image.png", (string?)null),
                (@"C:\data\video.mp4", "media/video.mp4", (string?)null)
            };

            // No need to specify drive letters - they're automatically detected
            var zipRequest = ZipRequestBuilder.Create()
                .WithZipFileName("complete-backup.zip")
                .WithZipFileLocation(@"C:\backups")
                .WithCompressionLevel(CompressionLevelEnumType.maximum)
                .WithValidation(ValidateEnumType.extract)
                .DeleteInputFiles(DeleteEnumType.none)
                .AddFiles(filesToZip)
                .Build();

            // Show the generated XML
            var xml = ZippingServiceClient.SerializeToXmlString(zipRequest, "http://localhost:5000");
            Console.WriteLine("Generated XML:");
            Console.WriteLine(xml);
        }

        /// <summary>
        /// Example without using builder pattern
        /// </summary>
        public static void ManualConstructionExample()
        {
            Console.WriteLine("\n=== Manual Construction Example ===");

            var zipInfo = new ZipInfoType
            {
                zipfilename = "manual-archive.zip",
                zipfiledirectory = @"C:\output",
                zipcompressionlevel = CompressionLevelEnumType.ultra,
                validatezipping = ValidateEnumType.extract,
                deleteinputfiles = DeleteEnumType.none,
                zipfiles = new[]
                {
                    new FileInfoType 
                    { 
                        filelocation = @"C:\file1.txt", 
                        internalziplocation = "docs/file1.txt",
                        filehash = ""
                    },
                    new FileInfoType 
                    { 
                        filelocation = @"D:\file2.txt", 
                        internalziplocation = "docs/file2.txt",
                        filehash = ""
                    }
                }
            };

            var xml = ZippingServiceClient.SerializeToXmlString(zipInfo, "http://localhost:5000");
            Console.WriteLine("Generated XML:");
            Console.WriteLine(xml);

            // Deserialize back
            var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
            var deserialized = ZippingServiceClient.DeserializeFromXmlBytes(bytes);
            Console.WriteLine($"\nDeserialized zip filename: {deserialized.zipfilename}");
            Console.WriteLine($"Number of files: {deserialized.zipfiles.Length}");
        }

        /// <summary>
        /// Example using custom HttpClient for advanced scenarios
        /// </summary>
        public static async Task CustomHttpClientExample()
        {
            Console.WriteLine("\n=== Custom HttpClient Example ===");

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            using var client = new ZippingServiceClient("http://localhost:5000", httpClient);

            var zipRequest = ZipRequestBuilder.Create()
                .WithZipFileName("large-archive.zip")
                .WithZipFileLocation(@"C:\output")
                .AddFile(@"C:\large-file.iso", "large-file.iso")
                .Build();

            try
            {
                var result = await client.SubmitZipRequestStringAsync(zipRequest);
                Console.WriteLine($"Success: {result}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Main entry point for examples
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("ZippingWorkerService Client Examples\n");

            // Run examples that don't require actual API
            MultipleFilesExample().Wait();
            ManualConstructionExample();

            // Uncomment these to test actual API calls
            // await BasicUsageExample();
            // await CustomHttpClientExample();
        }
    }
}
