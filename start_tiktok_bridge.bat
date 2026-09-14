@echo off
title TikTok Live Bridge Server (car-action)
echo ===================================================
echo   TIKTOK LIVE REAL-TIME CHAT BRIDGE SERVER
echo ===================================================
echo.
echo Starting bridge on http://127.0.0.1:8765 ...
python "%~dp0TikTokBridgeServer.py" 8765
pause
