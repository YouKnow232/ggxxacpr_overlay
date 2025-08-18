using System.Diagnostics;

namespace GGXXACPRInjector.DotnetInstaller
{
    internal class WingetInstaller : IInstaller
    {
        private const string WINGET_COMMAND = "winget";
        private const string DOTNET_RUNTIME_9_PACKAGE_NAME = "Microsoft .NET Windows Desktop Runtime 9.0";

        public WingetInstaller() { }

        public static bool VerifyWinget()
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = WINGET_COMMAND,
                Arguments = "-v",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null) return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }

        public bool Install()
        {
            Console.WriteLine("Installing via Winget..");

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = WINGET_COMMAND,
                Arguments = "install " + DOTNET_RUNTIME_9_PACKAGE_NAME,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                Console.WriteLine("Error: Winget not found.");
                return false;
            }

            process.WaitForExit();
            bool success = process.ExitCode == 0;

            if (success)
            {
                Console.WriteLine("Installation sucessful");
            }
            else
            {
                Console.WriteLine("Error during installation. {process.ExitCode}");
            }

            return success;
        }
    }
}
