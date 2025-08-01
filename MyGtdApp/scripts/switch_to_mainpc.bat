@echo off
echo Switching to MainPC configuration...

REM Move to parent directory (project root)
cd /d "%~dp0.."

REM Check if Properties folder exists, if not create it
if not exist "Properties" (
echo Creating Properties folder...
mkdir Properties
)

REM Check if launchSettings.json exists, if not create it
if not exist "Properties\launchSettings.json" (
echo Creating launchSettings.json...
)

(
echo {
echo   "$schema": "https://json.schemastore.org/launchsettings.json",
echo   "profiles": {
echo     "http": {
echo       "commandName": "Project",
echo       "dotnetRunMessages": true,
echo       "launchBrowser": true,
echo       "applicationUrl": "http://0.0.0.0:61183",
echo       "environmentVariables": {
echo         "ASPNETCORE_ENVIRONMENT": "Development"
echo       }
echo     },
echo     "https": {
echo       "commandName": "Project",
echo       "dotnetRunMessages": true,
echo       "launchBrowser": true,
echo       "applicationUrl": "https://0.0.0.0:61184;http://0.0.0.0:61183",
echo       "environmentVariables": {
echo         "ASPNETCORE_ENVIRONMENT": "Development"
echo       }
echo     }
echo   }
echo }
) > "Properties\launchSettings.json"

echo MainPC configuration applied successfully!
echo HTTP: http://0.0.0.0:61183
echo HTTPS: https://0.0.0.0:61184
echo Current directory: %CD%
pause