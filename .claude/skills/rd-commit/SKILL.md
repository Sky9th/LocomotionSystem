# rd-commit

确保**先归档文档再提交**，所有内容一次 commit。

## 流程（严格顺序，每步完成打 ✓）

### 1. `git diff --stat` 确认改动范围 [  ]
### 2. 升级版本 [  ]
- `.agent/VERSION.md` 更新版号
- 新建 `.agent/versions/vX.X.X.md`（改动摘要）
### 3. 归档文档 `/rd-doc` [  ] ← **此步跳过则禁止进入第 4 步**
- 调 `Skill` 工具，skill=`rd-doc`，args 传本次改动摘要
- 等待 Skill 返回，确认三层归档完成（session / tech / design）
### 4. 生成提交信息 [  ]
- 格式见下方
- body 必须包含 `- docs: 归档至 sessions/xxx.md, tech/.../xxx.md`
- **禁止在此步之前执行 `git add` 或 `git commit`**
### 5. 展示提交信息，用户确认后执行 `git add -A && git commit` [  ]

## 提交信息格式

```
<type>(<scope>): <summary>

- <change 1>
- <change 2>
- ...

- docs: 归档至 sessions/xxx.md, tech/.../xxx.md

vX.X.X
```

**强制规则**：
- 版号必须处于最后一行
- 禁止添加 `Co-Authored-By` 行

## 版号规则 a.b.c

| 位 | 名称 | 含义 | 晋升条件 |
|------|------|------|----------|
| **a** | 世代 | 项目阶段 | **手动指定**，不与代码变更联动 |
| **b** | 里程碑 | 功能累积 | **用户判断** |
| **c** | 修正 | 增量改进 | 修 bug、重构内部、纯文档更新、微调样式 |

- a 从 0 开始，上线时手动改为 1。打破性变更不影响 a，只升 b。
- b 默认升 c，等用户确认是否该升 b。

### b 的判断标准

不是一个窗口、一个 SO、一个组件就算里程碑。子系统成型、架构落地、文档体系重整才升 b。
