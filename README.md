# AdminEye

[![Latest release](https://img.shields.io/github/v/release/Ryannlt/AdminEye?label=latest&style=flat-square)](https://github.com/Ryannlt/AdminEye/releases/latest)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/Ryannlt/AdminEye/blob/main/LICENSE)

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Holdfast: Nations At War** that finds "rambos":
players who have left their formation and are off fighting on their own. It scores every player continuously
and marks the ones who stay out, so an admin does not have to eyeball a 150 player field.

**It only works for admins.** Nothing about another player is drawn until the server itself has authenticated
your `rc login`. See [Admin only](#admin-only).

## Install

**With a mod manager.** Install through [r2modman](https://r2modman.com/) or Thunderstore Mod Manager and launch
the game from the manager. BepInEx is pulled in as a dependency, so there is nothing else to set up.

**By hand.** Install
[BepInExPack](https://thunderstore.io/c/holdfast-nations-at-war/p/BepInEx/BepInExPack/) into the game folder and
run the game once so it creates its folders. Then put `AdminEye.dll` here:

```
Steam\steamapps\common\Holdfast Nations At War\BepInEx\plugins\AdminEye.dll
```

### Did it work?

Open `BepInEx\LogOutput.log` and look for:

```
[Info   :   BepInEx] Loading [AdminEye 1.0.0]
```

If it is not there, the mod was not loaded. See [Troubleshooting](#troubleshooting).

## Using it

Log into the server console as normal:

```
rc login <password>
```

From then on, anyone drifting gets a ring at their feet and a floating label with their scores. The marker
appears as soon as ISO passes `RingThreshold` and clears the moment they come back, because it follows the live
score rather than a timer.

Two states:

- **ISOLATED**, yellow. Out of position right now. Worth a look.
- **RAMBO**, red. Has held above `RamboThreshold` for `RamboHoldSeconds`, and the label counts the seconds.

A corner list gives you the same players sorted worst first, with scores and distance, so you know who to fly
to.

Press **F6** to hide and show the overlay. The scorer keeps running while it is hidden, so nothing is lost and a
player already flagged is still flagged when you bring it back. Change the key with `ToggleKey`.

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

Every part of this mod that reveals another player is behind `rc login`. Without it, the mod draws nothing but
your own scores.

That gate is deliberate. A HUD that marks isolated enemies is a wallhack in everything but intent, so it is tied
to the server's own admin authentication rather than to a setting. `RequireAdminLogin` exists so you can test
against bots on your own server, and turning it off gives you exactly what it sounds like.

## Settings

`BepInEx\config\com.ryannlt.admineye.cfg`, written on first run. Read live, so edits apply within a second with
no restart. Entries are grouped into `[General]`, `[Isolation]`, `[Scoring]`, `[Danger]`, `[Formation]`,
`[Flagging]` and `[Display]` sections.

For an in-game editor instead of a text file, [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
works, but only after setting `HideManagerGameObject = true` under `[Chainloader]` in
`BepInEx\config\BepInEx.cfg`. Holdfast's BepInEx pack ships that as `false`, which stops
ConfigurationManager drawing at all.

| Setting | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. Off stops the scorer as well as the HUD. |
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
| `RingThreshold` | `40` | ISO at which a marker appears. Follows the live score, so it clears on return. |
| `RamboThreshold` | `75` | ISO at which the dwell timer starts. |
| `RamboHoldSeconds` | `5` | Seconds of dwell above the threshold before flagging. |
| `ScoreCavalry` | `false` | Score cavalry too. |
| `ExemptClasses` | empty | Comma-separated class names never flagged, e.g. `Surgeon,Sapper`. |
| `ShowRings` | `true` | Ground ring under each watched player. |
| `ShowLabels` | `true` | Floating name and score label. |
| `ShowCornerList` | `true` | Corner list of watched players, worst first. |
| `ShowOwnScore` | `true` | Your own scores plus the raw distances behind them. Works without an `rc login`. |
| `MaxLabels` | `12` | Most floating labels at once, worst first. |
| `ToggleKey` | `F6` | Key that hides and shows the HUD. Any `KeyCode` name. |
| `StartHudVisible` | `true` | Whether the HUD starts visible. |

## Tuning it

`ShowOwnScore` is on by default and shows the raw inputs under your scores: how far you are from your two
nearest mates, how far the nearest enemy is, whether you count as in formation, and your dwell time. Walk away
from your own line and watch those move. Every threshold below is picked from that one readout.

**The scale is set by two numbers.** The score climbs towards the raw distance signal and stops there, so a
player is only ever flagged if their distance alone clears the threshold. With the defaults:

| Metres from your two nearest mates | Score settles at |
| --- | --- |
| under 10 | 0 |
| 15 | 25 |
| 18 | 40, a marker appears |
| 20 | 50 |
| 25 | 75, flagged as a rambo |
| 30 and beyond | 100 |

So a marker appears at about **18 m** out and the rambo flag needs **more than 25 m** held for long enough:
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

**Mod loads but nothing is drawn.** You are almost certainly not logged into the server console. Run
`rc login <password>` and try again. If that is not it, check `Enabled` and press the toggle key.

**Nothing is drawn and the log has no `scene loaded` line.** The mod lost its frame loop. Open an issue with
`BepInEx\LogOutput.log`; the `driver awake`, `driver destroyed` and `scene loaded` lines say what happened.

**Rings are missing but labels work.** The ring shader could not be found. Set `ShowRings` to false and open an
issue with your log.

**The game crashes on launch after adding the mod.** Remove the `.dll`, launch to confirm the game is fine, then
open an issue with `BepInEx\LogOutput.log` attached.

## Building

Only needed if you want to change something. Otherwise use the release above.

**Requires:** the game installed, BepInEx installed in an r2modman profile, and a
[.NET SDK](https://dotnet.microsoft.com/download) for the compiler.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

That compiles every `.cs` in the folder and copies the `.dll` into the r2modman profile's
`BepInEx\plugins\AdminEye\`. Restart the game to load it. Add `-NoDeploy` to build without copying, which is
useful while the game is running and holding the file. `-ProfileName` picks a profile other than `Dev`.

`package.ps1` builds the same way and stages an uploadable Thunderstore zip in `Package\`.

The script expects the default install path:

```
C:\Program Files (x86)\Steam\steamapps\common\Holdfast Nations At War
```

Edit `$GameDir` at the top of the script if yours differs.

The `.csproj` is for IDE support only. `build.ps1` is the real build. It drives `csc` directly, which avoids
needing a targeting pack installed.

### Why there is no CI build

Building needs `Assembly-CSharp.dll` from a real install. Those are AGS files, not mine to redistribute, so
they cannot be committed here and a GitHub Actions runner has no way to get them. Releases are built locally and
uploaded by hand.

## Compatibility

Built against Holdfast on Unity 2022.3.62f2 with BepInEx 5.4.23.5 (Mono). It reads named fields rather than
offsets, so it usually survives game updates. If AGS renames or restructures the client player managers it
will stop working rather than misbehave. Open an issue if that happens.

## Licence

[MIT](https://github.com/Ryannlt/AdminEye/blob/main/LICENSE). Do what you like with it, no warranty.
