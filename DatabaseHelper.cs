using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantManagementSystem
{
    /// <summary>
    /// Central database access layer.
    /// FIXES APPLIED:
    ///  1. Foreign Keys enabled on every connection (PRAGMA foreign_keys=ON)
    ///  2. WAL journal mode for better concurrency
    ///  3. Passwords stored as SHA-256 hashes, never plain text
    ///  4. CHECK constraints on Status, Role, Price, Quantity
    ///  5. Renamed Tables -> RestaurantTables (Tables is an SQL keyword clash)
    ///  6. Payments.OrderID is UNIQUE (one payment per order)
    ///  7. ON DELETE CASCADE on OrderItems so deleting an order removes its items
    ///  8. Performance indexes added
    ///  9. AuditLog table tracks every important action
    /// 10. ExecuteTransaction helper wraps logic in atomic transactions
    /// </summary>
    public static class DatabaseHelper
    {
        private static readonly string dbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "RestaurantDB.sqlite");

        public static string ConnectionString =>
            $"Data Source={dbPath};Version=3;Foreign Keys=True;Journal Mode=WAL;";

        // ─────────────────────────────────────────────
        // INITIALIZATION
        // ─────────────────────────────────────────────

        public static void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
                SQLiteConnection.CreateFile(dbPath);

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            EnablePragmas(conn);
            CreateTables(conn);
            SeedData(conn);
        }

        private static void EnablePragmas(SQLiteConnection conn)
        {
            Execute(conn, "PRAGMA foreign_keys = ON;");
            Execute(conn, "PRAGMA journal_mode = WAL;");
        }

        private static void CreateTables(SQLiteConnection conn)
        {
            Execute(conn, @"CREATE TABLE IF NOT EXISTS Users (
                UserID    INTEGER PRIMARY KEY AUTOINCREMENT,
                Username  TEXT NOT NULL UNIQUE COLLATE NOCASE,
                Password  TEXT NOT NULL,
                Role      TEXT NOT NULL DEFAULT 'Waiter'
                          CHECK(Role IN ('Admin','Cashier','Waiter','Chef')),
                FullName  TEXT NOT NULL,
                IsActive  INTEGER NOT NULL DEFAULT 1 CHECK(IsActive IN (0,1)),
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Categories (
                CategoryID   INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryName TEXT NOT NULL UNIQUE,
                Description  TEXT,
                SortOrder    INTEGER DEFAULT 0
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS MenuItems (
                ItemID      INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemName    TEXT NOT NULL,
                CategoryID  INTEGER NOT NULL,
                Price       REAL NOT NULL CHECK(Price >= 0),
                Description TEXT,
                IsAvailable INTEGER NOT NULL DEFAULT 1 CHECK(IsAvailable IN (0,1)),
                CreatedAt   DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
                    ON DELETE RESTRICT ON UPDATE CASCADE
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS RestaurantTables (
                TableID     INTEGER PRIMARY KEY AUTOINCREMENT,
                TableNumber INTEGER NOT NULL UNIQUE,
                Capacity    INTEGER NOT NULL CHECK(Capacity > 0),
                Status      TEXT NOT NULL DEFAULT 'Available'
                            CHECK(Status IN ('Available','Occupied','Reserved'))
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Orders (
                OrderID     INTEGER PRIMARY KEY AUTOINCREMENT,
                TableID     INTEGER NOT NULL,
                UserID      INTEGER NOT NULL,
                OrderDate   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Status      TEXT NOT NULL DEFAULT 'Open'
                            CHECK(Status IN ('Open','Paid','Cancelled')),
                TotalAmount REAL NOT NULL DEFAULT 0 CHECK(TotalAmount >= 0),
                Notes       TEXT,
                FOREIGN KEY (TableID) REFERENCES RestaurantTables(TableID)
                    ON DELETE RESTRICT ON UPDATE CASCADE,
                FOREIGN KEY (UserID)  REFERENCES Users(UserID)
                    ON DELETE RESTRICT ON UPDATE CASCADE
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS OrderItems (
                OrderItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderID     INTEGER NOT NULL,
                ItemID      INTEGER NOT NULL,
                Quantity    INTEGER NOT NULL CHECK(Quantity > 0),
                UnitPrice   REAL NOT NULL CHECK(UnitPrice >= 0),
                SubTotal    REAL NOT NULL CHECK(SubTotal >= 0),
                FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
                    ON DELETE CASCADE ON UPDATE CASCADE,
                FOREIGN KEY (ItemID)  REFERENCES MenuItems(ItemID)
                    ON DELETE RESTRICT ON UPDATE CASCADE
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Payments (
                PaymentID     INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderID       INTEGER NOT NULL UNIQUE,
                AmountPaid    REAL NOT NULL CHECK(AmountPaid >= 0),
                PaymentMethod TEXT NOT NULL DEFAULT 'Cash'
                              CHECK(PaymentMethod IN ('Cash','Credit Card','Debit Card','Mobile Pay')),
                PaymentDate   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
                    ON DELETE RESTRICT ON UPDATE CASCADE
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Inventory (
                InventoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemName    TEXT NOT NULL UNIQUE,
                Quantity    REAL NOT NULL DEFAULT 0 CHECK(Quantity >= 0),
                Unit        TEXT NOT NULL DEFAULT 'unit',
                MinStock    REAL NOT NULL DEFAULT 10 CHECK(MinStock >= 0),
                LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
            )");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS AuditLog (
                LogID     INTEGER PRIMARY KEY AUTOINCREMENT,
                UserID    INTEGER,
                Action    TEXT NOT NULL,
                TableName TEXT,
                RecordID  INTEGER,
                Details   TEXT,
                LogDate   DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE SET NULL
            )");

            // Performance indexes
            Execute(conn, "CREATE INDEX IF NOT EXISTS idx_orders_status    ON Orders(Status)");
            Execute(conn, "CREATE INDEX IF NOT EXISTS idx_orders_date      ON Orders(OrderDate)");
            Execute(conn, "CREATE INDEX IF NOT EXISTS idx_orderitems_order ON OrderItems(OrderID)");
            Execute(conn, "CREATE INDEX IF NOT EXISTS idx_menu_category    ON MenuItems(CategoryID)");
        }

        // ─────────────────────────────────────────────
        // SEED DATA
        // ─────────────────────────────────────────────

        private static void SeedData(SQLiteConnection conn)
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", conn))
            {
                if ((long)cmd.ExecuteScalar() == 0)
                {
                    InsertUser(conn, "admin",    "admin123",   "Admin",   "Administrator");
                    InsertUser(conn, "waiter1",  "waiter123",  "Waiter",  "John Doe");
                    InsertUser(conn, "cashier1", "cashier123", "Cashier", "Jane Smith");
                    InsertUser(conn, "chef1",    "chef123",    "Chef",    "Ali Hassan");
                }
            }

            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Categories", conn))
            {
                if ((long)cmd.ExecuteScalar() == 0)
                {
                    Execute(conn, "INSERT INTO Categories(CategoryName,Description,SortOrder) VALUES('Appetizers','Starter dishes',1)");
                    Execute(conn, "INSERT INTO Categories(CategoryName,Description,SortOrder) VALUES('Main Course','Primary dishes',2)");
                    Execute(conn, "INSERT INTO Categories(CategoryName,Description,SortOrder) VALUES('Desserts','Sweet treats',3)");
                    Execute(conn, "INSERT INTO Categories(CategoryName,Description,SortOrder) VALUES('Beverages','Drinks',4)");
                    Execute(conn, "INSERT INTO Categories(CategoryName,Description,SortOrder) VALUES('Soups','Hot soups',5)");
                }
            }

            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM MenuItems", conn))
            {
                if ((long)cmd.ExecuteScalar() == 0)
                {
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Spring Rolls',1,6.99,'Crispy vegetable spring rolls')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Garlic Bread',1,4.99,'Toasted bread with garlic butter')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Chicken Wings',1,9.99,'Spicy buffalo chicken wings')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Grilled Chicken',2,14.99,'Tender grilled chicken breast')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Beef Steak',2,24.99,'Premium beef steak')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Pasta Carbonara',2,12.99,'Classic Italian pasta')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Margherita Pizza',2,13.99,'Classic tomato and mozzarella')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Fish and Chips',2,16.99,'Crispy battered fish with fries')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Chocolate Cake',3,7.99,'Rich chocolate layer cake')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Ice Cream',3,5.99,'3 scoops of choice')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Cheesecake',3,8.99,'New York style cheesecake')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Cola',4,2.99,'Chilled soft drink')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Fresh Juice',4,4.99,'Seasonal fresh juice')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Mineral Water',4,1.99,'Still mineral water')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Coffee',4,3.99,'Freshly brewed coffee')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Tomato Soup',5,5.99,'Classic tomato basil soup')");
                    Execute(conn, "INSERT INTO MenuItems(ItemName,CategoryID,Price,Description) VALUES('Mushroom Soup',5,6.99,'Creamy mushroom soup')");
                }
            }

            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM RestaurantTables", conn))
            {
                if ((long)cmd.ExecuteScalar() == 0)
                {
                    for (int i = 1; i <= 12; i++)
                    {
                        int cap = i <= 4 ? 2 : i <= 9 ? 4 : 6;
                        Execute(conn, $"INSERT INTO RestaurantTables(TableNumber,Capacity,Status) VALUES({i},{cap},'Available')");
                    }
                }
            }

            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Inventory", conn))
            {
                if ((long)cmd.ExecuteScalar() == 0)
                {
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Chicken Breast',50,'kg',10)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Beef',30,'kg',8)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Pasta',20,'kg',5)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Tomatoes',40,'kg',10)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Flour',25,'kg',10)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Milk',15,'liters',5)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Cooking Oil',10,'liters',3)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Sugar',12,'kg',5)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Salt',8,'kg',2)");
                    Execute(conn, "INSERT INTO Inventory(ItemName,Quantity,Unit,MinStock) VALUES('Rice',35,'kg',10)");
                }
            }
        }

        // ─────────────────────────────────────────────
        // PASSWORD HASHING  (SHA-256)
        // ─────────────────────────────────────────────

        public static string HashPassword(string plain)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plain));
            return Convert.ToHexString(bytes).ToLower();
        }

        private static void InsertUser(SQLiteConnection conn,
            string username, string password, string role, string fullName)
        {
            using var cmd = new SQLiteCommand(
                "INSERT INTO Users(Username,Password,Role,FullName) VALUES(@u,@p,@r,@f)", conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", HashPassword(password));
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@f", fullName);
            cmd.ExecuteNonQuery();
        }

        // ─────────────────────────────────────────────
        // AUDIT LOG
        // ─────────────────────────────────────────────

        public static void LogAction(int? userId, string action,
            string? tableName = null, int? recordId = null, string? details = null)
        {
            try
            {
                ExecuteNonQuery(
                    "INSERT INTO AuditLog(UserID,Action,TableName,RecordID,Details) VALUES(@u,@a,@t,@r,@d)",
                    new SQLiteParameter[]
                    {
                        new("@u", (object?)userId ?? DBNull.Value),
                        new("@a", action),
                        new("@t", (object?)tableName ?? DBNull.Value),
                        new("@r", (object?)recordId ?? DBNull.Value),
                        new("@d", (object?)details ?? DBNull.Value)
                    });
            }
            catch { /* audit must never crash the app */ }
        }

        // ─────────────────────────────────────────────
        // PUBLIC DATA ACCESS
        // ─────────────────────────────────────────────

        public static DataTable ExecuteQuery(string sql, SQLiteParameter[]? parameters = null)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            EnablePragmas(conn);
            using var cmd = new SQLiteCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            var dt = new DataTable();
            using var adapter = new SQLiteDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static int ExecuteNonQuery(string sql, SQLiteParameter[]? parameters = null)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            EnablePragmas(conn);
            using var cmd = new SQLiteCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, SQLiteParameter[]? parameters = null)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            EnablePragmas(conn);
            using var cmd = new SQLiteCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Runs multiple DB operations in one atomic transaction.
        /// Rolls back everything automatically if any step throws.
        /// Use this for: placing orders, processing payments, etc.
        /// </summary>
        public static void ExecuteTransaction(Action<SQLiteConnection, SQLiteTransaction> work)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            EnablePragmas(conn);
            using var tx = conn.BeginTransaction();
            try
            {
                work(conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void Execute(SQLiteConnection conn, string sql)
        {
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
