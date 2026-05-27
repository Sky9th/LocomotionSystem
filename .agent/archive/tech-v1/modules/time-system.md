# 时间系统

日期: 2026-05-22

## 概述

`92e96d8` 将 UI 和 gameplay 时间尺度分离。

## 设计

| 层 | 时间源 | 行为 |
|----|--------|------|
| Gameplay（角色、相机、物理） | `Time.deltaTime` | 受暂停/慢放影响 |
| UI（Overlay、Screen） | `Time.unscaledDeltaTime` | 始终实时 |

## 实现

- `UIOverlay.DeltaTime` / `UIScreen.DeltaTime` → `Time.unscaledDeltaTime`
- `VitalsOverlay`, `StatusOverlay` 等通过 `DeltaTime` 属性获取时间
- `GameService` 统一管理时间尺度切换
