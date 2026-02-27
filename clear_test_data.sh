#!/bin/bash

# EonVientiane 清除测试数据脚本
# 用于清除所有测试区的数据，重置为初始状态

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 项目路径
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# 测试数据目录
TEST_CLIENT_1_DIR="$SCRIPT_DIR/test_client_1"
TEST_CLIENT_2_DIR="$SCRIPT_DIR/test_client_2"
TEST_CLIENT_3_DIR="$SCRIPT_DIR/test_client_3"
LONGTERM_DIR="$SCRIPT_DIR/test_longterm"
TEST_SERVER_DATA_DIR="$SCRIPT_DIR/test_server_data"

echo -e "${YELLOW}======================================${NC}"
echo -e "${YELLOW}EonVientiane 清除测试数据${NC}"
echo -e "${YELLOW}======================================${NC}"
echo ""
echo -e "${BLUE}将清除以下目录的数据:${NC}"
echo -e "  - test_client_1/"
echo -e "  - test_client_2/"
echo -e "  - test_client_3/"
echo -e "  - test_longterm/"
echo -e "  - test_server_data/"
echo ""
echo -e "${RED}警告: 此操作将删除所有测试数据，无法恢复！${NC}"
echo ""

# 询问确认
read -p "是否继续? [y/N] " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    echo -e "${YELLOW}操作已取消${NC}"
    exit 0
fi

echo ""
echo -e "${YELLOW}开始清除测试数据...${NC}"
echo ""

# 函数：清除目录数据
clear_directory() {
    local dir_path="$1"
    local dir_name=$(basename "$dir_path")
    
    if [ -d "$dir_path" ]; then
        echo -e "${YELLOW}清除 ${dir_name}...${NC}"
        rm -rf "$dir_path"/*
        echo -e "${GREEN}✓ ${dir_name} 已清除${NC}"
    else
        echo -e "${BLUE}ℹ ${dir_name} 不存在，跳过${NC}"
    fi
}

# 清除各个测试目录
clear_directory "$TEST_CLIENT_1_DIR"
clear_directory "$TEST_CLIENT_2_DIR"
clear_directory "$TEST_CLIENT_3_DIR"
clear_directory "$LONGTERM_DIR"
clear_directory "$TEST_SERVER_DATA_DIR"

echo ""
echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}测试数据清除完成！${NC}"
echo -e "${GREEN}======================================${NC}"
echo ""
echo -e "${BLUE}提示:${NC}"
echo -e "  - 所有测试数据已清除"
echo -e "  - 下次运行测试时将自动创建新的测试数据"
echo -e "  - 可以使用 ${YELLOW}./start_local_test.sh${NC} 重新开始测试"
echo ""
