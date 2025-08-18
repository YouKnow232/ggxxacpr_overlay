using System.Diagnostics;
using System.Security.Cryptography;

namespace GGXXACPRInjector.DotnetInstaller
{
    internal enum OS
    {
        DEFAULT,
        WINDOWS,
        IOS,
        LINUX,
    }

    internal class HttpInstaller(string url, string installerFileName, string checkSum) : IInstaller
    {
        private const string WINDOWS_DOWNLOAD_URL   = @"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/9.0.8/";
        private const string WINDOWS_INSTALLER_NAME = "windowsdesktop-runtime-9.0.8-win-x86.exe";
        private const string WINDOWS_CHECK_SUM      = "8a9c1b6de95330dc339b3211c52bb8f75ec73aef6e0361d58090faf2c66e4e831e7de53bce95346091a43a4102e4b480ade7de3625240e7797901d3e3b8ad5cb";
        private const string IOS_DOWNLOAD_URL       = "https://builds.dotnet.microsoft.com/dotnet/Runtime/9.0.8/";
        private const string IOS_INSTALLER_NAME     = "dotnet-runtime-9.0.8-osx-x64.pkg";
        private const string IOS_CHECK_SUM          = "b6067faf028de11f35c99e4119251da19725e07bb961dceff61618b25d192aed152372a866cffbb7ccf76a5071584c5f6ab0d7774f854a95ba302837721a6840";

        private readonly string downloadUrl = url;
        private readonly string installerName = installerFileName;
        private readonly string checkSum = checkSum;

        public HttpInstaller(OS operatingSystem)
            : this(WINDOWS_DOWNLOAD_URL, WINDOWS_INSTALLER_NAME, WINDOWS_CHECK_SUM)
        {
            switch (operatingSystem)
            {
                case OS.WINDOWS:
                    downloadUrl   = WINDOWS_DOWNLOAD_URL;
                    installerName = WINDOWS_INSTALLER_NAME;
                    checkSum      = WINDOWS_CHECK_SUM;
                    break;
                case OS.IOS:
                    downloadUrl   = IOS_DOWNLOAD_URL;
                    installerName = IOS_INSTALLER_NAME;
                    checkSum      = IOS_CHECK_SUM;
                    break;
            }
        }

        public bool DownloadInstaller()
        {
            using var client = new HttpClient();
            byte[] data = client.GetByteArrayAsync(downloadUrl + installerName).Result;
            if (data is null || data.Length == 0) return false;
            Console.WriteLine("Download complete");

            byte[] hash = SHA512.HashData(data);
            if (!Convert.ToHexString(hash).Equals(checkSum, StringComparison.OrdinalIgnoreCase))
                throw new InvalidHashException("Downloaded runtime exe did not match checksum");
            Console.WriteLine("File hash verified.");

            File.WriteAllBytes(installerName, data);

            return true;
        }

        public bool Install()
        {
            if (!DownloadInstaller()) return false;

            Console.WriteLine("Running installer ...");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = installerName,
                Arguments = "/quiet /norestart",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null) return false;

            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                Console.WriteLine("Installation complete.");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
