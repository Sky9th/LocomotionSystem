#!/usr/bin/env bash
# =============================================================================
# create_issues.sh
# Batch-create GitHub Issues from the Project 3 backlog using the `gh` CLI.
#
# Usage:
#   bash scripts/create_issues.sh [--dry-run] [--repo OWNER/REPO]
#
# Options:
#   --dry-run        Print the gh commands without executing them.
#   --repo           Override the target repository (default: current repo).
#
# Prerequisites:
#   1. Install GitHub CLI: https://cli.github.com/
#   2. Authenticate:  gh auth login
#   3. The labels referenced in this script must already exist in the repo.
#      Create missing labels with:
#        gh label create "P0" --color "d73a4a" --repo OWNER/REPO
#        gh label create "P1" --color "e4e669" --repo OWNER/REPO
#        gh label create "P2" --color "0075ca" --repo OWNER/REPO
#        gh label create "基础" --color "c2e0c6" --repo OWNER/REPO
#        ... (repeat for each domain label)
# =============================================================================

set -euo pipefail

DRY_RUN=false
REPO_FLAG=""

# ---------------------------------------------------------------------------
# Parse arguments
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run)
      DRY_RUN=true
      shift
      ;;
    --repo)
      REPO_FLAG="--repo $2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
  esac
done

# ---------------------------------------------------------------------------
# Helper: create one issue
# ---------------------------------------------------------------------------
create_issue() {
  local title="$1"
  local priority="$2"
  local labels="$3"       # comma-separated, e.g. "P0,基础"
  local estimate="$4"
  local description="$5"

  local body
  body=$(cat <<EOF
**Priority:** ${priority}
**Estimate:** ${estimate}h
**Labels:** ${labels}

## Description
${description}

## Acceptance Criteria
- [ ] (fill in before starting)
EOF
)

  if [[ "$DRY_RUN" == true ]]; then
    echo "[DRY-RUN] gh issue create \\"
    echo "  --title \"[Task] ${title}\" \\"
    echo "  --label \"${labels}\" \\"
    # shellcheck disable=SC2086
    echo "  --body \"...\" ${REPO_FLAG}"
    echo ""
  else
    # shellcheck disable=SC2086
    gh issue create \
      --title "[Task] ${title}" \
      --label "${labels}" \
      --body "${body}" \
      ${REPO_FLAG}
  fi
}

# ---------------------------------------------------------------------------
# Task list
# Format: create_issue "TITLE" "PRIORITY" "LABEL1,LABEL2" "ESTIMATE" "DESC"
# ---------------------------------------------------------------------------

create_issue "资源系统框架"    "P0" "P0,基础" "20" "物品数据结构 仓库 背包 鼠标拾取"
create_issue "生存指标"        "P0" "P0,基础" "44" "饥饿/口渴/体力/生命 基础消耗恢复"
create_issue "建造系统原型"    "P0" "P0,基础" "48" "网格化建造 放置/拆除 消耗材料 返还50%"
create_issue "丧尸基础AI"      "P0" "P0,基础" "48" "NavMesh 视觉感知 追逐 攻击"
create_issue "玩家基础控制"    "P0" "P0,基础" "48" "鼠标移动 交互 UI框架 格子背包"
create_issue "地图原型"        "P0" "P0,基础" "28" "用Synty资产搭建测试场景"
create_issue "NPC基础AI"       "P1" "P1,NPC"  "32" "玩家指令模式 框选 移动/攻击/工作"
create_issue "NPC任务分配"     "P1" "P1,NPC"  "48" "UI指派固定工作 自动产出"
create_issue "NPC招募"         "P1" "P1,NPC"  "36" "解救幸存者 对话加入"
create_issue "战斗系统基础"    "P1" "P1,战斗" "36" "近战武器 普通攻击 伤害计算"
create_issue "武器切换与技能栏" "P1" "P1,战斗" "44" "背包切换武器 技能栏更新"
create_issue "熟练度系统基础"  "P1" "P1,战斗" "36" "武器类型熟练度 显示等级"
create_issue "农业系统"        "P1" "P1,农业" "56" "农田建造 播种 生长 收获 留种"
create_issue "工具系统"        "P1" "P1,工具" "56" "斧/镐/锤/锄/锯/厨具 耐久 维修"
create_issue "热武器基础"      "P1" "P1,战斗" "24" "手枪 消耗弹药 噪音吸引"
create_issue "烹饪系统"        "P2" "P2,烹饪" "32" "篝火 2种基础食谱"
create_issue "科技树界面"      "P2" "P2,科技" "32" "列表式UI 显示节点 详情"
create_issue "图纸系统"        "P2" "P2,科技" "64" "图纸物品 学习消耗 解锁效果"
create_issue "尸潮触发机制"    "P2" "P2,尸潮" "48" "每日伪随机 概率动态调整"
create_issue "尸潮规模与构成"  "P2" "P2,尸潮" "24" "简单公式 天数×系数"
create_issue "尸潮行为"        "P2" "P2,尸潮" "40" "向据点移动 攻击建筑 掉落"

if [[ "$DRY_RUN" == true ]]; then
  echo "Done. Would create 21 issues (dry-run — nothing was created)."
else
  echo "Done. Created 21 issues."
fi
