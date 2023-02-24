# Redis for Windows

### Based on Github's powerful Actions automatic construction capabilities, Redis for Windows version is compiled for us in real time

Three operation modes are provided

It is recommended to use start.bat for the development environment

Or run from the command line

The production environment is recommended to run in the way of installing system services

## Installation Services
It will start automatically every time you start
Need to run as administrator
```shell
sc.exe create Redis binpath= 'C:\Software\Redis\RedisService.exe' start= auto
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

## Command line startup
cmd start
```shell
redis-server.exe redis.conf
```
powershell start
```shell
./redis-server.exe redis.conf
```

![image](https://user-images.githubusercontent.com/515784/215540157-65f55297-cde2-49b3-8ab3-14dca7e11ee0.png)


Project Home: https://github.com/redis-windows/redis-windows

express gratitude: https://www.zhihu.com/question/424272611/answer/2611312760
