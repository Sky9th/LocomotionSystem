# Editor UI Patterns · RedDust 项目 Editor 规范

> 从 StatsTreeEditorWindow / TagEditorWindow / AbilityEditorWindow 开发过程中提炼的 IMGUI 编辑器 UI 规范。适用于所有 RedDust Editor 窗口。

## 核心原则

1. **卡片即容器** — 所有逻辑区块用 `EditorUIUtility.DrawCard(pad, ...)` 包裹，禁止裸 `EditorStyles.helpBox`
2. **间距统一推导** — 全部间距从 `pad=6` 出发，卡片内边距 = 卡片间隙 = `pad`，不出现魔术数字
3. **宽高外置** — 卡片本身不控制宽高（100% 宽），由外层 `BeginHorizontal(options)` 控制
4. **子卡片单一责任** — 每个子卡片管理自己的四边内边距，间隙由 `EditorUIUtility.CardGap(pad)` 统一

## 空间常量

| 常量 | 值 | 来源 |
|------|-----|------|
| `pad` | 6 | 统一内边距 + 卡片间隙 |
| `foldoutW` | 14 | 折叠三角宽度（叶节点保留为占位对齐） |
| `foldoutGap` | 6 | 折叠区右侧间距 |
| `depthW` | 18 | 树节点每层缩进量 |

## Utility 函数

`EditorUIUtility` 位于 `Assets/Scripts/Shared/Editor/EditorUIUtility.cs`，命名空间 `RedDust.Shared.EditorUI`。

```csharp
// 绘制标准卡片：helpBox + pad 四边内边距
EditorUIUtility.DrawCard(pad, () =>
{
    // 内容 — 自动获得 pad 上/下/左/右内边距
});

// 卡片间隙 — 值应与内边距 pad 一致
EditorUIUtility.CardGap(pad);
```

**内部结构**：
```
BeginVertical(helpBox)
  Space(pad)              ← 上内边距
  BeginHorizontal
    Space(pad)            ← 左内边距
    BeginVertical
      [content]
    EndVertical
    Space(pad)            ← 右内边距
  EndHorizontal
  Space(pad)              ← 下内边距
EndVertical
```

## 间距规则

### 窗口级

```
Window edge
│ Space(pad)     ← 上
│ ┌─ BeginHorizontal ────────────────┐
│ │ Space(pad)   ← 左                │
│ │ content (BeginVertical)          │
│ │ Space(pad)   ← 右                │
│ └──────────────────────────────────┘
│ Space(pad)     ← 下
Window edge
```

窗口边缘间距 = `pad`。窗口内容包在 `BeginHorizontal` + `BeginVertical` 中。

### 卡片级（统一规则）

```
外层 BeginHorizontal(Width, ExpandHeight)    ← 控制卡片尺寸
  DrawCard(pad, () => { content })
EndHorizontal
```

- **所有卡片**走 `DrawCard`，禁止裸 helpBox
- **宽度**：外层 `BeginHorizontal(Width)` 约束；不设 Width = 100%
- **高度**：外层 `BeginHorizontal(ExpandHeight=true)` 撑满
- **卡片间**：`CardGap(pad)` — 间隙值 = 内边距值

### 卡片内行间距

| 位置 | 值 | 语义 |
|------|-----|------|
| 卡片内 Row ↔ Row | `Space(pad)` | 与内边距一致 |
| 卡片 ↔ 卡片 | `CardGap(pad)` | 与内边距一致 |
| 标题 ↔ Scroll | `Space(pad)` | 与内边距一致 |

## 布局模式

### 模式 A：多栏布局

```
BeginHorizontal                  ← 栏容器
  BeginHorizontal(Width, ExpandHeight)  ← 栏 1
    DrawCard(pad, () => { ... })
  EndHorizontal
  Space(pad)                     ← 栏间隙
  BeginHorizontal(ExpandWidth, ExpandHeight)  ← 栏 2
    DrawCard(pad, () => { ... })
  EndHorizontal
EndHorizontal
```

**规则**：
- 每栏 = `BeginHorizontal(尺寸选项)` → `DrawCard` → `EndHorizontal`
- 固定宽用 `Width(...)`，自适应用 `ExpandWidth(true)`
- 栏间隙 = `CardGap(pad)`

### 模式 B：子卡片列表

```
DrawCard(pad, () =>
{
    DrawCard(pad, () => { ... });   ← 子卡片在内层，100% 宽
    CardGap(pad);
    DrawCard(pad, () => { ... });
    CardGap(pad);
    DrawCard(pad, () => { ... });
});
```

**规则**：每个子区块是独立 `DrawCard`，间隙 `CardGap`。嵌套不会影响外层内边距——外层只管自己的 Space(pad)，内层 DrawCard 只加自己的边框和内边距。

### 模式 C：折叠树节点

```
DrawCard(pad, () =>
{
    BeginHorizontal;
      // 折叠区 14+6=20px
      BeginHorizontal(Width(20));
        foldRect(14) 或 dash
        Space(6)
      EndHorizontal;
      // 名称
      BeginVertical(ExpandWidth);
        Space(depth * 18);  ← 文字缩进，非卡片嵌套
        名称 label/button
        [子节点卡片列表 — 模式 B，在 DrawCard 外部]
      EndVertical;
    EndHorizontal;
});
```

**规则**：树节点是独立卡片（`DrawCard`），递归嵌套。缩进在文字层（`Space(depth * 18)`），不靠卡片层级。

## 按钮规范

| 用途 | 样式 | 尺寸 | 颜色 |
|------|------|------|------|
| 主操作（Save） | `GUILayout.Button` | Width(100) | 脏：绿 `(0.4, 0.8, 0.4)` / 净：灰色 disabled |
| 工具栏按钮 | `GUILayout.Button` | Height(24) | 默认 |
| 文字按钮 | `GUILayout.Button(label, labelStyle)` | ExpandWidth | 用 label style 去边框 |
| 添加/操作 | `EditorStyles.miniButton` | Width(28-35) | 默认 |
| 删除 | `EditorStyles.miniButton` + 红底 | Width(20) | `(0.9, 0.3, 0.3)` → 用完恢复 Color.white |

**颜色恢复规则**：修改 `GUI.backgroundColor` 后必须在同一代码块内恢复 `Color.white`。

## 常见陷阱

1. **禁止裸 helpBox** → 必须走 `EditorUIUtility.DrawCard`，避免手动间距不一致
2. **卡片嵌套合法** → 内层 `DrawCard` 不影响外层内边距，各自管理自己的边框和 Space(pad)
3. **禁止 emoji** → Unity IMGUI 默认字体不渲染 emoji，用纯文本
4. **禁止 `Space(2)`, `Space(4)` 等魔术数字** → 统一用 `pad`、`CardGap`
5. **`Foldout` 不支持 `GUILayoutOption`** → 用 `GetRect` + `EditorGUI.Foldout` 固定宽度
6. **`ref` 参数不能进 lambda** → 先捕获到本地变量，lambda 结束后回写
7. **`GUI.backgroundColor` 泄漏** → 修改后立即恢复 `Color.white`
8. **命名空间避免 `*.Editor`** → 与 `UnityEditor.Editor` 类名冲突，用 `*.EditorUI` 等
