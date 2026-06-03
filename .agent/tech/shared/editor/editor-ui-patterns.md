# Editor UI Patterns · RedDust 项目 Editor 规范

> 从 StatsTreeEditorWindow 开发过程中提炼的 IMGUI 编辑器 UI 规范。适用于所有 RedDust Editor 窗口。

## 核心原则

1. **卡片即容器** — 所有逻辑区块用 `EditorStyles.helpBox` 包裹
2. **间距统一推导** — 全部间距从 `pad=6` 出发，不出现魔术数字
3. **左右分块布局** — 左自动撑满，右固定宽度，`FlexibleSpace` 隔开

## 空间常量

| 常量 | 值 | 来源 |
|------|-----|------|
| `pad` | 6 | 统一内边距（卡片内上/下/左/右） |
| `toggleW` | 14 | IsEnabled 开关宽度 |
| `foldoutW` | 14 | 折叠三角宽度（叶节点保留为占位对齐） |
| `textPad` | 4 | LabelField 补偿偏移 |
| `childIndent` | 20 | 子节点缩进量 |

## 间距规则

### 窗口级

```
Window edge
│ Space(pad)     ← 上
│ ┌─ BeginHorizontal ────────────────┐
│ │ Space(pad)   ← 左                │
│ │ content                          │
│ │ Space(pad)   ← 右                │
│ └──────────────────────────────────┘
│ Space(pad)     ← 下
Window edge
```

窗口边缘与首/末卡片间距 = `pad`。整个窗口内容包在 `BeginHorizontal` + `BeginVertical` 中统一控制。

### 卡片级

```
┌─ BeginVertical(helpBox) ──────────┐
│ Space(pad)              ← 上       │
│ ┌─ BeginHorizontal ──────────────┐ │
│ │ Space(pad)    ← 左             │ │
│ │ ┌─ BeginVertical(ExpandWidth) ┐│ │
│ │ │ content                     ││ │
│ │ └─────────────────────────────┘│ │
│ │ Space(pad)    ← 右             │ │
│ └────────────────────────────────┘ │
│ Space(pad)              ← 下       │
└────────────────────────────────────┘
```

**关键**：卡片内部再包一层 `BeginHorizontal` + `BeginVertical(ExpandWidth)` 来管理左右 padding，而不是靠外层。这样缩进层次清晰，嵌套卡片也能复用。

### 卡片间距

| 位置 | 值 | 语义 |
|------|-----|------|
| 卡片 ↔ 卡片（同组兄弟） | `Space(2)` | 紧凑，表示归属关系 |
| 卡片 ↔ 卡片（跨组/根级） | `Space(pad)` | 与内部 padding 一致 |
| 卡片内 Row ↔ Row | `Space(2)` | 行间紧凑 |

## 布局模式

### 模式 A：左右分块（Header）

```
BeginHorizontal
  Space(pad)                    ← 左内边距
  BeginVertical(ExpandWidth)    ← 左块，自动撑满
    Row 1
    Row 2
  EndVertical
  Space(pad)                    ← 左右块间隙
  Button(Width:100)             ← 右块，固定宽度
  Space(pad)                    ← 右内边距
EndHorizontal
```

**规则**：
- 左块 `ExpandWidth(true)`，右块固定宽度
- 不加 `FlexibleSpace`——左 ExpandWidth 自然把右块推到右端
- 左块的子控件也需 `ExpandWidth(true)` 才能撑满

### 模式 B：左右分块（Toolbar / Folder Row）

```
BeginHorizontal
  Space(pad)                    ← 左内边距
  [左按钮 / 标签 ...]           ← 左对齐块
  FlexibleSpace                 ← 隔开左右
  [右按钮 ...]                  ← 右对齐块
  Space(pad)                    ← 右内边距
EndHorizontal
```

**适用**：左右两侧都是固定宽度控件，中间 `FlexibleSpace` 隔开。

### 模式 C：折叠区 + 内容区（Folder Card）

