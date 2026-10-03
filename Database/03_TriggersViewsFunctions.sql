-- ============================================================
-- SmartCafe Pro - Triggers, Views, Functions & Seed Data
-- ============================================================
USE SmartCafeProDB;
GO

-- ============================================================
-- TRIGGER: Auto deduct stock after order (simplified)
-- ============================================================
CREATE OR ALTER TRIGGER trg_OrderCompleted_StockDeduct
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- When an order moves to 'Completed' trigger inventory notification
    IF UPDATE(Status)
    BEGIN
        INSERT INTO Notifications (Title, Message, Type, TargetRole)
        SELECT
            'Order Completed: ' + i.OrderNumber,
            'Order ' + i.OrderNumber + ' completed. Amount: ' +
            CAST(i.TotalAmount AS NVARCHAR) + ' PKR',
            'Success',
            'Manager'
        FROM inserted i
        WHERE i.Status = 'Completed';
    END
END
GO

-- ============================================================
-- TRIGGER: Audit log on Orders UPDATE/DELETE
-- ============================================================
CREATE OR ALTER TRIGGER trg_Orders_Audit
ON Orders
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO AuditLogs (TableName, Operation, RecordID, OldValues)
        SELECT
            'Orders',
            CASE WHEN EXISTS (SELECT 1 FROM inserted) THEN 'UPDATE' ELSE 'DELETE' END,
            d.OrderID,
            'Status=' + d.Status + ';Total=' + CAST(d.TotalAmount AS NVARCHAR)
        FROM deleted d;
    END
END
GO

-- ============================================================
-- TRIGGER: Low stock alert
-- ============================================================
CREATE OR ALTER TRIGGER trg_Inventory_LowStock
ON Inventory
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Notifications (Title, Message, Type, TargetRole)
    SELECT
        'Low Stock Alert: ' + i.ItemName,
        'Item "' + i.ItemName + '" stock is ' + CAST(i.CurrentStock AS NVARCHAR) +
        ' ' + i.Unit + ' (minimum: ' + CAST(i.MinStockLevel AS NVARCHAR) + ')',
        'Warning',
        'Manager'
    FROM inserted i
    WHERE i.CurrentStock <= i.MinStockLevel;
END
GO

-- ============================================================
-- TRIGGER: Auto-update membership level based on total spent
-- ============================================================
CREATE OR ALTER TRIGGER trg_Customer_MembershipUpdate
ON Customers
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(TotalSpent)
    BEGIN
        UPDATE c SET
            MembershipLevel = CASE
                WHEN i.TotalSpent >= 100000 THEN 'VIP'
                WHEN i.TotalSpent >= 50000  THEN 'Platinum'
                WHEN i.TotalSpent >= 20000  THEN 'Gold'
                WHEN i.TotalSpent >= 5000   THEN 'Silver'
                ELSE 'Bronze'
            END
        FROM Customers c
        INNER JOIN inserted i ON c.CustomerID = i.CustomerID;
    END
END
GO

-- ============================================================
-- TRIGGER: Reservation status auto-cleanup
-- ============================================================
CREATE OR ALTER TRIGGER trg_Reservation_Audit
ON Reservations
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AuditLogs (TableName, Operation, RecordID, OldValues, NewValues)
    SELECT
        'Reservations', 'UPDATE', d.ReservationID,
        'Status=' + d.Status,
        'Status=' + i.Status
    FROM deleted d
    INNER JOIN inserted i ON d.ReservationID = i.ReservationID
    WHERE d.Status <> i.Status;
END
GO

-- ============================================================
-- VIEW: vw_DailySalesSummary
-- ============================================================
CREATE OR ALTER VIEW vw_DailySalesSummary AS
SELECT
    CAST(o.OrderDate AS DATE) AS SaleDate,
    COUNT(*)                  AS TotalOrders,
    SUM(o.TotalAmount)        AS TotalRevenue,
    SUM(o.TaxAmt)             AS TotalTax,
    SUM(o.DiscountAmt)        AS TotalDiscounts,
    AVG(o.TotalAmount)        AS AvgOrderValue
