-- ============================================================
-- SmartCafe Pro - Database Setup Script (Run This First)
-- Run as: sqlcmd -S localhost -i 04_SetupAdmin.sql
-- Or paste into SSMS and execute on master database
-- ============================================================

USE SmartCafeProDB;
GO

-- ============================================================
-- Step 1: Create a proper Admin user with hashed password
-- Default credentials: admin / Admin@123
-- The application's AuthService will verify this on login.
--
-- SHA-256("Admin@123" + "SCPRO_SALT_2024") in Base64:
-- Computed at runtime via AuthService.HashPassword()
--
-- To set via SQL directly (for initial setup), run the app
-- once and use the DatabaseSetupForm, then reset via:
-- ============================================================

-- Fix the placeholder admin password inserted in seed data
-- The app will handle hashing on first run if password = 'NEEDS_SETUP'
UPDATE Users
SET PasswordHash = 'NEEDS_SETUP',
    PasswordSalt = 'SCPRO_SALT_2024'
WHERE Username = 'admin' AND PasswordHash = 'computed_at_runtime';
GO

-- ============================================================
-- Step 2: Create sample employees linked to user accounts
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Employees WHERE FirstName = 'System')
BEGIN
    INSERT INTO Employees (UserID, FirstName, LastName, Phone, Department, Position, Salary, HireDate)
    SELECT UserID, 'System', 'Admin', '+92-300-0000000',
           'Management', 'System Administrator', 80000, GETDATE()
    FROM Users WHERE Username = 'admin';
END
GO

-- ============================================================
-- Step 3: Create additional demo users (optional)
-- Passwords all set to "Pass@123" — change after first login
-- ============================================================
DECLARE @ManagerRoleID INT, @CashierRoleID INT, @WaiterRoleID INT, @KitchenRoleID INT;
SELECT @ManagerRoleID = RoleID FROM Roles WHERE RoleName = 'Manager';
SELECT @CashierRoleID = RoleID FROM Roles WHERE RoleName = 'Cashier';
SELECT @WaiterRoleID  = RoleID FROM Roles WHERE RoleName = 'Waiter';
SELECT @KitchenRoleID = RoleID FROM Roles WHERE RoleName = 'KitchenStaff';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager1')
BEGIN
    INSERT INTO Users (Username, PasswordHash, PasswordSalt, RoleID, Email)
    VALUES ('manager1', 'NEEDS_SETUP', 'SCPRO_SALT_2024', @ManagerRoleID, 'manager@smartcafe.com');

    INSERT INTO Employees (UserID, FirstName, LastName, Phone, Department, Position, Salary, HireDate)
    SELECT UserID, 'Ahmed', 'Khan', '+92-321-1111111',
           'Management', 'Cafe Manager', 65000, GETDATE()
    FROM Users WHERE Username = 'manager1';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'cashier1')
BEGIN
    INSERT INTO Users (Username, PasswordHash, PasswordSalt, RoleID, Email)
    VALUES ('cashier1', 'NEEDS_SETUP', 'SCPRO_SALT_2024', @CashierRoleID, 'cashier@smartcafe.com');

    INSERT INTO Employees (UserID, FirstName, LastName, Phone, Department, Position, Salary, HireDate)
    SELECT UserID, 'Fatima', 'Ali', '+92-333-2222222',
           'Front Office', 'Cashier', 35000, GETDATE()
    FROM Users WHERE Username = 'cashier1';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'waiter1')
BEGIN
    INSERT INTO Users (Username, PasswordHash, PasswordSalt, RoleID, Email)
    VALUES ('waiter1', 'NEEDS_SETUP', 'SCPRO_SALT_2024', @WaiterRoleID, 'waiter@smartcafe.com');

    INSERT INTO Employees (UserID, FirstName, LastName, Phone, Department, Position, Salary, HireDate)
    SELECT UserID, 'Hassan', 'Raza', '+92-345-3333333',
           'Service', 'Head Waiter', 28000, GETDATE()
    FROM Users WHERE Username = 'waiter1';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'chef1')