```
BeginHorizontal
  Space(pad)
  GetRect(35, lineHeight)       ← 折叠按钮固定 35px
  EditorGUI.Foldout(foldRect)
  Space(4)                      ← textPad
  BeginVertical(ExpandWidth)    ← 内容区，与折叠按钮右侧对齐
    [toolbar row]               ← 模式 B
    Space(2)
    [子卡片列表]                 ← 模式 D
  EndVertical
  Space(pad)
EndHorizontal
```

**规则**：
- 折叠按钮用 `GUILayoutUtility.GetRect(35f, ...)` + `EditorGUI.Foldout` 固定宽度
- 内容区是独立 Vertical，天然与上层文字对齐
- 子卡片无需手动计算缩进——在 Vertical 内部自然对齐

### 模式 D：子卡片列表（Leaf List）

```
BeginVertical(helpBox)          ← 列表容器卡片
  Space(pad)
  BeginHorizontal
    Space(pad)
    BeginVertical(ExpandWidth)
      DrawChildCard()           ← 复用卡片级模式
      Space(2)
      DrawChildCard()
    EndVertical
    Space(pad)
  EndHorizontal
  Space(pad)
EndVertical
```

**规则**：与父卡片结构完全一致，递归嵌套。

## 按钮规范

| 用途 | 样式 | 尺寸 | 颜色 |
|------|------|------|------|
| 主操作（Save） | `GUILayout.Button` | Width(100) | 脏：绿 `(0.4, 0.8, 0.4)` / 净：灰色 disabled |
| 工具栏按钮 | `GUILayout.Button` | Height(24) | 默认 |
| 添加/操作 | `EditorStyles.miniButton` | Width(20) | 默认 |
| 删除 | `EditorStyles.miniButton` + 红底 | Width(20) | `(0.9, 0.3, 0.3)` → 用完恢复 Color.white |
| 清除值 | `EditorStyles.miniButton` | Width(20) | 默认 （`↺` 符号，非破坏性） |

**颜色恢复规则**：修改 `GUI.backgroundColor` 后必须在同一代码块内恢复 `Color.white`，否则后续控件全部染色。

## 叶节点卡片

单行布局，通过 foldout 占位与文件夹对齐：

```
┌─ leaf helpBox ───────────────────────────────────────┐
│ Space(6)                                              │
│ [6] [✓14] [6] [空14] [4] [Def ▲] Flex [50] [↺][✕紅][6]│
│ Space(6)                                              │
└──────────────────────────────────────────────────────┘
```

| 元素 | 控件 | 宽度 |
|------|------|------|
| IsEnabled | `Toggle("", true, Width(14))` | 14 |
| foldout 占位 | `Space(14)` | 14 — 保证与文件夹文字对齐 |
| textPad | `Space(4)` | 4 |
| Def 字段 | `ObjectField(null, ...)` | ExpandWidth |
| Val 数值 | `FloatField(..., Width(50))` | 50 |
| 清除覆盖 | `miniButton("↺", Width(20))` | 20 |
| 删除 | `miniButton("✕", Width(20))` 红底 | 20 |

## 常见陷阱

1. **`Foldout` 不支持 `GUILayoutOption`** → 用 `GetRect` + `EditorGUI.Foldout` 固定宽度
2. **`Toggle(bool, GUILayoutOption)` 重载冲突** → 用 `Toggle("", bool, GUILayoutOption)` 带空 label
3. **`ExpandWidth` 与 `FlexibleSpace` 冲突** → 模式 A 用 ExpandWidth 自然推右，模式 B 用 FlexSpace
4. **`GUI.backgroundColor` 泄漏** → 修改后立即恢复
5. **卡片内容贴边** → 卡片内部必须再包 Horizontal+Vertical 做 padding
6. **`EditorGUILayout.BeginVertical(helpBox)` 不会自动 ExpandWidth** → 嵌套在 Horizontal 中需显式传 `GUILayout.ExpandWidth(true)`
