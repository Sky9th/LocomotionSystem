# Editor UI Design Tokens · RedDust 项目 Editor 视觉标准

> 基底取自 Unity Editor Professional (Dark) 主题，颜色搭配/尺寸层级/间距比例参考 Element Plus。适用于所有 RedDust Editor 窗口的 USS 样式与视觉设计。与 [components.md](components.md) 互补——components 管 IMGUI 组件与布局，design-tokens 管 UI Toolkit 视觉令牌。

---

## 设计原则

1. **基底融入 Unity** — 面板背景、文字层级、边框色调沿用 Unity Professional 深色主题，确保工具窗口在 Editor 中不突兀
2. **语义色体系来自 Element** — primary / success / warning / danger / info 五色体系 + 9 级亮色 / 1 级暗色混色变体
3. **尺寸层级来自 Element** — L / M / S 三档贯穿所有控件，但基准收敛到 Unity 的紧凑密度
4. **间距比例来自 Element** — 内边距与控件高度的比例关系参考 Element Plus

---

## 1. 基底色板 (Base Palette) — 来自 Unity Professional

### 1.1 背景层级

| Token | 色值 | 来源 | 用途 |
|-------|------|------|------|
| `bg-canvas` | `#191919` | Unity `app_toolbar-background` | 最底层画布（主工具栏级） |
| `bg-page` | `#282828` | Unity `default-background` | 页面/默认背景 |
| `bg-panel` | `#383838` | Unity `window-background` | 面板/窗口背景 |
| `bg-overlay` | `#313131` | Unity `popup-background` | 浮层/弹出背景 |
| `bg-input` | `#2A2A2A` | Unity `input_field-background` | 输入控件背景 |
| `bg-button` | `#585858` | Unity `button-background` | 按钮默认背景 |
| `bg-toolbar` | `#3C3C3C` | Unity `toolbar-background` | 工具条背景 |
| `bg-tab` | `#353535` | Unity `tab-background` | 标签页背景 |
| `bg-tab-active` | `#3C3C3C` | Unity `tab-background-checked` | 标签页选中态 |
| `bg-headerbar` | `#3C3C3C` | Unity `headerbar-background` | 列标题/分类栏背景 |
| `bg-hover` | `rgba(255,255,255,0.06)` | Unity `highlight-background-hover` | 通用悬停高亮 |
| `bg-selected` | `#2C5D87` | Unity `highlight-background` | 选中态背景 |
| `bg-selected-inactive` | `#4D4D4D` | Unity `highlight-background-inactive` | 失焦选中态 |

### 1.2 文字层级

| Token | 色值 | 来源 | 用途 |
|-------|------|------|------|
| `text-primary` | `#D2D2D2` | Unity `default-text` | 主要文字 / 标题 |
| `text-regular` | `#C4C4C4` | Unity `label-text` | 常规正文 / 标签 |
| `text-secondary` | `#BDBDBD` | Unity `window-text` | 次要文字 / 辅助信息 |
| `text-placeholder` | `#8A8A8A` | 推算 (text-primary + 降低对比) | 占位符 / 提示文字 |
| `text-disabled` | `#6E6E6E` | Unity `button-background-focus` 降低 | 禁用态文字 |
| `text-link` | `#4C7EFF` | Unity `link-text` | 链接文字 |
| `text-link-visited` | `#FF00FF` | Unity `visited_link-text` | 已访问链接 |

### 1.3 边框层级

| Token | 色值 | 来源 | 用途 |
|-------|------|------|------|
| `border-default` | `#232323` | Unity `default-border` | 默认边框 |
| `border-light` | `#303030` | Unity `button-border` | 浅边框（面板间分隔） |
| `border-lighter` | `#242424` | Unity `window-border` | 更浅边框 |
| `border-input` | `#212121` | Unity `input_field-border` | 输入框边框 |
| `border-focus` | `#3A79BB` | Unity `input_field-border-focus` | 焦点边框 |
| `border-accent` | `#0D0D0D` | Unity `input_field-border_accent` | 强调边框（立体感底部） |

> 基础边框: `1px solid var(--border-default)`

### 1.4 背景填充辅助色 (Fill / Striping)

| Token | 色值 | 来源 | 用途 |
|-------|------|------|------|
| `fill-default` | `rgba(40,40,40,0.30)` | Unity `box-background` | 默认填充区域 |
| `fill-alternate` | `#3F3F3F` | Unity `alternated_rows-background` | 交替行背景（列表斑马纹） |
| `fill-hover` | `#5F5F5F` | Unity `highlight-background-hover-lighter` | 列表项悬停 |
| `fill-helpbox` | `rgba(96,96,96,0.20)` | Unity `helpbox-background` | 帮助提示框背景 |

