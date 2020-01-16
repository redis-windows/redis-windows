# Redis5-on-Windows
可以在Windows运行的redis 5.0.7

启动脚本：`start_redis5.bat`基于RunHiddenConsole.exe实现了redis后台无窗口运行，运行前记得修改路径
停止命令：`taskkill /F /IM redis-server.exe > nul`

默认密码是 `root`
在redis.windows.conf的445行修改






编译参考 https://www.cnblogs.com/LUA123/p/11447163.html
