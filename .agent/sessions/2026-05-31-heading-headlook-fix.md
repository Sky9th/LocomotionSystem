# 2026-05-31 朝向和头视修复

## 改动

- ComputeLocomotionHeading 非寻路时返回 ownerTransform.forward（不跟随鼠标）
- headLookSmoothingSpeed 540→5，适配身体固定后头部大幅度转向

## 原因

身体不再旋转跟随鼠标后，HeadLook target 值跳动变大（从接近 0 变成 ±1），需要更慢的平滑速度。

## 后续

- 长按右键后再转身功能待实现
- 资产文件 headLookSmoothingSpeed 需同步改为 3~5
