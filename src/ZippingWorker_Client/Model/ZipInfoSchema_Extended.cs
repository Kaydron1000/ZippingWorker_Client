using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace ZippingWorker_Client.Model
{
    public partial class ZipInfoType
    {
        private string? _ResolvedZipFileDirectory;
        [XmlIgnore]
        public string ResolvedZipFileDirectory
        {
            get
            {
                if (_ResolvedZipFileDirectory == null)
                    _ResolvedZipFileDirectory = ResolveZipFileDirectory();
                return _ResolvedZipFileDirectory;
            }
        }
        private string ResolveZipFileDirectory()
        {
            string appdir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string retStrg = Environment.ExpandEnvironmentVariables(this.zipfiledirectory).Replace("%APPDIR%", appdir).Replace("%APPPATH%", appdir);
            return retStrg;

        }

        /// <summary>
        /// Call before serialization to resolve environment variables
        /// </summary>
        public void PrepareForSerialization(string zippingServiceIpAddress)
        {
            zipfiledirectory = ResolvedZipFileDirectory;

            // Also update all file locations
            if (this.zipfiles != null)
            {
                foreach (var file in this.zipfiles)
                {
                    file.PrepareForSerialization();
                }
            }

            // Add drive UNC paths
            if (this.driveletters == null)
                this.driveletters = zippingServiceIpAddress.GetLocalDriveUNCPaths().ToArray();
            else
            {
                List<DriveLetterType> existingDriveLetters = new List<DriveLetterType>(this.driveletters);
                List<DriveLetterType> localDriveUNCPaths = zippingServiceIpAddress.GetLocalDriveUNCPaths();
                foreach (DriveLetterType uncPath in localDriveUNCPaths)
                {
                    var existingDrive = existingDriveLetters.SingleOrDefault(o => o.driveletter == uncPath.driveletter);
                    if (existingDrive == null)
                    {
                        existingDriveLetters.Add(uncPath);
                    }
                    else
                    {
                        if (existingDrive.drivepath != uncPath.drivepath)
                        {
                            // state warning - this means the user provided a drive letter mapping that conflicts with the auto-detected UNC path for that drive.
                            // The auto-detected UNC path will be used, but the user should be made aware of the conflict.
                            existingDrive.drivepath = uncPath.drivepath;
                        }
                    }
                }

                this.driveletters = existingDriveLetters.ToArray();
            }
        }
    }
    public partial class FileInfoType
    {
        private string? _ResolvedFileLocation;
        [XmlIgnore]
        public string ResolvedFileLocation
        {
            get
            {
                if (_ResolvedFileLocation == null)
                    _ResolvedFileLocation = ResolveFileLocation();
                return _ResolvedFileLocation;
            }
        }
        private string ResolveFileLocation()
        {
            string appdir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string retStrg = Environment.ExpandEnvironmentVariables(this.filelocation).Replace("%APPDIR%", appdir).Replace("%APPPATH%", appdir);
            if (System.IO.File.Exists(retStrg))
                return retStrg;
            else
                return "";
        }

        /// <summary>
        /// Call before serialization to resolve environment variables
        /// </summary>
        public void PrepareForSerialization()
        {
            this.filelocation = ResolvedFileLocation;
        }
    }
}
