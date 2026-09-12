# Changelog

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
