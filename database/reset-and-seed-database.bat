@echo off
setlocal

set "DB_NAME=%~1"
if "%DB_NAME%"=="" set "DB_NAME=roadmap_platform"

cd /d "%~dp0"

call "reset-database.bat" "%DB_NAME%"
if errorlevel 1 exit /b 1

call "apply-schema.bat" "%DB_NAME%"
if errorlevel 1 exit /b 1

call "seed.bat" "%DB_NAME%"
if errorlevel 1 exit /b 1

echo Database setup completed.
endlocal
