using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ZippingWorker_Client.Model
{
    public static class ModelExtensions
    {
        public static ZipInfoType ImportModel(this string xmlPath)
        {
            XDocument doc = XDocument.Load(xmlPath);
            return ImportModel(doc.CreateReader());

        }
        public static ZipInfoType ImportModel(this XmlReader xmlReader)
        {
            // Load the embedded XSD schema
            var assembly = Assembly.GetExecutingAssembly();
            // Replace "YourSchemaFileName.xsd" with your actual XSD filename
            string xsdName = "ZipInfoSchema.xsd";
            string[] resourceNames = assembly.GetManifestResourceNames();
            string? resourceName = resourceNames.FirstOrDefault(o => o.Contains(xsdName));
            using (Stream? schemaStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (schemaStream == null)
                {
                    throw new InvalidOperationException($"Embedded resource '{xsdName}' not found.");
                }

                using (XmlReader schemaReader = XmlReader.Create(schemaStream))
                {
                    // Deserialize using XML serializer (safer than BinaryFormatter)
                    ZipInfoType zipInfo = null;

                    //XmlSchemaSet schemaSet = XmlExtensions.LoadSchemaSet("ZippingWorker_Service", "ZipInfoSchema.xsd");
                    var ser = new XmlOnDeserializedSerializer(typeof(ZipInfoType));
                    XmlReader xmlContentNormalized = ser.ValidateAgainstSchemaIgnoreCaseAndRootLoc(xmlReader: xmlReader, 
                                                                                                    xsdReader: schemaReader,
                                                                                                    errorList: out List<ValidationEventArgs> localErrorList, 
                                                                                                    warningList: out List<ValidationEventArgs> localWarningList);
                    zipInfo = (ZipInfoType)ser.Deserialize(xmlContentNormalized);
                    return zipInfo;
                }
            }
        }
    }
}
