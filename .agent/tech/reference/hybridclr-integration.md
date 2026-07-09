# HybridCLR 技术分析

> 面向 RedDust 开发者的选型分析。覆盖原理、特性矩阵、接入步骤、Mod 加载方案。
> 最终结论：社区版满足 RedDust 全部需求，无需商业版。

---

## 一、是什么

HybridCLR（前身 huatuo）是 Code Philosophy 公司开发的 Unity 原生 C# 热更新方案。MIT 许可。

**核心原理**：扩展开源的 IL2CPP 运行时，注入一个 IL 解释器，将 IL2CPP 从纯 AOT 编译器变为 **"AOT + Interpreter"混合运行时**。原生支持 `Assembly.Load(byte[])` 动态加载外部 DLL。

```
原始 IL2CPP:  C# → IL → C++ (AOT) → 无法动态加载
HybridCLR:   C# → IL → C++ (AOT) + IL 解释器 → 可动态加载 Assembly
```

**关键区分**：HybridCLR 是 IL2CPP 的**扩展**，不是替换。原有 AOT 代码按最高性能运行，只有动态加载的 DLL 走解释器。

---

## 二、特性矩阵（社区版 vs 商业版）

| 特性 | 社区版（MIT） | 专业版 | 旗舰版 | RedDust 需要？ |
|------|-------------|--------|--------|---------------|
| `Assembly.Load()` 动态加载 | ✅ | ✅ | ✅ | **是** — Mod 加载的核心机制 |
| 解释执行 DLL | ✅ | ✅ | ✅ | **是** — Mod 代码走解释器 |
| MonoBehaviour 热更新 | ✅ | ✅ | ✅ | 可选 — Mod UI 会用到 |
| ScriptableObject | ✅ | ✅ | ✅ | 可选 |
| DOTS 兼容 | ✅ | ✅ | ✅ | 否 — RedDust 不用 DOTS |
| 泛型 / 反射 | ✅ | ✅ | ✅ | **是** — Mod 作者会用到 |
| async/await | ✅ | ✅ | ✅ | **是** |
| 增量 GC 兼容 | ✅ | ✅ | ✅ | **是** |
| 补充元数据 | ✅ | ✅ | ✅ | **是** — Mod 引用了 AOT 类型就需要 |
| 完全泛型共享 | ❌ | ✅ | ✅ | 不需要 |
| 标准解释性能优化 | ❌ | ✅ (数值指令 280-735%) | ✅ | 不需要 — Mod 不跑热路径 |
| Hotfix（运行时修 Bug） | ❌ | ✅ | ✅ | 不需要 — 单人游戏，重启即可 |
| 热重载（运行时卸载/重加载 Assembly） | ❌ | ❌ | ❌ (热重载版有) | **不需要** — 重启只需 10s |
| DHE 差分混合执行 | ❌ | ❌ | ✅ | 不需要 |
| 代码加密 | ❌ | ✅ | ✅ | 不需要 |
| 价格 | **免费** | 邮件咨询 | 邮件咨询 | — |

**结论**：社区版完全覆盖 RedDust 的 Mod 加载需求。商业版的 Hotfix 和热重载是为 MMO/在线游戏设计的——RedDust 是单人存档游戏，重启 10s 的代价可接受。

---

## 三、架构原理

### 3.1 运行时架构

```
┌─────────────────────────────────────────┐
│              Unity IL2CPP Runtime        │
│                                          │
│  ┌──────────────┐  ┌──────────────────┐ │
│  │  AOT 代码     │  │  Interpreter     │ │
│  │  (原生 C++)   │  │  (HybridCLR 注入) │ │
│  │              │  │                  │ │
│  │  游戏引擎     │  │  Mod DLL         │ │
│  │  官方游戏逻辑 │  │  热更新 DLL      │ │
│  │              │  │                  │ │
│  └──────────────┘  └──────────────────┘ │
│         ▲                  ▲            │
│         │                  │            │
│   编译时 AOT          运行时动态加载     │
└─────────────────────────────────────────┘
```

### 3.2 关键组件

| 组件 | 作用 |
|------|------|
| **IL 解释器** | 寄存器式 IL 解释器。执行动态加载的 DLL 中的方法体 |
| **元数据解析器** | 运行时解析 .NET Assembly 的元数据（类型、方法、字段信息） |
| **元数据动态注册** | 将解析的元数据注入 IL2CPP 的元数据系统，使 AOT 代码能"看到"动态类型 |
| **MethodBridge** | C++ 桥接层。处理 AOT 代码 ↔ 解释器代码之间的方法调用、参数传递 |
| **补充元数据** | 为 AOT 程序集生成额外的类型元数据，确保动态 DLL 能正确引用 AOT 类型 |

### 3.3 加载流程