---

## 2. 语义色系统 (Semantic Colors) — Element Plus 体系 + 暗色适配

Element Plus 的 5 类语义色 + 9 级亮色 / 1 级暗色混色体系，基准色针对 Unity 深色背景做了适配。

### 2.1 基准色

| 类型 | 基准色 | 来源 | 说明 |
|------|--------|------|------|
| `primary` | `#4C7EFF` | Unity `highlight-text` / `link-text` | 主色：Unity 编辑器蓝，比 Element `#409EFF` 更亮，适配深色底 |
| `success` | `#67C23A` | Element Plus | 成功绿：在深色底上对比度足够 |
| `warning` | `#E6A23C` | Element Plus | 警告橙：暗底可见 |
| `danger` | `#D32222` | Unity `error-text` | 危险红：Unity 的深红比 Element `#F56C6C` 在暗底上更沉稳 |
| `info` | `#A8B2BF` | 推算 | 信息灰蓝：Element `#909399` 在暗底对比度不足，提亮适配 |

### 2.2 色阶变体 (Light / Dark Mix)

沿用 Element Plus 的混色策略——每个语义色自动生成 **9 级亮色**（混白）和 **1 级暗色**（混黑）：

| 变体 | 混合比例 | 典型用途 |
|------|----------|----------|
| `light-1` | 10% 白 + 90% 基色 | 极少用 |
| `light-2` | 20% 白 + 80% 基色 | |
| `light-3` | 30% 白 + 70% 基色 | link hover 态 |
| `light-4` | 40% 白 + 60% 基色 | |
| `light-5` | 50% 白 + 50% 基色 | outline / 浅色边框 |
| `light-6` | 60% 白 + 40% 基色 | |
| `light-7` | 70% 白 + 30% 基色 | hover border |
| `light-8` | 80% 白 + 20% 基色 | |
| `light-9` | 90% 白 + 10% 基色 | hover 背景 / tag 背景 |
| `dark-2` | 20% 黑 + 80% 基色 | active / pressed 态 |

### 2.3 Primary 色阶参考值

以 `#4C7EFF` 为基准，各级混合近似值：

| 变体 | 近似色值 | 用途示例 |
|------|----------|----------|
| `primary` | `#4C7EFF` | 主按钮、焦点边框、选中文字 |
| `primary-light-3` | `#7DA8FF` | 链接 hover |
| `primary-light-5` | `#A6C4FF` | 浅色强调边框 |
| `primary-light-7` | `#CEDFFF` | 按钮 hover 边框 |
| `primary-light-9` | `#EDF2FF` | 按钮 hover 背景、选中行背景 |
| `primary-dark-2` | `#3065CC` | 按钮 active/pressed |

### 2.4 Danger 色阶参考值

以 `#D32222` 为基准：

| 变体 | 近似色值 | 用途示例 |
|------|----------|----------|
| `danger` | `#D32222` | 危险按钮、错误文字 |
| `danger-light-9` | `#FBE9E9` | 错误提示背景 |
| `danger-dark-2` | `#AA1B1B` | 危险按钮按下 |

### 2.5 纯色

| 名称 | 色值 | 用途 |
|------|------|------|
| White | `#FFFFFF` | 选中文字、高对比文字 |
| Black | `#000000` | 混色基色、蒙版 |

---

## 3. 字体排版 (Typography)

### 3.1 字体族

沿用 Unity 编辑器的 Inter 字体栈：

```
Inter, 'Helvetica Neue', Helvetica, 'PingFang SC', 
'Hiragino Sans GB', 'Microsoft YaHei', '微软雅黑', 
Arial, sans-serif
```

> Windows 后备：Verdana | macOS 后备：Lucida Grande

### 3.2 字号体系

以 Unity 12px 为基准，参考 Element Plus 的语义化命名：

| Token | 字号 | 字重 | 用途 |
|-------|------|------|------|
| `font-2xl` | `19px` | SemiBold (600) | 窗口主标题 |
| `font-xl` | `14px` | SemiBold (600) | 面板标题、Section 标题 |
| `font-lg` | `14px` | Regular (400) | 列表项、卡片标题 |
| `font-base` | `12px` | Regular (400) | **基准字号** — 正文、标签、控件 |
| `font-sm` | `11px` | Regular (400) | 辅助信息、搜索框 |
| `font-xs` | `10px` | Regular (400) | 极小注释、网格内标签 |
| `font-2xs` | `9px` | Regular (400) | 仅在绝对必要时（徽标等） |

