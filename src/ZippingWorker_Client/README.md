# ZippingWorker_Client

C# client library for interacting with the ZippingWorkerService API.

## Installation

Add a reference to this project in your application:

```bash
dotnet add reference path/to/ZippingWorker_Client.csproj
```

Or add it to your solution:

```bash
dotnet sln add ZippingWorker_Client/ZippingWorker_Client.csproj
```

## Features

- ✅ **Automatic Drive Detection** - Automatically detects and maps all logical drives from the system
- ✅ **Fluent Builder API** - Easy-to-use builder pattern for creating requests
- ✅ **Type-Safe** - Strongly-typed classes generated from XSD schema
- ✅ **Async Support** - Full async/await support
- ✅ **Manual Override** - Option to manually specify drive mappings when needed

## Usage

### Basic Example (Auto-Detected Drives)

By default, the builder automatically detects all logical drives from your system. You don't need to manually specify drive letters!

```csharp
using ZippingWorker_Client;

// Create a client instance
using var client = new ZippingServiceClient("http://localhost:5000");

// Build a zip request - drives are automatically detected!
var zipRequest = ZipRequestBuilder.Create()
    .WithZipFileName("my-archive.zip")
    .WithZipFileLocation(@"C:\output")
    .WithCompressionLevel(CompressionLevelEnumType.ultra)
    .WithValidation(true)
    // No need to call AddDriveLetter - drives are auto-detected!
    .AddFile(@"C:\source\file1.txt", "documents/file1.txt")
    .AddFile(@"C:\source\file2.pdf", "documents/file2.pdf")
    .Build();

// Submit the request
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
```

### Manual Drive Mapping (Override Auto-Detection)

If you need custom drive mappings, you can manually add them. This will automatically disable auto-detection:

```csharp
var zipRequest = ZipRequestBuilder.Create()
    .WithZipFileName("custom-archive.zip")
    .AddDriveLetter("C:", @"E:\CustomPath")  // Manual mapping disables auto-detection
    .AddDriveLetter("D:", @"F:\AnotherPath")
    .AddFile(@"C:\file.txt", "file.txt")
    .Build();
```

### Disable Auto-Detection

If you want to disable auto-detection without adding manual mappings:

```csharp
var zipRequest = ZipRequestBuilder.Create()
    .WithZipFileName("archive.zip")
    .WithAutoDriveDetection(false)  // Explicitly disable
    .AddDriveLetter("C:", @"C:\")   // Must add manually now
    .AddFile(@"C:\file.txt", "file.txt")
    .Build();
```

### Advanced Example with Multiple Files

```csharp
using ZippingWorker_Client;

using var client = new ZippingServiceClient("http://localhost:5000");

var filesToZip = new[]
{
    (@"C:\data\report.docx", "reports/report.docx", (string?)null),
    (@"C:\data\image.png", "images/image.png", (string?)null),
    (@"C:\data\video.mp4", "media/video.mp4", (string?)null)
};

// Drives are automatically detected - no manual mapping needed!
var zipRequest = ZipRequestBuilder.Create()
    .WithZipFileName("complete-backup.zip")
    .WithZipFileLocation(@"C:\backups")
    .WithCompressionLevel(CompressionLevelEnumType.maximum)
    .WithValidation(true)
    .DeleteInputFiles(false)
    .AddFiles(filesToZip)
    .Build();

try
{
    var result = await client.SubmitZipRequestStringAsync(zipRequest);
    Console.WriteLine($"Zip request submitted successfully: {result}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Request failed: {ex.Message}");
}
```

### Manual Object Creation

If you prefer not to use the builder pattern:

```csharp
var zipInfo = new ZipInfoType
{
    zipfilename = "manual-archive.zip",
    zipfilelocation = @"C:\output",
    zipcompressionlevel = CompressionLevelEnumType.ultra,
    validatezipping = true,
    deleteinputfiles = false,
    driveletters = new[]
    {
        new DriveLetterType { driveLetter = "C:", drivePath = @"E:\Data" }
    },
    zipfiles = new[]
    {
        new FileInfoType 
        { 
            filelocation = @"C:\file.txt", 
            internalziplocation = "file.txt",
            filehash = ""
        }
    }
};

using var client = new ZippingServiceClient("http://localhost:5000");
await client.SubmitZipRequestAsync(zipInfo);
```

### Serialization Utilities

The client also provides static methods for XML serialization:

```csharp
// Serialize to XML string
string xml = ZippingServiceClient.SerializeToXmlString(zipInfo);
Console.WriteLine(xml);

// Deserialize from bytes
byte[] xmlBytes = /* ... */;
var deserializedZipInfo = ZippingServiceClient.DeserializeFromXmlBytes(xmlBytes);
```

## API Reference

### ZippingServiceClient

Main client for interacting with the service.

**Constructor:**
- `ZippingServiceClient(string baseUrl, HttpClient? httpClient = null)`

**Methods:**
- `Task<HttpResponseMessage> SubmitZipRequestAsync(ZipInfoType zipInfo, CancellationToken cancellationToken = default)`
- `Task<string> SubmitZipRequestStringAsync(ZipInfoType zipInfo, CancellationToken cancellationToken = default)`

**Static Methods:**
- `string SerializeToXmlString(ZipInfoType zipInfo)`
- `ZipInfoType DeserializeFromXmlBytes(byte[] data)`

### ZipRequestBuilder

Fluent builder for creating `ZipInfoType` objects.

**Methods:**
- `WithZipFileName(string fileName)` - Sets the output zip file name (required)
- `WithZipFileLocation(string location)` - Sets the output directory (optional)
- `WithCompressionLevel(CompressionLevelEnumType level)` - Sets compression level
- `WithValidation(bool validate = true)` - Enables/disables validation
- `DeleteInputFiles(bool delete = true)` - Sets whether to delete source files
- `WithAutoDriveDetection(bool autoDetect = true)` - Enables/disables automatic drive detection (enabled by default)
- `AddDriveLetter(string driveLetter, string drivePath)` - Adds a drive mapping (disables auto-detection)
- `AddFile(string fileLocation, string internalZipLocation, string? fileHash = null)` - Adds a file
- `AddFiles(IEnumerable<(string, string, string?)> files)` - Adds multiple files
- `Build()` - Builds and returns the `ZipInfoType` object

**Auto-Detection Behavior:**
- By default, all logical drives (except network drives) are automatically detected and mapped to themselves
- Calling `AddDriveLetter()` automatically disables auto-detection
- Use `WithAutoDriveDetection(false)` to disable without adding manual mappings

### CompressionLevelEnumType

Enum representing compression levels:
- `nocompression`
- `fastest`
- `fast`
- `normal`
- `maximum`
- `ultra`

## Requirements

- .NET 10.0 or higher
- ZippingWorkerService running and accessible

## License

See the main project license.
