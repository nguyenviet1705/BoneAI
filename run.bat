@echo off
title BoneAI Launcher
echo ======================================================================
echo           Starting BoneAI: Backend (API) ^& Frontend (Web)
echo ======================================================================

echo [1/2] Starting Backend Web API on http://localhost:5203 ...
start "BoneAI - Backend Web API (Port 5203)" cmd /k "dotnet run --project BoneAI.API --launch-profile http"

echo Waiting 3 seconds for Backend to initialize ...
timeout /t 3 /nobreak >nul

echo [2/2] Starting Frontend Vite App on http://localhost:3000 ...
start "BoneAI - Frontend React (Port 3000)" cmd /k "cd frontend && npm run dev"

echo.
echo ======================================================================
echo   BoneAI is running!
echo   - Frontend Web UI:  http://localhost:3000
echo   - Backend Swagger:  http://localhost:5203/swagger
echo   - Backend API:      http://localhost:5203
echo ======================================================================
echo Để dừng hệ thống, chỉ cần đóng 2 cửa sổ dòng lệnh vừa được mở lên.
