# Changelog

## 1.1.1

Three additions for CTF events.

**Flags are highlighted.** With the overlay up, every flag in the round gets a cyan ring and a beam, either at
the carrier's feet or where the flag is lying, a label saying who has it, and a line at the top of the corner
list with the distance. On the minimap the carrier's own pointer is tinted rather than doubled up, and a flag on
the ground gets a marker of its own, named by its faction. Every flag carryable the game has counts, so this is
not tied to one CTF mod. It ships **off**, since it only matters on a flag game mode - turn on
`FlagHighlightEnabled` under the new `[CTF]` section.

**Players standing still are greyed out.** Anyone who has not moved for `AfkSeconds` is drawn grey with an `AFK`
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
blocked by the other side, and that spreads to everyone standing within `MeleeChainMetres` of them. A blank mark
means the mod was not tracking and does not know, which is never to be read as a clean kill. A square toggle
beside the heading switches the log to teamkills only, and typing `tk` in its search box does the same.

New settings live under `[PMenu]` and `[KillLog]`.

## 1.0.1

Fixed the BepInEx dependency. 1.0.0 depended on `BepInEx-BepInExPack`, which is not published in the Holdfast
community, so mod managers could not resolve it and installed no loader. Now depends on
`HoldfastModding-BepInExPack_Holdfast`. No code changes.

## 1.0.0

First release. Built for BepInEx 5.
