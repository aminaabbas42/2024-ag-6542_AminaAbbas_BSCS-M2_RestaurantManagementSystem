# Restaurant Management System
## CS-412 Visual Programming — Semester Project Documentation

**Student:** [Your Name]  
**Roll No:** [Your Roll No]  
**Degree & Section:** [Your Degree & Section]  
**Submission Date:** [Date]

---

# Section 1: SRS / Design Document

## 1.1 Problem Statement

Restaurant staff currently manage orders, menu items, tables, payments, and inventory using
manual methods (paper slips or basic spreadsheets). This leads to:
- Order errors and delays
- Difficulty tracking table availability in real time
- No centralized payment recording
- No low-stock alerts for kitchen inventory
- No sales reporting for management decisions

**CaféDesk Restaurant Management System** solves these problems by providing a single,
integrated desktop application for all daily restaurant operations.

## 1.2 Objectives

1. Automate order placement from table selection to payment
2. Provide role-based access (Admin, Cashier, Waiter, Chef)
3. Enable real-time table availability tracking
4. Maintain digital menu with category management
5. Record and track all payments
6. Monitor inventory stock levels with low-stock alerts
7. Generate sales and revenue reports
8. Maintain a full audit log of system activity

## 1.3 User Roles

| Role | Permissions |
|------|-------------|
| Admin | Full access to all modules including User Management |
| Cashier | Orders, Payments, Menu, Inventory, Reports, Tables |
| Waiter | Orders and Tables only |
| Chef | View-only access to Orders (for kitchen reference) |

## 1.4 Features List

### Core CRUD Features (Minimum Requirement)
- ✅ **Menu Items** — Create, Read, Update, Delete, Toggle Availability
- ✅ **Orders** — Create, Read, Cancel (soft delete), View Details
- ✅ **Tables** — Create, Read, Update Status
- ✅ **Inventory** — Create, Read, Update, Delete
- ✅ **Users** — Create, Read, Update, Delete (Admin only)
- ✅ **Categories** — Create via MenuItemDialog, Read in combo boxes

### Additional Features
- ✅ User login with role-based access control
- ✅ Dashboard with live statistics (orders, revenue, tables, pending)
- ✅ Search and filter on all list views
- ✅ POS-style order creation (point of sale)
- ✅ Payment processing with change calculation
- ✅ 5 report types with date range filtering
- ✅ Inventory low-stock visual alerts
- ✅ Receipt/order details view with print option
- ✅ Audit log table (tracks all key user actions)
- ✅ SHA-256 password hashing

---

# Section 2: Database Design

## 2.1 Entity Relationship Description

The database contains **8 tables** with proper foreign key relationships:

```
Users (1) ──────────── (N) Orders
RestaurantTables (1) ── (N) Orders
Categories (1) ──────── (N) MenuItems
Orders (1) ─────────── (N) OrderItems
MenuItems (1) ──────── (N) OrderItems
Orders (1) ─────────── (1) Payments   [UNIQUE constraint]
Users (1) ──────────── (N) AuditLog
```

## 2.2 Table Schemas

### Users
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| UserID | INTEGER | PK, AUTOINCREMENT | Unique user identifier |
| Username | TEXT | NOT NULL, UNIQUE | Login name (case-insensitive) |
| Password | TEXT | NOT NULL | SHA-256 hashed password |
| Role | TEXT | CHECK (Admin/Cashier/Waiter/Chef) | Access level |
| FullName | TEXT | NOT NULL | Display name |
| IsActive | INTEGER | DEFAULT 1, CHECK(0 or 1) | Soft disable without deleting |
| CreatedAt | DATETIME | DEFAULT CURRENT_TIMESTAMP | Account creation time |

### Categories
| Column | Type | Constraints |
|--------|------|-------------|
| CategoryID | INTEGER | PK, AUTOINCREMENT |
| CategoryName | TEXT | NOT NULL, UNIQUE |
| Description | TEXT | Optional |
| SortOrder | INTEGER | Display order |

### MenuItems
| Column | Type | Constraints |
|--------|------|-------------|
| ItemID | INTEGER | PK, AUTOINCREMENT |
| ItemName | TEXT | NOT NULL |
| CategoryID | INTEGER | FK → Categories, NOT NULL |
| Price | REAL | NOT NULL, CHECK ≥ 0 |
| Description | TEXT | Optional |
| IsAvailable | INTEGER | CHECK(0 or 1), DEFAULT 1 |
| CreatedAt | DATETIME | DEFAULT CURRENT_TIMESTAMP |

