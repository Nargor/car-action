@echo off
setlocal enabledelayedexpansion
title Build car-action - WebGL (Browser)

echo ===================================================
echo   CAR-ACTION: BUILD WEBGL (BROWSER)
echo ===================================================
echo.

:: 1. Locate Unity Editor Executable
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"

if not exist "!UNITY_EXE!" (
    for /d %%i in ("C:\Program Files\Unity\Hub\Editor\*") do (
        if exist "%%i\Editor\Unity.exe" (
            set "UNITY_EXE=%%i\Editor\Unity.exe"
        )
    )
)

if not exist "!UNITY_EXE!" (
    echo [ERROR] Unity Editor executable not found!
    echo Looked for: C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe
    pause
    exit /b 1
)

echo [INFO] Found Unity: "!UNITY_EXE!"

:: 2. Locate Project Path
set "PROJECT_PATH=%~dp0"
if "%PROJECT_PATH:~-1%"=="\" set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

if not exist "!PROJECT_PATH!\Assets" (
    if exist "C:\Users\tomdi\My project\Assets" (
        set "PROJECT_PATH=C:\Users\tomdi\My project"
    )
)

echo [INFO] Project Path: "!PROJECT_PATH!"

:: 3. Check if Unity Editor is currently holding the project lock
if exist "!PROJECT_PATH!\Temp\UnityLockfile" (
    echo.
    echo ===================================================
    echo [NOTICE] Unity Editor is currently OPEN!
    echo Unity is running and holding the project lockfile.
    echo.
    echo Options:
    echo   [1] Click menu in Unity Editor: "Build" -^> "Build WebGL"
    echo   [2] Let this script close Unity and build now
    echo ===================================================
    echo.
    set /p "CLOSE_UNITY=Do you want to close Unity Editor and build now? (Y/N): "
    if /i "!CLOSE_UNITY!"=="Y" (
        echo [INFO] Closing Unity Editor...
        taskkill /F /IM Unity.exe >nul 2>&1
        timeout /t 2 >nul
        if exist "!PROJECT_PATH!\Temp\UnityLockfile" del /f /q "!PROJECT_PATH!\Temp\UnityLockfile" >nul 2>&1
        echo [INFO] Unity Editor closed. Proceeding to build...
    ) else (
        echo [INFO] Build cancelled. You can build inside Unity Editor via menu: Build -^> Build WebGL
        echo.
        pause
        exit /b 0
    )
)

:: 4. Prepare Build and Log Directory
if not exist "!PROJECT_PATH!\Builds\WebGL" mkdir "!PROJECT_PATH!\Builds\WebGL"
set "LOG_FILE=!PROJECT_PATH!\Builds\build_webgl.log"

echo.
echo [BUILDING] Compiling WebGL WebAssembly player...
echo Log file: "!LOG_FILE!"
echo WebGL compilation takes 5-15 minutes (C# -> IL2CPP -> WASM). Please wait...
echo.

:: 5. Run Unity Build in Batchmode
"!UNITY_EXE!" -quit -batchmode -projectPath "!PROJECT_PATH!" -executeMethod BuildPipelineScript.BuildWebGL -logFile "!LOG_FILE!"

set BUILD_EXIT_CODE=%ERRORLEVEL%

echo.
echo ===================================================
if %BUILD_EXIT_CODE% EQU 0 (
    if exist "!PROJECT_PATH!\Builds\WebGL\index.html" (
        echo [SUCCESS] WebGL build completed successfully!
        echo Output: !PROJECT_PATH!\Builds\WebGL\index.html
    ) else (
        echo [WARNING] Unity exited with code 0 but index.html was not found.
        echo Check log: !LOG_FILE!
    )
) else (
    echo [FAILED] Build failed with exit code: %BUILD_EXIT_CODE%
    echo Please inspect log for details: !LOG_FILE!
)
echo ===================================================
echo.
pause
