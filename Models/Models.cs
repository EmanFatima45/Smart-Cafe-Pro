// ============================================================
// SmartCafe Pro - Models
// File: Models/BaseModel.cs
// ============================================================
using System;
using System.Collections.Generic;

namespace SmartCafePro.Models
{
    /// <summary>
    /// Abstract base model demonstrating Abstraction and Encapsulation.
    /// All models inherit from this class.
    /// </summary>
    public abstract class BaseModel
    {
        public int    ID        { get; protected set; }
        public bool   IsActive  { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Abstract method - forces child classes to implement validation
        public abstract bool Validate();

        // Virtual method - can be overridden (Polymorphism)
        public virtual string GetDisplayName() => $"{GetType().Name} #{ID}";

        public override string ToString() => GetDisplayName();
    }
}


// ============================================================
// File: Models/Role.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Role : BaseModel
    {
        public int    RoleID      { get; set; }
        public string RoleName    { get; set; }
        public string Description { get; set; }
        public string Permissions { get; set; }

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(RoleName);

        public override string GetDisplayName() => RoleName;
    }
}


// ============================================================
// File: Models/User.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class User : BaseModel
    {
        public int    UserID       { get; set; }
        public string Username     { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public int    RoleID       { get; set; }
        public string RoleName     { get; set; }
        public string Email        { get; set; }
        public string RememberToken { get; set; }
        public DateTime? LastLogin  { get; set; }

        // Encapsulation: role-checking helper
        public bool IsAdmin    => RoleName == "Admin";
        public bool IsManager  => RoleName == "Manager" || IsAdmin;
        public bool IsCashier  => RoleName == "Cashier" || IsManager;

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(Username) && RoleID > 0;

        public override string GetDisplayName() => Username;
    }
}


// ============================================================
// File: Models/Employee.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Employee : BaseModel
    {
        public int     EmployeeID  { get; set; }
        public int?    UserID      { get; set; }
        public string  FirstName   { get; set; }
        public string  LastName    { get; set; }
        public string  CNIC        { get; set; }
        public string  Phone       { get; set; }
        public string  Email       { get; set; }
        public string  Address     { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime HireDate   { get; set; } = DateTime.Today;
        public string  Department  { get; set; }
        public string  Position    { get; set; }
        public decimal Salary      { get; set; }
        public string  PhotoPath   { get; set; }

        // Composition: computed full name
        public string FullName => $"{FirstName} {LastName}";

        // Polymorphism: Override GetDisplayName
        public override string GetDisplayName() => FullName;

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName)  &&
            Salary >= 0;
    }
}


// ============================================================
// File: Models/Customer.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Customer : BaseModel
    {
        public int     CustomerID      { get; set; }
        public string  FirstName       { get; set; }
        public string  LastName        { get; set; }
        public string  Phone           { get; set; }
        public string  Email           { get; set; }
        public string  Address         { get; set; }
        public DateTime? DateOfBirth   { get; set; }
        public string  MembershipLevel { get; set; } = "Bronze";
        public decimal TotalSpent      { get; set; }
        public int     LoyaltyPoints   { get; set; }
        public string  Notes           { get; set; }
        public DateTime JoinDate       { get; set; } = DateTime.Now;

        public string FullName => $"{FirstName} {LastName}";

        // Membership badge color (for UI binding)
        public string MembershipColor => MembershipLevel switch
        {
            "VIP"      => "#FFD700",
            "Platinum" => "#E5E4E2",
            "Gold"     => "#DAA520",
            "Silver"   => "#C0C0C0",
            _          => "#CD7F32"
        };

        public decimal LoyaltyDiscount => MembershipLevel switch
        {
            "VIP"      => 15m,
            "Platinum" => 10m,
            "Gold"     => 7m,
            "Silver"   => 5m,
            _          => 0m
        };

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName);

        public override string GetDisplayName() => FullName;
    }
}


