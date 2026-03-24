@echo off
echo MokaDocs — Clear build cache
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0ClearProjectsCache.ps1" %*
pause