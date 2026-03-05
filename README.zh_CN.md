### [English](https://github.com/redis-windows/redis-windows/blob/main/README.md) | [简体中文](https://github.com/redis-windows/redis-windows/blob/main/README.zh_CN.md)

# Redis Windows 版

[![Build Status](https://github.com/redis-windows/redis-windows/actions/workflows/build-redis.yml/badge.svg)](https://github.com/redis-windows/redis-windows/actions)
[![Release](https://img.shields.io/github/v/release/redis-windows/redis-windows)](https://github.com/redis-windows/redis-windows/releases)
[![License](https://img.shields.io/github/license/redis-windows/redis-windows)](LICENSE)

基于官方 [Redis](https://github.com/redis/redis) 源码编译的 Windows 版本。整个编译过程完全透明公开，编译脚本位于 [.github/workflows/](https://github.com/redis-windows/redis-windows/tree/main/.github/workflows) 目录，编译日志可在 [Actions](https://github.com/redis-windows/redis-windows/actions) 页面查看。每次发布都会计算文件哈希值，方便校验下载文件的完整性。

## 目录

- [快速开始](#快速开始)
- [运行模式](#运行模式)
  - [直接运行](#直接运行)
  - [前台模式](#前台模式)
  - [Windows 服务](#windows-服务)
- [RedisService 命令行](#redisservice-命令行)
  - [命令](#命令)
  - [选项](#选项)
  - [示例](#示例)
- [数据持久化](#数据持久化)
- [常见问题](#常见问题)

## 快速开始

1. 从 [Releases](https://github.com/redis-windows/redis-windows/releases) 下载最新版本
2. 解压到任意目录
3. 运行 `start.bat` 或执行 `redis-server.exe redis.conf`

## 运行模式

### 直接运行

**CMD:**
```cmd
redis-server.exe redis.conf
```

**PowerShell:**
```powershell
./redis-server.exe redis.conf
```

### 前台模式

使用 RedisService 以前台模式运行，支持自定义选项：

```cmd
RedisService.exe run --foreground --port 6380 --dir ./data
```

按 `Ctrl+C` 优雅关闭（数据会自动保存）。

### Windows 服务

安装为 Windows 服务，实现开机自启动：

```cmd
# 安装服务（需要管理员权限）
RedisService.exe install -c redis.conf --port 6379

# 启动服务
net start Redis

# 停止服务（数据会自动保存）
net stop Redis

# 卸载服务
RedisService.exe uninstall
```

## RedisService 命令行

```
RedisService.exe [command] [options]

Commands:
  install       安装为 Windows 服务
  uninstall     卸载 Windows 服务
  run           运行 Redis（默认命令）

Options:
  -c, --config <FILE>      配置文件路径 (默认: redis.conf)
  --port <PORT>            覆盖 Redis 端口
  --dir <DIRECTORY>        覆盖 Redis 数据目录
  --loglevel <LEVEL>       日志级别: debug, verbose, notice, warning
  -f, --foreground         前台运行模式
  --service-name <NAME>    服务名称 (默认: Redis)
  --display-name <NAME>    服务显示名称
  --description <TEXT>     服务描述
  --start-mode <MODE>      启动类型: auto, manual (默认: auto)
  -h, --help               显示帮助
  -v, --version            显示版本
```

### 示例

```cmd
# 显示帮助
RedisService.exe --help

# 使用自定义配置前台运行
RedisService.exe run --foreground -c myredis.conf

# 指定端口和数据目录运行
RedisService.exe run --foreground --port 6380 --dir D:\redis-data

# 安装服务并指定配置
RedisService.exe install -c redis.conf --port 6379 --dir D:\redis-data

# 使用自定义服务名安装
RedisService.exe install --service-name MyRedis --display-name "我的 Redis 服务"

# 卸载服务
RedisService.exe uninstall

# 卸载自定义命名的服务
RedisService.exe uninstall --service-name MyRedis
```

## 数据持久化

停止 Redis 时（通过 `Ctrl+C`、`net stop` 或 `redis-cli SHUTDOWN`），数据会自动保存到配置的目录。

**重要提示：** `--dir` 选项会同时传递给 `redis-server` 和 `redis-cli`，确保关闭时数据正确保存。

示例工作流：
```cmd
# 启动并指定数据目录
RedisService.exe run --foreground --dir ./mydata

# 在另一个终端写入数据
redis-cli SET mykey myvalue

# 优雅关闭（Ctrl+C 或 redis-cli SHUTDOWN）
redis-cli SHUTDOWN

# 重启并验证数据持久化
RedisService.exe run --foreground --dir ./mydata
redis-cli GET mykey    # 返回 "myvalue"
```

## 常见问题

### Q: 重启 Redis 后数据丢失？

**A:** 请确保：
1. 使用 `--dir` 选项指定数据目录
2. 使用优雅关闭方式（`redis-cli SHUTDOWN` 或 `Ctrl+C`），不要强制杀进程
3. 重启时使用相同的 `--dir` 选项

### Q: 关闭时保存 RDB 文件失败？

**A:** 此问题已在 2.0.0 版本修复。`--dir` 选项现在会正确传递给关闭时的 `redis-cli`。详见 [Issue #69](https://github.com/redis-windows/redis-windows/issues/69)。

### Q: 服务启动失败？

**A:** 请检查：
1. 以管理员权限运行 `RedisService.exe`
2. 确保 `redis-server.exe` 和 `redis-cli.exe` 在同一目录
3. 查看 Windows 事件日志中的错误信息

### Q: 如何指定配置文件？

**A:** 使用 `-c` 或 `--config` 选项：
```cmd
RedisService.exe run -c D:\config\my-redis.conf
```

---

## 技术细节

- 使用 MSYS2 和 Cygwin 工具链编译
- 支持 MSYS2 和 Cygwin 两种运行时变体
- 服务包装器基于 .NET 10.0

## 鸣谢

[![NetEngine](https://avatars.githubusercontent.com/u/36178221?s=180&v=4)](https://www.zhihu.com/question/424272611/answer/2611312760)
[![JetBrains Logo (Main) logo](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://www.jetbrains.com/?from=redis-windows)

## 免责声明

本项目与 Redis Ltd. 无关。提供的许可证仅适用于本仓库内容，不适用于官方 [Redis](https://github.com/redis/redis) 项目。

建议仅在本地开发环境使用。生产环境请按照 Redis 官方指导在 Linux 上部署。本项目不对使用造成的任何损失承担责任。