```
1. 加载补充元数据（AOT 类型的"说明书"）
   RuntimeApi.LoadMetadataForAOTAssembly(metadataBytes)

2. 加载 Mod DLL
   Assembly modAss = Assembly.Load(File.ReadAllBytes(path))

3. AOT 代码通过反射调用 Mod 代码
   Type t = modAss.GetType("MyMod")
   MethodInfo m = t.GetMethod("Run")
   m.Invoke(null, null)

4. 或 Mod 代码直接调用 AOT API
   // Mod DLL 编译时已引用游戏的 AOT 程序集
   // 运行时通过 MethodBridge 无缝调用
```

---

## 四、Unity 版本兼容性

| 项 | RedDust 当前 | HybridCLR 要求 | 状态 |
|----|-------------|---------------|------|
| Unity 版本 | 2022.3.62f3c1 | 2022.3.x LTS ✅ | ✅ 支持 |
| Scripting Backend | IL2CPP | IL2CPP（必须） | ✅ 满足 |
| API Compatibility | .NET Standard 2.1（待确认） | .NET 4.x / .NET Framework | ⚠️ 可能需要调整 |
| 平台 | Windows Standalone | 全 IL2CPP 平台 | ✅ |
| Git | — | 需要安装 | ✅ |
| VS 2019+ | — | 需要 C++ 游戏开发组件 | ✅ |

### 🔒 版本锁定策略

由于团结引擎的关系，RedDust 固定在 Unity 2022.3 非团结版本，不再升级 Unity 大版本。这意味着：

- **HybridCLR 是永久方案**——不会等到 CoreCLR Desktop（需要 Unity 6.7+，无法获取）
- **HybridCLR 版本也应锁定**——选择与 Unity 2022.3 验证充分的 HybridCLR 版本，跟随其补丁更新但不急于升级大版本
- **Unity 2022.3 LTS 已 EoL**（最后补丁 2022.3.62f1，2025 年 5 月）——IL2CPP 层不会有新的 Unity 侧变更，HybridCLR 适配压力更低

### ⚠️ API Compatibility Level

HybridCLR 要求 `.NET Framework`（Unity 2021+）或 `.NET 4.x`（Unity 2019-2020）。如果当前项目用的是 `.NET Standard 2.1`，需要切换。影响：
- 部分 API 可用性变化（.NET Framework 的 API 面比 .NET Standard 2.1 更宽，基本向后兼容）
- 可能需要调整少量 `#if` 条件编译

---

## 五、接入策略

### 5.1 两种接入深度

| | 最小接入（推荐） | 全量热更新 |
|---|---|---|
| **做法** | 保持所有游戏代码为 AOT。只启用 HybridCLR 的 Interpreter 以支持外部 Mod DLL 加载 | 将游戏逻辑拆分为 AOT（引擎）+ HotUpdate（逻辑），内部也用热更新流程 |
| **Mod 支持** | ✅ Mod DLL 走解释器加载 | ✅ 同左 |
| **内部热重载** | ❌ 内部代码改动仍需编译 + 域重载 | ✅ 内部逻辑修改也走热更新，编译 C# → 拷贝 DLL → 无需域重载 |
| **构建复杂度** | +1 步（Generate → All）| + 多步（拆程序集、管理 DLL 版本、调试跨程序集引用） |
| **维护成本** | 低 | 中 |
| **适合** | **独立开发者，核心目标是 Mod 支持** | 多人团队，策划经常需要调逻辑不需要编译 |

### 5.2 推荐：最小接入

RedDust 的核心目标是 Mod 支持。内部开发当前的域重载流程（30s）可接受。最小接入步骤：

```
Step 1: 安装 HybridCLR 包
  Package Manager → Add from git URL →
  https://github.com/focus-creative-games/hybridclr_unity.git

Step 2: 初始化
  HybridCLR → Installer → 点击 Install（~30s）

Step 3: 配置 PlayerSettings
  Scripting Backend = IL2CPP
  Api Compatibility Level = .NET Framework

Step 4: 配置 HybridCLR Settings
  HybridCLR → Settings →
  不需要配置 hotUpdateAssemblies（不留热更新程序集）
  因为 Mod DLL 是外部编译的，不走 Unity 的 asmdef 体系

Step 5: 构建前生成
  HybridCLR → Generate → All
  （生成补充元数据、MethodBridge 等）
  产物输出到 HybridCLRData/AssembliesPostIl2CppStrip/{platform}/

Step 5b: 将产物接入 Addressables 构建
  ⚠️ RedDust 使用 Addressables，不能直接丢 StreamingAssets
  需要将 HybridCLR 生成的补充元数据 DLL 注册为 Addressable 资产：
  ├── 在 HybridCLRData/AssembliesPostIl2CppStrip/ 目录创建 Addressables Group
  ├── 所有 .dll.bytes 文件标记为 "aot-metadata" label
  └── 构建时随 boot 标签一起打包

Step 6: 运行时加载补充元数据
  ⚠️ 不走 File.ReadAllBytes——走 Addressables
  AssetService 加载 "aot-metadata" label → TextAsset[]
  → 逐个调用 RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)

Step 7: Mod 加载
  ModService 扫描 Mods/ 目录
  → Assembly.Load(File.ReadAllBytes(dllPath))  ← 这是文件系统，不走 Addressables
  → 注册 Mod 内容到 GameRegistry
```