// ============================================================
// File: Models/Product.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Product : BaseModel
    {
        public int     ProductID    { get; set; }
        public int     CategoryID   { get; set; }
        public string  CategoryName { get; set; }
        public string  ProductName  { get; set; }
        public string  Description  { get; set; }
        public decimal Price        { get; set; }
        public decimal CostPrice    { get; set; }
        public string  ImagePath    { get; set; }
        public bool    IsCombo      { get; set; }
        public bool    IsAvailable  { get; set; } = true;
        public int     PrepTimeMin  { get; set; } = 5;
        public int?    Calories     { get; set; }
        public string  Allergens    { get; set; }
        public string  Barcode      { get; set; }
        public int     DisplayOrder { get; set; }

        public decimal ProfitMargin => Price > 0 ? ((Price - CostPrice) / Price) * 100 : 0;

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(ProductName) &&
            Price >= 0 && CategoryID > 0;

        public override string GetDisplayName() => ProductName;
    }
}


// ============================================================
// File: Models/Order.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Order : BaseModel
    {
        public int      OrderID      { get; set; }
        public string   OrderNumber  { get; set; }
        public int?     CustomerID   { get; set; }
        public string   CustomerName { get; set; }
        public int?     TableID      { get; set; }
        public string   TableNumber  { get; set; }
        public int?     EmployeeID   { get; set; }
        public string   EmployeeName { get; set; }
        public string   OrderType    { get; set; } = "Dine-In";
        public string   Status       { get; set; } = "Pending";
        public decimal  SubTotal     { get; set; }
        public decimal  DiscountAmt  { get; set; }
        public decimal  TaxAmt       { get; set; }
        public decimal  TotalAmount  { get; set; }
        public string   Notes        { get; set; }
        public DateTime OrderDate    { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        // Aggregation: Order has a collection of OrderDetails
        public List<OrderDetail> Items { get; set; } = new();

        // Computed
        public int ItemCount => Items?.Count ?? 0;
        public bool IsPaid   => Status == "Completed";

        // Recalculate totals from items
        public void RecalculateTotals(decimal taxRate = 16m)
        {
            SubTotal    = 0;
            foreach (var item in Items)
                SubTotal += item.LineTotal;

            TaxAmt      = (SubTotal - DiscountAmt) * taxRate / 100;
            TotalAmount = SubTotal - DiscountAmt + TaxAmt;
        }

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(OrderType) && Items?.Count > 0;

        public override string GetDisplayName() => OrderNumber ?? $"Order #{OrderID}";
    }
}


// ============================================================
// File: Models/OrderDetail.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class OrderDetail : BaseModel
    {
        public int     OrderDetailID  { get; set; }
        public int     OrderID        { get; set; }
        public int     ProductID      { get; set; }
        public string  ProductName    { get; set; }
        public int     Quantity       { get; set; }
        public decimal UnitPrice      { get; set; }
        public decimal Discount       { get; set; }
        public decimal LineTotal      => Quantity * UnitPrice * (1 - Discount / 100);
        public string  KitchenStatus  { get; set; } = "Pending";
        public string  Notes          { get; set; }

        public override bool Validate() =>
            ProductID > 0 && Quantity > 0 && UnitPrice >= 0;
    }
}


// ============================================================
// File: Models/CafeTable.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class CafeTable : BaseModel
    {
        public int    TableID     { get; set; }
        public string TableNumber { get; set; }
        public int    Capacity    { get; set; } = 4;
        public string Location    { get; set; }
        public string Status      { get; set; } = "Available";
        public string QRCode      { get; set; }

        public bool IsAvailable  => Status == "Available";
        public bool IsOccupied   => Status == "Occupied";
        public bool IsReserved   => Status == "Reserved";

        // Status color for UI
        public string StatusColor => Status switch
        {
            "Available" => "#27AE60",
            "Reserved"  => "#F39C12",
            "Occupied"  => "#E74C3C",
            "Cleaning"  => "#3498DB",
            _           => "#95A5A6"
        };

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(TableNumber) && Capacity > 0;

        public override string GetDisplayName() => $"Table {TableNumber}";
    }
}


// ============================================================
// File: Models/Payment.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Payment : BaseModel
    {
        public int     PaymentID     { get; set; }
        public int     OrderID       { get; set; }
        public string  PaymentMethod { get; set; }
        public decimal Amount        { get; set; }
        public string  Reference     { get; set; }
        public string  Status        { get; set; } = "Completed";
        public int?    ProcessedBy   { get; set; }
        public DateTime PaymentDate  { get; set; } = DateTime.Now;

        public override bool Validate() =>
            OrderID > 0 && Amount > 0 && !string.IsNullOrWhiteSpace(PaymentMethod);
    }
}


