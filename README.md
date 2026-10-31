# Walls & Sieges for WorldBox

Cities build real fortifications, and wars turn into sieges. A mod for WorldBox 0.51 (via NeoModLoader), designed to stay close to the vanilla game: everything new is built on the game's own mechanics.

**Version 1.3.0** · by Rough_Sheriff42 · tested on WorldBox 0.51.2 · requires NeoModLoader

## What's new in 1.3

- **Manual gun control (2D and 3D).** Point at a siege gun and press **K** to take it, aim, **L** to fire. A dotted arc shows the shot; in 2D a small hint appears over the gun.
- **Gate health bar.** Point at a closed gate to see how long it will hold.
- **Siege scroll.** In 3D, after a siege you witnessed, a hint offers a chronicle of it, month by month (**J**).
- **Borders.** Walls no longer take away a city's borders, and houses no longer appear right in front of gates.
- **Gates.** They give in 15% faster to a crowd at the wall; when a gate falls, the fire goes out and the rubble can be walked over. Siege guns turn to face their target in 2D.
- **Worldfall 3D compatibility (optional).** 3D models for siege guns, ram, camp, towers, flags, palisade and rubble; man a gun in first person and follow the shell with the camera; ladders and hatches on watch towers; siege tents you can walk into; burning gates; siege events in the news feed and townsfolk talk about the war. The mod works without Worldfall as before.
- **Fixed** a very long game loading when Worldfall is not installed.

## What was new in 1.2

- **Culture traits.** Fortifications are now controlled by culture. A new trait group "Fortifications" adds eight traits: Wall Builders, Earthworks (moat), Coastal Defence, Double Wall, Mortar Craft, Paving, Ram Crews and Reinforced Gates. New cultures roll their traits when they appear; old saves are migrated once. Switch the requirement off in Mod Configure to build everything everywhere, as before.
- **Mortar.** A heavy siege gun with a long reload, a huge blast and a smouldering crater. Dwarves start with it, other cultures can learn it rarely during the game.
- **Siege ram.** Appears in the enemy camp 10 seconds after it is built (50%, one more try 10 seconds later; 70% for sea landings) and heads for the closed gates.
- **Alert levels.** Cities react to threats in steps (patrol, marching army, siege) and show it with a flag in the centre.
- **Better sieges.** Longer and more stable, armies stay at the walls while the guns work, defenders hold the gates and breaches.
- **New sprites in the game's style.** Siege guns, camp, ram, trait icons.
- **Languages.** English, Russian, Spanish, Portuguese (Brazil), Chinese (Simplified and Traditional). The mod follows the game's language.
- **Fixes.** Conflicts when building walls, stuck units, crashes.

## Features

**Walls and gates**
- Cities build walls with towers, gates and gate spurs. Capitals build two rings and dig a moat with bridges around the inner ring.
- Wall shapes vary: plain square, chamfered corners, corner bastions. Some cities have one side without a gate; capitals always have gates on every side.
- Four races are supported for now: humans, elves, orcs and dwarves. Every race has its own style of towers. Walls start out wooden and, over time, are upgraded to stone and repaired by the city (orcs keep their wooden walls and do not repair them). Other races are planned for a later update.
- Coastal towers and stakes protect the shore from ships.
- A capital's central plaza is paved with a random pattern (plaza decorations are disabled for now).
- Abandoned and shrinking cities slowly lose their walls.

**Sieges**
- When an enemy army approaches a fortified city, the gates close. Closed gates are iron, so they stand out from the rest of the wall.
- The army no longer turns back at the walls. It sets up a siege camp (campfire, tents, banner, guard tower, barrels and a cart) and besieges the city for up to five minutes.
- Big armies bring siege engines: a catapult or a trebuchet, and very large armies bring two or three. The engines hit the gates and the walls; explosions destroy buildings near the wall and hurt warriors.
- Damaged gates smoke, and defenders raise barricades with banners behind them. Broken gates stay open until the siege ends.
- An army without siege engines cannot take a walled city: the walls do their job.
- A landing party from a ship sets up a small beachhead.

**Other**
- News messages: walls finished, an enemy fleet approaching, siege started and ended.
- Tower repair, cheap wooden and stone towers.
- About a hundred settings in one plain text file.

## Download

Easiest: subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3827596767) (requires NeoModLoader), then switch the mod on in the NML mod list. Also on [GameBanana](https://gamebanana.com/mods/732259) and CurseForge.

Manual install: get the latest ZIP from the [Releases page](https://github.com/Rough-Sheriff42/Walls-Sieges-for-WorldBox/releases/latest) (file `Walls_and_Sieges_v1.3.0.zip`, not the "Source code" archives).

## Installation

1. Install [NeoModLoader](https://github.com/WorldBoxOpenMods/ModLoader) and enable experimental mode in the game settings.
2. Download the release ZIP, unpack it and copy the `Walls_and_Sieges` folder into the NeoModLoader `mods` folder (on Steam: `WorldBox_Data/StreamingAssets/mods`).
3. Fully restart the game. The mod is compiled at startup, which takes a few seconds.

## Settings

**In the game.** Open the mod list, select Walls & Sieges and open Mod Configure. It has the main options: siege camps, coastal towers, gates, moats, wall decay, wall messages, siege sounds and volume, the share of shots at walls, gate strength and gun reload. Changes apply after a full restart of the game.

**In a file.** About a hundred more options are in `Mymod_settings.txt`, created on the first launch in the game's data folder (on macOS: `find ~/Library -name Mymod_settings.txt`). Format: `key = value`, each setting has a short description. Delete the file to restore the defaults.

Most useful file settings:

| Key | Default | What it does |
|---|---|---|
| `gates_enabled` | true | gates close when a threat approaches |
| `camp_enabled` | true | siege camps |
| `siege_hold_seconds` | 300 | how long an army besieges a locked city |
| `siege_gun_min_army` | 46 | army size for the first siege engine |
| `siege_gun2_min_army` / `siege_gun3_min_army` | 90 / 140 | for the second and third engine (0 disables) |
| `siege_wall_share` | 30 | percent of shots aimed at the walls instead of the gates |
| `gate_hp` | 900 | gate durability against siege engines |
| `catapult_blast` / `trebuchet_blast` | 160 / 320 | explosion damage to buildings and warriors |
| `gate_army_range` | 30 | how many tiles before the wall the gates close for an approaching army |
| `wall_shapes` | true | varied wall shapes |
| `log_verbose` | false | detailed log for debugging |

## Known quirks

- In a crowded world with many wars at once, gates sometimes close incompletely. The vanilla game is too varied to predict everything.
- Siege guns are built only at land camps. A landing from the sea produces a beachhead, which can get a ram but no guns.
- Civilians may keep hitting buildings of a siege camp; a fix is planned for 1.4.
- The mod changes tiles and buildings in the world. Back up important saves before trying it.

## Compatibility

Tested on WorldBox 0.51.2 (macOS, Apple Silicon) with NeoModLoader. Other mods that change city building or armies may conflict.

## Reporting problems

Open an [issue](https://github.com/Rough-Sheriff42/Walls-Sieges-for-WorldBox/issues) and attach the game log (`Player.log`) and, if possible, a screenshot. Turning on `log_verbose = true` in `Mymod_settings.txt` helps.

## License

All rights reserved. You may play the mod and make videos, streams and reviews about it (credit and a link are appreciated). You may not copy, modify or re-upload the code or assets. See [LICENSE](LICENSE).
