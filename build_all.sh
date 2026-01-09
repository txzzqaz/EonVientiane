#!/bin/bash

# EonVientiane 多平台构建脚本
# 构建 Linux 客户端、Windows 客户端和 Windows 服务端

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 项目路径
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CLIENT_PROJECT="$SCRIPT_DIR/EonVientiane/EonVientiane.csproj"
SERVER_PROJECT="$SCRIPT_DIR/EonVientianeServer/EonVientianeServer.csproj"
BUILD_OUTPUT_DIR="$SCRIPT_DIR/build_output"
PUBLISH_DIR="$BUILD_OUTPUT_DIR/published"

# 清理输出目录
echo -e "${YELLOW}清理旧的构建文件...${NC}"
rm -rf "$BUILD_OUTPUT_DIR"
mkdir -p "$PUBLISH_DIR"

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}开始 EonVientiane 多平台构建${NC}"
echo -e "${GREEN}======================================${NC}"

# ==================== 0. 恢复依赖 ====================
echo -e "\n${YELLOW}[0/3] 恢复项目依赖...${NC}"
dotnet restore "$CLIENT_PROJECT" 2>&1 | tail -5
dotnet restore "$SERVER_PROJECT" 2>&1 | tail -5
echo -e "${GREEN}✓ 依赖恢复完成${NC}"

# ==================== 1. 构建 Linux 客户端 ====================
echo -e "\n${YELLOW}[1/3] 构建 Linux 客户端...${NC}"
LINUX_CLIENT_OUTPUT="$PUBLISH_DIR/EonVientiane-Linux"
mkdir -p "$LINUX_CLIENT_OUTPUT"

dotnet publish "$CLIENT_PROJECT" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$LINUX_CLIENT_OUTPUT" \
  /p:PublishTrimmed=true \
  /p:PublishReadyToRun=true 2>&1 | tail -20

echo -e "${GREEN}✓ Linux 客户端构建完成${NC}"

# ==================== 2. 构建 Windows 客户端 ====================
echo -e "\n${YELLOW}[2/3] 构建 Windows 客户端...${NC}"
WINDOWS_CLIENT_OUTPUT="$PUBLISH_DIR/EonVientiane-Windows"
mkdir -p "$WINDOWS_CLIENT_OUTPUT"

dotnet publish "$CLIENT_PROJECT" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o "$WINDOWS_CLIENT_OUTPUT" \
  /p:PublishTrimmed=false \
  /p:PublishReadyToRun=true 2>&1 | tail -20

echo -e "${GREEN}✓ Windows 客户端构建完成${NC}"

# ==================== 3. 构建 Windows 服务端 ====================
echo -e "\n${YELLOW}[3/3] 构建 Windows 服务端...${NC}"
WINDOWS_SERVER_OUTPUT="$PUBLISH_DIR/EonVientianeServer-Windows"
mkdir -p "$WINDOWS_SERVER_OUTPUT"

dotnet publish "$SERVER_PROJECT" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o "$WINDOWS_SERVER_OUTPUT" \
  /p:PublishTrimmed=false \
  /p:PublishReadyToRun=true 2>&1 | tail -20

echo -e "${GREEN}✓ Windows 服务端构建完成${NC}"

# ==================== 打包 ====================
echo -e "\n${YELLOW}打包为 ZIP 文件...${NC}"

cd "$PUBLISH_DIR"

# 打包 Linux 客户端
echo -e "  打包 EonVientiane-Linux..."
zip -r -q "EonVientiane-Linux.zip" "EonVientiane-Linux/"
SIZE_LINUX=$(du -sh "EonVientiane-Linux.zip" | cut -f1)
echo -e "  ${GREEN}✓ EonVientiane-Linux.zip${NC} (大小: $SIZE_LINUX)"

# 打包 Windows 客户端
echo -e "  打包 EonVientiane-Windows..."
zip -r -q "EonVientiane-Windows.zip" "EonVientiane-Windows/"
SIZE_WINDOWS=$(du -sh "EonVientiane-Windows.zip" | cut -f1)
echo -e "  ${GREEN}✓ EonVientiane-Windows.zip${NC} (大小: $SIZE_WINDOWS)"

# 打包 Windows 服务端
echo -e "  打包 EonVientianeServer-Windows..."
zip -r -q "EonVientianeServer-Windows.zip" "EonVientianeServer-Windows/"
SIZE_SERVER=$(du -sh "EonVientianeServer-Windows.zip" | cut -f1)
echo -e "  ${GREEN}✓ EonVientianeServer-Windows.zip${NC} (大小: $SIZE_SERVER)"

cd "$SCRIPT_DIR"

# ==================== 总结 ====================
echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}构建完成！${NC}"
echo -e "${GREEN}======================================${NC}"
echo -e "\n输出位置: ${YELLOW}$PUBLISH_DIR${NC}\n"
echo -e "生成的文件:"
echo -e "  1. ${GREEN}EonVientiane-Linux.zip${NC} (Linux 客户端)"
echo -e "  2. ${GREEN}EonVientiane-Windows.zip${NC} (Windows 客户端)"
echo -e "  3. ${GREEN}EonVientianeServer-Windows.zip${NC} (Windows 服务端)"
echo -e "\n文件清单:"
ls -lh "$PUBLISH_DIR"/*.zip
echo ""
