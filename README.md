# Wormhole Signal Bridge

![Wormhole Signal Bridge — CommNet routing through a KEX wormhole tunnel](https://i.imgur.com/QgyrQpI.png)

[English](README.md) | [中文](README-zh.md)

In saves like **Kcalbeloh System**, a familiar scene plays out: a relay orbits Jool at **WH3141A**, while a probe circles **WH3141B** on the far side—KEX wormholes let craft shuttle between mouths A and B, but **RealAntennas** still treats interstellar separation as real RF path length. Commands and science from Kerbin often fall short on link budget; **CommNet does not cross the throat on its own.**

**Wormhole Signal Bridge** runs after RA’s normal network rebuild, discovers every KEX wormhole pair from Kopernicus config, and injects a tunnel link between directional relays at each mouth. Multi-hop routing stays in RealAntennas, e.g. `Kerbin probe → Jool (WH3141A) relay → [tunnel] → Kcalbeloh (WH3141B) relay → destination`.

## Requirements

- [RealAntennas](https://github.com/KSP-RO/RealAntennas)
- [Kopernicus Expansion Continued](https://github.com/VabienArt/KopernicusExpansion-Continueder) (KEX-Wormholes)

## Relay setup

Each wormhole mouth (each celestial body in a KEX `body` / `partner` pair) needs **one powered relay in orbit** with:

- At least one enabled **directional RA antenna** (`ModuleRealAntenna`) aimed at the local wormhole **Mouth** using RA’s existing **Body Lat/Lon/Alt** targeting (enter lat, lon, alt for the wormhole CB). **Omni antennas cannot form the tunnel.**
- The **same RFBand** on both mouths, with compatible symbol-rate ranges
- Additional antennas recommended for KSC / in-system backbone backhaul

The tunnel exists only between **mouth A ↔ mouth B** relays. Other craft join via normal RA pathfinding:

- To the far side: `Kerbin probe → … → mouth A relay → [tunnel] → mouth B relay → … → destination`
- Back to Kerbol: `Kcalbeloh probe → mouth B relay → [tunnel] → mouth A relay → … → KSC`

Local hops (craft ↔ relay, relay ↔ backbone) still use normal RA physics—real distance, occlusion, and pointing. An omni on mouth B does **not** reach mouth A; both ends need compatible directional dishes aimed at their local wormhole **Mouth** (Body Lat/Lon/Alt on that CB).

| Mouth | Directional (tunnel) | Other antennas (local/backhaul) |
|-------|----------------------|---------------------------------|
| **A** (e.g. `WH3141A`, orbiting Jool) | Mouth on `WH3141A` (Body Lat/Lon/Alt) | KSC / Kerbol deep-space network |
| **B** (e.g. `WH3141B`, orbiting Kcalbeloh’s host star) | Mouth on `WH3141B` (Body Lat/Lon/Alt) | In-system relays / planetary network |

All KEX wormholes are discovered automatically—**no** manual configuration.

## Discovering wormhole mouths (GRAVMAX)

WSB adds a second science experiment to the stock **GRAVMAX** (`sensorGravimeter`) part: **Wormhole Mouth Gravioli Resonance Scan**. The original **Log Gravity Data** / Kerbalism `gravityScan` experiment is unchanged.

1. Orbit the wormhole celestial body (e.g. `WH3141A`) within the relay altitude band (above KEX influence altitude, below the jump zone ceiling).
2. Run the WSB resonance scan on GRAVMAX (stock science UI, or Kerbalism experiment UI when FeatureScience is enabled).
3. When data collection completes, WSB **permanently registers** that mouth’s **Body Lat/Lon/Alt** coordinates in the save (cached; they do **not** drift with orbit or time), and awards a **75 000 funds** bonus on first discovery per mouth. On first survey, the horizontal position is usually on the side of the wormhole CB facing its parent (`referenceBody`), e.g. the side of `WH3141A` facing Jool—that lat/lon then stays fixed for the save.
4. On each **directional** `ModuleRealAntenna` part, use **Wormhole Mouth Targeting** in the **RealAntennas** PAW group (same place as **Antenna Targeting**). This button appears only after at least one mouth has been surveyed. It opens a WSB window styled like RA’s target UI, listing discovered mouths (e.g. `WH3141A Mouth`). Select one to aim **this antenna only**—nothing is auto-aimed. Each mouth must be surveyed separately; you can only select a mouth while your relay orbits that wormhole body, with RA comm online and an acceptable orbit. Entries show link budget when available.

These actions set the same **Body Fixed Point** target RA already uses internally. Repeat scans on an already-discovered mouth still yield science (subject depletion rules apply) but do not re-register the mouth.

## Configuration

Per-save options in **Difficulty Settings → Wormhole Signal Bridge**. Choosing a game difficulty preset (Easy / Normal / Moderate / Hard) loads the values below; individual fields remain editable.

Below **Enable wormhole signal bridge**, four **Wormhole Signal Bridge preset** controls (Easy / Normal / Moderate / Hard) apply WSB values only. They are independent of the global game difficulty buttons at the top of the page—for example, you can run global Hard while keeping WSB on Easy.

### Difficulty presets

| | Easy | Normal | Moderate | Hard |
|---|:---:|:---:|:---:|:---:|
| Advanced orbit constraints | Off | On | On | On |
| Effective distance (m) | 500 | 1,000 | 1,500 | 2,500 |
| Insertion loss (dB) | 0 | 0 | 2 | 4 |
| Max mouth altitude (m) | 1,000,000 | 500,000 | 400,000 | 350,000 |
| Optimal max altitude (m) | 300,000 | 200,000 | 180,000 | 170,000 |
| Strict Pe/Ap bounds | — | No | Yes | Yes |
| Max ApA (m) | 1,000,000 | 500,000 | 400,000 | 350,000 |
| Strict inclination | — | No | No | Yes (5°–175°) |
| Preferred inclination | — | — | 10°–170° | 15°–165° |
| Strict eccentricity | — | No | No | Yes (≤ 0.45) |
| Ideal max eccentricity | — | 0.3 | 0.2 | 0.15 |
| Orbit loss scale | — | 0.75 | 1.0 | 1.5 |

All presets share the same **hard floor**: altitude must stay above each wormhole’s KEX **influence altitude** (minimum safe relay height). That floor is per wormhole, not a difficulty setting.

### Ideal relay orbit (quality = 1)

Let **H<sub>inf</sub>** = that wormhole’s influence altitude (km). “Perfect” means full orbit quality, no extra orbit penalty. Use a **circular** orbit and aim the directional antenna at the local **Mouth** via RA **Body Lat/Lon/Alt** on the wormhole CB.

| Preset | Ideal orbit |
|--------|-------------|
| **Easy** | Any orbit above **H<sub>inf</sub>** (advanced constraints off) |
| **Normal** | Circular, **H<sub>inf</sub>**–**200 km**, any inclination, *e* ≤ 0.3 |
| **Moderate** | Circular, **H<sub>inf</sub>**–**180 km**, inclination **10°–170°**, *e* ≤ 0.2, PeA ≥ **H<sub>inf</sub>**, ApA ≤ **400 km** |
| **Hard** | Circular, **H<sub>inf</sub>**–**170 km**, inclination **15°–165°**, *e* ≤ 0.15, PeA ≥ **H<sub>inf</sub>**, ApA ≤ **350 km** |

Mouth **altitude** is about **(KEX jump zone ceiling + H<sub>inf</sub>) / 2** (both in km). **Latitude/longitude** are set from the parent-facing side at GRAVMAX survey completion and **saved in the scenario**; afterward, aim the directional antenna at those registered Body Lat/Lon/Alt coordinates—no re-aiming as the wormhole orbits.

### Key parameters

| Parameter | Role |
|-----------|------|
| **Effective distance** | Nominal RF path length (m) stored on the injected wormhole hop. The tunnel prefers RA Precompute snapshots for the relay ↔ Mouth legs; when one or both relays are in the background and no snapshot is available, it falls back to the common data rate supported by the two wormhole-facing directional antennas. |
| **Insertion loss** | Extra throat attenuation (dB). Scales link **metric** (connection quality), not `dataRate` (transfer rate) directly. |
| **Advanced orbit constraints** | When on, applies altitude ceiling, orbit-quality scoring, and optional strict Pe/Ap, inclination, and eccentricity rules on top of **H<sub>inf</sub>**. |
| **Max mouth altitude** | Instant reject above this height (advanced mode). |
| **Optimal max altitude** | Top of the preferred altitude band (quality = 1 inside **H<sub>inf</sub>** … this value). |
| **Edge quality** | Quality score at the edge of an allowed range (lower = harsher falloff). |
| **Strict Pe/Ap bounds** | Reject if PeA &lt; **H<sub>inf</sub>** or ApA &gt; max ApA. |
| **Preferred / strict inclination** | Soft or hard limits on relay inclination. |
| **Ideal / max eccentricity** | Full quality below ideal; soft or hard limits above. |
| **Min usable orbit quality** | Combined orbit score must stay above this or the link is rejected. |
| **Orbit loss scale** | Multiplier for extra signal loss when orbit quality &lt; 1. |

| Key | Default (Normal) | Meaning |
|-----|------------------|---------|
| `enabled` | `true` | Global on/off |
| `debugLogging` | `false` | Log injected links |

## Build

Set environment variable `KSPDIR` to your KSP install. Build RealAntennas and KEX-Wormholes first, then:

```text
msbuild src\WormholeSignalBridge\WormholeSignalBridge.sln /p:Configuration=Release
```

Output: `GameData/WormholeSignalBridge/Plugins/WormholeSignalBridge.dll`

Copy the `GameData/WormholeSignalBridge` folder into your KSP `GameData` directory.

## Notes

- CommNet map lines may still draw across normal space; link quality uses RA budgets plus wormhole distance/loss and optional orbit quality.
- Third-party parts that still use stock `ModuleDataTransmitter` without an RA patch are ignored for tunnel hops.