### 3.3 标题层级

| 层级 | 字号 | 字重 | 用途 |
|------|------|------|------|
| Title 1 | `19px` | 600 | 窗口/对话框主标题 |
| Title 2 | `14px` | 600 | Section 标题、卡片标题 |
| Title 3 | `12px` | 600 | 小节标题、分组标签 |
| Body | `12px` | 400 | 正文 |
| Caption | `11px` | 400 | 辅助说明、脚注 |

### 3.4 行高

| 上下文 | 行高 | 说明 |
|--------|------|------|
| 单行控件文字 | 等于控件高度 | 垂直居中 |
| 多行正文 | `1.5` (18px @ 12px) | 段落阅读 |
| 列表项 | `18px` | 与单行控件高度对齐 |

---

## 4. 组件尺寸 (Component Sizing)

参考 Element Plus 的 L / M / S 三档体系，基准适配 Unity 的紧凑密度：

| Size | 高度 | 字号 | 适用控件 |
|------|------|------|----------|
| **Large** | `26px` | `14px` | 强调按钮、Section header 按钮 |
| **Default** | `20px` | `12px` | 标准输入框、下拉、按钮、选择器 |
| **Small** | `16px` | `10px` | 紧凑按钮、标签、toolbar 控件、mini toggle |

> 说明：Default `20px` 对齐 Unity `toolbar_button-height` (20px) / `single_line_large-height` (20px)；Small `16px` 对齐 Unity `single_line_small-height` (16px)；Large `26px` 为自定义放大档，用于需要视觉权重区分的主操作按钮。

### 4.1 控件内边距

| Size | 水平内边距 | 垂直内边距 | 说明 |
|------|-----------|-----------|------|
| Large | `14px` | `5px` | 按钮/输入框 |
| Default | `10px` | `3px` | 按钮/输入框 |
| Small | `6px` | `1px` | 紧凑按钮/tag |

---

## 5. 间距体系 (Spacing Scale)

参考 Element Plus 的比例化间距，适配 Unity 的紧凑画布：

| Token | 值 | 用途 |
|-------|-----|------|
| `space-2xs` | `2px` | 图标与文字间距、紧密元素间 |
| `space-xs` | `4px` | 同组控件间距、tag 间距 |
| `space-sm` | `6px` | 相关控件组内间距 |
| `space-md` | `8px` | 表单行间距、面板内元素间距 |
| `space-lg` | `12px` | 面板内边距、Section 间距 |
| `space-xl` | `16px` | 对话框内边距、卡片内边距 |
| `space-2xl` | `20px` | 面板/页面外边距 |

### 5.1 常见布局间距

| 场景 | 间距 |
|------|------|
| 表单行垂直间距 | `8px` (`space-md`) |
| Label 与控件水平间距 | `6px` (`space-sm`) |
| 同组按钮间距 | `4px` (`space-xs`) |
| Section 之间间距 | `12px` (`space-lg`) |
| 面板内边距 | `12px` (`space-lg`) |
| 对话框内边距 | `16px` (`space-xl`) |
| 面板边缘到内容 | `12px` (`space-lg`) |
| 列表项垂直间距 | `0` (紧密列表) / `2px` (`space-2xs`, 宽松列表) |

---

## 6. 圆角 (Border Radius)

| Token | 值 | 用途 |
|-------|-----|------|
| `radius-none` | `0` | table 内嵌元素、无圆角场景 |
| `radius-sm` | `2px` | 小控件（tag、badge、mini button） |
| `radius-base` | `3px` | **默认圆角** — 输入框、按钮、下拉 (Unity 基准) |
| `radius-md` | `4px` | 卡片、面板、对话框 (Element 基准) |
| `radius-round` | `9999px` | 胶囊形（tag rounded、badge） |
| `radius-circle` | `50%` | 圆形（头像、图标按钮） |

---

## 7. 阴影 (Shadows)

深色主题下阴影不易感知，以边框分隔为主，阴影作为辅助：

| Token | 值 | 用途 |
|-------|-----|------|
| `shadow-none` | `none` | 默认（深色主题多数情况） |
| `shadow-sm` | `0 1px 3px rgba(0,0,0,0.40)` | 浮起的面板/卡片 |
| `shadow-md` | `0 4px 12px rgba(0,0,0,0.50)` | 下拉菜单、弹出层 |
| `shadow-lg` | `0 8px 24px rgba(0,0,0,0.60)` | 对话框、模态窗口 |

