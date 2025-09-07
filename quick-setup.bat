@echo off
echo ================================================
echo PostgreSQL Setup for Telegram Channel Downloader
echo ================================================
echo.

set PSQL_PATH="C:\Program Files\PostgreSQL\16\bin\psql"
set PROJECT_PATH=%cd%

echo Step 1: Testing PostgreSQL installation...
%PSQL_PATH% --version
if %errorlevel% neq 0 (
    echo ERROR: PostgreSQL not found! Please install PostgreSQL 16+ first.
    echo Download from: https://www.postgresql.org/download/windows/
    pause
    exit /b 1
)

echo.
echo Step 2: Setting up database and user...
echo Please enter the PostgreSQL superuser (postgres) password when prompted.
%PSQL_PATH% -U postgres -h localhost -f setup-database.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to setup database. Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Step 3: Applying Entity Framework migrations...
cd "src\TelegramChannelDownloader.Desktop"
dotnet ef database update --project ..\TelegramChannelDownloader.Core --startup-project .
if %errorlevel% neq 0 (
    echo ERROR: Failed to apply EF migrations. Please check the error messages above.
    cd %PROJECT_PATH%
    pause
    exit /b 1
)

cd %PROJECT_PATH%

echo.
echo Step 4: Verifying setup...
echo Please enter password: secure_password_123
%PSQL_PATH% -U telegram_user -d telegram_downloads -h localhost -f verify-setup.sql

echo.
echo ================================================
echo Setup completed successfully!
echo ================================================
echo Database: telegram_downloads
echo Username: telegram_user  
echo Password: secure_password_123
echo.
echo You can now run your Telegram Channel Downloader application.
echo It will automatically use the PostgreSQL database for storage.
pause