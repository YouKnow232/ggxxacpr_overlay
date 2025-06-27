=== GGXXACPR Overlay ===
version: v2.0.0-Alpha-1-CL

Overlay for Guilty Gear XX Accent Core Plus R
Steam Release, product version: v1.2.0.0


== Instructions ==

1)  Copy this folder (GGXXACPROverlay) to the GGXXACPR game folder such that the mod
directory is "..\steamapps\common\Guilty Gear XX Accent Core Plus R\GGXXACPROverlay\"

2)  Launch GGXXACPRInjector.exe while +R is open.


== Troubleshooting ==

1)  This overlay depends on the .NET runtime to run. If you are getting errors related to that
download the correct runtime version from Microsoft here (must be x86 to match +R).
https://builds.dotnet.microsoft.com/dotnet/Runtime/9.0.6/dotnet-runtime-9.0.6-win-x86.exe

2)  Anti-virus software will hate this as it is unknown software that uses DLL injection techniques.
Configure any anti-virus software to ignore/exclude the mod folder. The anti-virus may delete any files
it considers dangerous, so make sure you have all files 


== OverlaySettings.ini ==

The mod will look for OverlaySettings.ini in the game folder, same directory as GGXXACPR_Win.exe.
If it doesn't see one, it should copy a default ini to that directory.

Comments start with ';' or '#'. Whitespace is ignored. Sections are maked by lines that begin
with '[' and end with ']'. Fields must be under the correct section. Nothing is case-sensitive.