// ============================================================
// File: Models/Reservation.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Reservation : BaseModel
    {
        public int      ReservationID   { get; set; }
        public int      CustomerID      { get; set; }
        public string   CustomerName    { get; set; }
        public string   CustomerPhone   { get; set; }
        public int      TableID         { get; set; }
        public string   TableNumber     { get; set; }
        public DateTime ReservationDate { get; set; }
        public TimeSpan StartTime       { get; set; }
        public TimeSpan EndTime         { get; set; }
        public int      GuestCount      { get; set; } = 1;
        public string   Status          { get; set; } = "Confirmed";
        public string   SpecialRequests { get; set; }
        public int?     CreatedBy       { get; set; }

        public string DisplayTime =>
            $"{ReservationDate:dd MMM} {StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

        public override bool Validate() =>
            CustomerID > 0 && TableID > 0 &&
            StartTime < EndTime && GuestCount > 0;
    }
}


// ============================================================
// File: Models/Inventory.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class InventoryItem : BaseModel
    {
        public int     InventoryID   { get; set; }
        public int?    SupplierID    { get; set; }
        public string  SupplierName  { get; set; }
        public string  ItemName      { get; set; }
        public string  Category      { get; set; }
        public string  Unit          { get; set; } = "pcs";
        public decimal CurrentStock  { get; set; }
        public decimal MinStockLevel { get; set; } = 5;
        public decimal CostPerUnit   { get; set; }
        public DateTime? ExpiryDate  { get; set; }
        public string  Location      { get; set; }
        public DateTime LastUpdated  { get; set; } = DateTime.Now;

        public bool IsLowStock       => CurrentStock <= MinStockLevel;
        public bool IsExpiringSoon   => ExpiryDate.HasValue &&
                                        ExpiryDate.Value <= DateTime.Today.AddDays(7);
        public string StockStatus    => IsLowStock ? "Low" :
                                       (CurrentStock <= MinStockLevel * 2 ? "Medium" : "Good");
        public string StockColor     => IsLowStock ? "#E74C3C" :
                                       (StockStatus == "Medium" ? "#F39C12" : "#27AE60");

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(ItemName) &&
            CurrentStock >= 0 && MinStockLevel >= 0;
    }
}


// ============================================================
// File: Models/Notification.cs
// ============================================================
namespace SmartCafePro.Models
{
    public class Notification : BaseModel
    {
        public int     NotificationID { get; set; }
        public string  Title          { get; set; }
        public string  Message        { get; set; }
        public string  Type           { get; set; } = "Info";
        public string  TargetRole     { get; set; }
        public bool    IsRead         { get; set; }
        public DateTime? ExpiresAt    { get; set; }

        public string TypeIcon => Type switch
        {
            "Warning" => "⚠",
            "Alert"   => "🔴",
            "Success" => "✓",
            "Error"   => "✗",
            _         => "ℹ"
        };

        public string TypeColor => Type switch
        {
            "Warning" => "#F39C12",
            "Alert"   => "#E74C3C",
            "Success" => "#27AE60",
            "Error"   => "#C0392B",
            _         => "#3498DB"
        };

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(Title) &&
            !string.IsNullOrWhiteSpace(Message);
    }
}


// ============================================================
// File: Models/DashboardStats.cs
// ============================================================
namespace SmartCafePro.Models
{
    /// <summary>
    /// DTO for dashboard data — not persisted, assembled in BLL
    /// </summary>
    public class DashboardStats
    {
        public decimal TodayRevenue       { get; set; }
        public int     TodayOrders        { get; set; }
        public int     PendingOrders      { get; set; }
        public int     TablesOccupied     { get; set; }
        public int     TotalTables        { get; set; }
        public int     LowStockItems      { get; set; }
        public int     TodayReservations  { get; set; }
        public int     UnreadNotifications { get; set; }
        public decimal MonthRevenue       { get; set; }
        public decimal AvgOrderValue      { get; set; }

        public List<(string Label, decimal Value)> WeeklyRevenue { get; set; } = new();
        public List<(string Name, int Sold)>       TopProducts   { get; set; } = new();
        public List<(string Method, decimal Amt)>  PaymentBreakdown { get; set; } = new();
    }
}
