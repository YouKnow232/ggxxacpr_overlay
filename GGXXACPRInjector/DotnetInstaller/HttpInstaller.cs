using System.Diagnostics;
using System.Security.Cryptography;

namespace GGXXACPRInjector.DotnetInstaller
{
    /// <summary>
    /// Downloads a specific runtime installer from Microsoft. Prefer ScriptInstaller, as this class does not download the latest available installer.
    /// </summary>
    internal class HttpInstaller(string url) : IInstaller
    {
        
        private const string WINDOWS_DOWNLOAD_URL   = @"http://aka.ms/dotnet/9.0/dotnet-runtime-win-x86.exe";
        private const string WINDOWS_INSTALLER_NAME = "dotnet-runtime-win-x86.exe";

        private readonly string downloadUrl = url;

        public HttpInstaller() : this(WINDOWS_DOWNLOAD_URL) { }

        public bool DownloadInstaller()
        {
            Console.WriteLine("Downloading ...");
            using var client = new HttpClient();
            byte[] data = client.GetByteArrayAsync(downloadUrl).Result;
            if (data is null || data.Length == 0) return false;
            Console.WriteLine("Download complete");

            File.WriteAllBytes(WINDOWS_INSTALLER_NAME, data);

            return true;
        }

        public bool Install()
        {
            if (!DownloadInstaller()) return false;

            Console.WriteLine("Running installer ...");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = WINDOWS_INSTALLER_NAME,
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

        public string GetDescription()
        {
            return downloadUrl;
        }
    }
}
