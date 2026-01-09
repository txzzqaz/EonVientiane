# EonVientiane 服务端部署指南

## 打包的文件

打包文件位置：`build_output/published/EonVientianeServer-Linux.tar.gz`

打包大小：约 31MB

## 部署步骤

### 1. 上传文件到服务器

将 `EonVientianeServer-Linux.tar.gz` 上传到你的 Linux 服务器。

### 2. 解压文件

```bash
tar -xzf EonVientianeServer-Linux.tar.gz
cd EonVientianeServer-Linux
```

### 3. 赋予执行权限

```bash
chmod +x EonVientianeServer
chmod +x start_server.sh
```

### 4. 运行服务器

#### 方式1：直接运行
```bash
./EonVientianeServer
```

#### 方式2：使用启动脚本
```bash
./start_server.sh
```

#### 方式3：后台运行（推荐用于生产环境）
```bash
nohup ./EonVientianeServer > server.log 2>&1 &
```

查看日志：
```bash
tail -f server.log
```

停止服务器：
```bash
pkill EonVientianeServer
```

### 5. 使用 systemd 服务（推荐）

创建服务文件 `/etc/systemd/system/eonvientiane.service`：

```ini
[Unit]
Description=EonVientiane Game Server
After=network.target

[Service]
Type=simple
User=your-username
WorkingDirectory=/path/to/EonVientianeServer-Linux
ExecStart=/path/to/EonVientianeServer-Linux/EonVientianeServer
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

启用并启动服务：
```bash
sudo systemctl daemon-reload
sudo systemctl enable eonvientiane
sudo systemctl start eonvientiane
```

查看服务状态：
```bash
sudo systemctl status eonvientiane
```

查看日志：
```bash
sudo journalctl -u eonvientiane -f
```

## 服务器配置

### 默认端口
- TCP 7777

### 防火墙配置

如果使用防火墙，需要开放 7777 端口：

#### Ubuntu/Debian (ufw)
```bash
sudo ufw allow 7777/tcp
```

#### CentOS/RHEL (firewalld)
```bash
sudo firewall-cmd --permanent --add-port=7777/tcp
sudo firewall-cmd --reload
```

#### iptables
```bash
sudo iptables -A INPUT -p tcp --dport 7777 -j ACCEPT
sudo iptables-save > /etc/iptables/rules.v4
```

## 系统要求

- 操作系统：Linux x64 (Ubuntu 20.04+, CentOS 8+, Debian 10+ 或其他现代 Linux 发行版)
- 内存：至少 512MB RAM
- 磁盘空间：至少 100MB 可用空间
- 网络：需要开放 TCP 7777 端口

## 注意事项

1. **自包含部署**：此服务端包含所有必要的 .NET 运行时文件，无需在服务器上安装 .NET SDK 或运行时。

2. **权限**：确保服务端可执行文件有执行权限。

3. **端口占用**：确保 7777 端口未被其他程序占用。

4. **日志**：服务器会输出日志到标准输出，建议重定向到文件或使用 systemd 管理。

5. **数据持久化**：服务端会在运行目录创建 `users.json` 文件存储用户数据，请定期备份。

## 故障排查

### 检查端口是否开放
```bash
netstat -tuln | grep 7777
```

### 查看进程
```bash
ps aux | grep EonVientianeServer
```

### 测试连接
```bash
telnet your-server-ip 7777
```

## 更新服务器

1. 停止运行中的服务器
2. 备份 `users.json` 文件（如果存在）
3. 解压新版本的 tar.gz 文件
4. 恢复 `users.json` 文件
5. 重新启动服务器

## 重新打包服务端

在开发机器上运行：

```bash
./build_server.sh
```

这将生成新的 `build_output/published/EonVientianeServer-Linux.tar.gz` 文件。

## 联系支持

如有问题，请查看项目文档或提交 issue。
