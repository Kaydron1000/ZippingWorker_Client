using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using ZippingWorker_Client;
using ZippingWorker_Client.Model;

public static class Program
{
    private static string[] _Args;
    public static async Task<int> Main(string[] args)
    {
        try
        {
            _Args = args;

            _Args = new string[] { "--example" };

            if (_Args.Length == 0)
            {
                Console.WriteLine("Usage: ZippingWorker_SampleClient <xml-file-path> [service-url]");
                Console.WriteLine();
                Console.WriteLine("Arguments:");
                Console.WriteLine("  xml-file-path  Path to the XML file containing zip information (required)");
                Console.WriteLine("  service-url    URL of the zipping service (optional, default: http://localhost:5000)");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  ZippingWorker_SampleClient C:\\data\\zipinfo.xml");
                Console.WriteLine("  ZippingWorker_SampleClient C:\\data\\zipinfo.xml http://localhost:8080");
                return 1;
            }
            else if (_Args.Length == 1 && _Args[0] == "--help") 
            {
                Console.WriteLine("Usage: ZippingWorker_SampleClient <xml-file-path> [service-url]");
                Console.WriteLine();
                Console.WriteLine("Arguments:");
                Console.WriteLine("  xml-file-path  Path to the XML file containing zip information (required)");
                Console.WriteLine("  service-url    URL of the zipping service (optional, default: http://localhost:5000)");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  ZippingWorker_SampleClient C:\\data\\zipinfo.xml");
                Console.WriteLine("  ZippingWorker_SampleClient C:\\data\\zipinfo.xml http://localhost:8080");
                return 1;
            }
            else if (_Args.Length == 1 && _Args[0] == "--example")
            {
                RunExamples();
                return 0;
            }
            else
            {
                return RunZip();
            }


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
    private static int RunZip()
    {
        string xmlFilePath = _Args[0];
        string serviceUrl = _Args.Length > 1 ? _Args[1] : "http://localhost:5000";

        if (!File.Exists(xmlFilePath))
        {
            Console.WriteLine($"Error: XML file not found at '{xmlFilePath}'");
            return 1;
        }

        Console.WriteLine($"Loading zip information from: {xmlFilePath}");
        ZipInfoType zipInfo = xmlFilePath.ImportModel();

        Console.WriteLine($"Successfully loaded zip configuration:");
        Console.WriteLine($"  Zip File Name: {zipInfo.zipfilename}");
        Console.WriteLine($"  Zip Directory: {zipInfo.zipfiledirectory}");
        Console.WriteLine($"  Compression Level: {zipInfo.zipcompressionlevel}");
        Console.WriteLine($"  Validate Zipping: {zipInfo.validatezipping}");
        Console.WriteLine($"  Delete Input Files: {zipInfo.deleteinputfiles}");

        if (zipInfo.zipfiles != null)
        {
            Console.WriteLine($"  Files to Zip: {zipInfo.zipfiles.Length}");
        }

        if (zipInfo.driveletters != null)
        {
            Console.WriteLine($"  Drive Mappings: {zipInfo.driveletters.Length}");
        }

        Console.WriteLine();
        Console.WriteLine($"Connecting to service at: {serviceUrl}");

        using (var client = new ZippingServiceClient(serviceUrl))
        {
            Console.WriteLine("Submitting zip request...");
            var response = client.SubmitZipRequestAsync(zipInfo).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine();
                Console.WriteLine("Success!");
                Console.WriteLine($"Response: {result}");
                return 0;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"Error: Request failed with status code {response.StatusCode}");
                var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    Console.WriteLine($"Details: {errorContent}");
                }
                return 1;
            }
        }
    }
    public static void RunExamples()
    {
        //APIExample("http://192.168.1.235:5000");
        _Args = new string[] { "E:\\Code\\CSharp\\ZippingWorker_Client\\src\\ZippingWorker_SampleClient\\Source_ExternalComputer_sample-zipinfo.xml", "http://192.168.1.235:5000" };
        RunZip();
        _Args = new string[] { "E:\\Code\\CSharp\\ZippingWorker_Client\\src\\ZippingWorker_SampleClient\\Source_LocalComputer_sample-zipinfo.xml", "http://192.168.1.235:5000" };
        RunZip();
    }
    public static void APIExample(string serviceUrl = "127.0.0.1")
    {
        ZippingWorker_Client.ZipRequestBuilder builder = new ZippingWorker_Client.ZipRequestBuilder()
                            .WithServiceAddress(serviceUrl)
                            .WithCompressionLevel(CompressionLevelEnumType.ultra)
                            .WithZipFileLocation("FINAL_LOCATION")
                            .WithValidation(true)
                            .WithZipFileName("FINAL_ZIP_NAME")
                            .DeleteInputFiles(true);
        builder.AddFile("FILE_LOCATION", "INTERNAL_ZIP_LOCATION", "OPTIONAL_FILE_HASH");
        builder.AddFile("FILE_LOCATION", "INTERNAL_ZIP_LOCATION", "OPTIONAL_FILE_HASH");
        builder.AddFile("FILE_LOCATION", "INTERNAL_ZIP_LOCATION", "OPTIONAL_FILE_HASH");
        ZipInfoType content = builder.Build();

        using (var client = new ZippingServiceClient(serviceUrl))
        {
            Console.WriteLine("Submitting zip request...");
            client.SubmitZipRequestAsync(content);
        }


    }
}