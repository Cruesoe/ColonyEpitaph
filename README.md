# Colony Epitaph

RimWorld 1.6 mod. When the colony is wiped, vanilla drops a Game Over **letter**. This replaces that with a paused full-screen epitaph.

Victory credits, the planetkiller fade, and Man in Black are left alone.

## What you see

When everyone is dead, kidnapped, or gone, the map dims and the game pauses:

- “The colony is gone.” plus the colony name
- How the story ended (vanilla letter text, when available)
- How long the run lasted
- Portraits of the fallen, with a short status (died / kidnapped / missing)
- The same choices vanilla already offers: keep watching, create wanderers, load a save, main menu

Wanderers stay on vanilla rules (delay, limits). Permadeath hides load. A setting under **Options → Mod options → Colony Epitaph** restores the original letter.

## Install

Copy this folder to `RimWorld\Mods\`, or add it as a local mod in RimSort. Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

## Build

```
dotnet build Source\ColonyEpitaph.csproj -c Debug
```

The DLL is copied to `1.6\Assemblies\ColonyEpitaph.dll` and to `RimWorld\Mods\Colony Epitaph\1.6\Assemblies\` if that folder exists.

To iterate without wiping a colony, enable development mode and use **Debug actions → Colony Epitaph → Open epitaph screen**.
