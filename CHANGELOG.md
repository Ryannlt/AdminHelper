# Changelog

## 1.4.0

- **Mods tab without an rc login.** AdminHelper's settings show to every player.
- **Minimap tab without an rc login.** It shows only your own team and its ships, with no actions. Enemy
  players, ships, rambo outlines and flags are never drawn there without an `rc login`.
- **Team filter.** With an `rc login`, **Both Teams** on the Minimap tab cycles between both teams and each team.
- **Ships on the Minimap tab.**
  - Each ship is drawn with the icon for its type, in its owner's color, and changes color when captured.
  - Players aboard are hidden and listed when the ship is clicked.
  - Go To, Bring Ship, Move Ship and Remove Ship act on the selected ship.
- **Tags.** A new Tags section turns each state on or off: RAMBO, ISOLATED, MELEE GRACE, CAV CHARGE and AFK. A
  state that is off is not drawn in the world or on the Minimap tab and is skipped by the spectate keys.
- **Header tabs** keep a normal width instead of stretching across the bar.
- **Setting descriptions.** Hovering over a setting in the Mods tab shows what it does.
- **Privateers** on the Minimap tab are drawn black with a white outline.
- The Minimap tab image no longer shows ships, or smoke from muskets and cannons.
- Needs RyLib 1.2.0.

## 1.3.1

- **Rings lie on the ground** and follow its slope. A rider's ring is wider and circles the horse.
- Needs RyLib 1.1.1.

## 1.3.0

- **Cavalry.**
  - A mounted player is cavalry. Riders are scored against the nearest mounted teammate at compass range, which
    AdminHelper reads from the game's compass.
  - A Hussar or Dragoon on foot is scored like any other class.
  - New `[Cavalry]` settings. `ScoreCavalry` is removed.
- **Multicharge.** A mounted kill with fewer than 3 riders chained at compass range gets a red horse icon in the
  kill log.
- **Cav Charge.** Mounted players in a melee show a light purple CAV CHARGE state. The melee spreads between
  riders over a longer distance and lasts while they stay close to an enemy. `]` reaches them right after the
  RAMBOs.
- **Kill log color key** under the title. The Killed In Melee and OUT OF SPACING icons are always listed, and the
  name colors are listed while they show.
- **Kill log name colors** show while Rambo UI is on, and with the new `KillLogColorsWithoutUi` while it is off.
- **FIGHT is renamed MELEE GRACE**, the state for an isolated player excused while fighting a melee.
- **Settings in the Mods tab.** Nearly every setting is in Mods > AdminHelper, grouped, with sliders, dropdowns
  and text boxes.
- **Rules tab hidden.** The game's Rules tab is hidden. `ShowRulesTab` brings it back.
- **Compact labels.** A Mods tab switch shortens labels to the player's name.
- **Reset to defaults** in the Mods tab puts every setting back to its default.
- **Minimap.**
  - The Map tab is renamed Minimap.
  - Its image is 8192 pixels, covers only the play terrains, and leaves out players, horses and ships.
  - Battle Area frames the current spawns and capture points, including spawns moved by a map mod.
- **American spelling.** `ClusterNearMetres`, `ClusterFarMetres`, `AfkMoveMetres` and `MeleeChainMetres` are now
  `...Meters`. Values in an existing config file carry over.
- Needs RyLib 1.1.0.

## 1.2.0

