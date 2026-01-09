# 多平台构建脚本使用指南

## 概述

`build_all.sh` 是一个自动化的 Bash 脚本，用于为 EonVientiane 游戏构建和打包三个版本：

1. **Linux 客户端** (`EonVientiane-Linux.zip`)
2. **Windows 客户端** (`EonVientiane-Windows.zip`)
3. **Windows 服务端** (`EonVientianeServer-Windows.zip`)

## 前提条件

在运行此脚本之前，请确保你的系统已安装以下工具：

- **dotnet SDK 8.0 或更高版本** - 用于构建 C# 项目
- **zip 工具** - 用于打包输出文件（Linux/macOS 通常已预装）
- **bash** - 脚本运行环境（Linux/macOS 通常已预装）

## 快速开始

### 方法 1：直接运行

```bash
cd /home/qazokmwsxijn/Documents/EonVientiane/EonVientiane
./build_all.sh
```

### 方法 2：使用 bash 命令

```bash
bash build_all.sh
```

## 脚本工作流程

脚本按以下步骤执行：

### 1. **环境准备** ✓
- 清理旧的构建文件
- 创建输出目录结构

### 2. **依赖恢复** ✓
- 恢复所有 NuGet 包依赖

### 3. **跨平台构建**
- **Linux 客户端**
  - 运行时标识符：`linux-x64`
  - 配置：Release
  - 优化：启用修剪 (Trimmed)

- **Windows 客户端**
  - 运行时标识符：`win-x64`
  - 配置：Release
  - 功能：完整功能版本

- **Windows 服务端**
  - 运行时标识符：`win-x64`
  - 配置：Release
  - 框架：.NET 8.0

### 4. **打包** ✓
- 将每个构建输出压缩为 ZIP 文件
- 保留目录结构和所有必需文件

## 输出文件

构建完成后，所有输出文件将位于：

```
build_output/published/
├── EonVientiane-Linux.zip         (约 18 MB)
├── EonVientiane-Windows.zip       (约 39 MB)
└── EonVientianeServer-Windows.zip (约 32 MB)
```

### 文件内容

每个 ZIP 文件包含：

- **可执行文件** - 直接运行的程序
- **运行时库** - 自包含的 .NET 运行时（无需单独安装 .NET）
- **资源文件** - 游戏内容、字体、配置等
- **依赖库** - 所有必需的第三方库

## 使用已构建的文件

### Linux 客户端

```bash
unzip EonVientiane-Linux.zip
cd EonVientiane-Linux
./EonVientiane
```

### Windows 客户端

```cmd
unzip EonVientiane-Windows.zip
cd EonVientiane-Windows
EonVientiane.exe
```

### Windows 服务端

```cmd
unzip EonVientianeServer-Windows.zip
cd EonVientianeServer-Windows
EonVientianeServer.exe
```

## 常见问题

### Q: 脚本执行失败，显示 "dotnet not found"
**A:** 请确保已安装 .NET SDK，并将其添加到 PATH。运行 `dotnet --version` 验证安装。

### Q: 编译时出现警告信息
**A:** 这些是编译警告（CS8632, IL2026 等），不会影响程序功能。这些警告来自：
- 可空引用类型检查
- JSON 序列化代码修剪分析
这些是正常的，可以放心忽略。

### Q: 构建很慢
**A:** 首次构建会下载所有依赖包，包括 MonoGame 框架。后续构建会快得多（使用缓存）。

### Q: 需要清理构建文件
**A:** 运行 `rm -rf build_output obj bin` 来清理所有构建缓存和输出。

### Q: 我想只构建某个特定平台
**A:** 编辑脚本，注释掉不需要的部分。例如，注释掉 Linux 构建部分：
```bash
# echo -e "\n${YELLOW}[1/3] 构建 Linux 客户端...${NC}"
# ... Linux 构建代码 ...
```

## 脚本定制

### 修改输出目录

编辑脚本，改变 `BUILD_OUTPUT_DIR` 变量：

```bash
BUILD_OUTPUT_DIR="/your/custom/path"
```

### 修改构建配置

- **改变优化级别**：将 `-c Release` 改为 `-c Debug`
- **禁用代码修剪**：在 Linux 构建中改 `PublishTrimmed=true` 为 `PublishTrimmed=false`
- **改变目标框架**：编辑各项目的 `.csproj` 文件的 `<TargetFramework>` 属性

## 故障排除

### 检查脚本权限

```bash
chmod +x build_all.sh
```

### 运行调试模式

在脚本顶部添加 `set -x` 以查看每条执行的命令：

```bash
#!/bin/bash
set -x
set -e
# ... 脚本其余内容 ...
```

### 查看详细输出

改变输出过滤，显示完整编译信息：

```bash
# 替换这一行：
dotnet publish ... 2>&1 | tail -20

# 为：
dotnet publish ...
```

## 后续步骤

1. **测试构建的应用**
   - 在各自的平台上运行已打包的应用
   - 验证游戏功能正常

2. **部署**
   - 将 ZIP 文件上传到你的服务器
   - 分发给用户或团队

3. **CI/CD 集成**
   - 将此脚本集成到 GitHub Actions、Jenkins 等 CI/CD 平台
   - 自动进行每日或每次提交时的构建

## 许可证

此脚本是 EonVientiane 项目的一部分。
