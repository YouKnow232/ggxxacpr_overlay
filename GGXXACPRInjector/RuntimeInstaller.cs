using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace GGXXACPRInjector
{
    internal static class RuntimeInstaller
    {
        private const string downloadUrl = @"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/9.0.4/";
        private const string installerName = "windowsdesktop-runtime-9.0.4-win-x86.exe";
        private const string checkSum = "214a98c6d468566cb0d84898e7129897890384b1f3a49f1c59187f3711cea6340df588850d65c1b0c239fd0151806cfa7bc056551da05d9a0d94130b2e4fba7d";

        internal static bool HandleRuntimeChecks()
        {
            if (CheckRuntimeInstallation()) return true;

            if (PromptUserAgreement("This mod requires .NET runtime v9.0.4, but no installation was found.\n" +
                                    "Would you like to download it now?\n\n" +
                                    $"The runtime will be downloaded from:\n{downloadUrl+installerName}"))
            {
                return DownloadDotnetRuntime();
            }
            else
            {
                return false;
            }
        }

        private static bool CheckRuntimeInstallation()
        {
            const string keyPath = @"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App";
            const string versionPrefix = "9.0.4";
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);

            if (key == null) return false;

            return key.GetValueNames().Any(name => name.StartsWith(versionPrefix));
        }

        private static bool PromptUserAgreement(string prompt)
        {
            Console.WriteLine($"{prompt}\n");
            string response;
            do
            {
                Console.Write("[Y/N]: ");
                response = Console.ReadLine()?.Trim() ?? "";
            }
            while (!response.Equals("y", StringComparison.OrdinalIgnoreCase) &&
                   !response.Equals("n", StringComparison.OrdinalIgnoreCase));

            return response.Equals("y", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DownloadDotnetRuntime()
        {
            Console.WriteLine("Downloading ...");
            using var client = new HttpClient();
            byte[] data = client.GetByteArrayAsync(downloadUrl + installerName).Result; ;
            Console.WriteLine("Download complete");

            byte[] hash = SHA512.HashData(data);
            if (!Convert.ToHexString(hash).Equals(checkSum, StringComparison.OrdinalIgnoreCase)) throw new InvalidHashException("Downloaded runtime exe did not match checksum");
            Console.WriteLine("File hash verified.");

            File.WriteAllBytes(installerName, data);

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
