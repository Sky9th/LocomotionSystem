# rd-commit

改写默认 rd-commit 流程，确保**先归档文档再提交**，所有内容一次 commit。

## 流程

原系统流程：git diff → 升级版本 → commit → arch doc
改写后流程：git diff → 升级版本 → **归档文档** → commit（含文档）

1. `git diff --stat` 确认改动范围
2. 升级 `.agent/VERSION.md` + 创建 `.agent/versions/vX.X.X.md`
3. **调用 `/rd-doc` 完成三层归档**（session / tech / design）
4. 生成提交信息（body 加一行 `- docs: 归档文档至 .agent/`）
5. **提交前展示完整提交信息，待用户确认后执行** `git add -A && git commit`

## 提交信息

**必须与被确认的展示完全一致，不得增删任何行。**

格式：

```
type(scope): 简短描述

- 要点 1
- 要点 2
- ...

- docs: 归档至 versions/vX.X.X.md

vX.X.X
```

- **不加** `Co-Authored-By` 行
- 尾部版号独占最后一行，无多余空行
- 第四步展示给用户的字符串就是最终 `git commit -m` 的入参，一字不改
