#!/bin/bash

# EonVientiane 停止脚本
# 用于停止所有运行中的服务端和客户端进程

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}正在停止 EonVientiane 进程...${NC}"

# 停止服务端
SERVER_PIDS=$(pgrep -f "EonVientianeServer")
if [ -n "$SERVER_PIDS" ]; then
    echo -e "${YELLOW}停止服务端进程...${NC}"
    pkill -f "EonVientianeServer"
    echo -e "${GREEN}✓ 服务端已停止${NC}"
else
    echo -e "${YELLOW}未发现运行中的服务端进程${NC}"
fi

# 停止客户端（排除服务端）
CLIENT_PIDS=$(pgrep -f "EonVientiane/bin" | grep -v "Server")
if [ -n "$CLIENT_PIDS" ]; then
    echo -e "${YELLOW}停止客户端进程...${NC}"
    pkill -f "EonVientiane/bin.*EonVientiane$"
    echo -e "${GREEN}✓ 客户端已停止${NC}"
else
    echo -e "${YELLOW}未发现运行中的客户端进程${NC}"
fi

echo -e "${GREEN}所有 EonVientiane 进程已停止${NC}"
