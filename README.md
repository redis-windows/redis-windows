### [English](https://github.com/redis-windows/redis-windows/blob/main/README.md) | [简体中文](https://github.com/redis-windows/redis-windows/blob/main/README.zh_CN.md)

# Redis Windows Version
### With the powerful automated building capability of GitHub Actions, we can compile the latest version of Redis for Windows system in real-time. 
The entire compilation process is completely transparent and open, with the compilation script located in the [.github/workflows/](https://github.com/redis-windows/redis-windows/tree/main/.github/workflows) directory and the compilation logs available on the [Actions](https://github.com/redis-windows/redis-windows/actions) page. In addition, we have added a hash calculation step when the compilation is completed, and the result is printed in the log. This is unmodifiable and recorded in the release page. You can verify the hash value of the downloaded file against the log and release page.  
Our project is absolutely pure and without any hidden features, and can withstand the scrutiny of all experts. If you have any good ideas, please feel free to communicate with us.  

## We provide three operation modes: 
1. Run the start.bat script in the project to start directly with one click.
2. Use the command line.
3. Support running as a system service.

### Command line startup:
cmd startup: 
```shell
redis-server.exe redis.conf
```
powershell startup: 
```shell
./redis-server.exe redis.conf
```

### Service installation:
Can achieve automatic startup on boot. Please run it as an administrator and change RedisService.exe to the actual directory where it is stored.
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

## Acknowledgement: 
[![NetEngine](https://avatars.githubusercontent.com/u/36178221?s=180&v=4)](https://www.zhihu.com/question/424272611/answer/2611312760) 
[![JetBrains Logo (Main) logo](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)](https://www.jetbrains.com/?from=redis-windows)


## Disclaimer
We suggest that you use it for local development and follow Redis official guidance to deploy it on Linux for production environment. This project doesn't bear any responsibility for any losses caused by using it and is only for learning and exchange purposes.
