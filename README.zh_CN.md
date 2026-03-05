# Redis for Windows

[![Build](https://github.com/redis-windows/redis-windows/actions/workflows/build-redis.yml/badge.svg)](https://github.com/redis-windows/redis-windows/actions)
[![Release](https://img.shields.io/github/v/release/redis-windows/redis-windows)](https://github.com/redis-windows/redis-windows/releases)

基于官方 Redis 源码编译的 Windows 版本。

## 快速开始

```cmd
# 下载解压后，直接运行
redis-server.exe redis.conf

# 或使用 RedisService（推荐）
RedisService.exe run --foreground
```

## 使用方式

### 方式一：RedisService.exe（推荐）

自动处理路径转换，支持 Windows 原生路径。

```cmd
# 前台运行
RedisService.exe run --foreground --port 6379 --dir C:\redis-data

# 安装为 Windows 服务
RedisService.exe install -c C:\config\redis.conf --dir D:\data\redis --port 6379
net start Redis

# 卸载服务
RedisService.exe uninstall
```

### 方式二：redis-server.exe（直接运行）

**重要：** 此版本使用 Cygwin 运行时，命令行路径必须使用 Cygwin 格式。

```cmd
# ✅ 正确 - Cygwin 路径格式
redis-server.exe /cygdrive/c/config/redis.conf --dir /cygdrive/d/data --port 6379

# ❌ 错误 - Windows 路径不支持
redis-server.exe C:\config\redis.conf --dir D:\data
```

**路径转换规则：**

| Windows | Cygwin |
|---------|--------|
| `C:\path` | `/cygdrive/c/path` |
| `D:\path` | `/cygdrive/d/path` |
| `.\data` | `./data`（相对路径直接使用） |

**配置文件中：** 可直接使用 Windows 风格的正斜杠路径。

```conf
# redis.conf 中推荐写法
dir C:/redis/data
logfile C:/redis/logs/redis.log
```

## RedisService 命令参考

```cmd
RedisService.exe [command] [options]

Commands:
  install       安装为 Windows 服务
  uninstall     卸载 Windows 服务
  run           运行 Redis（默认）

Options:
  -c, --config <FILE>      配置文件路径
  --port <PORT>            端口号
  --dir <DIRECTORY>        数据目录
  --loglevel <LEVEL>       日志级别 (debug/verbose/notice/warning)
  -f, --foreground         前台运行
  --service-name <NAME>    服务名称（默认: Redis）
  --start-mode <MODE>      启动类型 (auto/manual)
  -h, --help               显示帮助
  -v, --version            显示版本
```

## 跨分区/跨目录配置

配置文件、数据目录、程序可位于任意位置：

```cmd
# 程序: C:\redis\RedisService.exe
# 配置: D:\config\redis.conf
# 数据: E:\data\redis

RedisService.exe run -c D:\config\redis.conf --dir E:\data\redis --foreground
```

## 数据持久化

关闭 Redis 时数据自动保存。`RedisService.exe` 会正确传递 `--dir` 参数确保数据保存到指定目录。

```cmd
# 启动
RedisService.exe run --foreground --dir C:\redis-data

# 写入数据
redis-cli SET mykey myvalue

# 优雅关闭
redis-cli SHUTDOWN

# 重启后数据仍存在
redis-cli GET mykey   # 返回 "myvalue"
```

## 常见问题

### redis-server.exe 找不到配置文件？

使用 Cygwin 路径格式：
```cmd
redis-server.exe /cygdrive/c/config/redis.conf
```

或使用 `RedisService.exe`，它自动处理路径转换。

### 数据丢失？

1. 确保指定了 `--dir` 参数
2. 使用 `redis-cli SHUTDOWN` 或 `Ctrl+C` 优雅关闭，不要强制杀进程
3. 重启时使用相同的 `--dir` 参数

### 关闭时 RDB 保存失败？

此问题已在 2.0.0 修复。使用 `RedisService.exe` 会自动处理路径。

详见 [Issue #69](https://github.com/redis-windows/redis-windows/issues/69)。

## 技术细节

- 编译工具：MSYS2 / Cygwin
- 服务包装：.NET 10.0
- 路径转换：RedisService 自动处理 Windows ↔ Cygwin

---

[English](README.md) | 简体中文

[![JetBrains](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://www.jetbrains.com/?from=redis-windows)
