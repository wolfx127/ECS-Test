# ECS 与 Mono 性能实验总结

**数据**：Unity 6.2.14f1 Editor + URP；Mono / ECS × 10 / 100 / 1,000 / 10,000 敌人；每帧生成 10 个；600 帧 Profiler 采样。  
**口径**：帧时长 = PlayerLoop 均值 + EditorLoop 均值（用 FixedUpdate 步数交叉校验，偏差 ≤5%）；FPS = 1000 / 帧时长。  
**数据文件**：`mono10/100/1000/10000_...csv`、`ecs10/100/1000_...csv`、`ecs10000_10_5100_664.csv`（新导出）。旧的 `ecs10000_10_5120_694.csv` 与 ECS 1,000 完全重复，已弃用。

## 1. 结论摘要

- **10 / 100 规模：ECS 反而更慢。** 帧时长 5.64 / 5.97 ms，Mono 为 5.18 / 4.98 ms；ECS 慢 0.46 / 0.99 ms（+9% / +20%）。原因是 ECS 固定开销：10 个敌人时 Simulation + Presentation + Initialization 已 1.03 ms/帧，而敌人相关系统合计约 0.18 ms。
- **1,000 规模：ECS 略优，主要赢在 GC 和尾部。** 均值 5.95 vs 6.30 ms（ECS 快 5.6%），GC 0.63 vs 9.54 KB/帧（约 15×）；但中位帧 Mono 更低（5.05 vs 5.30 ms），Mono 的均值和尾部被 GC 尖峰拉高（Q3 5.27 vs 4.06 ms，Max 7.89 vs 6.11 ms）。
- **10,000 规模：ECS 明显胜出。** 均值 10.20 ms / 98.0 fps vs Mono 70.64 ms / 14.2 fps，约 **6.9×**；中位帧 10.90 ms / 91.7 fps vs 37.89 ms / 26.4 fps，约 **3.5×**。Mono 的 70.6 ms 还含 4.8k→10k 爬坡，因此 6.9× 是保守估计。
- **GC 差距是数量级**：Mono 10,000 为 741.26 KB/帧（600 帧累计 434.3 MB），ECS 为 0.63 KB/帧，且后者只是编辑器 LiveConversion 基线，不是玩法逻辑。
- **ECS 区块内存约 0.46 KB/实体**：10,000 实体 5,100 KB（已用 4,436 KB，利用率 87.0%），Unused 664 KB。

## 2. 关键数字

| 规模 | Mono 帧时长 / FPS | ECS 帧时长 / FPS | Mono GC | ECS GC |
|---|---|---|---|---|
| 10 | 5.18 ms / 193.2 | 5.64 ms / 177.3 | 0.70 KB/帧 | 0.63 KB/帧 |
| 100 | 4.98 ms / 201.0 | 5.97 ms / 167.6 | 1.08 KB/帧 | 0.63 KB/帧 |
| 1,000 | 6.30 ms / 158.8 | 5.95 ms / 168.2 | 9.54 KB/帧 | 0.63 KB/帧 |
| 10,000 | 70.64 ms / 14.2（含爬坡） | 10.20 ms / 98.0 | 741.3 KB/帧 | 0.63 KB/帧 |

> 中位帧（10,000）：Mono 37.89 ms / 26.4 fps；ECS 10.90 ms / 91.7 fps。均值差距（6.9×）大于中位差距（3.5×），因为 Mono 有 380 ms 级卡顿和生成爬坡。

## 3. 数据有效性

