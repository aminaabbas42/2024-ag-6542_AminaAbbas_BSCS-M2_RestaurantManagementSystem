# 🍽 Restaurant Management System
### C# Windows Forms | SQLite Database | .NET 8

---

## 📁 Project Structure

```
RestaurantManagementSystem/
├── RestaurantManagementSystem.sln       ← Open this in Visual Studio
├── RestaurantManagementSystem.csproj    ← Project file
├── Program.cs                           ← Entry point
├── DatabaseHelper.cs                    ← SQLite DB engine (backend)
├── Models/
│   └── Models.cs                        ← All data models + Session
└── Forms/
    ├── LoginForm.cs                     ← Login screen
    ├── MainDashboard.cs                 ← Main window with sidebar nav
    ├── HomePanel.cs                     ← Dashboard stats overview
    ├── OrdersForm.cs                    ← Order list & management
    ├── NewOrderForm.cs                  ← POS-style new order creation
    ├── OrderDetailsForm.cs              ← Order details & receipt
    ├── MenuForm.cs                      ← Menu item CRUD
    └── OtherForms.cs                    ← Tables, Payments, Inventory,
                                            Reports, Users
```

---

## 🚀 Setup in Visual Studio

### Prerequisites
- Visual Studio 2022 (Community or higher)
- .NET 8 SDK (included with VS 2022)
- Windows OS

### Steps

1. **Open the Solution**
   - Double-click `RestaurantManagementSystem.sln`
   - OR File → Open → Project/Solution in Visual Studio

2. **Restore NuGet Packages**
   - Visual Studio auto-restores on build
   - OR: Tools → NuGet Package Manager → Restore

3. **Build & Run**
   - Press `F5` or click ▶ Start
   - The SQLite database is created automatically on first run

---

## 🔑 Default Login Credentials

| Role     | Username  | Password    |
|----------|-----------|-------------|
| Admin    | admin     | admin123    |
| Waiter   | waiter1   | waiter123   |
| Cashier  | cashier1  | cashier123  |

---

## 🗄 Database (SQLite)

The database `RestaurantDB.sqlite` is created automatically in the same folder as the executable.

### Tables
| Table        | Description                        |
|--------------|------------------------------------|
| Users        | Staff accounts with roles          |
| Categories   | Menu categories                    |
| MenuItems    | Food & drink items with prices     |
| Tables       | Restaurant seating tables          |
| Orders       | Customer orders                    |
| OrderItems   | Individual items per order         |
| Payments     | Payment records per order          |
| Inventory    | Stock tracking with low-stock alert|

---

## 🖥 Features

### Frontend
- **Dark-themed login** with credential validation
- **Sidebar navigation** with hover effects
- **Dashboard** with live stat cards (orders, revenue, tables, pending)
- **Table map** with color-coded status (Available/Occupied/Reserved)
- **POS-style order entry** with category filtering

### Backend
- **Full CRUD** for Menu, Users, Inventory
- **Order lifecycle**: Open → Paid / Cancelled
- **Payment processing** with change calculation
- **5 report types** with date range filtering
- **Role-based access** (Admin sees Users section)
- **SQLite transactions** for data integrity

### Modules
1. 📋 **Orders** – Create, view, cancel orders
2. 🍕 **Menu** – Add/edit/delete items, toggle availability
3. 🪑 **Tables** – Visual floor plan, status management
4. 💳 **Payments** – Process payments (Cash/Card/Mobile)
5. 📦 **Inventory** – Stock management with low-stock alerts
6. 📊 **Reports** – Sales, revenue, top items, payment methods
7. 👥 **Users** – (Admin only) Staff account management

---

## 🛠 Technologies

- **Language**: C# 12
- **Framework**: .NET 8 Windows Forms
- **Database**: SQLite via `System.Data.SQLite`
- **UI**: Windows Forms with custom styling (no third-party UI library)
- **IDE**: Visual Studio 2022

---

## 📦 NuGet Packages

```xml
<PackageReference Include="System.Data.SQLite" Version="1.0.118" />
<PackageReference Include="Microsoft.VisualBasic" Version="10.3.0" />
```

These install automatically when you build in Visual Studio.
