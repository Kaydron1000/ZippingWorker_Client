using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ZippingWorker_Client.Model;

namespace ZippingWorker_Client
{
    public static class Extensions
    {
        /// <summary>
        /// Retrieves a list of local and network drive UNC paths for the current machine.
        /// This overload accepts an <see cref="IPAddress"/> object for the zipping service address.
        /// </summary>
        /// <param name="zippingServiceAddress">
        /// The IP address of the zipping service. Used to determine the local IP address for constructing UNC paths.
        /// If null, the local IP address will be determined automatically. This address helps identify which
        /// network interface should be used when multiple network adapters are present.
        /// </param>
        /// <returns>
        /// A list of <see cref="DriveLetterType"/> objects containing the drive letter and corresponding UNC path
        /// for each ready drive on the system. Returns an empty list if no drives are found.
        /// </returns>
        /// <remarks>
        /// This is a convenience overload that converts the <see cref="IPAddress"/> to a string representation
        /// before delegating to the primary implementation. See <see cref="GetLocalDriveUNCPaths(string)"/> for
        /// detailed behavior documentation.
        /// </remarks>
        public static List<DriveLetterType> GetLocalDriveUNCPaths(this IPAddress zippingServiceAddress)
        {
            return GetLocalDriveUNCPaths(zippingServiceAddress?.ToString());
        }

        /// <summary>
        /// Retrieves a list of local and network drive UNC paths for the current machine.
        /// For local drives, constructs administrative share paths (e.g., \\192.168.1.10\C$).
        /// For network drives, resolves to their actual UNC paths.
        /// </summary>
        /// <param name="zippingServiceIpAddress">
        /// The IP address of the zipping service. Used to determine the local IP address for constructing UNC paths.
        /// If null, the local IP address will be determined automatically.
        /// </param>
        /// <returns>
        /// A list of <see cref="DriveLetterType"/> objects containing the drive letter and corresponding UNC path
        /// for each ready drive on the system. Returns an empty list if no drives are found.
        /// </returns>
        /// <remarks>
        /// This method processes all ready drives on the system:
        /// <list type="bullet">
        /// <item><description>Local drives (Fixed, Removable, etc.) are converted to administrative share format (\\IP\DriveLetter$)</description></item>
        /// <item><description>Network drives retain their original UNC paths or are resolved from mapped drives</description></item>
        /// <item><description>Only drives in a ready state are included in the results</description></item>
        /// </list>
        /// </remarks>
        public static List<DriveLetterType> GetLocalDriveUNCPaths(this string? zippingServiceIpAddress)
        {
            List<DriveLetterType> driveLetters = new List<DriveLetterType>();
            var localIpAddress = GetLocalIpAddress(zippingServiceIpAddress);
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .ToList();

            foreach (var drive in drives)
            {
                var driveLetter = drive.Name.TrimEnd('\\');
                string drivePath;

                if (drive.DriveType == DriveType.Network)
                {
                    // For network drives, get the UNC path and resolve to IP if possible
                    drivePath = ResolveNetworkDriveToUncPath(drive);
                }
                else
                {
                    // For local drives, create UNC admin share path
                    var driveLetterOnly = driveLetter.TrimEnd(':');
                    drivePath = $@"\\{localIpAddress}\{driveLetterOnly}$";
                }

                driveLetters.Add(new DriveLetterType
                {
                    driveletter = driveLetter,
                    drivepath = drivePath
                });
            }
            return driveLetters;
        }

        /// <summary>
        /// Gets the local IP address to use for UNC paths
        /// </summary>
        private static string GetLocalIpAddress(string zippingServiceIpAddress)
        {
            // If a zipping service IP is provided, find the client's IP on the same subnet
            if (!string.IsNullOrWhiteSpace(zippingServiceIpAddress))
            {
                var serviceIp = ResolveServiceIpAddress(zippingServiceIpAddress);
                if (serviceIp != null)
                {
                    // If the service is on localhost, return localhost
                    if (IPAddress.IsLoopback(serviceIp))
                    {
                        return "127.0.0.1";
                    }

                    var clientIp = FindClientIpOnSameSubnet(serviceIp);
                    if (clientIp != null)
                        return clientIp;
                }
            }

            // Fallback: Send loopback IPv4 address

            return "127.0.0.1";
        }

        /// <summary>
        /// Resolves a service address (IP, hostname, or URL) to an IPAddress
        /// </summary>
        private static IPAddress? ResolveServiceIpAddress(string serviceAddress)
        {
            try
            {
                // Extract hostname from URL if necessary
                var hostname = serviceAddress;
                if (Uri.TryCreate(serviceAddress, UriKind.Absolute, out var uri))
                {
                    hostname = uri.Host;
                }

                // Try to parse as IP address first
                if (IPAddress.TryParse(hostname, out var ipAddress))
                {
                    return ipAddress;
                }

                // If it's not an IP, try resolving as hostname (e.g., "localhost")
                var hostEntry = Dns.GetHostEntry(hostname);
                return hostEntry.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the client's IP address on the same subnet as the zipping service
        /// </summary>
        private static string? FindClientIpOnSameSubnet(IPAddress serviceIp)
        {
            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var ipProperties = networkInterface.GetIPProperties();
                    foreach (var unicastAddress in ipProperties.UnicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        if (IPAddress.IsLoopback(unicastAddress.Address))
                            continue;

                        // Check if this client IP is on the same subnet as the service IP
                        if (IsInSameSubnet(unicastAddress.Address, serviceIp, unicastAddress.IPv4Mask))
                        {
                            return unicastAddress.Address.ToString();
                        }
                    }
                }
            }
            catch
            {
                // Fall through to return null
            }

            return null;
        }

