# DEPLOYMENT GUIDE
## Restaurant Management System — CS-412 Visual Programming

---

## System Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 10 / Windows 11 |
| .NET SDK | .NET 8.0 (Windows) |
| IDE | Visual Studio 2022 (Community or higher) |
| Database | SQLite (auto-created — no separate install needed) |
| RAM | 4 GB minimum |
| Disk Space | ~150 MB (including .NET runtime) |

---

## Step-by-Step Installation on a New Machine

### Step 1 — Install .NET 8 SDK
1. Visit: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. Download **.NET 8.0 SDK** (Windows x64)
3. Run the installer and follow prompts
4. Verify: open Command Prompt and run `dotnet --version` (should show `8.x.x`)

### Step 2 — Install Visual Studio 2022
1. Download from: https://visualstudio.microsoft.com/vs/community/
2. During install, select the workload:
   ✅ **.NET desktop development**
3. Complete the installation (~5–15 minutes)

### Step 3 — Get the Project
**Option A — From ZIP file:**
1. Extract the ZIP file to any folder (e.g., `C:\Projects\RestaurantMS\`)
2. Make sure `bin\` and `obj\` folders are NOT present (already removed)

**Option B — From Git:**
```bash
git clone https://github.com/YOUR_USERNAME/RestaurantManagementSystem.git
cd RestaurantManagementSystem
```

### Step 4 — Open in Visual Studio
1. Open Visual Studio 2022
2. Click **File → Open → Project/Solution**
3. Navigate to the project folder
4. Select `RestaurantManagementSystem.sln`
5. Click **Open**

### Step 5 — Restore NuGet Packages
Visual Studio will automatically restore packages on first build.

If it doesn't:
- Go to **Tools → NuGet Package Manager → Manage NuGet Packages for Solution**
- Click **Restore**

Or via terminal:
```bash
dotnet restore
```

### Step 6 — Build and Run
1. Press **F5** (Run with Debugging) or **Ctrl+F5** (Run without Debugging)
2. The application will start and automatically:
   - Create the SQLite database file (`RestaurantDB.sqlite`)
   - Create all tables
   - Insert sample/seed data

---

## Database Migration

The database is **SQLite** and requires **no separate server installation**.

### Automatic Setup
The database is created automatically on first run using `DatabaseHelper.InitializeDatabase()`.
File location: same folder as the executable (`bin\Debug\net8.0-windows\RestaurantDB.sqlite`)

### Manual Recreation (if needed)
If you need to recreate the database from scratch:
1. Delete `RestaurantDB.sqlite` (if it exists)
2. Run the application — it will auto-create a fresh database

### Database Script
The full database schema is embedded in `DatabaseHelper.cs` in the `CreateTables()` method.

---

## Default Login Credentials

| Role | Username | Password |
|------|----------|----------|
| Admin | admin | admin123 |
| Waiter | waiter1 | waiter123 |
| Cashier | cashier1 | cashier123 |
| Chef | chef1 | chef123 |

> **Note:** Passwords are stored as SHA-256 hashes in the database for security.

---

## Common Issues & Solutions

| Problem | Solution |
|---------|----------|
| App won't start | Ensure .NET 8 SDK is installed: `dotnet --version` |
| NuGet restore fails | Check internet connection; go to Tools → NuGet → Restore |
| Database error on launch | Delete `RestaurantDB.sqlite` and restart the app |
| Login fails | Use exact credentials from table above (case-sensitive) |
| Missing SQLite DLL | NuGet package `System.Data.SQLite` must be restored |

---

## Cross-Platform Note

This application targets `net8.0-windows` and uses Windows Forms.
It runs on **Windows 10 and Windows 11** of any version (32-bit or 64-bit).

For cross-platform deployment, the project would need migration to .NET MAUI
(beyond the scope of this submission).

---

## Quick Start Summary

```
1. Install .NET 8 SDK
2. Install Visual Studio 2022 with ".NET desktop development"
3. Open RestaurantManagementSystem.sln
4. Press F5
5. Login: admin / admin123
```