**预估工作量**：1-2 session（接入 1 session + 调试和流程固化 1 session）。

---

## 六、Mod C# 加载流程

### 6.1 Mod 作者侧

```
Mod 作者设备上：
1. 创建 C# 类库项目
2. 引用 RedDust 官方发布的 API 程序集 (RedDust.Api.dll)
3. 写 Mod 代码
4. 编译 → MyMod.dll
5. 打包 → MyMod/ 文件夹
   ├── manifest.json
   ├── data/（JSON 数据文件）
   └── mod.dll（C# 代码）
```

### 6.2 游戏侧加载流程

```
游戏启动
  → 0. Addressables.InitializeAsync()（已有——AssetService.EnsureInitialized）

  → 1. 加载补充元数据
       ⚠️ 走 Addressables，不走 StreamingAssets
       AssetService.LoadByLabel<TextAsset>("aot-metadata", assets => {
           foreach (var asset in assets)
               RuntimeApi.LoadMetadataForAOTAssembly(asset.bytes, HomologousImageMode.SuperSet);
       });

  → 2. ModService.Scan()
       扫描 persistentDataPath/Mods/ 和 Workshop 订阅目录
       ← 这是文件系统路径，不是 Addressables

  → 3. 按依赖拓扑排序 Mod 列表

  → 4. 逐个加载 Mod：
       ├── 加载 JSON 数据（items.json → GameRegistry.RegisterItem）
       │   ← 文件系统读取，不走 Addressables（用户内容，构建时不可知）
       ├── 加载 DLL（如果有）
       │   Assembly modDll = Assembly.Load(File.ReadAllBytes(dllPath))
       │   ← 文件系统读取。HybridCLR 解释器执行
       │   遍历 modDll 中的类型
       │   找到 [ModEntry] 标记的类 → 调用 Initialize()
       └── 注册到 ModRegistry（记录加载状态、版本）

  → 5. 发布 SModsLoaded 事件

  → 6. Mod 代码通过反射或接口调用官方 AOT API
       ← 官方代码是 AOT 全速，Mod 代码走解释器
```

### 6.3 RedDust 需要发布的 API 程序集

Mod 作者需要知道游戏暴露了哪些 API。方式：

| 方式 | 说明 | 推荐度 |
|------|------|--------|
| **发布 API 存根 DLL** | 把 `RedDust.Api.dll`（仅 public 接口，无实现）随游戏分发到 `Modding/` 目录 | ⭐⭐⭐ |
| **XML 注释文件** | 随 DLL 发布 `.xml` 文档注释，IDE 可智能提示 | ⭐⭐ |
| **后续 → 公开文档** | 产品战略顾问的四条件满足后，API 稳定锁定，写正式文档 | ⭐ |

API 存根 DLL 的内容 = 所有 Mod 作者可能用到的 `public` 类型和方法签名（从游戏程序集中提取）。这个 DLL 不是给游戏加载的——是给 Mod 作者编译用的。

---

## 六-A、Addressables 集成细节

RedDust 使用 Addressables 管理所有构建时资产。HybridCLR 接入后，加载路径需明确区分：

| 数据类型 | 来源 | 加载方式 | 原因 |
|----------|------|---------|------|
| **补充元数据 DLL** | HybridCLR `Generate → All` 构建时生成 | **Addressables**（label: `aot-metadata`） | 构建时已知，随游戏发布，需版本管理和缓存 |
| **Mod DLL** | 玩家手动安装 / Workshop 订阅 | **文件系统**（`File.ReadAllBytes`） | 运行时动态产生，构建时不可知，不走资源系统 |
| **Mod JSON** | 同上 | **文件系统**（`File.ReadAllText`） | 同上 |

### 补充元数据接入 Addressables

HybridCLR 的 `Generate → All` 在 `HybridCLRData/AssembliesPostIl2CppStrip/{platform}/` 下生成 `.dll` 文件。需要：

1. 重命名为 `.bytes`（Unity 不识别裸 `.dll` 为 TextAsset）
2. 纳入 Addressables 构建——创建专门的 Group（如 `AOTMetadata`）
3. 所有元数据文件打上 `aot-metadata` label
4. 构建顺序：**HybridCLR Generate → 拷贝到 Addressables 目录 → Addressables Build → IL2CPP Build**

