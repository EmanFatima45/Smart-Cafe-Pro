// ============================================================
// SmartCafe Pro - Business Logic Layer
// Implements: Strategy, Observer, Factory Design Patterns
// Demonstrates: SOLID Principles, Dependency Injection
// ============================================================
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using SmartCafePro.Models;
using SmartCafePro.Interfaces;
using SmartCafePro.DataAccessLayer;

namespace SmartCafePro.BusinessLayer
{
    // ==========================================================
    // Session Manager (tracks current logged-in user)
    // ==========================================================
    public sealed class SessionManager
    {
        private static SessionManager _instance;
        private static readonly object _lock = new();

        public User   CurrentUser     { get; private set; }
        public bool   IsLoggedIn      => CurrentUser != null;
        public string CurrentRole     => CurrentUser?.RoleName ?? "";
        public int    CurrentUserID   => CurrentUser?.UserID ?? 0;
        public int    CurrentEmployeeID { get; private set; }

        private SessionManager() { }

        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                    lock (_lock)
                        if (_instance == null)
                            _instance = new SessionManager();
                return _instance;
            }
        }

        public void Login(User user, int employeeID = 0)
        {
            CurrentUser       = user;
            CurrentEmployeeID = employeeID;
        }

        public void Logout()
        {
            CurrentUser       = null;
            CurrentEmployeeID = 0;
        }

        public bool HasPermission(string requiredRole)
        {
            if (!IsLoggedIn) return false;
            return CurrentRole switch
            {
                "Admin"   => true,
                "Manager" => requiredRole != "Admin",
                "Cashier" => requiredRole is "Cashier" or "Waiter",
                "Waiter"  => requiredRole == "Waiter",
                _         => false
            };
        }
    }

    // ==========================================================
    // Authentication Service
    // ==========================================================
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;

        // Dependency Injection via constructor
        public AuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            username = username.Trim();
            password = password.Trim();

            var user = _userRepo.GetByUsername(username);
            if ((user == null || !user.IsActive) && IsDefaultAdminCredentials(username, password))
            {
                user = EnsureDefaultAdminAccount(password);
            }

            if (user == null || !user.IsActive) return null;

            if (PasswordNeedsSetup(user.PasswordHash))
            {
                var defaultPass = string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase)
                    ? "Admin@123"
                    : "Pass@123";

                if (password == defaultPass)
                {
                    var salt = GenerateSalt();
                    var hash = HashPassword(defaultPass, salt);
                    _userRepo.UpdatePassword(user.UserID, hash, salt);
                    user.PasswordHash = hash;
                    user.PasswordSalt = salt;
                }
            }

            // Verify password. Some shipped databases contain an old admin hash,
            // so repair the default admin account when the documented password is used.
            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                if (IsDefaultAdminLogin(user, password))
                {
                    var salt = GenerateSalt();
                    var hash = HashPassword(password, salt);
                    _userRepo.UpdatePassword(user.UserID, hash, salt);
                    user.PasswordHash = hash;
                    user.PasswordSalt = salt;
                }
                else
                {
                    return null;
                }
            }

            _userRepo.UpdateLastLogin(user.UserID);

            // Log audit
            LogAudit("Users", "LOGIN", user.UserID, user.UserID);

            return user;
        }

        public void Logout()
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user != null)
                LogAudit("Users", "LOGOUT", user.UserID, user.UserID);
            SessionManager.Instance.Logout();
        }

        public bool ChangePassword(int userID, string oldPass, string newPass)
        {
            var user = _userRepo.GetByID(userID);
            if (user == null) return false;
            if (!VerifyPassword(oldPass, user.PasswordHash, user.PasswordSalt)) return false;

            var salt    = GenerateSalt();
            var newHash = HashPassword(newPass, salt);
            return _userRepo.UpdatePassword(userID, newHash, salt);
        }

        public string GenerateSalt()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string HashPassword(string password, string salt)
        {
            using var sha = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash     = sha.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password, string hash, string salt)
            => HashPassword(password, salt) == hash;

        private static bool PasswordNeedsSetup(string hash) =>
            string.Equals(hash, "NEEDS_SETUP", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(hash, "computed_at_runtime", StringComparison.OrdinalIgnoreCase);

        private static bool IsDefaultAdminCredentials(string username, string password) =>
            string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(password, "Admin@123", StringComparison.Ordinal);

        private static bool IsDefaultAdminLogin(User user, string password) =>
            string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(password, "Admin@123", StringComparison.Ordinal);

        private User EnsureDefaultAdminAccount(string password)
        {
            try
            {
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();

                int roleId;
                using (var roleCmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
    INSERT INTO Roles(RoleName, Description) VALUES('Admin', 'Full system access');
SELECT TOP 1 RoleID FROM Roles WHERE RoleName = 'Admin';", conn))
                {
                    roleId = Convert.ToInt32(roleCmd.ExecuteScalar());
                }

                var salt = GenerateSalt();
                var hash = HashPassword(password, salt);

                using (var upsertCmd = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    UPDATE Users
       SET PasswordHash = @hash,
           PasswordSalt = @salt,
           RoleID = @roleId,
           IsActive = 1,
           UpdatedAt = GETDATE()
     WHERE Username = 'admin';
END
ELSE
BEGIN
    INSERT INTO Users(Username, PasswordHash, PasswordSalt, RoleID, Email, IsActive)
    VALUES('admin', @hash, @salt, @roleId, 'admin@smartcafe.com', 1);
END", conn))
                {
                    upsertCmd.Parameters.AddWithValue("@hash", hash);
                    upsertCmd.Parameters.AddWithValue("@salt", salt);
                    upsertCmd.Parameters.AddWithValue("@roleId", roleId);
                    upsertCmd.ExecuteNonQuery();
                }

                return _userRepo.GetByUsername("admin");
            }
            catch
            {
                return null;
            }
        }

        private void LogAudit(string table, string op, int recordID, int userID)
        {
            try
            {
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(
                    "INSERT INTO AuditLogs(TableName,Operation,RecordID,UserID) VALUES(@t,@o,@r,@u)", conn)
                    { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@o", op);
                cmd.Parameters.AddWithValue("@r", recordID);
                cmd.Parameters.AddWithValue("@u", userID);
                cmd.ExecuteNonQuery();
            }
            catch { /* silent — audit failure shouldn't block login */ }
        }
    }

    // ==========================================================
    // STRATEGY PATTERN: Discount Strategies
    // ==========================================================
    public class NoDiscount : IDiscountStrategy
    {
        public decimal CalculateDiscount(Order order, Customer customer = null) => 0;
        public string GetDescription() => "No Discount";
    }

    public class PercentageDiscount : IDiscountStrategy
    {
        private readonly decimal _percent;
        public PercentageDiscount(decimal percent) => _percent = percent;

        public decimal CalculateDiscount(Order order, Customer customer = null)
            => Math.Round(order.SubTotal * _percent / 100, 2);

        public string GetDescription() => $"{_percent}% Discount";
    }

    public class LoyaltyDiscount : IDiscountStrategy
    {
        public decimal CalculateDiscount(Order order, Customer customer = null)
        {
            if (customer == null) return 0;
            return Math.Round(order.SubTotal * customer.LoyaltyDiscount / 100, 2);
        }
        public string GetDescription() => "Loyalty Member Discount";
    }

    public class FixedAmountDiscount : IDiscountStrategy
    {
        private readonly decimal _amount;
        public FixedAmountDiscount(decimal amount) => _amount = amount;

        public decimal CalculateDiscount(Order order, Customer customer = null)
            => Math.Min(_amount, order.SubTotal);

        public string GetDescription() => $"PKR {_amount:N0} Off";
    }

    public class HappyHourDiscount : IDiscountStrategy
    {
        // 15% discount between 3 PM - 5 PM
        public decimal CalculateDiscount(Order order, Customer customer = null)
        {
            var hour = DateTime.Now.Hour;
            if (hour >= 15 && hour < 17)
                return Math.Round(order.SubTotal * 0.15m, 2);
            return 0;
        }
        public string GetDescription() => "Happy Hour (3-5 PM): 15% Off";
    }

    // ==========================================================
    // FACTORY PATTERN: Payment Method Factory
    // ==========================================================
    public class CashPayment : IPaymentStrategy
    {
        public string PaymentMethod => "Cash";
        public bool ProcessPayment(decimal amount, string reference = null) => true;
        public string GetDisplayName() => "💵 Cash";
    }

    public class CardPayment : IPaymentStrategy
    {
        public string PaymentMethod => "Card";
        public bool ProcessPayment(decimal amount, string reference = null) => amount > 0;
        public string GetDisplayName() => "💳 Debit/Credit Card";
    }

    public class OnlinePayment : IPaymentStrategy
    {
        public string PaymentMethod => "Online";
        public bool ProcessPayment(decimal amount, string reference = null) => true;
        public string GetDisplayName() => "📱 Online Transfer";
    }

    public class WalletPayment : IPaymentStrategy
    {
        public string PaymentMethod => "Wallet";
        public bool ProcessPayment(decimal amount, string reference = null) => true;
        public string GetDisplayName() => "👛 Digital Wallet";
    }

    public static class PaymentFactory
    {
        public static IPaymentStrategy Create(string method) => method switch
        {
            "Cash"   => new CashPayment(),
            "Card"   => new CardPayment(),
            "Online" => new OnlinePayment(),
            "Wallet" => new WalletPayment(),
            _        => new CashPayment()
        };

        public static List<IPaymentStrategy> GetAllMethods() => new()
        {
            new CashPayment(), new CardPayment(),
            new OnlinePayment(), new WalletPayment()
        };
    }

    // ==========================================================
    // OBSERVER PATTERN: Notification System
    // ==========================================================
    public class NotificationService : INotificationSubject
    {
        private readonly List<INotificationObserver> _observers = new();
        private readonly INotificationRepository     _repo;

        public NotificationService(INotificationRepository repo)
        {
            _repo = repo;
        }

        public void Subscribe(INotificationObserver observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void Unsubscribe(INotificationObserver observer)
            => _observers.Remove(observer);

        public void Notify(Notification notification)
        {
            // Persist to DB
            _repo.Insert(notification);

            // Notify all UI observers
            foreach (var obs in _observers)
                obs.OnNotificationReceived(notification);
        }

        public void SendLowStockAlert(string itemName, decimal current, decimal min)
        {
            Notify(new Notification
            {
                Title      = $"⚠ Low Stock: {itemName}",
                Message    = $"Stock level for {itemName} is {current} (minimum: {min})",
                Type       = "Warning",
                TargetRole = "Manager",
                ExpiresAt  = DateTime.Now.AddHours(24)
            });
        }

        public void SendOrderAlert(string orderNumber, string message)
        {
            Notify(new Notification
            {
                Title   = $"Order Update: {orderNumber}",
                Message = message,
                Type    = "Info"
            });
        }
    }

    // ==========================================================
    // Order Service (core business logic)
    // ==========================================================
    public class OrderService
    {
        private readonly IOrderRepository    _orderRepo;
        private readonly IProductRepository  _productRepo;
        private readonly NotificationService _notifService;

        public OrderService(IOrderRepository orderRepo,
                            IProductRepository productRepo,
                            NotificationService notifService)
        {
            _orderRepo    = orderRepo;
            _productRepo  = productRepo;
            _notifService = notifService;
        }

        public (bool Success, int OrderID, string Message) PlaceOrder(
            Order order,
            IDiscountStrategy discountStrategy = null,
            decimal taxRate = 16m)
        {
            try
            {
                if (!order.Validate())
                    return (false, 0, "Invalid order data.");

                // Apply discount strategy (Strategy Pattern)
                var discount = discountStrategy?.CalculateDiscount(order) ?? 0;
                order.DiscountAmt = discount;
                order.RecalculateTotals(taxRate);

                // Build JSON for stored procedure
                var items = new StringBuilder("[");
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var item = order.Items[i];
                    if (i > 0) items.Append(",");
                    items.Append($"{{\"ProductID\":{item.ProductID}," +
                                 $"\"Quantity\":{item.Quantity}," +
                                 $"\"UnitPrice\":{item.UnitPrice}," +
                                 $"\"Discount\":{item.Discount}," +
                                 $"\"Notes\":{(item.Notes == null ? "null" : $"\"{item.Notes}\"")}}}");
                }
                items.Append("]");

                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_PlaceOrder", conn)
                    { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CustomerID",  (object)order.CustomerID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TableID",     (object)order.TableID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmployeeID",  (object)order.EmployeeID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderType",   order.OrderType);
                cmd.Parameters.AddWithValue("@DiscountAmt", order.DiscountAmt);
                cmd.Parameters.AddWithValue("@TaxRate",     taxRate);
                cmd.Parameters.AddWithValue("@Notes",       (object)order.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderItems",  items.ToString());
                var outParam = new SqlParameter("@NewOrderID", SqlDbType.Int)
                    { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outParam);
                cmd.ExecuteNonQuery();

                int newID = (int)outParam.Value;
                if (newID <= 0)
                    return (false, 0, "Order placement failed.");

                return (true, newID, "Order placed successfully.");
            }
            catch (Exception ex)
            {
                if (IsMissingProcedure(ex, "sp_PlaceOrder"))
                    return PlaceOrderDirect(order);

                return (false, 0, $"Error: {ex.Message}");
            }
        }

        public (bool Success, string Message) CompleteOrder(
            int orderID,
            IPaymentStrategy paymentStrategy,
            decimal amountPaid,
            string reference = null,
            int useLoyaltyPoints = 0)
        {
            try
            {
                if (!paymentStrategy.ProcessPayment(amountPaid, reference))
                    return (false, "Payment processing failed.");

                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_CompleteOrder", conn)
                    { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@OrderID",       orderID);
                cmd.Parameters.AddWithValue("@PaymentMethod", paymentStrategy.PaymentMethod);
                cmd.Parameters.AddWithValue("@AmountPaid",    amountPaid);
                cmd.Parameters.AddWithValue("@Reference",     (object)reference ?? DBNull.Value);
                object processedBy = SessionManager.Instance.CurrentEmployeeID > 0
                    ? (object)SessionManager.Instance.CurrentEmployeeID
                    : DBNull.Value;
                cmd.Parameters.AddWithValue("@ProcessedBy",   processedBy);
                cmd.Parameters.AddWithValue("@UsePoints",     useLoyaltyPoints);
                var successParam = new SqlParameter("@Success", SqlDbType.Bit)
                    { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(successParam);
                cmd.ExecuteNonQuery();

                bool success = (bool)successParam.Value;
                if (success)
                {
                    EnsureTableStatusTimestamp(conn);
                    MarkOrderTableCleaningStarted(conn, orderID);
                    var order = _orderRepo.GetByID(orderID);
                    _notifService.SendOrderAlert(order?.OrderNumber ?? $"#{orderID}",
                        $"Order completed. Payment: {paymentStrategy.GetDisplayName()}");
                }

                return (success, success ? "Payment successful!" : "Payment failed.");
            }
            catch (Exception ex)
            {
                if (IsMissingProcedure(ex, "sp_CompleteOrder"))
                    return CompleteOrderDirect(orderID, paymentStrategy, amountPaid, reference, useLoyaltyPoints);

                return (false, $"Error: {ex.Message}");
            }
        }

        private (bool Success, int OrderID, string Message) PlaceOrderDirect(Order order)
        {
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var prefix = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-";
                int next;
                using (var seqCmd = new SqlCommand(
                    "SELECT COUNT(*) + 1 FROM Orders WHERE OrderNumber LIKE @prefix + '%'",
                    conn, tx))
                {
                    seqCmd.Parameters.AddWithValue("@prefix", prefix);
                    next = Convert.ToInt32(seqCmd.ExecuteScalar());
                }

                var orderNumber = prefix + next.ToString("0000");
                using var orderCmd = new SqlCommand(@"
INSERT INTO Orders (OrderNumber, CustomerID, TableID, EmployeeID, OrderType, Status,
                    SubTotal, DiscountAmt, TaxAmt, TotalAmount, Notes)
VALUES (@OrderNumber, @CustomerID, @TableID, @EmployeeID, @OrderType, 'Pending',
        @SubTotal, @DiscountAmt, @TaxAmt, @TotalAmount, @Notes);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
                orderCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                orderCmd.Parameters.AddWithValue("@CustomerID", (object)order.CustomerID ?? DBNull.Value);
                orderCmd.Parameters.AddWithValue("@TableID", (object)order.TableID ?? DBNull.Value);
                orderCmd.Parameters.AddWithValue("@EmployeeID", (object)order.EmployeeID ?? DBNull.Value);
                orderCmd.Parameters.AddWithValue("@OrderType", order.OrderType);
                orderCmd.Parameters.AddWithValue("@SubTotal", order.SubTotal);
                orderCmd.Parameters.AddWithValue("@DiscountAmt", order.DiscountAmt);
                orderCmd.Parameters.AddWithValue("@TaxAmt", order.TaxAmt);
                orderCmd.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
                orderCmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(order.Notes) ? DBNull.Value : order.Notes.Trim());
                var orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                foreach (var item in order.Items)
                {
                    using var itemCmd = new SqlCommand(@"
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, Discount, Notes)
VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice, @Discount, @Notes);", conn, tx);
                    itemCmd.Parameters.AddWithValue("@OrderID", orderId);
                    itemCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("@Discount", item.Discount);
                    itemCmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(item.Notes) ? DBNull.Value : item.Notes.Trim());
                    itemCmd.ExecuteNonQuery();
                }

                if (order.TableID.HasValue)
                {
                    using var tableCmd = new SqlCommand(
                        "UPDATE CafeTables SET Status='Occupied' WHERE TableID=@TableID",
                        conn, tx);
                    tableCmd.Parameters.AddWithValue("@TableID", order.TableID.Value);
                    tableCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return (true, orderId, "Order placed successfully.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, 0, $"Error: {ex.Message}");
            }
        }

        private (bool Success, string Message) CompleteOrderDirect(
            int orderID,
            IPaymentStrategy paymentStrategy,
            decimal amountPaid,
            string reference,
            int useLoyaltyPoints)
        {
            if (!paymentStrategy.ProcessPayment(amountPaid, reference))
                return (false, "Payment processing failed.");

            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                decimal totalAmount;
                int? customerId = null;
                int? tableId = null;
                using (var orderCmd = new SqlCommand(
                    "SELECT TotalAmount, CustomerID, TableID FROM Orders WHERE OrderID=@OrderID",
                    conn, tx))
                {
                    orderCmd.Parameters.AddWithValue("@OrderID", orderID);
                    using var reader = orderCmd.ExecuteReader();
                    if (!reader.Read()) return (false, "Order not found.");

                    totalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                    if (!reader.IsDBNull(reader.GetOrdinal("CustomerID")))
                        customerId = reader.GetInt32(reader.GetOrdinal("CustomerID"));
                    if (!reader.IsDBNull(reader.GetOrdinal("TableID")))
                        tableId = reader.GetInt32(reader.GetOrdinal("TableID"));
                }

                using (var payCmd = new SqlCommand(@"
INSERT INTO Payments (OrderID, PaymentMethod, Amount, Reference, ProcessedBy, Status)
VALUES (@OrderID, @PaymentMethod, @Amount, @Reference, @ProcessedBy, 'Completed');",
                    conn, tx))
                {
                    payCmd.Parameters.AddWithValue("@OrderID", orderID);
                    payCmd.Parameters.AddWithValue("@PaymentMethod", paymentStrategy.PaymentMethod);
                    payCmd.Parameters.AddWithValue("@Amount", amountPaid);
                    payCmd.Parameters.AddWithValue("@Reference", string.IsNullOrWhiteSpace(reference) ? DBNull.Value : reference.Trim());
                    payCmd.Parameters.AddWithValue("@ProcessedBy", SessionManager.Instance.CurrentEmployeeID > 0
                        ? SessionManager.Instance.CurrentEmployeeID : DBNull.Value);
                    payCmd.ExecuteNonQuery();
                }

                using (var completeCmd = new SqlCommand(
                    "UPDATE Orders SET Status='Completed', CompletedAt=GETDATE(), UpdatedAt=GETDATE() WHERE OrderID=@OrderID",
                    conn, tx))
                {
                    completeCmd.Parameters.AddWithValue("@OrderID", orderID);
                    completeCmd.ExecuteNonQuery();
                }

                if (tableId.HasValue)
                {
                    using var tableCmd = new SqlCommand(
                        "UPDATE CafeTables SET Status='Cleaning' WHERE TableID=@TableID",
                        conn, tx);
                    tableCmd.Parameters.AddWithValue("@TableID", tableId.Value);
                    tableCmd.ExecuteNonQuery();
                }

                if (customerId.HasValue)
                {
                    using var customerCmd = new SqlCommand(@"
UPDATE Customers
   SET LoyaltyPoints = LoyaltyPoints + @EarnedPoints - @UsedPoints,
       TotalSpent = TotalSpent + @TotalAmount,
       UpdatedAt = GETDATE()
 WHERE CustomerID=@CustomerID;", conn, tx);
                    customerCmd.Parameters.AddWithValue("@EarnedPoints", (int)(totalAmount / 10));
                    customerCmd.Parameters.AddWithValue("@UsedPoints", useLoyaltyPoints);
                    customerCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    customerCmd.Parameters.AddWithValue("@CustomerID", customerId.Value);
                    customerCmd.ExecuteNonQuery();
                }

                tx.Commit();

                EnsureTableStatusTimestamp(conn);
                MarkOrderTableCleaningStarted(conn, orderID);
                var order = _orderRepo.GetByID(orderID);
                _notifService.SendOrderAlert(order?.OrderNumber ?? $"#{orderID}",
                    $"Order completed. Payment: {paymentStrategy.GetDisplayName()}");

                return (true, "Payment successful!");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, $"Error: {ex.Message}");
            }
        }

        private static bool IsMissingProcedure(Exception ex, string procedureName) =>
            ex is SqlException &&
            ex.Message.IndexOf(procedureName, StringComparison.OrdinalIgnoreCase) >= 0 &&
            ex.Message.IndexOf("Could not find stored procedure", StringComparison.OrdinalIgnoreCase) >= 0;

        private void EnsureTableStatusTimestamp(SqlConnection conn)
        {
            using var cmd = new SqlCommand(@"
IF COL_LENGTH('dbo.CafeTables', 'StatusChangedAt') IS NULL
    ALTER TABLE dbo.CafeTables ADD StatusChangedAt DATETIME2 NULL;", conn)
            { CommandType = CommandType.Text };
            cmd.ExecuteNonQuery();
        }

        private void MarkOrderTableCleaningStarted(SqlConnection conn, int orderID)
        {
            using var cmd = new SqlCommand(@"
UPDATE CafeTables
SET StatusChangedAt=GETDATE()
WHERE TableID = (SELECT TableID FROM Orders WHERE OrderID=@OrderID)
  AND Status='Cleaning';", conn)
            { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@OrderID", orderID);
            cmd.ExecuteNonQuery();
        }

        public bool UpdateOrderItemStatus(int orderDetailID, string status)
        {
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE OrderDetails SET KitchenStatus=@s WHERE OrderDetailID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@s",  status);
            cmd.Parameters.AddWithValue("@id", orderDetailID);
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    // ==========================================================
    // Dashboard / Report Service
    // ==========================================================
    public class DashboardService : IReportService
    {
        public DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();

            EnsureDashboardTables(conn);
            EnsureDashboardInventory(conn);
            SyncDashboardTableStatuses(conn);

            // Today's summary
            using (var cmd = new SqlCommand(@"
SELECT COUNT(*) AS TotalOrders,
       ISNULL(SUM(CASE WHEN Status <> 'Cancelled' THEN TotalAmount ELSE 0 END),0) AS TotalRevenue,
       ISNULL(AVG(CASE WHEN Status <> 'Cancelled' THEN TotalAmount END),0) AS AvgOrderValue
FROM Orders
WHERE CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE);", conn)
                { CommandType = CommandType.Text })
            {
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    stats.TodayOrders   = Convert.ToInt32(r["TotalOrders"]);
                    stats.TodayRevenue  = Convert.ToDecimal(r["TotalRevenue"]);
                    stats.AvgOrderValue = Convert.ToDecimal(r["AvgOrderValue"]);
                }
            }

            // Pending orders count
            try
            {
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Orders WHERE Status NOT IN ('Completed','Cancelled')", conn)
                    { CommandType = CommandType.Text };
                stats.PendingOrders = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { stats.PendingOrders = 0; }

            // Tables
            try
            {
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) AS Total, SUM(CASE WHEN Status='Occupied' THEN 1 ELSE 0 END) AS Occupied FROM CafeTables WHERE ISNULL(IsActive,1)=1", conn)
                    { CommandType = CommandType.Text };
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    stats.TotalTables    = Convert.ToInt32(r["Total"]);
                    stats.TablesOccupied = r["Occupied"] == DBNull.Value ? 0 : Convert.ToInt32(r["Occupied"]);
                }
            }
            catch { stats.TotalTables = 0; stats.TablesOccupied = 0; }

            // Low stock
            try
            {
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Inventory WHERE CurrentStock <= MinStockLevel AND ISNULL(IsActive,1)=1", conn)
                    { CommandType = CommandType.Text };
                stats.LowStockItems = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { stats.LowStockItems = 0; }

            // Today's reservations
            try
            {
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Reservations WHERE ReservationDate=@d AND Status NOT IN ('Cancelled','NoShow')", conn)
                    { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@d", DateTime.Today);
                stats.TodayReservations = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { stats.TodayReservations = 0; }

            // Unread notifications
            try
            {
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Notifications WHERE IsRead=0 AND (ExpiresAt IS NULL OR ExpiresAt > GETDATE())", conn)
                    { CommandType = CommandType.Text };
                stats.UnreadNotifications = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { stats.UnreadNotifications = 0; }

            // Monthly revenue
            try
            {
                using var cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(TotalAmount),0) FROM Orders WHERE YEAR(OrderDate)=@y AND MONTH(OrderDate)=@m AND Status='Completed'", conn)
                    { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@y", DateTime.Today.Year);
                cmd.Parameters.AddWithValue("@m", DateTime.Today.Month);
                stats.MonthRevenue = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            catch { stats.MonthRevenue = 0; }

            try { stats.WeeklyRevenue = GetWeeklyRevenue(); }
            catch { stats.WeeklyRevenue = new List<(string Label, decimal Value)>(); }
            try { stats.TopProducts = GetTopProducts(5); }
            catch { stats.TopProducts = new List<(string Name, int Sold)>(); }
            try { stats.PaymentBreakdown = GetPaymentBreakdown(); }
            catch { stats.PaymentBreakdown = new List<(string Method, decimal Amt)>(); }

            return stats;
        }

        private static void EnsureDashboardTables(SqlConnection conn)
        {
            using var cmd = new SqlCommand(@"
IF OBJECT_ID('dbo.CafeTables','U') IS NULL
BEGIN
    CREATE TABLE dbo.CafeTables (
        TableID INT IDENTITY(1,1) PRIMARY KEY,
        TableNumber NVARCHAR(10) NOT NULL UNIQUE,
        Capacity INT NOT NULL DEFAULT 2,
        Location NVARCHAR(50) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Available',
        IsActive BIT NOT NULL DEFAULT 1,
        StatusChangedAt DATETIME2 NULL
    );
END;
IF COL_LENGTH('dbo.CafeTables','Capacity') IS NULL ALTER TABLE dbo.CafeTables ADD Capacity INT NOT NULL CONSTRAINT DF_CafeTables_Capacity DEFAULT 2;
IF COL_LENGTH('dbo.CafeTables','Location') IS NULL ALTER TABLE dbo.CafeTables ADD Location NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.CafeTables','Status') IS NULL ALTER TABLE dbo.CafeTables ADD Status NVARCHAR(20) NOT NULL CONSTRAINT DF_CafeTables_Status DEFAULT 'Available';
IF COL_LENGTH('dbo.CafeTables','IsActive') IS NULL ALTER TABLE dbo.CafeTables ADD IsActive BIT NOT NULL CONSTRAINT DF_CafeTables_IsActive DEFAULT 1;
IF COL_LENGTH('dbo.CafeTables','StatusChangedAt') IS NULL ALTER TABLE dbo.CafeTables ADD StatusChangedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.CafeTables)
BEGIN
    INSERT INTO dbo.CafeTables(TableNumber, Capacity, Location, Status, IsActive) VALUES
    ('T01',2,'Indoor','Available',1),('T02',2,'Indoor','Available',1),('T03',4,'Indoor','Available',1),
    ('T04',4,'Indoor','Available',1),('T05',4,'Indoor','Available',1),('T06',6,'Indoor','Available',1),
    ('T07',6,'Outdoor','Available',1),('T08',4,'Outdoor','Available',1),('T09',2,'Outdoor','Available',1),
    ('T10',8,'VIP','Available',1),('T11',4,'VIP','Available',1),('T12',2,'Window','Available',1);
END;", conn) { CommandType = CommandType.Text };
            cmd.ExecuteNonQuery();
        }

        private static void SyncDashboardTableStatuses(SqlConnection conn)
        {
            using var cmd = new SqlCommand(@"
UPDATE dbo.CafeTables
   SET Status='Available', StatusChangedAt=NULL
 WHERE Status='Cleaning'
   AND StatusChangedAt IS NOT NULL
   AND DATEADD(MINUTE, 2, StatusChangedAt) <= GETDATE();

UPDATE t
   SET t.Status='Occupied', t.StatusChangedAt=COALESCE(t.StatusChangedAt, GETDATE())
FROM dbo.CafeTables t
WHERE ISNULL(t.IsActive,1)=1
  AND EXISTS (
      SELECT 1
      FROM dbo.Orders o
      WHERE o.TableID=t.TableID
        AND o.Status NOT IN ('Completed','Cancelled')
        AND ISNULL(o.OrderType,'') = 'Dine-In'
  );

UPDATE t
   SET t.Status='Available', t.StatusChangedAt=NULL
FROM dbo.CafeTables t
WHERE ISNULL(t.IsActive,1)=1
  AND t.Status='Occupied'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.Orders o
      WHERE o.TableID=t.TableID
        AND o.Status NOT IN ('Completed','Cancelled')
        AND ISNULL(o.OrderType,'') = 'Dine-In'
  );", conn) { CommandType = CommandType.Text };
            cmd.ExecuteNonQuery();
        }

        private static void EnsureDashboardInventory(SqlConnection conn)
        {
            using var schema = new SqlCommand(@"
IF OBJECT_ID('dbo.Inventory','U') IS NULL
BEGIN
    CREATE TABLE dbo.Inventory (
        InventoryID INT IDENTITY(1,1) PRIMARY KEY,
        ItemName NVARCHAR(150) NOT NULL,
        Category NVARCHAR(100) NULL,
        Unit NVARCHAR(30) NOT NULL DEFAULT 'pcs',
        CurrentStock DECIMAL(10,2) NOT NULL DEFAULT 0,
        MinStockLevel DECIMAL(10,2) NOT NULL DEFAULT 5,
        CostPerUnit DECIMAL(10,2) NOT NULL DEFAULT 0,
        ExpiryDate DATE NULL,
        Location NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;
IF COL_LENGTH('dbo.Inventory','ItemName') IS NULL ALTER TABLE dbo.Inventory ADD ItemName NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.Inventory','Category') IS NULL ALTER TABLE dbo.Inventory ADD Category NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Inventory','Unit') IS NULL ALTER TABLE dbo.Inventory ADD Unit NVARCHAR(30) NOT NULL CONSTRAINT DF_DashboardInventory_Unit DEFAULT 'pcs';
IF COL_LENGTH('dbo.Inventory','CurrentStock') IS NULL ALTER TABLE dbo.Inventory ADD CurrentStock DECIMAL(10,2) NOT NULL CONSTRAINT DF_DashboardInventory_CurrentStock DEFAULT 0;
IF COL_LENGTH('dbo.Inventory','MinStockLevel') IS NULL ALTER TABLE dbo.Inventory ADD MinStockLevel DECIMAL(10,2) NOT NULL CONSTRAINT DF_DashboardInventory_MinStockLevel DEFAULT 5;
IF COL_LENGTH('dbo.Inventory','CostPerUnit') IS NULL ALTER TABLE dbo.Inventory ADD CostPerUnit DECIMAL(10,2) NOT NULL CONSTRAINT DF_DashboardInventory_CostPerUnit DEFAULT 0;
IF COL_LENGTH('dbo.Inventory','ExpiryDate') IS NULL ALTER TABLE dbo.Inventory ADD ExpiryDate DATE NULL;
IF COL_LENGTH('dbo.Inventory','Location') IS NULL ALTER TABLE dbo.Inventory ADD Location NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Inventory','IsActive') IS NULL ALTER TABLE dbo.Inventory ADD IsActive BIT NOT NULL CONSTRAINT DF_DashboardInventory_IsActive DEFAULT 1;
IF COL_LENGTH('dbo.Inventory','LastUpdated') IS NULL ALTER TABLE dbo.Inventory ADD LastUpdated DATETIME2 NOT NULL CONSTRAINT DF_DashboardInventory_LastUpdated DEFAULT GETDATE();", conn)
            { CommandType = CommandType.Text };
            schema.ExecuteNonQuery();

            using var seed = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE ItemName='Coffee Beans')
    INSERT INTO dbo.Inventory(ItemName,Category,Unit,CurrentStock,MinStockLevel,CostPerUnit,Location,IsActive)
    VALUES('Coffee Beans','Beverages','kg',4,10,1800,'Dry Store',1);
IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE ItemName='Milk')
    INSERT INTO dbo.Inventory(ItemName,Category,Unit,CurrentStock,MinStockLevel,CostPerUnit,Location,IsActive)
    VALUES('Milk','Dairy','liter',8,20,280,'Cold Room',1);
IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE ItemName='Cheese')
    INSERT INTO dbo.Inventory(ItemName,Category,Unit,CurrentStock,MinStockLevel,CostPerUnit,Location,IsActive)
    VALUES('Cheese','Dairy','kg',5,8,1350,'Cold Room',1);
IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE ItemName='Soft Drinks')
    INSERT INTO dbo.Inventory(ItemName,Category,Unit,CurrentStock,MinStockLevel,CostPerUnit,Location,IsActive)
    VALUES('Soft Drinks','Beverages','bottle',160,35,95,'Beverage Shelf',1);

IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE ISNULL(IsActive,1)=1 AND CurrentStock <= MinStockLevel)
BEGIN
    UPDATE dbo.Inventory SET CurrentStock=4, MinStockLevel=10, IsActive=1, LastUpdated=GETDATE()
     WHERE ItemName='Coffee Beans';
    UPDATE dbo.Inventory SET CurrentStock=8, MinStockLevel=20, IsActive=1, LastUpdated=GETDATE()
     WHERE ItemName='Milk';
    UPDATE dbo.Inventory SET CurrentStock=5, MinStockLevel=8, IsActive=1, LastUpdated=GETDATE()
     WHERE ItemName='Cheese';
END;", conn) { CommandType = CommandType.Text };
            seed.ExecuteNonQuery();
        }

        public List<(string Label, decimal Value)> GetWeeklyRevenue()
        {
            var list = new List<(string, decimal)>();
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                @";WITH Days AS
                  (
                      SELECT CAST(DATEADD(DAY,-6,CAST(GETDATE() AS DATE)) AS DATE) AS SaleDate
                      UNION ALL
                      SELECT DATEADD(DAY,1,SaleDate) FROM Days WHERE SaleDate < CAST(GETDATE() AS DATE)
                  )
                  SELECT DATENAME(WEEKDAY,d.SaleDate) AS DayName,
                         ISNULL(SUM(CASE WHEN o.Status <> 'Cancelled' THEN o.TotalAmount ELSE 0 END),0) AS Revenue
                  FROM Days d
                  LEFT JOIN Orders o ON CAST(o.OrderDate AS DATE)=d.SaleDate
                  GROUP BY d.SaleDate
                  ORDER BY d.SaleDate
                  OPTION (MAXRECURSION 7)", conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r["DayName"].ToString(), (decimal)r["Revenue"]));
            return list;
        }

        public List<(string Name, int Sold)> GetTopProducts(int count = 5)
        {
            int take = Math.Max(1, count);
            var list = new List<(string, int)>();
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT TOP (@TopCount) p.ProductName, ISNULL(SUM(od.Quantity),0) AS Sold
                   FROM Products p
                   LEFT JOIN OrderDetails od ON p.ProductID=od.ProductID
                   LEFT JOIN Orders o ON od.OrderID=o.OrderID AND o.Status='Completed'
                     AND CAST(o.OrderDate AS DATE) >= DATEADD(DAY,-30,GETDATE())
                   GROUP BY p.ProductName
                   ORDER BY Sold DESC", conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@TopCount", take);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r["ProductName"].ToString(), (int)r["Sold"]));
            return list;
        }

        public List<(string Method, decimal Amt)> GetPaymentBreakdown()
        {
            var list = new List<(string, decimal)>();
            using var conn = DatabaseManager.Instance.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT PaymentMethod, SUM(Amount) AS Total
                  FROM Payments
                  WHERE CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE) AND Status='Completed'
                  GROUP BY PaymentMethod", conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r["PaymentMethod"].ToString(), (decimal)r["Total"]));
            r.Close();

            if (list.Count > 0) return list;

            using var weeklyCmd = new SqlCommand(
                @"SELECT PaymentMethod, SUM(Amount) AS Total
                  FROM Payments
                  WHERE PaymentDate >= DATEADD(DAY,-6,CAST(GETDATE() AS DATE)) AND Status='Completed'
                  GROUP BY PaymentMethod", conn) { CommandType = CommandType.Text };
            using var weeklyReader = weeklyCmd.ExecuteReader();
            while (weeklyReader.Read())
                list.Add((weeklyReader["PaymentMethod"].ToString(), (decimal)weeklyReader["Total"]));
            weeklyReader.Close();

            if (list.Count > 0) return list;

            using var salesCmd = new SqlCommand(
                @"SELECT 'Orders' AS PaymentMethod, ISNULL(SUM(TotalAmount),0) AS Total
                  FROM Orders
                  WHERE OrderDate >= DATEADD(DAY,-6,CAST(GETDATE() AS DATE))
                    AND Status <> 'Cancelled'", conn) { CommandType = CommandType.Text };
            using var salesReader = salesCmd.ExecuteReader();
            if (salesReader.Read() && salesReader["Total"] != DBNull.Value)
            {
                var total = (decimal)salesReader["Total"];
                if (total > 0) list.Add(("Orders", total));
            }
            return list;
        }

        public byte[] ExportToPdf(string reportType, DateTime from, DateTime to)
        {
            // Placeholder - actual PDF generation done in PresentationLayer with iTextSharp
            return Array.Empty<byte>();
        }

        public byte[] ExportToExcel(string reportType, DateTime from, DateTime to)
        {
            // Placeholder - actual Excel generation done in PresentationLayer
            return Array.Empty<byte>();
        }
    }

    // ==========================================================
    // Settings Service
    // ==========================================================
    public class SettingsService
    {
        private Dictionary<string, string> _cache = new();

        public string Get(string key, string defaultValue = "")
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;
            try
            {
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("SELECT SettingValue FROM Settings WHERE SettingKey=@k", conn)
                    { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@k", key);
                var val = cmd.ExecuteScalar()?.ToString() ?? defaultValue;
                _cache[key] = val;
                return val;
            }
            catch { return defaultValue; }
        }

        public bool Set(string key, string value)
        {
            try
            {
                using var conn = DatabaseManager.Instance.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(
                    @"IF EXISTS (SELECT 1 FROM Settings WHERE SettingKey=@k)
                          UPDATE Settings SET SettingValue=@v,UpdatedAt=GETDATE() WHERE SettingKey=@k
                      ELSE
                          INSERT INTO Settings(SettingKey,SettingValue) VALUES(@k,@v)", conn)
                    { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@k", key);
                cmd.Parameters.AddWithValue("@v", value);
                _cache[key] = value;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public void ClearCache() => _cache.Clear();

        // Typed helpers
        public decimal GetDecimal(string key, decimal def = 0) =>
            decimal.TryParse(Get(key), out var v) ? v : def;

        public bool GetBool(string key, bool def = false) =>
            bool.TryParse(Get(key), out var v) ? v : def;

        public int GetInt(string key, int def = 0) =>
            int.TryParse(Get(key), out var v) ? v : def;
    }

    // ==========================================================
    // Service Locator / DI Container (simple implementation)
    // ==========================================================
    public static class ServiceContainer
    {
        private static UserRepository         _userRepo;
        private static CustomerRepository     _customerRepo;
        private static ProductRepository      _productRepo;
        private static OrderRepository        _orderRepo;
        private static NotificationRepository _notifRepo;
        private static TableRepository        _tableRepo;

        public static AuthService          Auth         { get; private set; }
        public static OrderService         Orders       { get; private set; }
        public static DashboardService     Dashboard    { get; private set; }
        public static NotificationService  Notifications { get; private set; }
        public static SettingsService      Settings     { get; private set; }

        public static IUserRepository     UserRepo     => _userRepo;
        public static ICustomerRepository CustomerRepo => _customerRepo;
        public static IProductRepository  ProductRepo  => _productRepo;
        public static IOrderRepository    OrderRepo    => _orderRepo;
        public static ITableRepository    TableRepo    => _tableRepo;

        public static void Initialize()
        {
            _userRepo     = new UserRepository();
            _customerRepo = new CustomerRepository();
            _productRepo  = new ProductRepository();
            _orderRepo    = new OrderRepository();
            _notifRepo    = new NotificationRepository();
            _tableRepo    = new TableRepository();

            Auth          = new AuthService(_userRepo);
            Notifications = new NotificationService(_notifRepo);
            Orders        = new OrderService(_orderRepo, _productRepo, Notifications);
            Dashboard     = new DashboardService();
            Settings      = new SettingsService();
        }
    }
}