BEGIN
    INSERT INTO Users (Username, PasswordHash, PasswordSalt, RoleID, Email)
    VALUES ('chef1', 'NEEDS_SETUP', 'SCPRO_SALT_2024', @KitchenRoleID, 'chef@smartcafe.com');

    INSERT INTO Employees (UserID, FirstName, LastName, Phone, Department, Position, Salary, HireDate)
    SELECT UserID, 'Zainab', 'Malik', '+92-311-4444444',
           'Kitchen', 'Head Chef', 45000, GETDATE()
    FROM Users WHERE Username = 'chef1';
END
GO

-- ============================================================
-- Step 4: Insert sample customers for demo
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Phone = '0300-1234567')
BEGIN
    INSERT INTO Customers (FirstName, LastName, Phone, Email, MembershipLevel, LoyaltyPoints, TotalSpent)
    VALUES
        ('Ali',     'Hassan',   '0300-1234567', 'ali@email.com',     'Gold',    1200, 25000),
        ('Sara',    'Ahmed',    '0321-2345678', 'sara@email.com',    'Silver',   450,  8000),
        ('Omar',    'Khan',     '0333-3456789', 'omar@email.com',    'Platinum',2800, 65000),
        ('Ayesha',  'Malik',    '0345-4567890', 'ayesha@email.com',  'Bronze',   80,   1200),
        ('Bilal',   'Siddiqui', '0311-5678901', 'bilal@email.com',   'VIP',     5500, 130000),
        ('Mariam',  'Farooq',   '0322-6789012', 'mariam@email.com',  'Gold',    1850, 38000),
        ('Usman',   'Sheikh',   '0333-7890123', 'usman@email.com',   'Silver',   620, 11000),
        ('Hina',    'Baig',     '0300-8901234', 'hina@email.com',    'Bronze',   130,  2400);
END
GO

-- ============================================================
-- Step 5: Add sample suppliers
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Suppliers WHERE SupplierName = 'Premium Coffee Co.')
BEGIN
    INSERT INTO Suppliers (SupplierName, ContactName, Phone, Email, Address) VALUES
        ('Premium Coffee Co.',  'Tariq Mehmood',  '042-1234567', 'info@premiumcoffee.pk', 'Lahore, Punjab'),
        ('Fresh Dairy Farm',    'Iqbal Hussain',  '041-2345678', 'dairy@freshmilk.pk',    'Faisalabad, Punjab'),
        ('City Bakery Supply',  'Nadeem Akhtar',  '021-3456789', 'supply@citybakery.pk',  'Karachi, Sindh'),
        ('Green Farms Produce', 'Rizwan Chaudhry','051-4567890', 'veg@greenfarms.pk',     'Islamabad'),
        ('Spice House Trading', 'Kamran Mirza',   '042-5678901', 'orders@spicehouse.pk',  'Lahore, Punjab');
END
GO

-- ============================================================
-- Step 6: Link inventory to suppliers
-- ============================================================
DECLARE @CoffeeSup INT, @DairySup INT, @BakerySup INT, @VegSup INT;
SELECT @CoffeeSup = SupplierID FROM Suppliers WHERE SupplierName = 'Premium Coffee Co.';
SELECT @DairySup  = SupplierID FROM Suppliers WHERE SupplierName = 'Fresh Dairy Farm';
SELECT @BakerySup = SupplierID FROM Suppliers WHERE SupplierName = 'City Bakery Supply';
SELECT @VegSup    = SupplierID FROM Suppliers WHERE SupplierName = 'Green Farms Produce';

UPDATE Inventory SET SupplierID = @CoffeeSup WHERE ItemName = 'Coffee Beans';
UPDATE Inventory SET SupplierID = @CoffeeSup WHERE ItemName = 'Green Tea Bags';
UPDATE Inventory SET SupplierID = @DairySup  WHERE ItemName IN ('Milk','Eggs','Butter','Cream Cheese');
UPDATE Inventory SET SupplierID = @BakerySup WHERE ItemName IN ('Flour','Bread','Chocolate','Pasta');
UPDATE Inventory SET SupplierID = @VegSup    WHERE ItemName IN ('Tomatoes','Potatoes','Mango');
GO

