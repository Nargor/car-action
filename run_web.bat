@echo off
title Run car-action - WebGL Local Server
echo ===================================================
echo   CAR-ACTION: WEBGL LOCAL TEST SERVER
echo ===================================================
echo.

:: Smartly detect WebGL folder location
set "WEBGL_DIR="

if exist "%~dp0index.html" (
    set "WEBGL_DIR=%~dp0"
) else if exist "%~dp0Builds\WebGL\index.html" (
    set "WEBGL_DIR=%~dp0Builds\WebGL"
) else if exist "C:\Users\tomdi\My project\Builds\WebGL\index.html" (
    set "WEBGL_DIR=C:\Users\tomdi\My project\Builds\WebGL"
)

if not defined WEBGL_DIR (
    echo [ERROR] WebGL build not found!
    echo Looked in:
    echo   - %~dp0
    echo   - %~dp0Builds\WebGL
    echo   - C:\Users\tomdi\My project\Builds\WebGL
    echo.
    pause
    exit /b 1
)

:: Strip trailing backslash if any
if "%WEBGL_DIR:~-1%"=="\" set "WEBGL_DIR=%WEBGL_DIR:~0,-1%"

echo [INFO] Found WebGL build at: "%WEBGL_DIR%"
echo [INFO] Starting local server at http://localhost:8000
echo [INFO] Opening your default browser...
start http://localhost:8000
echo.
echo Press Ctrl+C in this window to stop the server when done playing.
echo ===================================================
echo.

python -m http.server 8000 --directory "%WEBGL_DIR%"
pause