        /// <summary>
        /// Checks if two IP addresses are in the same subnet
        /// </summary>
        private static bool IsInSameSubnet(IPAddress clientIp, IPAddress serviceIp, IPAddress subnetMask)
        {
            var clientBytes = clientIp.GetAddressBytes();
            var serviceBytes = serviceIp.GetAddressBytes();
            var maskBytes = subnetMask.GetAddressBytes();

            if (clientBytes.Length != serviceBytes.Length || clientBytes.Length != maskBytes.Length)
                return false;

            for (int i = 0; i < clientBytes.Length; i++)
            {
                if ((clientBytes[i] & maskBytes[i]) != (serviceBytes[i] & maskBytes[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves a network drive to its UNC path with IP address
        /// </summary>
        private static string ResolveNetworkDriveToUncPath(DriveInfo drive)
        {
            try
            {
                // Get the UNC path for the network drive
                var driveLetter = drive.Name.TrimEnd('\\');
                var uncPath = GetUncPath(driveLetter);

                if (!string.IsNullOrEmpty(uncPath) && uncPath.StartsWith(@"\\"))
                {
                    // Extract the server name from UNC path (e.g., \\server\share -> server)
                    var parts = uncPath.Substring(2).Split(new[] { '\\' }, 2);
                    if (parts.Length >= 1)
                    {
                        var serverName = parts[0];
                        var sharePath = parts.Length > 1 ? parts[1] : "";

                        // Try to resolve server name to IP address
                        var serverIp = ResolveHostToIpAddress(serverName);
                        if (serverIp != null)
                        {
                            return $@"\\{serverIp}\{sharePath}";
                        }

                        // If resolution fails, return original UNC path
                        return uncPath;
                    }
                }

                return drive.RootDirectory.FullName;
            }
            catch
            {
                return drive.RootDirectory.FullName;
            }
        }

        /// <summary>
        /// Resolves a hostname to an IP address
        /// </summary>
        private static IPAddress? ResolveHostToIpAddress(string hostOrIp)
        {
            try
            {
                // Try to parse as IP address first
                if (IPAddress.TryParse(hostOrIp, out var ipAddress))
                {
                    return ipAddress;
                }

                // Extract hostname from URL if necessary
                var hostname = hostOrIp;
                if (Uri.TryCreate(hostOrIp, UriKind.Absolute, out var uri))
                {
                    hostname = uri.Host;
                }

                // Resolve hostname to IP
                var hostEntry = Dns.GetHostEntry(hostname);
                return hostEntry.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the UNC path for a mapped network drive using WNetGetConnection
        /// </summary>
        private static string? GetUncPath(string driveLetter)
        {
            try
            {
                const int bufferSize = 512;
                var buffer = new System.Text.StringBuilder(bufferSize);
                var size = buffer.Capacity;

                var result = WNetGetConnection(driveLetter.TrimEnd('\\'), buffer, ref size);
                if (result == 0)
                {
                    return buffer.ToString();
                }
            }
            catch
            {
                // Fall through to return null
            }

            return null;
        }

        /// <summary>
        /// Windows API function that retrieves the name of the network resource associated with a local device.
        /// This is a P/Invoke declaration for the WNetGetConnection function from mpr.dll.
        /// </summary>
        /// <param name="localName">
        /// The name of the local device (e.g., "C:" or "Z:") for which to retrieve the associated network resource name.
        /// </param>
        /// <param name="remoteName">
        /// A <see cref="StringBuilder"/> that receives the UNC path of the network resource. 
        /// The buffer must be large enough to hold the path string.
        /// </param>
        /// <param name="length">
        /// On input, specifies the size of the buffer in characters. 
        /// On output, receives the required buffer size if the buffer is too small.
        /// </param>
        /// <returns>
        /// Returns 0 (NO_ERROR) if the function succeeds. 
        /// Returns an error code if the function fails (e.g., ERROR_MORE_DATA if buffer is too small, 
        /// ERROR_NOT_CONNECTED if the device is not a network resource).
        /// </returns>
        /// <remarks>
        /// This Windows API function is used to resolve mapped network drives to their UNC paths.
        /// For more information, see the Microsoft documentation for WNetGetConnection.
        /// </remarks>
        [System.Runtime.InteropServices.DllImport("mpr.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int WNetGetConnection(
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string localName,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] System.Text.StringBuilder remoteName,
            ref int length);

    }
}
