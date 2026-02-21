# scripts/README.md — Issue Import Helpers

This directory contains helper scripts for importing the Project 3 backlog into GitHub Issues.

---

## `create_issues.sh`

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

## Manual Alternative

If you prefer not to use the script, see `docs/project3-backlog.md` for a table overview and copy-pastable issue bodies for each task. You can paste those directly into the GitHub issue creation UI.
