using System;
using System.Collections.Generic;

/// <summary>
/// Models namespace contains all data model classes that mirror the database tables.
/// These are plain C# objects (POCOs) used to transfer data between the UI and database layers.
/// </summary>
namespace RestaurantManagementSystem.Models
{
    /// <summary>
    /// Represents a staff member who can log into the system.
    /// Roles: Admin, Cashier, Waiter, Chef
    /// </summary>
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }   // Stored as SHA-256 hash, never plain text
        public string Role { get; set; }        // Controls which modules are visible
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Groups menu items into sections (e.g., Appetizers, Main Course, Desserts).
    /// Each MenuItem belongs to exactly one Category.
    /// </summary>
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// A single item available for ordering from the restaurant menu.
    /// IsAvailable can be toggled to temporarily hide items (e.g. when out of stock).
    /// </summary>
    public class MenuItem
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }  // Populated via JOIN query for display
        public decimal Price { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// A physical dining table in the restaurant.
    /// Status can be: Available, Occupied, Reserved
    /// </summary>
    public class RestaurantTable
    {
        public int TableID { get; set; }
        public int TableNumber { get; set; }   // Human-friendly number shown on floor plan
        public int Capacity { get; set; }       // Maximum guests allowed
        public string Status { get; set; }
    }

    /// <summary>
    /// A customer order linked to a table and a waiter.
    /// Status lifecycle: Open → Paid  (or Open → Cancelled)
    /// TotalAmount is the sum of all OrderItem subtotals.
    /// </summary>
    public class Order
    {
        public int OrderID { get; set; }
        public int TableID { get; set; }
        public int TableNumber { get; set; }    // For display — avoids extra JOIN in UI
        public int UserID { get; set; }
        public string WaiterName { get; set; }  // Populated via JOIN for display
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        public List<OrderItem> Items { get; set; } = new();  // Child items (lazy-loaded)
    }

    /// <summary>
    /// A single line item within an Order.
    /// SubTotal = Quantity * UnitPrice (stored for performance, avoids recalculation).
    /// </summary>
    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ItemID { get; set; }
        public string ItemName { get; set; }    // Populated via JOIN for display
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }  // Price at time of order (may differ from current menu price)
        public decimal SubTotal { get; set; }
    }

    /// <summary>
    /// Records a completed payment for an order.
    /// One order has at most one payment (enforced by UNIQUE constraint on OrderID).
    /// </summary>
    public class Payment
    {
        public int PaymentID { get; set; }
        public int OrderID { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }  // Cash, Credit Card, Debit Card, Mobile Pay
        public DateTime PaymentDate { get; set; }
    }

    /// <summary>
    /// Tracks stock levels for kitchen ingredients.
    /// IsLowStock is a computed property — no database column needed.
    /// </summary>
    public class InventoryItem
    {
        public int InventoryID { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }       // e.g., kg, liters, unit
        public decimal MinStock { get; set; }   // Alert threshold
        public DateTime LastUpdated { get; set; }

        /// <summary>Returns true when stock has fallen to or below the minimum threshold.</summary>
        public bool IsLowStock => Quantity <= MinStock;
    }

    /// <summary>
    /// Static session class holds the currently logged-in user for the entire application lifetime.
    /// Cleared on logout. Used for role checks and audit logging throughout all forms.
    /// </summary>
    public static class Session
    {
        public static User CurrentUser { get; set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin    => CurrentUser?.Role == "Admin";
        public static bool IsCashier  => CurrentUser?.Role == "Cashier" || IsAdmin;
    }
}
