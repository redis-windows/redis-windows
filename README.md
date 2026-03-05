### [English](https://github.com/redis-windows/redis-windows/blob/main/README.md) | [简体中文](https://github.com/redis-windows/redis-windows/blob/main/README.zh_CN.md)

# Redis for Windows

[![Build Status](https://github.com/redis-windows/redis-windows/actions/workflows/build-redis.yml/badge.svg)](https://github.com/redis-windows/redis-windows/actions)
[![Release](https://img.shields.io/github/v/release/redis-windows/redis-windows)](https://github.com/redis-windows/redis-windows/releases)
[![License](https://img.shields.io/github/license/redis-windows/redis-windows)](LICENSE)

Compiled from the official [Redis](https://github.com/redis/redis) source code for Windows using GitHub Actions. The entire build process is transparent - scripts are in [.github/workflows/](https://github.com/redis-windows/redis-windows/tree/main/.github/workflows) and logs are available on the [Actions](https://github.com/redis-windows/redis-windows/actions) page. Hash values are calculated and recorded for verification.

## Table of Contents

- [Quick Start](#quick-start)
- [Running Modes](#running-modes)
  - [Direct Execution](#direct-execution)
  - [Foreground Mode](#foreground-mode)
  - [Windows Service](#windows-service)
- [RedisService CLI](#redisservice-cli)
  - [Commands](#commands)
  - [Options](#options)
  - [Examples](#examples)
- [Data Persistence](#data-persistence)
- [FAQ](#faq)

## Quick Start

1. Download the latest release from [Releases](https://github.com/redis-windows/redis-windows/releases)
2. Extract to your preferred directory
3. Run `start.bat` or execute `redis-server.exe redis.conf`

## Running Modes

### Direct Execution

**CMD:**
```cmd
redis-server.exe redis.conf
```

**PowerShell:**
```powershell
./redis-server.exe redis.conf
```

### Foreground Mode

Run RedisService in foreground with custom options:

```cmd
RedisService.exe run --foreground --port 6380 --dir ./data
```

Press `Ctrl+C` to gracefully shutdown (data will be saved).

### Windows Service

Install as a Windows service for automatic startup:

```cmd
# Install (requires administrator privileges)
RedisService.exe install -c redis.conf --port 6379

# Start service
net start Redis

# Stop service (data will be saved)
net stop Redis

# Uninstall
RedisService.exe uninstall
```

## RedisService CLI

```
RedisService.exe [command] [options]

Commands:
  install       Install as Windows service
  uninstall     Uninstall Windows service
  run           Run Redis (default command)

Options:
  -c, --config <FILE>      Configuration file path (default: redis.conf)
  --port <PORT>            Override Redis port
  --dir <DIRECTORY>        Override Redis data directory
  --loglevel <LEVEL>       Log level: debug, verbose, notice, warning
  -f, --foreground         Run in foreground mode
  --service-name <NAME>    Service name (default: Redis)
  --display-name <NAME>    Service display name
  --description <TEXT>     Service description
  --start-mode <MODE>      Startup type: auto, manual (default: auto)
  -h, --help               Show help
  -v, --version            Show version
```

### Examples

```cmd
# Show help
RedisService.exe --help

# Run in foreground with custom config
RedisService.exe run --foreground -c myredis.conf

# Run with custom port and data directory
RedisService.exe run --foreground --port 6380 --dir D:\redis-data

# Install service with custom settings
RedisService.exe install -c redis.conf --port 6379 --dir D:\redis-data

# Install with custom service name
RedisService.exe install --service-name MyRedis --display-name "My Redis Server"

# Uninstall service
RedisService.exe uninstall

# Uninstall custom named service
RedisService.exe uninstall --service-name MyRedis
```

## Data Persistence

When stopping Redis (via `Ctrl+C`, `net stop`, or `redis-cli SHUTDOWN`), data is automatically saved to the configured directory.

**Important:** The `--dir` option is passed to both `redis-server` and `redis-cli` to ensure data is saved correctly on shutdown.

Example workflow:
```cmd
# Start with data directory
RedisService.exe run --foreground --dir ./mydata

# In another terminal, write some data
redis-cli SET mykey myvalue

# Stop gracefully (Ctrl+C or redis-cli SHUTDOWN)
redis-cli SHUTDOWN

# Restart and verify data persists
RedisService.exe run --foreground --dir ./mydata
redis-cli GET mykey    # Returns "myvalue"
```

## FAQ

### Q: Data is lost after restarting Redis?

**A:** Make sure to:
1. Specify the `--dir` option to set the data directory
2. Use graceful shutdown (`redis-cli SHUTDOWN` or `Ctrl+C`), not `kill -9`
3. Use the same `--dir` option when restarting

### Q: Failed to save RDB file on shutdown?

**A:** This is fixed in version 2.0.0+. The `--dir` option is now properly passed to `redis-cli` during shutdown. See [Issue #69](https://github.com/redis-windows/redis-windows/issues/69).

### Q: Service fails to start?

**A:** Check:
1. Run `RedisService.exe` with administrator privileges
2. Ensure `redis-server.exe` and `redis-cli.exe` are in the same directory
3. Check Windows Event Log for errors

### Q: How to specify configuration file?

**A:** Use the `-c` or `--config` option:
```cmd
RedisService.exe run -c D:\config\my-redis.conf
```

---

## Technical Details

- Built with MSYS2 and Cygwin toolchains
- Supports both MSYS2 and Cygwin runtime variants
- .NET 10.0 based service wrapper

## Acknowledgement

[![NetEngine](https://avatars.githubusercontent.com/u/36178221?s=180&v=4)](https://www.zhihu.com/question/424272611/answer/2611312760)
[![JetBrains Logo (Main) logo](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://www.jetbrains.com/?from=redis-windows)

## Disclaimer

This GitHub organization and its repositories are not affiliated with, endorsed by, or sponsored by Redis Ltd. The license provided here applies only to the content of this repository, and does not apply to the official [Redis](https://github.com/redis/redis) project or its software.

We recommend using this for local development only. For production environments, please follow Redis official guidance and deploy on Linux. This project does not bear any responsibility for any losses caused by using it.
