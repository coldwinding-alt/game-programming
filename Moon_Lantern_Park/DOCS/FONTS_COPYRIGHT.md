# 字体版权与替换说明

更新日期：`2026-06-04`

本文件记录项目当前打包字体的版权状态，以及 `2026-06-04` 这次清理中实际采用的替换方案。

## 1. 已直接使用官方 Google Fonts 文件的字体

| 当前文件 | 实际字体 | 来源 | 许可 |
| --- | --- | --- | --- |
| `Rajdhani-Bold.ttf` | `Rajdhani-Bold` | <https://github.com/google/fonts/tree/main/ofl/rajdhani> | `SIL Open Font License 1.1` |
| `Rajdhani-SemiBold.ttf` | `Rajdhani-SemiBold` | <https://github.com/google/fonts/tree/main/ofl/rajdhani> | `SIL Open Font License 1.1` |
| `Griffy-Regular.ttf` | `Griffy-Regular` | <https://github.com/google/fonts/tree/main/ofl/griffy> | `SIL Open Font License 1.1` |

## 2. 已完成替换的高风险/来源不明字体

为避免大范围修改 Unity 资源路径、Prefab 引用和运行时代码，以下路径保留了原文件名，但文件内容已经替换为来源明确、允许再分发的官方 Google Fonts 文件。

| 兼容文件名 | 原风险 | 现已替换为 | 官方来源 | 许可 | 备注 |
| --- | --- | --- | --- | --- | --- |
| `Impact.ttf` | 原先对应商业字体 `Impact`，再分发存在许可风险 | `Anton-Regular.ttf` | <https://github.com/google/fonts/tree/main/ofl/anton> | `SIL Open Font License 1.1` | 保留原资源名仅为兼容现有 Unity 路径 |
| `Impact2.ttf` | 原先对应 `Impact` 变体，来源与再分发风险不清 | `Anton-Regular.ttf` | <https://github.com/google/fonts/tree/main/ofl/anton> | `SIL Open Font License 1.1` | 保留原资源名仅为兼容现有 Unity 路径 |
| `AgencyBold.ttf` | 原先对应商业字体 `Agency FB / Agency Bold` | `BarlowCondensed-Bold.ttf` | <https://github.com/google/fonts/tree/main/ofl/barlowcondensed> | `SIL Open Font License 1.1` | 风格接近的窄体粗字替换 |
| `CfCrackBold.ttf` | 原先来源不明，许可状态无法确认 | `Bungee-Regular.ttf` | <https://github.com/google/fonts/tree/main/ofl/bungee> | `SIL Open Font License 1.1` | 用于保持强显示感和可读性 |

## 3. 为什么保留 Impact / AgencyBold 等旧文件名

Unity 项目中，资源文件名与以下内容直接绑定：

- **Resources.Load 路径**：运行时代码通过 `Resources.Load<Font>("mlp/Fonts/Impact")` 等字符串加载字体，改文件名意味着全项目搜索替换这些路径。
- **Prefab 序列化引用**：`.prefab` 文件内部以 GUID + 文件名引用字体资产，批量改名会导致大量 Prefab 需要重新序列化或手动修复引用。
- **TMP_FontAsset 依赖链**：TextMeshPro 的 SDF 字体资产（`TMP/Impact2 SDF.asset` 等）内部记录了源字体路径，改名后需要同步重建所有 SDF 资产。

综合评估后，保留旧文件名是风险最低的方案：文件内容已经替换为安全的开源字体，功能和视觉效果不受影响，同时避免了大规模资源迁移带来的引用断裂风险。

## 4. 当前结论

当前仓库中打包给运行时使用的字体，已经没有必须继续保留的商业字体二进制，也没有来源不明的字体二进制。

需要注意的是：

- 某些 Unity 资源文件名仍然沿用历史命名，例如 `Impact.ttf`、`AgencyBold.ttf`。
- 这些文件名是兼容层，不代表其内部仍然是原商业字体。文件名与文件内容的对应关系见上方第 2 节表格。
- 实际来源、哈希、上游页面和本地 `OFL` 文本，统一记录在 `DOCS/FONT_PROVENANCE.md` 与 `DOCS/FontLicenses/`。

## 5. 本地许可文本

项目已将相关许可文本保存到以下目录：

- `DOCS/FontLicenses/Anton-OFL.txt`
- `DOCS/FontLicenses/BarlowCondensed-OFL.txt`
- `DOCS/FontLicenses/Bungee-OFL.txt`
- `DOCS/FontLicenses/Rajdhani-OFL.txt`
- `DOCS/FontLicenses/Griffy-OFL.txt`

## 6. 课程提交时可使用的表述

可以安全写成：

> 项目当前打包字体全部具备可追溯来源。`Rajdhani`、`Griffy` 直接使用官方 Google Fonts 文件；历史兼容路径 `Impact`、`Impact2`、`AgencyBold`、`CfCrackBold` 已分别替换为官方 Google Fonts 的 `Anton`、`Barlow Condensed Bold` 和 `Bungee`，并在仓库中保留了上游链接、哈希记录和 OFL 许可文本。
