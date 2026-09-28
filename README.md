# AdminHelper

[![Latest release](https://img.shields.io/github/v/release/Ryannlt/AdminHelper?label=latest&style=flat-square)](https://github.com/Ryannlt/AdminHelper/releases/latest)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/Ryannlt/AdminHelper/blob/main/LICENSE)

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Holdfast: Nations At War** that finds "rambos":
players who have left their formation and are off fighting on their own. It scores every player continuously
and marks the ones who stay out, so an admin does not have to eyeball a 150 player field.

**It only works for admins.** Nothing about another player is drawn until the server itself has authenticated
your `rc login`. See [Admin only](#admin-only).

## Install

**With a mod manager.** Install through [r2modman](https://r2modman.com/) or Thunderstore Mod Manager and launch
the game from the manager. BepInEx and [RyLib](https://github.com/Ryannlt/RyLib) are pulled in as dependencies,
so there is nothing else to set up.

**By hand.** Install
[BepInExPack_Holdfast](https://thunderstore.io/c/holdfast-nations-at-war/p/HoldfastModding/BepInExPack_Holdfast/)
into the game folder and run the game once so it creates its folders. Then download `AdminHelper.dll` from the
[latest release](https://github.com/Ryannlt/AdminHelper/releases/latest), and `RyLib.dll` from
[RyLib's](https://github.com/Ryannlt/RyLib/releases/latest), and put them here:

```
Steam\steamapps\common\Holdfast Nations At War\BepInEx\plugins\AdminHelper\AdminHelper.dll
Steam\steamapps\common\Holdfast Nations At War\BepInEx\plugins\RyLib\RyLib.dll
```

### Did it work?

Open `BepInEx\LogOutput.log` and look for:

```
[Info   :   BepInEx] Loading [RyLib 1.0.0]
[Info   :   BepInEx] Loading [AdminHelper 1.2.0]
```

If they are not there, the mod was not loaded. See [Troubleshooting](#troubleshooting).

## Using it

Log into the server console as normal:

```
rc login <password>
```

Then turn on the rambo overlay with the **Rambo UI** button under the players list in the P menu, or press
**F6**. The button steps through three modes, like the game's own Admin Raygun:

- **Always On.** The overlay shows whether you are playing, spectating or in free roam.
- **Freeflight.** It shows only while you are in free roam.
- **Off.** Nothing is drawn. The scorer keeps running, so a player already flagged is still flagged when you
  turn it back on.

F6 hides the overlay and brings it back in the mode it was in. The mode is remembered between launches. Change
the key with `ToggleKey`.

With the overlay up, anyone drifting glows through walls and terrain, gets a ring at their feet and a floating
label with their name, state and scores. A player is marked as soon as ISO passes `RingThreshold`, and the mark
clears the moment they come back, because it follows the live score rather than a timer.

Four states, and a colour that says how bad it is:

- **ISOLATED.** Out of position right now. Green when they have just crossed the threshold, turning yellow as
  ISO climbs to 100.
- **RAMBO.** Has held above `RamboThreshold` for `RamboHoldSeconds`, and the label counts the seconds.
- **FIGHT**, blue. In a melee they were not already flagged for, so they are held back from flagging while it
  lasts. See [Melee grace](#melee-grace).
- **AFK**, grey. Has not moved for a while. See [Players standing still](#players-standing-still).

Isolation alone never goes past yellow. Any danger - enemies close by - turns the mark orange, deepening to red
as DGR reaches 100. So a red player is both out on their own and in among the enemy, and is the one to look at
first.

### Spectating rambos

Press **]** to spectate the worst watched player, then **]** and **[** to step through the rest. The list runs
worst first: RAMBO, then ISOLATED and FIGHT by danger, then AFK. While the overlay is on, the game's own key bar
in free roam and while spectating shows a **Spectate Rambos** entry as a reminder. Change the keys with
`SpectateNextKey` and `SpectatePrevKey`.

### Settings in the P menu

Every switch lives in the P menu under **Mods > AdminHelper**, grouped into Rambo Detection, Flags, Players Tab,
Kill Log and Map. They are the same settings as the config file, so a change in either shows in the other.

### Finding players by class, regiment or side

The game's P menu already lists round players with a class icon, but its search box only matches names and IDs.
This mod widens it, so typing a class, a regiment or a faction narrows the list and the counter above shows how
many.

**By class:**

- **A class name.** `rifleman`, `surgeon`, `sapper`, `grenadier`. A prefix is enough, so `rifle` works.
- **A shorthand.** `line` for line infantry, plus `officer`, `sgt`, `medic`, `flag`, `drummer` and `gunner`.
- **A group.** `cavalry` for hussars and dragoons, `skirmishers` for rifles and light infantry, `rank & file`
  for the line, plus `infantry`, `artillery`, `support`, `naval`, `sailor` and `officers`.

**By faction.** `british`, `french`, `prussian`, `russian`, `italian`, `austrian`, `allied`, `central`,
`spanish`, `privateer`, `american`. Country names work too, so `france` and `britain` find the same players.

**By side.** `attackers` and `defenders` resolve to whichever faction is attacking or defending this round, so
you do not have to remember which is which. `att` and `def` are enough.

**Together.** Two or three of those combine, so `french cav` is French hussars and dragoons, and
`defenders surgeon` is the defending side's surgeons. Plurals are understood everywhere, so `officers` and
`officer` do the same thing.

**By regiment.** Tags are full of brackets, dots and dashes nobody wants to type, so both sides are stripped to
letters and digits before matching, and accents are folded. `[45e]` is found by `45e` or `45`, and `7.Fuß` is
found by `7fus`. Player names are matched the same way, so punctuation stops hiding people there too.

Nothing else changes. A name still searches names, a number still searches IDs, clearing the box brings the full
list back, and every row keeps its normal admin buttons so you can act on whoever you find. Anything the mod
cannot read as a class, faction or side is handed straight back to the name and tag search, so a regiment called
`Light` still finds itself. This part needs no `rc login`, because it only filters a list the game already shows
you.

It lists, it does not judge. Whether a regiment should be taking that class is your call. The three halves turn
off separately with `ClassFilterEnabled`, `FactionSearchEnabled` and `RegimentSearchEnabled`.

### Sending a player back to their regiment

Expanding a player's row as an admin gives the usual actions - Go To, Bring, Heal, Slap, Revive, Slay. This mod
adds two more at the end:

- **Reg TP.** Teleports that player to the middle of their own regiment's players **of the same class**, to the
  one standing nearest the middle of that group, so a rifleman lands among the regiment's riflemen rather than
  on a straggler or in the line. With nobody of that class in their regiment, they go to the closest friendly
  player instead.
- **Rez + Reg TP.** The same thing for a dead player: it asks the server to revive them, waits for them to come
  back, then teleports them. If the revive does not land within four seconds the teleport is dropped rather than
  fired into nothing.

Both use the game's own admin plumbing - the revive request the Revive button sends, and `rc teleport` - so the
server authorises them exactly as it would a command you typed. They live inside the admin block of the row, so
they only exist for a logged in admin. `RowActionsEnabled` removes them.

### Reading the kill log

Teamkills usually earn a revive, unless the victim was in a melee. The admin kill log now says which, so the
call can be made from the log instead of from memory.

**Crossed swords** between the two names mean the victim was in a melee with an enemy player when they died - a
legal melee kill. A line under the kill log's heading says so, for anyone new to it. A kill from range keeps
its distance and gains the swords after it.

A player counts as being in a melee from the moment they are part of one **against the other side**: their melee
attack hits an enemy, their attack is blocked by one, or they block one. Friendly swings are ignored, so
teamkilling a man in your own spawn does not mark him as fighting. It spreads to everyone within
`MeleeChainMetres` of them, whichever side they are on, so standing in a scrum counts even if your own blade
never lands. It ends once nobody in that group has hit or blocked for `MeleeWindowSeconds`.

No swords means no melee was seen, which is not always the same as none happening: the mod can only watch while
you are in the round, so kills from before you joined never have them.

**Coloured names.** While Rambo UI is on, the killer's and victim's names are drawn in the colour their rambo
mark had at the moment of the kill, so a kill by or of a flagged player stands out.

**Actions.** Click a row to open actions for both players: Spectate, Go To, Bring, Slay, Revive, Heal, Slap and
Kick for the killer and the victim side by side, plus Reg TP and Rez + Reg TP. Slay, Slap and Kick ask for a
second click.

**Teamkills only.** A square toggle beside the search box switches the log to teamkills only, and the heading
changes to `TEAM KILL LOG`. Typing `tk` in the kill log search box does the same, which is also the fallback if
the toggle cannot be placed. Suicides are not counted as teamkills.

### The map

A **Map** tab in the P menu shows an overhead picture of the battle with every player drawn by their class icon
in their faction's colour. It ships **off**; turn it on with the **Map tab** switch under Mods > AdminHelper >
Map, or `MapEnabled`.

- **Finding people.** A legend under the map lists the classes each side can spawn, with how many are on the
  field, and the search box filters the icons by name, class or faction. The mouse wheel zooms, dragging pans,
  and **Battle Area** / **Whole Map** switch between the fighting and the full map.
- **Acting on them.** Click a player to select them: Spectate, Go To, Bring, Teleport To, Slay, Reg TP and Rez + Reg TP.
  **Teleport To** and **Teleport Me** wait for you to click a spot on the map, and a right click cancels, so a
  stray click never moves anyone. Slay asks for a second click.
- **Rambos and flags.** While the rambo overlay is up, watched players get an outline in their mark's colour.
  With `FlagHighlightEnabled`, flags are drawn too.
- **Icon size.** **Icons -** and **Icons +** under the map, or `IconSize` in RyLib's config.

### Flags

Finding the flags on a 150 player field is most of admining a CTF event, so with `FlagHighlightEnabled` on and
the overlay up, every flag in the round glows through walls in its faction's colour, with a label saying whose
flag it is or who is carrying it.

This one ships **off**, because it has nothing to mark outside a flag game mode.

Both halves of a flag's life are covered. A **carried** flag is read from what each player has in hand, so it
follows them live. A flag **on the ground** is the map's own pickup object, which is where the flag sits at the
start of a round and where it lands when a carrier dies, so it is marked with the faction it belongs to.

On the minimap, flags follow the game's own rules: blue on your side, red on the enemy's, and the enemy's only
while their faction is revealed. As an admin you can see every flag regardless with `FlagMinimapAlways`, which
is on by default.

Nothing here is tied to one CTF mod. Every flag carryable the game has counts - the faction flags a CTF mod moves
around, and the custom bearing flag - so any mod or map using them is picked up. Like everything else that
reveals another player, it needs an `rc login`. `FlagMinimapMarkers` turns off the minimap half on its own, and
`ShowFlagLabels` the labels.

### Players standing still

A player who has not moved for `AfkSeconds` is drawn grey, with `AFK` and their time in the label. They keep
their mark, so someone parked where they should not be is still visible, just not read as someone worth flying
to. Moving more than `AfkMoveMetres` clears it on the next tick. `AfkMarkEnabled` turns it off.

### Melee grace

The last player standing in a fight their regiment started is not a rambo. So when a player enters a melee
without already being flagged, flagging is held off until `MeleeWindowSeconds` after that melee ends, plus the
usual `RamboHoldSeconds`, and the label reads `FIGHT` instead. Their dwell keeps building underneath, so if they
are still out on their own when the grace lapses they flag at once rather than starting over. A player who
charged in alone was already flagged when the melee started, so they get no grace. `MeleeGrace` turns it off.

### The two numbers

| | Meaning |
| --- | --- |
| **ISO** | Isolation. How far you are from your own side, built up over time. |
| **DGR** | Danger. How much of that isolation is actually a threat, based on how deep into the enemy you are. |

Both run 0 to 100, and DGR never exceeds ISO. A player alone in an empty field scores high ISO and **zero DGR**,
because being lost is not the same as ramboing. The same player at 60 ISO standing in the middle of three
enemies scores close to 60 DGR. Sort your attention by DGR, use ISO to see why.

### How a player is scored

Four signals, recomputed five times a second:

- **Distance from your side.** The midpoint of your two nearest living teammates, and how far you are from it.
  Nothing counts under `ClusterNearMetres`, and it saturates at `ClusterFarMetres`. See
  [Tuning it](#tuning-it) for what those two numbers actually mean in metres.
- **Time.** The score builds while you stay out and falls `RecoverMultiplier` times faster when you rejoin, so
  brief separations do not flag.
- **Enemies.** How close the nearest enemy is and how many are inside `EnemyRadius`. This scales ISO into DGR:
  no enemies nearby means no danger at all, and `EnemyCrowd` enemies at contact means the full score. A lone
  enemy at contact counts half of what a full crowd does.
- **Formation.** Being in formation removes `FormationSuppression` of the raw signal. Three ways to qualify:
  standing inside an officer's or sergeant's placed form line, sitting on a line your neighbours also sit on, or
  being in a tight cluster such as a square or a skirmisher knot. The last two cover regiments who line up
  manually without ever using the order.

Cavalry is not scored by default, since operating apart is its job. Artillery is never scored. Add anything else
you do not want flagged to `ExemptClasses`.

## Admin only

Every part of this mod that reveals another player is behind `rc login`. Without it, the mod draws nothing.

That gate is deliberate. A HUD that marks isolated enemies is a wallhack in everything but intent, so it is tied
to the server's own admin authentication rather than to a setting. `RequireAdminLogin` exists so you can test
against bots on your own server, and turning it off gives you exactly what it sounds like.

## Settings

`BepInEx\config\com.ryannlt.adminhelper.cfg`, written on first run. Read live, so edits apply within a second with
no restart. Entries are grouped into `[General]`, `[Isolation]`, `[Scoring]`, `[Danger]`, `[Formation]`,
`[Flagging]`, `[CTF]`, `[AFK]`, `[Display]`, `[Spectate]`, `[PMenu]`, `[KillLog]` and `[Map]` sections. Most of
them are also switches in the P menu, under Mods > AdminHelper.

| Setting | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. Off stops the scorer as well as the overlay. |
| `TickHz` | `5` | Scoring ticks per second. |
| `RequireAdminLogin` | `true` | Only reveal other players after an `rc login`. |
| `ClusterNearMetres` | `10` | Distance from your two nearest mates at which isolation starts counting. |
| `ClusterFarMetres` | `30` | Distance at which isolation is saturated. |
| `RiseSeconds` | `6` | Time constant for the score climbing. |
| `RecoverMultiplier` | `3` | How much faster it falls than it climbs. |
| `EnemyRadius` | `30` | How close an enemy must be before any isolation counts as danger. |
| `EnemyCrowd` | `3` | Enemies inside the radius that count as being fully inside their formation. |
| `FormationSuppression` | `0.9` | Fraction of the raw signal removed while in formation. |
| `LineRadius` | `10` | Radius searched for formation mates. |
| `LineMinMates` | `2` | Mates needed before the line fit is attempted. |
| `LineMaxMates` | `6` | Most mates fed into the line fit. |
| `LineResidual` | `2` | Metres of spread either side of the line still counted as a line. |
| `ClusterMinMates` | `3` | Mates close by that count as a square or skirmisher knot. |
| `ClusterFormationRadius` | `8` | Radius for the tight cluster test. |
| `RingThreshold` | `40` | ISO at which a mark appears. Follows the live score, so it clears on return. |
| `RamboThreshold` | `75` | ISO at which the dwell timer starts. |
| `RamboHoldSeconds` | `5` | Seconds of dwell above the threshold before flagging. |
| `ScoreCavalry` | `false` | Score cavalry too. |
| `ExemptClasses` | empty | Comma-separated class names never flagged, e.g. `Surgeon,Sapper`. |
| `MeleeGrace` | `true` | Hold off flagging a player whose melee started before they were flagged. |
| `FlagHighlightEnabled` | `false` | Track flags: glow and label them, and mark them on the minimap and the map. |
| `FlagMinimapMarkers` | `true` | The minimap half of that. |
| `ShowFlagLabels` | `true` | Label each flag with its faction or its carrier. |
| `FlagMinimapAlways` | `true` | Admins see every flag on the minimap, whatever the reveal rules. |
| `AfkMarkEnabled` | `true` | Grey out players who have stopped moving. |
| `AfkSeconds` | `90` | Seconds without moving before a player counts as AFK. |
| `AfkMoveMetres` | `0.75` | Metres of movement that resets the AFK timer. |
| `RamboUi` | `Off` | When the overlay shows: `On`, `FreeflightOnly` or `Off`. |
| `ToggleKey` | `F6` | Key that hides the overlay and brings it back. Any `KeyCode` name. |
| `ShowRings` | `true` | Ground ring under each watched player. |
| `ShowLabels` | `true` | Floating name and score label. |
| `ShowGlow` | `true` | Glow around watched players and flags, visible through walls. |
| `MaxLabels` | `12` | Most floating labels at once, worst first. |
| `SpectateNextKey` | `RightBracket` | Spectate the next watched player, worst first. |
| `SpectatePrevKey` | `LeftBracket` | Spectate the previous one. |
| `ClassFilterEnabled` | `true` | Let the P menu player search box filter by class. |
| `RegimentSearchEnabled` | `true` | Also match regiment tags there, ignoring punctuation and accents. |
| `FactionSearchEnabled` | `true` | Also match factions and sides there, and combine them with a class. |
| `RowActionsEnabled` | `true` | Add Reg TP and Rez + Reg TP to a player's admin actions. |
| `MeleeMarkerEnabled` | `true` | Crossed swords on kill log entries where the victim was in a melee. |
| `TeamkillFilterEnabled` | `true` | Add the teamkills only toggle, and accept `tk` in the kill log search box. |
| `MeleeChainMetres` | `10` | How close to a fight a player must be to count as in that melee. |
| `MeleeWindowSeconds` | `10` | Seconds without a hit or block anywhere in the melee before it counts as over. |
| `MapEnabled` | `false` | Add the Map tab to the P menu. |

Glow style and map icon size belong to [RyLib](https://github.com/Ryannlt/RyLib#settings), in
`com.ryannlt.rylib.cfg`.

## Tuning it

Every threshold comes down to distance, and the labels show ISO and DGR live, so the quickest way to tune is to
watch a few players you know are out of position and compare their numbers with the table below.

**The scale is set by two numbers.** The score climbs towards the raw distance signal and stops there, so a
player is only ever flagged if their distance alone clears the threshold. With the defaults:

| Metres from your two nearest mates | Score settles at |
| --- | --- |
| under 10 | 0 |
| 15 | 25 |
| 18 | 40, a mark appears |
| 20 | 50 |
| 25 | 75, flagged as a rambo |
| 30 and beyond | 100 |

So a mark appears at about **18 m** out and the rambo flag needs **more than 25 m** held for long enough:
roughly 8 seconds to cross, plus `RamboHoldSeconds` on top.

- **Too many false positives?** Raise `ClusterFarMetres`, which stretches the whole scale, or raise
  `RiseSeconds` so players have longer to get back.
- **Missing obvious rambos?** Lower `ClusterFarMetres`. If your typical rambo sits 18 m out, set it to 22 and
  they cross.
- **Flagging too slowly?** Lower `RiseSeconds`, or `RamboHoldSeconds`.

Dwell decays instead of resetting, so tapping back onto the line for a second does not wipe a rambo's timer. It
falls at `RecoverMultiplier` times the rate it builds, which clears a long-standing flag in a few seconds once
the player is genuinely back.

## Troubleshooting

**Nothing in the log at all, or no log.** BepInEx is not running. Check that `winhttp.dll` and a `BepInEx`
folder sit next to `Holdfast NaW.exe`. If you use a mod manager, launch the game from it rather than from Steam.

**AdminHelper is not loaded and the log mentions RyLib.** RyLib is missing or older than AdminHelper needs.
Install it, or update it, alongside AdminHelper.

**Mod loads but nothing is drawn.** You are almost certainly not logged into the server console. Run
`rc login <password>` and try again. If that is not it, check `Enabled` and that Rambo UI is not `Off`.

**Nothing is drawn and the log has no `scene loaded` line.** The mod lost its frame loop. Open an issue with
`BepInEx\LogOutput.log`; the `driver awake`, `driver destroyed` and `scene loaded` lines say what happened.

**The map shows a grid instead of a picture.** The overhead picture could not be taken on this map. Open an
issue with your log; the RyLib lines about the overhead map say why.

**The game crashes on launch after adding the mod.** Remove the `.dll`, launch to confirm the game is fine, then
open an issue with `BepInEx\LogOutput.log` attached.

## Building

Only needed if you want to change something. Otherwise use the release above.

**Requires:** the game installed, BepInEx and RyLib installed in an r2modman profile, and a
[.NET SDK](https://dotnet.microsoft.com/download) for the compiler.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

That compiles every `.cs` under the repo and copies the `.dll` into the r2modman profile's
`BepInEx\plugins\AdminHelper\`. Restart the game to load it. Add `-NoDeploy` to build without copying, which is
useful while the game is running and holding the file. `-ProfileName` picks a profile other than `Dev`. Build
and deploy RyLib first when changing both, since AdminHelper compiles against the copy in the profile.

`package.ps1` builds the same way and stages an uploadable Thunderstore zip in `Package\`.

The script expects the default install path:

```
C:\Program Files (x86)\Steam\steamapps\common\Holdfast Nations At War
```

Edit `$GameDir` at the top of the script if yours differs.

The `.csproj` is for IDE support only. `build.ps1` is the real build. It drives `csc` directly, which avoids
needing a targeting pack installed.

### Where things live

| Folder | What is in it |
| --- | --- |
| `Core` | The plugin itself, its driver, settings, logging, the overlay hotkey and rambo spectating. |
| `Game` | Everything that reads the game: player snapshots, faction and round lookups, text matching. |
| `Tracking` | The scorers - isolation, formation, melee, AFK and flags. |
| `Overlay` | What gets drawn, handed to RyLib: the world marks, the minimap flags and the map layer. |
| `PMenu` | The P menu: the Mods tab settings, search, kill log and the row actions. |

### Why there is no CI build

Building needs `Assembly-CSharp.dll` from a real install. Those are AGS files, not mine to redistribute, so
they cannot be committed here and a GitHub Actions runner has no way to get them. Releases are built locally and
uploaded by hand.

## Compatibility

Built against Holdfast on Unity 2022.3.62f2 with BepInEx 5.4.23.5 (Mono). It reads named fields rather than
offsets, so it usually survives game updates. If AGS renames or restructures the client player managers it
will stop working rather than misbehave. Open an issue if that happens.

## Licence

[MIT](https://github.com/Ryannlt/AdminHelper/blob/main/LICENSE). Do what you like with it, no warranty.
