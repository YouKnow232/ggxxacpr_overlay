using System.Text.RegularExpressions;
using GGXXACPRInjector.DotnetInstaller;
using Microsoft.Win32;

namespace GGXXACPRInjector
{
    internal static partial class RuntimeInstaller
    {
        internal static bool HandleRuntimeChecks()
        {
            if (CheckRuntimeInstallation()) return true;

            IInstaller installer = ChooseInstaller();

            if (PromptUserAgreement(
                "This mod requires .NET runtime v9.0.8 or newer, but no installation was found.\n" +
                "Would you like to download it now?\n\n"))
            {
                return installer.Install();
            }
            else
            {
                return false;
            }
        }

        private static IInstaller ChooseInstaller()
        {
            if (OperatingSystem.IsWindows())
            {
                if (WingetInstaller.VerifyWinget())
                {
                    return new WingetInstaller();
                }
                else
                {
                    return new HttpInstaller(OS.WINDOWS);
                }
            }
            else if (OperatingSystem.IsIOS())
            {
                return new HttpInstaller(OS.IOS);
            }
            else if (OperatingSystem.IsLinux())
            {
                throw new InvalidOperationException("Please use your Linux distro's preferred " +
                    ".NET package if you are trying to run +R and this overlay natively.");
            }
            else
            {
                throw new InvalidOperationException("No valid operating system detected. " +
                    "Please install the .NET v9 runtime and try again.");
            }
        }

        private static bool CheckRuntimeInstallation()
        {
            if (OperatingSystem.IsWindows())
            {
                const string keyPath = @"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App";
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);

                if (key == null) return false;

                // check latest version isn't bugged

                return key.GetValueNames().Any(name => CompatibleRuntimeVersionRegex().Match(name).Success);
            }
            else
            {
                // TODO: run "dotnet -v"
                throw new NotImplementedException();
            }
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

        [GeneratedRegex(@"9\.0\.[0123489]\d*")]
        private static partial Regex CompatibleRuntimeVersionRegex();
        [GeneratedRegex(@"9\.0\.[567]")]
        private static partial Regex BuggedRuntimeVersionRegex();
    }
}
