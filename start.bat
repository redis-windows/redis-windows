@echo off
cd /d %~dp0
redis-server.exe redis.conf
pause