### RestaurantTables
| Column | Type | Constraints |
|--------|------|-------------|
| TableID | INTEGER | PK, AUTOINCREMENT |
| TableNumber | INTEGER | NOT NULL, UNIQUE |
| Capacity | INTEGER | NOT NULL, CHECK > 0 |
| Status | TEXT | CHECK(Available/Occupied/Reserved) |

### Orders
| Column | Type | Constraints |
|--------|------|-------------|
| OrderID | INTEGER | PK, AUTOINCREMENT |
| TableID | INTEGER | FK → RestaurantTables, NOT NULL |
| UserID | INTEGER | FK → Users, NOT NULL |
| OrderDate | DATETIME | DEFAULT CURRENT_TIMESTAMP |
| Status | TEXT | CHECK(Open/Paid/Cancelled) |
| TotalAmount | REAL | CHECK ≥ 0 |
| Notes | TEXT | Special instructions |

### OrderItems
| Column | Type | Constraints |
|--------|------|-------------|
| OrderItemID | INTEGER | PK, AUTOINCREMENT |
| OrderID | INTEGER | FK → Orders, CASCADE DELETE |
| ItemID | INTEGER | FK → MenuItems, RESTRICT |
| Quantity | INTEGER | CHECK > 0 |
| UnitPrice | REAL | Price at time of order |
| SubTotal | REAL | Quantity × UnitPrice |

### Payments
| Column | Type | Constraints |
|--------|------|-------------|
| PaymentID | INTEGER | PK, AUTOINCREMENT |
| OrderID | INTEGER | FK → Orders, UNIQUE (1 payment per order) |
| AmountPaid | REAL | CHECK ≥ 0 |
| PaymentMethod | TEXT | CHECK(Cash/Credit Card/Debit Card/Mobile Pay) |
| PaymentDate | DATETIME | DEFAULT CURRENT_TIMESTAMP |

### Inventory
| Column | Type | Constraints |
|--------|------|-------------|
| InventoryID | INTEGER | PK, AUTOINCREMENT |
| ItemName | TEXT | NOT NULL, UNIQUE |
| Quantity | REAL | CHECK ≥ 0 |
| Unit | TEXT | kg/liters/unit |
| MinStock | REAL | Low-stock threshold |
| LastUpdated | DATETIME | Updated on each edit |

### AuditLog
| Column | Type | Description |
|--------|------|-------------|
| LogID | INTEGER | PK |
| UserID | INTEGER | FK → Users (nullable) |
| Action | TEXT | Action description |
| TableName | TEXT | Which DB table was affected |
| RecordID | INTEGER | Which record was affected |
| Details | TEXT | Extra info |
| LogDate | DATETIME | Timestamp |

## 2.3 Key Design Decisions

- **PRAGMA foreign_keys = ON** — Enabled on every connection to enforce referential integrity
- **PRAGMA journal_mode = WAL** — Write-Ahead Logging for better read/write concurrency
- **SHA-256 hashing** — Passwords never stored in plain text
- **CHECK constraints** — Prevent invalid data at the database level
- **ON DELETE CASCADE** on OrderItems — Deleting an order automatically removes its items
- **ON DELETE RESTRICT** on Orders→Tables — Cannot delete a table that has orders
- **UNIQUE on Payments.OrderID** — Prevents double-payment for the same order
- **Indexes** on Status, OrderDate, CategoryID — Query performance optimization

---

# Section 3: Form Designs

## Form List (16 UI Classes)

| # | Form/Class | Type | Purpose |
|---|-----------|------|---------|
| 1 | LoginForm | Form | User authentication |
| 2 | MainDashboard | Form | Main window, sidebar nav |
| 3 | HomePanel | UserControl | Dashboard stats overview |
| 4 | OrdersForm | UserControl | Order list & management |
| 5 | NewOrderForm | Form (Dialog) | POS order creation |
| 6 | OrderDetailsForm | Form (Dialog) | Receipt / order details |
| 7 | MenuForm | UserControl | Menu item list |
| 8 | MenuItemDialog | Form (Dialog) | Add/Edit menu item |
| 9 | TablesForm | UserControl | Visual table floor plan |
| 10 | PaymentsForm | UserControl | Payment processing + history |
| 11 | PaymentDialog | Form (Dialog) | Process single payment |
| 12 | InventoryForm | UserControl | Inventory list |
| 13 | InventoryDialog | Form (Dialog) | Add/Edit inventory item |
| 14 | ReportsForm | UserControl | Report generation |
| 15 | UsersForm | UserControl | Staff management (Admin only) |
| 16 | UserDialog | Form (Dialog) | Add/Edit user account |

