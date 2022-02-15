# DS3 Fog Gate Randomizer

Changes how areas link together by randomizing where fog gates lead to, in the spirit of ALttP/OoT entrance randomizers, and heavily based on [DS1 Fog Gate Randomizer](https://www.nexusmods.com/darksoulsremastered/mods/165)

Fog gates are now permanent, and traversing them warps you to the other side of a different fog gate. These warps are always fixed for a given seed.

This mod is possible because of engine similarities between DS1 and DS3 which are unfortunately not shared with DS2. Randomization of DLCs can be enabled or disabled.

If you have issues installing the mod or would like to provide feedback or playtest, you can join the discord server at https://discord.gg/QArcYud (also for DS3 Static Randomizer, Sekiro Randomizer, and hopefully Elden Ring Randomizer)

## Gameplay changes

It is configurable which types of entrances are randomized and which behave like the base game. They come in these categories:

- Boss fog gates
- Preexisting warps between areas
- PvP fog gates

No new fog gates are added. PvP fog gates are ones you normally only see when you are invaded, plus a few misc others.

By default, warps are bidirectional, meaning you can explore the world like it is a cut up and reassembled version of the base game. If you instead enable the "Disconnected fog gates" option, warps become points of no return. This is similar to ALttP Insanity Entrance Shuffle or OoT BetaQuest.

Usually more randomized entrances = longer run. Other randomizers and disconnected fog gates both result in more to explore.

Some misc things to know about mechanics:

- By default, placing the Coiled Sword in Firelink Shrine unlocks warping between bonfires. The randomizer tries to place Firelink Shrine in the first 15% of accessible areas, and spoiler logs also contain info on which areas are in the critical path. There is also an option for immediately allowing warping, with no initial searching.
- The warp after defeating Ancient Wyvern can be randomized. If you miss it, you can do it again by interacting with the bell near where he lands at the start.
- The warp to Flameless Shrine can also be randomized in some options. If you miss it, you can do it again by interacting with Prince Lothric's Throne.
- The warp from the Hollow Manservant (or his replacement) in Undead Settlement can be randomized, and is always available.
- The warp after defeating Aldrich and Yhorm is preserved, but not part of logic currently
- You can ascend out of Smouldering Lake back to the Catacombs, as the ladder bridge breaks automatically in the hallway beneath it. To restore the bridge, save+quit at the top.

Also, some things which are in the base game you may need to know about:

- Using the Small Envoy Banner also requires defeating Demon Prince
- There is a door in Dragonkin Mausoleum which opens only after Ancient Wyvern has been defeated
- You can drop down into Irithyll from Yorshka's Prison Tower

### How to win

By defeating Soul of Cinder. If the "Require Cinders of a Lord" option is enabled then placing the Cinders on their thrones is the only way to do this.

Taking notes can be helpful to remember how to get to different important places. One possible strategy (other than writing literally everything down) is to write down routes you *didn't* take when going deeper along one particular branch. And if you reach a place you need to come back to later, make sure to write down how to get there.

### Scaling

This is an optional feature of the mod to scale up and down enemy health and damage based on distance from the start. This is done statically when the randomizer is run, not during the playthrough. If you find enemies becoming too powerful, you may have missed other easier paths which branched off earlier on. The goal is to make it more enjoyable to actually fight enemies, rather than only run past them.

The randomizer checks what is the shortest path for you to access Firelink Shrine, and ensures that bosses on that path are scaled within reason for a +0 weapon. On the other hand, areas which appear earlier in the base game are never scaled down. Soul of Cinder is never scaled down.

If the enemy scaling option is used in DS3 Static Enemy Randomizer, the both scalings will be applied. The final scaling may be rough, but it should be in the right range.

## Installation

This randomizer can be used as a standalone mod with DS3 Mod Engine. It can also be used with other randomizers. It probably can't be used with other large overhaul mods, because it's a pile of hundreds of manually specified edits with associated logic, and doesn't magically account for changes it doesn't know about.

The overall installation approach is as follows. There are further details down below of how to configure other randomizers. The required order is Enemy/Item -> Fog Gate -> Irregulator.

1. Unzip DS3FogMod.zip into your Dark Souls 3 installation, so that there is a directory called `fog` which contains `FogMod.exe` and subdirectory `fogdist`.

2. [DS3 Mod Engine](https://www.nexusmods.com/darksouls3/mods/332) must be installed (both modengine.ini and dinput8.dll in your game directory), with modOverrideDirectory set to the mod directory, "\fog" by default. Note that Enemy Randomizers must use a different version of Mod Engine, so check their installation instructions for information on that.

3. This is where you install item randomizers, enemy randomizers, and non-randomizer mods. These must happen outside of the `fog` directory, because FogMod will use those files as a base for its own randomization. **See below for specific instructions.**

4. Finally open FogMod.exe, select your options, click "Randomize", and then start your game! (restart DS3 if it's currently running) You can tell it's intalled if your first bonfire has a "Travel" option.

5. Check the spoiler_logs directory if you get stuck!

### Using with other mods

I apologize in advance for how convoluted these installation instructions are, but please bear with it, since it is by far the simplest to maintain. In the maximum case, it involves 3 separate subdirectories of your main Dark Souls 3 installation location.

1. The `mod` directory is the base directory for manually installed mods. This includes cosmetic mods like skins or Poorly Translated Mod, and also [GodFilm233's enemy randomizer](https://www.nexusmods.com/darksouls3/mods/484) if you're not using my DS3 Static Enemy Randomizer.
2. The `randomizer` directory is used by [DS3 Static Item/Enemy Randomizer](https://www.nexusmods.com/darksouls3/mods/361). It can optionally merge itself with the files in `mod`.
3. The `fog` directory is for DS3 Fog Gate Randomizer, and it can merge itself with either of the above.

modengine.ini contains a modOverrideDirectory field which tells the game to load mods from the specified subdirectories. It should point at "\fog" at the end of all of the installation steps.

To reiterate, if you're using an enemy randomizer, you need to replace dinput8.dll with the version packaged with the enemy randomizer, or else the game will crash. Do not use any other version of dinput8.dll; do not use the official version from the DS3 Mod Engine mod page.

### Using DS3 Static Randomizer
[DS3 Static Item and Enemy Randomizer](https://www.nexusmods.com/darksouls3/mods/361) is a combined item/enemy randomizer compatible with Fog Gate Randomizer. Make sure to use the latest version.

If you have mods installed in the `mod` folder, make sure to check the "Merge mods" option in DS3 Static Randomizer. If this is successful, it means that the `randomizer` directory will contain both mods merged together, which can then be used as a base for Fog Gate Randomizer.

If you want to use the randomizer options which seem to result in the most interesting Fog Gate runs, then inside of the item randomizer UI, you can click the button which says "Set options from string" and paste this in:

dlc1 dlc2 dlc2fromdlc1 earlydlc earlylothric earlyreq edittext enemy item lizards mimics raceloc_ashes raceloc_chest raceloc_miniboss racemode racemode_health scale v4 yhormruler 10 0

(Then, optionally disable either item or enemy randomizer if you're only using one type of randomization.)

For item randomizer, adjust the "Key item placement" settings based on how much exploration you want to do, enable Tree Skip if you want, and of course any other options.

For enemy randomizer, you can select a preset for a more chaotic run and otherwise select your preferred options. As mentioned above, Enemy Randomizer scaling and Fog Gate Randomizer scaling work together well enough.

If you see a warning inside item randomizer UI about modOverrideDirectory being incorrect, you can ignore this, because it won't be the final directory anyway.

### Using Auto-Equip Item Randomizer

[Auto-Equip Item Randomizer](https://www.nexusmods.com/darksouls3/mods/241) isn't installed in a game subdirectory. Instead, edit modengine.ini to set `chainDInput8DLLPath` appropriately. You can do this in addition to installing either enemy randomizer.

### Running Fog Gate Randomizer

Finally, click on FogMod.exe in the `fog` directory. Using "Select other mod to merge", select Data0.bdt from the `randomizer` directory. By default, this path should be `C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game\randomizer\Data0.bdt` (note the "randomizer" part) This can also be the `mod` directory if you have mods installed there and you didn't use DS3 Static Randomizer.

If you enabled Tree Skip for DS3 Static Item Randomizer, also do it here. Select any options you like, then click "Randomize!".

The status bar at the bottom will show a 5-digit "key item hash" which changes depending on key item routing. If you intended to use key item randomization but this isn't showing up, you are probably merging from the wrong place.

Change the override directory in modengine.ini to "\fog".

### Irregulator

Finally, if you are using [Irregulator](https://www.nexusmods.com/darksouls3/mods/298), install it at the very end. This adds a lot of chaos so use it with caution. You can unzip it anywhere; it doesn't need to be in your DS3 game directory. When picking the directory to randomize, select the `fog` directory (you must change the "Game Directory" to the mod directory, not your actual game directory). From this point onwards, you can re-run Irregulator at any time.

### Reinstalling other mods in the middle of a run

The most common use case for changing other mods is rerolling Enemy Randomizer. The rerolling process basically requires going through the entire installation sequence again. This is unavoidable because all of the randomizers touch the exact same game files.

Make sure to run Item and Fog Gate Randomizers with the same seeds and options you used previously. By default, they remember your last used seed, so it can be done relatively quickly.

## Credits

Thanks to HotPocketRemix, who had done some preliminary work on the same idea in DS1, and TKGP, who forced my hand into finally doing it for DS3. The randomizer editing game files is done using SoulsFormats by TKGP with layouts thanks to Pav, with a special shoutout to Meowmaritus for making programmatic emevd editing possible. Research and development on the mod would not be possible without DSMapStudio by katalash and Yapped by TKGP. Learn more about modding Souls games (and find #tech-support if needed) in http://discord.gg/mT2JJjx.

Find the source code on GitHub: https://github.com/thefifthmatt/FogMod

## List of all entrances

Boss fog gates

- From Cemetery of Ash to Iudex Gundyr
- From High Wall to Vordt
- From Undead Settlement to Curse-Rotted Greatwood
- From Farron Keep to Abyss Watchers
- From Road of Sacrifices to Crystal Sage
- From Crystal Sage toward Cathedral
- From Cathedral to Deacons of the Deep
- From Catacombs to Wolnir's Room
- From Smouldering Lake to Old Demon King
- From Central Irithyll to Pontiff
- From Pontiff toward Anor Londo
- Between above Pontiff and Anor Londo
- From Anor Londo to Aldrich
- From Aldrich to left elevator after
- From Aldrich to right elevator after
- From Profaned Capital to Yhorm
- From High Wall to Dancer
- From Consumed King's Gardens to Oceiros
- From Lothric Castle to Dragonslayer Armour
- From Dragonslayer Armour toward Grand Archives
- From Grand Archives to Twin Princes
- From Untended Graves to Champion Gundyr
- From Archdragon Peak Start to Ancient Wyvern
- From Ancient Wyvern to Dragonkin Mausoleum past the chain axe serpent
- Between area overlooking Ancient Wyvern Arena and stairs around Dragonkin Mausoleum
- Between area overlooking Ancient Wyvern Arena and Dragonkin Mausoleum
- From Archdragon Peak to Nameless King
- From Ariandel Chapel to Sister Friede
- From Depths of Ariandel to Gravetender
- From Dreg Heap to Demon Prince
- From Demon Prince toward Ringed City
- From Ringed City to Halflight
- From Halflight to left side after
- From Halflight to right side after
- From Ringed City to Midir
- From Filianore's Rest to Gael
- From Gael to Shira's invasion area
- From Kiln Entrance to Soul of Cinder

Warps

- Transport after Vordt
- Examining the Hollow Manservant in Undead Settlement
- From Cathedral to Ariandel
- From Wolnir's Room to Wolnir
- From Wolnir back to Wolnir's Room
- From Irithyll Dungeon with Path of the Dragon to Archdragon Peak
- After defeating Ancient Wyvern
- From Firelink Shrine after placing all Cinders of a Lord to Flameless Shrine
- From Flameless Shrine to Kiln
- From Kiln to Dreg Heap
- From after Friede fight to Dreg Heap
- Transport after Demon Prince
- Waking up Filianore

PvP fog gates

- Between Firelink Shrine and front
- Between the entrance to Bell Tower and Firelink Shrine
- Between Undead Settlement and the Giant's Tower
- Between the start of Road of Sacrifices and Halfway Fortress
- Between the start of Cathedral and the chapel, on the left doorway
- Between the start of Cathedral and the chapel, on the right doorway
- Between Rosaria's room and the rest of the Cathedral
- Between the stairs after Abyss Watchers and Catacombs
- Between Smouldering Lake and Catacombs
- On the close end of the starting Irithyll bridge
- Between Yorshka and the Anor Londo spinning staircase
- Between the hallway immediately after Pontiff and Anor Londo
- Between Distant Manor and Irithyll
- Between the area above Dorhys' Gnawing and the illusory railing
- Between Distant Manor and Irithyll Dungeon
- Between above Emma's room and Consumed King's Gardens
- Between bottom of Twin Princes shortcut elevator and Lothric Castle
- Between a ledge above Emma's room and Lothric Castle bonfire
- Between Lothric Castle entrance and above Emma's room
- Between right after Dragonslayer Armour and the start of Grand Archives
- At the top of the shortcut elevator right outside Twin Princes
- Between top of Archdragon shortcut elevator and Dragonkin Mausoleum
- Between shortcut elevator and the start of Archdragon Peak
- Between below Nameless King and below the bell
- Between below Nameless King and Dragonkin Mausoleum
- Between the Ariandel Chapel basement and the dropdown to Dunnel
