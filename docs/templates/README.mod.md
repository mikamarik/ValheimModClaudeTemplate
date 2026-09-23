# MyValheimMod

<One or two sentences for a player: what the mod lets them do. Keep it the same as the manifest's
description, which may be 250 characters at most.>

<A GIF of the main feature, by its GitHub raw URL so it also shows on Thunderstore:
![MyValheimMod](https://raw.githubusercontent.com/YourName/MyValheimMod/main/docs/media/example.gif)>

## Features

<Written to read human: whole sentences, no bold lead-ins, no fragment lists.>

The example window opens with F9, or with L2 and R1 together on a controller. It
says hello. Delete this paragraph with the example.

## Controls

Everything works with a keyboard and mouse, and with a controller. "L2" is the game's own
modifier button, so it follows the controller layout you picked in the game's settings.

| What | Keyboard and mouse | Controller |
|---|---|---|
| Open or close the example window | F9 | L2 + R1 (LT + RB) |
| Press a button in the window | click | D-pad to pick, cross (A) to press |
| Close the window | Esc | circle (B) |

Keys can be changed in `BepInEx/config/com.yourname.myvalheimmod.cfg`, or in game with a
configuration manager.

## Install

With a mod manager (r2modman, Thunderstore Mod Manager): install MyValheimMod, it pulls in BepInEx.

By hand: install [BepInExPack for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/),
then copy `MyValheimMod.dll` into `BepInEx/plugins/`.

The mod is client-side. It adds nothing to the world save, and other players do not need it.

## Made with AI

This mod was made with the help of Generative AI (Claude, Anthropic).

## License

MIT, see LICENSE.
