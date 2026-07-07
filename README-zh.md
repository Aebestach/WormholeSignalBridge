# Wormhole Signal Bridge

![Wormhole Signal Bridge — 经 KEX 虫洞隧道的 CommNet 路由示意](https://i.imgur.com/XrCKAIr.png)

[English](README.md) | [中文](README-zh.md)

在 **Kcalbeloh System** 这类跨星系存档里，常见这样一幕：Jool 轨道上有绕飞 **WH3141A** 的中继，Kcalbeloh 一侧则有绕 **WH3141B** 的探测器——KEX 虫洞能让飞船在 A、B 两口之间穿梭，但 **RealAntennas** 仍把星际距离当作真实 RF 路径，Kerbin 的指令与科学回传往往在链路预算面前够不到彼端；**CommNet 信号也不会自己穿过虫洞喉道。**

**Wormhole Signal Bridge**（v2）在 RA 完成常规网络重建之后，自动扫描 KEX 配置里的全部虫洞配对，在两端定向中继之间注入隧道链路。多跳路由仍由 RealAntennas 处理，例如：`Kerbin 探测器 → Jool（WH3141A）中继 → [隧道] → Kcalbeloh（WH3141B）中继 → 目标`。

## 工作原理

WSB 通过 RealAntennas 网络事件接入，不再 Harmony 补丁 RA 内部逻辑：

1. **网络预更新** — 刷新 KEX 虫洞注册表，并同步不可见的 **虫洞口代理节点**（每个虫洞天体各一个）。每个代理是停在 Mouth Body Lat/Lon/Alt 上的地面站式 `RACommNode`，在各 RA 频段挂载高增益数字天线，供 Precompute 计算正常的 **中继 ↔ Mouth** 链路预算。
2. **网络更新完成** — 扫描绕飞中继，为符合条件的定向天线对注入 **A 口 ↔ B 口** 隧道跳路。

中继须满足：绕飞虫洞天体、通信有电、通过轨道约束、至少一根在线的 **定向** `RealAntennaDigital`，且该天线在 RA 最大指向损耗范围内 **物理对准** 本侧 Mouth。RA 目标应为该 CB 上已勘测的 Body Lat/Lon/Alt（手动填写或通过 **虫洞口瞄准** 设置）。

每个方向的隧道 **dataRate** 取该侧两段中继 ↔ Mouth Precompute 速率的最小值（源中继 → 其 Mouth，目标 Mouth → 目标中继）。隧道 **metric** 取两段 Mouth 链路 metric 的较弱者，再叠加插入损耗与轨道质量惩罚。任一侧缺少 Mouth 预算时，该天线对不会注入隧道。

Mouth 未勘测前，代理节点使用朝向母星的临时经纬度估计；GRAVMAX 发现后坐标写入存档并固定不变。

## 依赖

- [RealAntennas](https://github.com/KSP-RO/RealAntennas) 2.x
- [Kopernicus Expansion Continued](https://github.com/VabienArt/KopernicusExpansion-Continueder)（KEX-Wormholes）

**可选：** [Kerbalism](https://github.com/Kerbalism/Kerbalism) 且启用 **FeatureScience** — 在 GRAVMAX 上追加 Kerbalism 实验条目，与 stock WSB 扫描并存。隧道链路与 stock 科学不依赖 Kerbalism。

## 虫洞中继部署

每个虫洞口（KEX 配对的 `body` / `partner` 天体）各需**一艘绕飞该天体的常驻中继**，且满足：

- 至少一根启用的 **定向 RA 天线**（`ModuleRealAntenna`），用 RA **Body Lat/Lon/Alt** 瞄准本侧虫洞 **Mouth**（手动填入经纬高，或勘测后使用 **虫洞口瞄准**）。**全向天线不能建立虫洞隧道**
- 碟面须在 RA 最大指向损耗范围内 **物理对准** Mouth，不能仅填写坐标
- 两端中继使用**相同频段**（RFBand），符号率范围互相兼容
- 推荐另配其它天线，负责 KSC / 本星系深空网回传

隧道只在 **A 口中继 ↔ B 口中继** 之间建立；其他飞船经 RA 路径搜索多跳接入：

- 去远方：`Kerbin 探测器 → … → A 口中继 → [隧道] → B 口中继 → … → 目标`
- 回 Kerbol：`Kcalbeloh 探测器 → B 口中继 → [隧道] → A 口中继 → … → KSC`

本地/回传链路仍按 RA 正常物理计算（真实距离、遮挡、指向）。B 口的全向不会「找到」 A 口中继；两端都需兼容的定向天线，指向本侧虫洞 **Mouth**。

| 位置 | 定向（隧道） | 其它天线（本地/回传） |
|------|-------------|----------------------|
| **A 口**（如 `WH3141A`，默认绕 Jool） | `WH3141A` 上 Mouth（Body Lat/Lon/Alt） | KSC / Kerbol 深空网 |
| **B 口**（如 `WH3141B`，绕 Kcalbeloh 内恒星） | `WH3141B` 上 Mouth（Body Lat/Lon/Alt） | 本星系其他中继 / 行星网 |

Mod 会自动识别所有 KEX 虫洞——**无需**手动维护虫洞列表。

## 发现虫洞口（GRAVMAX）

WSB 在 stock **GRAVMAX**（`sensorGravimeter`）部件上**追加**第二种科学实验：**虫洞口负引力子共振扫描**。原有的 **记录重力数据** / Kerbalism `gravityScan` 实验**不会被覆盖**。

1. 环绕虫洞天体（如 `WH3141A`），并处于中继高度带内（高于 KEX 影响区上边界，低于跳跃区上沿）。
2. 在 GRAVMAX 上运行 WSB 共振扫描（纯 stock 科学界面，或启用 Kerbalism FeatureScience 时的实验界面）。
3. 数据采集完成后，WSB 在本存档**永久登记**该 Mouth 的 **Body Lat/Lon/Alt** 坐标（写入存档缓存，**不会**随轨道或时间漂移）；每个 Mouth **首次发现**额外奖励 **75 000** 资金。首次勘测时，水平位置通常落在虫洞 CB **朝向其母天体**（`referenceBody`）的那一侧，例如 `WH3141A` 面向 Jool 的一面——此后该经纬度在本存档中固定不变。
4. 在**定向** `ModuleRealAntenna` 部件的 **RealAntennas** PAW 组（与 **Antenna Targeting** 同组）中使用 **虫洞口瞄准**。至少完成一次 Mouth 勘测后才会出现该按钮。点击后打开 RA 风格的 WSB 窗口，列出已发现 Mouth（如 `WH3141A Mouth`），**手动选择**后仅瞄准**本天线**——不会自动对准，所选 Mouth 会记在部件上。每个 Mouth 须单独勘测；仅当本船环绕该虫洞天体、RA 通信在线且轨道合格时，对应条目才可选。有 Precompute 预算时会显示中继 ↔ Mouth 速率。

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
| **等效距离** | 注入虫洞跳路的标称 RF 路径长度（米）。隧道仍要求两端中继 ↔ Mouth 都已有当前 RA Precompute 预算；任一侧缺少 mouth 预算时，该天线对不会注入虫洞链路。 |
| **插入损耗** | 喉道额外衰减（dB）。主要压低链路 **metric**（连接质量），**不**直接降低 `dataRate`（传输速率）。 |
| **高级轨道约束** | 开启后，在 **H<sub>inf</sub>** 之上再应用高度上沿、轨道质量评分，以及可选的严格 Pe/Ap、倾角、偏心率规则。 |
| **虫洞口最高高度** | 高级模式下超过即拒链。 |
| **最优最高高度** | 推荐高度带上沿（**H<sub>inf</sub>** … 此值内质量 = 1）。 |
| **边缘质量** | 处于允许范围边缘时的质量分（越低惩罚越重）。 |
| **严格近/远点界** | PeA &lt; **H<sub>inf</sub>** 或 ApA &gt; 最高远点高度则拒链。 |
| **偏好 / 严格倾角** | 倾角的软限制或硬限制。 |
| **理想 / 最大偏心率** | 低于理想值满分；超出则软惩罚或硬拒链。 |
| **最低可用轨道质量** | 综合轨道分低于此值则拒链。 |
| **轨道损耗系数** | 轨道质量低于 1 时，对注入隧道 **metric** 施加惩罚的倍率。它不会直接降低 `dataRate`；中继 ↔ Mouth 的速率仍来自 RA Precompute。 |

| 键 | 默认值（普通） | 含义 |
|----|----------------|------|
| `enabled` | `true` | 总开关 |
| `debugLogging` | `false` | 将 Mouth 节点、中继候选、指向检查与注入链路写入 `KSP.log` |

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
- 在难度设置中开启 **调试日志** 可查看中继对失败原因（轨道、指向、频段不匹配、缺少 Mouth 预算等）。
- Mouth 代理节点不可见，仅供 RA Precompute 使用；玩家通过 GRAVMAX 勘测与 **虫洞口瞄准** 与登记坐标交互。
