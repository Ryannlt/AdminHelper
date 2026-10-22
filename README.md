# AdminHelper

[![Latest release](https://img.shields.io/github/v/release/Ryannlt/AdminHelper?label=latest&style=flat-square)](https://github.com/Ryannlt/AdminHelper/releases/latest)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/Ryannlt/AdminHelper/blob/main/LICENSE)

AdminHelper is a [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Holdfast: Nations At War**. It scores
every player on their distance from their own side and marks players who stay out of formation (rambos). It also
adds admin tools to the P menu.

Anything that shows another player requires an `rc login` on the server, except your own team on the Minimap
tab. See [Admin only](#admin-only).

## Install

### Mod manager

Install AdminHelper with [r2modman](https://r2modman.com/) or Thunderstore Mod Manager and start the game from the
manager. BepInEx and [RyLib](https://github.com/Ryannlt/RyLib) are installed with it.

### Manual

1. Install
   [BepInExPack_Holdfast](https://thunderstore.io/c/holdfast-nations-at-war/p/HoldfastModding/BepInExPack_Holdfast/)
   into the game folder and start the game once.
2. Download `AdminHelper.dll` from the [latest release](https://github.com/Ryannlt/AdminHelper/releases/latest)
   and `RyLib.dll` from the [RyLib release](https://github.com/Ryannlt/RyLib/releases/latest).
3. Copy them to:

```
Steam\steamapps\common\Holdfast Nations At War\BepInEx\plugins\AdminHelper\AdminHelper.dll
Steam\steamapps\common\Holdfast Nations At War\BepInEx\plugins\RyLib\RyLib.dll
```

### Checking the install

`BepInEx\LogOutput.log` contains these lines when both mods load:

```
[Info   :   BepInEx] Loading [RyLib 1.2.0]
[Info   :   BepInEx] Loading [AdminHelper 1.4.0]
```

If they are missing, see [Troubleshooting](#troubleshooting).

## Rambo overlay

1. Log in to the server console with `rc login <password>`.
2. Open the P menu and click **Rambo UI** under the players list, or press **F6**.

**Rambo UI** cycles through three modes:

| Mode | The overlay shows |
| --- | --- |
| Always On | While playing, spectating and in free roam. |
| Freeflight | In free roam only. |
| Off | Never. Scoring continues in the background. |

F6 turns the overlay off, and pressing it again restores the previous mode. The mode is saved between launches.
`ToggleKey` changes the key.

A player is marked once their ISO passes `RingThreshold`. The mark is a glow visible through walls and terrain,
a ring on the ground around them, and a label with their name, state and scores. A rider's ring is wider and
circles their horse. The mark is removed when their ISO drops
below the threshold.

**Compact labels** in **Mods > AdminHelper > Rambo UI**, or `CompactLabels`, shortens each label to the player's
name in their state color.

### States

| State | Condition |
| --- | --- |
| ISOLATED | ISO is above `RingThreshold`. |
| RAMBO | ISO has stayed above `RamboThreshold` for `RamboHoldSeconds`. The label shows the time in seconds. |
| MELEE GRACE | The player is isolated but joined a melee before being flagged, so they are not flagged while it lasts. See [Melee grace](#melee-grace). |
| CAV CHARGE | A mounted player is in a melee. See [Cavalry](#cavalry). Shown whatever their ISO. |
| AFK | The player has not moved for `AfkSeconds`. See [AFK players](#afk-players). |

**Tags** in **Mods > AdminHelper** has a switch for each state. A state that is switched off loses its glow,
ring and label, its outline on the Minimap tab, and its place in the spectate order. Kill log name colors stay.
The config file names are `ShowRamboTag`, `ShowIsolatedTag`, `ShowMeleeGraceTag`, `ShowCavChargeTag` and
`ShowAfkTag`.

### Colors

| Color | Condition |
| --- | --- |
| Green to yellow | ISO rising from `RingThreshold` to 100, with a DGR of 0. |
| Orange to red | DGR rising from 1 to 100. |
| Blue | MELEE GRACE. |
| Light purple | CAV CHARGE. |
| Gray | AFK. |

### Spectating marked players

With an `rc login`, **]** spectates the next marked player and **[** the previous one. The order is RAMBO, then
CAV CHARGE, then ISOLATED and MELEE GRACE sorted by DGR, then AFK. While Rambo UI is not Off, the key bar in free
roam and in spectator mode shows a **Spectate Rambos** entry for these keys. `SpectateNextKey` and
`SpectatePrevKey` change them.

## Cavalry

A mounted player is cavalry, whatever their class. A Hussar or Dragoon on foot is scored like any other class.

**Compass range** is the distance at which a friendly drops off the compass. AdminHelper reads it from the game
each round and logs it. `CompassRangeMeters` sets it by hand, and `0` uses the game's value.

### Isolation

A rider is measured to the nearest mounted teammate. Isolation is 0 inside compass range and reaches full at
compass range times `CavalryFarMultiplier`. A rider with no other mounted teammate is not scored.

### Multicharge

When a mounted player kills an enemy, AdminHelper counts the riders chained to them at that moment: every mounted
teammate within compass range of the killer or of another rider in the chain. With fewer than
`MultichargeMinRiders` in the chain, the killer included, the kill log shows a red horse icon between the names.
`MultichargeCheck` turns it off.

### Cav Charge

A mounted player enters a melee the same way as anyone else. Mounted players in a melee spread it to other
mounted players within `CavalryChainMeters`. A mounted player stays in the melee while any enemy is within
`CavalryEngageMeters`, so riders circling an enemy remain in the fight. Every mounted player in the melee is
marked CAV CHARGE in light purple.

`CavalryRules` turns all cavalry rules off. Mounted players are then not scored.

## P menu

### Settings

The P menu has AdminHelper's settings under **Mods > AdminHelper**, with or without an `rc login`. Each section
holds switches, sliders, dropdowns and text boxes for the same values as the config file:

| Section | Settings |
| --- | --- |
| Rambo UI | Compact labels, labels, rings, glow, mode, most labels, toggle key |
| Tags | Rambo, isolated, melee grace, cav charge, AFK |
| Detection | Marked at, rambo at, hold seconds, isolation distances, rise seconds, recover multiplier, exempt classes |
| Danger | Enemy radius, enemy crowd |
| Formation | Suppression and the line and cluster checks |
| Melee | Melee grace, chain distance, melee window |
| Cavalry | Cavalry rules, multicharge check, compass range, isolation multiplier, charge chain, engaged distance, multicharge riders |
| AFK | AFK marks, seconds, move distance |
| Flags | The four flag settings |
| Players Tab | Search parts, row buttons, Rules tab |
| Kill Log | Kill log marks, teamkill filter, color key, colors without Rambo UI |
| Minimap | Minimap tab, ships, hide ship crews, icon size |
| Spectate | Previous and next keys |
| Glow | Glow style from RyLib |
| General | Enabled, reset to defaults, scoring per second |

Hovering over a setting for a moment shows what it does. `RequireAdminLogin` is only in the config file.

**Reset to defaults** in the General section puts every AdminHelper setting back to its default, along with the
glow and map settings from RyLib shown in the tab. It needs a second click within four seconds.

### Rules tab

The game's Rules tab is hidden. **Rules tab** in the Players Tab section, or `ShowRulesTab`, shows it.

## Player search

The search box in the P menu players tab accepts classes, factions, sides and regiment tags as well as names and
IDs. It works without an `rc login`.

| Search by | Examples |
| --- | --- |
| Class name or its start | `rifleman`, `rifle`, `surgeon`, `sapper`, `grenadier` |
| Shorthand | `line`, `officer`, `sgt`, `medic`, `flag`, `drummer`, `gunner` |
| Class group | `cavalry` (hussars and dragoons), `skirmishers` (riflemen and light infantry), `rank & file` (line infantry), `infantry`, `artillery`, `support`, `naval`, `sailor`, `officers` |
| Faction | `british`, `french`, `prussian`, `russian`, `italian`, `austrian`, `allied`, `central`, `spanish`, `privateer`, `american`, and country names such as `france` and `britain` |
| Side | `attackers` or `att`, `defenders` or `def` |

Terms combine. `french cav` lists French hussars and dragoons, and `defenders surgeon` lists the defending
side's surgeons. Plurals work for all terms.

Regiment tags and player names are compared with punctuation removed and accents replaced by plain letters.
`[45e]` matches `45e` and `45`, and `7.Fuß` matches `7fus`.

Text that is not a class, faction or side searches names and tags as in the base game.

`ClassFilterEnabled`, `FactionSearchEnabled` and `RegimentSearchEnabled` turn each part off.

## Regiment teleport

An expanded player row in the P menu has two extra admin buttons.

| Button | Action |
| --- | --- |
| **Reg TP** | Teleports the player to the member of their regiment with the same class who is closest to the center of that group. If no one in the regiment has that class, the player goes to the closest friendly player. |
| **Rez + Reg TP** | Revives a dead player, waits for them to spawn, then does the same teleport. The teleport is canceled if they are not alive within four seconds. |

Both buttons use the game's revive request and `rc teleport`. `RowActionsEnabled` removes them.

## Kill log

### Melee mark

Crossed swords between the names mean the victim was in a melee with an enemy player when they died. A kill at
range shows the distance and then the swords.

A player enters a melee when they hit an enemy with a melee attack, have their attack blocked by an enemy, or
block an enemy's attack. Hits and blocks between teammates do not count. Every player within `MeleeChainMeters`
of someone in the melee is also in it, on either side. The melee ends when nobody in it has hit or blocked for
`MeleeWindowSeconds`.

Only kills that happen while you are in the round can be marked.

### Name colors

The killer's and victim's names use the color of their rambo mark at the time of the kill. They show while Rambo
UI is not Off. `KillLogColorsWithoutUi` also shows them while Rambo UI is Off.

### Multicharge mark

A red horse icon between the names marks a mounted kill that broke the multicharge rule. See
[Multicharge](#multicharge).

### Color key

A line under the kill log title shows what each icon and name color means. The swords are **Killed In Melee** and
the red horse is **OUT OF SPACING**. These two always show when their features are on. The state colors, with
orange to red as **Rambo**, show whenever the name colors do. `ColorKey` turns the key off.

### Actions

Clicking a row opens actions for the killer and the victim: Spectate, Go To, Bring, Slay, Revive, Heal, Slap,
Kick, Reg TP and Rez + Reg TP. Slay, Slap and Kick need a second click within four seconds.

### Teamkills

The square toggle next to the search box shows teamkills only and changes the title to `TEAM KILL LOG`. Typing
`tk` in the search box does the same. Suicides do not count as teamkills.

## Minimap

The Minimap tab is on by default. **Minimap tab** under **Mods > AdminHelper > Minimap**, or `MapEnabled`, turns
it off.

The tab shows an overhead image of the play area, with each player drawn as their class icon in their faction's
color and each ship as the icon for its type. The image is taken once per round at 8192 pixels, covers the
terrains that hold the spawns and capture points, and leaves out players, horses, ships and smoke.

- The legend under the map lists the classes each faction can spawn and how many of each are on the field.
- The search box filters the icons by name, class or faction.
- The mouse wheel zooms and dragging pans. **Battle Area** frames the current spawns and capture points, and
  **Whole Map** shows the full image.
- **Icons -** and **Icons +** change the icon size.
- **Both Teams** under them cycles between both teams and each team on its own.
- Clicking a player selects them. The panel has Spectate, Go To, Bring, Teleport To, Slay, Reg TP and
  Rez + Reg TP. Slay needs a second click.
- **Teleport To** moves the selected player and **Teleport Me** moves you. Both wait for a click on the map and
  teleport to that spot. Right click cancels.
- While the rambo overlay is showing, marked players have an outline in their mark color. States switched off
  under **Tags** have no outline.
- With `FlagHighlightEnabled` on, flags are drawn on the map.

### Ships

Ships and boats are drawn in their owner's color. A captured ship changes to its new owner's color, and its
tooltip names the faction it was captured from. A ship flying false colors is drawn in its real owner's color. A
sinking ship is drawn faded.

Players aboard a ship are hidden from the map. Clicking a ship selects it and lists its crew under the buttons.
Clicking a crew member opens their player actions, and **Back to Ship** returns to the ship.

| Button | Effect |
| --- | --- |
| Go To | Moves your free flight camera above the ship. While you are alive, teleports you to the ship's spawn point. |
| Bring Ship | Moves the ship to your camera, or to you while you are alive. |
| Move Ship | Waits for a click on water and moves the ship there. Shows only in free flight. Right click cancels. |
| Remove Ship | Removes the ship and everyone aboard. Needs a second click within four seconds. |

**Ships** and **Hide ship crews** in the Minimap section turn off the ships and the crew hiding.

### Without an rc login

Without an `rc login` the Minimap tab shows only your own team: your teammates, your team's ships and their
crews. Enemy players, enemy ships, enemy boarders, rambo outlines and flags are never drawn, whatever the
settings. There are no action buttons and no team filter.

## Flags

All flag settings are off by default.

| Setting | Effect |
| --- | --- |
| `FlagHighlightEnabled` | Tracks flags. While the overlay is showing, each flag glows in its faction's color. Flags are also drawn on the Minimap tab. Requires an `rc login`. |
| `ShowFlagLabels` | Labels each glowing flag with its faction or its carrier's name. |
| `FlagMinimapMarkers` | Shows flags on the minimap by the game's reveal rules. Your side's flags are blue. Enemy flags are red and only show while their faction is revealed. |
| `FlagMinimapAlways` | Shows every flag on the minimap to admins, whatever the reveal rules. Requires an `rc login`. |

`ShowFlagLabels` and `FlagMinimapMarkers` need `FlagHighlightEnabled`. `FlagMinimapAlways` needs
`FlagMinimapMarkers`.

A carried flag follows its carrier, and a dropped flag is marked where it lies. Tracking covers every flag
carryable in the game, including the custom bearing flag, whichever mod or map places it.

## AFK players

A player who has moved less than `AfkMoveMeters` in `AfkSeconds` is marked AFK in gray. Moving further clears
the mark. `AfkMarkEnabled` turns the check off. **AFK** under **Tags** keeps the check and hides the mark.

## Melee grace

When a player who is not flagged joins a melee, they cannot be flagged until `MeleeWindowSeconds` after the melee
ends, plus `RamboHoldSeconds`. Their label reads MELEE GRACE during this time. Their dwell time keeps counting,
and if they are still isolated when the grace ends they are flagged straight away. A player who is already
flagged when the melee starts gets no grace. `MeleeGrace` turns it off.

## Scores

| Score | Meaning |
| --- | --- |
| ISO | Isolation. Distance from the player's own side, built up over time. |
| DGR | Danger. The part of ISO that counts as a threat, based on nearby enemies. |

Both run from 0 to 100, and DGR is never higher than ISO. A player alone with no enemies nearby has a high ISO
and a DGR of 0. A player at 60 ISO with three enemies close by has a DGR near 60.

### Scoring

Scores update `TickHz` times a second from four inputs.

| Input | Effect |
| --- | --- |
| Distance | Distance from the midpoint of the player's two nearest living teammates. It starts counting at `ClusterNearMeters` and reaches the maximum at `ClusterFarMeters`. |
| Time | The score rises while the player stays out, at a rate set by `RiseSeconds`, and falls `RecoverMultiplier` times faster once they rejoin. |
| Enemies | The nearest enemy's distance and the number of enemies inside `EnemyRadius` turn ISO into DGR. With no enemies in range DGR is 0. `EnemyCrowd` enemies at contact give full DGR, and one enemy at contact gives half. |
| Formation | A player in formation has `FormationSuppression` of the raw score removed. A player counts as in formation inside an officer's or sergeant's placed line, on a line fitted through nearby teammates, or in a tight cluster such as a square. |

Mounted players follow the [cavalry rules](#cavalry). Artillery is never scored. `ExemptClasses` excludes other
classes.

## Admin only

Other players are shown only after the server accepts your `rc login`. Setting `RequireAdminLogin` to `false`
removes this check for testing with bots on your own server. With it off, every player is shown to anyone
running the mod. The Minimap tab ignores this setting and never shows the other team without an `rc login`.

## Settings

The config file is `BepInEx\config\com.ryannlt.adminhelper.cfg`, created on first launch. Changes to the file
apply within a second without a restart.

[ConfigurationManager](https://thunderstore.io/c/holdfast-nations-at-war/p/Ryanlt/ConfigurationManager/) can
edit the settings in game.

| Setting | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | Turns the whole mod on or off, scoring included. |
| `TickHz` | `5` | Scoring updates per second. |
| `RequireAdminLogin` | `true` | Only show other players after an `rc login`. |
| `ClusterNearMeters` | `10` | Distance from the two nearest teammates where isolation starts. |
| `ClusterFarMeters` | `30` | Distance where isolation reaches the maximum. |
| `RiseSeconds` | `6` | Time constant for the score rising. |
| `RecoverMultiplier` | `3` | How much faster the score falls than it rises. |
| `EnemyRadius` | `30` | Distance within which enemies count towards danger. |
| `EnemyCrowd` | `3` | Number of enemies at contact that gives full danger. |
| `FormationSuppression` | `0.9` | Fraction of the raw score removed in formation. |
| `LineRadius` | `10` | Radius searched for formation mates. |
| `LineMinMates` | `2` | Mates needed before a line is fitted. |
| `LineMaxMates` | `6` | Most mates used for the line fit. |
| `LineResidual` | `2` | Distance either side of the fitted line still counted as on it, in meters. |
| `ClusterMinMates` | `3` | Mates within `ClusterFormationRadius` that count as a tight cluster. |
| `ClusterFormationRadius` | `8` | Radius for the tight cluster check. |
| `RingThreshold` | `40` | ISO at which a player is marked. |
| `RamboThreshold` | `75` | ISO at which the dwell timer starts. |
| `RamboHoldSeconds` | `5` | Seconds above `RamboThreshold` before a player is flagged RAMBO. |
| `ExemptClasses` | empty | Comma separated class names that are never flagged, for example `Surgeon,Sapper`. |
| `MeleeGrace` | `true` | Turns on [melee grace](#melee-grace). |
| `FlagHighlightEnabled` | `false` | Tracks flags in the world and on the Minimap tab. |
| `FlagMinimapMarkers` | `false` | Shows flags on the minimap. |
| `ShowFlagLabels` | `false` | Labels each flag with its faction or carrier. |
| `FlagMinimapAlways` | `false` | Shows every flag on the minimap to admins. |
| `AfkMarkEnabled` | `true` | Marks players who have stopped moving. |
| `AfkSeconds` | `90` | Seconds without moving before a player is AFK. |
| `AfkMoveMeters` | `0.75` | Distance a player has to move to reset the AFK timer. |
| `RamboUi` | `Off` | When the overlay shows: `On`, `FreeflightOnly` or `Off`. |
| `ToggleKey` | `F6` | Key that turns the overlay off and back on. Any `KeyCode` name. |
| `CompactLabels` | `false` | Labels show only the player's name. |
| `ShowRamboTag` | `true` | Shows players marked RAMBO. |
| `ShowIsolatedTag` | `true` | Shows players marked ISOLATED. |
| `ShowMeleeGraceTag` | `true` | Shows players marked MELEE GRACE. |
| `ShowCavChargeTag` | `true` | Shows players marked CAV CHARGE. |
| `ShowAfkTag` | `true` | Shows players marked AFK. |
| `ShowRings` | `true` | Ring under each marked player. |
| `ShowLabels` | `true` | Label above each marked player. |
| `ShowGlow` | `true` | Glow on marked players and flags, visible through walls. |
| `MaxLabels` | `12` | Most labels shown at once, worst first. |
| `SpectateNextKey` | `RightBracket` | Spectates the next marked player. |
| `SpectatePrevKey` | `LeftBracket` | Spectates the previous marked player. |
| `ClassFilterEnabled` | `true` | Class search in the players tab. |
| `RegimentSearchEnabled` | `true` | Regiment tag search in the players tab. |
| `FactionSearchEnabled` | `true` | Faction and side search in the players tab. |
| `RowActionsEnabled` | `true` | Reg TP and Rez + Reg TP buttons on player rows. |
| `ShowRulesTab` | `false` | Shows the game's Rules tab. |
| `MeleeMarkerEnabled` | `true` | Crossed swords on melee kills in the kill log. |
| `TeamkillFilterEnabled` | `true` | Teamkill toggle and `tk` search in the kill log. |
| `MeleeChainMeters` | `10` | Distance from a melee within which a player counts as in it. |
| `MeleeWindowSeconds` | `10` | Seconds without a hit or block before a melee ends. |
| `ColorKey` | `true` | Color key under the kill log title. |
| `KillLogColorsWithoutUi` | `false` | Kill log name colors and their key entries while Rambo UI is Off. |
| `CavalryRules` | `true` | Scores mounted players with the cavalry rules. |
| `CompassRangeMeters` | `0` | Compass range in meters. `0` uses the game's value. |
| `CavalryFarMultiplier` | `2` | Compass range times this is where a rider's isolation is full. |
| `CavalryChainMeters` | `40` | Distance within which a melee spreads between mounted players. |
| `CavalryEngageMeters` | `30` | A mounted player stays in a melee while an enemy is this close. |
| `MultichargeCheck` | `true` | Marks mounted kills that break the multicharge rule. |
| `MultichargeMinRiders` | `3` | Riders chained at compass range, the killer included, for a valid mounted kill. |
| `MapEnabled` | `true` | Shows the Minimap tab in the P menu. |

Glow style, map icon size, ships and crew hiding are set in
[RyLib's config](https://github.com/Ryannlt/RyLib#settings), `com.ryannlt.rylib.cfg`.

## Tuning

Each marked player's label shows their ISO and DGR. Compare these with the table below to adjust the thresholds.

With the default settings, ISO settles at these values:

| Distance from the two nearest teammates | ISO |
| --- | --- |
| Under 10 m | 0 |
| 15 m | 25 |
| 18 m | 40, the player is marked |
| 20 m | 50 |
| 25 m | 75, the dwell timer starts |
| 30 m or more | 100 |

A player is marked at about 18 m. A RAMBO flag needs more than 25 m held for about 8 seconds, plus
`RamboHoldSeconds`.

| Problem | Change |
| --- | --- |
| Too many false positives | Raise `ClusterFarMeters` or `RiseSeconds`. |
| Rambos are not flagged | Lower `ClusterFarMeters`. For rambos at around 18 m, try 22. |
| Flagging is too slow | Lower `RiseSeconds` or `RamboHoldSeconds`. |

Dwell time falls at `RecoverMultiplier` times the rate it builds, and a short return to formation does not reset
it.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| No log file, or nothing in the log | BepInEx is not running. Check that `winhttp.dll` and the `BepInEx` folder are next to `Holdfast NaW.exe`. With a mod manager, start the game from the manager. |
| AdminHelper does not load and the log mentions RyLib | Install or update RyLib. |
| The mod loads but nothing is drawn | Run `rc login <password>`. Check `Enabled`, and that Rambo UI is not Off. |
| Nothing is drawn and the log has no `scene loaded` line | Open an issue with `BepInEx\LogOutput.log` attached. |
| The map shows a grid instead of an image | Open an issue with `BepInEx\LogOutput.log` attached. |
| The game crashes at launch | Remove the mod and check the game starts. Then open an issue with `BepInEx\LogOutput.log` attached. |

## Building

Requirements:

- The game.
- BepInEx installed in an r2modman profile.
- [RyLib](https://github.com/Ryannlt/RyLib) cloned next to this repo and built, so `..\RyLib\RyLib.dll` exists.
- A [.NET SDK](https://dotnet.microsoft.com/download), for the compiler.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

`build.ps1` compiles every `.cs` file in the repo and copies `AdminHelper.dll` to
`BepInEx\plugins\AdminHelper\` in the r2modman profile. Restart the game to load it.

| Option | Effect |
| --- | --- |
| `-NoDeploy` | Build without copying. Use this while the game is running. |
| `-ProfileName <name>` | Use a profile other than `Dev`. |

AdminHelper compiles against `..\RyLib\RyLib.dll`. When changing both, build RyLib first.

`package.ps1` builds the mod and writes a Thunderstore zip to `Package\`.

Both scripts expect the game in `C:\Program Files (x86)\Steam\steamapps\common\Holdfast Nations At War`. For
another location, change `$GameDir` at the top of the script.

The `.csproj` is for IDE support. `build.ps1` calls `csc` directly.

### Source layout

| Folder | Contents |
| --- | --- |
| `Core` | Plugin entry point, driver, settings, logging, overlay hotkey and rambo spectating. |
| `Game` | Game reads: player snapshots, faction and round lookups, text matching. |
| `Tracking` | Isolation, formation, melee, AFK and flag tracking. |
| `Overlay` | World marks, minimap flags and the map layer, drawn through RyLib. |
| `PMenu` | Mods tab settings, player search, kill log, color key and row actions. |

### Releases

There is no CI build. Building needs `Assembly-CSharp.dll` from a game install, which is not in this repo.
Releases are built locally.

## Compatibility

Built against Holdfast on Unity 2022.3.62f2 with BepInEx 5.4.23.5 (Mono). A game update that renames or
restructures the client player managers stops the mod from working. Please open an issue if that happens.

## License

[MIT](https://github.com/Ryannlt/AdminHelper/blob/main/LICENSE).