**Now built on [RyLib](https://github.com/Ryannlt/RyLib)**, a shared UI library, so AdminHelper and other mods can
add to the P menu without breaking each other. Mod managers install it automatically; by hand, it goes in
`BepInEx\plugins\RyLib\`.

**The overlay is drawn in the world, not on a HUD.** Watched players glow through walls and terrain, with a ring
at their feet and a label that sits under the P menu rather than over it. The corner list and your own score
readout are gone, and with them `ShowCornerList`, `ShowOwnScore` and `StartHudVisible`. New `ShowGlow`.

**Rambo UI modes.** A **Rambo UI** button under the players list steps between Always On, Freeflight (only in free
roam) and Off, like the game's Admin Raygun, and the mode is remembered between launches. F6 still hides and
restores it. New `RamboUi`.

**Colors say how bad it is.** Isolation alone runs green to yellow; any danger turns the mark orange, deepening
to red. FIGHT stays blue and AFK gray.

**Spectate rambos with `[` and `]`.** Steps through watched players worst first, and the game's own key bar shows
a Spectate Rambos entry in free roam and while spectating. New `SpectatePrevKey` and `SpectateNextKey`.

**All settings in the P menu,** under a shared **Mods** tab: Mods > AdminHelper, grouped into Rambo Detection,
Flags, Players Tab, Kill Log and Map.

**Kill log.** A melee kill is marked with crossed swords, explained by a line under the heading; the old
`IN MELEE` / `NO MELEE` text is gone. While Rambo UI is on, names are colored by their rambo mark at the moment
of the kill. Clicking a row opens actions for the killer and the victim.

**Reg TP prefers the same class.** It sends a player to the middle of their regiment's players of the same class,
and to the closest friendly player when there are none.

**Flags** glow in their faction's color instead of a ring and beam, and follow the game's minimap reveal rules,
with `FlagMinimapAlways` letting admins see all of them. New `ShowFlagLabels`. Every flag setting is now off by
default, including `FlagMinimapMarkers`.

**A battle map.** A Map tab with an overhead picture of the battle, every player drawn by class in
their faction's color, a class legend, search, zoom, rambo outlines and flags, and admin actions that only fire
after a button is pressed. Turn it off with `MapEnabled` or Mods > AdminHelper > Map.

## 1.1.2

**The player search reads factions and sides.** `french`, `allied`, `austrian` and the country names behind them
all filter the players tab, and `attackers` or `defenders` resolve to whichever faction is attacking or defending
this round. They combine with a class, so `french cav` is French hussars and dragoons and `def surgeon` is the
defending side's surgeons, and plurals are understood throughout. Class groups grew too: `skirmishers`,
`rank & file`, `infantry`, `support` and `horse`. Anything the mod cannot read is handed straight back to the
stock name and tag search, so nothing that worked before stopped working. New `FactionSearchEnabled`.

**Two new buttons on a player's admin row.** `Reg TP` teleports that player to their own regiment - to the mate
standing nearest the middle of the group, so they land in the line rather than on a straggler - and
`Rez + Reg TP` revives a dead player first, waits for them to come back, then sends them. Both go through the
game's own admin plumbing, and both live in the admin block, so they only exist once you are logged in. New
`RowActionsEnabled`.

Source files are now grouped into `Core`, `Game`, `Tracking`, `Overlay` and `PMenu` folders. No behavior
change, but `build.ps1` now recurses.

## 1.1.1

Three additions for CTF events.

**Flags are highlighted.** With the overlay up, every flag in the round gets a cyan ring and a beam, either at
the carrier's feet or where the flag is lying, a label saying who has it, and a line at the top of the corner
list with the distance. On the minimap the carrier's own pointer is tinted rather than doubled up, and a flag on
the ground gets a marker of its own, named by its faction. Every flag carryable the game has counts, so this is
not tied to one CTF mod. It ships **off**, since it only matters on a flag game mode - turn on
`FlagHighlightEnabled` under the new `[CTF]` section.

**Players standing still are grayed out.** Anyone who has not moved for `AfkSeconds` is drawn gray with an `AFK`
tag, so a parked player is not read as someone worth flying to. They keep their ring and their place in the list.
New `[AFK]` settings.

**Melee grace.** A player who enters a melee without already being flagged is not flagged while it lasts, plus
the usual window afterwards, and the label reads `FIGHT`. The last one or two left alive in a fight their mates
started no longer read as rambos, while a player who runs in alone still flags. Their dwell keeps building
underneath, so nothing is lost if they really are out on their own. Turn it off with `MeleeGrace`.

## 1.1.0

Two additions, both inside the game's own P menu rather than a separate window.

**Find players by class or regiment.** The players tab search box now also matches class names and shorthands,
so `rifleman`, `rifle`, `line`, `sgt` and `cavalry` all narrow the list, with the count above it. Regiment tags
are matched with punctuation and accents stripped from both sides, so `[45e]` is found by `45e` or `45`, and
`7.Fuß` by `7fus`. Searching by name or ID is unchanged.

**Kill log.** Every kill is marked with whether the victim was in a melee when they died, so a teamkill in a
scrum can be told apart from a clean one. A player counts as being in a melee once they hit, block, or are
blocked by the other side, and that spreads to everyone standing within `MeleeChainMeters` of them. A blank mark
means the mod was not tracking and does not know, which is never to be read as a clean kill. A square toggle
beside the heading switches the log to teamkills only, and typing `tk` in its search box does the same.

New settings live under `[PMenu]` and `[KillLog]`.

## 1.0.1

Fixed the BepInEx dependency. 1.0.0 depended on `BepInEx-BepInExPack`, which is not published in the Holdfast
community, so mod managers could not resolve it and installed no loader. Now depends on
`HoldfastModding-BepInExPack_Holdfast`. No code changes.

## 1.0.0

First release. Built for BepInEx 5.