### 运行时加载

```csharp
// 通过 Addressables 加载（非 StreamingAssets）
var handle = Addressables.LoadAssetsAsync<TextAsset>(
    "aot-metadata", null, Addressables.MergeMode.Union);
yield return handle;
foreach (var asset in handle.Result)
    RuntimeApi.LoadMetadataForAOTAssembly(asset.bytes, HomologousImageMode.SuperSet);
```

### 为什么不能放 StreamingAssets

RedDust 已经全量使用 Addressables。补充元数据和其他游戏资产走同一套加载、缓存、更新管线。丢进 StreamingAssets = 两套资产管线并存——维护负担翻倍。

### 为什么 Mod DLL 走文件系统

Mod 是运行时用户安装的内容，不在构建时 Addressables catalog 里。`Assembly.Load(byte[])` 不依赖 Unity 资源系统——直接向 HybridCLR 运行时注册 IL 即可。文件系统读取是最短路径。

---

## 七、不支持的特性（社区版）

HybridCLR 近乎完整实现 ECMA-335，**极少限制**。已知不支持的：

| 特性 | 影响 | 缓解 |
|------|------|------|
| `System.Reflection.Emit` | Mod 不能动态生成 IL | 不影响——Mod 作者编译时就生成了 IL |
| 某些 `System.TypedReference` 操作 | 极少场景 | 不影响正常 Mod 开发 |
| 商业版特性（DHE、热重载等） | 见第二节 | 不影响 RedDust |

对 Mod 开发的核心限制：**几乎没有**。泛型、反射、LINQ、async/await、MonoBehaviour、ScriptableObject 全部支持。

---

## 八、风险评估

| 风险 | 等级 | 说明 | 缓解 |
|------|------|------|------|
| **Unity 版本锁定** | 🟢 低 | Unity 2022.3 已 EoL，不再有 IL2CPP 侧变更。HybridCLR 适配压力反而更低——底层不再变动 | 锁定 HybridCLR 版本，仅跟随关键补丁 |
| **构建顺序错误** | 🟡 中 | HybridCLR Generate 必须在 Addressables Build 之前执行 | CI 脚本固化顺序，加 pre-build hook 自动执行 |
| **第三方库依赖** | 🟡 中 | MIT 许可，Code Philosophy 公司维护。已有数千商业项目上线 | 社区版代码开源，极端情况下可自行维护 |
| **Addressables catalog 膨胀** | 🟢 低 | 补充元数据 DLL 加入 Addressables 会增加 catalog 大小（预估 +2-5 MB） | 元数据文件稳定不常变，仅首次下载 |
| **性能** | 🟢 低 | Mod 代码走解释器，比 AOT 慢。但 Mod 不跑热路径 | 游戏引擎和官方逻辑仍然是 AOT 全速 |
| **安全** | 🟡 中 | Mod DLL 可以调用任何 public API，理论上可以做任何事 | 单人游戏无服务器，标注"Mod 代码不受安全审计" |

> ❌ 已移除 CoreCLR 迁移风险——Unity 2022.3 锁定意味着无法升级到 Unity 6.7+ CoreCLR Desktop。HybridCLR 是 RedDust 的永久方案，不是过渡方案。

---

## 九、结论

| 问题 | 答案 |
|------|------|
| 社区版够用吗？ | ✅ 够用。Assembly.Load() + 解释执行在社区版中完整支持 |
| 需要商业版吗？ | ❌ 不需要。商业版的 Hotfix/热重载是为 MMO 设计的 |
| 接入难度？ | 🟢 低。1-2 session。主要是安装 + 配置 + 生成管线 |
| 对 Mod 作者的限制？ | 几乎没有。C# 泛型/反射/async/LINQ 全支持 |
| 是过渡方案吗？ | ❌ 不是。Unity 2022.3 已锁定，HybridCLR 是**永久方案** |

**接入方向：最小接入——保持所有游戏代码 AOT，启用 HybridCLR 解释器仅用于 Mod DLL 加载。内部开发仍走 Unity 域重载流程。**

**版本策略：Unity 2022.3 + HybridCLR 均锁定，不追新。仅跟随关键补丁。**

---

## 关联文档

- [mod-community-decision-record.md](../../plans/mod-community-decision-record.md) — 战略决策记录（决策〇）
- [mod.md](../../design/systems/mod.md) — Mod 系统策划文档
- [mod-json-reference.md](../../design/systems/mod-json-reference.md) — Mod JSON 格式手册
- [HybridCLR 官方文档](https://www.hybridclr.cn/docs/intro)
- [HybridCLR GitHub](https://github.com/focus-creative-games/hybridclr)
