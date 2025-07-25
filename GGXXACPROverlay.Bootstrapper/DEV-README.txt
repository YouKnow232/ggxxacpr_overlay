== Dependencies ==

GGXXACPROverlay.Bootstrapper requires:
 - nethost.lib
 - nethost.dll (x86)
 - hostfxr.h
 - nethost.h
 - coreclr_delegates.h

These can be obtained by downloading the dotnet SDK (x86) and including the directory containing those files.
In initial development, this was:

"C:\Program Files (x86)\dotnet\packs\Microsoft.NETCore.App.Host.win-x86\9.0.6\runtimes\win-x86\native\"

If your installation differs in version or location, you must update the project settings to look in the correct directory.
