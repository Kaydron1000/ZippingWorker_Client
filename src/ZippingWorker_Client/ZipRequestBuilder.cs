namespace ZippingWorker_Client
{
    using Model;
    using System.Net;
    using System.Net.Sockets;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Fluent builder for creating ZipInfoType objects
    /// </summary>
    public class ZipRequestBuilder
    {
        private readonly ZipInfoType _zipInfo;
        private readonly List<FileInfoType> _files;
        private readonly List<DriveLetterType> _driveLetters;
        private string? _zippingServiceIpAddress;

        public ZipRequestBuilder()
        {
            _zipInfo = new ZipInfoType();
            _files = new List<FileInfoType>();
            _driveLetters = new List<DriveLetterType>();
        }

        /// <summary>
        /// Sets the service address
        /// </summary>
        public ZipRequestBuilder WithServiceAddress(string serviceAddress)
        {
            _zippingServiceIpAddress = serviceAddress;
            return this;
        }

        /// <summary>
        /// Sets the output zip file name
        /// </summary>
        public ZipRequestBuilder WithZipFileName(string fileName)
        {
            _zipInfo.zipfilename = fileName;
            return this;
        }

        /// <summary>
        /// Sets the output zip file location (optional)
        /// </summary>
        public ZipRequestBuilder WithZipFileLocation(string location)
        {
            _zipInfo.zipfiledirectory = location;
            return this;
        }

        /// <summary>
        /// Sets the compression level
        /// </summary>
        public ZipRequestBuilder WithCompressionLevel(CompressionLevelEnumType level)
        {
            _zipInfo.zipcompressionlevel = level;
            return this;
        }

        /// <summary>
        /// Sets whether to validate the zipping process
        /// </summary>
        public ZipRequestBuilder WithValidation(bool validate = true)
        {
            _zipInfo.validatezipping = validate;
            return this;
        }

        /// <summary>
        /// Sets whether to delete input files after zipping
        /// </summary>
        public ZipRequestBuilder DeleteInputFiles(bool delete = true)
        {
            _zipInfo.deleteinputfiles = delete;
            return this;
        }

        /// <summary>
        /// Automatically detects and adds all logical drives from the system.
        /// Local drives are mapped to UNC admin shares (e.g., "C:" -> "\\{ipaddress}\c$")
        /// Network drives are mapped to their UNC paths with IP addresses
        /// </summary>
        private void AutoDetectDrives()
        {
            _driveLetters.AddRange(_zippingServiceIpAddress.GetLocalDriveUNCPaths());
        }

        /// <summary>
        /// Adds a file to be included in the zip
        /// </summary>
        /// <param name="fileLocation">Source file location</param>
        /// <param name="internalZipLocation">Path within the zip archive</param>
        /// <param name="fileHash">Optional file hash for verification</param>
        public ZipRequestBuilder AddFile(string fileLocation, string internalZipLocation, string? fileHash = null)
        {
            _files.Add(new FileInfoType
            {
                filelocation = fileLocation,
                internalziplocation = internalZipLocation,
                filehash = fileHash ?? string.Empty
            });
            return this;
        }

        /// <summary>
        /// Adds multiple files to be included in the zip
        /// </summary>
        public ZipRequestBuilder AddFiles(IEnumerable<(string fileLocation, string internalZipLocation, string? fileHash)> files)
        {
            foreach (var (fileLocation, internalZipLocation, fileHash) in files)
            {
                AddFile(fileLocation, internalZipLocation, fileHash);
            }
            return this;
        }

        /// <summary>
        /// Builds and returns the ZipInfoType object
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if required fields are missing</exception>
        public ZipInfoType Build()
        {
            if (string.IsNullOrWhiteSpace(_zipInfo.zipfilename))
                throw new InvalidOperationException("Zip file name is required");

            // Auto-detect drives if enabled and no manual drives were added
            AutoDetectDrives();

            if (_driveLetters.Count == 0)
                throw new InvalidOperationException("At least one drive letter mapping is required. Enable auto-detection or add drives manually.");

            if (_files.Count == 0)
                throw new InvalidOperationException("At least one file is required");

            _zipInfo.driveletters = _driveLetters.ToArray();
            _zipInfo.zipfiles = _files.ToArray();

            return _zipInfo;
        }

        /// <summary>
        /// Creates a new builder instance
        /// </summary>
        public static ZipRequestBuilder Create() => new ZipRequestBuilder();
    }
}
