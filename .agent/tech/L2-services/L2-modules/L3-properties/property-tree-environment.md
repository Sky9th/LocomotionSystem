# PropertyTree — Environment 子树

> 日期: 2026-06-30 · 状态: 待推理（远期）
> 关联: `property-tree-structure.md`

```
Environment : Entity                             全局单例，WeatherService 驱动
│  继承: Common/DisplayName, Icon, Description, Weight, Tags
│
├── Atmosphere/
│   ├── FogDensity         Float     雾浓度
│   ├── Temperature_Env    Float     环境气温 (°C)
│   └── Humidity           Float     湿度 (%)
│
└── Time/
    └── TimeOfDay          Float     当前时间 (0-24h)
```