FROM Orders o
WHERE o.Status = 'Completed'
GROUP BY CAST(o.OrderDate AS DATE);
GO

-- ============================================================
-- VIEW: vw_ProductPerformance
-- ============================================================
CREATE OR ALTER VIEW vw_ProductPerformance AS
SELECT
    p.ProductID,
    p.ProductName,
    c.CategoryName,
    SUM(od.Quantity)    AS TotalSold,
    SUM(od.LineTotal)   AS TotalRevenue,
    AVG(od.UnitPrice)   AS AvgPrice,
    COUNT(DISTINCT o.OrderID) AS OrderCount
FROM Products p
INNER JOIN Categories c     ON p.CategoryID  = c.CategoryID
LEFT  JOIN OrderDetails od  ON p.ProductID   = od.ProductID
LEFT  JOIN Orders o         ON od.OrderID    = o.OrderID AND o.Status = 'Completed'
GROUP BY p.ProductID, p.ProductName, c.CategoryName;
GO

-- ============================================================
-- VIEW: vw_InventoryStatus
-- ============================================================
CREATE OR ALTER VIEW vw_InventoryStatus AS
SELECT
    i.InventoryID,
    i.ItemName,
    i.Category,
    i.Unit,
    i.CurrentStock,
    i.MinStockLevel,
    CASE WHEN i.CurrentStock <= i.MinStockLevel THEN 'Low'
         WHEN i.CurrentStock <= i.MinStockLevel * 2 THEN 'Medium'
         ELSE 'Good'
    END AS StockStatus,
    i.ExpiryDate,
    CASE WHEN i.ExpiryDate IS NOT NULL AND i.ExpiryDate <= DATEADD(DAY,7,GETDATE()) THEN 1 ELSE 0 END AS ExpiringSoon,
    s.SupplierName
FROM Inventory i
LEFT JOIN Suppliers s ON i.SupplierID = s.SupplierID
WHERE i.IsActive = 1;
GO

-- ============================================================
-- VIEW: vw_ActiveReservations
-- ============================================================
CREATE OR ALTER VIEW vw_ActiveReservations AS
SELECT
    r.ReservationID,
    r.ReservationDate,
    r.StartTime,
    r.EndTime,
    r.GuestCount,
    r.Status,
    r.SpecialRequests,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    c.Phone,
    t.TableNumber,
    t.Capacity
FROM Reservations r
INNER JOIN Customers c    ON r.CustomerID = c.CustomerID
INNER JOIN CafeTables t   ON r.TableID    = t.TableID
WHERE r.Status NOT IN ('Completed','Cancelled','NoShow')
  AND r.ReservationDate >= CAST(GETDATE() AS DATE);
GO

-- ============================================================
-- VIEW: vw_KitchenQueue
-- ============================================================
CREATE OR ALTER VIEW vw_KitchenQueue AS
SELECT
    od.OrderDetailID,
    o.OrderNumber,
    o.OrderType,
    t.TableNumber,
    p.ProductName,
    od.Quantity,
    od.KitchenStatus,
    od.Notes,
    o.OrderDate,
    DATEDIFF(MINUTE, o.OrderDate, GETDATE()) AS WaitMinutes
FROM OrderDetails od
INNER JOIN Orders o    ON od.OrderID    = o.OrderID
INNER JOIN Products p  ON od.ProductID  = p.ProductID
LEFT  JOIN CafeTables t ON o.TableID   = t.TableID
WHERE od.KitchenStatus IN ('Pending','Preparing')
  AND o.Status NOT IN ('Completed','Cancelled')
ORDER BY (SELECT NULL);
GO

-- ============================================================
-- SCALAR FUNCTION: Calculate loyalty tier discount
-- ============================================================
CREATE OR ALTER FUNCTION fn_GetLoyaltyDiscount(@CustomerID INT)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @Level NVARCHAR(20), @Discount DECIMAL(5,2);
    SELECT @Level = MembershipLevel FROM Customers WHERE CustomerID = @CustomerID;
    SET @Discount = CASE @Level
        WHEN 'VIP'      THEN 15.0
        WHEN 'Platinum' THEN 10.0
        WHEN 'Gold'     THEN 7.0
        WHEN 'Silver'   THEN 5.0
        ELSE 0.0
    END;
    RETURN ISNULL(@Discount, 0);
