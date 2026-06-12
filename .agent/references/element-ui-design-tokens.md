# Element Plus 设计令牌 (Design Tokens) 参考

> 来源：[Element Plus](https://element-plus.org/) `theme-chalk/src/common/var.scss` + `var.scss`
> 提取日期：2026-06-12

---

## 1. 色彩系统 (Color System)

### 1.1 基础色板

| 类型 | 基准色 | 用途 |
|------|--------|------|
| `primary` | `#409EFF` | 主色 / 品牌色 |
| `success` | `#67C23A` | 成功状态 |
| `warning` | `#E6A23C` | 警告状态 |
| `danger` | `#F56C6C` | 危险操作 |
| `error` | `#F56C6C` | 错误状态（同 danger） |
| `info` | `#909399` | 信息提示 |

### 1.2 色阶变体 (Light/Dark Mix)

每个类型色自动生成 **9 级亮色**（混白）和 **1 级暗色**（混黑）：

| 变体 | 混合比例 | 说明 |
|------|----------|------|
| `light-1` | 10% 白 + 90% 基色 | 最淡亮色 |
| `light-2` | 20% 白 + 80% 基色 | |
| `light-3` | 30% 白 + 70% 基色 | |
| `light-4` | 40% 白 + 60% 基色 | |
| `light-5` | 50% 白 + 50% 基色 | 中等亮色 |
| `light-6` | 60% 白 + 40% 基色 | |
| `light-7` | 70% 白 + 30% 基色 | |
| `light-8` | 80% 白 + 20% 基色 | |
| `light-9` | 90% 白 + 10% 基色 | 最淡（常用作 hover 背景） |
| `dark-2` | 20% 黑 + 80% 基色 | 暗色变体（常用于 active 状态） |

**CSS 变量命名**: `--el-color-{type}` / `--el-color-{type}-light-{i}` / `--el-color-{type}-dark-2`

### 1.3 纯色

| 名称 | 颜色值 | CSS 变量 |
|------|--------|----------|
| White | `#FFFFFF` | `--el-color-white` |
| Black | `#000000` | `--el-color-black` |

---

## 2. 文字颜色 (Text Colors)

| Token | 颜色值 | CSS 变量 | 用途 |
|-------|--------|----------|------|
| `primary` | `#303133` | `--el-text-color-primary` | 主要文字 / 标题 |
| `regular` | `#606266` | `--el-text-color-regular` | 常规正文 |
| `secondary` | `#909399` | `--el-text-color-secondary` | 次要文字 / 辅助信息 |
| `placeholder` | `#A8ABB2` | `--el-text-color-placeholder` | 占位符文字 |
| `disabled` | `#C0C4CC` | `--el-text-color-disabled` | 禁用态文字 |

---

## 3. 边框颜色 (Border Colors)

| Token | 颜色值 | CSS 变量 | 用途 |
|-------|--------|----------|------|
| (default) | `#DCDFE6` | `--el-border-color` | 默认边框 |
| `light` | `#E4E7ED` | `--el-border-color-light` | 浅边框 |
| `lighter` | `#EBEEF5` | `--el-border-color-lighter` | 更浅边框 |
| `extra-light` | `#F2F6FC` | `--el-border-color-extra-light` | 极浅边框 |
| `dark` | `#D4D7DE` | `--el-border-color-dark` | 深边框 |
| `darker` | `#CDD0D6` | `--el-border-color-darker` | 更深边框 |

**边框基础**：
- `--el-border-width`: `1px`
- `--el-border-style`: `solid`
- `--el-border`: `1px solid var(--el-border-color)`

---

## 4. 填充/背景颜色 (Fill & Background)

### 4.1 Fill Colors（填充色）

| Token | 颜色值 | CSS 变量 | 用途 |
|-------|--------|----------|------|
| (default) | `#F0F2F5` | `--el-fill-color` | 默认填充 |
| `light` | `#F5F7FA` | `--el-fill-color-light` | 浅填充 |
| `lighter` | `#FAFAFA` | `--el-fill-color-lighter` | 更浅填充 |
| `extra-light` | `#FAFCFF` | `--el-fill-color-extra-light` | 极浅填充 |
| `dark` | `#EBEDF0` | `--el-fill-color-dark` | 深填充 |
| `darker` | `#E6E8EB` | `--el-fill-color-darker` | 更深填充 |
| `blank` | `#FFFFFF` | `--el-fill-color-blank` | 空白填充 |

### 4.2 Background Colors（背景色）

| Token | 颜色值 | CSS 变量 | 用途 |
|-------|--------|----------|------|
| (default) | `#FFFFFF` | `--el-bg-color` | 默认背景 |
| `page` | `#F2F3F5` | `--el-bg-color-page` | 页面背景 |
| `overlay` | `#FFFFFF` | `--el-bg-color-overlay` | 浮层背景（下拉/弹窗） |

---

## 5. 字体排版 (Typography)

### 5.1 字号

| Token | 字号 | CSS 变量 | 用途 |
|-------|------|----------|------|
| `extra-large` | `20px` | `--el-font-size-extra-large` | 特大号 |
| `large` | `18px` | `--el-font-size-large` | 大号 |
| `medium` | `16px` | `--el-font-size-medium` | 中号 |
| `base` | `14px` | `--el-font-size-base` | **基准字号** |
| `small` | `13px` | `--el-font-size-small` | 小号 |
| `extra-small` | `12px` | `--el-font-size-extra-small` | 特小号 |

### 5.2 其他排版属性

| Token | 值 | CSS 变量 |
|-------|-----|----------|
| Font Family | `'Helvetica Neue', Helvetica, 'PingFang SC', 'Hiragino Sans GB', 'Microsoft YaHei', '微软雅黑', Arial, sans-serif` | `--el-font-family` |
| Font Weight (Primary) | `500` | `--el-font-weight-primary` |
| Line Height (Primary) | `24px` | `--el-font-line-height-primary` |

### 5.3 标题层级

| 标题 | 计算规则 | 最终值 (base=14px) |
|------|----------|---------------------|
| `h1` | `base + 6px` | `20px` |
| `h2` | `base + 4px` | `18px` |
| `h3` | `base + 2px` | `16px` |
| `h4-h6, p` | `inherit` | `14px` |

---

## 6. 组件尺寸 (Component Sizes)

三种标准尺寸，贯穿所有组件：

| Size | 高度 | CSS 变量 |
|------|------|----------|
| `large` | `40px` | `--el-component-size-large` |
| `default` | `32px` | `--el-component-size-default` |
| `small` | `24px` | `--el-component-size-small` |

适用的组件：Button、Input、Select、Tag、Radio 等。

---

## 7. 圆角 (Border Radius)

| Token | 值 | CSS 变量 | 用途 |
|-------|-----|----------|------|
| `base` | `4px` | `--el-border-radius-base` | **默认圆角** |
| `small` | `2px` | `--el-border-radius-small` | 小圆角 |
| `round` | `20px` | `--el-border-radius-round` | 胶囊形 |
| `circle` | `100%` | `--el-border-radius-circle` | 圆形 |

---

## 8. 阴影 (Box Shadows)

| Token | 值 | CSS 变量 |
|-------|-----|----------|
| default | `0px 12px 32px 4px rgba(0,0,0,0.04)`, `0px 8px 20px rgba(0,0,0,0.08)` | `--el-box-shadow` |
| `light` | `0px 0px 12px rgba(0,0,0,0.12)` | `--el-box-shadow-light` |
| `lighter` | `0px 0px 6px rgba(0,0,0,0.12)` | `--el-box-shadow-lighter` |
| `dark` | `0px 16px 48px 16px rgba(0,0,0,0.08)`, `0px 12px 32px rgba(0,0,0,0.12)`, `0px 8px 16px -8px rgba(0,0,0,0.16)` | `--el-box-shadow-dark` |

---

## 9. Z-Index 层级

| Token | 值 | CSS 变量 | 用途 |
|-------|-----|----------|------|
| `normal` | `1` | `--el-index-normal` | 普通层级 |
| `top` | `1000` | `--el-index-top` | 置顶层（Header/Backtop） |
| `popper` | `2000` | `--el-index-popper` | 弹出层（Dropdown/Popover/Tooltip） |

---

## 10. 断点 (Breakpoints)

| 断点 | 值 | 说明 |
|------|-----|------|
| `xs` | `< 768px` | 超小屏幕（手机竖屏） |
| `sm` | `≥ 768px` | 小屏幕（手机横屏/小平板） |
| `md` | `≥ 992px` | 中等屏幕（平板横屏） |
| `lg` | `≥ 1200px` | 大屏幕（桌面） |
| `xl` | `≥ 1920px` | 超大屏幕（宽屏桌面） |

---

## 11. 动画/过渡 (Transitions)

| Token | 值 | CSS 变量 |
|-------|-----|----------|
| Duration | `0.3s` | `--el-transition-duration` |
| Duration Fast | `0.2s` | `--el-transition-duration-fast` |
| Ease-in-out Bezier | `cubic-bezier(0.645, 0.045, 0.355, 1)` | `--el-transition-function-ease-in-out-bezier` |
| Fast Bezier | `cubic-bezier(0.23, 1, 0.32, 1)` | `--el-transition-function-fast-bezier` |

---

## 12. 禁用态 (Disabled State)

| Token | CSS 变量 | 值 |
|-------|----------|-----|
| 背景色 | `--el-disabled-bg-color` | `var(--el-fill-color-light)` = `#F5F7FA` |
| 文字色 | `--el-disabled-text-color` | `var(--el-text-color-placeholder)` = `#A8ABB2` |
| 边框色 | `--el-disabled-border-color` | `var(--el-border-color-light)` = `#E4E7ED` |

---

## 13. 遮罩/覆盖层 (Overlay & Mask)

| Token | 值 | CSS 变量 |
|-------|-----|----------|
| Overlay | `rgba(0,0,0,0.8)` | `--el-overlay-color` |
| Overlay Light | `rgba(0,0,0,0.7)` | `--el-overlay-color-light` |
| Overlay Lighter | `rgba(0,0,0,0.5)` | `--el-overlay-color-lighter` |
| Mask | `rgba(255,255,255,0.9)` | `--el-mask-color` |
| Mask Extra-light | `rgba(255,255,255,0.3)` | `--el-mask-color-extra-light` |

---

## 14. 关键组件 Token 速查

### 14.1 Button

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 高度 | `40px` | `32px` | `24px` |
| 字号 | `14px` | `14px` | `12px` |
| 水平内边距 | `20px` | `16px` | `12px` |
| 垂直内边距 | `13px` | `9px` | `6px` |
| 圆角 | `4px` | `4px` | `3px` |

### 14.2 Input

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 高度 | `40px` | `32px` | `24px` |
| 字号 | `14px` | `14px` | `12px` |
| 水平内边距 | `16px` | `12px` | `8px` |

### 14.3 Tag

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 高度 | `32px` | `24px` | `20px` |
| 内边距 | `12px` | `10px` | `8px` |
| 字号 | — | `12px` | — |
| 圆角 | — | `4px` | — |
| 图标大小 | `16px` | `14px` | `12px` |

### 14.4 Table

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 单元格内边距 | `0 12px` | `0 8px` | `0 4px` |
| 行内边距 | `12px 0` | `8px 0` | `4px 0` |
| 字号 | `14px` | `14px` | `12px` |

### 14.5 Dialog

| Token | 值 |
|-------|-----|
| 宽度 | `50%` |
| 上边距 | `15vh` |
| 内边距 | `16px` |
| 圆角 | `4px` |
| 标题字号 | `18px` |
| 内容字号 | `14px` |

### 14.6 Card

| Token | 值 |
|-------|-----|
| 圆角 | `4px` |
| 内边距 | `20px` |

### 14.7 Popover

| Token | 值 |
|-------|-----|
| 内边距 | `12px` |
| 大内边距 | `18px 20px` |
| 圆角 | `4px` |
| 标题字号 | `16px` |

### 14.8 Pagination

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 按钮宽高 | `40px` | `32px` | `24px` |
| 字号 | — | `14px` | `12px` |
| 圆角 | — | `2px` | — |

### 14.9 Select Dropdown

| Token | 值 |
|-------|-----|
| 选项高度 | `34px` |
| 下拉内边距 | `6px 0` |
| 最大高度 | `274px` |

### 14.10 Menu

| Token | 值 |
|-------|-----|
| 项高度 | `56px` |
| 子项高度 | `50px` |
| 横向菜单高度 | `60px` |
| 横向子项高度 | `36px` |
| 基础层级内边距 | `20px` |

### 14.11 Form

| Token | 值 |
|-------|-----|
| Label 字号 | `14px` |
| inline 内容宽度 | `220px` |

### 14.12 Avatar

| Token | Large | Default | Small |
|-------|-------|---------|-------|
| 尺寸 | `56px` | `40px` | `24px` |
| 文字大小 | — | `14px` | — |
| 图标大小 | — | `18px` | — |

### 14.13 Alert

| Token | 值 |
|-------|-----|
| 内边距 | `8px 16px` |
| 标题字号 | `14px` (有描述时 `16px`) |
| 描述字号 | `14px` |
| 图标大小 | `16px` (大图标 `28px`) |

### 14.14 Layout (Header / Main / Footer)

| Token | 值 |
|-------|-----|
| Header 高度 | `60px` |
| Header 内边距 | `0 20px` |
| Main 内边距 | `20px` |
| Footer 高度 | `60px` |
| Footer 内边距 | `0 20px` |

---

## 15. CSS 变量命名规范

Element Plus 使用 BEM 命名，命名空间 `el`：

```
--el-{block}[-{element}][-{modifier}]
```

示例：
- `--el-color-primary` — 颜色 / 主色
- `--el-color-primary-light-3` — 颜色 / 主色 / 亮色3级
- `--el-text-color-regular` — 文字颜色 / 常规
- `--el-border-radius-base` — 圆角 / 基础
- `--el-button-bg-color` — 按钮 / 背景色
- `--el-component-size-default` — 组件尺寸 / 默认

---

## 16. 关键尺寸速记卡

```
组件高度:    L:40px    M:32px    S:24px
基准字号:    14px
基准圆角:    4px
主要文字:    #303133
常规文字:    #606266
次要文字:    #909399
占位文字:    #A8ABB2
默认边框:    #DCDFE6
主色:        #409EFF
成功:        #67C23A
警告:        #E6A23C
危险:        #F56C6C
页面背景:    #F2F3F5
卡片内边距:   20px
对话框内边距: 16px
组件水平内边距: L:16px M:12px S:8px
组件垂直内边距: L:13px M:9px  S:6px
```