- **新 ECS 10,000 有效**：`ecs10000_10_5100_664.csv`，761 个 marker，首帧 1,098。按 10 个/帧推算，10,000 个需要约 1,000 帧，首帧已过生成期；与 ECS 1,000 有 5,183/17,928 个数值不同，不是重复导出。
- **旧 ECS 10,000 无效**：`ecs10000_10_5120_694.csv` 与 ECS 1,000 的 17,928 个数值完全一致，只有帧号不同；本文不再使用。
- **Mono 10,000 含爬坡**：首帧 483；第 820 帧对象池从 8,191 扩到 10,000（1,808 次 Instantiate），第 1,011 帧出现 557 次掉落物；600 帧里约 517 帧在爬坡，只有最后约 80 帧满规模。70.6 ms/帧是窗口均值，真实稳态可能更差；第 1,009 帧的 380 ms 是满规模信号。
- **帧时长口径**：各配置的固定步数推算与 PlayerLoop + EditorLoop 的偏差在 -1.4%～+5.0%（仅 Mono 10,000 为 -1.4%，其余 +3.2%～+5.0%），说明把两者相加作为帧时长是合理的。Mono 物理 50 Hz，ECS Entities FixedStep 60 Hz，ECS 每秒多做 20% 物理步，这对 ECS 略不利。

## 4. 结果拆解

### 4.1 1,000 规模：ECS 赢在 GC 和尾部

| 配置 | 物理+逻辑/ECS系统 | 渲染 | 编辑器 | 其他 | 总计 |
|---|---|---|---|---|---|
| Mono 1,000 | 0.91 | 1.85 | 2.59 | 0.95 | 6.30 |
| ECS 1,000 | 1.43 | 1.71 | 2.41 | 0.39 | 5.95 |

Mono 的“物理+逻辑”由 PhysX 0.41 ms、FixedUpdate 脚本 0.39 ms、Update 脚本 0.11 ms 组成；ECS 的 Simulation 0.70 ms、Presentation 0.52 ms、Initialization 0.21 ms，其中 EntitiesGraphics 0.34 ms、Unity Physics 0.25 ms。两者帧时长接近，但 Mono 每帧 9.5 KB 的 GC 造成了周期性尖峰。

### 4.2 10,000 规模：Mono 崩溃，ECS 进入百帧

| 配置 | 物理+逻辑/ECS系统 | 渲染 | 编辑器 | 其他 | 总计 |
|---|---|---|---|---|---|
| Mono 10,000 | 58.98 | 3.40 | 5.56 | 2.70 | 70.64 |
| ECS 10,000 | 4.39 | 2.23 | 3.11 | 0.47 | 10.20 |

Mono 10,000 的 70.6 ms 中：PhysX 30.02 ms（42.5%）、FixedUpdate 脚本 24.65 ms（34.9%）、Update 四叉树 4.31 ms（6.1%）、渲染仅 3.40 ms（4.8%）。渲染不是瓶颈。

ECS 10,000 的 10.2 ms 中：UpdateScene 4.86 ms（Simulation 3.44 + Presentation 0.72 + Initialization 0.23）、渲染 2.23 ms、编辑器 3.11 ms。Simulation 内部：物理 1.59 ms、ECSFrame 系统 1.68 ms、等待 Job 2.73 ms（三者是同一 Simulation 内的不同观察口径，有重叠，不能相加）。**ECS 的瓶颈已从“每实体主线程调用”变成“Job 等待 + 少量系统热点”**；其中 PlayerCatchPropSystem 就占 1.40 ms，是 10,000 时最大的 ECSFrame 系统（和右键掉落物数量有关）。

### 4.3 GC 与内存

| 规模 | Mono GC(MB/600f) | Mono KB/帧 | ECS KB/帧 |
|---|---|---|---|
| 10 | 0.41 | 0.70 | 0.63 |
| 100 | 0.63 | 1.08 | 0.63 |
| 1,000 | 5.59 | 9.54 | 0.63 |
| 10,000 | 434.3 | 741.3 | 0.63 |

Mono 10,000 的 434 MB 中，433.96 MB 挂在 `MonoManager.Update` 下，`FixedUpdate` 为 0。代码层面的原因是 `QuadTreeMgr.UpdateOneEnemy` 每处理一个敌人就 `new Stack<QuadNode>()`（QuadTree.cs 第 53 行），而四叉树每 0.2 s 重建一次；10,000 敌人时单帧 GC.Alloc 峰值 21,215 次，600 帧累计 239 万次分配。

