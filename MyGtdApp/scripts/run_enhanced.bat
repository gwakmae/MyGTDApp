@echo off
title .NET Application Runner
echo ================================
echo    .NET Application Runner
echo ================================
echo.

REM Move to parent directory (project root)
cd /d "%~dp0.."

echo Current working directory: %CD%
echo.

REM Check if we're in a .NET project directory
dir *.csproj >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
echo Error: No .csproj file found in project directory!
echo Make sure the .csproj file exists in the parent folder.
echo Current directory: %CD%
echo.
pause
exit /b 1
)

echo Found .csproj file(s):
dir /b *.csproj
echo.

REM Check Properties folder status BEFORE running
echo Checking Properties folder status...
if exist "Properties" (
echo Properties folder exists in correct location: %CD%\Properties
if exist "Properties\launchSettings.json" (
  echo launchSettings.json exists
) else (
  echo launchSettings.json NOT found
)
) else (
echo Properties folder does NOT exist
echo Expected location: %CD%\Properties
)

echo.
echo Starting .NET application...
echo Press Ctrl+C to stop the application
echo.

dotnet run

echo.
echo ================================
echo Application has stopped running
echo ================================

REM Check Properties folder status AFTER running
echo.
echo Checking Properties folder status after run...
if exist "Properties" (
echo Properties folder exists in: %CD%\Properties
if exist "Properties\launchSettings.json" (
  echo launchSettings.json exists
)
) else (
echo Properties folder still does NOT exist
)

pause