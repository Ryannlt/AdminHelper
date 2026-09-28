# AdminHelper

[![Latest release](https://img.shields.io/github/v/release/Ryannlt/AdminHelper?label=latest&style=flat-square)](https://github.com/Ryannlt/AdminHelper/releases/latest)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/Ryannlt/AdminHelper/blob/main/LICENSE)

AdminHelper is a [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Holdfast: Nations At War**. It scores
every player on their distance from their own side and marks players who stay out of formation (rambos). It also
adds admin tools to the P menu.

Anything that shows another player requires an `rc login` on the server. See [Admin only](#admin-only).

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
[Info   :   BepInEx] Loading [RyLib 1.0.0]
[Info   :   BepInEx] Loading [AdminHelper 1.2.0]
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
a ring at their feet, and a label with their name, state and scores. The mark is removed when their ISO drops
below the threshold.

### States

| State | Condition |
| --- | --- |
| ISOLATED | ISO is above `RingThreshold`. |
| RAMBO | ISO has stayed above `RamboThreshold` for `RamboHoldSeconds`. The label shows the time in seconds. |
| FIGHT | The player joined a melee before being flagged. See [Melee grace](#melee-grace). |
| AFK | The player has not moved for `AfkSeconds`. See [AFK players](#afk-players). |

### Colours

| Colour | Condition |
| --- | --- |
| Green to yellow | ISO rising from `RingThreshold` to 100, with a DGR of 0. |
| Orange to red | DGR rising from 1 to 100. |
| Blue | FIGHT. |
| Grey | AFK. |

### Spectating marked players

With an `rc login`, **]** spectates the next marked player and **[** the previous one. The order is RAMBO, then
ISOLATED and FIGHT sorted by DGR, then AFK. While Rambo UI is not Off, the key bar in free roam and in spectator
mode shows a **Spectate Rambos** entry for these keys. `SpectateNextKey` and `SpectatePrevKey` change them.

## P menu settings

The P menu has AdminHelper's settings under **Mods > AdminHelper**, in the sections Rambo Detection, Flags,
Players Tab, Kill Log and Map. They change the same values as the config file.

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
| **Reg TP** | Teleports the player to the member of their regiment with the same class who is closest to the centre of that group. If no one in the regiment has that class, the player goes to the closest friendly player. |
| **Rez + Reg TP** | Revives a dead player, waits for them to spawn, then does the same teleport. The teleport is cancelled if they are not alive within four seconds. |

Both buttons use the game's revive request and `rc teleport`. `RowActionsEnabled` removes them.

## Kill log

### Melee mark

Crossed swords between the names mean the victim was in a melee with an enemy player when they died. A line
under the kill log title explains the icon. A kill at range shows the distance and then the swords.

A player enters a melee when they hit an enemy with a melee attack, have their attack blocked by an enemy, or
block an enemy's attack. Hits and blocks between teammates do not count. Every player within `MeleeChainMetres`
of someone in the melee is also in it, on either side. The melee ends when nobody in it has hit or blocked for
`MeleeWindowSeconds`.

Only kills that happen while you are in the round can be marked.

### Name colours

While Rambo UI is not Off, the killer's and victim's names use the colour of their rambo mark at the time of the
kill.

### Actions

Clicking a row opens actions for the killer and the victim: Spectate, Go To, Bring, Slay, Revive, Heal, Slap,
Kick, Reg TP and Rez + Reg TP. Slay, Slap and Kick need a second click within four seconds.

### Teamkills

The square toggle next to the search box shows teamkills only and changes the title to `TEAM KILL LOG`. Typing
`tk` in the search box does the same. Suicides do not count as teamkills.

## Map

The Map tab is off by default. Turn it on with **Map tab** under **Mods > AdminHelper > Map**, or with
`MapEnabled`.

The tab shows an overhead image of the current map, with each player drawn as their class icon in their
faction's colour.

- The legend under the map lists the classes each faction can spawn and how many of each are on the field.
- The search box filters the icons by name, class or faction.
- The mouse wheel zooms and dragging pans. **Battle Area** frames the spawns and **Whole Map** shows the full map.
- **Icons -** and **Icons +** change the icon size.
- Clicking a player selects them. The panel has Spectate, Go To, Bring, Teleport To, Slay, Reg TP and
  Rez + Reg TP. Slay needs a second click.
- **Teleport To** moves the selected player and **Teleport Me** moves you. Both wait for a click on the map and
  teleport to that spot. Right click cancels.
- While the rambo overlay is showing, marked players have an outline in their mark colour.
- With `FlagHighlightEnabled` on, flags are drawn on the map.

## Flags

`FlagHighlightEnabled` turns on flag tracking. It is off by default.

While the overlay is showing, each flag glows in its faction's colour and has a label with the flag's faction or
its carrier's name. A carried flag follows its carrier, and a dropped flag is marked where it lies.

On the minimap, flags follow the game's reveal rules. Your side's flags are blue. Enemy flags are red and only
show while their faction is revealed. `FlagMinimapAlways` shows every flag to admins.

Tracking covers every flag carryable in the game, including the custom bearing flag, whichever mod or map places
it. It requires an `rc login`.

`FlagMinimapMarkers` turns off the minimap markers and `ShowFlagLabels` turns off the labels.

## AFK players

A player who has moved less than `AfkMoveMetres` in `AfkSeconds` is marked AFK in grey. Moving further clears
the mark. `AfkMarkEnabled` turns it off.

## Melee grace

When a player who is not flagged joins a melee, they cannot be flagged until `MeleeWindowSeconds` after the melee
ends, plus `RamboHoldSeconds`. Their label reads FIGHT during this time. Their dwell time keeps counting, and if
they are still isolated when the grace ends they are flagged straight away. A player who is already flagged when
the melee starts gets no grace. `MeleeGrace` turns it off.

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
| Distance | Distance from the midpoint of the player's two nearest living teammates. It starts counting at `ClusterNearMetres` and reaches the maximum at `ClusterFarMetres`. |
| Time | The score rises while the player stays out, at a rate set by `RiseSeconds`, and falls `RecoverMultiplier` times faster once they rejoin. |
| Enemies | The nearest enemy's distance and the number of enemies inside `EnemyRadius` turn ISO into DGR. With no enemies in range DGR is 0. `EnemyCrowd` enemies at contact give full DGR, and one enemy at contact gives half. |
| Formation | A player in formation has `FormationSuppression` of the raw score removed. A player counts as in formation inside an officer's or sergeant's placed line, on a line fitted through nearby teammates, or in a tight cluster such as a square. |

Cavalry is only scored with `ScoreCavalry` on. Artillery is never scored. `ExemptClasses` excludes other classes.

## Admin only

Other players are shown only after the server accepts your `rc login`. Setting `RequireAdminLogin` to `false`
removes this check for testing with bots on your own server. With it off, every player is shown to anyone
running the mod.

## Settings

The config file is `BepInEx\config\com.ryannlt.adminhelper.cfg`, created on first launch. Changes to the file
apply within a second without a restart.

[ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) can edit the settings in game.
It needs `HideManagerGameObject = true` under `[Chainloader]` in `BepInEx\config\BepInEx.cfg`.

| Setting | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | Turns the whole mod on or off, scoring included. |
| `TickHz` | `5` | Scoring updates per second. |
| `RequireAdminLogin` | `true` | Only show other players after an `rc login`. |
| `ClusterNearMetres` | `10` | Distance from the two nearest teammates where isolation starts. |
| `ClusterFarMetres` | `30` | Distance where isolation reaches the maximum. |
| `RiseSeconds` | `6` | Time constant for the score rising. |
| `RecoverMultiplier` | `3` | How much faster the score falls than it rises. |
| `EnemyRadius` | `30` | Distance within which enemies count towards danger. |
| `EnemyCrowd` | `3` | Number of enemies at contact that gives full danger. |
| `FormationSuppression` | `0.9` | Fraction of the raw score removed in formation. |
| `LineRadius` | `10` | Radius searched for formation mates. |
| `LineMinMates` | `2` | Mates needed before a line is fitted. |
| `LineMaxMates` | `6` | Most mates used for the line fit. |
| `LineResidual` | `2` | Distance either side of the fitted line still counted as on it, in metres. |
| `ClusterMinMates` | `3` | Mates within `ClusterFormationRadius` that count as a tight cluster. |
| `ClusterFormationRadius` | `8` | Radius for the tight cluster check. |
| `RingThreshold` | `40` | ISO at which a player is marked. |
| `RamboThreshold` | `75` | ISO at which the dwell timer starts. |
| `RamboHoldSeconds` | `5` | Seconds above `RamboThreshold` before a player is flagged RAMBO. |
| `ScoreCavalry` | `false` | Score cavalry. |
| `ExemptClasses` | empty | Comma separated class names that are never flagged, for example `Surgeon,Sapper`. |
| `MeleeGrace` | `true` | Turns on [melee grace](#melee-grace). |
| `FlagHighlightEnabled` | `false` | Tracks flags in the world, on the minimap and on the map. |
| `FlagMinimapMarkers` | `true` | Shows flags on the minimap. |
| `ShowFlagLabels` | `true` | Labels each flag with its faction or carrier. |
| `FlagMinimapAlways` | `true` | Shows every flag on the minimap to admins. |
| `AfkMarkEnabled` | `true` | Marks players who have stopped moving. |
| `AfkSeconds` | `90` | Seconds without moving before a player is AFK. |
| `AfkMoveMetres` | `0.75` | Distance a player has to move to reset the AFK timer. |
| `RamboUi` | `Off` | When the overlay shows: `On`, `FreeflightOnly` or `Off`. |
| `ToggleKey` | `F6` | Key that turns the overlay off and back on. Any `KeyCode` name. |
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
| `MeleeMarkerEnabled` | `true` | Crossed swords on melee kills in the kill log. |
| `TeamkillFilterEnabled` | `true` | Teamkill toggle and `tk` search in the kill log. |
| `MeleeChainMetres` | `10` | Distance from a melee within which a player counts as in it. |
| `MeleeWindowSeconds` | `10` | Seconds without a hit or block before a melee ends. |
| `MapEnabled` | `false` | Shows the Map tab in the P menu. |

Glow style and map icon size are set in [RyLib's config](https://github.com/Ryannlt/RyLib#settings),
`com.ryannlt.rylib.cfg`.

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
| Too many false positives | Raise `ClusterFarMetres` or `RiseSeconds`. |
| Rambos are not flagged | Lower `ClusterFarMetres`. For rambos at around 18 m, try 22. |
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
- BepInEx and RyLib installed in an r2modman profile.
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

AdminHelper compiles against the RyLib in the profile. When changing both, build and deploy RyLib first.

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
| `PMenu` | Mods tab settings, player search, kill log and row actions. |

### Releases

There is no CI build. Building needs `Assembly-CSharp.dll` from a game install, which is not in this repo.
Releases are built locally.

## Compatibility

Built against Holdfast on Unity 2022.3.62f2 with BepInEx 5.4.23.5 (Mono). A game update that renames or
restructures the client player managers stops the mod from working. Please open an issue if that happens.

## Licence

[MIT](https://github.com/Ryannlt/AdminHelper/blob/main/LICENSE).
