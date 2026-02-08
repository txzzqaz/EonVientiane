#!/bin/bash

# EonVientiane 本地测试启动脚本
# 用于启动服务端和两个客户端进行本地测试

set -e

# 颜色输出
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 项目路径
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SERVER_BIN="$SCRIPT_DIR/EonVientianeServer/bin/Debug/net9.0"
CLIENT_BIN="$SCRIPT_DIR/EonVientiane/bin/Debug/net9.0"

# 本地测试数据目录（持久化，不清理）
TEST_CLIENT_1_DIR="$SCRIPT_DIR/test_client_1"
TEST_CLIENT_2_DIR="$SCRIPT_DIR/test_client_2"
TEST_CLIENT_3_DIR="$SCRIPT_DIR/test_client_3"
LONGTERM_DIR="$SCRIPT_DIR/test_longterm"

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}EonVientiane 本地测试环境${NC}"
echo -e "${GREEN}======================================${NC}"

# 每次都重新构建项目
echo -e "\n${YELLOW}开始重新构建项目...${NC}"

# 构建 Shared
echo -e "${YELLOW}[1/3] 构建 Shared...${NC}"
dotnet build "$SCRIPT_DIR/Shared/Shared.csproj" -c Debug

# 构建客户端
echo -e "${YELLOW}[2/3] 构建客户端...${NC}"
dotnet build "$SCRIPT_DIR/EonVientiane/EonVientiane.csproj" -c Debug

# 构建服务端
echo -e "${YELLOW}[3/3] 构建服务端...${NC}"
dotnet build "$SCRIPT_DIR/EonVientianeServer/EonVientianeServer.csproj" -c Debug

echo -e "${GREEN}✓ 构建完成${NC}"

# 确保测试目录存在（不清理任何数据）
mkdir -p "$TEST_CLIENT_1_DIR" "$TEST_CLIENT_2_DIR" "$TEST_CLIENT_3_DIR" "$LONGTERM_DIR"

launch_in_terminal() {
	local workdir="$1"
	local cmd="$2"
	(cd "$workdir" && \
	gnome-terminal -- bash -c "$cmd; exec bash" 2>/dev/null || \
	xterm -e "$cmd" 2>/dev/null || \
	konsole -e "$cmd" 2>/dev/null || \
	bash -c "$cmd" &) 
}

# 启动服务端（使用长期测试目录持久化数据）
echo -e "\n${YELLOW}启动服务端（端口 7777，数据目录: $LONGTERM_DIR）...${NC}"
launch_in_terminal "$LONGTERM_DIR" "\"$SERVER_BIN/EonVientianeServer\" 7777"

# 等待服务端启动
sleep 2

# 启动第一个客户端
echo -e "${YELLOW}启动客户端 #1（数据目录: $TEST_CLIENT_1_DIR）...${NC}"
launch_in_terminal "$TEST_CLIENT_1_DIR" "\"$CLIENT_BIN/EonVientiane\""

# 等待一下
sleep 1

# 启动第二个客户端
echo -e "${YELLOW}启动客户端 #2（数据目录: $TEST_CLIENT_2_DIR）...${NC}"
launch_in_terminal "$TEST_CLIENT_2_DIR" "\"$CLIENT_BIN/EonVientiane\""

# 启动第三个客户端
echo -e "${YELLOW}启动客户端 #3（数据目录: $TEST_CLIENT_3_DIR）...${NC}"
launch_in_terminal "$TEST_CLIENT_3_DIR" "\"$CLIENT_BIN/EonVientiane\""

echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}所有进程已启动！${NC}"
echo -e "${GREEN}======================================${NC}"
echo -e "服务端: localhost:7777（数据目录: $LONGTERM_DIR）"
echo -e "客户端 #1: 已启动（数据目录: $TEST_CLIENT_1_DIR）"
echo -e "客户端 #2: 已启动（数据目录: $TEST_CLIENT_2_DIR）"
echo -e "客户端 #3: 已启动（数据目录: $TEST_CLIENT_3_DIR）"
echo -e "\n提示: 使用 pkill -f EonVientiane 停止所有进程"
