# DS3 Fog Gate Randomizer

Changes how areas link together by randomizing where fog gates lead to, in the spirit of ALttP/OoT entrance randomizers, and heavily based on [DS1 Fog Gate Randomizer](https://www.nexusmods.com/darksoulsremastered/mods/165)

Fog gates are now permanent, and traversing them warps you to the other side of a different fog gate. These warps are always fixed for a given seed.

Currently both DLCs (Ashes of Ariandel and The Ringed City) are required to use this mod. It might be possible to make them optional in the future. This mod is possible because of engine similarities between DS1 and DS3 which are unfortunately not shared with DS2.

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
- You can ascend out of Smouldering Lake back to the Catacombs by using a projectile such as a firebomb to break the bridge

Also, some things which are in the base game you may need to know about:

- Using the Small Envoy Banner also requires defeating Demon Prince
- There is a door in Dragonkin Mausoleum which opens after Ancient Wyvern has been defeated
- You can drop down into Irithyll from Yorshka's Prison Tower

### How to win

By defeating Soul of Cinder. If the "Require Cinders of a Lord" option is enabled then placing the Cinders on their thrones is the only way to do this.

Taking notes can be helpful to remember how to get to different important places. One possible strategy (other than writing literally everything down) is to write down routes you *didn't* take when going deeper along one particular branch. And if you reach a place you need to come back to later, make sure to write down how to get there.

### Scaling

This is an optional feature of the mod to scale up and down enemy health and damage based on distance from the start. This is done statically when the randomizer is run, not during the playthrough. The goal is to make it more enjoyable to actually fight enemies, rather than only run past them.

The randomizer checks what is the shortest path for you to access Firelink Shrine, and ensures that bosses on that path are scaled within reason for a +0 weapon. On the other hand, areas which appear earlier in the base game are never scaled down. Soul of Cinder is never scaled down.

## Installation

This randomizer can be used as a standalone mod with DS3 Mod Engine. It can also be used with other randomizers, with an installation process described below. It probably can't be used with other large overhaul mods, because it's a pile of hundreds of manually specified edits with associated logic, and doesn't magically account for changes it doesn't know about.

Scroll down to see instructions for using this mod with other randomizers. To use this mod by itself:

1. Unzip DS3FogMod.zip into your Dark Souls 3 installation, so that there is a directory called `fog` which contains FogMod.exe.

2. [DS3 Mod Engine](https://www.nexusmods.com/darksouls3/mods/332) must be installed (both modengine.ini and dinput8.dll in your game directory), with modOverrideDirectory set to the mod directory, "\fog" by default. Note that Enemy Randomizer should be using a different version of Mod Engine, but if you're using the mod with Enemy Randomizer or any other randomizer, you should be looking at the directions below anyway!

3. Open the randomizer exe, select your options, click "Randomize", and then start your game! (restart DS3 if it's currently running) You can tell it's intalled if your first bonfire has a "Travel" option.

### Using with other randomizers

I apologize in advance for how convoluted these installation instructions are, but please bear with it, since it is by far the simplest to maintain. In the maximum case, it involves 3 separate subdirectories of your main Dark Souls 3 installation location. The required order to do things in is Enemy -> Item -> Fog Gate -> Irregulator. You can skip any of these steps, but anyway, here are the most general instructions possible.

1. To use DS3 Enemy Randomizer: install and run Enemy Randomizer in the `mod` directory, following the instructions in its README.

You can use [GodFilm233's randomizer](https://www.nexusmods.com/darksouls3/mods/484) or anything else which randomizes enemies in-place. I would recommend checking both boxes in its installer for the best balance.

To emphasize, as part of this, you need to replace dinput8.dll with the version from Enemy Randomizer, or else the game will crash. Do not use any other version of dinput8.dll; do not use the official version from the DS3 Mod Engine mod page.

2. To use [DS3 Static Item Randomizer](https://www.nexusmods.com/darksouls3/mods/361) (randomize key items): install and run Item Randomizer (version at least v0.2) in the `randomizer` directory, following the instructions in its README. Make sure to check "Merge mods".

If you want to use the Item Randomizer options which seem to result in the most interesting Fog Gate runs, then inside of the item randomizer UI, you can click the button which says "Set options from string" and paste this in:
dlc1 dlc2 dlc2fromdlc1 earlydlc earlylothric mergemods raceloc_ashes raceloc_chest raceloc_miniboss racemode startingtwohand v2 10 0

And then adjust the "Key item placement" settings based on how much exploration you want to do, enable Tree Skip if you want, and adding NG+ rings if you want. Of course, feel free to experiment with any other options as well.

If you see a warning inside item randomizer UI about modOverrideDirectory being incorrect, you can ignore this, because it won't be the final directory anyway.

Note that if you are using the [Auto-Equip Item Randomizer](https://www.nexusmods.com/darksouls3/mods/241) instead, you can ignore this step, but make sure to disable key item randomization and set `chainDInput8DLLPath` appropriately in that case.

3. Finally, add Fog Gate Randomizer into the `fog` directory and open it up. Using "Select other mod to merge", select Data0.bdt from the `randomizer` directory, *not* from the DS3 game directory. By default, this path should be `C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game\randomizer\Data0.bdt` (note the "randomizer" part)

If you enabled Tree Skip for Item Randomizer, also do it here. Be careful about using scaling if you're using Enemy Randomizer, since it won't do much to hard enemies placed in early locations, and may create some way-too-buffed enemies later on. Select any options you like, then click "Randomize!".

The status bar at the bottom will show a 5-digit "key item hash" which changes depending on key item routing. If you intended to use key item randomization but this isn't showing up, you are probably merging from the wrong place.

4. Change the override directory in Mod Engine to "\fog".

5. Finally, you can run [Irregulator](https://www.nexusmods.com/darksouls3/mods/298) at the very end. You can unzip it anywhere; it doesn't need to be in your DS3 installation. When picking the directory to randomize, select the `fog` directory (you must change the "Game Directory" to the mod directory, not your action game directory). From this point onwards, you can re-run Irregulator at any time.

This process does mean that rerolling Enemy Randomizer is annoying. This is unavoidable because literally all of the randomizers touch the exact same game files. You can still do it at any point, but then you must go through the entire installation sequence again, making sure to run Item and Fog Gate Randomizers with the same seeds and options you used previously. By default, they remember your last used seed, so it can be done relatively quickly.

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