END
GO

-- ============================================================
-- TABLE-VALUED FUNCTION: Get orders by date range
-- ============================================================
CREATE OR ALTER FUNCTION fn_GetOrdersByDateRange
    (@StartDate DATE, @EndDate DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT o.OrderID, o.OrderNumber, o.OrderDate,
           o.OrderType, o.Status, o.TotalAmount,
           c.FirstName + ' ' + c.LastName AS CustomerName,
           e.FirstName + ' ' + e.LastName AS CashierName,
           t.TableNumber
    FROM Orders o
    LEFT JOIN Customers c   ON o.CustomerID  = c.CustomerID
    LEFT JOIN Employees e   ON o.EmployeeID  = e.EmployeeID
    LEFT JOIN CafeTables t  ON o.TableID     = t.TableID
    WHERE CAST(o.OrderDate AS DATE) BETWEEN @StartDate AND @EndDate
);
GO

-- ============================================================
-- SEED DATA: Roles
-- ============================================================
INSERT INTO Roles (RoleName, Description) VALUES
    ('Admin',         'Full system access'),
    ('Manager',       'Manage operations, reports, inventory'),
    ('Cashier',       'POS, billing, order management'),
    ('Waiter',        'Take orders, table management'),
    ('KitchenStaff',  'Kitchen display, update food status');
GO

-- ============================================================
-- SEED DATA: Admin user (password: Admin@123)
-- Hash is SHA-256 of "Admin@123" with salt "SMARTCAFE2024"
-- ============================================================
INSERT INTO Users (Username, PasswordHash, PasswordSalt, RoleID, Email) VALUES
('admin',
 'computed_at_runtime',  -- Will be set by application on first run
 'SMARTCAFE2024',
 1,
 'admin@smartcafe.com');
GO

-- ============================================================
-- SEED DATA: Categories
-- ============================================================
INSERT INTO Categories (CategoryName, Description, DisplayOrder) VALUES
    ('Hot Beverages',   'Coffee, Tea, Hot Chocolate',  1),
    ('Cold Beverages',  'Iced drinks, Smoothies, Juices', 2),
    ('Breakfast',       'Morning meals and snacks',    3),
    ('Main Course',     'Lunch and dinner items',      4),
    ('Desserts',        'Cakes, Pastries, Ice Cream',  5),
    ('Sandwiches',      'Wraps, Subs, Paninis',        6),
    ('Snacks',          'Fries, Nachos, Starters',     7),
    ('Combos',          'Value meal combinations',     8);
GO

-- ============================================================
-- SEED DATA: Products
-- ============================================================
INSERT INTO Products (CategoryID, ProductName, Description, Price, CostPrice, PrepTimeMin) VALUES
    (1, 'Espresso',           'Single shot espresso',          180,  30, 3),
    (1, 'Cappuccino',         'Espresso with steamed milk',    280,  50, 5),
    (1, 'Latte',              'Espresso with lots of milk',    300,  55, 5),
    (1, 'Green Tea',          'Premium Japanese green tea',    220,  25, 4),
    (1, 'Hot Chocolate',      'Rich Belgian hot chocolate',    320,  60, 5),
    (2, 'Iced Latte',         'Cold espresso with milk',       320,  60, 4),
    (2, 'Mango Smoothie',     'Fresh mango blended drink',     350,  80, 5),
    (2, 'Fresh Lime Soda',    'Refreshing lime drink',         180,  25, 3),
    (3, 'Pancakes',           'Fluffy pancakes with syrup',    420,  90, 10),
    (3, 'Eggs Benedict',      'Poached eggs on muffin',        580, 120, 12),
    (4, 'Grilled Chicken',    'Herb grilled chicken breast',   850, 200, 20),
    (4, 'Pasta Arabiata',     'Spicy tomato pasta',            650, 140, 15),
    (5, 'Chocolate Cake',     'Slice of rich chocolate cake',  380,  70,  2),
    (5, 'Cheesecake',         'New York style cheesecake',     420,  80,  2),
    (6, 'Club Sandwich',      'Triple decker club sandwich',   550, 110, 10),
    (7, 'French Fries',       'Crispy golden fries',           280,  50,  8),
    (7, 'Onion Rings',        'Beer battered onion rings',     320,  60,  8),
    (8, 'Breakfast Combo',    'Eggs + Toast + Tea',            680, 150, 15),
    (8, 'Lunch Combo',        'Main + Drink + Dessert',        980, 220, 20);
