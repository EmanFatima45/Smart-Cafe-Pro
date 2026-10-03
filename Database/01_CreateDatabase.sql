
-- SmartCafe Pro - Database Creation Script



USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SmartCafeProDB')
BEGIN
    ALTER DATABASE SmartCafeProDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SmartCafeProDB;
END
GO

CREATE DATABASE SmartCafeProDB
    COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE SmartCafeProDB;
GO
USE SmartCafeProDB;
GO

DECLARE @ConstraintName NVARCHAR(200);

SELECT @ConstraintName = kc.name
FROM sys.key_constraints kc
WHERE kc.parent_object_id = OBJECT_ID('Products')
AND kc.type = 'UQ';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE Products DROP CONSTRAINT ' + @ConstraintName);
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'UX_Products_Barcode_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Products_Barcode_NotNull
    ON Products(Barcode)
    WHERE Barcode IS NOT NULL;
END
GO


-- TABLE: Roles

CREATE TABLE Roles (
    RoleID      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(50)  NOT NULL UNIQUE,
    Description NVARCHAR(200) NULL,
    Permissions NVARCHAR(MAX) NULL,   -- JSON string of permissions
    IsActive    BIT           NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO


-- TABLE: Users

CREATE TABLE Users (
    UserID        INT IDENTITY(1,1) PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(256) NOT NULL,
    PasswordSalt  NVARCHAR(128) NOT NULL,
    RoleID        INT           NOT NULL REFERENCES Roles(RoleID),
    Email         NVARCHAR(100) NULL UNIQUE,
    IsActive      BIT           NOT NULL DEFAULT 1,
    RememberToken NVARCHAR(256) NULL,
    LastLogin     DATETIME2     NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETDATE(),
    UpdatedAt     DATETIME2     NULL
);
GO

-- TABLE: Employees

CREATE TABLE Employees (
    EmployeeID   INT IDENTITY(1,1) PRIMARY KEY,
    UserID       INT            NULL REFERENCES Users(UserID),
    FirstName    NVARCHAR(50)   NOT NULL,
    LastName     NVARCHAR(50)   NOT NULL,
    CNIC         NVARCHAR(15)   NULL UNIQUE,
    Phone        NVARCHAR(20)   NULL,
    Email        NVARCHAR(100)  NULL,
    Address      NVARCHAR(300)  NULL,
    DateOfBirth  DATE           NULL,
    HireDate     DATE           NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Department   NVARCHAR(100)  NULL,
    Position     NVARCHAR(100)  NULL,
    Salary       DECIMAL(10,2)  NOT NULL DEFAULT 0,
    PhotoPath    NVARCHAR(500)  NULL,
    IsActive     BIT            NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME2      NULL
);
GO


-- TABLE: Customers

CREATE TABLE Customers (
    CustomerID      INT IDENTITY(1,1) PRIMARY KEY,
    FirstName       NVARCHAR(50)  NOT NULL,
    LastName        NVARCHAR(50)  NOT NULL,
    Phone           NVARCHAR(20)  NULL UNIQUE,
    Email           NVARCHAR(100) NULL UNIQUE,
    Address         NVARCHAR(300) NULL,
    DateOfBirth     DATE          NULL,
    MembershipLevel NVARCHAR(20)  NOT NULL DEFAULT 'Bronze'
        CHECK (MembershipLevel IN ('Bronze','Silver','Gold','Platinum','VIP')),
    TotalSpent      DECIMAL(12,2) NOT NULL DEFAULT 0,
    LoyaltyPoints   INT           NOT NULL DEFAULT 0,
    IsActive        BIT           NOT NULL DEFAULT 1,
    JoinDate        DATETIME2     NOT NULL DEFAULT GETDATE(),
    Notes           NVARCHAR(500) NULL,
    UpdatedAt       DATETIME2     NULL
);
GO


-- TABLE: Categories

CREATE TABLE Categories (
    CategoryID   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description  NVARCHAR(300) NULL,
    IconPath     NVARCHAR(500) NULL,
    DisplayOrder INT           NOT NULL DEFAULT 0,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO


-- TABLE: Products (Menu Items)
-- TABLE: Products (Menu Items) - CORRECTED

CREATE TABLE Products (
    ProductID     INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID    INT            NOT NULL REFERENCES Categories(CategoryID),
    ProductName   NVARCHAR(150)  NOT NULL,
    Description   NVARCHAR(500)  NULL,
    Price         DECIMAL(10,2)  NOT NULL CHECK (Price >= 0),
    CostPrice     DECIMAL(10,2)  NOT NULL DEFAULT 0,
    ImagePath     NVARCHAR(500)  NULL,
    IsCombo       BIT            NOT NULL DEFAULT 0,
    IsAvailable   BIT            NOT NULL DEFAULT 1,
    PrepTimeMin   INT            NOT NULL DEFAULT 5,
    Calories      INT            NULL,
    Allergens     NVARCHAR(200)  NULL,

    
    Barcode       NVARCHAR(50)   NULL,

    DisplayOrder  INT            NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt     DATETIME2      NULL
);
GO

-- TABLE: Tables (Cafe Tables)

CREATE TABLE CafeTables (
    TableID      INT IDENTITY(1,1) PRIMARY KEY,
    TableNumber  NVARCHAR(10)  NOT NULL UNIQUE,
    Capacity     INT           NOT NULL DEFAULT 4,
    Location     NVARCHAR(100) NULL,   -- e.g. Indoor, Outdoor, VIP
    Status       NVARCHAR(20)  NOT NULL DEFAULT 'Available'
        CHECK (Status IN ('Available','Reserved','Occupied','Cleaning')),
    QRCode       NVARCHAR(500) NULL,
    IsActive     BIT           NOT NULL DEFAULT 1
);
GO

-- TABLE: Orders

CREATE TABLE Orders (
    OrderID       INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber   NVARCHAR(20)   NOT NULL UNIQUE,
    CustomerID    INT            NULL REFERENCES Customers(CustomerID),
    TableID       INT            NULL REFERENCES CafeTables(TableID),
    EmployeeID    INT            NULL REFERENCES Employees(EmployeeID),
    OrderType     NVARCHAR(20)   NOT NULL DEFAULT 'Dine-In'
        CHECK (OrderType IN ('Dine-In','Takeaway','Delivery')),
    Status        NVARCHAR(30)   NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending','Confirmed','Preparing','Ready','Served','Completed','Cancelled')),
    SubTotal      DECIMAL(12,2)  NOT NULL DEFAULT 0,
    DiscountAmt   DECIMAL(10,2)  NOT NULL DEFAULT 0,
    TaxAmt        DECIMAL(10,2)  NOT NULL DEFAULT 0,
    TotalAmount   DECIMAL(12,2)  NOT NULL DEFAULT 0,
    Notes         NVARCHAR(500)  NULL,
    OrderDate     DATETIME2      NOT NULL DEFAULT GETDATE(),
    CompletedAt   DATETIME2      NULL,
    UpdatedAt     DATETIME2      NULL
);
GO


-- TABLE: OrderDetails

CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID       INT           NOT NULL REFERENCES Orders(OrderID) ON DELETE CASCADE,
    ProductID     INT           NOT NULL REFERENCES Products(ProductID),
    Quantity      INT           NOT NULL CHECK (Quantity > 0),
    UnitPrice     DECIMAL(10,2) NOT NULL,
    Discount      DECIMAL(5,2)  NOT NULL DEFAULT 0,
    LineTotal     AS (Quantity * UnitPrice * (1 - Discount/100)) PERSISTED,
    KitchenStatus NVARCHAR(20)  NOT NULL DEFAULT 'Pending'
        CHECK (KitchenStatus IN ('Pending','Preparing','Ready','Served')),
    Notes         NVARCHAR(200) NULL
);
GO


-- TABLE: Payments
CREATE TABLE Payments (
    PaymentID     INT IDENTITY(1,1) PRIMARY KEY,
    OrderID       INT            NOT NULL REFERENCES Orders(OrderID),
    PaymentMethod NVARCHAR(30)   NOT NULL
        CHECK (PaymentMethod IN ('Cash','Card','Online','Wallet','Split')),
    Amount        DECIMAL(12,2)  NOT NULL,
    Reference     NVARCHAR(100)  NULL,   -- Card last4, transaction ref
    Status        NVARCHAR(20)   NOT NULL DEFAULT 'Completed'
        CHECK (Status IN ('Pending','Completed','Failed','Refunded')),
    ProcessedBy   INT            NULL REFERENCES Employees(EmployeeID),
    PaymentDate   DATETIME2      NOT NULL DEFAULT GETDATE()
);
GO


-- TABLE: Reservations

CREATE TABLE Reservations (
    ReservationID   INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID      INT           NOT NULL REFERENCES Customers(CustomerID),
    TableID         INT           NOT NULL REFERENCES CafeTables(TableID),
    ReservationDate DATE          NOT NULL,
    StartTime       TIME          NOT NULL,
    EndTime         TIME          NOT NULL,
    GuestCount      INT           NOT NULL DEFAULT 1,
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Confirmed'
        CHECK (Status IN ('Pending','Confirmed','Seated','Completed','Cancelled','NoShow')),
    SpecialRequests NVARCHAR(500) NULL,
    CreatedBy       INT           NULL REFERENCES Employees(EmployeeID),
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME2     NULL
);
GO

-- TABLE: Suppliers

CREATE TABLE Suppliers (
    SupplierID   INT IDENTITY(1,1) PRIMARY KEY,
    SupplierName NVARCHAR(150) NOT NULL,
    ContactName  NVARCHAR(100) NULL,
    Phone        NVARCHAR(20)  NULL,
    Email        NVARCHAR(100) NULL,
    Address      NVARCHAR(300) NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO


-- TABLE: Inventory

CREATE TABLE Inventory (
    InventoryID   INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID    INT            NULL REFERENCES Suppliers(SupplierID),
    ItemName      NVARCHAR(150)  NOT NULL,
    Category      NVARCHAR(100)  NULL,
    Unit          NVARCHAR(30)   NOT NULL DEFAULT 'pcs',
    CurrentStock  DECIMAL(10,2)  NOT NULL DEFAULT 0,
    MinStockLevel DECIMAL(10,2)  NOT NULL DEFAULT 5,
    CostPerUnit   DECIMAL(10,2)  NOT NULL DEFAULT 0,
    ExpiryDate    DATE           NULL,
    Location      NVARCHAR(100)  NULL,
    IsActive      BIT            NOT NULL DEFAULT 1,
    LastUpdated   DATETIME2      NOT NULL DEFAULT GETDATE()
);
GO

-- TABLE: PurchaseOrders

CREATE TABLE PurchaseOrders (
    POID         INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID   INT           NOT NULL REFERENCES Suppliers(SupplierID),
    InventoryID  INT           NOT NULL REFERENCES Inventory(InventoryID),
    Quantity     DECIMAL(10,2) NOT NULL,
    UnitCost     DECIMAL(10,2) NOT NULL,
    TotalCost    AS (Quantity * UnitCost) PERSISTED,
    Status       NVARCHAR(20)  NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending','Ordered','Received','Cancelled')),
    OrderDate    DATETIME2     NOT NULL DEFAULT GETDATE(),
    ReceivedDate DATETIME2     NULL,
    Notes        NVARCHAR(300) NULL,
    CreatedBy    INT           NULL REFERENCES Employees(EmployeeID)
);
GO

-- TABLE: Attendance

CREATE TABLE Attendance (
    AttendanceID  INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeID    INT          NOT NULL REFERENCES Employees(EmployeeID),
    AttendDate    DATE         NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CheckIn       TIME         NULL,
    CheckOut      TIME         NULL,
    Status        NVARCHAR(20) NOT NULL DEFAULT 'Present'
        CHECK (Status IN ('Present','Absent','Late','HalfDay','Leave')),
    OvertimeHrs   DECIMAL(4,2) NOT NULL DEFAULT 0,
    Notes         NVARCHAR(200) NULL,
    CONSTRAINT UQ_Attendance UNIQUE (EmployeeID, AttendDate)
);
GO

-- TABLE: Salaries

CREATE TABLE Salaries (
    SalaryID      INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeID    INT           NOT NULL REFERENCES Employees(EmployeeID),
    Month         INT           NOT NULL CHECK (Month BETWEEN 1 AND 12),
    Year          INT           NOT NULL,
    BaseSalary    DECIMAL(10,2) NOT NULL,
    Allowances    DECIMAL(10,2) NOT NULL DEFAULT 0,
    OvertimePay   DECIMAL(10,2) NOT NULL DEFAULT 0,
    Deductions    DECIMAL(10,2) NOT NULL DEFAULT 0,
    NetSalary     AS (BaseSalary + Allowances + OvertimePay - Deductions) PERSISTED,
    PaidDate      DATE          NULL,
    Status        NVARCHAR(20)  NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending','Paid','Hold')),
    CONSTRAINT UQ_Salary UNIQUE (EmployeeID, Month, Year)
);
GO

-- TABLE: LoyaltyPoints

CREATE TABLE LoyaltyPoints (
    LoyaltyID    INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID   INT           NOT NULL REFERENCES Customers(CustomerID),
    OrderID      INT           NULL REFERENCES Orders(OrderID),
    Points       INT           NOT NULL,
    TransType    NVARCHAR(10)  NOT NULL CHECK (TransType IN ('Earn','Redeem','Bonus','Expire')),
    Description  NVARCHAR(200) NULL,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

-- TABLE: Feedback

CREATE TABLE Feedback (
    FeedbackID   INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID   INT          NULL REFERENCES Customers(CustomerID),
    OrderID      INT          NULL REFERENCES Orders(OrderID),
    Rating       TINYINT      NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Category     NVARCHAR(50) NOT NULL DEFAULT 'General',
    Comment      NVARCHAR(MAX) NULL,
    IsResolved   BIT          NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2    NOT NULL DEFAULT GETDATE()
);
GO
 
-- TABLE: Notifications

CREATE TABLE Notifications (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    Title          NVARCHAR(200) NOT NULL,
    Message        NVARCHAR(MAX) NOT NULL,
    Type           NVARCHAR(50)  NOT NULL DEFAULT 'Info'
        CHECK (Type IN ('Info','Warning','Alert','Success','Error')),
    TargetRole     NVARCHAR(50)  NULL,
    IsRead         BIT           NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2     NOT NULL DEFAULT GETDATE(),
    ExpiresAt      DATETIME2     NULL
);
GO


-- TABLE: AuditLogs

CREATE TABLE AuditLogs (
    AuditID     INT IDENTITY(1,1) PRIMARY KEY,
    TableName   NVARCHAR(100) NOT NULL,
    Operation   NVARCHAR(10)  NOT NULL CHECK (Operation IN ('INSERT','UPDATE','DELETE','LOGIN','LOGOUT')),
    RecordID    INT           NULL,
    OldValues   NVARCHAR(MAX) NULL,
    NewValues   NVARCHAR(MAX) NULL,
    UserID      INT           NULL REFERENCES Users(UserID),
    IPAddress   NVARCHAR(50)  NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

-- TABLE: Settings

CREATE TABLE Settings (
    SettingID    INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey   NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX) NOT NULL,
    Category     NVARCHAR(50)  NOT NULL DEFAULT 'General',
    Description  NVARCHAR(300) NULL,
    UpdatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);
GO

-- INDEXES for performance

CREATE NONCLUSTERED INDEX IX_Orders_Date       ON Orders(OrderDate);
CREATE NONCLUSTERED INDEX IX_Orders_Status     ON Orders(Status);
CREATE NONCLUSTERED INDEX IX_Orders_Customer   ON Orders(CustomerID);
CREATE NONCLUSTERED INDEX IX_OrderDetails_Ord  ON OrderDetails(OrderID);
CREATE NONCLUSTERED INDEX IX_Payments_Order    ON Payments(OrderID);
CREATE NONCLUSTERED INDEX IX_Attendance_Emp    ON Attendance(EmployeeID, AttendDate);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Table   ON AuditLogs(TableName, CreatedAt);
CREATE NONCLUSTERED INDEX IX_Products_Cat      ON Products(CategoryID);
CREATE NONCLUSTERED INDEX IX_LoyaltyPts_Cust   ON LoyaltyPoints(CustomerID);
GO

PRINT 'Database and tables created successfully.';
GO
