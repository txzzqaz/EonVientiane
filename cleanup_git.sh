#!/bin/bash

# Git历史清理脚本 - 移除已提交的不需要的文件/文件夹

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}================================${NC}"
echo -e "${BLUE}  Git 历史清理脚本${NC}"
echo -e "${BLUE}================================${NC}\n"

# 确认操作
echo -e "${YELLOW}⚠️  警告：此操作将移除git缓存中的文件夹${NC}"
echo -e "${YELLOW}建议在clean branch上操作，避免直接force push到main${NC}\n"

# 定义要清理的路径列表
paths_to_remove=(
    "build/"
    "build_output/"
    "EonVientiane/bin/"
    "EonVientiane/obj/"
    "EonVientianeServer/bin/"
    "EonVientianeServer/obj/"
    "Shared/bin/"
    "Shared/obj/"
)

echo -e "${BLUE}将清理以下路径：${NC}"
for path in "${paths_to_remove[@]}"; do
    echo -e "  ${YELLOW}✓ $path${NC}"
done

echo -e "\n${YELLOW}继续？ (y/n)${NC}"
read -p "> " confirm

if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo -e "${YELLOW}已取消${NC}"
    exit 0
fi

echo -e "\n${BLUE}开始清理...${NC}\n"

# 移除每个路径的git缓存
for path in "${paths_to_remove[@]}"; do
    if git ls-files --error-unmatch "$path" &>/dev/null; then
        echo -e "${BLUE}移除: $path${NC}"
        git rm -r --cached "$path" 2>/dev/null || true
    fi
done

# 提交清理操作
echo -e "\n${BLUE}提交清理操作...${NC}"
git add .gitignore
git commit -m "chore: Remove build artifacts and cache files from git history" || true

echo -e "\n${GREEN}================================${NC}"
echo -e "${GREEN}  ✓ 清理完成${NC}"
echo -e "${GREEN}================================${NC}\n"

echo -e "${YELLOW}后续步骤：${NC}"
echo -e "1. 检查改动: ${BLUE}git status${NC}"
echo -e "2. 如果确认无误，推送: ${BLUE}git push origin <branch>${NC}"
echo -e "\n${YELLOW}注意：${NC}"
echo -e "- 如果是main分支，可能需要force push: ${BLUE}git push -f origin main${NC}"
echo -e "- Force push会改写历史，确保团队成员已知晓"