---

## 8. 状态色 (State Colors)

| 状态 | 文字色 | 背景色 | 边框色 |
|------|--------|--------|--------|
| Hover | — | `rgba(255,255,255,0.06)` | — |
| Focus | — | — | `#3A79BB` |
| Active/Pressed | — | `primary-dark-2` / `danger-dark-2` | — |
| Selected | `#FFFFFF` | `#2C5D87` | — |
| Selected Inactive | `#FFFFFF` | `#4D4D4D` | — |
| Disabled (文字) | `#6E6E6E` | — | — |
| Disabled (背景) | — | `#3A3A3A` | `#1A1A1A` |
| Error | `#D32222` | `danger-light-9` | `#D32222` |
| Warning | `#F4BC02` | `rgba(244,188,2,0.15)` | `#F4BC02` |

---

## 9. 关键组件 Token

### 9.1 Button

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 高度 | `26px` | `20px` | `16px` |
| 字号 | `14px` | `12px` | `10px` |
| 水平内边距 | `14px` | `10px` | `6px` |
| 圆角 | `3px` | `3px` | `2px` |
| 背景 | `#585858` | `#585858` | `transparent` (toolbar 风格) |
| 文字色 | `#EEEEEE` | `#EEEEEE` | `#C4C4C4` |
| Hover 背景 | `#676767` | `#676767` | `rgba(255,255,255,0.06)` |
| Pressed 背景 | `primary-dark-2` | `primary-dark-2` | `rgba(255,255,255,0.10)` |
| Disabled 文字 | `#6E6E6E` | `#6E6E6E` | `#6E6E6E` |

### 9.2 Primary Button（主操作按钮）

基于 Default Button，用 primary 色填充：

| Token | 值 |
|-------|-----|
| 背景 | `#4C7EFF` (primary) |
| 文字色 | `#FFFFFF` |
| Hover 背景 | `#7DA8FF` (primary-light-3) |
| Pressed 背景 | `#3065CC` (primary-dark-2) |
| 边框 | `transparent` |

### 9.3 Input Field

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 高度 | `26px` | `20px` | `16px` |
| 字号 | `14px` | `12px` | `10px` |
| 水平内边距 | `14px` | `10px` | `6px` |
| 背景 | `#2A2A2A` | `#2A2A2A` | `#2A2A2A` |
| 边框 | `#212121` | `#212121` | `#212121` |
| 焦点边框 | `#3A79BB` | `#3A79BB` | `#3A79BB` |
| 圆角 | `3px` | `3px` | `2px` |

### 9.4 Dropdown / Select

| Token | 值 |
|-------|-----|
| 控件高度 | 与 Input 相同 (L:26 / M:20 / S:16) |
| 选项高度 | `22px` |
| 下拉背景 | `#313131` (bg-overlay) |
| 下拉边框 | `#232323` |
| 下拉内边距 | `4px 0` |
| Hover 背景 | `#515151` (Unity `dropdown-background`) |
| 选中文字色 | `#4C7EFF` (primary) |

### 9.5 Tag / Chip

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 高度 | `22px` | `18px` | `14px` |
| 内边距 | `10px` | `8px` | `4px` |
| 字号 | `11px` | `10px` | `9px` |
| 圆角 | `3px` | `2px` | `2px` |

### 9.6 Tab

| Token | 值 |
|-------|-----|
| 背景 | `#353535` |
| 选中背景 | `#3C3C3C` |
| Hover 背景 | `#303030` |
| 文字色 | `#BDBDBD` |
| 选中文字色 | `#D2D2D2` |
| 高度 | `26px` |
| 水平内边距 | `12px` |

### 9.7 Tree / List

| Token | 值 |
|-------|-----|
| 行高度 | `18px` (紧凑) / `22px` (默认) |
| 缩进宽度 | `14px` |
| 选中背景 | `#2C5D87` |
| Hover 背景 | `rgba(255,255,255,0.06)` |
| 文字色 | `#C4C4C4` |
| 展开箭头色 | `#BDBDBD` |

### 9.8 Tooltip

| Token | 值 |
|-------|-----|
| 背景 | `#373737` |
| 边框 | `#191919` |
| 文字色 | `#D2D2D2` |
| 内边距 | `6px 10px` |
| 字号 | `11px` |
| 圆角 | `3px` |

### 9.9 Scrollbar

