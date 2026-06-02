# Wormhole Signal Bridge

![Wormhole Signal Bridge — 经 KEX 虫洞隧道的 CommNet 路由示意](https://i.imgur.com/QgyrQpI.png)

[English](README.md) | [中文](README-zh.md)

在 **RealAntennas** 里，跨星系的物理距离会让链路预算几乎不可能成立——而 **Kopernicus Expansion Continued**（KEX-Wormholes）的虫洞可以把飞船送到另一个恒星系，却不会帮 CommNet「抄近路」。

**Wormhole Signal Bridge** 在 RA 完成常规网络重建之后，自动扫描 KEX 配置里的全部虫洞配对，在虫洞两端的中继之间注入一条**等效距离很短**的隧道链路。多跳路由仍由 RealAntennas 自己算，例如：

`Kerbin 探测器 → Jool 虫洞中继 → [隧道] → Kcalbeloh 虫洞中继 → 目标`

你需要在每个虫洞口部署一艘**绕飞该天体**、带 RA 天线的常驻中继；Mod 会识别所有 KEX 虫洞，**无需**手动填写虫洞名称或逐项配置。**虫洞隧道段仅使用全向↔全向**；定向天线只参与本地/回传链路。隧道段不受真实空间距离（例如 200 Mm）约束，但飞船连到中继、或中继连回本星系深空网，仍按 RA 的正常 RF 物理计算。

## 依赖

- [RealAntennas](https://github.com/KSP-RO/RealAntennas)
- [Kopernicus Expansion Continued](https://github.com/VabienArt/KopernicusExpansion-Continueder)（KEX-Wormholes）
- [Harmony](https://github.com/KSPModdingLibs/HarmonyKSP)

## 工作原理

RealAntennas 完成常规网络重建后，本 Mod 会：

1. 扫描 KEX 虫洞配对（Kopernicus 配置中的 `partner` 字段），**自动识别所有虫洞**，无需手动填写虫洞名称。
2. 收集各虫洞天体上的 CommNet 节点（**仅限**绕飞该天体、有电且能通信的飞船）。
3. 在配对虫洞的中继之间注入**全向↔全向**隧道链路，使用**较短的等效距离**与可配置的**插入损耗**。
4. 多跳路由仍由 RealAntennas 路径搜索处理，例如：  
   `Kerbin 探测器 → WH3141A 中继 → WH3141B 中继 → KSC`。

## 虫洞中继部署

### 最低要求

每个虫洞口（KEX 配对的 `body` / `partner` 天体）各需**一艘绕飞该天体的常驻中继**，且满足：

- 有电、能通信（探头 / 指令舱等）
- 至少一根启用的 **全向 RA 天线**（`ModuleRealAntenna`，RA 判定为全向形态；安装 RA 后 stock 小型通信天线通常满足）。**虫洞隧道必需**；仅定向则无法建隧道
- 推荐另配 **定向天线**，负责 KSC / 本星系深空网回传
- 两端中继使用**相同频段**（RFBand），符号率范围互相兼容

隧道只在 **A 口中继 ↔ B 口中继** 之间建立；其他飞船经路径搜索多跳接入，例如：

- 去远端：`Kerbin 探测器 → … → A 口中继 → [隧道] → B 口中继 → … → 目标`
- 回 Kerbol：`Kcalbeloh 探测器 → B 口中继 → [隧道] → A 口中继 → … → KSC`

### 全向 vs 定向

| 链路类型 | 行为 |
|---------|------|
| **虫洞隧道段**（A ↔ B） | **仅全向↔全向**；定向天线不参与隧道 |
| **本地段**（其他飞船 ↔ 中继，或中继 ↔ 本星系深空网） | 仍走 RealAntennas 正常物理：真实距离、遮挡、**指向**；定向/全向均可 |

**常见误解：** B 口的全向不会通过无线电在真实空间中“找到” A 口的中继——两端位于不同星系，A ↔ B 由 Mod 注入隧道完成，与天线在真实空间中的指向无关。

### 推荐配置

**推荐每端「全向建隧道 + 定向做骨干」**（两端 RFBand 一致）：

| 位置 | 全向（隧道） | 定向（本地/回传） |
|------|-------------|------------------|
| **A 口**（如 Kcalbeloh 的 `WH3141A`，默认绕 Jool） | 与 B 口全向建立虫洞隧道 | 指向 KSC / Kerbol 深空网 |
| **B 口**（如 `WH3141B`，绕 Kcalbeloh 内恒星） | 与 A 口全向建立虫洞隧道 | 指向本星系其他中继 / 行星网 |

只带一种天线时：

- **只定向**：**无法**建立虫洞隧道；仅能承担 SOI 内或回传链路，且需持续对准目标。
- **只全向**：可建隧道；近距接驳方便，但远距回传（Jool 口对 Kerbin、B 口对本星系骨干）可能增益不足。

预算紧张时：**全向不可省**（隧道必需）；优先保证定向回传方向正确。

## 配置

所有虫洞共用 `PluginData/Settings.cfg` 中的全局参数，**无需** `Wormholes.cfg` 或按虫洞名称单独配置。

### `PluginData/Settings.cfg`

| 键 | 默认值 | 含义 |
|----|--------|------|
| `enabled` | `true` | 总开关 |
| `effectiveDistance` | `1000` | 穿越虫洞的等效 RF 路径长度（米） |
| `insertionLoss` | `0` | 额外衰减（dB）；`0` 约等于无损隧道 |
| `debugLogging` | `false` | 将注入的链路写入 KSP.log |

## 编译

将环境变量 `KSPDIR` 设为 KSP 安装目录。先编译 RealAntennas 与 KEX-Wormholes，然后执行：

```text
msbuild src\WormholeSignalBridge\WormholeSignalBridge.sln /p:Configuration=Release
```

输出：`GameData/WormholeSignalBridge/Plugins/WormholeSignalBridge.dll`

将整个 `GameData/WormholeSignalBridge` 文件夹复制到 KSP 的 `GameData` 目录。

## 说明

- 隧道链路**叠加**于 RealAntennas 正常链路之上；其他飞船连到虫洞中继仍按真实距离与遮挡计算。
- 隧道段**仅全向↔全向**（RA 的 `AntennaShape.Omni`）；定向天线只用于本地/回传段。
- 隧道段使用 **RA 数字调制天线**（`ModuleRealAntenna` 部件；未受 RA 补丁的第三方 `ModuleDataTransmitter` 不参与）。
- 虫洞口两端中继须使用**相同频段**（RFBand），符号率范围互相兼容。
- CommNet 地图上的连线可能仍画穿正常空间；链路质量按隧道物理模型计算。
