# Localization — 多语言架构

> **官方语言**：简体中文 (zh-CN) + 英文 (en)
> **社区翻译**：通过外部化字符串文件支持任意语言
> **核心原则**：代码中不硬编码任何面向玩家的字符串

---

## 一、设计原则

| 原则 | 说明 |
|------|------|
| **字符串外部化** | 所有玩家可见文本存储在独立文件中，代码通过 Key 引用 |
| **标准格式** | 使用 JSON（社区译者无需专用工具） |
| **按域分割** | 不同模块的字符串分文件存储，降低社区翻译门槛 |
| **Fallback 链** | 请求语言缺失 → en → zh-CN → 原始 Key 展示 |
| **字体先行** | 默认字体必须同时覆盖 CJK + Latin 字符集 |

---

## 二、文件结构

```
Assets/Resources/Locales/
├── zh-CN/                       ← 官方中文（开发主语言）
│   ├── ui.json                  ← 通用 UI 文本
│   ├── items.json               ← 物品名称与描述
│   ├── abilities.json           ← 技能名称与描述
│   ├── entities.json            ← 角色/NPC/敌人名称
│   ├── buildings.json           ← 建筑名称与描述
│   ├── dialogue.json            ← 对话文本
│   ├── tutorials.json           ← 教程/提示
│   └── system.json              ← 系统消息/通知
│
├── en/                          ← 官方英文
│   ├── ui.json
│   ├── items.json
│   ├── ...（与 zh-CN 同结构）
│
└── community/                   ← 社区翻译（Steam Workshop）
    ├── ja/                      ← 例：日语
    │   ├── ui.json
    │   └── ...
    └── ...
```

**规则**：
- `zh-CN/` 和 `en/` 随游戏本体发布，打包在 `Resources` 中
- `community/` 目录在运行时从 `Application.persistentDataPath` 加载，优先于打包资源
- 社区翻译者只需复制 `en/` 目录，修改 JSON 的 value 即可

---

## 三、字符串 Key 规范

使用点分层次命名：`{domain}.{category}.{key}`

| Domain | 内容 | Key 示例 |
|--------|------|----------|
| `ui` | 通用 UI | `ui.hud.health_label`, `ui.menu.save_game` |
| `item` | 物品 | `item.bandage.name`, `item.bandage.desc` |
| `ability` | 技能 | `ability.blade.light_cut.name` |
| `entity` | 角色/敌人 | `entity.zombie_runner.name` |
| `building` | 建筑 | `building.wood_wall.name` |
| `dialogue` | 对话 | `dialogue.npc_recruit.greeting` |
| `tutorial` | 教程 | `tutorial.first_night.tip` |
| `system` | 系统通知 | `system.save_success`, `system.horde_warning` |

**命名规则**：
- 全小写，单词用下划线分隔
- 从通用到具体排列
- 避免缩写（`desc` 而非 `dscrp`）

---

## 四、JSON 格式

```json
{
  "locale": "en",
  "version": 1,
  "entries": {
    "item.bandage.name": "Bandage",
    "item.bandage.desc": "A simple cloth bandage. Stops minor bleeding.",
    "ui.hud.health_label": "HP",
    "ui.save.confirm_overwrite": "Overwrite existing save?"
  }
}
```

- 每个文件一个 JSON 对象，`entries` 字段内是扁平 Key-Value 映射
- 不嵌套——扁平结构便于 diff 和社区翻译工具
- `version` 字段用于将来迁移时检测过期翻译

---

## 五、Fallback 链

```
请求 "ja" (日语)
  ├── 查找 community/ja/items.json       ← 社区翻译
  │   └── 缺失 → 
  ├── 查找 en/items.json                 ← 官方英文
  │   └── 缺失 →
  ├── 查找 zh-CN/items.json              ← 官方中文（最终 Fallback）
  │   └── 缺失 →
  └── 返回原始 Key: "item.xxx.name"      ← 防止崩溃，同时暴露未翻译项
```

---

## 六、字体方案

中英双语需要统一字体覆盖 CJK + Latin：

| 方案 | 字体 | 说明 |
|------|------|------|
| **UGUI 正文** | SDF 字体图集（思源黑体 Source Han Sans） | Google/Adobe 开源，同时覆盖简繁中文 + 日文假名 + 拉丁字符 |
| **UGUI 标题** | SDF 字体图集（独立加粗版本） | 同一字体家族 |
| **数字/等宽** | SDF 字体图集（独立等宽版本） | 属性面板数值对齐 |

> SDF (Signed Distance Field) 使一个字体图集可以渲染任意大小文字，避免每个字号打包一套字体。

---

## 七、社区翻译工作流

1. 社区译者从 Steam Workshop 或 GitHub 下载 `en/` 模板
2. 将 `en/` 复制为 `{locale}/`（如 `ja/`、`ko/`、`fr/`）
3. 逐文件翻译 value，Key 保持不变
4. 放入 `{persistentDataPath}/Locales/community/{locale}/`
5. 游戏内语言选择菜单自动检测 `community/` 下的可用语言
6. 翻译更新时只需替换对应 JSON 文件

**工具支持**：JSON 格式可直接用任何文本编辑器打开，也可用 POEditor、Weblate 等在线翻译平台导入。

---

## 八、开发约定

| 规则 | 说明 |
|------|------|
| **禁止硬编码** | 所有面向玩家的字符串必须用 Key 引用，不得直接写中文或英文 |
| **先加 Key 再写逻辑** | 新增功能时，先在对应 JSON 中添加 Key 和 zh-CN 值，再在代码中使用 |
| **Key 不承载逻辑** | 代码不得解析 Key 字符串做判断——Key 只是查找键 |
| **format 用模板** | 带参数的文本用 `{0}`, `{1}` 占位：`"You dealt {0} damage to {1}"` |

---

## 九、编辑器支持

- Editor 工具可扫描所有 JSON 中未翻译的 Key（en 有而 zh-CN 无，反之亦然）
- 运行时 Debug 模式可在屏幕上显示当前文本的 Key（方便定位）
- 构建时检查：CI 中确认 `zh-CN/` 和 `en/` 的 Key 集合完全一致

---

## 十、关联文档

- `../design/game-overview.md` — GDD 总览，多语言需求入口
- `L2-services/L2-ui/` — UI 服务，字符串消费方
