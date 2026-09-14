@echo off
title Run car-action - WebGL Local Server
echo ===================================================
echo   CAR-ACTION: WEBGL LOCAL TEST SERVER
echo ===================================================
echo.

set "WEBGL_DIR=%~dp0Builds\WebGL"
if not exist "%WEBGL_DIR%\index.html" (
    echo [ERROR] WebGL build not found at: %WEBGL_DIR%
    pause
    exit /b 1
)

echo [INFO] Starting local server at http://localhost:8000
echo [INFO] Opening your default browser...
start http://localhost:8000
echo.
echo Press Ctrl+C in this window to stop the server when done playing.
echo ===================================================
echo.

python -m http.server 8000 --directory "%WEBGL_DIR%"
pause