| Token | 值 |
|-------|-----|
| 滑块背景 | `#5F5F5F` |
| 滑块 Hover | `#686868` |
| 滑块边框 | `#323232` |
| 轨道背景 | `rgba(0,0,0,0.05)` |
| 宽度 | `12px` |

### 9.10 Separator / Divider

| Token | 值 |
|-------|-----|
| 颜色 | `#303030` (border-light) |
| 粗细 | `1px` |
| 水平边距 | `0` (通栏) / `8px` (缩进) |

### 9.11 Section Header / Accordion Header

| Token | 值 |
|-------|-----|
| 高度 | `22px` |
| 背景 | `#3C3C3C` (bg-headerbar) |
| 文字色 | `#D2D2D2` |
| 字号 | `12px` (font-base), 字重 600 |
| 水平内边距 | `8px` |

---

## 10. 变量命名规范 (USS / CSS)

沿用 Unity USS 的变量命名惯例（`--unity-` 前缀风格），RedDust 自定义变量使用 `--rd-` 前缀：

```
--rd-{category}-{element}[-{variant}][-{state}]
```

### 10.1 类别 (Category)

| 前缀 | 用途 |
|------|------|
| `--rd-color-` | 颜色（语义色 + 基底色） |
| `--rd-text-` | 文字颜色 |
| `--rd-bg-` | 背景颜色 |
| `--rd-border-` | 边框颜色 |
| `--rd-font-` | 字体大小 / 字重 |
| `--rd-size-` | 控件尺寸 |
| `--rd-space-` | 间距 |
| `--rd-radius-` | 圆角 |
| `--rd-shadow-` | 阴影 |

### 10.2 命名示例

```
--rd-bg-panel              → 面板背景
--rd-bg-input              → 输入框背景
--rd-text-primary          → 主要文字色
--rd-color-primary         → 主色
--rd-color-primary-light-3 → 主色亮色3级
--rd-font-base             → 基准字号
--rd-size-default          → 默认控件高度
--rd-space-md              → 中等间距
--rd-radius-base           → 默认圆角
--rd-shadow-md             → 中阴影
```

### 10.3 USS 使用示例

```css
.rd-panel {
    background-color: var(--rd-bg-panel);
    padding: var(--rd-space-lg);
}

.rd-button--primary {
    background-color: var(--rd-color-primary);
    color: var(--unity-colors-white);
    font-size: var(--rd-font-base);
    height: var(--rd-size-default);
    border-radius: var(--rd-radius-base);
    padding: 0 var(--rd-space-md);
}

.rd-button--primary:hover {
    background-color: var(--rd-color-primary-light-3);
}

.rd-input {
    background-color: var(--rd-bg-input);
    border: 1px solid var(--rd-border-input);
    color: var(--rd-text-regular);
    height: var(--rd-size-default);
}

.rd-input:focus {
    border-color: var(--rd-border-focus);
}
```

---

## 11. 速记卡 (Cheat Sheet)

### 颜色

```
面板背景:      #383838    页面背景:      #282828
输入背景:      #2A2A2A    按钮背景:      #585858
主要文字:      #D2D2D2    常规文字:      #C4C4C4
次要文字:      #BDBDBD    占位文字:      #8A8A8A
禁用文字:      #6E6E6E    默认边框:      #232323
焦点边框:      #3A79BB

主色(蓝):      #4C7EFF    成功(绿):      #67C23A
警告(橙):      #E6A23C    危险(红):      #D32222
信息(灰蓝):    #A8B2BF
```

### 尺寸

```
控件高度:      L:26px      M:20px      S:16px
基准字号:      12px
基准圆角:      3px

水平内边距:    L:14px      M:10px      S:6px
面板内边距:    12px
对话框内边距:  16px
表单项间距:    8px
Section 间距: 12px
```

### 对比两套源系统

| 维度 | Unity Editor | Element Plus | **RedDust** |
|------|-------------|-------------|-------------|
| 主题 | Dark (Pro) | Light | **Dark** |
| 字号 | 12px | 14px | **12px** |
| 控件高 | 18px | 32px | **20px** |
| 圆角 | 3px | 4px | **3px** |
| 主色 | #4C7EFF | #409EFF | **#4C7EFF** |
| 密度 | 极高 | 标准 | **高** |
| 间距体系 | 隐式 | 显式 scale | **显式 scale** |
| 尺寸档位 | 3档(隐式) | 3档(L/M/S) | **3档(L/M/S)** |
| 语义色阶 | 无 | 9+1级混色 | **9+1级混色** |
