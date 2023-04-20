### [English](https://github.com/redis-windows/redis-windows/blob/main/README.md) | [简体中文](https://github.com/redis-windows/redis-windows/blob/main/README.zh_CN.md)

# Redis for Windows

### Based on Github's powerful Actions automatic construction capabilities, Redis for Windows version is compiled for us in real time

The whole compilation process is completely open and transparent. The compilation script is located in the [.github/workflows/](https://github.com/redis-windows/redis-windows/tree/main/.github/workflows) directory of the project. The compilation log can be viewed in [Actions](https://github.com/redis-windows/redis-windows/actions). In addition, after the compilation is completed, Added hash calculation, the hash value will be printed in the compilation log, which cannot be modified, and will also be written in releases. You can check whether the hash is consistent with the log and releases pages after downloading.
This project is absolutely pure and selfless, and can stand the scrutiny of everyone. If you have good ideas, you are also welcome to communicate.


### Three operation modes are provided

It is recommended to use start.bat for the development environment

Or run from the command line

It can be run as a system service


## Command line startup
cmd start
```shell
redis-server.exe redis.conf
```
powershell start
```shell
./redis-server.exe redis.conf
```

## Installation Services
It will start automatically every time you start
Need to run as administrator
```shell
sc.exe create Redis binpath=C:\Software\Redis\RedisService.exe start= auto
```
Start service
```shell
net start Redis
```
Out of Service
```shell
net stop Redis
```
Uninstall service
```shell
sc.exe delete Redis
```

![image](https://user-images.githubusercontent.com/515784/215540157-65f55297-cde2-49b3-8ab3-14dca7e11ee0.png)


Project Home: https://github.com/redis-windows/redis-windows

express gratitude: https://www.zhihu.com/question/424272611/answer/2611312760

### disclaimer
It is recommended to use it in the development process of the local machine. Please deploy it on Linux in the production environment. This project does not bear any responsibility for any losses caused to you!
