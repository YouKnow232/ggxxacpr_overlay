=== GGXXACPR Overlay ===
version: v2.0.0.2

Overlay for Guilty Gear XX Accent Core Plus R
Steam Release, product version: v1.2.0.0


== Instructions ==

1)  Copy this folder (GGXXACPROverlay) to the GGXXACPR game folder such that the mod
directory is "..\steamapps\common\Guilty Gear XX Accent Core Plus R\GGXXACPROverlay\"

2)  Launch GGXXACPRInjector.exe while +R is open.


== Hotkeys ==

F1 = Toggle Hitboxes
F2 = Toggle Untech/HSD meters
F3 = Toggle throw range boxes
F4 = Toggle between both boxes / P1 box only / P2 box only
F5 = Freeze frame (i.e. pause->hide menu hotkey)
F6 = Frame step from freeze frame (works from pause->hide menu)
F7 = Toggle black out background


== FAQ ==

1)	Q: How do air throw boxes work?
	A: If the opponent's bottom edge of their push box intersects with the attackers throw box they are in throw range.

2)	Q: How do Sol's clean hit boxes work?
	A: If the opponent's origin point (the purple cross by default) is within the orange
		clean hit box at the moment the attack connects, it'll be a clean hit.

3)	Q: What do the colors on the side guage mean?
	A: They're [hitstun decay thresholds.](https://dustloop.com/w/GGACR/Damage#Untechable_Time_Scaling)


== Troubleshooting ==

1) Anti-virus software will hate this as it is unknown software that uses DLL injection techniques.
Configure any anti-virus software to ignore/exclude the mod folder. The anti-virus may delete any files
it considers dangerous, so make sure you have all files

2) This overlay may have incompatibilities with other overlays/mods. If you don't see any hitboxes but some of the hotkeys still work this is likely the issue.
MSI Afterburner overlay is a known incompatibility. OBS 'capture specific window' will not see the hitbox overlay if OBS is opened after injecting the overlay.
To get OBS 'capture specific window' to see the overlay, you'll have to get the game window capture setup first and then inject the overlay.


== OverlaySettings.ini ==

The mod will look for OverlaySettings.ini in the GGXXACPROverlay folder, same directory as GGXXACPR_Win.exe.
If it doesn't see one, it will copy a default settings ini to that directory.

Comments start with ';' or '#'. White-space is ignored. Sections are maked by lines that begin
with '[' and end with ']'. Fields must be under the correct section. Nothing is case-sensitive.
