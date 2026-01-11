#!/bin/bash

# 一键提交GitHub仓库脚本
# 功能：检查状态、添加文件、提交、推送

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}================================${NC}"
echo -e "${BLUE}  GitHub 一键提交脚本${NC}"
echo -e "${BLUE}================================${NC}\n"

# 检查是否在git仓库中
if [ ! -d .git ]; then
    echo -e "${RED}✗ 错误：当前目录不是一个git仓库${NC}"
    exit 1
fi

# 显示当前分支
current_branch=$(git rev-parse --abbrev-ref HEAD)
echo -e "${BLUE}当前分支：${GREEN}$current_branch${NC}"

# 显示git状态
echo -e "\n${BLUE}仓库状态：${NC}"
git status --short || true

# 检查是否有未跟踪的更改
if [ -z "$(git status --porcelain)" ]; then
    echo -e "\n${YELLOW}✓ 没有待提交的更改${NC}"
    exit 0
fi

# 提示用户输入提交信息
echo -e "\n${YELLOW}请输入提交信息（或按回车使用默认信息）：${NC}"
read -p "提交信息: " commit_message

if [ -z "$commit_message" ]; then
    commit_message="Update: $(date '+%Y-%m-%d %H:%M:%S')"
fi

# 添加所有更改
echo -e "\n${BLUE}1. 添加所有更改...${NC}"
git add -A
echo -e "${GREEN}✓ 已添加所有更改${NC}"

# 提交更改
echo -e "\n${BLUE}2. 提交更改...${NC}"
git commit -m "$commit_message"
echo -e "${GREEN}✓ 已提交${NC}"

# 推送到远程仓库
echo -e "\n${BLUE}3. 推送到远程仓库...${NC}"
git push origin "$current_branch"
echo -e "${GREEN}✓ 已推送${NC}"

echo -e "\n${GREEN}================================${NC}"
echo -e "${GREEN}  ✓ 提交完成！${NC}"
echo -e "${GREEN}================================${NC}"
