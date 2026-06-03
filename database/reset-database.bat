@echo off
setlocal

set "DB_NAME=%~1"
if "%DB_NAME%"=="" set "DB_NAME=roadmap_platform"

set "DB_USER=%PGUSER%"
if "%DB_USER%"=="" set "DB_USER=postgres"

set "DB_HOST=%PGHOST%"
if "%DB_HOST%"=="" set "DB_HOST=localhost"

set "DB_PORT=%PGPORT%"
if "%DB_PORT%"=="" set "DB_PORT=5432"

cd /d "%~dp0"

echo Resetting database schema for %DB_NAME%...
psql -h "%DB_HOST%" -p "%DB_PORT%" -U "%DB_USER%" -d "%DB_NAME%" -v ON_ERROR_STOP=1 -f "reset-database.sql"

if errorlevel 1 (
    echo Reset failed.
    exit /b 1
)

echo Reset completed.
endlocal
