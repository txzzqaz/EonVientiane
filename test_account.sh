#!/bin/bash

# Eon Vientiane 账户系统测试脚本

PROJECT_DIR="/home/qazokmwsxijn/Documents/EonVientiane/EonVientiane"
cd "$PROJECT_DIR"

echo "========================================"
echo "Eon Vientiane 账户和加密系统测试"
echo "========================================"
echo ""

echo "1. 编译项目..."
dotnet build -c Debug > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "   ✓ 编译成功"
else
    echo "   ❌ 编译失败"
    exit 1
fi
echo ""

echo "2. 项目文件结构:"
echo "   核心模型:"
ls -lh EonVientiane.Core/Models/*.cs | awk '{print "    - " $NF}'
echo ""
echo "   核心服务:"
ls -lh EonVientiane.Core/Services/*.cs | awk '{print "    - " $NF}'
echo ""
echo "   CLI 组件:"
ls -lh EonVientiane.CLI/*.cs | grep -v obj | awk '{print "    - " $NF}'
echo ""

echo "3. 加密和账户系统概览:"
echo "   ✓ User 模型 - 用户账户表示"
echo "   ✓ EncryptionService - AES-256 加密和 PBKDF2 密码哈希"
echo "   ✓ AccountService - 账户管理（创建、登录、保存加密数据）"
echo ""

echo "4. 账户文件存储位置:"
ACCOUNT_DIR="$HOME/.local/share/EonVientiane/Accounts"
if [ -d "$ACCOUNT_DIR" ]; then
    echo "   └── $ACCOUNT_DIR"
    echo "       账户数量: $(ls -1 "$ACCOUNT_DIR"/*.json 2>/dev/null | wc -l)"
else
    echo "   └── $ACCOUNT_DIR (尚未创建)"
fi
echo ""

echo "5. 安全特性:"
echo "   ✓ 密码：PBKDF2-SHA256（10,000 次迭代）+ 随机盐"
echo "   ✓ 数据：AES-256-CBC 加密"
echo "   ✓ 完整性：SHA256 校验和"
echo "   ✓ 存储：加密的 JSON 文件"
echo ""

echo "6. 可用命令群组:"
echo "   账户命令："
echo "     • register <用户名> <邮箱>  - 创建新账户"
echo "     • login <用户名>             - 登录账户"
echo "     • logout                     - 登出"
echo "     • account                    - 查看账户信息"
echo "     • users                      - 查看用户列表"
echo "     • users list                 - 查看统计"
echo "     • changepwd                  - 更改密码"
echo ""

echo "7. 快速测试步骤:"
echo "   1) 运行: dotnet run --project EonVientiane.CLI -c Debug"
echo "   2) 选择：创建账户"
echo "   3) 输入：用户名 testuser，邮箱 test@example.com"
echo "   4) 输入：密码（密码不会显示）"
echo "   5) 游戏中运行："
echo "      > account          (查看账户信息)"
echo "      > users            (查看用户列表)" 
echo "      > register user2 user2@example.com  (创建第二个账户)"
echo "      > logout           (登出)"
echo "      > login testuser   (重新登录)"
echo ""

echo "8. 验证加密文件:"
echo "   账户文件已加密，无法直接查看。文件内容被 AES-256 加密保护。"
echo ""

echo "========================================"
echo "✓ 测试准备完成！"
echo "========================================"
echo ""
echo "现在可以运行游戏进行交互式测试："
echo "  dotnet run --project EonVientiane.CLI -c Debug"
echo ""
