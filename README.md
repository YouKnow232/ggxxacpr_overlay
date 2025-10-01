# GGXXACPROverlay
Displays hitboxes and a frame meter in an overlay for the Steam release of Guilty Gear XX Accent Core Plus R.

## How to Use
1. Copy the GGXXACPROverlay folder to Plus R's folder. (..steamapps\common\Guilty Gear XX Accent Core Plus R\GGXXACPROverlay)
2. Launch Plus R
3. Launch GGXXACPRInjector.exe
4. (Optional) Check README.txt and OverlaySettings.ini for additional information

## Known Issues
- Issues with anti-virus software. Exclude the overlay folder from your anti-virus if you are having problems.
- Incompatible with some overlays/mods (e.g. MSI Afterburner's overlay)
- Background black out may cause desyncs in replay mode, toggle it off before opening a replay.
- This overlay was not intended to be used during netplay and therefore was not testing during it. Crashes, desyncs, and other unexpected behavior may occur.

## Build Instructions
This project was built with Visual Studio 2022 v17.14.9 + .NET SDK v9.0.302

GGXXACPROverlay.dll and its related files must be in the designated mod folder GGXXACPROverlay.
There's a GameFolder publish profile you can configure to facilitate that. After that's built you can use the normal build & run workflow since
GGXXACPRInjector doesn't need to know the file directory in order to inject bootstrapper dll. You just need to republish in order to reflect
changes in the main GGXXACPROverlay.dll.

### Dependencies
This project requires an x86 installation of the .NET hosting api in order to host the runtime within the GGXXACPR_Win process.
This should be bundled with a regular x64 .NET SDK installation.
See DEV-README.txt

### Steps
1. Clone the repository
```shell
git clone https://github.com/YouKnow232/ggxxacpr_overlay.git
```
2. Open the .sln file in Visual Studio
3. Configure C++ linker settings if needed (see DEV-README.txt)
4. Right-click GGXXACPRInjector in the Solution Explorer -> Publish...
5. Select FolderPublish.pubxml -> Publish
6. Click "Navigate" in the green "publish succeeded" box

## Project Architecture
1. GGXXACPRInjector injects GGXXACPROverlay.Bootstrapper.dll
2. Bootstrapper hosts .NET runtime, loads and calls GGXXACPROverlay.dll
3. GGXXACPROverlay.dll installs hooks


## Special Thanks / References
- Labreezy and contributors of [rev2-wakeup-tool](https://github.com/Labreezy/rev2-wakeup-tool)
- odabugs and contributors of [kof-combo-hitboxes](https://github.com/odabugs/kof-combo-hitboxes)
- TheLettuceClub, Ryn for +R reverse engineering knowledge [GGXXACPR_Framework](https://github.com/TheLettuceClub/GGXXACPR_Framework)
- DPScrub for frame stepping concept and for giving this overlay a home in [ACPR_IM](https://github.com/DPS-FGC/ACPR_IM)
- Everyone who helped by raising issues and/or sharing their knowledge
