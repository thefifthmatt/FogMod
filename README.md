# DS1 Fog Gate Randomizer

Changes how areas link together by randomizing where fog gates lead to, in the spirit of ALttP/OoT entrance randomizers.

Fog gates are now permanent, and traversing them warps you to the other side of a different fog gate. Most warps are bidirectional. Bosses and enemies can be automatically scaled based on these new connections.

**This mod must be played with the game offline. Also, before going back online, be sure to delete any save files you used.** Steam does not have to be offline, just the in-game network settings.

PTDE support is not possible because I don't own PTDE on Steam. If you want to play this mod, use Dark Souls Remastered.

## Gameplay changes
It is configurable which types of entrances are randomized and which behave like the base game. They come in these categories:

- Traversable fog gates (non-boss)
- Boss fog gates
- Preexisting warps between areas
- Lordvessel gates
- Major PvP fog gates (between areas)
- Minor PvP fog gates

No new fog gates are added. PvP fog gates are ones you normally only see when you are invaded. Adding them makes randomizations a bit more different from the base game, while still being interestingly nonlinear.

There are a few differences from the base game:

- Estus and class items are available before any fog gates
- All bonfires have an option to return to the start of the game, in case you get stuck after using a bonfire
- You can't save & quit to escape a boss fight, because positions before warps are always discarded by the game. Use Pacifist Mode for more mobility options.
- The trigger for Undead Asylum #2 is using the Big Pilgrim's Key, rather than traveling to Firelink.
- Seath's scripted player death has been replaced with an object you can use to warp always
- NPC invasions removed for now, they are messy to clean up after

### How to win
By defeating Gwyn. Warps to the kiln are not randomized, mainly because they are tied to serpent loyalty. If the "Require Lord Souls" option is enabled then opening the Kiln door is the only way to get to Gwyn.

Taking notes can be helpful to remember how to get to different places. Alternatively, have a good memory, or rely on chat to have good memory.

Starting with or using the master key is never required. If you have it, it can be used to access areas early, but there may be significant scaling differences on the other side of those doors.

### Scaling
This is an optional feature of the mod to scale up or down enemy health and damage based on estimated SL. The goal is to make it more enjoyable to actually fight enemies, rather than only run past them. Note that it scales health and damage uniformly, so it is possible for enemies to deal more or less damage than you'd expect.

The randomizer checks what is the shortest path for you to access a blacksmith (Andre, Giant Blacksmith, or Vamos), and ensures that bosses on that path are scaled within reason for a +0 weapon. On the other hand, early game areas, such as before Undead Parish, are never scaled down.

## Installation
To install the mod, run the included program, select your options, and click "Randomize!" to randomize. This automatically creates backups. You must select the game exe manually if you have a non-standard DS1R install location. You can also enter a seed (any 32-bit integer). For all changes to take effect, restart the game, and start a new save file.

When randomizing with scaling enabled, try not to access existing save files that have been used with different scaling (different seed or options) or no scaling. If you quit out of a save file with enemies loaded into the area, and the scaling changes too much on reload, the game will crash to desktop. The save file is not corrupted, but can't be opened anymore unless you return to the previous enemy scaling.

To uninstall, click "Restore backups". This replaces the game files with whatever they were before the first randomization. To be completely sure all mods are gone, select Properties → Local Files → Verify Integrity Of Game Files in Steam.

### With DS1 item randomizer
This mod is partly compatible with https://www.nexusmods.com/darksoulsremastered/mods/86

DS1 item randomizer's "Key Placement" option must be set to "Not Shuffled". After you run fog gate randomizer, run DS1 item randomizer from the same game directory. This has the effect of shuffling around weapons, armor, upgrade items, etc. while keeping fog gate connections.

### Compatibility with other mods
Fog Gate Randomizer is *not* compatible with game-file-based mods which make event-based changes to game progression, like Daughters of Ash or Scorched Contract or Enemy Randomizer.

If the other mod doesn't modify any game files in event\, map\MapStudio, msg\ENGLISH, param\GameParam, or script\talk, then the mods should be independent, and you can follow the other mod's installation steps.

If the other mod contains files in map\MapStudio, msg\ENGLISH, or param\GameParam, merging may be possible. Whenever Fog Gate Randomizer runs, it always does its randomization based on game files in the "dist" directory under FogMod.exe, modifies them, and copies those into the actual game. Any other mods to params, MSBs, or messages can override the vanilla version in dist\, and if the changes aren't actually incompatible then randomization will succeed.

If the other mod requires modifications to event\ or script\talk\, it is not compatible.