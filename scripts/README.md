# scripts/README.md — Issue Import Helpers

This directory contains helper scripts for importing the Project 3 backlog into GitHub Issues.

---

## 方式一（推荐）：GitHub Actions 工作流 — 无需本地环境

无需安装任何工具，直接在 GitHub 网页端操作即可创建全部 Issue：

1. 打开仓库页面，点击顶部 **Actions** 标签页。
2. 在左侧列表中选择 **Create Backlog Issues**。
3. 点击右侧 **Run workflow** 按钮。
4. 确认参数：
   - **Dry run**（默认 `true`）：勾选时只打印日志，不创建 Issue，安全预览用。
   - 取消勾选 **Dry run** 后点击 **Run workflow**，即可自动创建全部 21 个 Issue 并生成所需标签。

> 工作流文件位于 `.github/workflows/create-backlog-issues.yml`，会自动处理标签创建，无需任何额外准备。

---

## 方式二（本地备选）：`create_issues.sh`

Batch-creates all 21 backlog tasks as GitHub Issues using the [GitHub CLI (`gh`)](https://cli.github.com/).

### Prerequisites

1. **Install `gh`**  
   Follow the official guide: <https://cli.github.com/manual/installation>

2. **Authenticate**

   ```bash
   gh auth login
   ```

3. **Create required labels** in the repository before running the script.  
   Priority labels (`P0`, `P1`, `P2`) and domain labels (`基础`, `NPC`, `战斗`, `农业`, `工具`, `烹饪`, `科技`, `尸潮`) must exist.  
   You can create them with:

   ```bash
   REPO="Sky9th/LocomotionSystem"

   gh label create "P0" --color "d73a4a" --repo "$REPO"
   gh label create "P1" --color "e4e669" --repo "$REPO"
   gh label create "P2" --color "0075ca" --repo "$REPO"
   gh label create "基础" --color "c2e0c6" --repo "$REPO"
   gh label create "NPC"  --color "bfdadc" --repo "$REPO"
   gh label create "战斗" --color "ee0701" --repo "$REPO"
   gh label create "农业" --color "84b6eb" --repo "$REPO"
   gh label create "工具" --color "e6e6e6" --repo "$REPO"
   gh label create "烹饪" --color "f9d0c4" --repo "$REPO"
   gh label create "科技" --color "d4c5f9" --repo "$REPO"
   gh label create "尸潮" --color "b60205" --repo "$REPO"
   ```

---

### Usage

**Dry-run (recommended first step — prints commands, creates nothing):**

```bash
bash scripts/create_issues.sh --dry-run
```

**Create issues in the current repository:**

```bash
bash scripts/create_issues.sh
```

**Create issues in a specific repository:**

```bash
bash scripts/create_issues.sh --repo Sky9th/LocomotionSystem
```

---

### Options

| Option | Description |
|--------|-------------|
| `--dry-run` | Print `gh` commands without executing them. Safe to run at any time. |
| `--repo OWNER/REPO` | Override the target repository. Defaults to the repo in the current directory. |

---

## 方式三：手动粘贴

如果不想使用任何脚本，参见 `docs/project3-backlog.md`，其中提供了每个任务的完整 Issue 正文，可直接粘贴到 GitHub Issue 创建界面。