-- ============================================================
-- Step 7: Insert sample feedback entries
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Feedback)
BEGIN
    INSERT INTO Feedback (CustomerID, Rating, Category, Comment)
    SELECT c.CustomerID, 5, 'Food Quality', 'Excellent cappuccino! Best in the city.'
    FROM Customers c WHERE c.Phone = '0300-1234567';

    INSERT INTO Feedback (CustomerID, Rating, Category, Comment)
    SELECT c.CustomerID, 4, 'Service', 'Great service, slightly slow during peak hours.'
    FROM Customers c WHERE c.Phone = '0321-2345678';

    INSERT INTO Feedback (CustomerID, Rating, Category, Comment)
    SELECT c.CustomerID, 3, 'Ambience', 'Nice place but could use better lighting.'
    FROM Customers c WHERE c.Phone = '0333-3456789';

    INSERT INTO Feedback (CustomerID, Rating, Category, Comment)
    SELECT c.CustomerID, 5, 'Overall', 'Love the loyalty program! Keep it up.'
    FROM Customers c WHERE c.Phone = '0311-5678901';
END
GO

-- ============================================================
-- Step 8: Create a sample reservation
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Reservations)
BEGIN
    INSERT INTO Reservations (CustomerID, TableID, ReservationDate, StartTime, EndTime, GuestCount, Status, SpecialRequests)
    SELECT
        (SELECT TOP 1 CustomerID FROM Customers WHERE MembershipLevel = 'VIP'),
        (SELECT TOP 1 TableID FROM CafeTables WHERE Location = 'VIP'),
        CAST(DATEADD(DAY, 1, GETDATE()) AS DATE),
        '19:00', '21:00', 4,
        'Confirmed',
        'Birthday celebration — please arrange a cake.';
END
GO

-- ============================================================
-- Step 9: Add sample notifications
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Notifications)
BEGIN
    INSERT INTO Notifications (Title, Message, Type, TargetRole) VALUES
        ('Welcome to SmartCafe Pro!',
         'System initialized successfully. Please change your default passwords.',
         'Success', NULL),
        ('Low Stock Alert: Coffee Beans',
         'Coffee Beans stock is running low. Current: 10 kg, Minimum: 2 kg. Please reorder.',
         'Warning', 'Manager'),
        ('New Reservation',
         'VIP table reserved for tomorrow evening (7 PM - 9 PM). Birthday celebration.',
         'Info', 'Manager');
END
GO

-- ============================================================
-- Step 10: Verification query
-- ============================================================
SELECT 'Database setup complete!' AS Status;
SELECT 'Users'        AS TableName, COUNT(*) AS Records FROM Users
UNION ALL SELECT 'Roles',       COUNT(*) FROM Roles
UNION ALL SELECT 'Employees',   COUNT(*) FROM Employees
UNION ALL SELECT 'Customers',   COUNT(*) FROM Customers
UNION ALL SELECT 'Categories',  COUNT(*) FROM Categories
UNION ALL SELECT 'Products',    COUNT(*) FROM Products
UNION ALL SELECT 'CafeTables',  COUNT(*) FROM CafeTables
UNION ALL SELECT 'Inventory',   COUNT(*) FROM Inventory
UNION ALL SELECT 'Suppliers',   COUNT(*) FROM Suppliers
UNION ALL SELECT 'Reservations',COUNT(*) FROM Reservations
UNION ALL SELECT 'Feedback',    COUNT(*) FROM Feedback
UNION ALL SELECT 'Notifications',COUNT(*) FROM Notifications
UNION ALL SELECT 'Settings',    COUNT(*) FROM Settings;
GO

PRINT '✓ SmartCafe Pro database is ready!';
PRINT '  Default login: admin / Admin@123  (change on first use)';
GO
