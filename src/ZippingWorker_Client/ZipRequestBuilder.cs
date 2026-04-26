namespace ZippingWorker_Client
{
    using Model;

    /// <summary>
    /// Fluent builder for creating ZipInfoType objects
    /// </summary>
    public class ZipRequestBuilder
    {
        private readonly ZipInfoType _zipInfo;
        private readonly List<DriveLetterType> _driveLetters;
        private readonly List<FileInfoType> _files;
        private bool _autoDetectDrives;

        public ZipRequestBuilder()
        {
            _zipInfo = new ZipInfoType();
            _driveLetters = new List<DriveLetterType>();
            _files = new List<FileInfoType>();
            _autoDetectDrives = true; // Auto-detect drives by default
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
            _zipInfo.zipfilelocation = location;
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
        /// Enables automatic drive letter detection from the system environment.
        /// When enabled, all logical drives will be automatically mapped to themselves.
        /// This is the default behavior.
        /// </summary>
        public ZipRequestBuilder WithAutoDriveDetection(bool autoDetect = true)
        {
            _autoDetectDrives = autoDetect;
            return this;
        }

        /// <summary>
        /// Adds a drive letter mapping. 
        /// Note: Manually adding drive letters will disable auto-detection.
        /// </summary>
        public ZipRequestBuilder AddDriveLetter(string driveLetter, string drivePath)
        {
            // Manual drive letter addition disables auto-detection
            _autoDetectDrives = false;

            _driveLetters.Add(new DriveLetterType
            {
                driveLetter = driveLetter,
                drivePath = drivePath
            });
            return this;
        }

        /// <summary>
        /// Automatically detects and adds all logical drives from the system.
        /// Each drive is mapped to itself (e.g., "C:" -> "C:\")
        /// </summary>
        private void AutoDetectDrives()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType != DriveType.Network)
                .ToList();

            foreach (var drive in drives)
            {
                var driveLetter = drive.Name.TrimEnd('\\');
                _driveLetters.Add(new DriveLetterType
                {
                    driveLetter = driveLetter,
                    drivePath = drive.RootDirectory.FullName
                });
            }
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
            if (_autoDetectDrives && _driveLetters.Count == 0)
            {
                AutoDetectDrives();
            }

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
