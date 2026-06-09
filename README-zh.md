# Wormhole Signal Bridge

![Wormhole Signal Bridge — 经 KEX 虫洞隧道的 CommNet 路由示意](https://i.imgur.com/QgyrQpI.png)

[English](README.md) | [中文](README-zh.md)

在 **Kcalbeloh System** 这类跨星系存档里，常见这样一幕：Jool 轨道上有绕飞 **WH3141A** 的中继，Kcalbeloh 一侧则有绕 **WH3141B** 的探测器——KEX 虫洞能让飞船在 A、B 两口之间穿梭，但 **RealAntennas** 仍把星际距离当作真实 RF 路径，Kerbin 的指令与科学回传往往在链路预算面前够不到彼端；**CommNet 信号也不会自己穿过虫洞喉道。**

**Wormhole Signal Bridge** 在 RA 完成常规网络重建之后，自动扫描 KEX 配置里的全部虫洞配对，在两端定向中继之间注入隧道链路。多跳路由仍由 RealAntennas 处理，例如：`Kerbin 探测器 → Jool（WH3141A）中继 → [隧道] → Kcalbeloh（WH3141B）中继 → 目标`。

## 依赖

- [RealAntennas](https://github.com/KSP-RO/RealAntennas)
- [Kopernicus Expansion Continued](https://github.com/VabienArt/KopernicusExpansion-Continueder)（KEX-Wormholes）

## 虫洞中继部署

每个虫洞口（KEX 配对的 `body` / `partner` 天体）各需**一艘绕飞该天体的常驻中继**，且满足：

- 至少一根启用的 **定向 RA 天线**（`ModuleRealAntenna`），用 RA 现有的 **Body Lat/Lon/Alt** 瞄准模式，在本侧虫洞 CB 上填入 Mouth 的经纬度与高度。**全向天线不能建立虫洞隧道**
- 两端中继使用**相同频段**（RFBand），符号率范围互相兼容
- 推荐另配其它天线，负责 KSC / 本星系深空网回传

隧道只在 **A 口中继 ↔ B 口中继** 之间建立；其他飞船经 RA 路径搜索多跳接入：

- 去远方：`Kerbin 探测器 → … → A 口中继 → [隧道] → B 口中继 → … → 目标`
- 回 Kerbol：`Kcalbeloh 探测器 → B 口中继 → [隧道] → A 口中继 → … → KSC`

本地/回传链路仍按 RA 正常物理计算（真实距离、遮挡、指向）。B 口的全向不会「找到」 A 口中继；两端都需兼容的定向天线，用 **Body Lat/Lon/Alt** 指向本侧虫洞 **Mouth**。

| 位置 | 定向（隧道） | 其它天线（本地/回传） |
|------|-------------|----------------------|
| **A 口**（如 `WH3141A`，默认绕 Jool） | `WH3141A` 上 Mouth（Body Lat/Lon/Alt） | KSC / Kerbol 深空网 |
| **B 口**（如 `WH3141B`，绕 Kcalbeloh 内恒星） | `WH3141B` 上 Mouth（Body Lat/Lon/Alt） | 本星系其他中继 / 行星网 |

Mod 会自动识别所有 KEX 虫洞——**无需**手动配置。

## 发现虫洞口（GRAVMAX）

WSB 在 stock **GRAVMAX**（`sensorGravimeter`）部件上**追加**第二种科学实验：**虫洞口负引力子共振扫描**。原有的 **记录重力数据** / Kerbalism `gravityScan` 实验**不会被覆盖**。

1. 环绕虫洞天体（如 `WH3141A`），并处于中继高度带内（高于 KEX 影响区上边界，低于跳跃区上沿）。
2. 在 GRAVMAX 上运行 WSB 共振扫描（纯 stock 科学界面，或启用 Kerbalism FeatureScience 时的实验界面）。
3. 数据采集完成后，WSB 在本存档**永久登记**该 Mouth 的 **Body Lat/Lon/Alt** 坐标（写入存档缓存，**不会**随轨道或时间漂移）；每个 Mouth **首次发现**额外奖励 **75 000** 资金。首次勘测时，水平位置通常落在虫洞 CB **朝向其母天体**（`referenceBody`）的那一侧，例如 `WH3141A` 面向 Jool 的一面——此后该经纬度在本存档中固定不变。
4. 在**定向** `ModuleRealAntenna` 部件的 **RealAntennas** PAW 组（与 **Antenna Targeting** 同组）中使用 **虫洞口瞄准**。至少完成一次 Mouth 勘测后才会出现该按钮。点击后打开 RA 风格的 WSB 窗口，列出已发现 Mouth（如 `WH3141A Mouth`），**手动选择**后仅瞄准**本天线**——不会自动对准。每个 Mouth 须单独勘测；仅当本船环绕该虫洞天体、RA 通信在线且轨道合格时，对应条目才可选。有链路预算时会显示速率。

这些操作写入与 RA **Body Fixed Point** 相同的目标。对已发现 Mouth 重复扫描仍可获得科学（受 subject 耗尽规则约束），但不会重复登记。

## 配置

**难度设置 → 虫洞信号桥** 中的每存档参数。选择游戏难度预设（简单 / 普通 / 中等 / 困难）会载入下表数值；各字段仍可单独修改。

**启用虫洞信号桥** 下方另有四个 **虫洞信号桥预设** 按钮（简单 / 普通 / 较难 / 困难），只影响本 Mod 参数，与页面顶部的全局游戏难度独立——例如全局选「困难」时，仍可将虫洞信号桥单独设为「简单」。

### 难度预设

| | 简单 | 普通 | 中等 | 困难 |
|---|:---:|:---:|:---:|:---:|
| 高级轨道约束 | 关 | 开 | 开 | 开 |
| 等效距离 (m) | 500 | 1,000 | 1,500 | 2,500 |
| 插入损耗 (dB) | 0 | 0 | 2 | 4 |
| 虫洞口最高高度 (m) | 1,000,000 | 500,000 | 400,000 | 350,000 |
| 最优最高高度 (m) | 300,000 | 200,000 | 180,000 | 170,000 |
| 严格近/远点界 | — | 否 | 是 | 是 |
| 最高远点高度 (m) | 1,000,000 | 500,000 | 400,000 | 350,000 |
| 严格倾角界 | — | 否 | 否 | 是 (5°–175°) |
| 偏好倾角 | — | — | 10°–170° | 15°–165° |
| 严格偏心率界 | — | 否 | 否 | 是 (≤ 0.45) |
| 理想最大偏心率 | — | 0.3 | 0.2 | 0.15 |
| 轨道损耗系数 | — | 0.75 | 1.0 | 1.5 |

所有预设共用同一**硬下沿**：高度必须高于各虫洞 KEX 配置的**影响区上边界高度（最低安全中继高度）**，该值因虫洞而异，不由难度决定。

### 理想中继轨道（轨道质量 = 1）

设 **H<sub>inf</sub>** = 该虫洞的影响区上边界高度（km）。「完美」指轨道质量满分、无额外轨道惩罚。建议**近圆轨道**，定向天线在 RA **Body Lat/Lon/Alt** 模式下指向本侧虫洞 **Mouth**。

| 预设 | 理想轨道 |
|------|----------|
| **简单** | 高于 **H<sub>inf</sub>** 即可（高级轨道约束关闭） |
| **普通** | 近圆，**H<sub>inf</sub>**–**200 km**，任意倾角，偏心率 *e* ≤ 0.3 |
| **中等** | 近圆，**H<sub>inf</sub>**–**180 km**，倾角 **10°–170°**，*e* ≤ 0.2，PeA ≥ **H<sub>inf</sub>**，ApA ≤ **400 km** |
| **困难** | 近圆，**H<sub>inf</sub>**–**170 km**，倾角 **15°–165°**，*e* ≤ 0.15，PeA ≥ **H<sub>inf</sub>**，ApA ≤ **350 km** |

Mouth **高度**约为 **(KEX 跳跃区上沿 + H<sub>inf</sub>) / 2**（均为 km）。**经纬度**在 GRAVMAX 勘测完成时按当时母星方向确定并**写入存档**；之后天线只要用 **Body Lat/Lon/Alt** 指向该登记坐标即可，无需随轨道重新瞄准。

### 主要参数说明

| 参数 | 作用 |
|------|------|
| **等效距离** | 注入虫洞跳路的标称 RF 路径长度（米）。隧道优先采用 RA Precompute 对中继 ↔ Mouth 两段的快照预算；当一端或两端处于后台、快照不可用时，改用两端虫洞定向天线共同支持的后台速率。 |
| **插入损耗** | 喉道额外衰减（dB）。主要压低链路 **metric**（连接质量），**不**直接降低 `dataRate`（传输速率）。 |
| **高级轨道约束** | 开启后，在 **H<sub>inf</sub>** 之上再应用高度上沿、轨道质量评分，以及可选的严格 Pe/Ap、倾角、偏心率规则。 |
| **虫洞口最高高度** | 高级模式下超过即拒链。 |
| **最优最高高度** | 推荐高度带上沿（**H<sub>inf</sub>** … 此值内质量 = 1）。 |
| **边缘质量** | 处于允许范围边缘时的质量分（越低惩罚越重）。 |
| **严格近/远点界** | PeA &lt; **H<sub>inf</sub>** 或 ApA &gt; 最高远点高度则拒链。 |
| **偏好 / 严格倾角** | 倾角的软限制或硬限制。 |
| **理想 / 最大偏心率** | 低于理想值满分；超出则软惩罚或硬拒链。 |
| **最低可用轨道质量** | 综合轨道分低于此值则拒链。 |
| **轨道损耗系数** | 轨道不完美时的额外信号损耗倍率。 |

| 键 | 默认值（普通） | 含义 |
|----|----------------|------|
| `enabled` | `true` | 总开关 |
| `debugLogging` | `false` | 将注入的链路写入 KSP.log |

## 编译

将环境变量 `KSPDIR` 设为 KSP 安装目录。先编译 RealAntennas 与 KEX-Wormholes，然后执行：

```text
msbuild src\WormholeSignalBridge\WormholeSignalBridge.sln /p:Configuration=Release
```

输出：`GameData/WormholeSignalBridge/Plugins/WormholeSignalBridge.dll`

将整个 `GameData/WormholeSignalBridge` 文件夹复制到 KSP 的 `GameData` 目录。

## 说明

- CommNet 地图上的连线可能仍画穿正常空间；链路质量按 RA 预算、虫洞等效距离/损耗和可选轨道质量计算。
- 未受 RA 补丁的第三方 `ModuleDataTransmitter` 部件不参与虫洞隧道。