ECS 的 0.37 MB 来自编辑器 SubScene LiveConversion（4,193×644 B），与敌人数无关；稳态玩法逻辑没有托管分配。

ECS 内存：

| 敌人数量 | Allocated | Unused | 已用 | 利用率 |
|---|---|---|---|---|
| 10 | 544 KB | 299 KB | 245 KB | 45.0% |
| 100 | 592 KB | 310 KB | 282 KB | 47.6% |
| 1,000 | 960 KB | 315 KB | 645 KB | 67.2% |
| 10,000 | 5,100 KB | 664 KB | 4,436 KB | 87.0% |

拟合：Allocated ≈ 530.5 KB + 0.4567 KB/实体；10,000 实体约 5.1 MB。Mono 侧 Archetype 读数恒为 480/256，仅 DefaultWorld 基线，不能用于 Mono 内存对比。

## 5. 机理

- **Mono**：10,000 个 GameObject/Rigidbody；每个 fixed step 遍历 10,000 个 `EnemyData`，调用 `MovePosition` 并写四叉树更新缓冲（单步约 7.1 ms）；PhysX 单步约 8.6 ms；低帧率触发平均 3.48 次、最大 17 次补步，17 步约 267 ms，直接把一帧推到 380 ms。GC + 补步 + 物理形成正反馈。
- **ECS**：chunk/SoA 存储、IJobEntity + Burst、Unity Physics、Entities Graphics、ECB；主线程只负责调度与等待，10,000 时 Job 等待约 2.7 ms。ECS 的代价是低规模固定开销（1,000 时 Simulation + Presentation + Initialization 约 1.43 ms/帧），所以在 10/100 规模不如 Mono 的直接循环。
- **比较边界**：PhysX vs Unity Physics、MeshRenderer vs Entities Graphics 同时变化，属于“Mono 技术栈 vs ECS 技术栈”的对比，不是 ECS 架构单变量实验。

## 6. 可以怎么写结论

**推荐写法：**

> 在 1,000 敌人以内，ECS 与 Mono 的帧时间接近：ECS 在 1,000 时均值约快 5.6%，但中位帧略慢；ECS 的主要收益是 GC 低约 15 倍、尾延迟更稳。到 10,000 敌人，ECS 均值 10.2 ms/帧（98 fps），Mono 70.6 ms/帧（14 fps），均值约 6.9×、中位约 3.5×；Mono 的 70.6 ms 还包含生成爬坡，因此 6.9× 是保守估计。ECS 的区块内存约 0.46 KB/实体，10,000 实体约 5.1 MB。需要注意，这是 Mono 技术栈（GameObject + PhysX）与 ECS 技术栈（Entity + Unity Physics + Entities Graphics）的整体对比，不能全部归因于 ECS 架构本身。

**不要写：**

- “ECS 在所有规模都更快” —— 10/100 的数据相反。
- “ECS 10,000 的 6.9× 是纯架构收益” —— 物理引擎和渲染路径同时换了。
- “ECS 内存一定比 Mono 省” —— Mono 的 GameObject 内存没有测。

## 7. 建议补做

1. 每配置跑 3 次，报告 median / p95 / p99 / 1% low；当前是单次采样。
2. 用 Standalone Development Build 去掉 EditorLoop（约 2.4–3.1 ms）和 LiveConversion。
3. Mono / ECS 物理频率对齐（都 50 Hz 或都 60 Hz），或分别报告每次物理步成本。
4. 重录 Mono 10,000 稳态：满规模后再等 600 帧，或把 Profiler 缓冲调到 2,000 帧；记录活跃敌人数。
5. 优化 ECS 的 PlayerCatchPropSystem（10,000 时 1.4 ms）：给掉落物加空间索引/距离剔除，或把拾取合并到已有查询。
