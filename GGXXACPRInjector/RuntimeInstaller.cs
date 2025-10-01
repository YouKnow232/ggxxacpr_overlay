using System.Diagnostics;
using System.Text.RegularExpressions;
using GGXXACPRInjector.DotnetInstaller;

namespace GGXXACPRInjector
{
    internal static partial class RuntimeInstaller
    {
        private const string DOTNET_CLI = "dotnet.exe";
        private const string DOTNET_LIST_RUNTIMES = "--list-runtimes --arch x86";

        /// <summary>
        /// A bug in versions 9.0.4 - 9.0.6 severly impacts stability in this overlay.
        /// </summary>
        [GeneratedRegex(@"Microsoft\.NETCore\.App 9\.(?!0\.[456]\s)\d+.\d+")]
        private static partial Regex AcceptableDotnet9VersionsRegex { get; }

        /// <summary>
        /// Returns true if a valid runtime is installed else runs an installation
        /// script and returns whether the installation was successful.
        /// </summary>
        internal static bool HandleRuntimeChecks()
        {
            if (CheckRuntimeInstallation()) return true;

            IInstaller installer = ChooseInstaller();

            if (PromptUserAgreement(
                "This overlay requires a .NET 9 x86 Runtime, but no suitable installation was found.\n" +
                $"Would you like to download it now? (via \"{installer.GetDescription()}\")"))
            {
                return installer.Install();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determins the installation method to use. Currently only returns an IInstaller
        /// that downloads and executes the offical runtime-win-x86 installer, but I'm leaving
        /// this method here for when I look into linux/iOS compatability.
        /// </summary>
        private static IInstaller ChooseInstaller()
        {
            if (OperatingSystem.IsWindows())
            {
                return new HttpInstaller();
            }
            else
            {
                throw new InvalidOperationException(
                    "This overlay requires a Microsoft .NET 9.0 Runtime Windows x86 installation, but none was found. " +
                    "Install it in the same environment that +R is running in then run this injector in that same environment. " +
                    "The injector will look for the installation via the dotnet CLI, so make sure that's also accessible.");
            }
        }

        /// <summary>
        /// Checks for a valid .NET installation by attempting to run the dotnet CLI
        /// </summary>
        private static bool CheckRuntimeInstallation()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string path = Path.Combine(programFiles, "dotnet", DOTNET_CLI);

            if (!File.Exists(path)) return false;

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = DOTNET_LIST_RUNTIMES,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });

            if (process is null) return false;
            process.WaitForExit();  // use a timeout?
            if (process.ExitCode != 0) return false;

            string output = process.StandardOutput.ReadToEnd();
            return AcceptableDotnet9VersionsRegex.IsMatch(output);
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
    }
}
