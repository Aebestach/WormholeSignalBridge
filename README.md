# Wormhole Signal Bridge

![Wormhole Signal Bridge — CommNet routing through a KEX wormhole tunnel](https://i.imgur.com/QgyrQpI.png)

[English](README.md) | [中文](README-zh.md)

**RealAntennas** treats interstellar separation as real RF path length—often making links across star systems impossible on link budget alone. **Kopernicus Expansion Continued** (KEX-Wormholes) can move vessels between systems, but it does not extend CommNet through the throat.

**Wormhole Signal Bridge** runs after RA’s normal network rebuild, discovers every KEX wormhole pair from Kopernicus config, and injects a **short effective-distance** tunnel between relays orbiting each mouth. Multi-hop routing stays in RealAntennas, for example:

`Kerbin probe → Jool mouth relay → [tunnel] → Kcalbeloh mouth relay → destination`

Place one **powered relay with RA antennas** in orbit at each wormhole body. All KEX wormholes are picked up automatically—**no** manual wormhole names or per-pair config. **Tunnel hops are omni↔omni only**; directionals serve local/backhaul links. The tunnel hop ignores real-space separation (e.g. 200 Mm); hops to/from the relay and the home-system backbone still use normal RA physics.

## Requirements

- [RealAntennas](https://github.com/KSP-RO/RealAntennas)
- [Kopernicus Expansion Continued](https://github.com/VabienArt/KopernicusExpansion-Continueder) (KEX-Wormholes)
- [Harmony](https://github.com/KSPModdingLibs/HarmonyKSP)

## How it works

After RealAntennas finishes its normal network rebuild, this mod:

1. Finds all KEX wormhole pairs from Kopernicus config (`partner` field)—**automatically discovers every wormhole**; no manual names or per-pair config.
2. Collects CommNet nodes on each wormhole body (**only** vessels orbiting that body, with power and comm capability).
3. Injects **omni↔omni** tunnel links between mouth relays on paired bodies using a **short effective distance** plus configurable **insertion loss**.
4. Normal RealAntennas pathfinding handles multi-hop routes, e.g.  
   `Kerbin probe → WH3141A relay → WH3141B relay → KSC`.

## Wormhole relay setup

### Minimum requirements

Each wormhole mouth (each celestial body in a KEX `body` / `partner` pair) needs **one resident relay vessel in orbit** with:

- Power and comm capability (probe core, command pod, etc.)
- At least one enabled **omni RA antenna** (`ModuleRealAntenna` classified as omni; stock small comm parts usually qualify after RA is installed). **Required for the tunnel**—directional-only relays cannot open a tunnel
- A **directional antenna** is recommended for KSC / in-system backbone backhaul
- The **same RFBand** on both mouth relays, with compatible symbol-rate ranges

The tunnel exists only between **mouth A ↔ mouth B**. Other craft join via multi-hop pathfinding, e.g.:

- Outbound: `Kerbin probe → … → mouth A relay → [tunnel] → mouth B relay → … → destination`
- Inbound: `Kcalbeloh probe → mouth B relay → [tunnel] → mouth A relay → … → KSC`

### Omni vs directional

| Link type | Behavior |
|-----------|----------|
| **Wormhole tunnel** (A ↔ B) | **Omni↔omni only**; directionals never carry the tunnel |
| **Local hops** (other craft ↔ relay, or relay ↔ home-system backbone) | Normal RealAntennas physics: real distance, occlusion, **pointing**; omni or directional |

**Common mistake:** an omni on mouth B does **not** “find” the relay at mouth A over real-space RF—they are in different star systems. A ↔ B is an injected tunnel hop, independent of real-space antenna pointing.

### Recommended layout

**Best practice: omni for the tunnel + directional for backbone** (same RFBand on both mouths):

| Mouth | Omni (tunnel) | Directional (local/backhaul) |
|-------|---------------|----------------------------|
| **A** (e.g. Kcalbeloh `WH3141A`, orbiting Jool by default) | Tunnel to mouth B omni | Toward KSC / Kerbol deep-space network |
| **B** (e.g. `WH3141B`, in the Kcalbeloh system) | Tunnel to mouth A omni | Toward in-system relays / planetary network |

If you can only fit one antenna:

- **Directional only:** **no wormhole tunnel**; local/backhaul only, and you must keep targets in the beam.
- **Omni only:** tunnel works; easy locally, but long backhaul (Jool mouth → Kerbin, B mouth → in-system backbone) may lack gain.

When mass is tight: **do not drop the omni** (required for the tunnel); prioritize correct directional backhaul aim.

## Configuration

All wormholes share the global options in `PluginData/Settings.cfg`. **No** `Wormholes.cfg` or per-wormhole name entries are needed.

### `PluginData/Settings.cfg`

| Key | Default | Meaning |
|-----|---------|---------|
| `enabled` | `true` | Global on/off |
| `effectiveDistance` | `1000` | Effective RF path length through the wormhole (m) |
| `insertionLoss` | `0` | Extra attenuation (dB); `0` ≈ lossless tunnel |
| `debugLogging` | `false` | Log injected links |

## Build

Set environment variable `KSPDIR` to your KSP install. Build RealAntennas and KEX-Wormholes first, then:

```text
msbuild src\WormholeSignalBridge\WormholeSignalBridge.sln /p:Configuration=Release
```

Output: `GameData/WormholeSignalBridge/Plugins/WormholeSignalBridge.dll`

Copy the `GameData/WormholeSignalBridge` folder into your KSP `GameData` directory.

## Notes

- Tunnel links are **in addition to** normal RealAntennas links. Local links to a wormhole relay still use real distance and occlusion.
- Tunnel hops are **omni↔omni only** (RA `AntennaShape.Omni`). Directionals are for local/backhaul segments only.
- Tunnel hops use **RA digital-modulation antennas** (`ModuleRealAntenna` parts). Third-party parts that still use stock `ModuleDataTransmitter` without an RA patch are ignored.
- Mouth relays on both ends must share the **same RFBand**, with compatible symbol-rate ranges.
- CommNet map lines may still draw across normal space; link quality uses tunnel physics.
