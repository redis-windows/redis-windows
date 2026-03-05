# Redis for Windows

[![Build](https://github.com/redis-windows/redis-windows/actions/workflows/build-redis.yml/badge.svg)](https://github.com/redis-windows/redis-windows/actions)
[![Release](https://img.shields.io/github/v/release/redis-windows/redis-windows)](https://github.com/redis-windows/redis-windows/releases)

Compiled from official Redis source for Windows.

## Quick Start

```cmd
# After download and extract
redis-server.exe redis.conf

# Or use RedisService (recommended)
RedisService.exe run --foreground
```

## Usage

### Option 1: RedisService.exe (Recommended)

Automatically handles path conversion. Use native Windows paths.

```cmd
# Run in foreground
RedisService.exe run --foreground --port 6379 --dir C:\redis-data

# Install as Windows service
RedisService.exe install -c C:\config\redis.conf --dir D:\data\redis --port 6379
net start Redis

# Uninstall service
RedisService.exe uninstall
```

### Option 2: redis-server.exe (Direct)

**Important:** This build uses Cygwin runtime. Command-line paths must use Cygwin format.

```cmd
# ✅ Correct - Cygwin path format
redis-server.exe /cygdrive/c/config/redis.conf --dir /cygdrive/d/data --port 6379

# ❌ Wrong - Windows paths not supported
redis-server.exe C:\config\redis.conf --dir D:\data
```

**Path Conversion:**

| Windows | Cygwin |
|---------|--------|
| `C:\path` | `/cygdrive/c/path` |
| `D:\path` | `/cygdrive/d/path` |
| `.\data` | `./data` (relative works as-is) |

**In config file:** Use forward slashes (Windows style with `/`).

```conf
# Recommended in redis.conf
dir C:/redis/data
logfile C:/redis/logs/redis.log
```

## RedisService CLI Reference

```cmd
RedisService.exe [command] [options]

Commands:
  install       Install as Windows service
  uninstall     Uninstall Windows service
  run           Run Redis (default)

Options:
  -c, --config <FILE>      Config file path
  --port <PORT>            Server port
  --dir <DIRECTORY>        Data directory
  --loglevel <LEVEL>       Log level (debug/verbose/notice/warning)
  -f, --foreground         Run in foreground
  --service-name <NAME>    Service name (default: Redis)
  --start-mode <MODE>      Startup type (auto/manual)
  -h, --help               Show help
  -v, --version            Show version
```

## Cross-Partition/Directories

Config, data, and program can be in any location:

```cmd
# Program: C:\redis\RedisService.exe
# Config:  D:\config\redis.conf
# Data:    E:\data\redis

RedisService.exe run -c D:\config\redis.conf --dir E:\data\redis --foreground
```

## Data Persistence

Data is saved automatically on shutdown. `RedisService.exe` correctly passes `--dir` to ensure data is saved to the specified directory.

```cmd
# Start
RedisService.exe run --foreground --dir C:\redis-data

# Write data
redis-cli SET mykey myvalue

# Graceful shutdown
redis-cli SHUTDOWN

# Restart - data persists
redis-cli GET mykey   # Returns "myvalue"
```

## FAQ

### redis-server.exe can't find config file?

Use Cygwin path format:
```cmd
redis-server.exe /cygdrive/c/config/redis.conf
```

Or use `RedisService.exe` which handles path conversion automatically.

### Data lost after restart?

1. Always specify `--dir` option
2. Use graceful shutdown (`redis-cli SHUTDOWN` or `Ctrl+C`), don't kill the process
3. Use the same `--dir` when restarting

## Technical Details

- Build toolchain: MSYS2 / Cygwin
- Service wrapper: .NET 10.0
- Path handling: RedisService auto-converts Windows ↔ Cygwin paths

---

English | [简体中文](README.zh_CN.md)

## Disclaimer

This project is not affiliated with, endorsed by, or sponsored by Redis Ltd. The license provided here applies only to this repository, not to the official Redis project.

This is recommended for local development only. For production environments, please follow Redis official guidance and deploy on Linux. This project is not responsible for any losses caused by its use.
