@echo off
title BoneAI Launcher
echo ======================================================================
echo           Starting BoneAI: AI Service + Backend + Frontend
echo ======================================================================

echo [1/3] Starting Python AI Service (YOLOv8m best.pt) on http://localhost:8000 ...
start "BoneAI - Python AI Service (Port 8000)" cmd /k ""%LOCALAPPDATA%\Programs\Python\Python311\python.exe" -m uvicorn app.main:app --app-dir AIService --port 8000"

echo Waiting 3 seconds for AI Service to load YOLOv8m weights ...
timeout /t 3 /nobreak >nul

echo [2/3] Starting Backend Web API on http://localhost:5203 ...
start "BoneAI - Backend Web API (Port 5203)" cmd /k "dotnet run --project BoneAI.API --launch-profile http"

echo Waiting 3 seconds for Backend to initialize ...
timeout /t 3 /nobreak >nul

echo [3/3] Starting Frontend Vite App on http://localhost:3000 ...
start "BoneAI - Frontend React (Port 3000)" cmd /k "cd frontend && npm run dev"

echo.
echo ======================================================================
echo   BoneAI is running with REAL YOLOv8m Inference!
echo   - AI Service (Python): http://localhost:8000
echo   - Backend Web API:     http://localhost:5203
echo   - Frontend Web UI:     http://localhost:3000
echo ======================================================================
echo De dung he thong, dong ca 3 cua so dong lenh duoc mo len.
