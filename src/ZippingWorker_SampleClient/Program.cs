using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using ZippingWorker_Client;
using ZippingWorker_Client.Model;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            //args = new string[] { "E:\\Code\\CSharp\\ZippingWorker_Client\\src\\ZippingWorker_SampleClient\\sample-zipinfo.xml" };       
            if (args.Length == 0)
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

            string xmlFilePath = args[0];
            string serviceUrl = args.Length > 1 ? args[1] : "http://localhost:5000";

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
                var response = await client.SubmitZipRequestAsync(zipInfo);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine();
                    Console.WriteLine("Success!");
                    Console.WriteLine($"Response: {result}");
                    return 0;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine($"Error: Request failed with status code {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorContent))
                    {
                        Console.WriteLine($"Details: {errorContent}");
                    }
                    return 1;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}