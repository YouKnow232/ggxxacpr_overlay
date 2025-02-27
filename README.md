# GGXXACPROverlay
Displays hitboxes and a frame meter in a seamless overlay for the Steam release of Guilty Gear XX Accent Core Plus R.

## How to Use
1. Launch Plus R
2. Launch Overlay
3. Overlay will display when you are in game!

## Frame Meter Legend
![#01b597](https://placehold.co/15x15/01b597/01b597.png) Startup / Counter Hit State <br>
![#CB2B67](https://placehold.co/15x15/CB2B67/CB2B67.png) Active <br>
![#006FBC](https://placehold.co/15x15/006FBC/006FBC.png) Recovery <br>
![#C8C800](https://placehold.co/15x15/C8C800/C8C800.png) Blockstun / Hitstun <br>
![#41F8FC](https://placehold.co/15x15/41F8FC/41F8FC.png) Movement <br>
### Under Lines
![#FFFF00](https://placehold.co/15x15/FFFF00/FFFF00.png) FRC <br>
![#FF0000](https://placehold.co/15x15/FF0000/FF0000.png) Slash Back <br>
![#FFFFFF](https://placehold.co/15x15/FFFFFF/FFFFFF.png) Full Invuln <br>
![#FF7D00](https://placehold.co/15x15/FF7D00/FF7D00.png) Throw Invuln Only <br>
![#007DFF](https://placehold.co/15x15/007DFF/007DFF.png) Strike Invuln Only <br>
![#785000](https://placehold.co/15x15/785000/785000.png) Armor / Guardpoint / Parry<br>

## Known Issues
### General
- Fullscreen is not currently supported
- Frame Meter does not fully support Replay mode
    - Rewinding is not recognized by the Frame
    - Frame Meter may skip ahead while paused in a replay
- Startup is not fully implemented for EX characters
- Projectile startup is not implemented
- Startup/Advantage will not exceed 77
- Slidehead unblockable hit has no hitbox/active frames
- Dizzy 421S second stage has no hitbox/active frames
- Dizzy fish laser has no hitbox/active frames
- Startup for supers may be incorrect if the opponent removes super freeze from themselves (i.e. Baiken)
    - When using the "toggle superflash pausing" settings, startup will count superflash frames

## Special Thanks / References
- michel-pi and contributors of [GameOverlay.Net](https://github.com/michel-pi/GameOverlay.Net)
- Labreezy and contributors of [rev2-wakeup-tool](https://github.com/Labreezy/rev2-wakeup-tool)
- odabugs and contributors of [kof-combo-hitboxes](https://github.com/odabugs/kof-combo-hitboxes)
- TheLettuceClub and Ryn for +R reverse engineering knowledge [GGXXACPR_Framework](https://github.com/TheLettuceClub/GGXXACPR_Framework)
