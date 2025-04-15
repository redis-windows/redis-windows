@echo off
SETLOCAL ENABLEEXTENSIONS
SETLOCAL ENABLEDELAYEDEXPANSION

set "REDIS_PATH=%~dp0"
set "REDIS_SERVICE_NAME=RedisServiceCtrl.exe"
set "SERVICE_NAME=MyRedis"
set "SERVICE_DISPLAY_NAME=My Redis Service"
set "MAX_WAIT_RETRY=10"

:-------------------------------------
REM  --> Check for permissions
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo 请求管理员权限...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
    pushd "%CD%"
    CD /D "%REDIS_PATH%"
:--------------------------------------

echo ----------------------------------
echo 正在初始化服务管理操作...

call :uninstallService

echo ----------------------------------
echo 正在安装服务 %SERVICE_NAME%...
sc.exe create %SERVICE_NAME% binpath="%REDIS_PATH%%REDIS_SERVICE_NAME%" start= AUTO DisplayName= "%SERVICE_DISPLAY_NAME%"
if %errorlevel% neq 0 (
    echo [错误] 服务创建失败（错误码：%errorlevel%）
    pause
    exit /B 1
)
echo 服务安装成功

echo 正在设置服务描述...
sc.exe description "%SERVICE_NAME%" "Redis 服务控制器"
if %errorlevel% neq 0 (
    echo [警告] 设置服务描述失败（错误码：%errorlevel%）
)

call :startService

echo ----------------------------------
echo [成功] 服务安装并启动完成
pause
exit /B 0

:uninstallService
    echo 正在检查服务 %SERVICE_NAME% 是否存在...
    sc query %SERVICE_NAME% >nul 2>&1
    if %errorlevel% equ 0 (
        echo 服务 %SERVICE_NAME% 存在，正在停止...
        sc stop %SERVICE_NAME%
        
        echo 等待服务 %SERVICE_NAME% 停止...
        set "COUNT=0"
        :check_stopped
            sc query %SERVICE_NAME% | findstr /R /C:"STATE.*STOPPED" >nul 2>&1
            if %errorlevel% equ 0 (
                echo 服务 %SERVICE_NAME% 已成功停止
                goto delete_service
            )
            set /a "COUNT+=1"
            if !COUNT! geq %MAX_WAIT_RETRY% (
                echo [警告] 服务停止超时
                goto delete_service
            )
            timeout /t 1 /nobreak >nul
            echo 已等待 !COUNT! 秒...
        goto check_stopped
        
        :delete_service
        echo 服务 %SERVICE_NAME% 已停止，正在删除...
        sc delete %SERVICE_NAME%
        
        echo 等待服务 %SERVICE_NAME% 删除确认...
        set "COUNT=0"
        :check_deleted
            sc query %SERVICE_NAME% >nul 2>&1
            if %errorlevel% neq 0 (
                echo 服务 %SERVICE_NAME% 已成功删除
                goto:eof
            )
            set /a "COUNT+=1"
            if !COUNT! geq %MAX_WAIT_RETRY% (
                echo [警告] 服务删除确认超时
                goto:eof
            )
            timeout /t 1 /nobreak >nul
            echo 已等待 !COUNT! 秒...
        goto check_deleted
    ) else (
        echo 服务 %SERVICE_NAME% 不存在，跳过卸载步骤
    )
goto:eof

:startService
    echo 正在检查服务 %SERVICE_NAME% 是否正在运行...
    sc query %SERVICE_NAME% | findstr /R /C:"STATE.*RUNNING" >nul 2>&1
    if %errorlevel% neq 0 (
        echo 服务 %SERVICE_NAME% 未运行，正在启动...
        net start %SERVICE_NAME%
        
        echo 等待服务 %SERVICE_NAME% 启动（最多%MAX_WAIT_RETRY%秒）...
        set "COUNT=0"
        :check_started
            sc query %SERVICE_NAME% | findstr /R /C:"STATE.*RUNNING" >nul 2>&1
            if %errorlevel% equ 0 (
                echo 服务 %SERVICE_NAME% 已成功启动
                goto:eof
            )
            set /a "COUNT+=1"
            if !COUNT! geq %MAX_WAIT_RETRY% (
                echo [警告] 服务启动状态检测超时
                sc query %SERVICE_NAME%
                goto:eof
            )
            timeout /t 1 /nobreak >nul
            echo 已等待 !COUNT! 秒...
        goto check_started
    ) else (
        echo 服务 %SERVICE_NAME% 已经在运行
    )
goto:eof



