# PostgreSQL Installation Guide - Telegram Channel Downloader

## Step 1: Download PostgreSQL

1. **Visit the official PostgreSQL download page:**
   - Go to: https://www.postgresql.org/download/windows/
   - Click "Download the installer"
   - Select **PostgreSQL 16.x** for Windows x86-64
   - Download the installer (approximately 200MB)

## Step 2: Install PostgreSQL

1. **Run the installer as Administrator:**
   - Right-click the downloaded `.exe` file
   - Select "Run as administrator"

2. **Follow the installation wizard:**
   - **Installation Directory**: Keep default `C:\Program Files\PostgreSQL\16`
   - **Components**: Select all:
     - [x] PostgreSQL Server
     - [x] pgAdmin 4 (GUI management tool)
     - [x] Stack Builder (additional tools)
     - [x] Command Line Tools
   - **Data Directory**: Keep default `C:\Program Files\PostgreSQL\16\data`
   - **Password**: Set a strong password for the `postgres` superuser
     - **IMPORTANT**: Remember this password! You'll need it.
     - Example: `PostgresAdmin2024!`
   - **Port**: Keep default `5432`
   - **Locale**: Keep default (Default locale)

3. **Complete Installation:**
   - Click "Next" through remaining steps
   - Click "Finish" to complete installation

## Step 3: Verify Installation

1. **Open Command Prompt as Administrator**
2. **Test PostgreSQL installation:**
   ```cmd
   "C:\Program Files\PostgreSQL\16\bin\psql" --version
   ```
   You should see: `psql (PostgreSQL) 16.x`

3. **Test connection:**
   ```cmd
   "C:\Program Files\PostgreSQL\16\bin\psql" -U postgres -h localhost
   ```
   Enter the password you set during installation.

## Step 4: Run Database Setup Script

1. **Navigate to your project directory:**
   ```cmd
   cd "C:\Users\User\RiderProjects\TelegramChanelDowonloader"
   ```

2. **Run the database setup script:**
   ```cmd
   "C:\Program Files\PostgreSQL\16\bin\psql" -U postgres -h localhost -f setup-database.sql
   ```

3. **You should see output like:**
   ```
   CREATE ROLE
   CREATE DATABASE
   GRANT
   You are now connected to database "telegram_downloads" as user "postgres".
   GRANT
   GRANT
   GRANT
   CREATE EXTENSION
   CREATE EXTENSION
   telegram_downloads | telegram_user
   Database setup completed successfully!
   ```

## Step 5: Update Application Configuration

The configuration file has already been updated to use the new database credentials:
- **Database**: telegram_downloads
- **Username**: telegram_user
- **Password**: secure_password_123

## Step 6: Create Database Schema

1. **Navigate to Desktop project:**
   ```cmd
   cd "C:\Users\User\RiderProjects\TelegramChanelDowonloader\src\TelegramChannelDownloader.Desktop"
   ```

2. **Run Entity Framework migration:**
   ```cmd
   dotnet ef database update --project ../TelegramChannelDownloader.Core --startup-project .
   ```

   You should see output like:
   ```
   Build succeeded.
   Applying migration '20250907152831_InitialTelegramSchema'.
   Done.
   ```

## Step 7: Verify Everything Works

1. **Test application database connection:**
   ```cmd
   "C:\Program Files\PostgreSQL\16\bin\psql" -U telegram_user -d telegram_downloads -h localhost
   ```
   Password: `secure_password_123`

2. **Check created tables:**
   ```sql
   \dt
   ```
   You should see:
   ```
   public | download_sessions | table | telegram_user
   public | telegram_message  | table | telegram_user
   ```

3. **Exit PostgreSQL:**
   ```sql
   \q
   ```

## Troubleshooting

### PostgreSQL Service Not Running
```cmd
net start postgresql-x64-16
```

### Connection Issues
1. Check Windows Firewall settings
2. Verify PostgreSQL service is running
3. Confirm port 5432 is available

### Permission Issues
- Make sure you run Command Prompt as Administrator
- Check that the postgres user password is correct

### Path Issues
If `psql` command is not found, add PostgreSQL to your PATH:
1. Open System Properties → Environment Variables
2. Add `C:\Program Files\PostgreSQL\16\bin` to your PATH

## Success Indicators

✅ PostgreSQL 16.x installed successfully  
✅ Database `telegram_downloads` created  
✅ User `telegram_user` created with proper permissions  
✅ UUID extension enabled  
✅ Entity Framework migration applied successfully  
✅ Application can connect to database  

Your PostgreSQL database is now ready for the Telegram Channel Downloader!