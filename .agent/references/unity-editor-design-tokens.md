# Unity Editor 设计令牌 (Design Tokens) 参考

> 来源：[Unity 2022.3 USS Built-in Variable Reference](https://docs.unity3d.com/2022.3/Documentation/Manual/UIE-uss-built-in-variable-reference.html) + [Unity Foundations](https://www.foundations.unity.com/)
> 提取日期：2026-06-12

---

## 色彩系统总览

Unity Editor 提供 **三套主题**：
| 主题 | 说明 | 对应 USS 列名 |
|------|------|---------------|
| **Professional** | 深色主题（默认，ProSkin） | Professional |
| **Personal** | 浅色主题 | Personal |
| **Runtime** | 运行时 UI 默认值 | Runtime |

---

## 1. 全局默认色 (Default)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `default-background` | `#282828` | `#A5A5A5` | — |
| `default-border` | `#232323` | `#999999` | `#959595` |
| `default-text` | `#D2D2D2` | `#090909` | `#1B1B1B` |
| `default-text-hover` | `#BDBDBD` | `#090909` | — |

---

## 2. 窗口 & 面板 (Window / Background)

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `window-background` | `#383838` | `#C8C8C8` |
| `window-border` | `#242424` | `#939393` |
| `window-text` | `#BDBDBD` | `#090909` (Runtime: `#1B1B1B`) |
| `box-background` | `rgba(40,40,40,0.30)` | `rgba(185,185,185,0.90)` |
| `box-border` | `transparent` | `transparent` |

---

## 3. 工具栏 (Toolbar / App Toolbar)

### 3.1 主工具栏 (App Toolbar)

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `app_toolbar-background` | `#191919` | `#8A8A8A` |
| `app_toolbar_button-background` | `#383838` | `#C8C8C8` |
| `app_toolbar_button-background-checked` | `#6A6A6A` | `#656565` |
| `app_toolbar_button-background-hover` | `#424242` | `#BBBBBB` |
| `app_toolbar_button-background-pressed` | `#6A6A6A` | `#656565` |
| `app_toolbar_button-border` | `#191919` | `#6B6B6B` |
| `app_toolbar_button-border_accent` | `#222222` | `#6B6B6B` |

### 3.2 次级工具栏 (Toolbar)

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `toolbar-background` | `#3C3C3C` | `#CBCBCB` |
| `toolbar-border` | `#232323` | `#999999` |
| `toolbar_button-background` | `#3C3C3C` | `#CBCBCB` |
| `toolbar_button-background-checked` | `#505050` | `#EFEFEF` |
| `toolbar_button-background-focus` | `#464646` | `#C1C1C1` |
| `toolbar_button-background-hover` | `#464646` | `#C1C1C1` |
| `toolbar_button-border` | `#232323` | `#999999` |
| `toolbar_button-text` | `#C4C4C4` | `#090909` |
| `toolbar_button-text-hover` | `#BDBDBD` | `#090909` |

---

## 4. 按钮 (Button)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `button-background` | `#585858` | `#E4E4E4` | `#BCBCBC` |
| `button-background-focus` | `#6E6E6E` | `#BEBEBE` | — |
| `button-background-hover` | `#676767` | `#ECECEC` | `#D1D1D1` |
| `button-background-hover_pressed` | `#4F657F` | `#B0D2FC` | — |
| `button-background-pressed` | `#46607C` | `#96C3FB` | `#959595` |
| `button-background-disabled` | — | — | `#959595` |
| `button-border` | `#303030` | `#B2B2B2` | `#959595` |
| `button-border_accent` | `#242424` | `#939393` | — |
| `button-border_accent-focus` | `#7BAEFA` | `#018CFF` | — |
| `button-border-pressed` | `#0D0D0D` | `#707070` | `#646464` |
| `button-text` | `#EEEEEE` | `#090909` | `#1B1B1B` |
| `button-text-disabled` | — | — | `#2D2D2D` |

---

## 5. 输入框 (Input Field)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `input_field-background` | `#2A2A2A` | `#F0F0F0` | `#F0F0F0` |
| `input_field-background-disabled` | — | — | `#D1D1D1` |
| `input_field-border` | `#212121` | `#B7B7B7` | `#646464` |
| `input_field-border_accent` | `#0D0D0D` | `#A0A0A0` | — |
| `input_field-border-focus` | `#3A79BB` | `#1D5087` | `#006AA6` |
| `input_field-border-hover` | `#656565` | `#6C6C6C` | `#323232` |
| `input_field-text-disabled` | — | — | `#585858` |

---

## 6. 下拉菜单 (Dropdown)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `dropdown-background` | `#515151` | `#DFDFDF` | `#DFDFDF` |
| `dropdown-background-hover` | `#585858` | `#E4E4E4` | `#E8E8E8` |
| `dropdown-border` | `#303030` | `#B2B2B2` | `#999999` |
| `dropdown-border_accent` | `#242424` | `#939393` | `#939393` |
| `dropdown-text` | `#E4E4E4` | `#090909` | `#1B1B1B` |

---

## 7. 标签 & 文字 (Label / Text)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `label-text` | `#C4C4C4` | `#090909` | `#1B1B1B` |
| `label-text-focus` | `#81B4FF` | `#003C88` | `#00526A` |
| `label-text-disabled` | — | — | `#585858` |

---

## 8. 标签页 (Tab)

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `tab-background` | `#353535` | `#B6B6B6` |
| `tab-background-checked` | `#3C3C3C` | `#CBCBCB` |
| `tab-background-hover` | `#303030` | `#B0B0B0` |
| `tab-text` | `#BDBDBD` | `#090909` |

---

## 9. 高亮 & 选中 (Highlight / Selection)

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `highlight-background` | `#2C5D87` | `#3A72B0` |
| `highlight-background-hover` | `rgba(255,255,255,0.06)` | `rgba(0,0,0,0.06)` |
| `highlight-background-hover-lighter` | `#5F5F5F` | `#9A9A9A` |
| `highlight-background-inactive` | `#4D4D4D` | `#AEAEAE` |
| `highlight-text` | `#4C7EFF` | `#0032E6` |
| `highlight-text-inactive` | `#FFFFFF` | `#FFFFFF` |

---

## 10. 滚动条 (Scrollbar)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `scrollbar_thumb-background` | `#5F5F5F` | `#9A9A9A` | `#E7E7E7` |
| `scrollbar_thumb-background-hover` | `#686868` | `#8E8E8E` | `#F0F0F0` |
| `scrollbar_thumb-border` | `#323232` | `#B9B9B9` | `transparent` |
| `scrollbar_thumb-border-hover` | `#686868` | `#8E8E8E` | `transparent` |
| `scrollbar_groove-background` | `rgba(0,0,0,0.05)` | `rgba(0,0,0,0.05)` | `#BCBCBC` |
| `scrollbar_groove-border` | `rgba(0,0,0,0.10)` | `rgba(0,0,0,0.10)` | `#959595` |
| `scrollbar_button-background` | `rgba(0,0,0,0.05)` | `rgba(0,0,0,0.05)` | `#F0F0F0` |
| `scrollbar_button-background-hover` | `#494949` | `#A7A7A7` | `#E7E7E7` |

---

## 11. 滑块 (Slider)

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `slider_groove-background` | `#5E5E5E` | `#8F8F8F` | `#7E7E7E` |
| `slider_groove-background-disabled` | `#575757` | `#A4A4A4` | `#7E7E7E` |
| `slider_thumb-background` | `#999999` | `#616161` | `#F0F0F0` |
| `slider_thumb-background-disabled` | `#666666` | `#9B9B9B` | `#808080` |
| `slider_thumb-background-hover` | `#EAEAEA` | `#4F4F4F` | `#F0F0F0` |
| `slider_thumb-border` | `#999999` | `#616161` | `#646464` |
| `slider_thumb-border-disabled` | `#666666` | `#666666` | `#7E7E7E` |
| `slider_thumb_halo-background` | `rgba(16,111,205,0.50)` | `rgba(12,108,203,0.50)` | `rgba(12,108,203,0.50)` |

---

## 12. Inspector 标题栏

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `inspector_titlebar-background` | `#3E3E3E` | `#CBCBCB` |
| `inspector_titlebar-background-hover` | `#474747` | `#D6D6D6` |
| `inspector_titlebar-border` | `#1A1A1A` | `#7F7F7F` |
| `inspector_titlebar-border_accent` | `#303030` | `#BABABA` |

---

## 13. Header Bar / 列标题

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `headerbar-background` | `#3C3C3C` | `#CBCBCB` | `#BCBCBC` |
| `headerbar_column-background` | `#3C3C3C` | `#CBCBCB` | `#BCBCBC` |
| `headerbar_column-background-hover` | `#464646` | `#C1C1C1` | `#D1D1D1` |
| `headerbar_column-background-pressed` | `#505050` | `#EFEFEF` | `#959595` |

---

## 14. 状态色 (Role Colors)

| Token | Professional (Dark) | Personal (Light) |
|-------|---------------------|-------------------|
| `error-text` | `#D32222` | `#5A0000` |
| `warning-text` | `#F4BC02` | `#333308` |
| `link-text` | `#4C7EFF` | `#4C7EFF` |
| `visited_link-text` | `#FF00FF` | `#AA00AA` |

---

## 15. 其他控件色

| Token | Professional (Dark) | Personal (Light) | Runtime |
|-------|---------------------|-------------------|---------|
| `object_field-background` | `#282828` | `#EDEDED` | — |
| `object_field-border` | `#202020` | `#B0B0B0` | — |
| `object_field-border-focus` | `#3A79BB` | `#1D5087` | — |
| `object_field_button-background` | `#373737` | `#DEDEDE` | — |
| `object_field_button-background-hover` | `#4C4C4C` | `#CCCCCC` | — |
| `popup-background` | `#313131` | `#C1C1C1` | — |
| `preview-background` | `#313131` | `#C1C1C1` | — |
| `preview-border` | `#232323` | `#999999` | — |
| `preview-text` | `#BDBDBD` | `#090909` | — |
| `preview_overlay-text` | `#DEDEDE` | `#FFFFFF` | — |
| `helpbox-background` | `rgba(96,96,96,0.20)` | `rgba(235,235,235,0.20)` | — |
| `helpbox-border` | `#232323` | `#A9A9A9` | — |
| `helpbox-text` | `#BDBDBD` | `#161616` | — |
| `tooltip-background` | `#373737` | `#DEDEDE` | — |
| `tooltip-border` | `#191919` | `#8A8A8A` | — |
| `play_mode-background` | `#606060` | `#ECECEC` | — |
| `progress-background` | `#303030` | `#606060` | — |
| `alternated_rows-background` | `#3F3F3F` | `#CACACA` | — |

---

## 16. 字体排版 (Typography)

### 16.1 字体族

| 平台 | 主字体 | 后备字体 |
|------|--------|----------|
| **Windows** | Inter | Verdana |
| **macOS** | Inter | Lucida Grande |
| **Linux** | Inter | Verdana |

CSS 变量: `--unity-font` → `UIPackageResources/Fonts/Inter/Inter-Regular SDF.asset`

### 16.2 字号体系

| Token | Professional | Personal | Runtime | 用途 |
|-------|-------------|----------|---------|------|
| `font_tiny_size` | `9px` | `9px` | `11px` | 极小（仅在必要时使用） |
| `font_small_size` | `10px` | `10px` | `12px` | 小号（网格内标签、Timeline 轨道） |
| `font_semi_small_size` | `11px` | `11px` | `13px` | 半小号（工具栏搜索框） |
| `font_normal_size` | `12px` | `12px` | `14px` | **基准字号**（最常用） |
| `font_big_size` | `14px` | `14px` | `15px` | 大号（列表标签） |
| `font_very_big_size` | `19px` | `19px` | `21px` | 特大号（窗口标题） |

**字重**: Regular (400) / SemiBold (600)
**CSS 变量**: `--unity-font-weight-regular` / `--unity-font-weight-bold`

---

## 17. 尺寸 & 间距 (Metrics)

### 17.1 控件高度

| Token | Professional | Personal | Runtime | 说明 |
|-------|-------------|----------|---------|------|
| `single_line-height` | `18px` | `18px` | — | 标准单行控件高度（如单行文本框） |
| `single_line_large-height` | `20px` | `20px` | — | 大单行控件（如 Inspector 标题栏） |
| `single_line_small-height` | `16px` | `16px` | — | 小单行控件（如 mini toggle） |
| `toolbar-height` | `21px` | `21px` | — | 工具栏高度 |
| `toolbar_button-height` | `20px` | `20px` | — | 工具栏按钮高度 |
| `inspector_titlebar-height` | `22px` | `22px` | — | Inspector 标题栏高度 |

### 17.2 圆角

| Token | Professional | Personal | Runtime |
|-------|-------------|----------|---------|
| `default-border_radius` | `3px` | `3px` | `0px` |

### 17.3 字体微调 (Font Padding)

| Token | Professional | Personal |
|-------|-------------|----------|
| `button-padding-bottom` | `1px` | `1px` |
| `button-padding-top` | `1px` | `1px` |
| `popup-padding-bottom` | `1px` | `1px` |
| `popup-padding-top` | `0` | `0` |
| `standard-padding-bottom` | `0` | `0` |
| `standard-padding-bottom-with-border` | `0` | `0` |

---

## 18. 开关/复选 (Toggle / Checkmark) — Runtime

| Token | Runtime |
|-------|---------|
| `toggle-text-disabled` | `#585858` |
| `toggle_checkmark-background` | `#F0F0F0` |
| `toggle_checkmark-background-disabled` | `#959595` |
| `toggle_checkmark-border` | `#646464` |
| `toggle_checkmark-border-disabled` | `#7E7E7E` |
| `toggle_checkmark-border-focus` | `#006AA6` |
| `toggle_checkmark-border-hover` | `#323232` |
| `toggle_checkmark-border-pressed` | `#323232` |

---

## 19. 进度条 (Progress Bar) — Runtime

| Token | Runtime |
|-------|---------|
| `progress_bar-background` | `#BCBCBC` |
| `progress_bar-border` | `#808080` |
| `progress_bar-text` | `#1B1B1B` |
| `progress_bar_progress-background` | `#E7E7E7` |

---

## 20. CSS 变量命名规范

Unity USS 变量命名模式：

```
--unity-{group}-{role_or_control}-{sub_element}-{pseudo_state_sequence}
```

### 20.1 Group（数据类别）

| Group | 用途 |
|-------|------|
| `colors` | 颜色属性（`background-color`、`border-color`、文字颜色） |
| `metrics` | 尺寸和形状（`border-radius`、`border-width`、`margin`、`padding`） |
| `icons` | Unity 标准图标图片路径 |
| `fonts` | 字体微调 padding |

### 20.2 Pseudo States（伪状态）

按字母序组合：`(none)` → `checked` → `disabled` → `focus` → `hover` → `inactive` → `pressed` → `selected`

示例: `--unity-colors-toolbar_button-text-focus_selected`

### 20.3 使用示例

```css
.my-editor-panel {
    background-color: var(--unity-colors-window-background);
    color: var(--unity-colors-window-text);
    font-size: var(--unity-metrics-default-font-normal_size);
    border-radius: var(--unity-metrics-default-border_radius);
    height: var(--unity-metrics-single_line-height);
}
```

---

## 21. 深色主题色值速记卡 (Professional/Dark)

```
窗口背景:    #383838
默认背景:    #282828
默认文字:    #D2D2D2
标签文字:    #C4C4C4
窗口文字:    #BDBDBD
按钮背景:    #585858
按钮文字:    #EEEEEE
按钮 Hover:  #676767
按钮按下:    #46607C
输入框背景:  #2A2A2A
输入框边框:  #212121
焦点边框:    #3A79BB
选中背景:    #2C5D87
选中文字:    #4C7EFF
错误文字:    #D32222
警告文字:    #F4BC02
链接文字:    #4C7EFF
工具栏背景:  #3C3C3C
主工具栏:    #191919
标签页背景:  #353535
Tab 选中:    #3C3C3C
滚动条滑块:  #5F5F5F
下滑块:      #5E5E5E
弹出背景:    #313131
下拉背景:    #515151
标题栏:      #3E3E3E
禁用文字:    #585858 (Runtime)
```

## 22. 浅色主题色值速记卡 (Personal/Light)

```
窗口背景:    #C8C8C8
默认背景:    #A5A5A5
默认文字:    #090909
标签文字:    #090909
按钮背景:    #E4E4E4
按钮文字:    #090909
按钮 Hover:  #ECECEC
按钮按下:    #96C3FB
输入框背景:  #F0F0F0
输入框边框:  #B7B7B7
焦点边框:    #1D5087
选中背景:    #3A72B0
选中文字:    #0032E6
错误文字:    #5A0000
警告文字:    #333308
链接文字:    #4C7EFF
主工具栏:    #8A8A8A
标签页背景:  #B6B6B6
Tab 选中:    #CBCBCB
滚动条滑块:  #9A9A9A
```

## 23. 尺寸速记卡

```
基准字号:       12px (Runtime: 14px)
标准控件高度:   18px
大控件高度:     20px
小控件高度:     16px
工具栏高度:     21px
工具栏按钮高度: 20px
标题栏高度:     22px
默认圆角:       3px (Runtime: 0)
```

---

## 24. IMGUI EditorStyles 提醒

IMGUI 使用 `EditorStyles` / `GUISkin` 类（内置皮肤 `Builtin Skins/DarkSkin/` 和 `Builtin Skins/LightSkin/`），通过 `EditorGUIUtility.isProSkin` 判断当前主题。

IMGUI 常用场景颜色：
- `EditorGUIUtility.isProSkin ? Color.white : Color.black` — 基础文字/线条色
- `GUI.backgroundColor` — 控件 tint 色（如红/绿色标记按钮）

> **建议**: 新 UI 优先使用 UI Toolkit + USS 变量自动适配主题；老 IMGUI 代码用 `EditorStyles` 获取对应样式。
