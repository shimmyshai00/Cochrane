# Cochrane
Star Trek-alike warp drive mods for Kerbal Space Program. Not even really alpha yet as texturing artwork is neither great nor complete. Also contains a
(not yet independent) resource management system.

## Features
Provides a warp drive with game mechanics similar to those found on the popular science fiction series, _Star Trek_. Somewhat inspired
by a similar mod called "TrekDrive" by "TheShadow", but hopefully much more polished while also giving just the drive as a standalone part set instead of
attempting to bring whole Star Trek ships to the game.

* The warp drive requires several components to function: a warp core, warp nacelles, and warp field generator.
* The warp drive is powered by Liquid Deuterium and Antimatter, mixed appropriately in a 1:1 mass ratio.
* The warp core generates Warp Plasma from these two fuels, specially-confined energized plasma which runs the rest of the drive train.
* Warp nacelles must be affixed in order for the drive to function.
* Warp speed is set and measured in warp factors, using the TNG-era (not TOS) warp scale, so that the speed, when measured in lightspeeds, rises as the
10/3 power of the warp factor until a speed of warp 10 is approached (though no drive here supports warp 10 which is massive overkill for the KSPverse).
* Parts are designed with a form factor that fits well on conventional KSP vessels.
* Dilithium is a necessary resource but no in-game
* By default, the warp drive is tuned specifically for use with mods that add interstellar destinations to the game: to avoid spoiling the in-system rocket
mechanics, the warp drive cannot be engaged until you are beyond the stock Kerbol system (about 150 Gm distant from Kerbol).

### SmartResources
This mod also contains its own resource manager subsystem, which it could be argued as being a "demo" for: SmartResources, which uses a special flow graph
simulation methodology to balance resource flows so that they work without issues at arbitrary time warps. In particular, here it is applied to the special
WarpPlasma power resource. This framework is meant to be used for porting other mods and may be included as stock in the putative OpenKSP project.

## Dependencies
This mod depends on the following others:
* [ModuleManager (4.2.1)](https://github.com/sarbian/ModuleManager)
* [Community Resource Pack (1.4.2)](https://github.com/BobPalmer/CommunityResourcePack)

## Installation
The GameData folder from this repository should work if its contents, together with the two dependencies above, are delivered in the usual way into the
same-named folder of your KSP installation.

### Compilation (GNU/Linux only)
The provided .csproj file will likely require adjustments to be made to the file path specs it contains in order to meet the needs of your own operating 
system installation and user account.

## Legal
You may redistribute this mod's artwork, but it must not be sold. You must credit the original author (Shimrra Shai) appropriately. The other mods given 
as dependencies are not included in this repository.

The program code is distributed under GPLv3; the SmartResources component, however, is distributed under the MIT License.
