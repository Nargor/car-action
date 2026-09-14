@echo off
setlocal enabledelayedexpansion
title Build car-action - Windows PC (Standalone 64-bit)

echo ===================================================
echo   CAR-ACTION: BUILD WINDOWS PC (64-bit)
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
    echo   [1] Click menu in Unity Editor: "Build" -^> "Build Windows (PC)"
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
        echo [INFO] Build cancelled. You can build inside Unity Editor via menu: Build -^> Build Windows (PC)
        echo.
        pause
        exit /b 0
    )
)

:: 4. Prepare Build and Log Directory
if not exist "!PROJECT_PATH!\Builds\PC" mkdir "!PROJECT_PATH!\Builds\PC"
set "LOG_FILE=!PROJECT_PATH!\Builds\build_windows.log"

echo.
echo [BUILDING] Compiling Windows Standalone 64-bit player...
echo Log file: "!LOG_FILE!"
echo Please wait, this may take 1-3 minutes...
echo.

:: 5. Run Unity Build in Batchmode
"!UNITY_EXE!" -quit -batchmode -projectPath "!PROJECT_PATH!" -executeMethod BuildPipelineScript.BuildWindows -logFile "!LOG_FILE!"

set BUILD_EXIT_CODE=%ERRORLEVEL%

echo.
echo ===================================================
if %BUILD_EXIT_CODE% EQU 0 (
    if exist "!PROJECT_PATH!\Builds\PC\car-action.exe" (
        echo [SUCCESS] Windows PC build completed successfully!
        echo Output: !PROJECT_PATH!\Builds\PC\car-action.exe
    ) else (
        echo [WARNING] Unity exited with code 0 but car-action.exe was not found.
        echo Check log: !LOG_FILE!
    )
) else (
    echo [FAILED] Build failed with exit code: %BUILD_EXIT_CODE%
    echo Please inspect log for details: !LOG_FILE!
)
echo ===================================================
echo.
pause
