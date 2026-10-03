// ============================================================
// SmartCafe Pro - Interfaces
// Demonstrates Interface Segregation Principle (SOLID)
// ============================================================
using System;
using System.Collections.Generic;
using SmartCafePro.Models;

namespace SmartCafePro.Interfaces
{
    // ----------------------------------------------------------
    // Generic Repository Interface (Open/Closed Principle)
    // ----------------------------------------------------------
    public interface IRepository<T> where T : BaseModel
    {
        T         GetByID(int id);
        List<T>   GetAll();
        List<T>   GetActive();
        bool      Insert(T entity);
        bool      Update(T entity);
        bool      Delete(int id);
    }

    // ----------------------------------------------------------
    // User / Auth
    // ----------------------------------------------------------
    public interface IUserRepository : IRepository<User>
    {
        User   GetByUsername(string username);
        bool   UpdatePassword(int userID, string newHash, string salt);
        bool   UpdateLastLogin(int userID);
        bool   UpdateRememberToken(int userID, string token);
    }

    // ----------------------------------------------------------
    // Customer
    // ----------------------------------------------------------
    public interface ICustomerRepository : IRepository<Customer>
    {
        List<Customer> Search(string keyword);
        Customer       GetByPhone(string phone);
        bool           UpdateLoyaltyPoints(int customerID, int points);
        List<Order>    GetOrderHistory(int customerID);
    }

    // ----------------------------------------------------------
    // Product / Menu
    // ----------------------------------------------------------
    public interface IProductRepository : IRepository<Product>
    {
        List<Product> GetByCategory(int categoryID);
        List<Product> Search(string keyword);
        List<Product> GetAvailable();
        Product       GetByBarcode(string barcode);
        bool          ToggleAvailability(int productID);
    }

    // ----------------------------------------------------------
    // Order
    // ----------------------------------------------------------
    public interface IOrderRepository : IRepository<Order>
    {
        Order          GetWithDetails(int orderID);
        List<Order>    GetByStatus(string status);
        List<Order>    GetByDate(DateTime date);
        List<Order>    GetByDateRange(DateTime start, DateTime end);
        List<Order>    GetByCustomer(int customerID);
        List<Order>    GetActiveOrders();
        bool           UpdateStatus(int orderID, string status);
        string         GenerateOrderNumber();
    }

    // ----------------------------------------------------------
    // Table
    // ----------------------------------------------------------
    public interface ITableRepository : IRepository<CafeTable>
    {
        List<CafeTable> GetByStatus(string status);
        bool            UpdateStatus(int tableID, string status);
    }

    // ----------------------------------------------------------
    // Inventory
    // ----------------------------------------------------------
    public interface IInventoryRepository : IRepository<InventoryItem>
    {
        List<InventoryItem> GetLowStock();
        List<InventoryItem> GetExpiringSoon(int daysAhead = 7);
        bool                AdjustStock(int id, decimal qty, string operation);
    }

    // ----------------------------------------------------------
    // Reservation
    // ----------------------------------------------------------
    public interface IReservationRepository : IRepository<Reservation>
    {
        List<Reservation> GetByDate(DateTime date);
        List<Reservation> GetActiveReservations();
        List<Reservation> GetByTable(int tableID, DateTime date);
        bool              UpdateStatus(int reservationID, string status);
    }

    // ----------------------------------------------------------
    // Employee
    // ----------------------------------------------------------
    public interface IEmployeeRepository : IRepository<Employee>
    {
        Employee       GetByUserID(int userID);
        List<Employee> GetByDepartment(string dept);
        bool           MarkAttendance(int employeeID, string status);
    }

    // ----------------------------------------------------------
    // Notification
    // ----------------------------------------------------------
    public interface INotificationRepository : IRepository<Notification>
    {
        List<Notification> GetUnread(string role = null);
        int                GetUnreadCount(string role = null);
        bool               MarkAsRead(int id);
        bool               MarkAllRead(string role = null);
    }

    // ----------------------------------------------------------
    // Reporting
    // ----------------------------------------------------------
    public interface IReportService
    {
        DashboardStats      GetDashboardStats();
        List<(string Label, decimal Value)> GetWeeklyRevenue();
        List<(string Name, int Sold)>    GetTopProducts(int count = 5);
        byte[]              ExportToPdf(string reportType, DateTime from, DateTime to);
        byte[]              ExportToExcel(string reportType, DateTime from, DateTime to);
    }

    // ----------------------------------------------------------
    // Authentication Service
    // ----------------------------------------------------------
    public interface IAuthService
    {
        User   Login(string username, string password);
        void   Logout();
        bool   ChangePassword(int userID, string oldPass, string newPass);
        string HashPassword(string password, string salt);
        bool   VerifyPassword(string password, string hash, string salt);
        string GenerateSalt();
    }

    // ----------------------------------------------------------
    // Payment Strategy (Strategy Pattern interface)
    // ----------------------------------------------------------
    public interface IPaymentStrategy
    {
        string PaymentMethod { get; }
        bool   ProcessPayment(decimal amount, string reference = null);
        string GetDisplayName();
    }

    // ----------------------------------------------------------
    // Discount Strategy (Strategy Pattern interface)
    // ----------------------------------------------------------
    public interface IDiscountStrategy
    {
        decimal CalculateDiscount(Order order, Customer customer = null);
        string  GetDescription();
    }

    // ----------------------------------------------------------
    // Notification Observer
    // ----------------------------------------------------------
    public interface INotificationObserver
    {
        void OnNotificationReceived(Notification notification);
    }

    public interface INotificationSubject
    {
        void Subscribe(INotificationObserver observer);
        void Unsubscribe(INotificationObserver observer);
        void Notify(Notification notification);
    }

    // ----------------------------------------------------------
    // Database connection
    // ----------------------------------------------------------
    public interface IDatabaseConnection
    {
        Microsoft.Data.SqlClient.SqlConnection GetConnection();
        bool TestConnection();
    }
}