### Form Navigation Flow
```
LoginForm
    └── MainDashboard (sidebar navigation)
            ├── HomePanel (default view)
            ├── OrdersForm ──► NewOrderForm (dialog)
            │              ──► OrderDetailsForm (dialog)
            ├── MenuForm ────► MenuItemDialog (dialog)
            ├── TablesForm
            ├── PaymentsForm ► PaymentDialog (dialog)
            ├── InventoryForm ► InventoryDialog (dialog)
            ├── ReportsForm
            └── UsersForm ──► UserDialog (dialog)  [Admin only]
```

---

# Section 4: Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER (UI)               │
│  Windows Forms — LoginForm, Dashboard, All UserControls  │
│  Responsible for: Display, User Input, Navigation        │
└───────────────────────┬─────────────────────────────────┘
                        │ calls
┌───────────────────────▼─────────────────────────────────┐
│                    DATA ACCESS LAYER                     │
│  DatabaseHelper.cs                                       │
│  - ExecuteQuery(), ExecuteNonQuery(), ExecuteScalar()    │
│  - ExecuteTransaction() — atomic multi-step operations   │
│  - LogAction() — audit trail                             │
│  - HashPassword() — SHA-256 security                     │
│  All queries use parameterized inputs (no SQL injection)  │
└───────────────────────┬─────────────────────────────────┘
                        │ reads/writes
┌───────────────────────▼─────────────────────────────────┐
│                    DATA LAYER                            │
│  SQLite Database (RestaurantDB.sqlite)                   │
│  8 Tables: Users, Categories, MenuItems,                 │
│  RestaurantTables, Orders, OrderItems, Payments,         │
│  Inventory, AuditLog                                     │
└─────────────────────────────────────────────────────────┘

            ┌──────────────────┐
            │   MODELS LAYER   │
            │  Models/Models.cs │
            │  Plain C# objects │
            │  (User, Order,   │
            │  MenuItem, etc.)  │
            └──────────────────┘
            (used across all layers)
