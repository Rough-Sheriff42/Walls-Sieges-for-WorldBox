# Changelog

## 1.3.0
**New**
- Manual gun control (2D and 3D): K to take a siege gun, L to fire, a dotted arc shows the shot; a hint over the gun in 2D.
- Gate health bar: point at a closed gate.
- Siege scroll in 3D: a chronicle of a siege you witnessed (J).
- Optional Worldfall 3D compatibility: 3D models for siege guns, ram, camp, towers, flags, palisade and rubble; first-person gunner with a shell camera; ladders and hatches on watch towers; enterable siege tents; burning gates; siege events in the news feed and townsfolk talk; construction stages for the cheap watch tower.

**Improved**
- Walls no longer take away a city's borders.
- Houses are no longer built right in front of gates.
- Gates give in 15% faster to a crowd; a fallen gate's fire goes out and its rubble is passable.
- Siege guns turn to face their target in 2D.
- Interface hints follow the game's language (English or Russian).

**Fixed**
- Very long game loading when Worldfall is not installed.

**Known issues**
- Civilians may keep hitting buildings of a siege camp (planned for 1.4).

## 1.2.0
**New**
- Culture traits: new trait group "Fortifications" with eight traits (Wall Builders, Earthworks, Coastal Defence, Double Wall, Mortar Craft, Paving, Ram Crews, Reinforced Gates). Walls, moats, coastal towers and paving now depend on culture. Option "Culture traits required" in Mod Configure turns this off. Old saves get their traits once.
- Mortar: heavy siege gun (reload 12 s, big blast, deep smouldering crater, 20% longer range than other guns). Dwarves start with the trait; others can learn it rarely.
- Siege ram appears 10 s after the camp (50%, second try 10 s later; 70% at sea landings). Ram Crews trait: always arrives and has 20% more health. Reinforced Gates trait: gates 50% tougher.
- Double Wall trait: a second layer on the inner wall (rare).
- Alert levels: patrol, marching army, siege; a flag in the city centre shows the level. Civilians take shelter in the centre, defenders hold the gates and breaches.
- Languages: English, Russian, Spanish, Portuguese (Brazil), Chinese (Simplified and Traditional).

**Improved**
- Armies go for fortified cities first and set up camps even at closed gates; the mortar does not fire at close targets.
- Sieges are longer and more stable: armies stay at the walls while the guns work, guns pick targets near the gates, the ram goes for the gates.
- New sprites in the game's style: siege guns, ram, camp, trait icons, cannon animations.
- New sounds for shots, impacts, collapsing walls and gates, the ram creaks while moving.

**Fixed**
- Conflicts when building walls near borders and other buildings.
- Units getting stuck at walls, in camps and on the way to the target.
- Crashes: error in the enemy target check, rendering error after a captured or emptied city; buildings of a captured city pass to the new owner.

## 1.1.0
- Walls are no longer built on mountains (walls, towers, moats and roads).
- Kingdoms build walls on unclaimed land, and follow a neighbour's border when they run into it.
- When borders shift and walls end up on another kingdom's land, they are removed and the wall is re-planned along the new border (new option "Re-plan walls at borders", on by default).
- Optional "Border buffer": keep 2 tiles of space from other kingdoms when building on unclaimed land (off by default).
- Fixed walls turning a city's land neutral: building a wall in an empty zone no longer makes the game take the zone away, and zones cut off by walls stay with the city.
- The outer ring of a capital is rebuilt right away after a re-plan, instead of leaving gaps.
- Fixed a missing wall side on small cities whose neighbour's border is close to the centre.
- Units no longer get stuck at unfinished walls when borders flicker.
- Extra diagnostics in Player.log: re-plans, lost zones.

## 1.0.0
- First public release.
- Walls, gates, moats, coastal towers for humans, elves, orcs and dwarves.
- Siege camps with up to three siege engines, bombardment of gates and walls, barricades with banners.
- In-game Mod Configure window with the main options; about a hundred more in `Mymod_settings.txt`.