GO

-- ============================================================
-- SEED DATA: Cafe Tables
-- ============================================================
INSERT INTO CafeTables (TableNumber, Capacity, Location) VALUES
    ('T01', 2,  'Indoor'),  ('T02', 2,  'Indoor'),
    ('T03', 4,  'Indoor'),  ('T04', 4,  'Indoor'),
    ('T05', 4,  'Indoor'),  ('T06', 6,  'Indoor'),
    ('T07', 6,  'Outdoor'), ('T08', 4,  'Outdoor'),
    ('T09', 2,  'Outdoor'), ('T10', 8,  'VIP'),
    ('T11', 4,  'VIP'),     ('T12', 2,  'Window');
GO

-- ============================================================
-- SEED DATA: Settings
-- ============================================================
INSERT INTO Settings (SettingKey, SettingValue, Category, Description) VALUES
    ('CafeName',       'SmartCafe Pro',  'General', 'Name of the cafe'),
    ('CafeAddress',    '123 Coffee St',  'General', 'Cafe address'),
    ('CafePhone',      '+92-300-0000000','General', 'Contact number'),
    ('CafeEmail',      'info@smartcafe.com','General','Contact email'),
    ('CurrencySymbol', 'PKR',            'Finance', 'Currency symbol'),
    ('TaxRate',        '16',             'Finance', 'Tax rate percentage'),
    ('LoyaltyRate',    '10',             'Finance', 'PKR per loyalty point'),
    ('Theme',          'Dark',           'UI',      'Application theme'),
    ('Language',       'en-US',          'UI',      'Application language'),
    ('ReceiptFooter',  'Thank you for visiting SmartCafe Pro!', 'General', 'Receipt footer text'),
    ('BackupPath',     'C:\\SmartCafeBackup', 'System', 'Auto backup path'),
    ('AutoBackup',     'true',           'System', 'Enable auto backup');
GO

-- ============================================================
-- SEED DATA: Inventory
-- ============================================================
INSERT INTO Inventory (ItemName, Category, Unit, CurrentStock, MinStockLevel, CostPerUnit) VALUES
    ('Coffee Beans',     'Beverages', 'kg',   10.0,  2.0, 1500),
    ('Milk',             'Dairy',     'ltr',  20.0,  5.0,  120),
    ('Sugar',            'Basics',    'kg',    5.0,  1.0,   80),
    ('Flour',            'Basics',    'kg',    8.0,  2.0,   90),
    ('Eggs',             'Dairy',     'pcs',  60.0, 12.0,   20),
    ('Butter',           'Dairy',     'kg',    2.0,  0.5,  500),
    ('Chicken Breast',   'Proteins',  'kg',    5.0,  1.0,  600),
    ('Pasta',            'Dry Goods', 'kg',    4.0,  1.0,  150),
    ('Tomatoes',         'Vegetables','kg',    3.0,  1.0,   80),
    ('Bread',            'Bakery',    'pcs',  10.0,  5.0,   35),
    ('Mango',            'Fruits',    'kg',    4.0,  1.0,  200),
    ('Chocolate',        'Baking',    'kg',    2.0,  0.5,  800),
    ('Cream Cheese',     'Dairy',     'kg',    1.5,  0.5,  700),
    ('Potatoes',         'Vegetables','kg',    6.0,  2.0,   60),
    ('Green Tea Bags',   'Beverages', 'pcs', 100.0, 20.0,   10);
GO

PRINT 'Triggers, views, functions, and seed data created successfully.';
GO
