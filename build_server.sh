#!/bin/bash

# EonVientiane 服务端构建脚本
# 构建 Linux 服务端

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 项目路径
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SERVER_PROJECT="$SCRIPT_DIR/EonVientianeServer/EonVientianeServer.csproj"
BUILD_OUTPUT_DIR="$SCRIPT_DIR/build_output"
PUBLISH_DIR="$BUILD_OUTPUT_DIR/published"

echo -e "${GREEN}======================================${NC}"
echo -e "${GREEN}EonVientiane 服务端构建${NC}"
echo -e "${GREEN}======================================${NC}"

# 恢复依赖
echo -e "\n${YELLOW}恢复项目依赖...${NC}"
dotnet restore "$SERVER_PROJECT"
echo -e "${GREEN}✓ 依赖恢复完成${NC}"

# 构建 Linux 服务端
echo -e "\n${YELLOW}构建 Linux 服务端...${NC}"
LINUX_SERVER_OUTPUT="$PUBLISH_DIR/EonVientianeServer-Linux"
mkdir -p "$LINUX_SERVER_OUTPUT"

dotnet publish "$SERVER_PROJECT" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$LINUX_SERVER_OUTPUT" \
  /p:PublishTrimmed=false \
  /p:PublishReadyToRun=true

echo -e "${GREEN}✓ Linux 服务端构建完成${NC}"

# 创建启动脚本
echo -e "\n${YELLOW}创建启动脚本...${NC}"
cat > "$LINUX_SERVER_OUTPUT/start_server.sh" << 'EOF'
#!/bin/bash
cd "$(dirname "$0")"
./EonVientianeServer
EOF

chmod +x "$LINUX_SERVER_OUTPUT/start_server.sh"
echo -e "${GREEN}✓ 启动脚本创建完成${NC}"

# 打包
echo -e "\n${YELLOW}打包为 tar.gz 文件...${NC}"
cd "$PUBLISH_DIR"
tar -czf "EonVientianeServer-Linux.tar.gz" "EonVientianeServer-Linux/"
SIZE=$(du -sh "EonVientianeServer-Linux.tar.gz" | cut -f1)
echo -e "${GREEN}✓ EonVientianeServer-Linux.tar.gz${NC} (大小: $SIZE)"

# 显示输出信息
echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}构建完成!${NC}"
echo -e "${GREEN}======================================${NC}"
echo -e "服务端目录: ${YELLOW}$LINUX_SERVER_OUTPUT${NC}"
echo -e "打包文件: ${YELLOW}$PUBLISH_DIR/EonVientianeServer-Linux.tar.gz${NC}"
echo -e "\n运行服务端:"
echo -e "  ${YELLOW}cd $LINUX_SERVER_OUTPUT${NC}"
echo -e "  ${YELLOW}./start_server.sh${NC}"
echo -e "或者:"
echo -e "  ${YELLOW}./EonVientianeServer${NC}"

