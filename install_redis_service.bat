@echo off
SET REDIS_PATH=%~dp0

:: BatchGotAdmin
:-------------------------------------
REM  --> Check for permissions
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
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

echo 1.Please enter the redis installation path
echo The default is the current path: %REDIS_PATH%
echo If you don't want to modify it, press Enter
set /p REDIS_INSTALL_PATH=Installation Path:

echo.

echo 2.Please enter the redis configuration file path
echo The default is the current path: %REDIS_PATH%redis.conf
echo Must be an absolute path
echo If you don't want to modify it, press Enter
set /p REDIS_CONF_PATH=Configuration file Path:

if defined REDIS_INSTALL_PATH (
    if defined REDIS_CONF_PATH (
        REM Install and Conf
        echo Installation Path: %REDIS_INSTALL_PATH%
        echo Configuration file Path: %REDIS_CONF_PATH%
        pause
        call:existInstallPath
        call:existConfPath
        call:installRedis
        sc.exe create "Redis" binpath="%REDIS_INSTALL_PATH%\RedisService.exe -c %REDIS_CONF_PATH%" start= AUTO
    ) else (
        REM Install
        echo Installation Path: %REDIS_INSTALL_PATH%
        echo Configuration file Path: %REDIS_INSTALL_PATH%\redis.conf
        pause
        call:existInstallPath
        call:installRedis
        sc.exe create "Redis" binpath="%REDIS_INSTALL_PATH%\RedisService.exe" start= AUTO
    )
) else (
    if defined REDIS_CONF_PATH (
        REM Conf
        echo Installation Path: %REDIS_PATH%
        echo Configuration file Path: %REDIS_CONF_PATH%
        pause
        call:existConfPath
        sc.exe create "Redis" binpath="%REDIS_PATH%\RedisService.exe -c %REDIS_CONF_PATH%" start= AUTO
    ) else (
        REM null
        echo Installation Path: %REDIS_PATH%
        echo Configuration file Path: %REDIS_PATH%\redis.conf
        pause
        sc.exe create "Redis" binpath="%REDIS_PATH%RedisService.exe" start= AUTO
    )
) 

net start "Redis"
pause

:existInstallPath
    if not exist %REDIS_INSTALL_PATH% (
        md %REDIS_INSTALL_PATH%
        if not exist %REDIS_INSTALL_PATH% (
            echo Failed to create folder!
            pause
            exit 1
        )
    )
goto:eof

:existConfPath
    if not exist %REDIS_CONF_PATH% (
        echo Configuration file does not exist!
        pause
        exit 1
    )
goto:eof

:installRedis
    xcopy %REDIS_PATH% %REDIS_INSTALL_PATH% /y
goto:eof