```

**Why this architecture?**
Separating UI, Data Access, and Models follows the **3-tier architecture** pattern. This means:
- Changing the database (e.g., SQLite → SQL Server) only requires changes in `DatabaseHelper.cs`
- Adding a new form does not require touching the database logic
- Models are reusable across any number of forms

---

# Section 5: Technical Manual

## Prerequisites
- Windows 10 or 11
- .NET 8.0 SDK — https://dotnet.microsoft.com/download/dotnet/8.0
- Visual Studio 2022 Community with ".NET desktop development" workload

## NuGet Packages (auto-restored)
| Package | Version | Purpose |
|---------|---------|---------|
| System.Data.SQLite | 1.0.118 | SQLite database driver |
| Microsoft.VisualBasic | 10.3.0 | InputBox for table creation |

## Setup Steps
1. Extract project ZIP to a folder
2. Open `RestaurantManagementSystem.sln` in Visual Studio 2022
3. Press **F5** — packages restore automatically
4. Database is created automatically on first run
5. Login with `admin` / `admin123`

## Project File Structure
```
RestaurantManagementSystem/
├── Program.cs              Entry point — calls DB init, opens LoginForm
├── DatabaseHelper.cs       All database operations (ADO.NET)
├── Models/
│   └── Models.cs           Data model classes + Session
├── Forms/
│   ├── LoginForm.cs        Authentication UI
│   ├── MainDashboard.cs    Main window with navigation
│   ├── HomePanel.cs        Dashboard statistics
│   ├── OrdersForm.cs       Order list and management
│   ├── NewOrderForm.cs     POS order creation
│   ├── OrderDetailsForm.cs Receipt view
│   ├── MenuForm.cs         Menu CRUD
│   └── OtherForms.cs       Tables, Payments, Inventory, Reports, Users
├── .gitignore
├── DEPLOYMENT.md
├── REPOSITORY.txt
└── README.md
```

---

# Section 6: User Manual

## 6.1 Logging In
1. Launch the application
2. Enter username and password (default: `admin` / `admin123`)
3. Click **LOGIN** or press **Enter**
4. On success, the main dashboard opens

## 6.2 Dashboard (Home)
The home screen shows 4 live stat cards:
- **Today's Orders** — count of orders placed today
- **Today's Revenue** — total paid revenue today
- **Available Tables** — how many tables are free
- **Pending Orders** — open unpaid orders

A recent orders table shows the last 20 orders.

## 6.3 Creating a New Order
1. Click **Orders** in the sidebar
2. Click **+ New Order**
3. Select an available table from the dropdown
4. Filter menu items by category if needed
5. Click a menu item, enter quantity, click **➕ Add to Order**
6. Repeat for each item
7. Add optional notes (allergies, special requests)
8. Click **✔ PLACE ORDER**

The table status changes to "Occupied" automatically.

## 6.4 Processing Payment
1. Click **Payments** in the sidebar
2. Select the open order from the list
3. Click **💳 Process Payment**
4. Choose payment method (Cash/Card/Mobile)
5. Enter amount received
6. Change is calculated automatically
7. Click **✔ Confirm Payment**

The order status changes to "Paid" and the table becomes "Available".

## 6.5 Menu Management
1. Click **Menu Items** in the sidebar
2. Click **+ Add Item** to create a new dish
3. Select an item and click **✎ Edit** to modify it
4. Click **Toggle Avail.** to temporarily hide an item from orders
5. Click **🗑 Delete** to permanently remove an item (with confirmation)

## 6.6 Table Management
The Tables view shows a visual floor plan. Each card shows:
- Table number and capacity
- Color: 🟢 Green = Available, 🔴 Red = Occupied, 🟡 Yellow = Reserved

Right-click (or click) a card to change its status.

## 6.7 Inventory
1. Click **Inventory** in the sidebar
2. Items highlighted in red in the Status column are **Low Stock**
3. Click **+ Add Item** to add a new ingredient
4. Click a row then **✎ Edit** to update stock levels

## 6.8 Reports
1. Click **Reports** in the sidebar
2. Select a report type:
   - Sales Summary (by date)
   - Orders by Table
   - Top Menu Items
   - Payment Methods
   - Daily Revenue
3. Set date range using the From/To date pickers
4. Click **Generate Report**

## 6.9 User Management (Admin Only)
1. Click **Users** in the sidebar (visible to Admin only)
2. Click **+ Add User** to create a new staff account
3. Assign a role: Admin, Cashier, Waiter, or Chef
4. Use **✎ Edit** to change name, username, password, or role
5. Click **🗑 Delete** to remove a user (cannot delete your own account)

---

# Section 7: Challenges & Solutions

| Challenge | Solution |
|-----------|----------|
| SQLite `Tables` is a reserved word causing syntax ambiguity | Renamed the table to `RestaurantTables` throughout schema and all queries |
| Passwords stored in plain text — security risk | Implemented SHA-256 hashing in `DatabaseHelper.HashPassword()` |
| Foreign keys silently ignored in SQLite | Added `PRAGMA foreign_keys = ON` to every connection via `EnablePragmas()` |
| Payment could be recorded twice for one order | Added `UNIQUE` constraint on `Payments.OrderID` at the DB level |
| Manual transaction management caused inconsistent rollback | Created `ExecuteTransaction(Action)` helper that wraps all logic and guarantees rollback |
| No record of who changed what | Added `AuditLog` table and `LogAction()` method called on key operations |
| No data validation on DB inserts | Added `CHECK` constraints on price, status, role fields at schema level |
| Slow queries on large order lists | Added performance indexes on `Status`, `OrderDate`, `CategoryID` |

---

# Section 8: Trade-offs & Decisions

## Database: SQLite vs SQL Server
**Chose SQLite because:**
- No server installation required — just a single `.sqlite` file
- Perfect for single-machine desktop applications
- Zero configuration, works immediately on any Windows machine
- Deployment is simpler — just copy the file

**Trade-off:** SQLite does not support multiple concurrent write users well. If this were a networked system with 10+ simultaneous staff terminals, SQL Server would be the better choice.

## Data Access: ADO.NET vs Entity Framework
**Chose ADO.NET because:**
- Direct control over every SQL query
- Better performance for bulk reads (DataTable + DataGridView pattern)
- Easier to understand for academic review — no "magic" abstraction
- No need to install EF NuGet packages or manage migrations

**Trade-off:** More verbose code. EF Core would generate queries automatically and reduce boilerplate in the data access layer.

## UI: Windows Forms vs WPF
**Chose Windows Forms because:**
- Required by the course specification
- Excellent DataGridView support for tabular data
- Simpler event-driven programming model
- Faster to build for data-entry focused applications

**Trade-off:** WPF offers data binding and MVVM pattern for cleaner separation, and handles high-DPI displays better.

## Architecture: 3-Tier vs MVC
**Chose 3-tier (UI / Data Access / Database) because:**
- Simpler to implement in Windows Forms
- Clear responsibility: Forms handle display, DatabaseHelper handles all DB calls, Models hold data
- Easy to explain and navigate during viva

**Trade-off:** Full MVC or MVVM would give better testability (unit tests on ViewModels) but adds complexity.
